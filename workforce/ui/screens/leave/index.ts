/**
 * Leave & Requests — one screen, two tabs, one approver.
 *
 * Composes the balances, the request list, the approval queue and the open
 * swap. What it owns is the tab state and the counts.
 */

import { formatNumber, type HostApi, load, type PropertyEnvironment } from "@hotelos/sdk";

import { control, el } from "../../chrome/element";
import { failureScreen } from "../../chrome/failure";
import { ROSTER_READ } from "../../chrome/permissions";
import { type LeaveBoard } from "../../roster/leave";
import { noSwap, queue, swapCard } from "./approvals";
import { decision } from "./decision";
import { requestForm } from "./form";
import { balances, requests } from "./requests";
import { withdrawRequest } from "./withdraw";

/**
 * What is open over this screen, and which row each thing is open on.
 *
 * One object rather than seven trailing parameters. Two of them arrived with
 * the decision panel and two more with withdrawal, and a positional list that
 * long is one a caller gets wrong silently — `leave(h, m, tab, go, false, f, g,
 * null, h)` says nothing about which `null` is which.
 */
export interface LeavePlace {
  /** Whether the request form is open. */
  dialog?: boolean;

  /** Open it. */
  open?: () => void;

  /** Close whatever is open. */
  close?: () => void;

  /** Which waiting row the approver's decision panel is open on. */
  chosen?: string | null;

  /** Open the panel on one, or close it by choosing it again. */
  onChoose?: (id: string) => void;

  /** Which of this person's own requests is being withdrawn, when one is. */
  withdrawing?: string | null;

  /** Open the withdraw dialog on one, or close it with `null`. */
  onWithdraw?: (id: string | null) => void;
}

/**
 * Draw the screen.
 *
 * @param host the bridge
 * @param main the container
 * @param tab which tab is open
 * @param go called when the other tab is chosen
 * @param place what is open over it
 */
export async function leave(
  host: HostApi,
  main: HTMLElement,
  tab: string,
  go: (tab: string) => void,
  place: LeavePlace = {},
): Promise<void> {
  const {
    dialog = false,
    open = () => {},
    close = () => {},
    chosen = null,
    onChoose = () => {},
    withdrawing = null,
    onWithdraw = () => {},
  } = place;

  const got = await load<LeaveBoard>(host, ROSTER_READ, "leave");

  // No fallback - `APPS-Q26(4)`. A failed read renders the failure,
  // never a recorded list with an apology under it.
  if (!got.ok) {
    failureScreen(main, "Leave & Requests", got.failure, { the: "leave" }, host.property,
      () => void leave(host, main, tab, go, place));
    return;
  }

  const board = got.value;

  const body = el("div", "body");

  if (tab === "Approvals") {
    // Side by side, as the drawing has it. Stacked, the swap's own preview
    // table fell past the bottom of the screen — the table was built and
    // simply could not be reached, which is the worst shape a layout
    // divergence takes.
    // **A row opens a decision panel beside the queue** — owner, 2026-09-20,
    // `64g` §4 B. The panel replaces the swap card's place when a row is open:
    // the approver is deciding one thing, and two panes competing for that
    // column would be two decisions offered at once.
    const open_ = board.waiting.find((row) => row.id === chosen) ?? null;

    const split = el("div", "asplit");
    split.append(queue(board.waiting, host.property, chosen, onChoose),
      open_ !== null
        ? decision(host, open_, host.property, () => { onChoose(open_.id); })
        : board.swap === null ? noSwap() : swapCard(board.swap, host.property));
    body.append(split);
  } else {
    // **The person's own rows carry a withdraw** — owner, 2026-09-20, `64g`
    // §4 B. `leave.request · withdraw` has taken an id and a version all
    // along, and nothing on this screen could reach it.
    body.append(
      balances(board.balances, host.property),
      requests(board.requests, host.property, (row) => { onWithdraw(row.id); }));
  }

  main.replaceChildren(header(board, open, host.property),
    tabs(board, tab, go, host.property), body);

  // The form chooses nothing on the person's behalf. This passed two people as
  // literals and the overdrawn balance, *"because that is the one the frame
  // raises the warning against — a form that showed a healthy balance would
  // demonstrate nothing"* — the drawing's demonstration, shipped as a request
  // (the app surface audit, 2026-09-19). And it opened only when the board had
  // a balance, so on a property with none the button silently did nothing.
  //
  // It raises now: the types come from the read, whose leave is the caller's
  // (ADR 0172), and closing re-draws this screen, which re-reads the board.
  if (dialog) main.append(requestForm(host, board.balances, close, close));

  // Withdrawing is a second dialog, and `withdrawing` names the row rather
  // than being a second boolean: two open at once is not expressible.
  const mine = board.requests.find((row) => row.id === withdrawing);

  if (mine !== undefined) {
    main.append(withdrawRequest(host, mine, host.property,
      () => { onWithdraw(null); }, () => { onWithdraw(null); }));
  }
}

/** The header, with counts derived from the board. */
function header(
  board: LeaveBoard, open: () => void, property: PropertyEnvironment,
): HTMLElement {
  const head = el("div", "tools");
  const title = el("div");
  const n = (value: number): string => formatNumber(value, property, "whole");

  const pending = board.requests.filter((row) => row.state === "Requested").length;

  const swaps = board.waiting.filter((item) => item.kind === "Swap").length;

  title.append(
    el("div", "hsub",
      `${n(board.waiting.length)} waiting · ${n(board.waiting.length - swaps)} leave`
      + ` · ${n(swaps)} swap · ${n(pending)} of mine pending`),
  );

  const grow = el("div", "grow");
  const raise = control("btn pri", "＋ Request leave", open);

  head.append(title, grow, raise);
  return head;
}

/** The two tabs, the second carrying what is waiting. */
function tabs(
  board: LeaveBoard, current: string, go: (tab: string) => void, property: PropertyEnvironment,
): HTMLElement {
  const row = el("div", "tabs");

  for (const label of ["Requests", "Approvals"]) {
    // A button, so a keyboard reaches it: a `div` with a click listener worked
    // under a mouse and was not there for anyone else (tests/no-dead-controls).
    const tab = control(label === current ? "tab on" : "tab", label, () => go(label));

    // The count comes from the queue itself — the same list the tab opens.
    if (label === "Approvals") {
      // The chrome styles `.tab .cnt` as a pill. A bare `s` here rendered
      // "Approvals3" — the number welded to the word, which is what an
      // element with no rule looks like.
      tab.append(el("span", "cnt", formatNumber(board.waiting.length, property, "whole")));
    }

    row.append(tab);
  }

  return row;
}

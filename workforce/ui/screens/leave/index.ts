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
import { requestForm } from "./form";
import { balances, requests } from "./requests";

/**
 * Draw the screen.
 *
 * @param host the bridge
 * @param main the container
 * @param tab which tab is open
 * @param go called when the other tab is chosen
 */
export async function leave(
  host: HostApi,
  main: HTMLElement,
  tab: string,
  go: (tab: string) => void,
  dialog = false,
  open: () => void = () => {},
  close: () => void = () => {},
): Promise<void> {
  const got = await load<LeaveBoard>(host, ROSTER_READ, "leave");

  // No fallback - `APPS-Q26(4)`. A failed read renders the failure,
  // never a recorded list with an apology under it.
  if (!got.ok) {
    failureScreen(main, "Leave & Requests", got.failure, { the: "leave" }, host.property,
      () => void leave(host, main, tab, go, dialog, open, close));
    return;
  }

  const board = got.value;

  const body = el("div", "body");

  if (tab === "Approvals") {
    // Side by side, as the drawing has it. Stacked, the swap's own preview
    // table fell past the bottom of the screen — the table was built and
    // simply could not be reached, which is the worst shape a layout
    // divergence takes.
    const split = el("div", "asplit");
    split.append(queue(board.waiting, host.property),
      board.swap === null ? noSwap() : swapCard(board.swap, host.property));
    body.append(split);
  } else {
    body.append(balances(board.balances, host.property), requests(board.requests, host.property));
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

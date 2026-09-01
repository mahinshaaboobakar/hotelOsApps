/**
 * Leave & Requests — one screen, two tabs, one approver.
 *
 * Composes the balances, the request list, the approval queue and the open
 * swap. What it owns is the tab state and the counts.
 */

import type { HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { load } from "../../roster";
import { recordedLeave, type LeaveBoard } from "../../roster/leave";
import { queue, swapCard } from "./approvals";
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
): Promise<void> {
  const got = await load(host, ROSTER_READ, "leave", recordedLeave);
  const board = got.value;

  const body = el("div", "body");

  if (tab === "Approvals") {
    body.append(queue(board.waiting), swapCard(board.swap));
  } else {
    body.append(balances(board.balances), requests(board.requests));
  }

  if (!got.live) {
    body.append(standIn());
  }

  main.replaceChildren(header(board), tabs(board, tab, go), body);
}

/** The header, with counts derived from the board. */
function header(board: LeaveBoard): HTMLElement {
  const head = el("div", "head");
  const title = el("div");

  const pending = board.requests.filter((row) => row.state === "Requested").length;

  title.append(
    el("div", "ht", "Leave & Requests"),
    el("div", "hsub", `Requests · ${pending} pending`),
  );

  const grow = el("div", "grow");
  head.append(title, grow, el("div", "btn go", "＋ Request leave"));
  return head;
}

/** The two tabs, the second carrying what is waiting. */
function tabs(board: LeaveBoard, current: string, go: (tab: string) => void): HTMLElement {
  const row = el("div", "tabs");

  for (const label of ["Requests", "Approvals"]) {
    const tab = el("div", label === current ? "tab on" : "tab", label);

    // The count comes from the queue itself — the same list the tab opens.
    if (label === "Approvals") {
      tab.append(el("s", undefined, String(board.waiting.length)));
    }

    tab.addEventListener("click", () => go(label));
    row.append(tab);
  }

  return row;
}

/** ADR 0124: it fails in place and names what it awaits. */
function standIn(): HTMLElement {
  const panel = el("div", "panel");
  const note = el("div", "note");

  note.append(
    el("b", undefined, "Showing the approved example. "),
    el("span", undefined, "The desktop has no Workforce client yet."),
  );

  panel.append(note);
  return panel;
}

/**
 * Supervision — the rooms that are the supervisor's today, each with its
 * decision (frame 5; S5 c9, S4, S0). Every decision carries a name, a time and
 * a reason; a DND approved is a full record, not an absence.
 */

import type { HostApi } from "@hotelos/sdk";

import { failed } from "../../chrome/failure";
import { pager, scroller } from "../../chrome/bar";
import { control, el } from "../../chrome/element";
import { clock, nearDay, when } from "../../chrome/instant";
import { READ, act, holds, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { actions, dialog } from "../../chrome/overlay";
import { ordinal, reason } from "../../chrome/words";
import type { Paging } from "../../model";

interface LaneRow {
  supervisionId: string | null;
  roomId: string;
  room: string;
  roomVersion: number;
  reason: string;
  since: string;
  sinceDay: string | null;
  whatWeKnow: { text: string; at: string | null; day: string | null }[];
  days: number | null;
  taskId: string | null;
  taskVersion: number | null;
  decision: string | null;
  decidedBy: string | null;
  decidedAt: string | null;
  note: string | null;
}

interface Lane {
  needDecision: number;
  decidedToday: number;
  at: string;
  rows: LaneRow[];
  paging: Paging;
}

export async function supervision(host: HostApi, body: HTMLElement, nav: Nav, page: number, goPage: (page: number) => void): Promise<void> {
  const got = await load<Lane>(host, READ, "supervision", { page });
  if (!got.ok) {
    body.append(failed(host, got.failure, "the supervision lane", nav.show));
    return;
  }

  const lane = got.value;
  const strip = el("div", "strip");
  const count = (n: number, label: string): HTMLElement => { const c = el("span"); c.append(el("b", undefined, String(n)), document.createTextNode(label)); return c; };
  strip.append(count(lane.needDecision, "need a decision"), count(lane.decidedToday, "decided today"), el("span", "end", when(host, lane.at)));

  const table = el("table", "list");
  const head = el("tr");
  for (const name of ["Room", "Why it is here", "Since", "What we know", "Decision"]) head.append(el("th", undefined, name));
  table.append(head);
  for (const row of lane.rows) {
    const tr = el("tr", row.decision === null ? "pick" : "pick dim");
    tr.addEventListener("click", (event) => { if ((event.target as HTMLElement).closest("button") === null) nav.openRoom(row.roomId); });
    const known = row.whatWeKnow.map((k) => (k.day !== null ? `${k.text} ${nearDay(host, k.day)}` : k.at === null ? k.text : `${k.text} ${clock(host, k.at)}`)).join(" · ");
    const since = el("td");
    since.append(el("b", undefined, row.sinceDay !== null ? nearDay(host, row.sinceDay) : clock(host, row.since)));
    tr.append(el("td", "num", row.room), why(row), since, el("td", undefined, known), decisionCell(host, nav, row));
    table.append(tr);
  }

  body.append(strip, scroller(table), pager(lane.paging, lane.rows.length, "rooms in the lane", goPage));
}

function why(row: LaneRow): HTMLElement {
  const cell = el("td");
  if (row.decision !== null) cell.append(document.createTextNode(`${row.reason === "DAYS_WITHOUT_SERVICE" ? "Days without service" : reason(row.reason)} — decided`));
  else if (row.reason === "DAYS_WITHOUT_SERVICE") cell.append(el("span", "pill soft-bad", row.days === null ? "days without service" : `${ordinal(row.days)} day without service`));
  else cell.append(el("span", `pill ${row.reason === "DISAGREEMENT" ? "soft-warn" : "soft-bad"}`, reason(row.reason)));
  return cell;
}

function decisionCell(host: HostApi, nav: Nav, row: LaneRow): HTMLElement {
  const cell = el("td");
  if (row.decision !== null) {
    cell.append(el("span", "dim", `${row.decidedBy ?? "name not on record"} · ${clock(host, row.decidedAt)}${row.note === null ? "" : ` · "${row.note}"`}`));
    return cell;
  }
  if (!holds(host, "roomcare.amend")) {
    cell.append(el("span", "dim", "a supervisor's decision"));
    return cell;
  }
  const buttons = el("div", "row");
  buttons.style.gap = "4px";
  if (row.reason === "DISAGREEMENT") {
    buttons.append(control("btn sm", "Keep ours", () => void clear(host, nav, row, "OURS")), control("btn sm", "Take theirs", () => void clear(host, nav, row, "THEIRS")));
  } else if (row.reason === "NOBODY_AVAILABLE") {
    buttons.append(control("btn sm", "Assign anyway…", () => nav.openRoom(row.roomId)), control("btn sm", "Leave for reconcile", () => decide(host, nav, row, "OTHER", "left for the next press")));
  } else {
    buttons.append(
      control("btn sm", "DND approved — no cleaning", () => decide(host, nav, row, "DND_APPROVED", null)),
      control("btn sm", "Clean it", () => decide(host, nav, row, "CLEAN", null)),
      control("btn sm", "Other…", () => decide(host, nav, row, "OTHER", "")),
    );
  }
  cell.append(buttons);
  return cell;
}

async function clear(host: HostApi, nav: Nav, row: LaneRow, kept: string): Promise<void> {
  const done = await act(host, "roomcare.amend", "clearDisagreement", { roomId: row.roomId, version: row.roomVersion, kept });
  if (done.ok) nav.show();
  else confirmFailed(nav, done.because);
}

/** A decision is final, so it is confirmed in a dialog — with a note when it is "other". */
function decide(host: HostApi, nav: Nav, row: LaneRow, decision: string, note: string | null): void {
  const overlay = dialog(nav.frame, `Room ${row.room} — ${decision === "DND_APPROVED" ? "DND approved" : decision === "CLEAN" ? "clean it" : "the supervisor's call"}`);
  const text = el("textarea", "field") as HTMLTextAreaElement;
  text.value = note ?? "";
  overlay.body.append(el("p", undefined, "A supervisor's decision is final and yours on the record."));
  if (note !== null) overlay.body.append(el("label", "lbl", "Note"), text);
  actions(overlay, "Decide", () => void (async () => {
    const done = await act(host, "roomcare.amend", "decide", { supervisionId: row.supervisionId, decision, note: text.value.trim() === "" ? null : text.value.trim() });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })());
}

function confirmFailed(nav: Nav, because: string): void {
  const overlay = dialog(nav.frame, "Not done");
  overlay.body.append(el("p", "said bad", because));
  overlay.foot.append(control("btn", "Close", () => overlay.close()));
}

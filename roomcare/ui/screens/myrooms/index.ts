/**
 * My rooms — an attendant's list, what the supervisor accepted, in the
 * ladder's order (frame 3; S0, S5 c3/c4). No "pick your own rooms", no zone
 * picker, no capacity number; no restock line until Inventory is installed.
 */

import type { HostApi } from "@hotelos/sdk";

import { failed } from "../../chrome/failure";
import { pager, scroller } from "../../chrome/bar";
import { el } from "../../chrome/element";
import { clock, minutes } from "../../chrome/instant";
import { load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { ordinal, service } from "../../chrome/words";
import type { Outcome, Paging } from "../../model";
import { door } from "./door";

export interface MyRoom {
  taskId: string;
  version: number;
  roomId: string;
  room: string;
  service: string;
  reduction: string | null;
  soldAt: string | null;
  priority: number;
  earliestAt: string | null;
  linen: string;
  declinedDay: number | null;
  state: Outcome;
  recheckAt: string | null;
}

interface MyRooms {
  rooms: number;
  done: number;
  inProgress: number;
  plannedMinutes: number;
  at: string;
  rows: MyRoom[];
  paging: Paging;
}

export async function myRooms(host: HostApi, body: HTMLElement, nav: Nav, taskId: string | null, open: (taskId: string | null) => void): Promise<void> {
  if (taskId !== null) return door(host, body, nav, taskId, () => open(null));
  const got = await load<MyRooms>(host, "myRooms", { page: 0 });
  if (!got.ok) {
    body.append(failed(got.failure, "your rooms", nav.show));
    return;
  }

  const v = got.value;
  const strip = el("div", "strip");
  const count = (text: string, label: string): HTMLElement => { const c = el("span"); c.append(el("b", undefined, text), document.createTextNode(label)); return c; };
  strip.append(count(String(v.rooms), "rooms"), count(String(v.done), "done"), count(String(v.inProgress), "in progress"),
    el("span", undefined, `${minutes(v.plannedMinutes)} planned`), el("span", "end", clock(host, v.at)));

  const table = el("table");
  const head = el("tr");
  for (const name of ["Room", "Service", "Pri", "Earliest", "Linen", "State"]) head.append(el("th", undefined, name));
  table.append(head);
  for (const row of v.rows) {
    const tr = el("tr", "pick");
    tr.addEventListener("click", () => open(row.taskId));
    const what = el("td");
    what.append(document.createTextNode(service(row.service)));
    if (row.soldAt !== null) what.append(document.createTextNode(" · "), el("span", "mono", `arrival ${clock(host, row.soldAt)}`));
    if (row.reduction !== null) what.append(document.createTextNode(" · "), el("i", undefined, row.reduction));
    const pri = el("td");
    pri.append(el("span", `pill p${Math.min(row.priority, 3)}`, String(row.priority)));
    const earliest = el("td");
    earliest.append(row.earliestAt === null ? document.createTextNode("—") : el("b", undefined, clock(host, row.earliestAt)));
    tr.append(el("td", "num", row.room), what, pri, earliest, linen(row), stateCell(host, row));
    table.append(tr);
  }

  body.append(strip, scroller(table), pager(v.paging, v.rows.length, "rooms assigned to you today", () => {}));
}

function linen(row: MyRoom): HTMLElement {
  const cell = el("td");
  if (row.declinedDay !== null) cell.append(document.createTextNode(`declined day ${row.declinedDay}`));
  else if (row.linen === "STRIP") cell.append(document.createTextNode("strip"));
  else if (row.linen === "DUE" || row.linen === "MUST") cell.append(el("span", "pill warn", row.linen.toLowerCase()));
  else cell.append(document.createTextNode("—"));
  return cell;
}

function stateCell(host: HostApi, row: MyRoom): HTMLElement {
  const cell = el("td");
  const tone: Record<string, string> = { IN_PROGRESS: "run", DONE: "ok", INSPECTION_REQUESTED: "ok", READY: "ok", PARTIAL: "ok", DND: "warn", DECLINED: "warn" };
  const text = stateText(host, row);
  const kind = row.state.kind;
  if (tone[kind] !== undefined) cell.append(el("span", `pill ${tone[kind]}`, text));
  else cell.append(el("span", kind === "WAITING" ? "dim" : undefined, text));
  return cell;
}

/** The row's state, as the attendant reads it. */
export function stateText(host: HostApi, row: MyRoom): string {
  const s = row.state;
  switch (s.kind) {
    case "IN_PROGRESS": return `IN PROGRESS · since ${clock(host, s.at)}`;
    case "DONE": return `DONE ${clock(host, s.at)}`;
    case "INSPECTION_REQUESTED": return `DONE ${clock(host, s.at)} · inspection requested`;
    case "READY": return `READY ${clock(host, s.at)}`;
    case "PARTIAL": return `PARTIAL ${clock(host, s.at)} · ${s.detail ?? ""}`;
    case "DECLINED": return `DECLINED ${clock(host, s.at)}`;
    case "DND": return `DND ${clock(host, s.at)} · re-check due ${clock(host, row.recheckAt)}`;
    case "WAITING": return `waiting for ${clock(host, s.until)}`;
    case "SUPERVISION": return s.days === null || s.days === undefined ? "the supervisor's room" : `the supervisor's room · ${ordinal(s.days)} day`;
    case "ENDED": return (s.detail ?? "ended").toLowerCase().replaceAll("_", " ");
    default: return "to do";
  }
}

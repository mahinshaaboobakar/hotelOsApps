/**
 * My rooms — an attendant's list, what the supervisor accepted, in the
 * ladder's order (frame 3; S0, S5 c3/c4). No "pick your own rooms", no zone
 * picker, no capacity number; no restock line until Inventory is installed.
 */

import type { HostApi } from "@hotelos/sdk";

import { pager } from "../../chrome/bar";
import { el } from "../../chrome/element";
import { clock, minutes } from "../../chrome/instant";
import { failed, load } from "../../chrome/load";
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
    body.append(failed("Your rooms", got.because));
    return;
  }

  const v = got.value;
  const strip = el("div", "strip");
  const count = (text: string, label: string): HTMLElement => { const c = el("span"); c.append(el("b", undefined, text), document.createTextNode(label)); return c; };
  strip.append(count(String(v.rooms), "rooms"), count(String(v.done), "done"), count(String(v.inProgress), "in progress"), count(minutes(v.plannedMinutes), "planned"), el("span", "end", clock(host, v.at)));

  const table = el("table", "list");
  const head = el("tr");
  for (const name of ["Room", "Service", "Pri", "Earliest", "Linen", "State"]) head.append(el("th", undefined, name));
  table.append(head);
  for (const row of v.rows) {
    const tr = el("tr", "pick");
    tr.addEventListener("click", () => open(row.taskId));
    const what = [service(row.service), row.soldAt === null ? null : `arrival ${clock(host, row.soldAt)}`, row.reduction].filter((x) => x !== null).join(" · ");
    const linen = row.declinedDay !== null ? `declined day ${row.declinedDay}` : row.linen === "STRIP" ? "strip" : row.linen === "NOT_DUE" ? "—" : row.linen.toLowerCase();
    tr.append(el("td", "num", row.room), el("td", undefined, what), el("td", "num", String(row.priority)), el("td", "num", row.earliestAt === null ? "—" : clock(host, row.earliestAt)),
      el("td", undefined, linen), el("td", undefined, stateText(host, row)));
    table.append(tr);
  }

  body.append(strip, table, pager(v.paging, v.rows.length, "rooms assigned to you today", () => {}));
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
    case "SUPERVISION": return `the supervisor's room · ${ordinal(s.days ?? 1)} day`;
    case "ENDED": return (s.detail ?? "ended").toLowerCase().replaceAll("_", " ");
    default: return "to do";
  }
}

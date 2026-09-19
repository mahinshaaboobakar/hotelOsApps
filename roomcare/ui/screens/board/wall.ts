/**
 * The wall — every room one line, every column, grouped by zone with sticky
 * headers carrying the zone's counts, a zone collapsible, no pages (frame 1b).
 * "250 of 250 — no pages": a stated departure from page 64 §6 the owner ruled.
 */

import type { HostApi } from "@hotelos/sdk";

import { el, fill, opener } from "../../chrome/element";
import { clock } from "../../chrome/instant";
import type { Nav } from "../../chrome/nav";
import { whole } from "../../chrome/number";
import { conditionClass, ordinal, service, source } from "../../chrome/words";
import type { Board, BoardRoom } from "../../model";
import type { Lit } from "./index";

const COLUMNS = ["Room", "Condition", "Occ.", "Sold tonight", "Service", "Pri", "Attendant", "Outcome so far", "Linen"];

export function wall(host: HostApi, data: Board, lit: Lit, collapsed: Set<string>, redraw: () => void, nav: Nav): HTMLElement {
  const house = el("div", "house");
  const table = el("table", "wall");
  const headRow = el("tr");
  for (const name of COLUMNS) headRow.append(el("th", undefined, name));
  const thead = el("thead");
  thead.append(headRow);
  const tbody = el("tbody");
  let total = 0;
  for (const zone of data.zones) {
    const c = zone.counts;
    const group = el("tr", "g");
    const extra = [c.dnd > 0 ? `${whole(host, c.dnd)} DND` : null, c.blocked > 0 ? `${whole(host, c.blocked)} blocked` : null, c.supervision > 0 ? `${whole(host, c.supervision)} supervision` : null].filter((x) => x !== null);
    const fold = opener(`${collapsed.has(zone.name) ? "▸" : "▾"} ${[zone.name, `${whole(host, c.rooms)} rooms`, `${whole(host, c.dirty)} dirty`, `${whole(host, c.inProgress)} in progress`, `${whole(host, c.ready)} ready`, ...extra].join(" · ")}`);
    fold.setAttribute("aria-expanded", String(!collapsed.has(zone.name)));
    const cell = el("td");
    cell.append(fold);
    cell.setAttribute("colspan", String(COLUMNS.length));
    group.append(cell);
    group.addEventListener("click", () => {
      if (collapsed.has(zone.name)) collapsed.delete(zone.name);
      else collapsed.add(zone.name);
      redraw();
    });
    tbody.append(group);
    total += zone.rooms.length;
    if (collapsed.has(zone.name)) continue;
    for (const room of zone.rooms) tbody.append(line(host, room, lit(room), nav));
  }
  table.append(thead, tbody);
  house.append(table, el("div", "legend", `${whole(host, total)} of ${whole(host, total)} — no pages · the whole house scrolls within the window`));
  return house;
}

function line(host: HostApi, room: BoardRoom, lit: boolean, nav: Nav): HTMLElement {
  const row = el("tr", lit ? "pick" : "pick dim");
  row.addEventListener("click", () => nav.openRoom(room.id));
  const condition = el("td");
  condition.append(el("i", `glyph ${conditionClass(room.condition)}`), document.createTextNode(`${room.condition.toLowerCase()} `));
  if (room.source !== "SYSTEM") condition.append(el("span", "src", `${room.setBy?.split(" ")[0] ?? source(room.source)} ${clock(host, room.setAt)}`));
  if (room.marks.manual) condition.append(el("span", "tag man", "manual"));
  if (room.marks.disagreement) condition.append(el("span", "tag port", "!"));
  const occ = room.marks.blocked ? "out of order" : room.vacantDays !== null && room.vacantDays > 0 ? `vacant · ${whole(host, room.vacantDays)} days` : room.occupancy.toLowerCase();
  const serviceText = room.marks.blocked ? "— blocked" : room.service === null ? "—"
    : [service(room.service), room.reduction, room.earliestAt !== null ? `not before ${clock(host, room.earliestAt)}` : null].filter((x) => x !== null).join(" · ");
  const attendant = room.attendant ?? (room.outcome.kind === "NOBODY_AVAILABLE" ? "— nobody available" : room.marks.pending ? "— pending policy" : room.outcome.kind === "SUPERVISION" ? "— supervisor's room" : "—");
  row.append(
    fill(el("td", "num"), opener(room.number)),
    condition,
    el("td", undefined, occ),
    el("td", undefined, room.soldAt !== null && room.marks.soldTonight ? `★ ${clock(host, room.soldAt)}` : "—"),
    el("td", undefined, serviceText),
    el("td", undefined, room.priority === null ? "" : whole(host, room.priority)),
    el("td", room.attendant === null ? "dim" : undefined, attendant),
    el("td", ["WAITING", "PENDING", "NEW_SINCE", "NOBODY_AVAILABLE"].includes(room.outcome.kind) ? "dim" : undefined, outcome(host, room)),
    el("td", undefined, room.linen === null || room.service !== "DAILY_SERVICE" ? "—" : room.linen.toLowerCase()),
  );
  return row;
}

/** "Outcome so far", worded — the kinds the backend names, never re-derived here. */
export function outcome(host: HostApi, room: Pick<BoardRoom, "outcome">): string {
  const o = room.outcome;
  switch (o.kind) {
    case "IN_PROGRESS": return `in progress · ${clock(host, o.at)}`;
    case "DONE": return `done ${clock(host, o.at)}`;
    case "INSPECTION_REQUESTED": return `done ${clock(host, o.at)} · inspection requested`;
    case "READY": return `ready ${clock(host, o.at)}`;
    case "PARTIAL": return `done ${clock(host, o.at)} · partial (${o.detail ?? ""})`;
    case "DECLINED": return `declined ${clock(host, o.at)}`;
    case "DND": return `⏸ DND ${clock(host, o.at)} · re-check ${clock(host, o.until)}`;
    case "WAITING": return `waiting for ${clock(host, o.until)}`;
    case "SUPERVISION": return o.days === null || o.days === undefined ? "⚑ days without service" : `⚑ ${ordinal(o.days)} day without service`;
    case "DISAGREEMENT": return `! PMS says ${(o.detail ?? "").toLowerCase()} ${clock(host, o.at)}`;
    case "PENDING": return `${(o.detail ?? "").includes("may wait") ? "may wait" : "pending"} · one click`;
    case "NOBODY_AVAILABLE": return "nobody available";
    case "NEW_SINCE": return `new since ${clock(host, o.at)}`;
    case "BLOCKED": return "off the day";
    case "ENDED": return `${(o.detail ?? "ended").toLowerCase().replaceAll("_", " ")} ${clock(host, o.at)}`;
    default: return "—";
  }
}

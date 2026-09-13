/**
 * 4c · the sheet — the wall with editable cells, source · when on every row,
 * a changed row tinted, a conflicting row red, and "set for all" on a selection.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, option } from "../../chrome/element";
import { clock } from "../../chrome/instant";
import type { Nav } from "../../chrome/nav";
import { source } from "../../chrome/words";
import type { RoomStates, StateRow } from "../../model";
import type { Edits } from "./edits";

export const CONDITIONS = ["DIRTY", "CLEAN", "INSPECTED"];
export const OCCUPANCIES = ["VACANT", "OCCUPIED"];
export const STAYS: readonly (readonly [string, string])[] = [["DEPARTED", "departed"], ["ARRIVED", "arrived"], ["IN_HOUSE", "in house"], ["NONE", "none"]];

export function sheetView(host: HostApi, data: RoomStates, edits: Edits, conflicts: ReadonlySet<string>, redraw: () => void, nav: Nav): HTMLElement {
  const house = el("div", "house");
  const selected = new Set<string>();
  const table = el("table", "wall");
  const head = el("tr");
  for (const name of ["", "Room", "Condition", "Occupancy", "Sold tonight", "Stay", "Source · when", ""]) head.append(el("th", undefined, name));
  const thead = el("thead");
  thead.append(head);
  const tbody = el("tbody");
  for (const zone of data.zones) {
    const group = el("tr", "g");
    const changed = zone.rooms.filter((r) => edits.has(r.roomId)).length;
    const cell = el("td", undefined, `▾ ${zone.name} · ${zone.rooms.length} rooms${changed > 0 ? ` · ${changed} changed` : ""}`);
    cell.setAttribute("colspan", "8");
    group.append(cell);
    tbody.append(group);
    for (const row of zone.rooms) tbody.append(line(host, row, edits, conflicts, selected, redraw, nav));
  }
  table.append(thead, tbody);
  house.append(table, dock(selected, edits, redraw, data.rooms));
  return house;
}

function line(host: HostApi, row: StateRow, edits: Edits, conflicts: ReadonlySet<string>, selected: Set<string>, redraw: () => void, nav: Nav): HTMLElement {
  const tr = el("tr", conflicts.has(row.roomId) ? "conf" : edits.has(row.roomId) ? "chg" : "");
  const open = control("btn sm", "▸", () => nav.openRoom(row.roomId));
  if (row.blocked) {
    const cell = el("td", "dim", "blocked · deep clean · out of order (its owner) — not editable here");
    cell.setAttribute("colspan", "6");
    tr.append(el("td"), el("td", "num", row.number), cell, el("td").appendChild(open).parentElement as HTMLElement);
    return tr;
  }
  const box = el("input") as HTMLInputElement;
  box.type = "checkbox";
  box.setAttribute("aria-label", `select ${row.number}`);
  box.addEventListener("change", () => (box.checked ? selected.add(row.roomId) : selected.delete(row.roomId)));
  const pick = (fact: "condition" | "occupancy" | "stay", options: readonly (readonly [string, string])[]): HTMLElement => {
    const select = el("select", edits.has(row.roomId) && edits.value(row, fact) !== row[fact] ? "cell chg" : "cell") as HTMLSelectElement;
    for (const [value, label] of options) select.append(option(label, value, edits.value(row, fact) === value));
    select.addEventListener("change", () => { edits.set(row.roomId, fact, select.value); redraw(); });
    return select;
  };
  const sold = el("input", "cell") as HTMLInputElement;
  sold.type = "time";
  sold.value = row.soldAt === null ? "" : clock(host, row.soldAt);
  sold.addEventListener("change", () => { edits.set(row.roomId, "soldAt", sold.value === "" ? null : sold.value); redraw(); });
  const mark = edits.has(row.roomId) ? `${row.number} ✎` : row.number;
  const origin = conflicts.has(row.roomId) ? `${source(row.source)} changed it since — look again` : `${row.sourceBy ?? source(row.source)} ${clock(host, row.sourceAt)}${edits.has(row.roomId) ? " · unsaved" : ""}`;
  const cells = [el("td"), el("td", "num", mark), el("td"), el("td"), el("td"), el("td"), el("td", "mono", origin), el("td")];
  cells[0]!.append(box);
  cells[2]!.append(pick("condition", CONDITIONS.map((c) => [c, c.toLowerCase()] as const)));
  cells[3]!.append(pick("occupancy", OCCUPANCIES.map((c) => [c, c.toLowerCase()] as const)));
  cells[4]!.append(sold);
  cells[5]!.append(pick("stay", STAYS));
  cells[7]!.append(open);
  tr.append(...cells);
  return tr;
}

function dock(selected: Set<string>, edits: Edits, redraw: () => void, total: number): HTMLElement {
  const bar = el("div", "dock");
  const condition = el("select", "cell") as HTMLSelectElement;
  condition.append(option("condition", ""), ...CONDITIONS.map((c) => option(c.toLowerCase(), c)));
  const occupancy = el("select", "cell") as HTMLSelectElement;
  occupancy.append(option("occupancy", ""), ...OCCUPANCIES.map((c) => option(c.toLowerCase(), c)));
  const stay = el("select", "cell") as HTMLSelectElement;
  stay.append(option("stay", ""), ...STAYS.map(([v, l]) => option(l, v)));
  bar.append(el("span", undefined, "set for the selected rooms:"), condition, occupancy, stay,
    control("btn sm", "Apply to selected", () => {
      for (const id of selected) {
        if (condition.value !== "") edits.set(id, "condition", condition.value);
        if (occupancy.value !== "") edits.set(id, "occupancy", occupancy.value);
        if (stay.value !== "") edits.set(id, "stay", stay.value);
      }
      redraw();
    }),
    el("span", "grow dim", `${total} of ${total} — no pages`));
  return bar;
}

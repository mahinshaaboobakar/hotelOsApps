/**
 * 4e · compact — segmented controls, one tap sets a value, two zones side by
 * side: the whole house in about half the scroll of the sheet.
 */

import { control, el, option } from "../../chrome/element";
import type { Nav } from "../../chrome/nav";
import type { RoomStates, StateRow } from "../../model";
import type { Edits } from "./edits";
import { STAYS } from "./sheet";

export function compact(data: RoomStates, edits: Edits, conflicts: ReadonlySet<string>, redraw: () => void, nav: Nav): HTMLElement {
  const house = el("div", "house");
  const pairs = el("div", "pairs");
  for (const zone of data.zones) {
    const table = el("table", "wall");
    const changed = zone.rooms.filter((r) => edits.has(r.roomId)).length;
    const group = el("tr", "g");
    const cell = el("td", undefined, `▾ ${zone.name} · ${zone.rooms.length}${changed > 0 ? ` · ${changed} changed` : ""}`);
    cell.setAttribute("colspan", "5");
    group.append(cell);
    const head = el("tr");
    for (const name of ["Room", "Condition", "Occ.", "Stay", ""]) head.append(el("th", undefined, name));
    table.append(group, head);
    for (const row of zone.rooms) table.append(line(row, edits, conflicts, redraw, nav));
    pairs.append(table);
  }
  house.append(pairs, el("div", "legend", `${data.rooms} of ${data.rooms} — no pages · source and time on the room's page`));
  return house;
}

function line(row: StateRow, edits: Edits, conflicts: ReadonlySet<string>, redraw: () => void, nav: Nav): HTMLElement {
  const tr = el("tr", conflicts.has(row.roomId) ? "conf" : edits.has(row.roomId) ? "chg" : "");
  const open = el("td");
  open.append(control("btn sm", "▸", () => nav.openRoom(row.roomId)));
  if (row.blocked) {
    const cell = el("td", "dim", "blocked · deep clean — not editable here");
    cell.setAttribute("colspan", "3");
    tr.append(el("td", "num", row.number), cell, open);
    return tr;
  }
  const segs = (fact: "condition" | "occupancy", options: readonly (readonly [string, string])[], tone: string): HTMLElement => {
    const group = el("span", `segs ${tone}`);
    for (const [value, label] of options) {
      const on = edits.value(row, fact) === value;
      const button = control(on ? "on" : "", label, () => { edits.set(row.roomId, fact, value); redraw(); });
      button.setAttribute("aria-pressed", String(on));
      button.title = value.toLowerCase();
      group.append(button);
    }
    return group;
  };
  const condition = edits.value(row, "condition");
  const stay = el("select", "cell") as HTMLSelectElement;
  for (const [value, label] of STAYS) stay.append(option(label, value, edits.value(row, "stay") === value));
  stay.addEventListener("change", () => { edits.set(row.roomId, "stay", stay.value); redraw(); });
  const cells = [el("td", "num", edits.has(row.roomId) ? `${row.number} ✎` : row.number), el("td"), el("td"), el("td")];
  cells[1]!.append(segs("condition", [["DIRTY", "D"], ["CLEAN", "C"], ["INSPECTED", "I"]], condition === "DIRTY" ? "bad" : condition === "CLEAN" ? "ok" : ""));
  cells[2]!.append(segs("occupancy", [["VACANT", "V"], ["OCCUPIED", "O"]], ""));
  cells[3]!.append(stay);
  tr.append(...cells, open);
  return tr;
}

/**
 * 4d · the tap grid — pick what to paint, tap rooms, tap again to undo, save.
 * "Select all" on a zone header is the whole-wing checkout in one gesture.
 */

import { control, el } from "../../chrome/element";
import { conditionClass } from "../../chrome/words";
import type { RoomStates } from "../../model";
import type { Edit, Edits } from "./edits";
import { CONDITIONS, OCCUPANCIES, STAYS } from "./sheet";

/** What the next tap paints — one fact and its value. */
let paint: { fact: keyof Edit; value: string } = { fact: "condition", value: "DIRTY" };

export function grid(data: RoomStates, edits: Edits, conflicts: ReadonlySet<string>, redraw: () => void): HTMLElement {
  const house = el("div", "house");
  const palette = el("div", "chips");
  palette.append(el("span", "lbl", "Paint with:"));
  const choice = (fact: keyof Edit, value: string, label: string): HTMLElement => {
    const on = paint.fact === fact && paint.value === value;
    const button = control(on ? "btn chip on" : "btn chip", label, () => { paint = { fact, value }; redraw(); });
    button.setAttribute("aria-pressed", String(on));
    return button;
  };
  palette.append(el("span", "lbl", "condition"), ...CONDITIONS.map((c) => choice("condition", c, c.toLowerCase())));
  palette.append(el("span", "lbl", "occupancy"), ...OCCUPANCIES.map((c) => choice("occupancy", c, c.toLowerCase())));
  palette.append(el("span", "lbl", "stay"), ...STAYS.map(([v, l]) => choice("stay", v, l)));
  const legend = el("div", "legend", "then tap rooms · tap again to undo · ✎ = unsaved · corner = occupancy (O / V)");
  house.append(palette, legend);

  for (const zone of data.zones) {
    const header = el("div", "grp");
    header.append(el("b", undefined, `${zone.name} · ${zone.rooms.length}`), control("btn sm", `Select all ${zone.rooms.length}`, () => {
      for (const row of zone.rooms) edits.set(row.roomId, paint.fact, paint.value);
      redraw();
    }));
    const tiles = el("div", "tilegrid");
    for (const row of zone.rooms) {
      const condition = edits.value(row, "condition");
      const classes = ["tile", row.blocked ? "blocked" : conditionClass(condition)];
      if (edits.has(row.roomId)) classes.push("chg");
      if (conflicts.has(row.roomId)) classes.push("picked");
      const tile = el("button", classes.join(" "), row.number);
      tile.setAttribute("type", "button");
      tile.append(el("small", undefined, edits.value(row, "occupancy") === "OCCUPIED" ? "O" : "V"));
      tile.title = `${condition.toLowerCase()} · ${edits.value(row, "occupancy").toLowerCase()} · ${edits.value(row, "stay").toLowerCase()}`;
      tile.addEventListener("click", () => {
        if (row.blocked) return;
        if (edits.has(row.roomId)) edits.clear(row.roomId);
        else edits.set(row.roomId, paint.fact, paint.value);
        redraw();
      });
      tiles.append(tile);
    }
    house.append(header, tiles);
  }
  return house;
}

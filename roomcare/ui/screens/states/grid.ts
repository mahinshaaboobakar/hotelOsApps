/**
 * 4d · the tap grid — pick what to paint, tap rooms, tap again to undo, save.
 * "Select all" on a zone header is the whole-wing checkout in one gesture.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el } from "../../chrome/element";
import { clock } from "../../chrome/instant";
import { conditionClass } from "../../chrome/words";
import type { RoomStates } from "../../model";
import type { Edit, Edits } from "./edits";
import { CONDITIONS, OCCUPANCIES, STAYS } from "./sheet";

/** What the next tap paints — one fact and its value. */
let paint: { fact: keyof Edit; value: string } = { fact: "condition", value: "DIRTY" };

export function grid(host: HostApi, data: RoomStates, edits: Edits, conflicts: ReadonlySet<string>, redraw: () => void): HTMLElement[] {
  const palette = el("div", "dock");
  palette.style.margin = "0 0 12px";
  const segs = (fact: keyof Edit, options: readonly (readonly [string, string])[], tone: string): HTMLElement => {
    const group = el("span", `segs ${tone}`);
    for (const [value, label] of options) {
      const on = paint.fact === fact && paint.value === value;
      const button = control(on ? "on" : "", label, () => { paint = { fact, value }; redraw(); });
      button.setAttribute("aria-pressed", String(on));
      group.append(button);
    }
    return group;
  };
  const sold = el("input", "cell") as HTMLInputElement;
  sold.placeholder = "15:00";
  sold.value = paint.fact === "soldAt" ? paint.value : "";
  sold.setAttribute("aria-label", "sold tonight");
  sold.addEventListener("change", () => { paint = { fact: "soldAt", value: sold.value.trim() }; redraw(); });
  palette.append(
    el("b", undefined, "Paint with:"),
    el("span", "dim", "condition"), segs("condition", CONDITIONS.map((c) => [c, c.toLowerCase()] as const), paint.value === "DIRTY" ? "bad" : paint.value === "CLEAN" ? "ok" : ""),
    el("span", "dim", "occupancy"), segs("occupancy", OCCUPANCIES.map((c) => [c, c.toLowerCase()] as const), ""),
    el("span", "dim", "stay"), segs("stay", STAYS, ""),
    el("span", "dim", "sold tonight"), sold,
    el("span", "grow mono", "then tap rooms · tap again to undo · ✎ = unsaved"),
  );
  const legend = el("div", "legend");
  for (const [cls, label] of [["dirty", "dirty"], ["clean", "clean"], ["insp", "inspected"]] as const) {
    const item = el("span");
    item.append(el("i", `sw ${cls}`), document.createTextNode(label));
    legend.append(item);
  }
  legend.append(el("span", undefined, "· small corner = occupancy (O / V) and sold-tonight time · hover = full state and source"));

  const house = el("div", "house");
  for (const zone of data.zones) {
    const header = el("div", "grp mono");
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
      if (!row.blocked) {
        const occupied = edits.value(row, "occupancy") === "OCCUPIED" ? "O" : "V";
        tile.append(el("small", undefined, row.soldAt === null ? occupied : `${occupied} ${clock(host, row.soldAt)}`));
      }
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
  return [palette, legend, house];
}

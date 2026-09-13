/**
 * The map — one tile per room (frame 1a). Fill is the condition and only the
 * condition; the ring is today's service state; the corner marks are the
 * exceptions: ★ sold tonight · ⏸ DND · ! disagreement · ⚑ supervision.
 */

import { el } from "../../chrome/element";
import type { Nav } from "../../chrome/nav";
import { conditionClass, service } from "../../chrome/words";
import type { Board, BoardRoom } from "../../model";
import type { Lit } from "./index";

export function map(data: Board, lit: Lit, nav: Nav): HTMLElement {
  const house = el("div", "house");
  house.append(legend());
  for (const zone of data.zones) {
    const header = el("div", "grp");
    header.append(el("b", undefined, zone.name));
    const c = zone.counts;
    for (const [value, label] of [[c.rooms, "rooms"], [c.dirty, "dirty"], [c.inProgress, "in progress"], [c.ready, "ready"], [c.dnd, "DND"], [c.blocked, "blocked"], [c.supervision, "supervision"]] as const) {
      if (value > 0 || label === "rooms") header.append(el("span", undefined, `${value} ${label}`));
    }
    const grid = el("div", "tilegrid");
    for (const room of zone.rooms) grid.append(tile(room, lit(room), nav));
    house.append(header, grid);
  }
  return house;
}

function tile(room: BoardRoom, lit: boolean, nav: Nav): HTMLElement {
  const classes = ["tile"];
  if (room.marks.blocked) classes.push("blocked");
  else if (room.marks.pending) classes.push("pend");
  else classes.push(conditionClass(room.condition));
  if (room.marks.inProgress) classes.push("run");
  else if (!room.marks.blocked && !room.marks.pending && room.condition !== "DIRTY") classes.push("done");
  if (!lit) classes.push("dim");
  const button = el("button", classes.join(" "), room.number);
  button.setAttribute("type", "button");
  button.title = `${service(room.service)} · ${room.attendant ?? "unassigned"} · ${room.outcome.kind.toLowerCase().replaceAll("_", " ")}`;
  const mark = room.marks.disagreement ? "!" : room.marks.supervision ? "⚑" : room.marks.dnd ? "⏸" : room.marks.soldTonight ? "★" : null;
  if (mark !== null) button.append(el("i", undefined, mark));
  button.addEventListener("click", () => nav.openRoom(room.id));
  return button;
}

function legend(): HTMLElement {
  const line = el("div", "legend");
  for (const [cls, label] of [["dirty", "dirty"], ["clean", "clean"], ["insp", "inspected"], ["ring", "in progress"], ["pend", "pending policy"], ["blk", "blocked"]] as const) {
    const item = el("span");
    item.append(el("i", `sw ${cls}`), document.createTextNode(label));
    line.append(item);
  }
  line.append(el("span", undefined, "★ sold tonight · ⏸ DND · ! disagreement · ⚑ supervision"));
  return line;
}

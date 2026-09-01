/**
 * The week grid — one row per person, seven cells each.
 *
 * # The chip is data
 *
 * Its colour and its short code are the **catalogue's**, resolved to a
 * published token by the tone the property chose. Nothing here maps a shift
 * name to a colour: a property invents the shifts it runs (`WF-Q11`), so a
 * module that knew "Morning is blue" could not draw a catalogue it had not been
 * told about.
 */

import { el } from "../../chrome/element";
import type { Cell, Person } from "../../roster";

/**
 * Build the grid.
 *
 * @param days the seven headings
 * @param people the rows, in the rota's order
 * @param open called with a person and a day index when a cell is chosen
 * @returns the grid element
 */
export function grid(
  days: readonly string[],
  people: readonly Person[],
  open: (person: Person, day: number) => void,
): HTMLElement {
  const table = el("div", "rgrid");

  table.append(el("div", "rhd", ""));
  for (const day of days) {
    table.append(el("div", "rhd", day));
  }

  for (const person of people) {
    table.append(who(person));

    person.week.forEach((cell, day) => {
      const node = draw(cell);
      node.addEventListener("click", () => open(person, day));
      table.append(node);
    });
  }

  return table;
}

/** The person column — the zone included, because it completes the posting. */
function who(person: Person): HTMLElement {
  const row = el("div", "who");
  const text = el("div");
  const name = el("div", "wn");

  name.append(el("span", undefined, person.name));

  // `WF-Q7`: the zone lives on the posting that already carries the department,
  // so "Zone 3" is never shown as a fact on its own.
  if (person.head) {
    name.append(el("em", undefined, "head"));
  }

  text.append(
    name,
    el("div", "wr", person.zone === null ? person.role : `${person.role} · ${person.zone}`),
  );

  row.append(el("div", "av", person.initials), text);
  return row;
}

/** One cell, in whichever of its four states it is in. */
function draw(cell: Cell): HTMLElement {
  if (cell.leave !== null) {
    return el("div", "away", cell.leave);
  }

  if (cell.gap) {
    // Named, not blank. The header counts it, and a manager should be able to
    // find the one the count refers to without reading every cell.
    return el("div", "gap", "gap — cover?");
  }

  if (cell.shift === null) {
    return el("div", "empty", "＋");
  }

  const chip = el("div", `chip ${cell.shift.tone}`);
  chip.append(el("b", undefined, cell.shift.code));

  // The override replaces the hours *for this day* and is drawn on the chip it
  // belongs to — WF-Q17. Rendering it as its own cell would lose the colour and
  // the code, which is what a rota is read by.
  if (cell.override !== null) {
    chip.append(el("u", undefined, cell.override));
  } else if (cell.shift.hours !== null) {
    chip.append(el("i", undefined, cell.shift.hours));
  }

  return chip;
}

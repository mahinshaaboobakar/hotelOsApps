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

import { formatDay, type PropertyEnvironment } from "@hotelos/sdk";

import { span } from "../../chrome/clock";
import { el } from "../../chrome/element";
import type { Cell, Person } from "../../roster";

/**
 * Build the grid.
 *
 * @param days the seven days, each as an ISO date
 * @param people the rows, in the rota's order
 * @param open called with a person and a day index when a cell is chosen
 * @returns the grid element
 */
export function grid(
  days: readonly string[],
  people: readonly Person[],
  property: PropertyEnvironment,
  open: (person: Person, day: number) => void,
): HTMLElement {
  const table = el("div", "rgrid");

  table.append(el("div", "rhd", ""));
  // Formatted here. `days` are ISO now, and a heading that rendered one raw
  // would read "2026-08-24" — which the compiler cannot catch, both being
  // strings, and which no test asserting "a heading exists" would either.
  for (const day of days) {
    table.append(el("div", "rhd", formatDay(day, property, "weekday-day")));
  }

  for (const person of people) {
    table.append(who(person));

    person.week.forEach((cell, day) => {
      const node = draw(cell, property);

      // A button, so a keyboard reaches it: these were `div`s with a click
      // listener, which a mouse could open and nothing else could (ledger D4,
      // tests/mouse-only). Its name says whose day it is, because the cell's
      // own text is a code or a "＋" that means nothing read aloud.
      node.setAttribute("type", "button");
      const heading = days[day] === undefined ? "" : formatDay(days[day], property, "weekday-day");
      node.setAttribute("aria-label",
        [person.name, heading, spoken(cell, property)].filter((one) => one !== "").join(" · "));
      node.addEventListener("click", () => open(person, day));
      table.append(node);
    });
  }

  return table;
}

/** The person column — the zone included, because it completes the posting. */
function who(person: Person): HTMLElement {
  const row = el("div", "person");
  const text = el("div");
  const name = el("div", "wn");

  name.append(el("span", undefined, person.name));

  // `WF-Q7`: the zone lives on the posting that already carries the department,
  // so "Zone 3" is never shown as a fact on its own.
  if (person.head) {
    name.append(el("em", undefined, "★ head"));
  }

  text.append(
    name,
    // The role alone (owner, 2026-09-20, `64g` §5): the zone the frames drew
    // here was never sent, and the posting's zone is People's.
    el("div", "wr", person.role),
  );

  row.append(el("div", "av", person.initials), text);
  return row;
}

/**
 * What a cell says when read aloud. Composed from the cell rather than taken
 * from its `textContent`, which welds the code to the hours ("M07:00–15:00").
 */
function spoken(cell: Cell, property: PropertyEnvironment): string {
  if (cell.leave !== null) return cell.leave;
  if (cell.gap) return "gap, needs cover";
  if (cell.shift === null) return "no shift";

  const when = span(cell.override ?? cell.shift.hours, property);
  return when === null ? cell.shift.name : `${cell.shift.name}, ${when}`;
}

/**
 * One cell, in whichever of its four states it is in — a `<button>` in every
 * one, because every one opens the picker. The classes carry the drawing; the
 * chrome's `button` reset carries nothing that shows.
 */
function draw(cell: Cell, property: PropertyEnvironment): HTMLElement {
  if (cell.leave !== null) {
    return el("button", "away", cell.leave);
  }

  if (cell.gap) {
    // Named, not blank. The header counts it, and a manager should be able to
    // find the one the count refers to without reading every cell.
    return el("button", "gap", "gap — cover?");
  }

  if (cell.shift === null) {
    return el("button", "empty", "＋");
  }

  const chip = el("button", `chip ${cell.shift.tone}`);
  chip.append(el("b", undefined, cell.shift.code));

  // The override replaces the hours *for this day* and is drawn on the chip it
  // belongs to — WF-Q17. Rendering it as its own cell would lose the colour and
  // the code, which is what a rota is read by.
  if (cell.override !== null) {
    chip.append(el("u", undefined, span(cell.override, property) ?? ""));
  } else if (cell.shift.hours !== null) {
    chip.append(el("i", undefined, span(cell.shift.hours, property) ?? ""));
  }

  return chip;
}

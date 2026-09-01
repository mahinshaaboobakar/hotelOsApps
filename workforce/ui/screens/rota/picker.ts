/**
 * The shift picker — what a rota cell offers when it is clicked.
 *
 * # It lists the property's own catalogue and nothing else
 *
 * `WF-Q11`: a property invents the shifts it runs, so this popover is rendered
 * from the catalogue it was handed. Nothing is preset, and a shift added in
 * Policy appears here the moment it is saved.
 *
 * # "Custom hours" is anchored, not free-floating
 *
 * `WF-Q17` ruled on the drawing: the frame's *Custom hours…* sat as a peer of
 * the six shifts, and a catalogue-less cell has no colour and no short code —
 * which every rota cell must render. So the one-off span **adjusts the shift
 * chosen above it**, and the frame loses the free-standing entry at its next
 * pass. This is the ratified shape, drawn.
 */

import { el } from "../../chrome/element";
import type { Person, Shift } from "../../roster";

/**
 * Build the picker.
 *
 * @param person whose cell it is
 * @param day the day's heading
 * @param catalogue the property's shifts
 * @param close called when it is dismissed
 * @returns the popover
 */
export function picker(
  person: Person,
  day: string,
  catalogue: readonly Shift[],
  close: () => void,
): HTMLElement {
  const scrim = el("div", "scrim");
  const pop = el("div", "pick");

  const head = el("div");
  head.append(
    el("div", "ht", `${person.name} · ${day}`),
    // "one shift per day" is the model's rule, said where somebody might
    // otherwise try to add a second: a split shift is ONE catalogue entry with
    // two spans, not two assignments.
    el("div", "hsub",
      `${person.zone === null ? person.role : `${person.role} · ${person.zone}`}`
      + " · one shift per day"),
  );

  const list = el("div", "picks");
  for (const shift of catalogue) {
    list.append(option(shift));
  }

  const custom = el("div", "custom");
  custom.append(
    el("b", undefined, "Custom hours"),
    el("s", undefined, "this day only — adjusts the shift chosen above"),
  );

  const note = el("div", "note",
    "The property's own catalogue — Policy → Shifts adds to this list.");

  pop.append(head, list, custom, note);
  scrim.append(pop);

  scrim.addEventListener("click", (event) => {
    if (event.target === scrim) close();
  });

  return scrim;
}

/** One catalogue entry, as the picker offers it. */
function option(shift: Shift): HTMLElement {
  const row = el("div", "pk");

  row.append(
    el("b", `code ${shift.tone}`, shift.code),
    el("span", undefined, shift.name),
    el("s", undefined, shift.hours ?? "—"),
  );

  return row;
}

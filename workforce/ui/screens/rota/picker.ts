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
import { codeChip } from "../../chrome/code";
import type { Person, Shift, Week } from "../../roster";

/**
 * Build the picker.
 *
 * @param person whose cell it is
 * @param day which day of the week, zero-based from Monday
 * @param week the week it belongs to — its department, month and catalogue
 * @param close called when it is dismissed
 * @returns the popover
 */
export function picker(
  person: Person,
  day: number,
  week: Week,
  close: () => void,
): HTMLElement {
  const scrim = el("div", "scrim");
  const pop = el("div", "pick");

  // The day, with its month: this names one particular day, and a column
  // heading's "Thu 27" is not a date somebody can act on.
  const heading = `${week.days[day] ?? ""} ${week.month}`.trim();

  // The DEPARTMENT, not the job role. The rota is a department's, and what makes
  // a zone mean anything is the department beside it — WF-Q7's whole argument,
  // in the one place a manager is about to change the posting's day.
  const where = person.zone === null
    ? week.department
    : `${week.department} · ${person.zone}`;

  const head = el("div");
  head.append(
    el("div", "ht", `${person.name} · ${heading}`),
    // "one shift per day" is the model's rule, said where somebody might
    // otherwise try to add a second: a split shift is ONE catalogue entry with
    // two spans, not two assignments.
    el("div", "hsub", `${where} · one shift per day`),
  );

  const current = person.week[day]?.shift?.id ?? null;

  const list = el("div", "picks");
  for (const shift of week.catalogue) {
    list.append(option(shift, shift.id === current));
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

/**
 * One catalogue entry, as the picker offers it.
 *
 * The one already in the cell is marked. A picker that offered six identical
 * choices would make a manager check the grid behind it to see what they were
 * changing from.
 */
function option(shift: Shift, current: boolean): HTMLElement {
  const row = el("div", current ? "pk on" : "pk");

  row.append(
    codeChip(shift.code, shift.tone),
    el("span", undefined, shift.name),
    el("s", undefined, shift.hours ?? "—"),
  );

  return row;
}

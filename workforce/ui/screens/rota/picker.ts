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

import { formatDay, type HostApi, type PropertyEnvironment } from "@hotelos/sdk";

import { span } from "../../chrome/clock";
import { el } from "../../chrome/element";
import { codeChip } from "../../chrome/code";
import { foot } from "../../chrome/confirm";
import { UNKNOWN_OUTCOME, write, WriteRefused } from "../../roster";
import type { Person, Shift, Week } from "../../roster";

/**
 * Build the picker.
 *
 * @param host the bridge
 * @param person whose cell it is
 * @param day which day of the week, zero-based from Monday
 * @param week the week it belongs to — its anchor, department and catalogue
 * @param close called when it is dismissed
 * @param done called after the assignment lands, so the grid re-reads
 * @returns the popover
 *
 * @remarks
 * **The date is arithmetic on {@link Week.monday}, never on a heading.** The
 * column says `THU 27`; the write says `2026-08-27`. Recovering the second from
 * the first would mean parsing a string composed for reading, which is the same
 * mistake in the other direction as formatting one on the server.
 */
export function picker(
  host: HostApi,
  person: Person,
  day: number,
  week: Week,
  close: () => void,
  done: () => void,
): HTMLElement {
  // `mid`: a popover over a cell, not a §9 sheet. The bare `.scrim` is the
  // sheet's and holds the right edge; this was centred and stays so.
  const scrim = el("div", "scrim mid");
  const pop = el("div", "pick");

  // The day, with its month: this names one particular day, and a column
  // heading's "Thu 27" is not a date somebody can act on. It used to append a
  // separate `month` field that existed only because the headings were rendered
  // too short to carry one; the day is a date now and the style says how much
  // of it to show.
  const property = host.property;
  const heading = week.days[day] === undefined
    ? ""
    : formatDay(week.days[day], property, "day-month-year");

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

  // What the cell already holds, so pressing Assign with nothing touched
  // re-states the existing choice rather than refusing. A picker opened on a
  // filled cell is a person changing it, and one opened on an empty cell has
  // nothing to send until they choose.
  let chosen: string | null = current;

  const list = el("div", "picks");
  for (const shift of week.catalogue) {
    list.append(option(shift, shift.id === current, host.property, () => {
      chosen = shift.id;
      for (const other of Array.from(list.querySelectorAll(".pk"))) {
        other.classList.remove("on");
      }
      list.children[week.catalogue.indexOf(shift)]?.classList.add("on");
      acts.waitingFor(null);
    }));
  }

  const custom = el("div", "custom");
  custom.append(
    el("b", undefined, "Custom hours"),
    el("s", undefined, "this day only — adjusts the shift chosen above"),
  );

  const note = el("div", "note",
    "The property's own catalogue — Policy → Shifts adds to this list.");

  const refusal = el("div", "note warn");
  const acts = foot("Assign", "Assigning…", close);
  acts.waitingFor(chosen === null ? "Choose a shift" : null);
  acts.onConfirm(() => { void submit(); });

  async function submit(): Promise<void> {
    if (chosen === null) return;

    refusal.replaceChildren();
    acts.working(true);

    try {
      await write(host, "roster.plan", "assign", {
        staffId: person.id,
        date: dayOf(week.monday, day),
        shiftId: chosen,

        // The CODE, which is why the read now carries it. `week.department` is
        // "Front Office" and the command wants `FO`.
        department: week.departmentCode,
      });
      done();
    } catch (error) {
      // Section 9: a refusal keeps the popover open, carrying the reason.
      refusal.append(el("span", undefined,
        error instanceof WriteRefused
          ? error.message
          : UNKNOWN_OUTCOME));
      acts.working(false);

      if (!(error instanceof WriteRefused)) throw error;
    }
  }

  pop.append(head, list, custom, note, refusal, acts.row);
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
function option(
  shift: Shift,
  current: boolean,
  property: PropertyEnvironment,
  pick: () => void,
): HTMLElement {
  const row = el("div", current ? "pk on" : "pk");
  row.addEventListener("click", pick);

  row.append(
    codeChip(shift.code, shift.tone),
    el("span", undefined, shift.name),
    el("s", undefined, span(shift.hours, property) ?? "—"),
  );

  return row;
}

/**
 * The date a column stands for.
 *
 * @param monday the week's anchor, `YYYY-MM-DD`
 * @param day zero-based from Monday
 * @returns the day's own date, in the same form
 *
 * @remarks
 * UTC throughout. A local `Date` built from `YYYY-MM-DD` is midnight UTC and
 * renders as the previous day west of Greenwich, so adding a day in local time
 * is how a rota quietly writes to Wednesday on a Thursday cell.
 */
function dayOf(monday: string, day: number): string {
  const [year, month, date] = monday.split("-").map(Number);
  const at = new Date(Date.UTC(year ?? 0, (month ?? 1) - 1, date ?? 1));

  at.setUTCDate(at.getUTCDate() + day);
  return at.toISOString().slice(0, 10);
}

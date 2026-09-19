/**
 * The Team Rota — a department's week.
 *
 * Composes the header, the duty ribbon and the grid, and owns none of their
 * drawing. What it does own is the screen's own rules: the counts, the overtime
 * warning, and saying when it is not looking at the property's own data.
 */

import { control, el, unavailable } from "../../chrome/element";
import { legend } from "../../chrome/legend";
import { failureScreen } from "../../chrome/failure";
import { ROSTER_READ } from "../../chrome/permissions";
import { type Week } from "../../roster";
import type { OvertimeWarning } from "../../roster/model";
import { formatDay, formatNumber, type HostApi, load, type PropertyEnvironment }
  from "@hotelos/sdk";
import { grid } from "./grid";
import { picker } from "./picker";
import { ribbon } from "./ribbon";

/**
 * Draw the rota into `main`.
 *
 * @param host the bridge
 * @param main the screen's container
 * @param fixture the week to fall back to — the harness varies it
 */
export async function rota(
  host: HostApi,
  main: HTMLElement,
  print: () => void = () => {},
  pick: { person: string; day: number } | null = null,
  onPick: (person: string, day: number) => void = () => {},
  closePick: () => void = () => {},
): Promise<void> {
  const got = await load<Week>(host, ROSTER_READ, "week");

  // No fallback - `APPS-Q26(4)`. The `fixture` parameter went with it: a
  // recorded week as a DEFAULT ARGUMENT put the fabrication in the shipped
  // signature, where a caller passing nothing got one without deciding to.
  if (!got.ok) {
    failureScreen(main, "Rota", got.failure, { the: "the team rota" }, host.property,
      () => void rota(host, main, print, pick, onPick, closePick));
    return;
  }

  const week = got.value;

  const body = el("div", "body");
  const view = el("div", "rota");

  view.append(
    ribbon(week.duty, host.property),
    grid(week.days, week.people, host.property, (person, day) => onPick(person.id, day)),
  );
  body.append(view, legend(week.catalogue, host.property, "edit a shift → effective forward only"));

  if (week.overtime.length > 0) {
    body.append(overtime(week, host));
  }

  main.replaceChildren(header(week, host, print), body);

  // Over the cell it belongs to, because the week behind it is what makes the
  // choice legible — which shift the person has either side of this day.
  if (pick !== null) {
    const person = week.people.find((candidate) => candidate.id === pick.person);

    if (person !== undefined) {
      // Closed and re-read on success: the grid behind the popover is the
      // thing that changed, and a picker that dismissed without re-reading
      // would leave the old cell on screen under a write that landed.
      main.append(picker(host, person, pick.day, week, closePick, () => {
        closePick();
        void rota(host, main, print, null, onPick, closePick);
      }));
    }
  }
}

/**
 * The header, and its counts.
 *
 * **Every number is derived from the week itself** — the FF precedent: a header
 * that carried its own totals would eventually disagree with the grid beneath
 * it, and the header is the one a manager reads first.
 */
function header(week: Week, host: HostApi, print: () => void): HTMLElement {
  const head = el("div", "tools");
  const title = el("div");

  // **A week-off is not a shift** — `WF-Q12`: it is a rota marker, with no
  // request, no balance and no hours. The backend's month-end applies the same
  // rule to "days posted"; counting it here and not there would have made one
  // screen disagree with the other about the same week.
  const shifts = week.people.reduce(
    (total, person) =>
      total + person.week.filter((cell) => cell.shift?.hours != null).length, 0);
  const away = week.people.filter(
    (person) => person.week.some((cell) => cell.leave !== null)).length;
  const gaps = week.people.reduce(
    (total, person) => total + person.week.filter((cell) => cell.gap).length, 0);

  const n = (value: number): string => formatNumber(value, host.property, "whole");
  title.append(
    el("div", "hsub",
      `${n(week.people.length)} people · ${n(shifts)} shifts · ${n(away)} on leave · `
      + `${n(gaps)} slot uncovered`),
  );

  const picker = el("div", "sel");
  picker.append(el("span", undefined, week.department), el("i", undefined, "▾"));

  const grow = el("div", "grow");

  // **Three inert controls, and each says which piece is missing.** A control
  // that looks live and does nothing is worse than one that is absent: a person
  // presses it. `＋ Assign shift` below is live because the picker it opens
  // captures everything `assign` requires; these three do not.
  //
  //   ‹ Week ›   one element for two directions. The `week` read accepts an
  //              anchor, so the call is available — the control is not, and
  //              splitting it into two arrows is the frame's decision
  //   Copy       `copyWeek` writes across a whole week and there is no confirm
  //              surface in front of it. §9 is not optional for that
  //   Swap       needs two assignments named by id, and `Cell` carries none.
  //              Both the read and a two-cell selection are missing
  // Composed here, and the word "Week" appears ONCE. The service sent
  // "24/08/2026 – 30 Aug Week" — a locale's full short-date pattern, a second
  // format for the other end, and a word this line was already writing — so a
  // real property read "… Week  Week" (ADR 0175).
  const week_ = unavailable("btn",
    `‹ ${formatDay(week.monday, host.property, "day-month")}`
    + ` – ${formatDay(week.sunday, host.property, "day-month")} Week ›`,
    "Other weeks cannot be opened here yet.");
  const copy = unavailable("btn", "⧉ Copy last week", "A week cannot be copied here yet.");
  const swap = unavailable("btn", "⇄ Swap", "Swaps cannot be proposed from here yet.");
  const printBtn = control("btn", "⎙ Print", print);
  // Shifts are assigned by picking a cell, which works; this header button
  // was never wired to anything (the app surface audit, 2026-09-19, C8 · C11).
  const assign = unavailable("btn pri", "＋ Assign shift", "Pick a cell in the week to assign a shift.");

  head.append(title, picker, grow, week_, copy, swap, printBtn, assign);
  return head;
}

/**
 * The overtime warning — `WF-Q14`, warn and never block.
 *
 * It carries **the number**, because *"Vishnu is over"* tells a manager nothing
 * they can act on and *"60.0 against 48"* tells them how much to move. Nothing
 * on this screen is disabled by it.
 */
function overtime(week: Week, host: HostApi): HTMLElement {
  const panel = el("div", "panel");
  const note = el("div", "note");

  note.append(el("b", undefined, "Overtime — planned, not worked. "));

  for (const warning of week.overtime) {
    note.append(el("span", undefined, `${overSentence(warning, host.property)} `));
  }

  note.append(el("span", undefined,
    "The rota still takes the assignment — a manager covering a sick shift decides."));

  panel.append(note);
  return panel;
}

/**
 * One person's warning, as a sentence — the weekly one where it applies.
 *
 * The service sends numbers and this writes the words (NUM-Q1, ADR 0174). The
 * weekly total leads because it is the larger fact: a week over its threshold
 * is over whatever its days did. A week over neither never arrives — the check
 * warns only when one is crossed — and a threshold nobody set arrives as null
 * and is never named.
 */
function overSentence(warning: OvertimeWarning, property: PropertyEnvironment): string {
  const n = (value: number): string => formatNumber(value, property, "at-most-1");

  if (warning.weekly && warning.weeklyHours !== null) {
    return `${warning.who} is planned ${n(warning.planned)} hours against a weekly `
      + `threshold of ${n(warning.weeklyHours)}.`;
  }

  if (warning.dailyHours !== null) {
    const days = warning.daysOver === 1 ? "day" : "days";
    return `${warning.who} is over the daily threshold of ${n(warning.dailyHours)} hours `
      + `on ${formatNumber(warning.daysOver, property, "whole")} ${days}.`;
  }

  // Neither threshold named: say what was planned and nothing about a limit
  // the wire did not give — a sentence ending "against" would be an invention.
  return `${warning.who} is planned ${n(warning.planned)} hours.`;
}

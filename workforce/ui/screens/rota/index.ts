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
import { copyWeek, shifted } from "./copy";
import { grid } from "./grid";
import { picker } from "./picker";
import { ribbon } from "./ribbon";

/**
 * Which week, and whether Copy is being confirmed — the application's state.
 *
 * Held by the application rather than here, so the week a person stepped to
 * survives a redraw, and the read is asked for it.
 */
export interface RotaNav {
  /** Any day of the week to show, or null for the one the service calls current. */
  week: string | null;

  /** Step to another week — the Monday of the one to open. */
  onWeek: (monday: string) => void;

  /** Whether the Copy last week dialog is open. */
  copying: boolean;

  /** Open it. */
  onCopy: () => void;
}

const HERE: RotaNav = { week: null, onWeek: () => {}, copying: false, onCopy: () => {} };

/**
 * Draw the rota into `main`.
 *
 * @param host the bridge
 * @param main the screen's container
 * @param nav which week, and the copy dialog
 */
export async function rota(
  host: HostApi,
  main: HTMLElement,
  print: () => void = () => {},
  pick: { person: string; day: number } | null = null,
  onPick: (person: string, day: number) => void = () => {},
  closePick: () => void = () => {},
  nav: RotaNav = HERE,
): Promise<void> {
  const got = await load<Week>(
    host, ROSTER_READ, "week", nav.week === null ? undefined : { week: nav.week });

  // No fallback - `APPS-Q26(4)`. The `fixture` parameter went with it: a
  // recorded week as a DEFAULT ARGUMENT put the fabrication in the shipped
  // signature, where a caller passing nothing got one without deciding to.
  if (!got.ok) {
    failureScreen(main, "Rota", got.failure, { the: "the team rota" }, host.property,
      () => void rota(host, main, print, pick, onPick, closePick, nav));
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

  main.replaceChildren(header(week, host, print, nav), body);

  // Closed and re-read, as the picker is: the grid is what a copy changes.
  if (nav.copying) {
    main.append(copyWeek(host, week, closePick, () => {
      closePick();
    }));
  }

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
        void rota(host, main, print, null, onPick, closePick, nav);
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
function header(week: Week, host: HostApi, print: () => void, nav: RotaNav): HTMLElement {
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

  // **The week and Copy work; Swap is off, and says what is missing.** All
  // three were dead on the owner's 0.3.3 (2026-09-19).
  //
  //   ‹ Week ›   one control, as the frame draws it, with the two arrows real
  //              buttons inside it: the `week` read takes the week to answer
  //   Copy       `copyWeek` fills empty cells only, and still writes a whole
  //              week — so it opens a §9 dialog saying so, never acts on a press
  //   Swap       needs two assignments named by id, and the week read's cells
  //              carry none (`RotaView.Cell`) — and picking two cells is an
  //              interaction nobody has drawn. The read and a design are
  //              missing, so it stays off with that reason
  //
  // The range is composed here, and the word "Week" appears ONCE. The service
  // sent "24/08/2026 – 30 Aug Week" — a locale's full short-date pattern, a
  // second format for the other end, and a word this line was already writing
  // — so a real property read "… Week  Week" (ADR 0175).
  const week_ = el("div", "wknav");
  week_.append(
    step("‹", "Previous week", () => { nav.onWeek(shifted(week.monday, -7)); }),
    el("span", undefined,
      `${formatDay(week.monday, host.property, "day-month")}`
      + ` – ${formatDay(week.sunday, host.property, "day-month")} Week`),
    step("›", "Next week", () => { nav.onWeek(shifted(week.monday, 7)); }),
  );
  const copy = control("btn", "⧉ Copy last week", nav.onCopy);
  // What is missing is the developer's (see above); what a person reads is
  // plain — owner ruling, 2026-09-19.
  const swap = unavailable("btn", "⇄ Swap", "Swapping two shifts is not available here yet.");
  const printBtn = control("btn", "⎙ Print", print);
  // Shifts are assigned by picking a cell, which works; this header button
  // was never wired to anything (the app surface audit, 2026-09-19, C8 · C11).
  const assign = unavailable("btn pri", "＋ Assign shift", "Pick a cell in the week to assign a shift.");

  head.append(title, picker, grow, week_, copy, swap, printBtn, assign);
  return head;
}

/** One arrow of the week control — a real button, named for a screen reader. */
function step(glyph: string, label: string, go: () => void): HTMLElement {
  const button = el("button", "wkstep", glyph);
  button.setAttribute("type", "button");
  button.setAttribute("aria-label", label);
  button.addEventListener("click", go);
  return button;
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

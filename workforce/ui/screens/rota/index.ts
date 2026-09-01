/**
 * The Team Rota — a department's week.
 *
 * Composes the header, the duty ribbon and the grid, and owns none of their
 * drawing. What it does own is the screen's own rules: the counts, the overtime
 * warning, and saying when it is not looking at the property's own data.
 */

import { el } from "../../chrome/element";
import { standIn } from "../../chrome/standin";
import { ROSTER_READ } from "../../chrome/permissions";
import { load, recordedWeek, type Week } from "../../roster";
import type { HostApi } from "@hotelos/sdk";
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
  fixture: Week = recordedWeek,
  pick: { person: string; day: number } | null = null,
  onPick: (person: string, day: number) => void = () => {},
  closePick: () => void = () => {},
): Promise<void> {
  const got = await load(host, ROSTER_READ, "week", fixture);
  const week = got.value;

  const body = el("div", "body");
  const view = el("div", "rota");

  view.append(
    ribbon(week.duty),
    grid(week.days, week.people, (person, day) => onPick(person.id, day)),
  );
  body.append(view);

  if (week.overtime.length > 0) {
    body.append(overtime(week));
  }

  if (!got.live) {
    body.append(standIn("week", got.because));
  }

  main.replaceChildren(header(week, print), body);

  // Over the cell it belongs to, because the week behind it is what makes the
  // choice legible — which shift the person has either side of this day.
  if (pick !== null) {
    const person = week.people.find((candidate) => candidate.id === pick.person);
    const day = week.days[pick.day];

    if (person !== undefined && day !== undefined) {
      main.append(picker(person, day, week.catalogue, closePick));
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
function header(week: Week, print: () => void): HTMLElement {
  const head = el("div", "head");
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

  title.append(
    el("div", "ht", "Team Rota"),
    el("div", "hsub",
      `${week.people.length} people · ${shifts} shifts · ${away} on leave · ${gaps} slot uncovered`),
  );

  const picker = el("div", "sel");
  picker.append(el("span", undefined, week.department), el("i", undefined, "▾"));

  const grow = el("div", "grow");
  const week_ = el("div", "btn", `‹ ${week.label}  Week ›`);
  const copy = el("div", "btn", "⧉ Copy last week");
  const swap = el("div", "btn", "⇄ Swap");
  const printBtn = el("div", "btn", "⎙ Print");
  printBtn.addEventListener("click", print);
  const assign = el("div", "btn go", "＋ Assign shift");

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
function overtime(week: Week): HTMLElement {
  const panel = el("div", "panel");
  const note = el("div", "note");

  note.append(el("b", undefined, "Overtime — planned, not worked. "));

  for (const warning of week.overtime) {
    note.append(el("span", undefined,
      `${warning.who} is planned ${warning.planned} hours against ${warning.threshold}. `));
  }

  note.append(el("span", undefined,
    "The rota still takes the assignment — a manager covering a sick shift decides."));

  panel.append(note);
  return panel;
}

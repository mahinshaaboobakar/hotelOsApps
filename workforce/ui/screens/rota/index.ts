/**
 * The Team Rota — a department's week.
 *
 * Composes the header, the duty ribbon and the grid, and owns none of their
 * drawing. What it does own is the screen's own rules: the counts, the overtime
 * warning, and saying when it is not looking at the property's own data.
 */

import { el } from "../../chrome/element";
import { legend } from "../../chrome/legend";
import { failureScreen } from "../../chrome/failure";
import { ROSTER_READ } from "../../chrome/permissions";
import { type Week } from "../../roster";
import { type HostApi, load } from "@hotelos/sdk";
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
    failureScreen(main, "Rota", got.failure, { the: "the team rota" },
      () => void rota(host, main, print, pick, onPick, closePick));
    return;
  }

  const week = got.value;

  const body = el("div", "body");
  const view = el("div", "rota");

  view.append(
    ribbon(week.duty, host.property),
    grid(week.days, week.people, (person, day) => onPick(person.id, day)),
  );
  body.append(view, legend(week.catalogue, "edit a shift → effective forward only"));

  if (week.overtime.length > 0) {
    body.append(overtime(week));
  }

  main.replaceChildren(header(week, print), body);

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
function header(week: Week, print: () => void): HTMLElement {
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

  title.append(
    el("div", "hsub",
      `${week.people.length} people · ${shifts} shifts · ${away} on leave · ${gaps} slot uncovered`),
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
  const week_ = el("div", "btn", `‹ ${week.label}  Week ›`);
  const copy = el("div", "btn", "⧉ Copy last week");
  const swap = el("div", "btn", "⇄ Swap");
  const printBtn = el("div", "btn", "⎙ Print");
  printBtn.addEventListener("click", print);
  const assign = el("div", "btn pri", "＋ Assign shift");

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

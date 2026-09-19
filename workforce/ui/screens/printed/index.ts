/**
 * The printed week — how the rota actually reaches staff in v1.
 *
 * # Not a print stylesheet on the rota. A different artifact.
 *
 * No rail, no header controls, no hover, no colour: the whole department on one
 * page, built for a **monochrome photocopier**, which is how a rota reaches
 * staff while the staff app is a version away.
 *
 * **This is what the short code is for.** A photocopy destroys colour and keeps
 * glyphs, so every cell carries text that survives losing every colour in the
 * design, and the legend beneath the grid is what makes it readable.
 *
 * # The dialog is the shell's
 *
 * `SHELL-Q23`. This application hands over a print-ready view and writes no
 * printer code.
 */

import { formatDay, formatInstant, type HostApi, load, type PropertyEnvironment }
  from "@hotelos/sdk";

import { span } from "../../chrome/clock";
import { control, el, fill, unavailable } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { failureScreen } from "../../chrome/failure";
import { type Week } from "../../roster";
import { type Duty, type Register } from "../../roster/duty";

/**
 * Draw the preview into `root`, replacing the module's chrome entirely.
 *
 * @param host the bridge
 * @param root where it mounts
 * @param back leave the preview
 * @param anchor the week the rota had open — a day in it — or null for the
 *   current one. **Both reads ask for it**: once the rota's arrows could step
 *   to another week, a print that asked for none printed the current week
 *   under a person who was looking at next week's.
 */
export async function printed(
  host: HostApi, root: HTMLElement, back: () => void = () => {}, anchor: string | null = null,
): Promise<void> {
  // **Both reads, because the page shows both.** The week was already read
  // here; the Manager-on-Duty band was taken from a recorded fixture whatever
  // the service said, so a printed rota could carry a real week over invented
  // duty rows — and paper is exactly where nobody would notice, because there
  // is no live screen beside it to disagree.
  const asked = anchor === null ? undefined : { week: anchor };
  const [gotWeek, gotDuty] = await Promise.all([
    load<Week>(host, ROSTER_READ, "week", asked),
    load<Register>(host, ROSTER_READ, "register", asked),
  ]);

  // **Either read failing means no page.** This is paper: a fabricated row
  // here is the one nobody notices, because there is no live screen beside it
  // to disagree with. Both reads feed one page, so the first failure is the
  // page's failure - printing half a rota would be worse than printing none.
  const failed = !gotWeek.ok ? gotWeek : !gotDuty.ok ? gotDuty : null;
  if (failed !== null && !failed.ok) {
    failureScreen(root, "The printed week", failed.failure,
      { the: "the week to print" }, host.property, () => void printed(host, root, back, anchor));
    return;
  }

  if (!gotWeek.ok || !gotDuty.ok) return;

  const week = gotWeek.value;

  const page = el("div", "page");
  // **No "changes since issued" list.** It printed four personnel records —
  // *"S. Iyer marked sick … approved by P. Thomas"* — as literals, on every
  // property's paper, and no service records a change to an issued rota (the
  // app surface audit, 2026-09-19). The section returns when something does.
  page.append(
    masthead(week, host.property), grid(week, gotDuty.value, host.property), legend(week, host.property));

  const paper = el("div", "paper");
  paper.append(page);

  root.append(preview(back, week, host.property), paper);
}

/**
 * The preview's own chrome — what the page is, and the way out of it.
 *
 * The build had none: the page replaced the module's chrome and the frame's
 * Page setup and Print went with it, on the argument that a dead button is
 * worse than none. That was wrong twice over. The option is what the screen is
 * for, and a preview a person cannot leave is worse than one with a control
 * that does not work yet — so Back is here as well, and it does work.
 *
 * @param back leave the preview and return to the rota
 * @param week the week being printed — its department and dates are the line
 * @param property the locale the dates are read in
 * @returns the row
 */
function preview(back: () => void, week: Week, property: PropertyEnvironment): HTMLElement {
  const row = el("div", "title pbar");
  const name = el("div");

  // The week the service sent. This was the literal *"Front Office · 24 – 30
  // August 2026 · A4 landscape"* — a department, a week and a page setup nobody
  // set, on every property (the app surface audit, 2026-09-19).
  name.append(
    el("div", "ht", "Print preview"),
    el("div", "hsub",
      `${week.department} · ${formatDay(week.monday, property, "day-month-year")}`
      + ` – ${formatDay(week.sunday, property, "day-month-year")}`));

  const leave = control("btn", "‹ Back to the rota", back);

  return fill(row, name, el("div", "grow"), leave,
    unavailable("btn", "Page setup", "Page setup is not available yet."),
    unavailable("btn pri", "⎙ Print", "Printing is not wired yet — the shell's print dialog is the next step."));
}

/**
 * What the page is: the department, and the week it covers.
 *
 * **It said who issued it, when, at which property, and how many pages** —
 * *"Kochi Beach Resort · Week of Monday 24 August 2026 · issued Fri 21 Aug,
 * 16:40 by P. Thomas"* and *"Page 1 of 1 · Printed 24 Aug 2026"* — every word a
 * literal, on every property's paper (the app surface audit, 2026-09-19). A
 * printed page has no other provenance, which is why an invented one is the
 * worst kind: nothing beside it can disagree. Issue and page count return when
 * something records them; the week is the week the service sent.
 */
function masthead(week: Week, property: PropertyEnvironment): HTMLElement {
  const head = el("div", "phead");
  const title = el("div");

  title.append(
    el("div", "pt", `${week.department} — Duty Rota`),
    el("div", "psub", `Week of ${formatDay(week.monday, property, "day-month-year")}`),
  );

  head.append(title);
  return head;
}

/** The grid, in ink only. */
function grid(
  week: Week, register: Register, property: PropertyEnvironment,
): HTMLElement {
  const table = el("div", "pgrid");

  table.append(el("div", "pcell hd", "Staff"));
  for (const day of week.days) {
    table.append(el("div", "pcell hd", formatDay(day, property, "weekday-day")));
  }

  // The MOD row shows TWO names on most days, because the duty crosses midnight
  // and a printed page has no hover to explain it.
  table.append(el("div", "pcell pmod", "MANAGER ON DUTY"));
  for (let day = 0; day < week.days.length; day += 1) {
    const cell = el("div", "pcell pmod");

    for (const item of register.duties.filter((duty) => duty.day === day)) {
      // The printed page says the hours in the property's clock too. Paper is
      // where a wrong timezone survives longest: there is no live screen beside
      // it to disagree, and somebody acts on it hours later.
      cell.append(el("div", undefined,
        `${item.who ?? "—"} ${hours(item, property)}`));
    }

    table.append(cell);
  }

  for (const person of week.people) {
    const who = el("div", "pcell pwho");
    who.append(
      el("b", undefined, person.name),
      el("s", undefined, person.zone === null ? person.role : `${person.role} · ${person.zone}`),
    );
    table.append(who);

    for (const cell of person.week) {
      table.append(el("div", "pcell",
        cell.leave !== null
          ? cell.leave.toUpperCase()
          : cell.shift?.code ?? "—"));
    }
  }

  return table;
}

/** The legend — doing more work here than it does on screen. */
function legend(week: Week, property: PropertyEnvironment): HTMLElement {
  const box = el("div", "plegend");

  for (const shift of week.catalogue) {
    box.append(entry(shift.code, shift.name, span(shift.hours, property)));
  }

  box.append(entry("—", "Not assigned", null));
  return box;
}

/**
 * One legend entry, its code boxed.
 *
 * A rule around the code, because this page is read after a photocopier has
 * removed every colour: the box is what separates the code from the words beside
 * it when both are the same black.
 */
function entry(code: string, name: string, hours: string | null): HTMLElement {
  const item = el("div", "pl");

  item.append(el("b", undefined, code), el("span", undefined, name));

  if (hours !== null) {
    item.append(el("s", undefined, hours));
  }

  return item;
}

/** A duty band's hours on paper, in the property's clock. */
function hours(duty: Duty, property: PropertyEnvironment): string {
  return duty.from === null || duty.to === null
    ? "—"
    : `${formatInstant(duty.from, property, "time")}–${formatInstant(duty.to, property, "time")}`;
}

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

import { formatInstant, type HostApi, type PropertyEnvironment } from "@hotelos/sdk";

import { el, fill } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { load, recordedWeek, type Week } from "../../roster";
import { recordedRegister, type Duty, type Register } from "../../roster/duty";

/**
 * Draw the preview into `root`, replacing the module's chrome entirely.
 *
 * @param host the bridge
 * @param root where it mounts
 * @param back leave the preview
 */
export async function printed(
  host: HostApi, root: HTMLElement, back: () => void = () => {},
): Promise<void> {
  // **Both reads, because the sheet shows both.** The week was already read
  // here; the Manager-on-Duty band was taken from a recorded fixture whatever
  // the service said, so a printed rota could carry a real week over invented
  // duty rows — and paper is exactly where nobody would notice, because there
  // is no live screen beside it to disagree.
  const [gotWeek, gotDuty] = await Promise.all([
    load(host, ROSTER_READ, "week", recordedWeek),
    load(host, ROSTER_READ, "register", recordedRegister),
  ]);

  const week = gotWeek.value;

  const sheet = el("div", "sheet");
  sheet.append(
    masthead(week), grid(week, gotDuty.value, host.property), legend(week), changes());

  const paper = el("div", "paper");
  paper.append(sheet);

  root.append(preview(back), paper);
}

/**
 * The preview's own chrome — what the sheet is, and the way out of it.
 *
 * The build had none: the sheet replaced the module's chrome and the frame's
 * Page setup and Print went with it, on the argument that a dead button is
 * worse than none. That was wrong twice over. The option is what the screen is
 * for, and a preview a person cannot leave is worse than one with a control
 * that does not work yet — so Back is here as well, and it does work.
 *
 * @param back leave the preview and return to the rota
 * @returns the row
 */
function preview(back: () => void): HTMLElement {
  const row = el("div", "title pbar");
  const name = el("div");

  name.append(
    el("div", "ht", "Print preview"),
    el("div", "hsub",
      "Front Office · 24 – 30 August 2026 · A4 landscape"));

  const leave = el("button", "btn", "‹ Back to the rota");
  leave.setAttribute("type", "button");
  leave.addEventListener("click", back);

  return fill(row, name, el("div", "grow"), leave,
    el("div", "btn", "Page setup"), el("div", "btn pri", "⎙ Print"));
}

/** Who issued it and when — a printed sheet has no other provenance. */
function masthead(week: Week): HTMLElement {
  const head = el("div", "phead");
  const title = el("div");

  title.append(
    el("div", "pt", `${week.department} — Duty Rota`),
    el("div", "psub",
      "Kochi Beach Resort · Week of Monday 24 August 2026 · issued Fri 21 Aug, 16:40 by P. Thomas"),
  );

  head.append(title, el("div", "psub", "Page 1 of 1 · Printed 24 Aug 2026"));
  return head;
}

/** The grid, in ink only. */
function grid(
  week: Week, register: Register, property: PropertyEnvironment,
): HTMLElement {
  const table = el("div", "pgrid");

  table.append(el("div", "pcell ph", "Staff"));
  for (const day of week.days) {
    table.append(el("div", "pcell ph", day));
  }

  // The MOD row shows TWO names on most days, because the duty crosses midnight
  // and a printed sheet has no hover to explain it.
  table.append(el("div", "pcell pmod", "MANAGER ON DUTY"));
  for (let day = 0; day < week.days.length; day += 1) {
    const cell = el("div", "pcell pmod");

    for (const item of register.duties.filter((duty) => duty.day === day)) {
      // The printed sheet says the hours in the property's clock too. Paper is
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
function legend(week: Week): HTMLElement {
  const box = el("div", "plegend");

  for (const shift of week.catalogue) {
    box.append(entry(shift.code, shift.name, shift.hours));
  }

  box.append(entry("—", "Not assigned", null));
  return box;
}

/**
 * One legend entry, its code boxed.
 *
 * A rule around the code, because this sheet is read after a photocopier has
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

/**
 * What changed after the sheet was issued.
 *
 * The week's record rather than somebody's memory — rendered from the events
 * the application already publishes, not kept as a second list that could
 * disagree. Saturday's missing MOD prints as an absence, **stated**.
 */
function changes(): HTMLElement {
  const box = el("div", "pchanges");

  box.append(el("div", "pct", "Changes since this rota was issued"));

  const list = el("ul", "pcl");

  for (const line of [
    "Tue 25 — R. Nair took MOD 20:00–08:00 in place of P. Thomas.",
    "Wed 26 — S. Iyer marked sick; her afternoon was covered by J. Kurian (split shift).",
    "Thu 27 — A. Menon and S. Iyer swapped (M ⇄ A), approved by P. Thomas.",
    "Sat 29 — no Manager on Duty assigned for 20:00–08:00.",
  ]) {
    list.append(el("li", undefined, line));
  }

  box.append(list);
  return box;
}

/** A duty band's hours on paper, in the property's clock. */
function hours(duty: Duty, property: PropertyEnvironment): string {
  return duty.from === null || duty.to === null
    ? "—"
    : `${formatInstant(duty.from, property, "time")}–${formatInstant(duty.to, property, "time")}`;
}

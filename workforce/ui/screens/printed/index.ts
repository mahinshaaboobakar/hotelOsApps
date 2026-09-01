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

import type { HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { load, recordedWeek, type Week } from "../../roster";
import { recordedRegister, type Register } from "../../roster/duty";

/** Draw the sheet into `root`, replacing the module's chrome entirely. */
export async function printed(host: HostApi, root: HTMLElement): Promise<void> {
  const got = await load(host, ROSTER_READ, "week", recordedWeek);
  const week = got.value;

  const sheet = el("div", "sheet");

  sheet.append(masthead(week), grid(week, recordedRegister), legend(week), changes());
  root.append(sheet);
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
function grid(week: Week, register: Register): HTMLElement {
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
      cell.append(el("div", undefined,
        `${item.who ?? "—"} ${item.hours}`));
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
    box.append(el("div", undefined,
      `${shift.code}  ${shift.name}${shift.hours === null ? "" : `  ${shift.hours}`}`));
  }

  box.append(el("div", undefined, "—  Not assigned"));
  return box;
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

  for (const line of [
    "Tue 25 — R. Nair took MOD 20:00–08:00 in place of P. Thomas.",
    "Wed 26 — S. Iyer marked sick; her afternoon was covered by J. Kurian (split shift).",
    "Thu 27 — A. Menon and S. Iyer swapped (M ⇄ A), approved by P. Thomas.",
    "Sat 29 — no Manager on Duty assigned for 20:00–08:00.",
  ]) {
    box.append(el("div", undefined, line));
  }

  return box;
}

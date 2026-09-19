/**
 * Reports — the month-end summary, handed over.
 *
 * # Nothing here can be edited
 *
 * A number you can type over is not a record. Every figure is traceable to a
 * fact the application already holds: posted from the rota, present and late
 * from attendance, leave from the ledger, hours from in/out times, overtime
 * from hours against the property's threshold.
 */

import { formatDay, formatNumber, type HostApi, load } from "@hotelos/sdk";

import { el, unavailable } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { failureScreen } from "../../chrome/failure";
import { type Month, type MonthRow } from "../../roster/reports";

const HEADINGS = [
  "Person", "Posted", "Present", "Late", "Casual", "Sick", "Earned", "Comp",
  "Holidays worked", "Hours", "Overtime",
] as const;

const COLUMNS = "1.5fr repeat(7,58px) 118px 74px 74px";

/** Draw the screen. */
export async function reports(host: HostApi, main: HTMLElement): Promise<void> {
  const got = await load<Month>(host, ROSTER_READ, "month");

  // No fallback - `APPS-Q26(4)`. A failed read renders the failure,
  // never a recorded list with an apology under it.
  if (!got.ok) {
    failureScreen(main, "Reports", got.failure, { the: "this month's numbers" }, host.property,
      () => void reports(host, main));
    return;
  }

  const month = got.value;

  const body = el("div", "body");
  body.append(table(month.rows, host), boundary());

  const absent = missing(month);
  if (absent !== null) body.append(absent);

  main.replaceChildren(header(month, host), body);
}

function header(month: Month, host: HostApi): HTMLElement {
  const head = el("div", "tools");
  const title = el("div");

  title.append(
    // Composed here — the month's name, the day forms, the range separator and
    // the dots between the clauses are all the reader's (ADR 0175).
    el("div", "hsub", [
      formatDay(month.month, host.property, "month-year"),
      month.department,
      `${formatDay(month.from, host.property, "day-month")}`
      + ` – ${formatDay(month.to, host.property, "day-month")}`,
      `${formatNumber(month.rows.length, host.property, "whole")} people`,
    ].filter((one) => one !== null).join(" · ")),
  );

  const picker = el("div", "sel");
  picker.append(el("span", undefined, month.department), el("i", undefined, "▾"));

  const grow = el("div", "grow");
  head.append(title, picker, grow,
    unavailable("btn", "⎙ Print", "Reports cannot be printed here yet."),
    unavailable("btn", "↓ Export CSV", "Reports cannot be exported here yet."));
  return head;
}

function table(rows: readonly MonthRow[], host: HostApi): HTMLElement {
  const list = el("div", "rows");

  const head = el("div", "row hd");
  head.style.gridTemplateColumns = COLUMNS;
  for (const label of HEADINGS) {
    head.append(el("div", undefined, label));
  }
  list.append(head);

  for (const row of rows) {
    const item = el("div", "row");
    item.style.gridTemplateColumns = COLUMNS;

    const who = el("div");
    who.append(el("b", undefined, row.who), el("s", undefined, row.role));

    item.append(who);

    for (const figure of [row.posted, row.present, row.late, row.casual,
      row.sick, row.earned, row.comp]) {
      // The default precision rather than whole: leave taken can be a half day.
      item.append(el("div", figure === 0 ? "quiet" : undefined,
        formatNumber(figure, host.property)));
    }

    // Absent, never zero — WF-Q18. A dash says the number was not computed; a
    // zero would say the staff worked no holidays, and payroll cannot tell the
    // difference between those two from a figure alone.
    item.append(el("div", "quiet",
      row.holidays === null ? "—" : formatNumber(row.holidays, host.property)));

    // `=== 0` on a number, where this compared against the string "0" that the
    // service emitted instead of "0.0" precisely so this line would match. A
    // screen's styling keyed to the exact characters a service chose.
    item.append(
      el("div", undefined, formatNumber(row.hours, host.property, "exactly-1")),
      el("div", row.overtime === 0 ? "quiet" : "otv",
        formatNumber(row.overtime, host.property, "exactly-1")));

    list.append(item);
  }

  return list;
}

/** Where this application stops. */
function boundary(): HTMLElement {
  const panel = el("div", "panel");
  const note = el("div", "note");

  note.append(
    el("b", undefined, "These are inputs, not a payslip. "),
    el("span", undefined,
      "Workforce produces the numbers and stops there. Pay differs by country "
      + "(WPS, PF, ESI) and by hotel, and getting it wrong is a salary dispute "
      + "rather than a bug. The accountant or the payroll system takes this file."),
  );

  panel.append(note);
  return panel;
}

/**
 * Why one column is dashes.
 *
 * The figure is **named as absent with its reason** rather than left for
 * somebody to puzzle over — a column of dashes with no explanation is the
 * silence this application refused in the backend.
 */
function missing(month: Month): HTMLElement | null {
  if (month.rows.every((row) => row.holidays !== null)) return null;

  const panel = el("div", "panel");
  const note = el("div", "note");

  note.append(
    el("b", undefined, "Holidays worked is not computed. "),
    el("span", undefined,
      "It needs the property's holiday calendar, which Core Administration owns "
      + "and which does not exist yet (WF-Q18). It is shown absent rather than "
      + "as zero, because a zero cannot be told apart from a month in which "
      + "nobody worked a holiday."),
  );

  panel.append(note);
  return panel;
}

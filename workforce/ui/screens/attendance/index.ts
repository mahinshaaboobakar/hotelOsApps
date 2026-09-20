/**
 * Attendance — who was posted, who actually came.
 *
 * # The counts are the union's own shape
 *
 * *"5 of 6 present against posted"*, *"1 present, not rostered"* — both are
 * derived from the rows, and the second only exists because the comparison is a
 * union. A screen that joined on the rota would count five rows and be wrong
 * about the day.
 */

import { formatClock, formatDay, formatNumber, type HostApi, load, type PropertyEnvironment }
  from "@hotelos/sdk";

import { allDepartments } from "../../chrome/department";
import { el, unavailable } from "../../chrome/element";
import { failureScreen } from "../../chrome/failure";
import { ROSTER_READ } from "../../chrome/permissions";
import { type Day, type DayRow } from "../../roster/attendance";

/** Draw the screen. */
export async function attendance(host: HostApi, main: HTMLElement): Promise<void> {
  const got = await load<Day>(host, ROSTER_READ, "day");

  // No fallback — `APPS-Q26(4)`. The header goes with the body, because this
  // screen's header carries the day it is about and a read that failed produced
  // no day to name.
  if (!got.ok) {
    failureScreen(main, "Attendance", got.failure, { the: "today's attendance" }, host.property,
      () => void attendance(host, main));
    return;
  }

  const day = got.value;
  const body = el("div", "body");

  body.append(marks(day, host.property), table(day.rows, host));
  main.replaceChildren(header(day, host.property), body);
}

function header(day: Day, property: PropertyEnvironment): HTMLElement {
  const head = el("div", "tools");
  const title = el("div");

  // Composed here, in the property's own language: the service sent
  // "Friday 28 August · business day" — a weekday and a month name in
  // whatever culture it runs under, with the clause welded on (ADR 0175).
  //
  // `weekday-day-month` is the SDK's, added for this header (`64g` §5, ruled
  // 2026-09-20): the weekday is what a person reads first on a screen whose
  // subject is one day, and composing it here from an array of English names
  // would be the same defect one layer down. The clause after it is this
  // screen's own sentence, which is why it is written here and not sent.
  title.append(el("div", "hsub",
    `${formatDay(day.date, property, "weekday-day-month")} · business day`));

  const picker = allDepartments();

  const grow = el("div", "grow");
  // Neither control is wired, and the stepper's label was a literal — *"Fri
  // 28 Aug"* on every day but one (the app surface audit, 2026-09-19). The day
  // is the sub-line's, so the stepper carries only its arrows.
  head.append(title, picker, grow,
    unavailable("btn", "‹ ›", "Other days cannot be opened here yet."),
    unavailable("btn pri", "＋ Mark attendance", "Attendance cannot be marked here yet."));
  return head;
}

/**
 * What a row's comparison says, in words — the screen's, never the service's.
 *
 * The service sends a state and, when late, the minutes. "Late 20 min" used to
 * arrive whole, in the service's culture, and the number inside it was not in
 * the property's number format either.
 */
function verdict(row: DayRow, property: PropertyEnvironment): string {
  switch (row.state) {
    case "absent": return "Absent";
    case "unrostered": return "Present, not rostered";
    case "onShift": return "On shift";
    case "onTime": return "On time";
    case "late": return row.lateBy === null
      ? "Late"
      : `Late ${formatNumber(row.lateBy, property, "whole")} min`;
  }
}

/** The four marks, each counted from the rows themselves. */
function marks(day: Day, property: PropertyEnvironment): HTMLElement {
  const row = el("div", "marks");
  const n = (value: number): string => formatNumber(value, property, "whole");

  const posted = day.rows.filter((r) => r.rostered);
  const present = posted.filter((r) => r.in !== null).length;
  // Counted from the state the service sends, never from its prose: this read
  // `against.startsWith("Late")` and `against === "Absent"`.
  const late = day.rows.filter((r) => r.state === "late").length;
  const absent = day.rows.filter((r) => r.state === "absent").length;
  const unplanned = day.rows.filter((r) => !r.rostered && r.in !== null).length;

  row.append(
    mark(`${n(present)} of ${n(posted.length)}`, "present against posted", "ok"),
    mark(n(late), "late", "warn"),
    mark(n(absent), "absent", "bad"),
    mark(n(unplanned), "present, not rostered", "warn"),
  );

  return row;
}

/**
 * One figure on the strip.
 *
 * A card no longer — the app surface standard's one thin bar. The tone stays on
 * the class rather than on the figure's own element, because the strip may
 * later want to tint the whole item and the caller should not have to know.
 */
function mark(figure: string, label: string, tone: string): HTMLElement {
  const item = el("div", `mk ${tone}`);
  item.append(el("b", undefined, figure), el("div", undefined, label));
  return item;
}

/**
 * What the rota planned.
 *
 * @param row the person's day
 * @param property for the locale the clock is read in
 * @returns the cell
 *
 * @remarks
 * **The shift code is gone, and its absence is the finding.** This split
 * `"M 07:00"` on a space and drew a {@link codeChip} from the first half — but
 * the service sends no code: `DayComparison.DayRow` carries a department code,
 * a rostered flag and a scheduled start, and nothing else. Only the fixture
 * ever had one, so against a real property this drew an empty chip beside a
 * time. Reported rather than invented; the chip returns when the wire carries
 * something to put in it.
 *
 * Three states, because the wire now distinguishes them: nothing rostered, a
 * day rostered with no start, and a start.
 */
function posted(row: DayRow, property: PropertyEnvironment): HTMLElement {
  const cell = el("div", "postedcell");

  if (!row.rostered) {
    cell.append(el("span", "quiet", "not rostered"));
  } else if (row.postedAt === null) {
    // Rostered, start unknown. Not the same as "not rostered", and the old
    // shape said this with the word `rostered` chosen in a service.
    cell.append(el("span", "quiet", "rostered"));
  } else {
    cell.append(el("span", "quiet", formatClock(row.postedAt, property)));
  }

  return cell;
}

/** The day's rows, planned beside actual. */
function table(rows: readonly DayRow[], host: HostApi): HTMLElement {
  const list = el("div", "rows");
  const columns = "1.5fr 96px 78px 78px 1fr";

  const head = el("div", "row hd");
  head.style.gridTemplateColumns = columns;
  for (const label of ["Person", "Posted", "In", "Out", "Against the rota"]) {
    head.append(el("div", undefined, label));
  }
  list.append(head);

  for (const row of rows) {
    const item = el("div", "row");
    item.style.gridTemplateColumns = columns;

    const who = el("div");
    who.append(el("b", undefined, row.who), el("s", undefined, row.role));

    // The sentence is written here, in the property's own number format: the
    // service sends `late` and the minutes, never "Late 20 min".
    const against = el("div", "ag");
    against.append(el("span", `pill ${row.tone}`, verdict(row, host.property)));

    // The source is on the row because it is the difference between evidence
    // and an assertion — and a device record names a reading, never a person.
    if (row.source !== null) {
      against.append(el("s", "src", row.source));
    }

    item.append(
      who,
      posted(row, host.property),
      el("div", undefined, row.in === null ? "—" : formatClock(row.in, host.property)),
      el("div", undefined, row.out === null
        ? (row.in === null ? "—" : "— still in")
        : formatClock(row.out, host.property)),
      against,
    );

    list.append(item);
  }

  return list;
}

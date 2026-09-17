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

import { formatClock, type HostApi, load, type PropertyEnvironment }
  from "@hotelos/sdk";

import { el } from "../../chrome/element";
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
    failureScreen(main, "Attendance", got.failure, { the: "today's attendance" },
      () => void attendance(host, main));
    return;
  }

  const day = got.value;
  const body = el("div", "body");

  body.append(marks(day), table(day.rows, host));
  main.replaceChildren(header(day), body);
}

function header(day: Day): HTMLElement {
  const head = el("div", "tools");
  const title = el("div");

  title.append(el("div", "hsub", day.date));

  const picker = el("div", "sel");
  picker.append(el("span", undefined, day.department), el("i", undefined, "▾"));

  const grow = el("div", "grow");
  head.append(title, picker, grow,
    el("div", "btn", "‹ Fri 28 Aug ›"),
    el("div", "btn pri", "＋ Mark attendance"));
  return head;
}

/** The four marks, each counted from the rows themselves. */
function marks(day: Day): HTMLElement {
  const row = el("div", "marks");

  const posted = day.rows.filter((r) => r.rostered);
  const present = posted.filter((r) => r.in !== null).length;
  const late = day.rows.filter((r) => r.against.startsWith("Late")).length;
  const absent = day.rows.filter((r) => r.against === "Absent").length;
  const unplanned = day.rows.filter((r) => !r.rostered && r.in !== null).length;

  row.append(
    mark(`${present} of ${posted.length}`, "present against posted", "ok"),
    mark(String(late), "late", "warn"),
    mark(String(absent), "absent", "bad"),
    mark(String(unplanned), "present, not rostered", "warn"),
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

    const against = el("div", "ag");
    against.append(el("span", `pill ${row.tone}`, row.against));

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

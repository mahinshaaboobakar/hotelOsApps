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

import type { HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { codeChip } from "../../chrome/code";
import { standIn } from "../../chrome/standin";
import { ROSTER_READ } from "../../chrome/permissions";
import { load } from "../../roster";
import { recordedDay, type Day, type DayRow } from "../../roster/attendance";

/** Draw the screen. */
export async function attendance(host: HostApi, main: HTMLElement): Promise<void> {
  const got = await load(host, ROSTER_READ, "day", recordedDay);
  const day = got.value;

  const body = el("div", "body");
  body.append(marks(day), table(day.rows));

  if (!got.live) {
    body.append(standIn("day", got.because));
  }

  main.replaceChildren(header(day), body);
}

function header(day: Day): HTMLElement {
  const head = el("div", "head");
  const title = el("div");

  title.append(el("div", "ht", "Attendance"), el("div", "hsub", day.date));

  const picker = el("div", "sel");
  picker.append(el("span", undefined, day.department), el("i", undefined, "▾"));

  const grow = el("div", "grow");
  head.append(title, picker, grow,
    el("div", "btn", "‹ Fri 28 Aug ›"),
    el("div", "btn go", "＋ Mark attendance"));
  return head;
}

/** The four marks, each counted from the rows themselves. */
function marks(day: Day): HTMLElement {
  const row = el("div", "marks");

  const posted = day.rows.filter((r) => r.posted !== null);
  const present = posted.filter((r) => r.in !== null).length;
  const late = day.rows.filter((r) => r.against.startsWith("Late")).length;
  const absent = day.rows.filter((r) => r.against === "Absent").length;
  const unplanned = day.rows.filter((r) => r.posted === null && r.in !== null).length;

  row.append(
    mark(`${present} of ${posted.length}`, "present against posted", "ok"),
    mark(String(late), "late", "warn"),
    mark(String(absent), "absent", "bad"),
    mark(String(unplanned), "present, not rostered", "warn"),
  );

  return row;
}

function mark(figure: string, label: string, tone: string): HTMLElement {
  const card = el("div", `mk ${tone}`);
  card.append(el("b", undefined, figure), el("div", undefined, label));
  return card;
}

/**
 * What the rota planned, as the rota draws it.
 *
 * The chip and the time, not a string: this column is read against the rota
 * beside it, and a code that looks different here than it does there makes a
 * person check whether it is the same shift.
 */
function posted(value: string | null): HTMLElement {
  const cell = el("div", "postedcell");

  if (value === null) {
    cell.append(el("span", "quiet", "not rostered"));
    return cell;
  }

  const [code = "", ...time] = value.split(" ");
  cell.append(codeChip(code, tone(code)), el("span", "quiet", time.join(" ")));
  return cell;
}

/** The catalogue's tone for a code the row carries. */
function tone(code: string): string {
  if (code === "M") return "brand";
  if (code === "A") return "ok";
  if (code === "N") return "warn";
  return "neutral";
}

/** The day's rows, planned beside actual. */
function table(rows: readonly DayRow[]): HTMLElement {
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
      posted(row.posted),
      el("div", undefined, row.in ?? "—"),
      el("div", undefined, row.out ?? (row.in === null ? "—" : "— still in")),
      against,
    );

    list.append(item);
  }

  return list;
}

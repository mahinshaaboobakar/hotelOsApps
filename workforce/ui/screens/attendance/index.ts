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
    const panel = el("div", "panel");
    const note = el("div", "note");
    note.append(
      el("b", undefined, "Showing the approved example day. "),
      el("span", undefined, "The desktop has no Workforce client yet."),
    );
    panel.append(note);
    body.append(panel);
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
      el("div", "dim", row.posted ?? "not rostered"),
      el("div", undefined, row.in ?? "—"),
      el("div", undefined, row.out ?? (row.in === null ? "—" : "— still in")),
      against,
    );

    list.append(item);
  }

  return list;
}

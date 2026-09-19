/**
 * The five widget summaries, in the shape the service sends them.
 *
 * # Why a module ships facts at all
 *
 * The same reason `recorded.ts` does: the harness and the suite need something
 * true to draw. A widget on a property draws only what its read returned.
 *
 * # The wire's shape, with the canvas's numbers where the wire can carry them
 *
 * These were *"exactly as the approved canvas draws them"* — and the canvas and
 * `WidgetViews` disagree in places: the canvas draws four Attendance figures and
 * the wire sends three (present of rostered, late, absent); a late-in row's
 * shift start (`"07:00"`) and `"+22m"` where the wire sends the department and
 * the minutes late; `"2 of 11"` where it sends a rostered count; request rows
 * named `"Swap · N. Pillai → D. Rao"` where it sends the raiser and a colleague;
 * `"Wed–Fri"` where it sends names. A fixture is a claim about what the service
 * sends, and one that holds the drawing instead renders a card no property will
 * ever see (the app surface audit, 2026-09-19, records each divergence for the
 * owner). So these follow `WidgetViews.cs`, and keep the drawing's figures.
 *
 * # Numbers, not text
 *
 * Every count travels as a number and the widget writes it (NUM-Q1, ADR 0174);
 * a row names the drawing's form for it — `count`, `minutes`, `days`, `out-of`.
 *
 * # What is NOT here, and why
 *
 * Coming Up has two rows fewer than the approved catalogue: *unfilled* and
 * *thin*. Workforce has no staffing demand model, so neither has anything to
 * be measured against, and the honesty rule makes an unanswerable row absent
 * rather than approximate. The widget says so on its own face.
 */

import type {
  AttendanceToday,
  ComingUp,
  OnLeave,
  PendingRequests,
  RowForm,
  ShiftBoard,
  SummaryRow,
} from "./widget";

/** A row, as `WidgetViews.Row` builds one. */
function row(
  name: string,
  meta: string | null,
  value: number,
  form: RowForm,
  tone: SummaryRow["tone"],
  opens: string,
  extra: Partial<SummaryRow> = {},
): SummaryRow {
  return { name, meta, context: null, value, form, tone, opens, ...extra };
}

/** Shift Board — who is on now, by department. */
export const recordedShiftBoard: ShiftBoard = {
  onNow: 24,
  // Six, while four rows are drawn. The popover is one size and **content that
  // does not fit is cut by the widget, not by the shell** — so the figure
  // counts the property and the list shows what the frame holds.
  departments: 6,
  rows: [
    row("HK", null, 9, "count", "muted", "rota?department=HK", { from: "07:00", to: "15:00" }),
    row("FO", null, 5, "count", "muted", "rota?department=FO", { from: "07:00", to: "15:00" }),
    row("KIT", null, 6, "count", "muted", "rota?department=KIT", { from: "06:00", to: "14:00" }),
    row("ENG", null, 4, "count", "muted", "rota?department=ENG", { from: "08:00", to: "17:00" }),
  ],
  // **An instant, in the form the service sends.** The fixture carries what
  // the wire carries, so the recorded card and the live one render through
  // the same formatter — a fixture holding "15:00" would look right offline
  // and be the one thing never checked against the property's zone.
  nextChange: { at: "2026-09-05T09:30:00Z", on: 14, off: 14 },
};

/** Attendance Today — the rota against who came. */
export const recordedAttendanceToday: AttendanceToday = {
  figures: [
    { count: 34, of: 38, label: "present", tone: "ink" },
    { count: 3, of: null, label: "late", tone: "warn" },
    { count: 4, of: null, label: "absent", tone: "bad" },
  ],
  // Present-and-on-time, late, absent — over the rostered total, as the wire
  // builds it, so the bar adds up to what was planned.
  share: [
    { count: 31, tone: "ok" },
    { count: 3, tone: "warn" },
    { count: 4, tone: "bad" },
  ],
  byDepartment: [
    row("HK", null, 2, "count", "bad", "attendance?department=HK",
      { context: { count: 11, word: "rostered" } }),
    row("KIT", null, 1, "count", "bad", "attendance?department=KIT",
      { context: { count: 8, word: "rostered" } }),
    row("FO", null, 1, "count", "bad", "attendance?department=FO",
      { context: { count: 6, word: "rostered" } }),
  ],
  lateIn: [
    row("S. Kumar", "HK", 22, "minutes", "warn", "attendance?department=HK"),
    row("A. Fernandes", "KIT", 14, "minutes", "warn", "attendance?department=KIT"),
  ],
};

/** Pending Requests — what is waiting, oldest first. */
export const recordedPendingRequests: PendingRequests = {
  figures: [
    { count: 6, of: null, label: "leave", tone: "ink" },
    { count: 4, of: null, label: "swaps", tone: "ink" },
  ],
  rows: [
    row("N. Pillai", "HK · with D. Rao", 6, "days", "warn", "leave?department=HK"),
    row("M. Joseph", "FO", 5, "days", "warn", "leave?department=FO"),
    row("S. Kumar", "HK", 4, "days", "warn", "leave?department=HK"),
    row("T. Abraham", "KIT · with J. Luke", 3, "days", "warn", "leave?department=KIT"),
    row("A. Fernandes", "KIT", 2, "days", "muted", "leave?department=KIT"),
  ],
};

/** Coming Up — the next seven days, for what can be measured. */
export const recordedComingUp: ComingUp = {
  figures: [
    { count: 3, of: null, label: "overlapping leave", tone: "warn" },
    { count: 2, of: null, label: "certs expiring", tone: "warn" },
  ],
  // **The wire's shape, not the frame's.** The day is an ISO date in `on` and
  // the panel says it in the property's form — a fixture that carried "Thu 11"
  // would look right offline and be the one card never checked against a
  // property's own zone and locale.
  overlaps: [
    row("Housekeeping", null, 11, "out-of", "warn", "leave?department=HK",
      { on: "2026-09-11", context: { count: 3, word: "away" } }),
    row("Kitchen", null, 8, "out-of", "warn", "leave?department=KIT",
      { on: "2026-09-12", context: { count: 2, word: "away" } }),
    row("Front Office", null, 6, "out-of", "warn", "leave?department=FO",
      { on: "2026-09-13", context: { count: 2, word: "away" } }),
  ],
  expiring: [
    row("Fire warden · S. Kumar", null, 4, "days", "bad", "people?capability=expiring"),
    row("Food safety · T. Abraham", null, 6, "days", "bad", "people?capability=expiring"),
  ],
};

/** On Leave — who is away, today and for the rest of the week. */
export const recordedOnLeave: OnLeave = {
  figures: [
    { count: 5, of: null, label: "away today", tone: "ink" },
    { count: 12, of: null, label: "this week", tone: "muted" },
  ],
  today: [
    row("HK", "P. Das, R. Kurian", 2, "count", "muted", "leave?department=HK"),
    row("KIT", "V. Nambiar", 1, "count", "muted", "leave?department=KIT"),
    row("FO", "L. D'Souza", 1, "count", "muted", "leave?department=FO"),
    row("ENG", "B. Shetty", 1, "count", "muted", "leave?department=ENG"),
  ],
  restOfWeek: [
    row("HK", "M. Joseph, S. Kumar, A. Nair, J. Paul", 4, "count", "muted", "leave?department=HK"),
    row("KIT", "T. Abraham, V. Nambiar, R. Das", 3, "count", "muted", "leave?department=KIT"),
  ],
};

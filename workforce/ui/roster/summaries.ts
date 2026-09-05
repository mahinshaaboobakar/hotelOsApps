/**
 * The five widget summaries, exactly as the approved canvas draws them.
 *
 * # Why a module ships facts at all
 *
 * The same reason `recorded.ts` does: the desktop has no Workforce client, so
 * `host.call` fails `unavailable` and a widget needs something true to draw
 * until one lands. A widget is told which it got, like every screen.
 *
 * # These are the canvas's numbers, not invented ones
 *
 * Read off the five approved artboards — `ShiftBoard`, `AttendanceToday`,
 * `PendingRequests`, `ComingUp`, `OnLeave`. A capture beside a frame is
 * evidence only when the two show the same property.
 *
 * # They are property-wide, and the screens' facts are not
 *
 * `recorded.ts` is one department's week — six people in Front Office — and
 * these count six departments and thirty-eight rostered. That is not a
 * contradiction to be reconciled: a widget answers a property-wide question
 * and the screen it taps through to answers a departmental one. Making the two
 * agree numerically would mean making one of them wrong.
 *
 * # What is NOT here, and why
 *
 * Coming Up has two rows fewer than the approved catalogue: *unfilled* and
 * *thin*. Workforce has no staffing demand model, so neither has anything to
 * be measured against, and the honesty rule makes an unanswerable row absent
 * rather than approximate. The widget says so on its own face. They return
 * when a demand model does.
 */

import type {
  AttendanceToday,
  ComingUp,
  OnLeave,
  PendingRequests,
  ShiftBoard,
  SummaryRow,
} from "./widget";

/** A row, with the fields the frames give every row. */
function row(
  name: string,
  meta: string | null,
  value: string,
  tone: SummaryRow["tone"],
  opens: string,
): SummaryRow {
  return { name, meta, value, tone, opens };
}

/** Shift Board — who is on now, by department. */
export const recordedShiftBoard: ShiftBoard = {
  onNow: 24,
  // Six, while four rows are drawn. The popover is one size and **content that
  // does not fit is cut by the widget, not by the shell** — so the figure
  // counts the property and the list shows what the frame holds.
  departments: 6,
  rows: [
    row("Housekeeping", "07:00–15:00", "9", "muted", "rota?department=HK"),
    row("Front Office", "07:00–15:00", "5", "muted", "rota?department=FO"),
    row("Kitchen", "06:00–14:00", "6", "muted", "rota?department=KIT"),
    row("Engineering", "08:00–17:00", "4", "muted", "rota?department=ENG"),
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
    { value: "38", label: "rostered", tone: "ink" },
    { value: "34", label: "present", tone: "ok" },
    { value: "4", label: "absent", tone: "bad" },
    { value: "3", label: "late", tone: "warn" },
  ],
  // 34 present against 4 absent — the bar is the two figures above it in
  // proportion, not a third number.
  share: [
    { count: 34, tone: "ok" },
    { count: 4, tone: "bad" },
  ],
  byDepartment: [
    row("Housekeeping", "2 of 11", "2", "bad", "attendance?department=HK&mark=absent"),
    row("Kitchen", "1 of 8", "1", "bad", "attendance?department=KIT&mark=absent"),
    row("Front Office", "1 of 6", "1", "bad", "attendance?department=FO&mark=absent"),
  ],
  lateIn: [
    row("S. Kumar · Housekeeping", "07:00", "+22m", "warn", "attendance?mark=late"),
    row("A. Fernandes · Kitchen", "06:00", "+14m", "warn", "attendance?mark=late"),
  ],
};

/** Pending Requests — what is waiting, oldest first. */
export const recordedPendingRequests: PendingRequests = {
  figures: [
    { value: "4", label: "swaps", tone: "warn" },
    { value: "6", label: "leave", tone: "warn" },
  ],
  rows: [
    row("Swap · N. Pillai → D. Rao", "Housekeeping", "6d", "warn", "leave?tab=approvals"),
    row("Leave · M. Joseph", "Front Office", "5d", "warn", "leave?tab=approvals"),
    row("Leave · S. Kumar", "Housekeeping", "4d", "warn", "leave?tab=approvals"),
    row("Swap · T. Abraham → J. Luke", "Kitchen", "3d", "warn", "leave?tab=approvals"),
    row("Leave · A. Fernandes", "Kitchen", "2d", "warn", "leave?tab=approvals"),
  ],
};

/** Coming Up — the next seven days, for what can be measured. */
export const recordedComingUp: ComingUp = {
  figures: [
    { value: "3", label: "overlapping leave", tone: "warn" },
    { value: "2", label: "certs expiring", tone: "warn" },
  ],
  // **The wire's shape, not the frame's.** The day is an ISO date in `meta`
  // and the panel says it in the property's form — a fixture that carried
  // "Thu 11" would look right offline and be the one card never checked
  // against a property's own zone and locale.
  overlaps: [
    { ...row("Housekeeping", "3 away", "of 11", "warn", "leave?department=HK"),
      on: "2026-09-11" },
    { ...row("Kitchen", "2 away", "of 8", "warn", "leave?department=KIT"),
      on: "2026-09-12" },
    { ...row("Front Office", "2 away", "of 6", "warn", "leave?department=FO"),
      on: "2026-09-13" },
  ],
  expiring: [
    row("Fire warden · S. Kumar", null, "4d", "warn", "people?capability=expiring"),
    row("Food safety · T. Abraham", null, "6d", "warn", "people?capability=expiring"),
  ],
};

/** On Leave — who is away, today and for the rest of the week. */
export const recordedOnLeave: OnLeave = {
  figures: [
    { value: "5", label: "away today", tone: "ink" },
    { value: "12", label: "this week", tone: "muted" },
  ],
  today: [
    row("Housekeeping", "P. Das, R. Kurian", "2", "muted", "leave?department=HK"),
    row("Kitchen", "V. Nambiar", "1", "muted", "leave?department=KIT"),
    row("Front Office", "L. D'Souza", "1", "muted", "leave?department=FO"),
    row("Engineering", "B. Shetty", "1", "muted", "leave?department=ENG"),
  ],
  restOfWeek: [
    row("Housekeeping", "Wed–Fri", "4", "muted", "leave?department=HK"),
    row("Kitchen", "Thu–Sat", "3", "muted", "leave?department=KIT"),
  ],
};

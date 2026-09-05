/**
 * What a Workforce widget is given — the shapes, with no data in them.
 *
 * Parallel to `model.ts` and deliberately separate from it: that file's one
 * line is *what a **screen** is given*, and a widget is not a screen. A screen
 * shows a department's week; a widget answers one property-wide question at a
 * glance, and the two want different shapes even where they read the same rows.
 *
 * # Every widget is the same five parts
 *
 * The popover is one size for every widget (`SHELL-Q35`), so the vocabulary is
 * fixed rather than per-widget: a headline row of figures, labelled sections,
 * rows of three fields, and a note. A widget that invented a sixth part would
 * be a widget the shell's single frame could not promise to hold.
 *
 * # A row knows where it opens
 *
 * `56-app-widgets.md`: *every element taps through to that filtered screen —
 * not to the app's home*. So the destination is data on the row rather than a
 * branch at the click site, and a row that could not name one would be a row
 * with nothing to tap to.
 */

/** How a value reads — the widget's whole colour vocabulary. */
export type Tone = "ink" | "muted" | "ok" | "warn" | "bad";

/** One figure in a widget's headline row. */
export interface Figure {
  /** The number, already formatted. A widget never computes one. */
  value: string;

  /** What it counts, in the frame's own words. */
  label: string;

  /** How it reads. */
  tone: Tone;
}

/** One row of a widget's list: what it is, a qualifier, and a value. */
export interface SummaryRow {
  /** The subject — a department, a person, a request. */
  name: string;

  /**
   * The day this row is about, as an **ISO date** — only rows that have one.
   *
   * Carried beside the name rather than rendered into it, because the service
   * cannot say a date in the property's form: it does not know the locale. The
   * panel that knows its own rows are dated composes the name; the generic row
   * renderer ignores this, and every other widget leaves it undefined.
   */
  on?: string;

  /** The qualifier beside it, or null when the row has none. */
  meta: string | null;

  /** The value at the right. */
  value: string;

  /** How the value reads. */
  tone: Tone;

  /**
   * Where tapping this row opens the application.
   *
   * A screen name and its filter, as the module's own rail spells them. It is
   * carried and **not yet acted on** — see `widgets/card.ts`: the shell's
   * bridge has no navigation channel, so this is the one place that will need
   * changing when it does.
   */
  opens: string;
}

/** When the next set of people comes on, and how many change over. */
export interface Changeover {
  /**
   * When it happens — an **ISO instant**, never a rendered time.
   *
   * The card formats it with the property's zone and locale. A service that
   * sent "15:00" would have chosen a timezone on the property's behalf, and a
   * Gulf property would read a Kochi hour with nothing anywhere saying so.
   */
  at: string;

  /** How many start. */
  on: number;

  /** How many finish. */
  off: number;
}

/** Shift Board — who is on now, by department, and what changes next. */
export interface ShiftBoard {
  /** People on shift at this moment, property-wide. */
  onNow: number;

  /** How many departments have somebody on. */
  departments: number;

  /** A row per department on now: name, the shift's hours, the headcount. */
  rows: readonly SummaryRow[];

  /**
   * The next changeover, or null when nothing more changes today.
   *
   * Null rather than a placeholder time: *uncomputable is absent* — a widget
   * that drew a dash here would be answering a question it had not been able
   * to ask.
   */
  nextChange: Changeover | null;
}

/**
 * One part of a proportion bar.
 *
 * A **count**, never a percentage. The width is presentation arithmetic the
 * widget does over the counts it was given; a percentage in the data would be
 * a number the backend never computed, arriving as though it had.
 */
export interface Segment {
  /** How many this part stands for. */
  count: number;

  /** How it reads. */
  tone: Tone;
}

/** Attendance Today — the rota against who actually came. */
export interface AttendanceToday {
  /** The four headline figures, in the frame's order. */
  figures: readonly Figure[];

  /** Present against absent, drawn in proportion under the figures. */
  share: readonly Segment[];

  /** Absences against the rota, by department: "2 of 11". */
  byDepartment: readonly SummaryRow[];

  /** Who arrived after the rota expected them, and by how much. */
  lateIn: readonly SummaryRow[];
}

/** Pending Requests — swaps and leave waiting on a decision, oldest first. */
export interface PendingRequests {
  /** Swaps waiting, and leave waiting. */
  figures: readonly Figure[];

  /** The queue itself, oldest first. */
  rows: readonly SummaryRow[];
}

/** Coming Up — the next seven days' risks, for the risks that can be measured. */
export interface ComingUp {
  /** Overlapping leave, and certifications lapsing. */
  figures: readonly Figure[];

  /** Two or more away from one department on one day. */
  overlaps: readonly SummaryRow[];

  /** Certifications that lapse inside the window. */
  expiring: readonly SummaryRow[];
}

/** On Leave — who is away today, and for the rest of the week. */
export interface OnLeave {
  /** Away today, and away at some point this week. */
  figures: readonly Figure[];

  /** Away today, by department, with the names. */
  today: readonly SummaryRow[];

  /** The rest of the week, by department, with the days. */
  restOfWeek: readonly SummaryRow[];
}

/**
 * What a Workforce screen is given — the shapes, with no data in them.
 *
 * These are the module's own view types, not the service's DTOs. A screen
 * renders what it is handed; the day a Workforce client lands in the desktop,
 * the mapping into these shapes is one file's problem and no screen's.
 *
 * # Every judgment is already made when a screen sees it
 *
 * The backend computes the band, the lateness, the planned hours and the
 * overtime, because each is derived from data a screen does not hold. A screen
 * that recomputed one would be a second implementation of a rule — and the two
 * would drift in the direction nobody checks.
 */

import type { Span } from "../chrome/clock";

/** A shift as the catalogue defines it — `WF-Q11`, property-created. */
export interface Shift {
  /** The catalogue entry's id. */
  id: string;

  /** What fits a rota cell, and survives a photocopier. */
  code: string;

  /** What people read. */
  name: string;

  /**
   * How the week reads at a glance — the property's own choice.
   *
   * A **token name**, never a hex value: the catalogue stores what the property
   * picked and the module resolves it to a published shell token, so a shift
   * cannot introduce a colour the platform does not have.
   */
  tone: "brand" | "ok" | "warn" | "bad" | "neutral";

  /** The hours, as a person reads them. Absent for an off entry — `WF-Q12`. */
  /**
   * The shift's hours, as two ends — null on an off day.
   *
   * It arrived joined (`07:00–15:00`), so the separator and the hour cycle were
   * a service's (ADR 0175). The surface composes it through `chrome/clock`.
   */
  hours: Span | null;
}

/** One person's one day on the rota. */
export interface Cell {
  /** The catalogue entry, or null when nothing is rostered. */
  shift: Shift | null;

  /**
   * A one-off span for this day only — `WF-Q17`.
   *
   * **Not a copy of the catalogue's hours**, which would be a projection a
   * client is allowed to disagree with. It is a different fact: this person,
   * this day, deliberately outside the entry's hours. It renders *anchored to*
   * the chip rather than replacing it, because the frame's cell must still
   * carry a colour and a short code.
   */
  /** A one-off span for this day, as two ends — null when the cell has none. */
  override: Span | null;

  /** Approved leave covering the day, by its type's name. */
  leave: string | null;

  /** True where the rota planned nobody and somebody is needed. */
  gap: boolean;
}

/** A person, and their week. */
export interface Person {
  id: string;
  name: string;

  /** Drawn in the avatar. Derived here so every row derives it the same way. */
  initials: string;

  /** The job role from their posting. */
  role: string;

  /** The zone the posting carries, when it carries one — `WF-Q7`. Optional. */
  zone: string | null;

  /** Whether this posting is the department's headship. */
  head: boolean;

  /** Seven cells, Monday first. */
  week: readonly Cell[];
}

/** One stretch of the Manager-on-Duty ribbon — `WF-Q8`, a span. */
export interface DutySpan {
  /** Who holds it, or null for an uncovered stretch the register draws. */
  who: string | null;

  /** A department code shown beside the name, when there is one. */
  department: string | null;

  /**
   * The span, as **ISO instants** — null when it is a whole day.
   *
   * The ribbon and the duty register draw the same spans, so both take
   * instants and both say them in the property's clock. One rendering the
   * server's hours while the other rendered the property's would be the same
   * fact told two ways on two screens.
   *
   * A shift's `hours` above is deliberately NOT this: a Morning shift starts
   * at 07:00 wherever the property is, so it is a wall clock with no zone, and
   * putting it through an instant formatter would attach a timezone to
   * something that never had one.
   */
  startsAt: string | null;
  endsAt: string | null;

  /** Where the span starts, as a fraction of the week. */
  from: number;

  /** How much of the week it covers. */
  span: number;

  /** Whether it runs into the next day — drawn as a continuous bar. */
  overnight: boolean;
}

/**
 * Somebody planned past the property's overtime threshold — `WF-Q14`.
 *
 * **Warn, never block.** It carries the number because *"Vishnu is over"* tells
 * a manager nothing they can act on, and *"60.0 against 48"* tells them how
 * much to move.
 */
export interface OvertimeWarning {
  who: string;
  /**
   * Hours planned, as a number.
   *
   * It carried `" h planned"` and the surface wrote *"is planned … hours
   * against …"* around it, so the sentence rendered **"is planned 9 h planned
   * hours against 8"**. Two words composed in a service and the rest here,
   * stuttering where they met.
   */
  planned: number;
  threshold: string;
}

/** A department's week, as the Team Rota draws it. */
export interface Week {
  /**
   * The Monday this week starts on, as `YYYY-MM-DD`.
   *
   * **The only machine-readable date on this screen.** {@link Week.days} are
   * seven display headings and {@link Week.label} is a rendered range; every
   * write the rota makes names a date, and a surface that parsed its own
   * heading to recover one would be deriving a fact from a presentation.
   */
  monday: string;

  /** The department this rota is for. */
  department: string;

  /**
   * The same department as a code — `FO`, `HK` — which is what a write names.
   *
   * {@link Week.department} is for reading and this is for sending. They are
   * two fields because a name is not a key, and the day somebody renames a
   * department is the day a screen that sent the name would start writing to
   * nothing.
   */
  departmentCode: string;

  /** The week's label, as the header shows it. */
  label: string;

  /**
   * The month the week sits in, spelled as a person reads it.
   *
   * Stated rather than sliced off the label: the grid's day headings are short
   * by design ("Thu 27"), and the picker needs the month ("Thu 27 Aug") because
   * it is naming one particular day rather than heading a column. Deriving it
   * from a display string is the kind of thing that survives until a label is
   * reworded.
   */
  month: string;

  /** Seven day headings, Monday first. */
  days: readonly string[];

  /** The MOD ribbon across the same seven days. */
  duty: readonly DutySpan[];

  /** The people, in the order the rota lists them. */
  people: readonly Person[];

  /** The catalogue, as the picker offers it. */
  catalogue: readonly Shift[];

  /** Anybody the plan pushes past the threshold. Empty when nothing to say. */
  overtime: readonly OvertimeWarning[];
}

/**
 * Who is signed in, drawn at the bar's right.
 *
 * **Three clauses, not two** — `name · department · property`, owner ruling,
 * 2026-09-04. The property looks redundant on a single-property desk and stops
 * looking redundant the day an organization has two, which the corporate model
 * already allows for. A desk machine is shared and every write on these screens
 * is attributed, so the bar says *who*, *for which department*, *at which
 * hotel*.
 */
export interface Operator {
  /**
   * Which staff member, when the property can say.
   *
   * **The bar never draws it**, and it lives here rather than beside the bar's
   * own types for that reason: it is the `me` read's shape, and the bar is one
   * of its readers. Three screens ask this application a question about the
   * signed-in person — their month, their leave, a request they are raising —
   * and each names a staff id the bundle has no other way to learn.
   *
   * Null exactly when {@link Operator.name} is null, which is the collapse the
   * bar makes on purpose: a caller with no user, a login with no staff record
   * and a person no longer active are one answer, and an id carried through any
   * of them would have split at the wire what the screen shows as one.
   */
  staffId: string | null;

  name: string | null;

  /** Which department they are working in. */
  department: string | null;

  /** Which hotel. */
  property: string | null;

  /** Their role, which the bar has no room for and the rail used to show. */
  role: string | null;
}

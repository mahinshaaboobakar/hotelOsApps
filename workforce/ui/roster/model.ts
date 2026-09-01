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
  hours: string | null;
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
  override: string | null;

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

  /** The hours, when the span is not a whole day. */
  hours: string | null;

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
  planned: string;
  threshold: string;
}

/** A department's week, as the Team Rota draws it. */
export interface Week {
  /** The department this rota is for. */
  department: string;

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

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

  /**
   * The department a cell on this row is assigned under — their primary
   * posting's code, the same posting {@link Person.role} comes from.
   *
   * **Not {@link Week.departmentCode}**, which is the filter the week was read
   * with and is `""` when none was named. Sending that refused every
   * assignment on a real property.
   */
  departmentCode: string;

  /** The zone the posting carries, when it carries one — `WF-Q7`. Optional. */
  /**
   * **Gone from the rota** — owner, 2026-09-20, on `64g` §5.
   *
   * The frames drew "Night auditor · Zone 1" under each name and the service
   * has never sent a zone here, so a real property read the role alone. Ruled
   * together with Attendance's, so nobody is zoned on one screen and unzoned
   * on the next. The posting's zone still lives on People, where `WF-Q7` put
   * it: this is the rota's row, not the fact.
   */
  zone?: never;

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

  /**
   * Whether the week's total is over the weekly threshold, and that threshold.
   *
   * These replaced `threshold: string`, which the service filled with English
   * it composed ("over the weekly threshold", "2 day over") and this screen
   * placed after "against" — so a property read "against over the weekly
   * threshold". The fixture sent "48", so no harness ever showed it.
   */
  weekly: boolean;
  weeklyHours: number | null;

  /** How many days are over the daily threshold, and that threshold. */
  daysOver: number;
  dailyHours: number | null;
}

/** A department's week, as the Team Rota draws it. */
export interface Week {
  /**
   * The Monday this week starts on, as `YYYY-MM-DD`.
   *
   * **Every date on this screen is machine-readable now.** It was the only
   * one: `days` were seven rendered headings and `label` a rendered range, and
   * a surface that parsed its own heading to recover a date would be deriving
   * a fact from a presentation.
   */
  monday: string;

  /**
   * The department the week was asked for, echoed from the request — null
   * when none was named, which is every read this screen makes today.
   *
   * This said *"the department this rota is for"* and was typed as a name.
   * The service echoes the request's parameter: a code when one was sent,
   * null otherwise — so a picker rendering it read "null · one shift per day".
   */
  department: string | null;

  /**
   * The same filter as a code, `""` when none was named.
   *
   * **A filter, not a key a cell write can use** — `copyWeek` passes it on,
   * where `""` means every department; `assign` takes the row's own
   * {@link Person.departmentCode} instead.
   */
  departmentCode: string;

  /** The Sunday this week ends on, as `YYYY-MM-DD`. */
  sunday: string;

  /**
   * The week's seven days, Monday first, each as `YYYY-MM-DD`.
   *
   * They arrived as `"MON 24"` — rendered AND uppercased in the service, and
   * case is a script's property rather than a style: a locale whose weekday
   * names carry no case distinction gets the same string back, and one whose
   * uppercase rules differ from the invariant gets the wrong letters.
   *
   * The screen chooses how short to draw them. A column heading wants
   * `weekday-day`; the picker names one particular day and wants the month with
   * it, which is why `month` is gone — it existed only because the headings
   * were too short to carry one.
   */
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

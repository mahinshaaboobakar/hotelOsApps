/**
 * What a Jobs screen is given — the shapes, with no data in them. These are
 * the module's view types, not the service's DTOs; every judgment (concern,
 * accountable, running) arrives made, so no screen recomputes a rule.
 */

/** One row of the board — frame 1's nine columns. */
export interface JobRow {
  id: string;
  number: string;
  where: string;
  what: string;
  priority: string;
  status: string;
  raisedBy: string;
  assignedTo: string;
  concern: string;
  concernDetail: string | null;
  /** ISO instant, or null for a job with no clock. */
  dueAt: string | null;
  tags: readonly string[];

  /**
   * Whether the person looking at this holds it. The module cannot know who is
   * looking — `ModuleIdentity` carries no user — so the service says, where the
   * caller is established. The work controls are the assignee's own acts.
   */
  viewerIsAssignee: boolean;
}

/**
 * What a page of a list carries — CORE-Q13's <code>PagedResponse</code>, field
 * for field. The shape was minted in Jobs and is now the platform's; the
 * module mirrors it so that when a client lands the mapping is one field to
 * one, and so the numbered pager divides by the size the service <b>applied</b>
 * rather than the one it was asked for.
 */
export interface Paging {
  page: number;
  pageSize: number;
  total: number;
}

/** A page of the board. */
export interface BoardPage {
  rows: readonly JobRow[];
  paging: Paging;
}

/** Today's strip above the board. */
export interface Today {
  open: number;
  breached: number;
  stuck: number;
  running: number;
  closedToday: number;
  avgResolveMinutes: number;
  department: string;
  /** ISO instant the strip was read. */
  at: string;
}

/** A key/value line on the Overview tab. */
export interface Detail {
  k: string;
  v: string;
}

/** One session on the Work tab. */
export interface Session {
  no: number;
  who: string;
  startedAt: string;
  pausedAt: string | null;
  pauseReason: string | null;
  resumedAt: string | null;
  stoppedAt: string | null;
  workedSeconds: number;
}

/** One line of the History tab — status, concern or work, interleaved. */
export interface HistoryLine {
  at: string;
  kind: "status" | "concern" | "work";
  what: string;
  by: string;
  detail: string;
}

export interface Note {
  who: string;
  at: string;
  text: string;
  photo: string | null;

  /** The text the job was raised with, shown as such (frame 2d). */
  raising?: boolean;
}

/**
 * Who is signed in, as the service knows them. The module cannot invent this:
 * `ModuleIdentity` carries capabilities, not a person, so a name in the header
 * that the module made up would be a fabricated identity shown to a real one
 * (audit finding, 2026-09-04).
 */
export interface Operator {
  name: string;
  where: string;
}

export interface Step {
  no: number;
  number: string;
  what: string;
  status: string;
  clock: string;
  assignedTo: string;
}

export interface Link {
  number: string;
  department: string;
  what: string;
  status: string;
  assignedTo: string;
}

export interface Rating {
  stars: number;
  text: string;
  ratedAt: string;
  askedAt: string;
  windowUntil: string;
  resolvedBy: string;
  minutesRaisedToResolved: number;
}

/** Everything the job view's seven tabs draw — frames 2 to 2g. */
export interface JobDetail {
  row: JobRow;

  /** How it was raised, as parts — the module composes the line, so every date goes through the formatter. */
  raised: { at: string; via: string; kind: string; who: string };

  /** When it ended, for a closed job. */
  endedAt: string | null;

  /**
   * Seconds worked on the running session as of the service's reply, or null
   * when nothing runs. Never computed from the machine's clock: a desktop
   * whose clock is minutes off would show a figure the property never had.
   */
  runningSeconds: number | null;
  runningWho: string | null;
  totalWorkedSeconds: number;
  accountable: string;
  whatAndWhere: readonly Detail[];
  whoAsked: readonly Detail[];
  priorityAndTime: readonly Detail[];
  assignment: readonly Detail[];
  resolution: string | null;
  sessions: readonly Session[];
  history: readonly HistoryLine[];
  notes: readonly Note[];
  steps: readonly Step[];
  links: readonly Link[];
  rating: Rating | null;
  record: readonly Detail[];
}

/** One department on the Live tab. */
export interface LiveDepartment {
  code: string;
  name: string;
  presence: "present" | "hours" | "off";
  presenceLine: string;
  people: readonly { name: string; doing: string; tone: "run" | "hold" | "bad" | "dim" }[];
  peopleTotal: number;
  open: number;
  breached: number;
}

/** One row of the Live tab's concern table. */
export interface ConcernRow {
  number: string;
  department: string;
  concern: string;
  since: string;
  accountable: string;
  lastNudge: string;
}

export interface Live {
  departments: readonly LiveDepartment[];
  concern: readonly ConcernRow[];
  sweptAt: string;
}

/** A scheduled row — frame 6. */
export interface ScheduledRow {
  scheduledFor: string;
  number: string;
  where: string;
  what: string;
  tags: readonly string[];
  raisedBy: string;
  assignedTo: string;
  dueAt: string;
}

export interface CatalogueCategory {
  id: string;
  name: string;
  department: string;
  items: number;
  activeHere: boolean;
}

export interface CatalogueItem {
  id: string;
  categoryId: string;
  name: string;
  department: string;
  defaultPriority: string;
  dueWithinMinutes: number | null;
  restricted: boolean;
  aliases: readonly string[];
  activeAt: readonly { property: string; on: boolean }[];
  /**
   * What may be chosen when this is resolved — with ids, because resolving
   * names one and the service stores an id, not a phrase.
   */
  resolutions: readonly { id: string; name: string; noteRequired: boolean }[];
}

export interface Catalogue {
  organisation: string;
  categories: readonly CatalogueCategory[];
  items: readonly CatalogueItem[];
}

/** One policy on the settings list — page 02 frame 7. */
export interface PolicyRow {
  scope: "property" | "department" | "category" | "item";
  scopeLabel: string;
  name: string;
  due: string;
  atRisk: string;
  ladder: string;
  usedBy: string;
}

export interface PolicyRule {
  priority: string;
  due: string;
  atRisk: string;
  notAccepted: string;
  noSession: string;
  ladder: string;
  managerAtRisk: boolean;
}

export interface PresenceRow {
  department: string;
  enabled: boolean;
  followShifts: boolean;
  hours: string;
  now: string;
}

export interface Settings {
  scopes: readonly { label: string; state: string; indent: number }[];
  policies: readonly PolicyRow[];
  engineeringRules: readonly PolicyRule[];
  presence: readonly PresenceRow[];
  whoIsTold: readonly { role: string; atRisk: boolean; breached: string; stuck: string; untriaged: boolean; repeat: string; departments: string }[];
  holds: readonly Detail[];
  holdWarnings: readonly { when: string; who: string }[];
  closing: readonly { scope: string; hours: string }[];
  rating: readonly Detail[];
  access: readonly { label: string; who: string; from: string }[];
  numbering: string;
}

/** One row of a dock widget — what it is, how long it has been, its tone. */
export interface WidgetRow {
  id: string;
  number: string;
  what: string;
  since: string;
  tone: "warn" | "hold" | "run" | "bad";
}

/**
 * <i>The Board</i> — the shape of the work right now, and what has waited
 * longest unclaimed. Z's canvas, owner-approved 2026-09-03.
 */
export interface BoardNow {
  raised: number;
  running: number;
  onHold: number;
  doneToday: number;
  longestWaiting: readonly WidgetRow[];
}

/**
 * <i>Blocked</i> — two states, because whose delay it is decides whose clock
 * runs: a held job's concern clock is stopped, a paused session's is not.
 */
export interface BlockedNow {
  onHold: number;
  pausedCount: number;
  held: readonly WidgetRow[];
  paused: readonly WidgetRow[];
}

/** The widget's three numbers and the worst rows — the manifest's `jobs-now`. */
export interface JobsNow {
  scope: string;
  open: number;
  running: number;
  atRisk: number;
  breached: number;
  stuck: number;
  worst: readonly { number: string; line: string; tone: "bad" | "warn" | "run" }[];
  unreadNudges: number;
}

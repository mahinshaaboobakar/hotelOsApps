/**
 * The stay's other five tabs. Frames 4, 5, 5b, 6 and 7.
 */

import type { Chip, Tag } from "./day";

/**
 * One line of the activity list — frame 4.
 *
 * **Not `Moment`**, which frame 3's compact timeline already uses. They are two
 * renderings of the same underlying events at different fidelity — the overview
 * shows a dot, a time and a phrase; this shows the date, who said it, and the
 * basis. Both are projected from one backend source, so the two cannot disagree
 * about what happened; what differs is how much of it is drawn.
 *
 * **Three sources, one list, and the difference is never hidden.** `pms` is a
 * fact the PMS wrote, `override` is one of ours with a person's name on it, and
 * `other` is another application's own record — **read through the Context
 * Service and stored nowhere here**. If that application is uninstalled
 * tomorrow its rows simply stop appearing, and nothing in this stay's history
 * is orphaned.
 */
export interface ActivityEntry {
  /** `28 Aug` and `09:14`, as two lines. */
  date: string;
  time: string;

  /** Who said it — `Opera`, `Anitha M.`, `Room Care`, `Jobs`. */
  who: Chip;

  what: string;

  /** The line under it: the basis, the reference, where it was read from. */
  detail: string;

  /**
   * True for the row that records a disagreement.
   *
   * Washed in place rather than lifted into a banner: **clearing a
   * disagreement adds a row, it never removes one**, so a banner would vanish
   * and take the history with it.
   */
  disagrees: boolean;
}

/** Which sources the activity list is showing — frame 4's four buttons. */
export interface SourceFilter {
  label: string;
  on: boolean;
}

/** The activity tab — frame 4. */
export interface Activity {
  filters: readonly SourceFilter[];
  entries: readonly ActivityEntry[];
}

/** One guest request, and whatever became of it — frame 5. */
export interface Request {
  /** `14:35`, or a reference where the row is a job. */
  key: string;

  what: string;

  /** `raised as JOB-8821`, `no job needed`, `logged`. */
  state: string | null;
  stateTone: "ok" | "warn" | "neutral";

  /** A quieter trailing note — `since 15:06`. */
  note: string | null;
}

/**
 * The requests tab — frames 5 and 5b.
 *
 * **The request is ours; the work is not.** GuestOps records the guest's
 * request and announces it; Jobs creates the job and owns everything after
 * that — assignment, status, completion. GuestOps never calls Jobs, never
 * stores a job's status and never assigns a person.
 */
export interface Requests {
  /** What the guest asked for. Always present, whatever else is installed. */
  ours: readonly Request[];

  /**
   * What Jobs made of them, resolved live through Context.
   *
   * Null when Jobs is not installed on this property — which is frame 5b, and
   * is a different state from *installed and nothing raised yet*. The two must
   * not collapse: one invites the property to install Jobs and the other would
   * be telling them to install what they already have.
   */
  jobs: readonly Request[] | null;

  /**
   * True when Jobs answered for this property.
   *
   * **Null means nobody established it**, and the screen then draws the
   * installed variant — because an application's own flow is never gated on a
   * neighbour (owner ruling, 2026-08-31), and guessing *absent* would take the
   * raise button away from a property that has Jobs.
   */
  jobsInstalled: boolean | null;
}

/** One night of the stay — frame 6's strip. */
export interface Night {
  /** `Sun 31 Aug`, with the weekday and date split for the design's emphasis. */
  weekday: string;
  date: string;

  /** `arrival`, `today`, `departure` — the design's trailing qualifier. */
  qualifier: string | null;

  /** True for the night the property is currently on. */
  now: boolean;

  /** What happened, as a mark where it is a fact and a pill where it is a state. */
  mark: Chip | null;
  state: string | null;
  stateTone: "ok" | "warn" | "neutral";

  /** The explanation under it. */
  detail: string | null;

  /** An action offered on that night — `Ask again this evening`. */
  action: string | null;
}

/**
 * The servicing tab — frame 6.
 *
 * **GuestOps owns none of this.** It announces occupancy and departure; Room
 * Care decides what work that becomes (APPS-Q1, S21). The tab reports and
 * asserts nothing — which is why a declined day is *declined* rather than
 * clean or dirty, and why the strip is per night rather than one status.
 */
export interface Servicing {
  /** Null when Room Care is not installed — the tab is then dimmed, not empty. */
  nights: readonly Night[] | null;
  roomCareInstalled: boolean | null;
}

/** One row of the payment tab. */
export interface TermRow {
  label: string;
  value: string;
  strong?: string;
  tail?: string;

  /** True for the two rows the design sets large — the rate and the total. */
  big?: boolean;

  tags: readonly Tag[];
}

/**
 * The payment tab — frame 7, and the band that is a reported finding.
 *
 * **The terms are v1 and buildable today** (GUEST-Q6): what the stay was sold
 * on, with every amount carrying value, currency and whether tax is included
 * (R19), and every deadline **computed from its stored offset, never stored**
 * (R18) — move the arrival and the cancellation deadline moves with it.
 *
 * **The folio is not ruled and nothing is built behind it.** In a PMS-connected
 * property it lives in Opera and showing it here needs the connector to carry a
 * balance, which v1's inbound contract does not include (ADR 0128 §4). In a
 * standalone property it needs Finance. It is drawn because payment information
 * was asked for, and reported as a finding rather than proposed as a plan.
 */
export interface Payment {
  terms: readonly TermRow[];

  /** The sentence under the terms — why the deadlines are computed. */
  note: string;

  /** Each folio line, and the reason there is nothing behind it. */
  folio: readonly { label: string; because: string }[];

  /** The two gaps, and why they need two different answers. */
  folioNote: string;
}

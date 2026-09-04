/**
 * What the Teams screen is given — a group of people, and who is in it today.
 *
 * # A team is people, and that is why it is Workforce's
 *
 * Ruled 2026-09-04 on Jobs' `S3-D1`. The distinction worth carrying into the
 * view types: a **zone** is where the work is — Master Data's, a place, already
 * on the posting — and a **team** is who does it together. A property that
 * organises by area assigns to zones; one that organises by crew assigns to
 * teams, and the posting is where they meet.
 *
 * # Every member is a borrowed name
 *
 * Workforce holds a staff id and nothing about the person. A name here was read
 * from Master Data at the moment the answer was composed and is kept nowhere —
 * *serving is not storing* — which is why a member carries a `name` that may be
 * absent rather than one that is guaranteed.
 */

/** One team, as the list draws it. */
export interface Team {
  id: string;

  name: string;

  /** The canon department code — ADR 0119. Exactly one, by ruling. */
  department: string;

  /**
   * What that code is called, as the property reads it.
   *
   * Carried beside the code rather than looked up in the screen: the canon list
   * is Master Data's, and a module holding its own copy of it is the second
   * spelling of Housekeeping that eventually disagrees.
   */
  departmentName: string;

  /** What the property wrote under the name, or nothing. */
  note: string | null;

  /** Live members on the day being viewed. */
  members: number;

  /** When it was formed, spelled as a person reads it. */
  formed: string;

  /** Whether it is offered when work is assigned — ADR 0062's flag. */
  active: boolean;
}

/** One person in a team, on the day being viewed. */
export interface Member {
  /** Master Data's person — this module's only handle on them. */
  staffId: string;

  /**
   * What a name badge shows, or null when Master Data did not answer.
   *
   * **Never filled in.** "We could not find this person" and "this person has
   * no name" are different facts, and neither of them is *Unknown*.
   */
  name: string | null;

  /** The initials the avatar draws — derived from the name, absent without one. */
  initials: string;

  /** When they joined, as the row shows it. */
  since: string;
}

/** Somebody the picker may offer, and whether it may. */
export interface Candidate {
  staffId: string;
  name: string;
  role: string;

  /** Their department, which is the reason the third one is refused. */
  department: string;

  /**
   * Why they cannot be added, or null when they can.
   *
   * Carried rather than computed in the dialog: the refusal is the service's,
   * and a screen that worked out its own version would eventually disagree with
   * the one the save produces.
   */
  refused: string | null;
}

/** A team, opened. */
export interface TeamDetail {
  team: Team;

  /** The day "members" is being asked about. */
  on: string;

  members: readonly Member[];

  /** Who the Add-a-member dialog offers, refusals included. */
  candidates: readonly Candidate[];
}

/** The screen. */
export interface Teams {
  /** The property, as the header names it. */
  property: string;

  /** Every team, active first — the order the service returns. */
  teams: readonly Team[];

  /** The day the counts are for. */
  on: string;

  /**
   * The team whose roll the answer carries, when it carries one.
   *
   * **Selecting a team is a screen state; having its roll is an answer.** The
   * two are separate because they fail separately — the list arrives and the
   * detail may not — and folding them into one field made frame 1 unreachable:
   * a fixture with a team open drew the split every time, and the plain list
   * had no state that could produce it.
   */
  detail: TeamDetail | null;
}

/** A membership a posting is holding open — what frame 6's panel lists. */
export interface Supported {
  team: string;
  department: string;
  since: string;
}

/** What ending a posting is about to do, as the dialog states it. */
export interface PostingEnding {
  who: string;
  department: string;

  /** The last day, as the field shows it. */
  lastDay: string;

  /**
   * The memberships that close with it.
   *
   * **Read from the service, never predicted here.** It is the same query the
   * write makes, which is the point: a screen with its own version of the rule
   * would eventually be the one a person read and the wrong one.
   */
  alsoEnds: readonly Supported[];
}

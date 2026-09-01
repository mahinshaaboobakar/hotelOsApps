/**
 * The reservation book, as this module can reach it.
 *
 * # One seam, and it is deliberately the only one
 *
 * Every screen reads through `load`. Nothing else in this module touches
 * `host.call`, so the day the desktop grows a GuestOps client there is exactly
 * one file to change and the screens do not know it happened.
 *
 * # Why the recorded facts are here rather than in a test
 *
 * **The desktop has no GuestOps gRPC client.** The bridge's host side answers a
 * capability by dispatching to a client the shell holds, and there is none for
 * this application — the same gap BB reported for integration. So a call for
 * real data fails `unavailable` today, and will succeed unchanged the moment
 * that client lands.
 *
 * These facts are the ones the backend's own suite already asserts on — the
 * same stays, the same disagreement, the same candidate — so the screens are
 * driven by something true rather than by a designer's placeholder, and the
 * switch is the absence of a fallback rather than a rewrite. It is the pattern
 * the inbound consumer and the Jobs reply already use: build against recorded
 * facts, and the flip is one line on a tested surface.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

/** Where a fact came from — the four marks the whole application carries. */
export type Mark = "pms" | "override" | "disagrees" | "missing";

/** One room-stay, as a screen needs it. */
export interface Stay {
  id: string;
  guest: string;
  room: string | null;
  roomType: string;
  arrival: string | null;
  departure: string | null;
  lifecycle: string;
  marks: readonly Mark[];
}

/** The day, as the front desk reads it. */
export interface Today {
  businessDate: string;
  rollsAt: string;
  connected: boolean;
  arrivals: readonly Stay[];
  inHouse: readonly Stay[];
  departures: readonly Stay[];
}

/** Something a person must resolve — a disagreement or a candidate link. */
export interface Attention {
  id: string;
  kind: "disagreement" | "candidate";
  stay: string;
  ours: string;
  theirs: string;
  detail: string;
}

/** What a screen got, and whether it is the property's own data. */
export interface Loaded<T> {
  value: T;
  /**
   * True when this came from the platform.
   *
   * Screens render it. A person looking at a stay must be able to tell whether
   * they are seeing their hotel or a stand-in, and a module that hid the
   * difference would be one somebody eventually acts on.
   */
  live: boolean;
  /** Why it is not live, when it is not — shown only if ADR 0041 permits. */
  because: string | null;
}

/**
 * Ask the platform, and fall back to the recorded facts.
 *
 * @param host the bridge, and the only route out of this realm
 * @param capability the permission the manifest requested
 * @param method the operation within it
 * @param recorded what to show when the platform cannot answer
 * @returns the value, and whether it is real
 */
export async function load<T>(
  host: HostApi,
  capability: string,
  method: string,
  recorded: T,
): Promise<Loaded<T>> {
  // Asking for a capability that was not granted is not worth a round trip, and
  // the refusal would read as an outage rather than as a permission a property
  // chose not to give.
  if (!host.identity.capabilities.includes(capability)) {
    return { value: recorded, live: false, because: null };
  }

  try {
    return { value: (await host.call(capability, method)) as T, live: true, because: null };
  } catch (error) {
    if (error instanceof HostCallError) {
      // ADR 0041, asked by the SDK so a package does not rediscover the rule:
      // `internal` and `forbidden` carry a message for a log, and putting one
      // on a hotel's screen leaks a platform diagnostic to a receptionist.
      return { value: recorded, live: false, because: error.isForPeople ? error.message : null };
    }

    throw error;
  }
}

/**
 * The day, as the backend's own tests record it.
 *
 * A standalone property — the book itself — so nothing carries a `pms` mark and
 * the unassigned arrival is the one the desk must act on.
 */
export const recordedToday: Today = {
  businessDate: "Tue 1 Sep",
  rollsAt: "04:00",
  connected: false,
  arrivals: [
    {
      id: "a1",
      guest: "Rajesh Pillai",
      room: null,
      roomType: "Deluxe King",
      arrival: "1 Sep",
      departure: "4 Sep",
      lifecycle: "Booked",
      marks: ["missing"],
    },
    {
      id: "a2",
      guest: "Anna Varghese",
      room: "412",
      roomType: "Deluxe Twin",
      arrival: "1 Sep",
      departure: "3 Sep",
      lifecycle: "Booked",
      marks: [],
    },
  ],
  inHouse: [
    {
      id: "h1",
      guest: "Joseph Mathew",
      room: "318",
      roomType: "Deluxe King",
      arrival: "31 Aug",
      departure: "2 Sep",
      lifecycle: "In house",
      marks: [],
    },
  ],
  departures: [
    {
      id: "d1",
      guest: "Meera Nair",
      room: "205",
      roomType: "Standard",
      arrival: "29 Aug",
      departure: "1 Sep",
      lifecycle: "In house",
      marks: [],
    },
  ],
};

/**
 * The two flows a person finishes, as slice 2's suite records them.
 *
 * One of each on purpose: a disagreement where the desk and the PMS differ, and
 * a candidate where a stay this property created may be the same stay the PMS
 * has just sent.
 */
export const recordedAttention: readonly Attention[] = [
  {
    id: "t1",
    kind: "disagreement",
    stay: "318 · Joseph Mathew",
    ours: "In house",
    theirs: "Departed",
    detail:
      "The desk checked this guest in at 11:04. Opera says they left. The stay keeps the desk's value until somebody clears it — one truth still leaves the application.",
  },
  {
    id: "t2",
    kind: "candidate",
    stay: "412 · Anna Varghese",
    ours: "Created here, 1 Sep – 3 Sep",
    theirs: "Opera 84119377, 1 Sep – 3 Sep",
    detail:
      "Same room, overlapping dates. The names rank the list and never link it: staff confirm, or reject and the room is honestly double-booked.",
  },
];

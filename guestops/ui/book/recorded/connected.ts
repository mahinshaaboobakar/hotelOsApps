/**
 * The same day in a PMS-connected property, with the feed late. Frame 11.
 */

import type { Today } from "../model";
import { recordedToday } from "./day";

/**
 * Frame 11 — and the point is how little of it differs.
 *
 * **The fork was removed, not chosen** (S36, GUEST-Q4). The property is
 * PMS-writes-first at all times, so an override means one thing in every
 * condition and the screen always says the same true thing: *your action
 * stands*. A mode switch keyed on connector health would flip the desk's
 * meaning mid-shift on a false trigger — and connector health is exactly the
 * signal R27 proved unreliable, because a connector can be authenticated,
 * polling and green while check-ins specifically have stopped.
 *
 * **So the outage is a staleness banner, per capability, and it gates
 * nothing.** Everything else on this screen is the same screen with different
 * rows, which is why this fixture is built *from* frame 1's rather than written
 * out again: two transcriptions of one screen would let the two modes drift
 * apart in the fixture and prove a difference the product does not have.
 */
export const recordedTodayConnected: Today = {
  ...recordedToday,

  connected: true,

  stale: {
    headline: "PMS feed silent since 09:00 — your entries stand.",
    detail: "Check-ins have not arrived for 5 h 12 m. Bookings and room status "
      + "are still arriving.",
  },

  // The counts carry what the desk has to act on, which the plain figures do
  // not: six arrivals with no room is work, and an outage batch is a group of
  // held facts rather than six unrelated ones.
  stats: [
    { value: "14", label: "Arrivals · 6 unassigned" },
    { value: "42", label: "In house" },
    { value: "11", label: "Departures" },
    { value: "2", label: "Attention · 1 outage batch" },
  ],

  lists: recordedToday.lists.map((list) =>
    list.label !== "Arrivals" ? list : { ...list, rows: rows() }),
};

/**
 * The four rows frame 11 draws, and each one is a different provenance.
 *
 * **Three marks, three different facts**: `walk-in` is how the guest arrived,
 * `Opera doesn't know` is who knows about them, `from Opera` is who established
 * the value. Meera's row is the fourth case — agreement arriving late, which
 * settles **silently as confirmed** and is not work. Twenty such rows would be
 * twenty non-events; only *differing* values become a disagreement.
 */
function rows(): Today["lists"][number]["rows"] {
  return [
    {
      id: "c1", guest: "Rajesh Pillai", contact: null, party: null, unnamed: false,
      booking: "BK-4471 · 1 of 3", roomType: "Deluxe King", room: "214",
      nights: "31 Aug → 4 Sep",
      chips: [
        { kind: "mark", tone: "override", text: "override" },
        { kind: "mark", tone: "disagrees", text: "disagrees" },
      ],
    },
    {
      id: "c2", guest: "Meera Krishnan", contact: null, party: null, unnamed: false,
      booking: "BK-4482", roomType: "Executive Suite", room: "506",
      nights: "31 Aug → 1 Sep",
      chips: [
        { kind: "mark", tone: "override", text: "override" },
        { kind: "pill", tone: "ok", text: "confirmed 15:44" },
      ],
    },
    {
      id: "c3", guest: "Joseph Mathew", contact: null, party: null, unnamed: false,
      booking: "created here", roomType: "Deluxe Twin", room: "308",
      nights: "31 Aug → 1 Sep",
      chips: [
        { kind: "mark", tone: "walkin", text: "walk-in" },
        { kind: "mark", tone: "unknown", text: "Opera doesn't know" },
      ],
    },
    {
      id: "c4", guest: "Daniel Fernandes", contact: null, party: null, unnamed: false,
      booking: "BK-4488", roomType: "Deluxe Twin", room: "309",
      nights: "31 Aug · day use",
      chips: [{ kind: "mark", tone: "pms", text: "from Opera" }],
    },
  ];
}

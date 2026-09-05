/**
 * The bookings list, and one booking being cancelled — frames 2 and 8.
 */

import type { Bookings, BookingDetail, CancelPlan } from "../model";

/**
 * Frame 2's list — nine rows, and every state the design says stays visible.
 *
 * **Cancelled and no-show rows are here on purpose** (S25, S27, ADR 0062): a
 * cancelled reservation exists, its penalty may be chargeable, and a no-show
 * is reportable. Neither is a deletion, so neither leaves the list.
 *
 * The contacts the frame draws are **absent** — GUEST-Q12. Nothing in this
 * process can mask a value it cannot decrypt, and a fixture that showed one
 * would make the capture of this screen claim a column the build does not have.
 */
export const recordedBookings: Bookings = {
  search: "",

  filters: [
    { key: "when", choices: [{ label: "Arriving · next 30 days", on: true }] },
    { key: "status", choices: [{ label: "Any status", on: true }] },
    { key: "source", choices: [{ label: "Any source", on: true }] },
  ],

  total: 218,

  rows: [
    {
      id: "b1", guest: "Rajesh Pillai", contact: null, unnamed: false,
      reference: "BK-4471", createdHere: false, confirmation: "84119377",
      // The group said out loud. Three rooms claimed, one sent — and the two
      // unsent stays are not rows here or anywhere (GUEST-Q2, frame 9).
      rooms: "1 of 3 known", dates: "31 Aug → 2 Sep",
      status: "In house", statusTone: "ok",
      chips: [{ mark: "pms", text: "Opera" }, { mark: "disagrees", text: "disagrees" }],
    },
    {
      id: "b2", guest: "Meera Krishnan", contact: null, unnamed: false,
      reference: "BK-4482", createdHere: false, confirmation: null,
      rooms: "1", dates: "31 Aug → 1 Sep",
      status: "In house", statusTone: "ok",
      chips: [{ mark: "pms", text: "Opera" }, { mark: "override", text: "override" }],
    },
    {
      id: "b3", guest: "Joseph Mathew", contact: null, unnamed: false,
      reference: "created here", createdHere: true, confirmation: null,
      rooms: "1", dates: "31 Aug → 1 Sep",
      status: "In house", statusTone: "ok",
      chips: [
        { mark: "walkin", text: "walk-in" },
        { mark: "unknown", text: "Opera doesn't know" },
      ],
    },
    {
      id: "b4", guest: "Anand Varma", contact: null, unnamed: false,
      reference: "BK-4455", createdHere: false, confirmation: null,
      rooms: "1", dates: "30 Aug → 2 Sep",
      status: "In house", statusTone: "ok",
      chips: [{ mark: "disagrees", text: "Opera says cancelled" }],
    },
    {
      id: "b5", guest: "Fatima Sheikh", contact: null, unnamed: false,
      reference: "BK-4506", createdHere: false, confirmation: null,
      rooms: "2", dates: "3 Sep → 7 Sep",
      status: "Booked", statusTone: "neutral",
      chips: [
        { mark: "pms", text: "Opera" },
        { mark: "missing", text: "no rooms assigned" },
      ],
    },
    {
      id: "b6", guest: "Aisha Rahman", contact: null, unnamed: false,
      reference: "BK-4511", createdHere: false, confirmation: null,
      rooms: "1", dates: "24 Dec → 27 Dec",
      // A first-class state, shown as one — GUEST-Q9. It holds no room, so
      // counting it against inventory would make a full hotel look oversold.
      status: "Waitlisted", statusTone: "warn",
      chips: [{ mark: "pms", text: "Opera" }, { mark: "missing", text: "holds no room" }],
    },
    {
      id: "b7", guest: "Vikram Nair", contact: null, unnamed: false,
      reference: "BK-4390", createdHere: false, confirmation: null,
      rooms: "1", dates: "24 Aug → 27 Aug",
      status: "Departed", statusTone: "neutral",
      chips: [{ mark: "pms", text: "Opera" }],
    },
    {
      id: "b8", guest: "Priya Ramesh", contact: null, unnamed: false,
      reference: "BK-4372", createdHere: false, confirmation: null,
      rooms: "1", dates: "22 Aug → 24 Aug",
      status: "Cancelled", statusTone: "bad",
      chips: [{ mark: "pms", text: "Opera" }, { mark: "note", text: "penalty applied" }],
    },
    {
      id: "b9", guest: "Thomas George", contact: null, unnamed: false,
      reference: "BK-4361", createdHere: false, confirmation: null,
      rooms: "1", dates: "19 Aug → 20 Aug",
      status: "No-show", statusTone: "bad",
      chips: [{ mark: "pms", text: "Opera" }],
    },
  ],
};

/** Frame 8's booking — two stays, one of them not yet named. */
export const recordedBooking: BookingDetail = {
  id: "b5",
  guest: "Fatima Sheikh",
  reference: "BK-4506",
  summary: "Two stays · 3 Sep → 7 Sep",
  managedBy: "Opera manages this booking",
  incomplete: null,
  incompleteDetail: null,
  elsewhere: null,
  facts: [],

  stays: [
    {
      id: "s1", guest: "Fatima Sheikh", unnamed: false, stayId: "01J9M…22B1",
      roomType: "Executive Suite", room: null, dates: "3 Sep → 7 Sep",
      status: "Booked", statusTone: "neutral", chips: [],
    },
    {
      id: "s2", guest: "Not yet named", unnamed: true, stayId: "01J9M…22B2",
      roomType: "Deluxe King", room: null, dates: "3 Sep → 7 Sep",
      status: "Booked", statusTone: "neutral",
      chips: [{ mark: "missing", text: "party unnamed" }],
    },
  ],
};

/**
 * Frame 8's dialog — the object, the consequence and the limit.
 *
 * **The penalty is computed and recorded, never charged** (GUEST-Q6, R18):
 * charging is Finance's, a later round. And the limit is the sentence that
 * must not be omitted — nothing GuestOps records reaches the PMS in v1
 * (CONN-Q5, ADR 0128 §4).
 */
export const recordedCancelPlan: CancelPlan = {
  subject: "BK-4506 · Fatima Sheikh · two stays, 3 – 7 September",
  stays: 2,

  consequence:
    "This cancels two stays, one at a time. A booking is a group and every "
    + "operation happens to a stay — so this records two cancellations, and "
    + "either can be reinstated separately afterwards.",

  rows: [
    {
      label: "Executive Suite", value: "3 – 7 Sep · ", strong: "penalty ₹ 12 000.00",
      tags: [{ kind: "lock", tone: "neutral", text: "GROSS · 1 NIGHT" }],
    },
    {
      label: "Deluxe King", value: "3 – 7 Sep · ", strong: "penalty ₹ 8 400.00",
      tags: [{ kind: "lock", tone: "neutral", text: "GROSS · 1 NIGHT" }],
    },
    {
      label: "Why a penalty",
      value: "cancelling within 48 h of arrival, per the booking's terms",
      tags: [],
    },
    {
      label: "Afterwards",
      value: "both rooms return to inventory for 3 – 7 September",
      tags: [],
    },
  ],

  notTold:
    "Opera will not be told. This records the cancellation in HotelOS only — "
    + "it does not reach the PMS, and Opera will keep showing this booking as "
    + "live until somebody cancels it there too.",

  reasons: ["Guest cancelled — flight changed"],
};

/**
 * Frame 9's booking — three rooms claimed, one sent.
 *
 * **The frame that will be wrong first if it is drawn dishonestly.** The
 * reference system met this exact source shape — three rooms in a header, one
 * room in the payload — and minted sibling identifiers by string concatenation
 * that always produced `-1` (R9). The honest form is a sentence and a number.
 */
export const recordedGroup: BookingDetail = {
  id: "b1",
  guest: "Rajesh Pillai",
  reference: "BK-4471",
  summary: "Group 84119377 · from Opera · booked 28 Aug",
  managedBy: null,

  incomplete:
    "1 of 3 rooms known. Opera says this booking is for three rooms and has "
    + "sent one. The other two have not arrived — they may arrive in minutes, "
    + "tomorrow, or never.",

  incompleteDetail:
    "Two rooms expected and not received. They are described here in words and "
    + "nowhere else: no empty rows, no “TBA” guests, and they count towards "
    + "nothing — not occupancy, not the arrivals figure, not the group's "
    + "status. A group has no single arrival state; what you see above is per "
    + "stay, and any summary is a count.",

  elsewhere:
    "This booking continues elsewhere. Group 84119377 also has legs at Avenue "
    + "Regent, Kochi. Those stays live in that property's GuestOps — this "
    + "installation holds only its own, and asks no one for the rest.",

  facts: [
    {
      title: "Partial check-in",
      key: "BK-4402",
      value: "2 of 3 arrived",
      hint: "Departure is per stay too — two rooms leave Thursday, one stays "
        + "until Saturday. There is no such thing as checking out a group.",
    },
    {
      title: "Group identifier",
      key: "Carried",
      value: "from day one",
      hint: "Carried from the first fact so a chain-level journey needs no "
        + "migration later — and no cross-installation query is built now.",
    },
    {
      title: "Expected count",
      key: "Opera says",
      value: "3 · one known",
      hint: "“Three expected, one known” and “one known, expectation "
        + "unstated” are different states, and the model keeps them apart.",
    },
  ],

  stays: [
    {
      id: "g1", guest: "Rajesh Pillai", unnamed: false, stayId: "01J9K…7F3A",
      roomType: "Deluxe King", room: "214", dates: "31 Aug → 4 Sep",
      status: "In house", statusTone: "ok",
      chips: [
        { mark: "override", text: "override" },
        { mark: "disagrees", text: "disagrees" },
      ],
    },
  ],
};

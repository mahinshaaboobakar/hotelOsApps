/**
 * The facts the screens draw when the platform cannot be reached.
 *
 * **These are the approved frames' own data**, transcribed — the same guests,
 * rooms, references and times the gold mockup draws. That is deliberate: the
 * screens are being built to those frames, and a fixture invented separately
 * would make every capture a comparison against something nobody approved.
 *
 * They live behind `load()` in `index.ts`, so a screen never chooses between
 * live and recorded — it asks once and is told which it got.
 */

import type { AttentionCard, StayPage, Today } from "./model";

/** The honest list — gold frame 12, all four kinds. */
export const recordedAttention: readonly AttentionCard[] = [
  {
    id: "a1",
    kind: "Same stay, or two?",
    status: { mark: "unknown", text: "Opera doesn't know" },
    rows: [
      {
        label: "You created", value: "", strong: "Joseph Mathew",
        tail: " · room 308 · 31 Aug → 1 Sep · walk-in at 11:04", tags: [],
      },
      {
        label: "Opera now sends", value: "", strong: "Joseph K Mathew",
        tail: " · room 308 · 31 Aug → 1 Sep · reservation 84119512", tags: [],
      },
      {
        label: "Why it is here", value: "",
        tags: [
          { kind: "pill", tone: "neutral", text: "same room" },
          { kind: "pill", tone: "neutral", text: "overlapping dates" },
          { kind: "pill", tone: "warn", text: "names look alike" },
        ],
      },
    ],
    note:
      "Same room and overlapping dates is what raised this. The names only ordered the list — they "
      + "can never join two stays. Until you decide, Opera's version is held and applied to nothing.",
    hint: null,
    actions: ["Same stay", "Two different stays"],
  },
  {
    id: "a2",
    kind: "Opera disagrees · 1 stay",
    status: "from the outage batch",
    rows: [
      {
        label: "Rajesh Pillai", value: "room — you: ", strong: "214", tail: " · Opera: 208",
        tags: [{ kind: "mark", tone: "disagrees", text: "standing" }],
      },
    ],
    note: null,
    hint:
      "14 facts arrived when the feed returned at 14:12. 13 matched what you had and settled "
      + "silently. This is the one that differs.",
    actions: ["Open the stay", "Keep 214 for all"],
  },
  {
    id: "a3",
    kind: "Opera says cancelled · the guest is in the room",
    status: null,
    rows: [
      {
        label: "Anand Varma", value: "in house since 30 Aug · room 411 · Opera sent ",
        strong: "cancelled", tail: " at 03:40", tags: [],
      },
    ],
    note:
      "A cancellation cannot move a stay that is already in house, so it was recorded and not "
      + "applied — the guest stays served and the room stays occupied. Someone has to look, because "
      + "the two records cannot both be right.",
    hint: null,
    actions: ["Open the stay", "Mark resolved"],
  },
  {
    id: "a4",
    kind: "Incomplete group",
    status: null,
    rows: [
      { label: "BK-4471", value: "1 of 3 rooms known · unchanged for 3 days", tags: [] },
    ],
    note: null,
    hint:
      "Not an error — Opera sends papers one at a time, and two may never come. It sits here so "
      + "nobody discovers it at the desk on arrival day.",
    actions: [],
  },
];

/** The front desk day — gold frame 1, a standalone property. */
export const recordedToday: Today = {
  businessDate: "Tue 31 Aug",
  rollsAt: "04:00",
  connected: false,
  stats: [
    { value: "14", label: "Arrivals · 6 unassigned" },
    { value: "42", label: "In house" },
    { value: "11", label: "Departures · 3 gone" },
    // Four, not gold frame 1's two. **The two approved frames disagree**:
    // frame 1's rail and strip say 2, frame 12's rail says 4 and its subtitle
    // says "Four things". They are two moments of one day in a mockup, but one
    // running screen cannot hold both — a strip reading 2 above a rail reading
    // 4 is incoherent to the person at the desk. The count is derived from the
    // attention list itself, so it cannot drift again. Reported as a proposed
    // mockup amendment rather than resolved by picking a number.
    { value: String(recordedAttention.length), label: "Attention" },
  ],
  lists: [
    {
      key: "arrivals",
      label: "Arrivals",
      count: "14",
      rows: [
        {
          id: "r1", guest: "Rajesh Pillai", contact: "+91 98470 •••• 12", unnamed: false,
          booking: "BK-4471 · 1 of 3", roomType: "Deluxe King", room: "214",
          nights: "31 Aug → 2 Sep",
          chips: [{ mark: "missing", text: "no ID captured" }],
        },
        {
          id: "r2", guest: "Not yet named", contact: "party of 2", unnamed: true,
          booking: "BK-4471 · 1 of 3", roomType: "Deluxe King", room: null,
          nights: "31 Aug → 2 Sep",
          chips: [
            { mark: "missing", text: "party unnamed" },
            { mark: "missing", text: "no room" },
          ],
        },
        {
          id: "r3", guest: "Meera Krishnan", contact: "meera.k@•••••.com", unnamed: false,
          booking: "BK-4482", roomType: "Executive Suite", room: null,
          nights: "31 Aug → 1 Sep",
          chips: [{ mark: "missing", text: "no room" }],
        },
        {
          id: "r4", guest: "Daniel Fernandes", contact: "+44 7700 •••• 09", unnamed: false,
          booking: "BK-4488", roomType: "Deluxe Twin", room: "309",
          nights: "31 Aug · day use",
          chips: [{ mark: "dayuse", text: "day use · out 18:00" }],
        },
        {
          id: "r5", guest: "Sunita & Arvind Rao", contact: "+91 99000 •••• 41", unnamed: false,
          booking: "BK-4490", roomType: "Deluxe King", room: "402",
          nights: "31 Aug → 4 Sep", chips: [],
        },
      ],
    },
    {
      key: "inhouse", label: "In house", count: "42",
      rows: [
        {
          id: "h1", guest: "Joseph Mathew", contact: "+91 98221 •••• 76", unnamed: false,
          booking: "BK-4455", roomType: "Deluxe King", room: "318",
          nights: "30 Aug → 2 Sep",
          chips: [{ mark: "disagrees", text: "Opera disagrees" }],
        },
      ],
    },
    {
      key: "departures", label: "Departures", count: "11",
      rows: [
        {
          id: "d1", guest: "Meera Nair", contact: "+91 98470 •••• 55", unnamed: false,
          booking: "BK-4440", roomType: "Standard", room: "205",
          nights: "29 Aug → 31 Aug", chips: [],
        },
      ],
    },
    { key: "attention", label: "Attention", count: String(recordedAttention.length), rows: [] },
  ],
};

/** The stay page — gold frame 3, PMS-connected with an override standing. */
export const recordedStay: StayPage = {
  id: "r1",
  guest: "Rajesh Pillai",
  room: "214",
  stayId: "01J9K…7F3A",
  bookingRef: "BK-4471",
  managedBy: "Opera manages this stay",
  actions: [
    { label: "Move room", danger: false },
    { label: "Check out", danger: false },
    { label: "Cancel…", danger: true },
  ],
  tabs: [
    { label: "Overview" },
    { label: "Activity", count: "14" },
    { label: "Requests & jobs", count: "2" },
    { label: "Servicing", count: "4 nights" },
    { label: "Payment" },
    { label: "Documents" },
  ],
  banner: {
    headline: "Opera disagrees about the room.",
    detail: "You have 214 · Opera says 208 — recorded 15:30, not applied.",
    attribution:
      "Your entry stands everywhere until someone decides: override by Anitha Menon at 14:10, "
      + "when Opera said due in, no room.",
    actions: ["Keep 214", "Take Opera's 208"],
  },
  standing: "override standing",
  rows: [
    {
      label: "Room", value: "", strong: "214", tail: " · Deluxe King",
      tags: [
        { kind: "mark", tone: "override", text: "yours" },
        { kind: "mark", tone: "disagrees", text: "Opera: 208" },
      ],
    },
    {
      label: "Arrival", value: "31 Aug ", strong: "14:10",
      tags: [
        { kind: "lock", tone: "neutral", text: "OBSERVED" },
        { kind: "mark", tone: "override", text: "override" },
      ],
    },
    {
      label: "Departure", value: "4 Sep ", strong: "11:00",
      tags: [{ kind: "lock", tone: "neutral", text: "DERIVED FROM PROPERTY CLOCK" }],
    },
    {
      label: "Room type", value: "Deluxe King",
      tags: [{ kind: "lock", tone: "neutral", text: "FROM OPERA" }],
    },
    {
      label: "Party", value: "Rajesh Pillai",
      tags: [
        { kind: "pill", tone: "neutral", text: "primary" },
        { kind: "text", tone: "neutral", text: "· Lakshmi Pillai" },
        { kind: "link", tone: "neutral", text: "＋ add" },
      ],
    },
    {
      label: "Contact", value: "+91 98470 •••• 12",
      tags: [
        { kind: "link", tone: "neutral", text: "reveal" },
        { kind: "lock", tone: "neutral", text: "MOBILE · PRIMARY" },
        { kind: "text", tone: "neutral", text: "· rajesh.p@•••••.com" },
        { kind: "link", tone: "neutral", text: "reveal" },
      ],
    },
    {
      label: "Registration", value: "GRC 2026/08/1147 · ID captured",
      tags: [{ kind: "pill", tone: "ok", text: "complete" }],
    },
    {
      label: "Preferences", value: "High floor, away from the lift", quiet: true,
      tags: [{ kind: "lock", tone: "neutral", text: "GUEST · CARRIES TO NEXT STAY" }],
    },
    {
      label: "Note", value: "Anniversary — cake sent 31 Aug", quiet: true,
      tags: [{ kind: "lock", tone: "neutral", text: "THIS STAY ONLY" }],
    },
  ],
  timeline: [
    {
      time: "28 Aug", tone: "pms", what: "Booked in Opera",
      detail: "reservation 84119377 · 4 nights · Deluxe King",
    },
    {
      time: "14:10", tone: "override", what: "Assigned 214 · checked in",
      detail: "by Anitha Menon — override, Opera was unreachable",
    },
    {
      time: "15:30", tone: "disagrees", what: "Opera reports room 208",
      detail: "differs from yours — recorded, not applied",
    },
    {
      time: "now", tone: "none", what: "Awaiting a decision",
      detail: "Room Care, Context and the board all read 214",
    },
  ],
  consequence:
    "Taking Opera's 208 publishes the same room-changed fact a move does, so Room Care re-plans "
    + "from the event stream as always — both rooms' axes flip, and no consumer needs a special case.",
};

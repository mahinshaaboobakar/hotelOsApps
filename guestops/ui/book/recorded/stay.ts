/**
 * One stay, PMS-connected, with an override standing — frame 3.
 */

import type { StayPage } from "../model";
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

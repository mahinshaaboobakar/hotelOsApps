/**
 * What is free, and the walk-in sold at the desk — frames 14 and 10.
 */

import type { Availability, RoomConflict, WalkInDraft } from "../model";

/**
 * Frame 14's answer — and the Suite row is the one that explains the design.
 *
 * Four suites are physically fine, unsold, and **not for sale**: a manager held
 * them for a wedding party. That is stop-sell — our own setting, per room type
 * and date range, the seller's control. The Deluxe King's out-of-order room is
 * a different thing entirely: EngineeringOps says that room cannot be used, and
 * we hear it as an event. **Neither number is stored as inventory here**, which
 * is why GUEST-Q7 needed no new inventory owner.
 */
export const recordedAvailability: Availability = {
  query: {
    arrive: "3 Sep",
    depart: "7 Sep",
    arriveOn: "2026-09-03",
    departOn: "2026-09-07",
    party: "1 room · 2 adults",
  },
  mode: "Standalone — this property is the book",

  types: [
    {
      roomType: "Deluxe King", rate: "₹ 8 400 · gross",
      total: 24, sold: 19,
      outOfOrder: 1, outOfOrderBy: "EngineeringOps",
      stopSold: 0, stopSoldWhy: null,
      free: 4,
    },
    {
      roomType: "Deluxe Twin", rate: "₹ 8 400 · gross",
      total: 18, sold: 18,
      outOfOrder: 0, outOfOrderBy: null,
      stopSold: 0, stopSoldWhy: null,
      free: 0,
    },
    {
      roomType: "Executive Suite", rate: "₹ 12 000 · gross",
      total: 6, sold: 2,
      outOfOrder: 0, outOfOrderBy: null,
      stopSold: 4, stopSoldWhy: "wedding party",
      free: 0,
    },
  ],
};

/** Frame 14's conflict — it names the other stay and lets a person decide. */
export const recordedConflict: RoomConflict = {
  room: "214",
  headline: "214 already has a stay over these dates.",
  detail: "Rajesh Pillai, 31 Aug → 4 Sep. Assign anyway, or pick another room.",
};

/**
 * Frame 10's sheet — one action, because booking and arrival are one moment.
 *
 * The **walk-in flag is set when the stay is created or it is unrecoverable**
 * (S13): the walk-in ratio is a number every hotel reports on, and nothing
 * later can reconstruct it. The **room is in this sheet and not behind a later
 * step**, because check-in is the one operation that refuses to proceed without
 * one (S8).
 */
export const recordedWalkIn: WalkInDraft = {
  guest: "Joseph Mathew",
  guestNote: "new guest",

  contact: "+91 98950 44120",
  contactKind: "phone",

  roomType: "Deluxe Twin",
  room: "308",
  roomState: "vacant · clean",

  arrives: "31 Aug · now",
  departs: "1 Sep · 11:00",

  rate: "₹ 6 200.00 INR",
  rateBasis: "gross",

  consequence: "Room 308 will be marked occupied.",
};

/**
 * What is free, and the walk-in sold at the desk — frames 14 and 10.
 */

import type { Availability, FreeRooms, RoomConflict } from "../model";

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
    arrive: "2026-09-03",
    depart: "2026-09-07",
    party: "1 room · 2 adults",
  },
  mode: "Standalone — this property is the book",

  types: [
    {
      roomTypeId: "3a5f1c44-0000-4000-8000-000000000001", roomType: "Deluxe King", rate: null,
      sleeps: { included: 2, most: 3, adults: 2, children: 1, extraBed: true, extraBeds: 1 },
      total: 24, sold: 19,
      outOfOrder: 1, outOfOrderBy: null,
      stopSold: 0, stopSoldWhy: null,
      free: 4,
    },
    {
      roomTypeId: "3a5f1c44-0000-4000-8000-000000000002", roomType: "Deluxe Twin", rate: null,
      sleeps: { included: 2, most: 2, adults: 2, children: 0, extraBed: false, extraBeds: 0 },
      total: 18, sold: 18,
      outOfOrder: 0, outOfOrderBy: null,
      stopSold: 0, stopSoldWhy: null,
      free: 0,
    },
    {
      roomTypeId: "3a5f1c44-0000-4000-8000-000000000003", roomType: "Executive Suite", rate: null,
      sleeps: { included: 3, most: 5, adults: 4, children: 2, extraBed: true, extraBeds: 1 },
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
/**
 * The rooms free for the chosen type — frame 10's room chooser.
 *
 * **This replaced `recordedWalkIn`, and the shape change is the point.** That
 * fixture held rendered strings for a sheet that drew a draft and captured
 * nothing; the sheet now captures, so what a fixture must supply is what the
 * SERVICE sends — ids the write can carry, and the count that keeps an empty
 * list from meaning two things.
 */
export const recordedFreeRooms: FreeRooms = {
  rooms: [
    { id: "3a1f0c22-0000-4000-8000-000000000308", number: "308" },
    { id: "3a1f0c22-0000-4000-8000-000000000311", number: "311" },
  ],
  ofType: 6,
};

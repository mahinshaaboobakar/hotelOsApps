/**
 * The front desk day in a standalone property — frame 1.
 */

import { recordedAttention } from "./attention";
import type { Today } from "../model";
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
          id: "r1", guest: "Rajesh Pillai", contact: null, party: null, unnamed: false,
          booking: "BK-4471 · 1 of 3", roomType: "Deluxe King", room: "214",
          nights: "31 Aug → 2 Sep",
          chips: [{ mark: "missing", text: "no ID captured" }],
        },
        {
          id: "r2", guest: "Not yet named", contact: null, party: "party of 2", unnamed: true,
          booking: "BK-4471 · 1 of 3", roomType: "Deluxe King", room: null,
          nights: "31 Aug → 2 Sep",
          chips: [
            { mark: "missing", text: "party unnamed" },
            { mark: "missing", text: "no room" },
          ],
        },
        {
          id: "r3", guest: "Meera Krishnan", contact: null, party: null, unnamed: false,
          booking: "BK-4482", roomType: "Executive Suite", room: null,
          nights: "31 Aug → 1 Sep",
          chips: [{ mark: "missing", text: "no room" }],
        },
        {
          id: "r4", guest: "Daniel Fernandes", contact: null, party: null, unnamed: false,
          booking: "BK-4488", roomType: "Deluxe Twin", room: "309",
          nights: "31 Aug · day use",
          chips: [{ mark: "dayuse", text: "day use · out 18:00" }],
        },
        {
          id: "r5", guest: "Sunita & Arvind Rao", contact: null, party: null, unnamed: false,
          booking: "BK-4490", roomType: "Deluxe King", room: "402",
          nights: "31 Aug → 4 Sep", chips: [],
        },
      ],
    },
    {
      key: "inhouse", label: "In house", count: "42",
      rows: [
        {
          id: "h1", guest: "Joseph Mathew", contact: null, party: null, unnamed: false,
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
          id: "d1", guest: "Meera Nair", contact: null, party: null, unnamed: false,
          booking: "BK-4440", roomType: "Standard", room: "205",
          nights: "29 Aug → 31 Aug", chips: [],
        },
      ],
    },
    { key: "attention", label: "Attention", count: String(recordedAttention.length), rows: [] },
  ],
};

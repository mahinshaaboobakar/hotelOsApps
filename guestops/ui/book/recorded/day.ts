/**
 * The front desk day in a standalone property — frame 1.
 */

import { recordedAttention } from "./attention";
import type { Today } from "../model";
/** The front desk day — gold frame 1, a standalone property. */
export const recordedToday: Today = {
  businessDate: "2026-08-31",
  rollsAt: "2026-08-31T04:00:00+05:30",
  connected: false,
  stale: null,

  stats: [
    { value: 14, label: "Arrivals" },
    { value: 42, label: "In house" },
    { value: 11, label: "Departures" },
    // Four, not gold frame 1's two. **The two approved frames disagree**:
    // frame 1's rail and strip say 2, frame 12's rail says 4 and its subtitle
    // says "Four things". They are two moments of one day in a mockup, but one
    // running screen cannot hold both — a strip reading 2 above a rail reading
    // 4 is incoherent to the person at the desk. The count is derived from the
    // attention list itself, so it cannot drift again. Reported as a proposed
    // mockup amendment rather than resolved by picking a number.
    { value: recordedAttention.total, label: "Attention" },
  ],
  lists: [
    {
      key: "arrivals",
      label: "Arrivals",
      count: 14,
      rows: [
        {
          id: "r1", guest: "Rajesh Pillai", contact: null, party: null, unnamed: false,
          booking: "BK-4471 · 1 of 3", roomType: "Deluxe King", room: "214",
          arrive: "2026-08-31", depart: "2026-09-02",
          chips: [{ kind: "mark", tone: "missing", text: "no ID captured" }],
        },
        {
          id: "r2", guest: "Not yet named", contact: null, party: 2, unnamed: true,
          booking: "BK-4471 · 1 of 3", roomType: "Deluxe King", room: null,
          arrive: "2026-08-31", depart: "2026-09-02",
          chips: [
            { kind: "mark", tone: "missing", text: "party unnamed" },
            { kind: "mark", tone: "missing", text: "no room" },
          ],
        },
        {
          id: "r3", guest: "Meera Krishnan", contact: null, party: null, unnamed: false,
          booking: "BK-4482", roomType: "Executive Suite", room: null,
          arrive: "2026-08-31", depart: "2026-09-01",
          chips: [{ kind: "mark", tone: "missing", text: "no room" }],
        },
        {
          id: "r4", guest: "Daniel Fernandes", contact: null, party: null, unnamed: false,
          booking: "BK-4488", roomType: "Deluxe Twin", room: "309",
          arrive: "2026-08-31", depart: "2026-08-31",
          chips: [{ kind: "mark", tone: "dayuse", text: "day use · out 18:00" }],
        },
        {
          id: "r5", guest: "Sunita & Arvind Rao", contact: null, party: null, unnamed: false,
          booking: "BK-4490", roomType: "Deluxe King", room: "402",
          arrive: "2026-08-31", depart: "2026-09-04", chips: [],
        },

        // **Rows 6 to 14, added 2026-09-05.** The fixture carried five of the
        // frame's fourteen, so the built pane showed five rows under a strip
        // and a tab both reading 14 — a list that stops without saying so,
        // beside a drawing that does not. The 17x17 could not see it: it
        // compares computed properties on the first matching element, and five
        // rows and fourteen rows have identical padding. Only the side-by-side
        // showed it, which is the whole argument for the side-by-side.
        {
          id: "r6", guest: "Not yet named", contact: null, party: 3,
          unnamed: true,
          booking: "BK-4471 · 3 of 3", roomType: "Deluxe King", room: "216",
          arrive: "2026-08-31", depart: "2026-09-02",
          chips: [{ kind: "mark", tone: "missing", text: "party unnamed" }],
        },
        {
          id: "r7", guest: "Anjali Nair", contact: null, party: null, unnamed: false,
          booking: "BK-4495", roomType: "Deluxe Twin", room: "118",
          arrive: "2026-08-31", depart: "2026-09-03", chips: [],
        },
        {
          id: "r8", guest: "Thomas Weber", contact: null, party: null, unnamed: false,
          booking: "BK-4497", roomType: "Executive Suite", room: null,
          arrive: "2026-08-31", depart: "2026-09-05",
          chips: [{ kind: "mark", tone: "missing", text: "no room" }],
        },
        {
          id: "r9", guest: "Priya Menon", contact: null, party: null, unnamed: false,
          booking: "BK-4501", roomType: "Deluxe King", room: "221",
          arrive: "2026-08-31", depart: "2026-09-01", chips: [],
        },
        {
          id: "r10", guest: "Yusuf Al-Amri", contact: null, party: null, unnamed: false,
          booking: "BK-4503", roomType: "Deluxe King", room: null,
          arrive: "2026-08-31", depart: "2026-09-06",
          chips: [{ kind: "mark", tone: "missing", text: "no room" }],
        },
        {
          id: "r11", guest: "Grace Okonkwo", contact: null, party: null, unnamed: false,
          booking: "BK-4506", roomType: "Deluxe Twin", room: "305",
          arrive: "2026-08-31", depart: "2026-09-02", chips: [],
        },
        {
          id: "r12", guest: "Vikram Desai", contact: null, party: null, unnamed: false,
          booking: "BK-4509", roomType: "Deluxe King", room: null,
          arrive: "2026-08-31", depart: "2026-09-04",
          chips: [
            { kind: "mark", tone: "missing", text: "no room" },
            { kind: "mark", tone: "missing", text: "no ID captured" },
          ],
        },
        {
          id: "r13", guest: "Lena Vasquez", contact: null, party: null, unnamed: false,
          booking: "BK-4512", roomType: "Executive Suite", room: "501",
          arrive: "2026-08-31", depart: "2026-09-03", chips: [],
        },
        {
          id: "r14", guest: "Hiroshi Tanaka", contact: null, party: null, unnamed: false,
          booking: "BK-4515", roomType: "Deluxe Twin", room: null,
          arrive: "2026-08-31", depart: "2026-09-02",
          chips: [{ kind: "mark", tone: "missing", text: "no room" }],
        },
      ],
    },
    {
      key: "inhouse", label: "In house", count: 42,
      rows: [
        {
          id: "h1", guest: "Joseph Mathew", contact: null, party: null, unnamed: false,
          booking: "BK-4455", roomType: "Deluxe King", room: "318",
          arrive: "2026-08-30", depart: "2026-09-02",
          chips: [{ kind: "mark", tone: "disagrees", text: "PMS disagrees" }],
        },
      ],
    },
    {
      key: "departures", label: "Departures", count: 11,
      rows: [
        {
          id: "d1", guest: "Meera Nair", contact: null, party: null, unnamed: false,
          booking: "BK-4440", roomType: "Standard", room: "205",
          arrive: "2026-08-29", depart: "2026-08-31", chips: [],
        },
      ],
    },
    { key: "attention", label: "Attention", count: recordedAttention.total, rows: [] },
  ],
};

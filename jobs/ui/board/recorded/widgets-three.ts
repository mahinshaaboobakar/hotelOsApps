/**
 * The approved examples of the three widgets whose frames were amended — By
 * Priority, Due Soon and Raised Today, owner-approved 2026-09-08.
 *
 * The figures are the frames' own, so a capture taken beside a drawing compares
 * the same property's morning rather than two different ones. Where a frame was
 * corrected, these follow the **corrected** drawing: Due Soon's two groups read
 * overdue first, which is what the headings say after `16d1d263`.
 */

import type { DueNow, PriorityNow, RaisedNow } from "../model";

export const recordedPriorityNow: PriorityNow = {
  p1: 1,
  p2: 5,
  p3: 14,
  notTriaged: 4,
  pressing: [
    { id: "j2210", number: "JOB-2210", what: "Water leak · 507", priority: "P1", raised: "guest" },
    { id: "j2214", number: "JOB-2214", what: "AC not cooling · 214", priority: "P2", raised: "guest" },
    { id: "j2216", number: "JOB-2216", what: "Ready for 14:00 arrival · 302", priority: "P2", raised: "flow" },
  ],
};

export const recordedDueNow: DueNow = {
  overdue: 2,
  dueWithinTwoHours: 2,
  late: [
    { id: "j2210", number: "JOB-2210", what: "Water leak · 507", allowance: "SLA 30m", mark: "+12m", tone: "bad" },
    { id: "j2214", number: "JOB-2214", what: "AC not cooling · 214", allowance: "SLA 45m", mark: "+3m", tone: "bad" },
  ],
  soon: [
    { id: "j2302", number: "JOB-2302", what: "302 · ready before arrival", allowance: "SLA 2h", mark: "in 1h", tone: "warn" },
    { id: "j2415", number: "JOB-2415", what: "415 · ready before arrival", allowance: "SLA 2h", mark: "in 2h", tone: "warn" },
  ],
};

export const recordedRaisedNow: RaisedNow = {
  raised: 24,
  closed: 19,
  byCategory: [
    { name: "Air conditioning", department: "ENG", count: 9 },
    { name: "Housekeeping", department: "HK", count: 8 },
    { name: "Plumbing", department: "ENG", count: 6 },
  ],
  otherCategories: 4,
};

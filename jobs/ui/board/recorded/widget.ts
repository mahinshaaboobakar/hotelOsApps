/**
 * The approved examples of the widget — frame 9: quiet, escalated, and mine.
 */

import type { JobsNow } from "../model";

export const recordedQuiet: JobsNow = {
  scope: "ENG · all quiet", open: 7, running: 2, atRisk: 0, breached: 0, stuck: 0, worst: [], unreadNudges: 0,
};

export const recordedEscalated: JobsNow = {
  scope: "ENG · 14:24", open: 11, running: 3, atRisk: 1, breached: 2, stuck: 1,
  worst: [
    { number: "MRN-ENG-142", line: "14m over → Priya", tone: "bad" },
    { number: "MRN-ENG-139", line: "not accepted 9m", tone: "bad" },
    { number: "MRN-ENG-140", line: "6m left", tone: "warn" },
  ],
  unreadNudges: 0,
};

export const recordedMine: JobsNow = {
  scope: "you · Arjun", open: 3, running: 1, atRisk: 1, breached: 1, stuck: 0,
  worst: [
    { number: "MRN-ENG-142", line: "running 23m", tone: "run" },
    { number: "MRN-ENG-140", line: "6m left", tone: "warn" },
  ],
  unreadNudges: 1,
};

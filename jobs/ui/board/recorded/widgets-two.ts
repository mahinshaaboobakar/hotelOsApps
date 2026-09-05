/**
 * The approved examples of the two widgets built from Z's canvas — The Board
 * and Blocked, `1e28fc0`, owner-approved 2026-09-03.
 *
 * The figures are the frames' own, so a capture taken beside the drawing is
 * comparing the same property's morning rather than two different ones.
 */

import type { BlockedNow, BoardNow } from "../model";

export const recordedBoardNow: BoardNow = {
  raised: 6,
  running: 9,
  onHold: 3,
  doneToday: 7,
  longestWaiting: [
    { id: "j2214", number: "MRN-ENG-214", what: "Air conditioning › Not cooling", since: "48m", tone: "warn" },
    { id: "j2219", number: "MRN-HK-219", what: "Housekeeping › Towels", since: "31m", tone: "warn" },
    { id: "j2221", number: "MRN-ENG-221", what: "Lifts › Alarm test", since: "12m", tone: "warn" },
  ],
};

export const recordedBlockedNow: BlockedNow = {
  onHold: 3,
  pausedCount: 2,
  held: [
    { id: "j2201", number: "MRN-ENG-201", what: "part on order", since: "2d", tone: "hold" },
    { id: "j2207", number: "MRN-HK-207", what: "guest DND", since: "4h", tone: "hold" },
    { id: "j2212", number: "MRN-ENG-212", what: "earlier step", since: "1d", tone: "hold" },
  ],
  paused: [
    { id: "j2218", number: "MRN-ENG-218", what: "assignee break", since: "22m", tone: "run" },
  ],
};

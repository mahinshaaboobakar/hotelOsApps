/**
 * The four kinds of thing a person has to decide — frame 12.
 */

import type { AttentionCard } from "../model";
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

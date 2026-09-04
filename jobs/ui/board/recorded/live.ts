/**
 * The approved examples of the Live tab (frame 5) and the Scheduled tab
 * (frame 6) — Marina Bay, 2 September 2026, the sweep at 14:24:00.
 */

import type { Live, ScheduledRow } from "../model";
import { at } from "./board";

export const recordedLive: Live = {
  sweptAt: at("14:24"),
  departments: [
    {
      code: "ENG", name: "Engineering", presence: "present", presenceLine: "day shift since 02 Sep 07:00 · 9 on",
      people: [
        { name: "Arjun Menon", doing: "MRN-ENG-142 · 23m", tone: "run" },
        { name: "Farhan Ali", doing: "MRN-ENG-134 · 41m", tone: "run" },
        { name: "Team · Day shift (3)", doing: "MRN-ENG-133 · 12m", tone: "run" },
        { name: "Deepak Rao", doing: "on hold · 141", tone: "hold" },
        { name: "Team · Night shift", doing: "139 not accepted", tone: "bad" },
        { name: "Vikram S.", doing: "free · 1 accepted", tone: "dim" },
      ],
      peopleTotal: 9, open: 11, breached: 2,
    },
    {
      code: "HK", name: "Housekeeping", presence: "present", presenceLine: "since 02 Sep 07:00 · 14 on",
      people: [
        { name: "Meera Krishnan", doing: "MRN-HK-391 · 6m", tone: "run" },
        { name: "Lakshmi P.", doing: "MRN-HK-390 · 12m", tone: "run" },
        { name: "Team · Floor 12 (4)", doing: "MRN-HK-389 · 3m", tone: "run" },
        { name: "Anita D.", doing: "free · 2 accepted", tone: "dim" },
        { name: "Joseph K.", doing: "free", tone: "dim" },
        { name: "Rekha M.", doing: "free", tone: "dim" },
      ],
      peopleTotal: 14, open: 14, breached: 0,
    },
    {
      code: "FO", name: "Front Office", presence: "off",
      presenceLine: "No shift feed, no service hours — jobs run on the property clock (S7 D8: off).",
      people: [{ name: "Sana Rahman", doing: "free · 2 open", tone: "dim" }],
      peopleTotal: 1, open: 2, breached: 0,
    },
  ],
  concern: [
    { number: "MRN-ENG-142", department: "ENG", concern: "BREACHED", since: at("14:10"), accountable: "Priya Nair · supervisor", lastNudge: "02 Sep 14:10 · in-app" },
    { number: "MRN-ENG-139", department: "ENG", concern: "STUCK", since: at("14:15"), accountable: "Priya Nair · supervisor", lastNudge: "02 Sep 14:15 · in-app" },
    { number: "MRN-ENG-140", department: "ENG", concern: "AT_RISK", since: at("14:34"), accountable: "Arjun Menon · assignee", lastNudge: "02 Sep 14:34 · in-app" },
  ],
};

export const recordedScheduled: readonly ScheduledRow[] = [
  { scheduledFor: "2026-09-03", number: "MRN-ENG-131", where: "Floor 12 · all rooms", what: "Air conditioning › Filter change", tags: ["parent · 24 steps"], raisedBy: "APPLICATION · Engineering", assignedTo: "Team · Day shift", dueAt: at("18:00", "2026-09-03") },
  { scheduledFor: "2026-09-03", number: "MRN-ENG-144", where: "Room 1204", what: "Air conditioning › Filter replace", tags: ["child 2/2 of 142"], raisedBy: "Arjun Menon", assignedTo: "AUTO on the day", dueAt: at("12:00", "2026-09-03") },
  { scheduledFor: "2026-09-15", number: "MRN-ENG-118", where: "Plant room B", what: "Generator › Load test", tags: [], raisedBy: "APPLICATION · Engineering", assignedTo: "Deepak Rao", dueAt: at("17:00", "2026-09-15") },
  { scheduledFor: "2026-09-19", number: "MRN-HK-402", where: "Suite 2001", what: "Housekeeping › Deep clean", tags: [], raisedBy: "Sana Rahman (FO)", assignedTo: "Team · Floor 20", dueAt: at("15:00", "2026-09-19") },
];

/**
 * The approved examples of one job — MRN-ENG-142 mid-work and breached
 * (frames 2 to 2e, 2g) and MRN-HK-388 closed and rated (frame 2f).
 */

import type { JobDetail } from "../model";
import { at, recordedBoard } from "./board";

const row142 = recordedBoard.rows[0]!;
const row388 = recordedBoard.rows[11]!;

export const recordedJob: JobDetail = {
  // The approved example is Arjun's own job: he is the assignee looking at it,
  // so the work controls are his to press (frame 2's action row).
  row: { ...row142, viewerIsAssignee: true },
  raised: { at: at("13:31"), via: "GUEST_APP", kind: "GUEST", who: "the guest of stay 7F2A" },
  endedAt: null,
  runningSeconds: 23 * 60 + 41,
  runningWho: "Arjun Menon",
  totalWorkedSeconds: 31 * 60 + 12,
  accountable: "Priya Nair (supervisor, ladder step 2)",
  whatAndWhere: [
    { k: "Category › item", v: "Air conditioning › Not cooling" },
    { k: "Summary", v: "Room feels warm since noon, set to 19 but blowing ambient." },
    { k: "Details", v: "Guest has a medical need for a cool room; FO offered a fan at 13:40." },
    { k: "Location", v: "Room 1204 · Floor 12 · Tower B" },
    { k: "Asset", v: "FCU-12-04 · Daikin FXFQ · filter changed 11 weeks ago · 2 earlier jobs" },
    { k: "Department", v: "Engineering (ENG)" },
    { k: "Restricted", v: "No (catalogue default)" },
  ],
  whoAsked: [
    { k: "Raised", v: "GUEST · via the guest app · 02 Sep 13:31" },
    { k: "Stay", v: "7F2A · Mr. Okafor · 1204 · in-house, departs Thu 04 Sep" },
    { k: "Rating", v: "— asked after close" },
  ],
  priorityAndTime: [
    { k: "Priority", v: "P1 · decided by flow (guest in room) · catalogue said P2" },
    { k: "Scheduled for", v: "— raised now" },
    { k: "Due at", v: "02 Sep 14:10 · policy P1 = 40 min" },
    { k: "Hold", v: "—" },
  ],
  assignment: [
    { k: "Current", v: "Arjun Menon · ENG technician · shift 14:00–22:00" },
    { k: "Accepted", v: "02 Sep 13:47 · 14 min after assignment" },
    { k: "How", v: "AUTO · on shift on the execution date, fewest open P1" },
  ],
  resolution: null,
  sessions: [
    { no: 1, who: "Arjun Menon", startedAt: at("13:52"), pausedAt: at("14:02"), pauseReason: "fetch gauge", resumedAt: null, stoppedAt: at("14:02"), workedSeconds: 600 },
    { no: 2, who: "Arjun Menon", startedAt: at("14:06"), pausedAt: null, pauseReason: null, resumedAt: null, stoppedAt: null, workedSeconds: 23 * 60 + 41 },
  ],
  history: [
    { at: at("14:10"), kind: "concern", what: "BREACHED", by: "sweep", detail: "accountable → Priya Nair (step 2) · nudge in-app" },
    { at: at("14:06"), kind: "work", what: "session 2 started", by: "Arjun Menon", detail: "" },
    { at: at("14:02"), kind: "work", what: "session 1 paused, then stopped", by: "Arjun Menon", detail: "fetch gauge" },
    { at: at("14:00"), kind: "concern", what: "AT_RISK", by: "sweep", detail: "75 % of 40 min · nudge to Arjun Menon" },
    { at: at("13:52"), kind: "status", what: "IN_PROGRESS", by: "Arjun Menon", detail: "session 1 started" },
    { at: at("13:47"), kind: "status", what: "ACCEPTED", by: "Arjun Menon", detail: "" },
    { at: at("13:33"), kind: "status", what: "ASSIGNED", by: "AUTO", detail: "on shift on the execution date, fewest open P1 → Arjun Menon" },
    { at: at("13:31"), kind: "status", what: "RAISED", by: "guest · stay 7F2A", detail: "via the guest app · priority P1 by flow (catalogue P2)" },
  ],
  notes: [
    { who: "Arjun Menon", at: at("14:07"), text: "Suction pressure low, likely refrigerant.", photo: "gauge.jpg · 1.2 MB" },
    { who: "Sana Rahman (FO)", at: at("13:40"), text: "Guest called too; offered fan meanwhile.", photo: null },
    { who: "Guest", at: at("13:31"), text: "Room feels warm since noon, set to 19 but blowing ambient.", photo: null },
  ],
  steps: [
    { no: 1, number: "MRN-ENG-130", what: "Air conditioning › Leak test", status: "ASSIGNED · blocked", clock: "stopped until this job resolves", assignedTo: "Arjun Menon" },
    { no: 2, number: "MRN-ENG-144", what: "Air conditioning › Filter replace", status: "SCHEDULED 03 Sep", clock: "starts on the day", assignedTo: "AUTO" },
  ],
  links: [
    { number: "MRN-HK-388", department: "HK", what: "Housekeeping › Extra towels", status: "RESOLVED", assignedTo: "Meera Krishnan" },
  ],
  rating: null,
  record: [
    { k: "job_id", v: "018f3c…9a1e" },
    { k: "Number", v: "MRN-ENG-142" },
    { k: "Property", v: "Marina Bay · mrn" },
    { k: "Version", v: "9" },
    { k: "Created", v: "02 Sep 13:31 · guest · stay 7F2A" },
    { k: "Updated", v: "02 Sep 14:07 · Arjun Menon" },
    { k: "Deleted", v: "—" },
    { k: "Reminders", v: "none" },
  ],
};

/** The towel job, closed and rated — the filled state of the Rating tab. */
export const recordedRatedJob: JobDetail = {
  ...recordedJob,
  row: { ...row388, status: "CLOSED", concern: "ON_TRACK", concernDetail: null, viewerIsAssignee: false },
  raised: { at: at("13:53"), via: "GUEST_APP", kind: "GUEST", who: "the guest of stay 7F2A" },
  endedAt: at("18:00"),
  runningSeconds: null,
  runningWho: null,
  totalWorkedSeconds: 6 * 60,
  accountable: "—",
  resolution: "Delivered · Meera Krishnan · 02 Sep 13:59",
  sessions: [{ no: 1, who: "Meera Krishnan", startedAt: at("13:54"), pausedAt: null, pauseReason: null, resumedAt: null, stoppedAt: at("13:59"), workedSeconds: 300 }],
  steps: [],
  links: [{ number: "MRN-ENG-142", department: "ENG", what: "Air conditioning › Not cooling", status: "IN_PROGRESS", assignedTo: "Arjun Menon" }],
  history: [
    { at: at("18:14"), kind: "status", what: "RATED", by: "guest · stay 7F2A", detail: "5 stars" },
    { at: at("18:00"), kind: "status", what: "CLOSED", by: "sweep", detail: "auto-close after 1 h" },
    { at: at("13:59"), kind: "status", what: "RESOLVED", by: "Meera Krishnan", detail: "Delivered" },
    { at: at("13:54"), kind: "work", what: "session 1 started", by: "Meera Krishnan", detail: "" },
    { at: at("13:53"), kind: "status", what: "RAISED", by: "guest · stay 7F2A", detail: "via the guest app" },
  ],
  notes: [{ who: "Guest", at: at("13:53"), text: "Could we have two extra towels please?", photo: null }],
  rating: {
    stars: 5, text: "Towels came in six minutes. Thank you Meera.", ratedAt: at("18:14"),
    askedAt: at("18:00"), windowUntil: "departure Thu 04 Sep", resolvedBy: "Meera Krishnan · \"Delivered\" · 02 Sep 13:59",
    minutesRaisedToResolved: 6,
  },
};

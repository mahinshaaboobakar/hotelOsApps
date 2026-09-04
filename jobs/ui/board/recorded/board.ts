/**
 * The approved example of the board — mockup 01 frame 1, The Marina Bay, ENG,
 * Tuesday 2 September 2026 at 14:24 property time (11:24Z). Twelve rows of 47.
 */

import type { BoardPage, JobRow, Today } from "../model";

/** An instant on 2 Sep 2026, given the property-time clock (Asia/Qatar, UTC+3). */
export function at(hhmm: string, day = "2026-09-02"): string {
  const [h = "0", m = "0"] = hhmm.split(":");
  const utc = new Date(Date.UTC(2026, 8, Number(day.slice(8)), Number(h) - 3, Number(m)));
  return utc.toISOString();
}

export const recordedToday: Today = {
  open: 11, breached: 2, stuck: 1, running: 3, closedToday: 18, avgResolveMinutes: 41,
  department: "ENG", at: at("14:24"),
};

const rows: JobRow[] = [
  { id: "j142", number: "MRN-ENG-142", where: "Room 1204", what: "Air conditioning › Not cooling", priority: "P1", status: "IN_PROGRESS", raisedBy: "Guest · stay 7F2A", assignedTo: "Arjun Menon", concern: "BREACHED", concernDetail: "14m", dueAt: at("14:10"), tags: [] },
  { id: "j141", number: "MRN-ENG-141", where: "Pool plant room", what: "Pump › Vibration", priority: "P2", status: "ON_HOLD", raisedBy: "Deepak Rao", assignedTo: "Deepak Rao", concern: "ON_TRACK", concernDetail: "waiting · parts, Thu", dueAt: at("09:00", "2026-09-04"), tags: [] },
  { id: "j140", number: "MRN-ENG-140", where: "Room 0817", what: "Lighting › Bedside lamp dead", priority: "P3", status: "ACCEPTED", raisedBy: "Sana Rahman (FO)", assignedTo: "Arjun Menon", concern: "AT_RISK", concernDetail: "6m left", dueAt: at("14:40"), tags: [] },
  { id: "j139", number: "MRN-ENG-139", where: "Restaurant · Azure", what: "Refrigeration › Walk-in warm", priority: "P1", status: "ASSIGNED", raisedBy: "Chef Anand (F&B)", assignedTo: "Team · Night shift", concern: "STUCK", concernDetail: "not accepted 9m", dueAt: at("14:25"), tags: [] },
  { id: "j138", number: "MRN-ENG-138", where: "Room 1512", what: "Plumbing › Shower drains slowly", priority: "NOT_TRIAGED", status: "RAISED", raisedBy: "Guest · QR · stay 9C11", assignedTo: "—", concern: "ON_TRACK", concernDetail: null, dueAt: at("16:00"), tags: [] },
  { id: "j136", number: "MRN-ENG-136", where: "Lobby", what: "Doors › Revolving door slow", priority: "P2", status: "RAISED", raisedBy: "Sana Rahman (FO)", assignedTo: "AUTO · pending", concern: "ON_TRACK", concernDetail: null, dueAt: at("15:50"), tags: [] },
  { id: "j135", number: "MRN-ENG-135", where: "Room 0402", what: "Plumbing › Tap dripping", priority: "P3", status: "ACCEPTED", raisedBy: "Meera Krishnan (HK)", assignedTo: "Farhan Ali", concern: "ON_TRACK", concernDetail: null, dueAt: at("17:00"), tags: [] },
  { id: "j134", number: "MRN-ENG-134", where: "Terrace bar", what: "Lighting › String lights out", priority: "P3", status: "IN_PROGRESS", raisedBy: "Chef Anand (F&B)", assignedTo: "Farhan Ali", concern: "ON_TRACK", concernDetail: null, dueAt: at("18:00"), tags: [] },
  { id: "j133", number: "MRN-ENG-133", where: "Room 1105", what: "Air conditioning › Noisy", priority: "P2", status: "IN_PROGRESS", raisedBy: "Guest · WhatsApp · stay 3B70", assignedTo: "Team · Day shift", concern: "ON_TRACK", concernDetail: null, dueAt: at("15:30"), tags: [] },
  { id: "j132", number: "MRN-ENG-132", where: "Floor 9 · corridor", what: "Lighting › Emergency light fault", priority: "P1", status: "ASSIGNED", raisedBy: "Rohan Desai", assignedTo: "Deepak Rao", concern: "ON_TRACK", concernDetail: null, dueAt: at("14:50"), tags: ["restricted"] },
  { id: "j130", number: "MRN-ENG-130", where: "Room 1204", what: "Air conditioning › Leak test", priority: "P2", status: "ASSIGNED", raisedBy: "Arjun Menon", assignedTo: "Arjun Menon", concern: "ON_TRACK", concernDetail: "clock stopped", dueAt: at("12:00", "2026-09-03"), tags: ["child 1/2 · blocked"] },
  { id: "j388", number: "MRN-HK-388", where: "Room 1204", what: "Housekeeping › Extra towels", priority: "P3", status: "RESOLVED", raisedBy: "Guest · stay 7F2A", assignedTo: "Meera Krishnan (HK)", concern: "ON_TRACK", concernDetail: "auto-close 18:00", dueAt: null, tags: ["linked"] },
];

export const recordedBoard: BoardPage = { rows, total: 47, page: 0, pageSize: 12 };

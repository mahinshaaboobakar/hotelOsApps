/**
 * Every surface Workforce draws, against its recorded read — one list, shared.
 *
 * Two checks walk every surface (numbers in the property's locale, and no
 * register id shown to staff). Private lists drift in the half nobody reads —
 * which surfaces each one bothered to visit — so the visited set is one fact,
 * here, and a new screen is added once.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { recordedDay } from "../roster/attendance";
import { recordedRegister } from "../roster/duty";
import { recordedLeave } from "../roster/leave";
import { recordedPeople } from "../roster/people";
import { recordedPolicy } from "../roster/policy";
import { recordedWeek } from "../roster/recorded";
import { recordedMonth } from "../roster/reports";
import { recordedSchedule } from "../roster/schedule";
import {
  recordedAttendanceToday, recordedComingUp, recordedOnLeave, recordedPendingRequests,
  recordedShiftBoard,
} from "../roster/summaries";
import { recordedTeams } from "../roster/teams";
import { attendance } from "../screens/attendance";
import { duty } from "../screens/duty";
import { leave } from "../screens/leave";
import { people } from "../screens/people";
import { policy } from "../screens/policy";
import { printed } from "../screens/printed";
import { reports } from "../screens/reports";
import { rota } from "../screens/rota";
import { schedule } from "../screens/schedule";
import { shifts } from "../screens/shifts";
import { teams } from "../screens/teams";
import { attendanceToday } from "../widgets/panel/attendance-today";
import { comingUp } from "../widgets/panel/coming-up";
import { onLeave } from "../widgets/panel/on-leave";
import { pendingRequests } from "../widgets/panel/pending-requests";
import { shiftBoard } from "../widgets/panel/shift-board";

/** What each read answers — the recorded fixtures, keyed by method. */
export const ANSWERS: Record<string, unknown> = {
  day: recordedDay, register: recordedRegister, leave: recordedLeave,
  people: recordedPeople, policy: recordedPolicy, week: recordedWeek,
  month: recordedMonth, schedule: recordedSchedule, teams: recordedTeams,
  attendanceToday: recordedAttendanceToday, comingUp: recordedComingUp,
  onLeave: recordedOnLeave, pendingRequests: recordedPendingRequests,
  shiftBoard: recordedShiftBoard,
};

/** A host answering every recorded read, in the given locale. */
export function surfaceHost(locale: string): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale },
    call: (_capability: string, method: string) => method in ANSWERS
      ? Promise.resolve(ANSWERS[method])
      : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" })),
    on: () => () => {},
  };
}

/** Draw one surface into a container. */
export type Draw = (host: HostApi, main: HTMLElement) => Promise<unknown>;

/** Every surface, by the name a failure message will use. */
export const SURFACES: readonly (readonly [string, Draw])[] = [
  ["attendance", (host, main) => attendance(host, main)],
  ["duty", (host, main) => duty(host, main)],
  ["leave · requests", (host, main) => leave(host, main, "Requests", () => {})],
  ["leave · approvals", (host, main) => leave(host, main, "Approvals", () => {})],
  ["people", (host, main) => people(host, main)],
  ["policy", (host, main) => policy(host, main)],
  ["printed", (host, main) => printed(host, main)],
  ["reports", (host, main) => reports(host, main)],
  ["rota", (host, main) => rota(host, main)],
  ["schedule", (host, main) => schedule(host, main, {
    ok: true,
    value: { staffId: "a3f1c064-5d21-4e8b-9f02-1a7c6b40d911", name: "Anjali Menon",
      department: "Front Office", role: "Receptionist", property: "" },
  })],
  ["shifts", (host, main) => shifts(host, main)],
  ["teams", (host, main) => teams(host, main)],
  ["attendance today", async (host, main) => { main.append(await attendanceToday(host)); }],
  ["coming up", async (host, main) => { main.append(await comingUp(host)); }],
  ["on leave", async (host, main) => { main.append(await onLeave(host)); }],
  ["pending requests", async (host, main) => { main.append(await pendingRequests(host)); }],
  ["shift board", async (host, main) => { main.append(await shiftBoard(host)); }],
];

/** The text a person reads — a style sheet is not text. */
export function readable(root: HTMLElement): HTMLElement {
  const copy = root.cloneNode(true) as HTMLElement;
  for (const style of Array.from(copy.querySelectorAll("style"))) style.remove();
  return copy;
}

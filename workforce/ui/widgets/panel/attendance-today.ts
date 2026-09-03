/**
 * Attendance Today — the rota against who actually came.
 *
 * # `present` is the one figure this widget cannot settle on its own
 *
 * `DayComparison` is a **union**: a row exists for anybody the rota planned
 * *or* anybody who turned up, and `Attended` is true for both. So the domain
 * knows two different populations — *rostered and present*, and *present at
 * all* — and the approved artboard's arithmetic (34 present + 4 absent = 38
 * rostered) is the first.
 *
 * That is the same question the Attendance screen carries to the owner as
 * *4 of 5, or 5 of 6*: whether somebody who came unrostered counts as present.
 * This widget draws the artboard's reading, and the round's report says so —
 * it is not a number to pick quietly, because the two answers differ by
 * exactly the person nobody planned for.
 */

import type { HostApi } from "@hotelos/sdk";

import { ROSTER_READ } from "../../chrome/permissions";
import { load } from "../../roster";
import { recordedAttendanceToday } from "../../roster/summaries";

import { bar, card, figures, rows, section } from "../card";

/**
 * Draw the card.
 *
 * @param host the bridge, and the only route out of this realm
 * @returns the card
 */
export async function attendanceToday(host: HostApi): Promise<HTMLElement> {
  const got = await load(host, ROSTER_READ, "attendanceToday", recordedAttendanceToday);
  const day = got.value;

  return card("Attendance Today", got.live, [
    figures(day.figures),
    bar(day.share),
    section("Absent against the rota"),
    rows(day.byDepartment, host),
    section("Late in"),
    rows(day.lateIn, host),
  ]);
}

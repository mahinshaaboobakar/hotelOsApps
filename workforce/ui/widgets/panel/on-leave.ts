/**
 * On Leave — who is away today, and for the rest of the week.
 *
 * # The department is the posting's, not the request's
 *
 * The artboard says so at its foot, and it is a real rule rather than a
 * caption: a `LeaveRequest` carries a staff id and no department, because a
 * person's department is a property of where they are posted and can change
 * while a request sits waiting. Grouping by anything the request carried would
 * file somebody under a department they had already left.
 */

import type { HostApi } from "@hotelos/sdk";

import { ROSTER_READ } from "../../chrome/permissions";
import { load } from "../../roster";
import { recordedOnLeave } from "../../roster/summaries";

import { card, figures, note, rows, section } from "../card";

/**
 * Draw the card.
 *
 * @param host the bridge, and the only route out of this realm
 * @returns the card
 */
export async function onLeave(host: HostApi): Promise<HTMLElement> {
  const got = await load(host, ROSTER_READ, "onLeave", recordedOnLeave);
  const away = got.value;

  return card("On Leave", got.live, [
    figures(away.figures),
    section("Away today"),
    rows(away.today, host),
    section("Rest of the week"),
    rows(away.restOfWeek, host),
    note("Department comes from the staff member's posting, not the request."),
  ]);
}

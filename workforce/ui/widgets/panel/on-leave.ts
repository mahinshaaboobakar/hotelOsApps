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

import { type HostApi, load } from "@hotelos/sdk";

import { ROSTER_READ } from "../../chrome/permissions";

import type { OnLeave } from "../../roster/widget";
import { failureCard, card, figures, rows, section } from "../card";

/**
 * Draw the card.
 *
 * @param host the bridge, and the only route out of this realm
 * @returns the card
 */
export async function onLeave(host: HostApi): Promise<HTMLElement> {
  const got = await load<OnLeave>(host, ROSTER_READ, "onLeave");
  if (!got.ok) {
    return failureCard('On Leave', got.failure, { the: 'who is away' },
      { host, opens: "leave", again: () => onLeave(host) });
  }

  const away = got.value;

  return card("On Leave", [
    figures(away.figures, host.property),
    section("Away today"),
    rows(away.today, host),
    section("Rest of the week"),
    rows(away.restOfWeek, host),
    // No foot note. The artboard drew "Department comes from the staff
    // member's posting, not the request" — where a figure comes from is a note
    // for the developer, never UI (owner ruling, 2026-09-19). The rule itself
    // stands, and is this file's header.
  ]);
}

/**
 * Pending Requests — swaps and leave waiting on a decision, oldest first.
 *
 * # Age is time waiting
 *
 * The artboard says so on its own face, and it is the whole reason the widget
 * sorts by it: a request five days old is a person who has been waiting five
 * days, whatever date the shift falls on. `SwapProposal.CreatedAt` and
 * `LeaveRequest.CreatedAt` are both stored, so the figure is arithmetic over a
 * clock rather than a state anything holds.
 *
 * # Whose queue this is, is the open question
 *
 * The backend answers *waiting on one person* — `SwapProposalService.WaitingOnAsync`
 * and `LeaveService.QueueAsync` both take a staff id — and nothing answers
 * *waiting at this property*. The artboard's header says neither. The two are
 * different widgets for a general manager, and the round's report carries the
 * question rather than choosing.
 */

import type { HostApi } from "@hotelos/sdk";

import { ROSTER_READ } from "../../chrome/permissions";
import { load } from "../../roster";
import { recordedPendingRequests } from "../../roster/summaries";

import { card, figures, note, rows, section } from "../card";

/**
 * Draw the card.
 *
 * @param host the bridge, and the only route out of this realm
 * @returns the card
 */
export async function pendingRequests(host: HostApi): Promise<HTMLElement> {
  const got = await load(host, ROSTER_READ, "pendingRequests", recordedPendingRequests);
  const queue = got.value;

  return card("Pending Requests", got.live, [
    figures(queue.figures),
    section("Oldest first"),
    rows(queue.rows, host),
    note("Age is time waiting, not time until the shift."),
  ]);
}

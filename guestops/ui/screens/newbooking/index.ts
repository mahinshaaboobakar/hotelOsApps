/**
 * New booking — the answer before the sale. Gold frame 14.
 *
 * **Availability is computed from what we already hold; it is not a table
 * anyone feeds** (GUEST-Q7). Both modes are fully v1, because a standalone
 * property that cannot say what is free is not a property that can open — and
 * no new inventory owner was created to do it.
 *
 * **What this is not**: no pricing by occupancy, no minimum stay, no
 * closed-to-arrival, no travel-agent allotments. Those are revenue-management
 * concepts and the platform has named no owner for them.
 *
 * This screen keeps a title, unlike the list screens. `New booking` is not a
 * word the bar says — it names what is being done here, and docs/working/64 §3
 * removes only the heading that repeats a section name.
 *
 * **The screen is the title and the flow.** It read availability with no dates
 * until 2026-09-23 and drew a static query bar above the answer — so it showed
 * what was free on dates nobody had chosen, and had nowhere to go from there.
 * `flow.ts` holds the five steps the owner approved; this composes them with
 * the heading and the walk-in control.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el } from "../../chrome/element";
import { flow } from "./flow";

/**
 * Render the screen.
 *
 * **Every room type, with only the list scrolling — the owner's ruling on this
 * screen, 2026-09-19 (G7).** It paged by twelve, and frame 14 put an
 * explanatory note and a "where each number comes from" card under the pager,
 * leaving the list two rows tall. The owner ruled both panels developer notes
 * that should never have been built as screen, and the list unpaged.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 * @param walkIn what the Walk-in action does
 */
export async function newBooking(
  host: HostApi,
  into: HTMLElement,
  walkIn: () => void,
  opened: (bookingId: string) => void,
): Promise<void> {
  const title = el("div", "title");
  const heading = el("div");

  heading.append(el("div", "ht", "New booking"));
  title.append(heading, el("div", "grow"), control("btn", "Walk-in", walkIn));

  const stage = el("div");
  into.replaceChildren(title, stage);

  await flow(host, stage, opened);
}

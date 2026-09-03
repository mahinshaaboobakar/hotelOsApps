/**
 * Coming Up — the next seven days' risks, for the risks that can be measured.
 *
 * # It ships two rows short, and says so
 *
 * The approved catalogue asked for three: *unfilled*, *thin*, and *overlapping
 * leave*. Workforce has **no staffing demand model** — nothing anywhere in it
 * says how many people a department needs on a Thursday — so *unfilled* and
 * *thin* have nothing to be measured against. A rota with four people on it is
 * not thin or full; it is four people.
 *
 * The honesty rule settles what to do about that: *a number the backend cannot
 * honestly compute is absent, never approximate*. Not drawn as zero, not as a
 * dash, not smaller — absent, with the gap stated on the widget's own face so
 * a manager is never left believing the panel checked something it could not.
 *
 * The rows return the day a demand model does, and this file is where they go.
 */

import type { HostApi } from "@hotelos/sdk";

import { ROSTER_READ } from "../../chrome/permissions";
import { load } from "../../roster";
import { recordedComingUp } from "../../roster/summaries";

import { card, figures, note, rows, section } from "../card";

/**
 * Draw the card.
 *
 * @param host the bridge, and the only route out of this realm
 * @returns the card
 */
export async function comingUp(host: HostApi): Promise<HTMLElement> {
  const got = await load(host, ROSTER_READ, "comingUp", recordedComingUp);
  const ahead = got.value;

  return card("Coming Up", got.live, [
    section("Next 7 days"),
    figures(ahead.figures),
    section("Two or more away, same department"),
    rows(ahead.overlaps, host),
    section("Certifications expiring"),
    rows(ahead.expiring, host),
    note(
      "Unfilled posts and thin shifts are not drawn — Workforce has no staffing demand model.",
    ),
  ]);
}

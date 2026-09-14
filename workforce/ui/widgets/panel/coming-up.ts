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

import { formatDay, type HostApi, load, type PropertyEnvironment } from "@hotelos/sdk";

import { ROSTER_READ } from "../../chrome/permissions";
import type { SummaryRow } from "../../roster/widget";

import type { ComingUp } from "../../roster/widget";
import { failureCard, card, figures, note, rows, section } from "../card";

/**
 * Draw the card.
 *
 * @param host the bridge, and the only route out of this realm
 * @returns the card
 */
export async function comingUp(host: HostApi): Promise<HTMLElement> {
  const got = await load<ComingUp>(host, ROSTER_READ, "comingUp");
  if (!got.ok) {
    return failureCard('Coming Up', got.failure, { the: 'the next seven days' });
  }

  const ahead = got.value;

  return card("Coming Up", [
    section("Next 7 days"),
    figures(ahead.figures),
    section("Two or more away, same department"),
    rows(dated(ahead.overlaps, host.property), host),
    section("Certifications expiring"),
    rows(ahead.expiring, host),
    note(
      "Unfilled posts and thin shifts are not drawn — Workforce has no staffing demand model.",
    ),
  ]);
}

/**
 * The overlap rows, with their day said in the property's form.
 *
 * The service sends the department in words and the day as ISO; the name the
 * card draws is the two together, and only a panel that knows its rows are
 * dated can compose it — the service does not know the locale, and the generic
 * row renderer must not sniff a field for something that looks like a date.
 *
 * **The glance form, and why it is the SDK's rather than this file's.** The
 * approved frame draws *Housekeeping · Thu 11*, and for a while this drew
 * *Housekeeping · 11 Sept 2026* — `formatDay`'s only form carried the year, and
 * `InstantStyle.date` takes an instant, which a calendar day is not. Writing
 * the short form here would have forked the one day formatter the platform has
 * so that one card could match one drawing, so it was reported instead and
 * `DayStyle.weekday-day` was published for every application's widgets to use
 * (BB, `9fb3c42`).
 *
 * **The part order is the locale's, not ours.** A US-locale property renders
 * *10 Thu*, which is that locale ordering the same two parts and not a defect
 * to correct here — a card that reimposed weekday-then-day would be this file
 * deciding what a property's language does.
 */
function dated(
  overlaps: readonly SummaryRow[], property: PropertyEnvironment,
): readonly SummaryRow[] {
  return overlaps.map((one) => one.on === undefined
    ? one
    : { ...one, name: `${one.name} · ${formatDay(one.on, property, "weekday-day")}` });
}

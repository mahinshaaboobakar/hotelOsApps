/**
 * An instant or a day the wire may not have — drawn by the SDK, or as `—`.
 *
 * Page 64 §11: *"Absent renders `—`, never today."* The SDK's formatters take a
 * value and nothing else, and every nullable field on GuestOps' wire — a
 * departure time nobody recorded, a feed that has never spoken — would otherwise
 * repeat the same `=== null` at each call site, where one of them eventually
 * draws `""` or the word `null` instead. Shared by the screens and the widgets,
 * as `glyph.ts` is.
 */

import { formatDay, formatInstant, type DayStyle, type InstantStyle, type PropertyEnvironment } from "@hotelos/sdk";

/** The dash an absent value is drawn as — never a date somebody could act on. */
export const ABSENT = "—";

/** An ISO instant in the property's form, or the dash. */
export function instant(
  iso: string | null,
  property: PropertyEnvironment,
  style: InstantStyle,
): string {
  return iso === null ? ABSENT : formatInstant(iso, property, style);
}

/** An ISO day (`2026-09-03`) in the property's form, or the dash. */
export function day(
  iso: string | null,
  property: PropertyEnvironment,
  style: DayStyle,
): string {
  return iso === null ? ABSENT : formatDay(iso, property, style);
}

/**
 * A stay's span — `03 Sept → 07 Sept`, or the day-use form when it arrives and
 * leaves on one day.
 *
 * Composed from the two ISO days the service sends: the order of day and month,
 * and the month's abbreviation, are the property's locale's and never the
 * server's.
 *
 * **The arrow join is a decision, not a gap — do not "improve" it into a
 * compressed range** (owner ruling, 2026-09-20, option B long). `3–7 Sept`
 * reads shorter and carries a grammar: the month sits at the second end only
 * because this locale puts the month after the day, and the same two days read
 * `Sep 3 – 7` in `en-US`. **Two separately formatted days joined by an arrow
 * assert nothing about either language**, which is exactly why the compressed
 * form had to go when the service stopped writing sentences.
 *
 * The compressed form was measured before it was rejected — `Intl`'s
 * `formatRange` produces `3–7 Sept`, `Sep 3 – 7` and `31 Aug – 2 Sept`
 * correctly per locale, so this is a choice between two working options rather
 * than a limitation. It was drawn for the owner (`docs/mockups/07`) and the
 * long form was chosen. **No shared range formatter is to be added for it.**
 *
 * It lived in `screens/today/table.ts` until 2026-09-20, when Booking, Bookings
 * and the cancel plan needed the same span. Two copies drift in the half nobody
 * reads — which one says *day use*, which one dashes an absent arrival.
 */
export function span(
  arrive: string | null,
  depart: string | null,
  property: PropertyEnvironment,
  style: DayStyle = "day-month",
): string {
  if (arrive === null) return day(null, property, style);

  const from = day(arrive, property, style);
  if (depart === null) return from;

  return depart === arrive
    ? `${from} · day use`
    : `${from} → ${day(depart, property, style)}`;
}

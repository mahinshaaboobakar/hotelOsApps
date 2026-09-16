/**
 * Reading a span of days, in the property's own form.
 *
 * **The range is the screen's to compose.** The service used to join it, in the
 * culture of whichever account the service process runs under, and to compress
 * a same-month range to `7 – 8 Sep`.
 *
 * **This cited "ADR 0152" and that was wrong — recorded rather than quietly
 * replaced, because anyone reinstating the composition would otherwise have to
 * delete the record to do it.** ADR 0152 is *a declared capability is served,
 * not necessarily reachable*, and mentions locale, range and grammar nowhere.
 * The citation was invented in this file and repeated from it, into two of my
 * reports and one of the architect's, before anybody opened the ADR.
 *
 * The rule it names is real and has **no ADR**: it is the recorded finding of
 * 2026-09-10 in `CLAUDE.md` — *a locale's word order is not a format, it is a
 * grammar, and it was baked into a service*. Whether it warrants an ADR of its
 * own is with the planner; until then this points at the finding rather than at
 * a number, because a number that does not say what the sentence claims is
 * worse than no number at all.
 *
 * The neighbouring rule, which does have an authority: instants cross the wire
 * machine-readable and each surface renders them through one shared utility
 * deriving locale and timezone from the property — `JOBS-Q1(8)`, 2026-09-04.
 *
 * **That compression does not come across the bridge, and the reason is the
 * ruling in miniature.** Putting the month only at the second end assumes the
 * month *follows* the day — true of `en-GB`, false of `en-US`, where the same
 * two days read *Sep 7 – 8*. It was a locale's word order written into a
 * service, and rebuilding it here would be the same assumption one layer over.
 * Both ends are rendered in full, and the property's locale decides what full
 * looks like.
 *
 * Here rather than in a screen because three screens draw a leave span — the
 * requests table, the approvals queue and the balance card — and a range that
 * reads two ways on two screens is the drift page 64 exists to stop.
 */

import { formatDay, type PropertyEnvironment } from "@hotelos/sdk";

/** The two ends of something, as the wire carries them. */
export interface Span {
  from: string;
  to: string;
}

/**
 * A span of days, as a person reads it.
 *
 * @param span the two ends, ISO
 * @param property the property's zone and locale
 * @returns one day when both ends are the same, and both otherwise
 */
export function days(span: Span, property: PropertyEnvironment): string {
  const from = formatDay(span.from, property, "day-month-year");
  if (span.from === span.to) return from;

  return `${from} – ${formatDay(span.to, property, "day-month-year")}`;
}

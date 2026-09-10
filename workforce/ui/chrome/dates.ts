/**
 * Reading a span of days, in the property's own form.
 *
 * **The range is the screen's to compose** — ADR 0152. The service used to join
 * it, in the culture of whichever account the service process runs under, and
 * to compress a same-month range to `7 – 8 Sep`.
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

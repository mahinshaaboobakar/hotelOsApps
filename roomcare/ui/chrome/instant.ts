/**
 * Every date and time on a Room Care screen, in the property's form — the SDK's
 * formatters bound to what the host handed at connect (`JOBS-Q1(8)`; page 64
 * §11). A screen calls these and never `Date` or `Intl` itself.
 *
 * **These comments state each function's shape, never its characters.** What a
 * date looks like is the property's locale's to decide — the order, the
 * separator, the hour cycle, the case and the script all move between locales,
 * and one locale's abbreviation of one month differs from the other eleven — so
 * an example typed here would be one locale's output on one day, and would
 * drift the day CLDR changes. To see the real form, run the SDK under the
 * property's locale.
 *
 * Which function a screen calls is decided by **what the value is**, never by
 * how far it is from the machine's clock: machine time is for machine facts
 * (page 64 §11), and a hotel's dates are not one.
 */

import { formatDay, formatInstant, type HostApi } from "@hotelos/sdk";

import { whole } from "./number";

/** An instant as a date and a time — the SDK's `date-time`. Absent is `—`. */
export function when(host: HostApi, iso: string | null | undefined): string {
  return iso === null || iso === undefined ? "—" : formatInstant(iso, host.property);
}

/** An instant as a time alone — the SDK's `time` — for a screen about today, whose date the strip states. Absent is `—`. */
export function clock(host: HostApi, iso: string | null | undefined): string {
  return iso === null || iso === undefined ? "—" : formatInstant(iso, host.property, "time");
}

/**
 * A calendar day with its year — the SDK's `formatDay`, `day-month-year`. For a
 * day that may be months away or ago: a deep clean's due date, its last one, a
 * grant's start. Absent is `—`.
 */
export function day(host: HostApi, isoDate: string | null | undefined): string {
  return isoDate === null || isoDate === undefined ? "—" : formatDay(isoDate, host.property);
}

/**
 * A calendar day without its year — the SDK's `formatDay`, `day-month` — for a
 * day that is near by what it is, never by a measurement against now: a linen
 * date, the days a supervision lane counts. Absent is `—`.
 *
 * Through `formatDay` like `day`, so the two render a property with no locale
 * established the same way, in two columns of one screen.
 */
export function nearDay(host: HostApi, isoDate: string | null | undefined): string {
  return isoDate === null || isoDate === undefined ? "—" : formatDay(isoDate, host.property, "day-month");
}

/**
 * Minutes as a person reads a shift — hours and minutes, or minutes alone under
 * an hour. Room Care's words, not a date; the digits are the property's
 * (`whole`, page 64 §12), and the minutes past an hour keep two places by a
 * leading zero written in the same digits.
 */
export function minutes(host: HostApi, total: number): string {
  const h = Math.floor(total / 60);
  const m = total % 60;
  return h === 0 ? `${whole(host, m)} min` : `${whole(host, h)} h ${m < 10 ? whole(host, 0) : ""}${whole(host, m)}`;
}

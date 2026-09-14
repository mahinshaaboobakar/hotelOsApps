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
 * A calendar day without its year — the SDK's `date` — for a day that is only
 * ever within the last or next few days by what it is: a linen date, the days a
 * supervision lane counts. Absent is `—`.
 *
 * **A day is not an instant.** It is formatted at noon in UTC, so the number the
 * wire sent is the number shown, whatever the property's offset — the SDK's own
 * `formatDay` reasoning. The SDK has no yearless day style, so this goes through
 * `formatInstant` with the zone set to UTC for this one call.
 */
export function nearDay(host: HostApi, isoDate: string | null | undefined): string {
  return isoDate === null || isoDate === undefined ? "—" : formatInstant(`${isoDate}T12:00:00Z`, { ...host.property, timezone: "UTC" }, "date");
}

/** Minutes as a person reads a shift — hours and minutes, or minutes alone under an hour. Room Care's words, not a date. */
export function minutes(total: number): string {
  const h = Math.floor(total / 60);
  const m = total % 60;
  return h === 0 ? `${m} min` : `${h} h ${String(m).padStart(2, "0")}`;
}

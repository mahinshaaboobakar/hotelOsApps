/**
 * Every timestamp on a Jobs screen, in the property's form — the SDK's
 * formatter bound to what the host handed at connect. A screen calls these
 * and never `Date` or `Intl` itself, so the whole module renders one way.
 */

import { formatDay, formatDuration, formatInstant, type HostApi } from "@hotelos/sdk";

/** `02 Sep 13:31` — date and time, always both (owner, 2026-09-04). */
export function when(host: HostApi, iso: string | null): string {
  return iso === null ? "—" : formatInstant(iso, host.property);
}

/** `13:31` — for a column whose date is already on the line. */
export function clock(host: HostApi, iso: string | null): string {
  return iso === null ? "—" : formatInstant(iso, host.property, "time");
}

/** `Tue 02 Sept, 14:24` — a line that names today, as the board's strip does. */
export function today(host: HostApi, iso: string): string {
  return formatInstant(iso, host.property, "weekday-time");
}

/** `03 Sep 2026` — a calendar day the property named. */
export function day(host: HostApi, isoDate: string): string {
  return formatDay(isoDate, host.property);
}

/** `00:23:41` — a worked duration. */
export function elapsed(seconds: number): string {
  return formatDuration(seconds);
}

// There is deliberately no "seconds since" here. Elapsed time is computed by
// the service, which owns the clock the property runs on, and handed over as a
// figure; a module that subtracted from `new Date()` would show the machine's
// drift as the hotel's promise (audit finding, 2026-09-04).

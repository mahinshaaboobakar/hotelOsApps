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

/** `03 Sep 2026` — a calendar day the property named. */
export function day(host: HostApi, isoDate: string): string {
  return formatDay(isoDate, host.property);
}

/** `00:23:41` — a worked duration. */
export function elapsed(seconds: number): string {
  return formatDuration(seconds);
}

/** Seconds since an instant, as of now — the live timer's figure. */
export function sinceSeconds(iso: string, now: Date = new Date()): number {
  const start = new Date(iso).getTime();
  return Number.isNaN(start) ? 0 : Math.max(0, Math.floor((now.getTime() - start) / 1000));
}

/**
 * Every timestamp on a Room Care screen, in the property's form — the SDK's
 * formatter bound to what the host handed at connect. A screen calls these and
 * never `Date` or `Intl` itself, so the whole module renders one way.
 */

import { formatDay, formatInstant, type HostApi } from "@hotelos/sdk";

/** `05 Sep 09:12` — date and time, always both. */
export function when(host: HostApi, iso: string | null | undefined): string {
  return iso === null || iso === undefined ? "—" : formatInstant(iso, host.property);
}

/** `09:12` — a time on today's screen, where the date is the strip's. */
export function clock(host: HostApi, iso: string | null | undefined): string {
  return iso === null || iso === undefined ? "—" : formatInstant(iso, host.property, "time");
}

/** `05 Sep 2026` — a business date. */
export function day(host: HostApi, isoDate: string | null | undefined): string {
  return isoDate === null || isoDate === undefined ? "—" : formatDay(isoDate, host.property);
}

/** Half a year either side of today — near enough that a date's year is plain. */
const NEAR_MS = 183 * 24 * 60 * 60 * 1000;

/** `03 Sep` for a date near enough that its year is plain; `03 Sep 2025` otherwise, so last year never reads as this week. */
export function shortDay(host: HostApi, isoDate: string | null | undefined): string {
  if (isoDate === null || isoDate === undefined) return "—";
  const noon = `${isoDate}T12:00:00Z`;
  return Math.abs(Date.parse(noon) - Date.now()) > NEAR_MS ? formatDay(isoDate, host.property) : formatInstant(noon, host.property, "date");
}

/** `3 h 40` — minutes as a person reads a shift. */
export function minutes(total: number): string {
  const h = Math.floor(total / 60);
  const m = total % 60;
  return h === 0 ? `${m} min` : `${h} h ${String(m).padStart(2, "0")}`;
}

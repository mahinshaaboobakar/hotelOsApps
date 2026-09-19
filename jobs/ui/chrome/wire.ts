/**
 * A value the backend sent, in the words a person at the property reads.
 *
 * The service sends data, not sentences: an instant as ISO, a day as
 * `2026-09-20`, a status or role as a token (`IN_PROGRESS`, `JOBS_MANAGER`), a
 * worked span as seconds (`1421s`). Screens that printed those as sent put
 * `2026-09-02T11:10:00.0000000+00:00` on a live property's Overview while every
 * guard was green, because the harness's recorded answers already held the
 * frames' words (found 2026-09-19; `tests/wire-shaped.test.ts`). Anything else
 * passes through untouched, so a value that is already words stays as it is.
 */

import type { HostApi } from "@hotelos/sdk";

import { day, elapsed, when } from "./instant";

const INSTANT = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/;
const DAY = /^\d{4}-\d{2}-\d{2}$/;
const SECONDS = /^(\d+)s$/;
const TOKEN = /\b[A-Z]{2,}(?:_[A-Z]+)+\b|\b(?:ASSIGNEE|SUPERVISOR|MANAGER|FLOW|STAFF|GUEST|APPLICATION|SWEEP)\b/g;

/**
 * A job's status and concern are the pills' vocabulary, drawn in capitals on
 * every screen ("IN PROGRESS", "BREACHED") — so they keep them, and lose only
 * the underscore, as `marks.ts` does.
 */
const STATES = new Set([
  "RAISED", "SCHEDULED", "ASSIGNED", "ACCEPTED", "IN_PROGRESS", "ON_HOLD", "RESOLVED", "CLOSED", "CANCELLED",
  "REOPENED", "ON_TRACK", "AT_RISK", "BREACHED", "STUCK", "NOT_TRIAGED",
]);

/** `IN_PROGRESS` → "IN PROGRESS"; `JOBS_MANAGER` → "Jobs manager"; `GUEST_APP` → "Guest app". */
function spoken(token: string): string {
  if (STATES.has(token)) return token.replaceAll("_", " ");
  const lower = token.toLowerCase().replaceAll("_", " ");
  return lower.charAt(0).toUpperCase() + lower.slice(1);
}

/** The value in the property's words — an instant, a day, a span or a token turned; anything else as it came. */
export function words(host: HostApi, value: string): string {
  if (INSTANT.test(value)) return when(host, value);
  if (DAY.test(value)) return day(host, value);
  const seconds = SECONDS.exec(value);
  if (seconds !== null) return elapsed(Number(seconds[1]));
  return value.replace(TOKEN, spoken);
}

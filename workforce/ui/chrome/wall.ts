/**
 * A wall-clock moment the person typed, turned into the instant it names **at
 * the property**.
 *
 * # The write direction had no home
 *
 * The SDK reads instants — `formatInstant`, `formatClock`, `formatDay` — and
 * each of them takes the property's zone or deliberately consults none. There
 * is no counterpart for the other direction, and a form with a
 * `datetime-local` control needs one: the control's value is *wall clock, no
 * zone*, and `new Date("2026-09-20T07:00")` reads it **in the zone of the
 * machine the desktop happens to be running on**.
 *
 * ```text
 * property  Asia/Kolkata      the operator types 07:00
 * machine   set to UTC        new Date(…) → 07:00Z
 * stored    07:00Z            which is 12:30 at the property
 * ```
 *
 * The desktop is *usually* on a machine in the property's own zone, which is
 * why this is invisible rather than absent — a laptop taken home, a machine
 * imaged elsewhere, a support session, and the duty someone typed for seven in
 * the morning is stored as a different hour with nothing on the screen to say
 * so.
 *
 * # It stays here until a second application needs it
 *
 * Nothing about this is Workforce's: any application with a date-and-time
 * control needs the same conversion, and two copies of a zone calculation drift
 * exactly like two UUID generators do. The rule is **two or more consumers →
 * the package**, and Workforce is the first — so it lives here, and it was
 * announced rather than assumed (architect, 2026-09-20).
 *
 * > **The day a second application needs this, it moves to the SDK and this
 * > file loses it.**
 *
 * **Deleted, not wrapped.** A file left behind re-exporting the SDK's version
 * is the second copy the rule exists to prevent, wearing a forwarding address:
 * it keeps the old import path working, so nobody ever finds the callers, and
 * the next divergence has somewhere to live.
 */

/**
 * What a zone's offset is at a given instant, in milliseconds.
 *
 * Read from `Intl` rather than from a table: the offset is a function of the
 * instant, because a zone's rules change with the season and with the law.
 */
function offsetAt(instant: number, timeZone: string): number {
  const parts = new Intl.DateTimeFormat("en-US", {
    timeZone,
    hour12: false,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
  }).formatToParts(new Date(instant));

  const read = (type: Intl.DateTimeFormatPartTypes): number =>
    Number(parts.find((part) => part.type === type)?.value);

  // `hour12: false` renders midnight as `24` in some ICU versions, which is the
  // same moment as hour zero of the same day.
  const hour = read("hour") % 24;

  const wall = Date.UTC(
    read("year"), read("month") - 1, read("day"), hour, read("minute"), read("second"));

  return wall - instant;
}

/**
 * The instant a typed wall-clock moment names at the property.
 *
 * @param local what a `datetime-local` control holds — `YYYY-MM-DDTHH:mm`
 * @param timeZone the property's zone, as Master Data answered it
 * @returns the instant, ISO with a `Z`, or `null` when nothing can be
 *   established
 *
 * @remarks
 * **`null` rather than a machine-local reading.** A property whose zone Master
 * Data has not answered, or a control the browser has left half-typed, produces
 * *no instant* — and a caller that cannot send one says so. Falling back to the
 * machine's own zone is precisely the defect this function exists to remove,
 * and it would be silent.
 *
 * The offset is applied and then **read again at the answer**: an hour near a
 * daylight-saving change has a different offset before and after it, so the
 * first subtraction can land on the wrong side. The second reading settles it.
 * A wall time inside a spring-forward gap does not exist at the property at
 * all; this yields the instant the zone's own rules put it at rather than
 * refusing, because the control cannot express the gap and a form that rejected
 * a time the operator can see on the clock in front of them would be worse.
 */
export function instantAt(local: string, timeZone: string | null): string | null {
  if (timeZone === null) return null;

  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2}))?$/.exec(local);
  if (match === null) return null;

  const [y, mo, d, h, mi, s] = match.slice(1).map((part) =>
    part === undefined ? 0 : Number(part));

  const wall = Date.UTC(y!, mo! - 1, d!, h!, mi!, s!);

  let offset: number;
  try {
    offset = offsetAt(wall, timeZone);
  } catch {
    // An unknown zone name. Absent is an answer; a machine-local guess is not.
    return null;
  }

  const first = wall - offset;
  const second = wall - offsetAt(first, timeZone);

  return new Date(second).toISOString();
}

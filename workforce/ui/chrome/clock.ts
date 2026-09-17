import { formatClock, type PropertyEnvironment } from "@hotelos/sdk";

/**
 * Reading a time of day, in the property's own form.
 *
 * The sibling of `dates.ts`, and separate from it for the reason that file is
 * separate from a screen: a day and a clock time are different things. A day has
 * no time and no zone; a clock time has no date and no zone — a Morning shift
 * starts at 07:00 wherever the property is — which is why `formatClock` exists
 * beside `formatDay` and neither is `formatInstant`.
 *
 * # The separator is here because it is the reader's
 *
 * Four services joined the two ends themselves, three with a bare en-dash and
 * one with spaces around it — so the character, the spacing and the order were
 * decided in a service, in one culture, and a screen could not have said any of
 * it differently (ADR 0175). They send the two ends now.
 *
 * # And a span is here rather than in each screen
 *
 * A shift's hours are drawn on the rota grid, the shift picker, the catalogue,
 * the printed week and a dock widget. A range that reads two ways on two of them
 * is the drift page 64 exists to stop.
 */

/** The two ends of a span, as the wire carries them. */
export interface Span {
  /** The start, as a clock string. */
  from: string;

  /** The end. */
  to: string;
}

/**
 * A span of clock time, composed for a reader.
 *
 * @param span the two ends, or null when there is no span
 * @param property for the locale that decides the hour cycle
 * @returns the range as a person reads it, or null when there is nothing to say
 *
 * @remarks
 * **Null in, null out — never a placeholder.** What absence looks like belongs
 * to the screen showing it: an off day reads as an em-dash on the catalogue and
 * as nothing at all on a rota chip, and a service that chose one of those would
 * have chosen it for both.
 */
export function span(span_: Span | null, property: PropertyEnvironment): string | null {
  return span_ === null
    ? null
    : `${formatClock(span_.from, property)}–${formatClock(span_.to, property)}`;
}

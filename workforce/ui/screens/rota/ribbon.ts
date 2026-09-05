/**
 * The Manager-on-Duty ribbon — a timeline across the week.
 *
 * # Why it is not seven day cells
 *
 * `WF-Q8`, the owner's own sentence: *"we can't do per-day, because MOD may run
 * 8:00 pm to 8:00 am — it covers two dates."* A duty is a **span**, so the
 * ribbon positions each one by where it starts and how much of the week it
 * covers, and an overnight duty is one continuous bar rather than two halves
 * that a reader has to join up.
 *
 * # An uncovered stretch is drawn
 *
 * Not left blank. *"Nobody is on"* and *"nobody has entered it yet"* are
 * different answers, and a gap that looked like whitespace would be read as the
 * second when it is the first.
 */

import { formatInstant, type PropertyEnvironment } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import type { DutySpan } from "../../roster";

/**
 * Build the ribbon.
 *
 * @param spans the week's duties, in order
 * @returns the ribbon element
 */
export function ribbon(
  spans: readonly DutySpan[], property: PropertyEnvironment,
): HTMLElement {
  const row = el("div", "ribbon");
  const label = el("div", "rlab", "★ MOD");
  const bars = el("div", "bars");

  for (const span of spans) {
    bars.append(bar(span, property));
  }

  row.append(label, bars);
  return row;
}

/** One stretch, positioned as a fraction of the week. */
function bar(span: DutySpan, property: PropertyEnvironment): HTMLElement {
  const element = el("div", span.who === null ? "bar none" : "bar");

  element.style.left = `${(span.from * 100).toFixed(3)}%`;
  element.style.width = `${(span.span * 100).toFixed(3)}%`;

  if (span.who === null) {
    element.append(el("span", undefined, "no MOD"));
    return element;
  }

  element.append(el("span", undefined, span.who));

  if (span.department !== null) {
    element.append(el("s", undefined, span.department));
  }

  // The hours appear only when the span is not a plain day — which is exactly
  // when a reader needs them, and never as decoration on the six that are.
  if (span.startsAt !== null && span.endsAt !== null) {
    element.append(el("s", undefined,
      `${formatInstant(span.startsAt, property, "time")}`
      + `–${formatInstant(span.endsAt, property, "time")}`));
  }

  return element;
}

/**
 * A failure state's mark, drawn from the SDK's geometry — for a screen and a card.
 *
 * Two callers, which is why it is here and not in `marks.ts`: the screen's
 * failure and the widget's both draw it, and the widget bundle does not carry
 * the screen's stylesheet or its marks. The geometry is the SDK's, so three
 * applications cannot draw it three ways; the element is this module's, because
 * the SDK ships no DOM.
 */

import type { Cause, Glyph } from "@hotelos/sdk";

/** The three colours a failure's mark can take — the class the stylesheets key on. */
export type Tone = "wait" | "no" | "fault";

/**
 * The mark's colour, per cause — `64b` and `64e`, both owner-approved.
 *
 * ```text
 * unanswered                          wait    amber — waiting could work
 * forbidden · unadmitted · ungranted  no      grey  — 64e: "neutral grey, like
 *                                                     64b's refusal: none of them
 *                                                     is a fault or an outage"
 * faulted · undecidable               fault   red   — 64e draws the model state
 *                                                     `c-fault`
 * ```
 *
 * **Exhaustive, with no default, and that is the point.** The two call sites
 * this replaced asked *"is it unanswered? is it faulted?"* and let everything
 * else fall to grey — so when contract v2 added `undecidable`, the model state
 * drew as a refusal, grey where 64e draws red, and nothing failed. A seventh
 * cause now fails to compile here instead of choosing a colour silently.
 */
export function tone(cause: Cause): Tone {
  switch (cause) {
    case "unanswered":
      return "wait";

    case "forbidden":
    case "unadmitted":
    case "ungranted":
      return "no";

    case "faulted":
    case "undecidable":
      return "fault";
  }
}

/**
 * The mark as an SVG element.
 *
 * `createElementNS`, because `createElement("svg")` makes an unknown HTML
 * element that renders nothing — a silent blank where the mark belongs.
 *
 * @param glyph the SDK's geometry for the state
 * @param className the size the caller draws it at — 26 on a screen, 20 on a card
 * @returns the element, hidden from assistive technology because the label
 *   beside it says the same thing in words
 */
export function stateMark(glyph: Glyph, className: string): SVGElement {
  const NS = "http://www.w3.org/2000/svg";
  const svg = document.createElementNS(NS, "svg");

  svg.setAttribute("class", className);
  svg.setAttribute("viewBox", glyph.viewBox);
  svg.setAttribute("fill", "none");
  svg.setAttribute("stroke", "currentColor");
  svg.setAttribute("stroke-width", "1.6");
  svg.setAttribute("stroke-linecap", "round");
  svg.setAttribute("stroke-linejoin", "round");
  svg.setAttribute("aria-hidden", "true");

  for (const d of glyph.paths) {
    const path = document.createElementNS(NS, "path");
    path.setAttribute("d", d);
    svg.append(path);
  }

  return svg;
}

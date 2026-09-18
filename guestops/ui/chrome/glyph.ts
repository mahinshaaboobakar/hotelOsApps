/**
 * A failure state's mark, drawn from the SDK's geometry — for a screen and a card.
 *
 * Two callers, which is why it is here and not in `marks.ts`: the screen's
 * failure and the widget's both draw it, and the widget bundle does not carry
 * the screen's stylesheet or its marks. The geometry is the SDK's, so three
 * applications cannot draw it three ways; the element is this module's, because
 * the SDK ships no DOM.
 */

import type { Glyph } from "@hotelos/sdk";

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

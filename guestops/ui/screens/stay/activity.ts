/**
 * What happened, in order — the stay's activity timeline.
 *
 * The desk's answer to *"who said this, and when"*. Every entry carries a time,
 * a coloured dot naming where the fact came from, what happened, and who did
 * it — the same four marks the rest of the application uses, so a person reads
 * the timeline with the vocabulary they already have.
 *
 * The last entry is deliberately a state rather than an event
 * (*"Awaiting a decision"*): a disagreement that nobody has resolved is a fact
 * about the stay now, and a timeline that stopped at the last thing to happen
 * would end without saying the stay is still waiting.
 */

import type { Moment } from "../../book/model";
import { el } from "../../chrome/element";

/**
 * Draw the timeline.
 *
 * @param moments the entries, oldest first
 * @returns the timeline
 */
export function timeline(moments: readonly Moment[]): HTMLElement {
  const element = el("div", "tl");

  moments.forEach((moment, index) => {
    const entry = el("div", moment.tone === "none" ? "te" : `te ${moment.tone}`);

    const gutter = el("div", "g");
    gutter.append(el("i"));

    // The connecting rule stops at the last dot: a line continuing past the
    // final entry would promise something after it.
    if (index < moments.length - 1) gutter.append(el("u"));

    const said = el("div", "d");
    said.append(document.createTextNode(moment.what), el("span", undefined, moment.detail));

    entry.append(el("div", "t", moment.time), gutter, said);
    element.append(entry);
  });

  return element;
}

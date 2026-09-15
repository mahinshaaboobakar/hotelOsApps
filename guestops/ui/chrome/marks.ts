/**
 * The marks a value carries, and the four kinds the design keeps distinct.
 *
 * The gold frames use four visually different attachments and they mean
 * different things — which is why they are four functions here rather than one
 * styled string:
 *
 * ```text
 * mark   where a value came from        from Opera · override · disagrees
 * lock   how it was established         OBSERVED · FROM OPERA · DERIVED …
 * pill   a state of the thing           complete · same room · names look alike
 * link   an action inline in the value  reveal · ＋ assign · ＋ add
 * ```
 *
 * A `lock` is deliberately not a lock on editing — `FROM OPERA` says who
 * established the value, and staff may still write it. PMS-connected has never
 * meant read-only (GUEST-Q1, as amended).
 */

import { FAILURE_LABELS, type FailureDrawing } from "@hotelos/sdk";

import type { Chip, Tag } from "../book/model";
import { control, el } from "./element";

/**
 * A mark: a coloured dot and a word.
 *
 * @param chip the mark and its text
 * @returns the chip element
 */
export function mark(chip: Chip): HTMLElement {
  // Not a mark, and drawn as what it is. Frame 2 puts `penalty applied` in the
  // marks column with no dot, no tint and no border — a footnote about the row
  // rather than a claim about where the row came from.
  if (chip.mark === "note") {
    return el("span", "hint", chip.text);
  }

  const element = el("span", `sh ${chip.mark}`);

  // `missing` is a dashed outline with no dot: it marks an absence, and a dot
  // would give the absence a colour it has not earned.
  if (chip.mark !== "missing") element.append(el("i"));

  element.append(document.createTextNode(chip.text));
  return element;
}

/**
 * Render whatever a value carries, in the order the design places it.
 *
 * @param tags the attachments
 * @returns the elements, ready to append
 */
export function tags(list: readonly Tag[]): readonly HTMLElement[] {
  return list.map(one);
}

function one(tag: Tag): HTMLElement {
  if (tag.kind === "text") {
    return el("span", undefined, tag.text);
  }

  if (tag.kind === "lock") {
    // A lock naming something the platform cannot do is bad-toned — frames 4,
    // 6, 7 and 16. It is still a lock rather than a pill, because what it says
    // is *how this value was established*: by nothing.
    return el("span", tag.tone === "bad" ? "lock no" : "lock", tag.text);
  }

  if (tag.kind === "pill") {
    return el("span", `pill ${tag.tone}`, tag.text);
  }

  if (tag.kind === "link") {
    // A link is an action. It is a button for the same reason every control
    // here is: the mockup's `<span class="link">` is not reachable by keyboard.
    return control("link", tag.text);
  }

  return mark({ mark: tag.tone as Chip["mark"], text: tag.text });
}

/**
 * A read that did not answer, drawn as the failure it is.
 *
 * **This replaced `standIn`, and the difference is what is on the screen** —
 * `APPS-Q42`. `standIn` drew a banner above recorded values: a hotel's screen
 * full of names, rates and room numbers belonging to nobody, with a sentence
 * over it. Whoever read the sentence knew; whoever read the list did not, and
 * the list is what a person at a desk reads.
 *
 * So there is no data beneath this. The reason is the content, and it names
 * what failed rather than apologising: a capability the property declined says
 * so, and a platform that could not be reached says that.
 *
 * **It now draws the SDK's structured failure rather than a sentence** —
 * `38c5855e`. The heading used to be *"GuestOps could not load this"* whatever
 * had happened, which is the same sentence for a refusal, a timeout and a
 * fault — three different things with three different remedies, and a person
 * told the middle one waits for something that is never coming. The drawing
 * carries which it was, why, the wire line somebody can quote to whoever can
 * act on it, and whether a retry could work at all.
 *
 * @param drawing what the SDK says this failure looks like
 * @param retry offered only where the drawing says a retry could succeed
 * @returns the element to put where the data would have gone
 */
/**
 * A screen that cannot draw, for a reason that is not a read.
 *
 * **Separate from {@link failed} because it has no wire line and must not
 * invent one.** A stay opened without an id never reached the platform: there
 * is no capability, no method, no cause and no time of asking, and passing it
 * through the SDK's failure drawing would print a provenance line for a call
 * nobody made. That is the gap rule in a surface — a value standing in for a
 * measurement nobody took.
 *
 * @param said what a person is told
 * @param why what they can do about it
 * @returns the element to put where the screen would have gone
 */
export function cannot(said: string, why: string): HTMLElement {
  const box = el("div", "fail");
  box.append(el("div", "fh", said), el("div", "fb", why));
  return box;
}

export function failed(drawing: FailureDrawing, retry?: () => void): HTMLElement {
  const box = el("div", "fail");

  box.append(
    el("div", "fm", drawing.mark),
    el("div", "fh", drawing.said),
    el("div", "fb", drawing.why),

    // **Stated, never omitted.** A failure surface that shows no provenance
    // reads as one that did not bother to ask — and this line is the only part
    // a person can carry to somebody able to fix it.
    el("div", "fw", drawing.wire),
  );

  // No button on a refusal or a fault. Offering one is the second lie a failure
  // surface tells, after the first one that something was read at all.
  if (drawing.retryable && retry !== undefined) {
    box.append(control("btn sm", FAILURE_LABELS.retry, retry));
  }

  return box;
}

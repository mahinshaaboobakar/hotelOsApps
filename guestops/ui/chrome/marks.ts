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

import { type Fact, type FailureDrawing, type Glyph } from "@hotelos/sdk";

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
export function failed(drawing: FailureDrawing, retry?: () => void): HTMLElement {
  // The cause rides on the element so the mark takes the state's colour.
  const tone = drawing.cause === "unanswered" ? "wait"
    : drawing.cause === "faulted" ? "fault" : "no";
  const box = el("div", `fail ${tone}`);
  const doing = el("div", "fd");

  // **The affordance is what separates the three states, not the wording.** A
  // timeout gets a button because waiting can work; a refusal and a fault get a
  // sentence, because a retry on either is a promise the platform cannot keep.
  if (drawing.act.kind === "retry" && retry !== undefined) {
    doing.append(control("btn pri", drawing.act.label, retry));
  } else if (drawing.act.kind === "copy") {
    doing.append(control("btn", drawing.act.label, () => {
      void navigator.clipboard?.writeText(drawing.wire);
    }));
  }

  doing.append(el("div", "fn", drawing.act.note));

  box.append(
    stateMark(drawing.glyph),
    el("div", "fl", drawing.label),
    el("div", "fh", drawing.said),
    el("div", "fb", drawing.why),
    doing,

    // **The four facts, labelled.** They were one dotted line and the owner read
    // it as a log entry — every fact right, the most useful part set as the
    // least readable. Stated, never omitted: this is what a person carries to
    // whoever can act on it.
    facts(drawing.facts),
  );

  return box;
}

/**
 * The state's mark, drawn from the SDK's geometry.
 *
 * `createElementNS`, because `createElement("svg")` makes an unknown HTML
 * element that renders nothing — a silent blank where the mark belongs. The
 * geometry is the SDK's so three applications cannot draw it three ways; the
 * element is this module's, because the SDK ships no DOM.
 */
function stateMark(glyph: Glyph): SVGElement {
  const NS = "http://www.w3.org/2000/svg";
  const svg = document.createElementNS(NS, "svg");

  svg.setAttribute("class", "fg");
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

/** What was asked, what came back, and when — as a grid, not a line. */
function facts(list: readonly Fact[]): HTMLElement {
  const grid = el("dl", "fp");

  for (const fact of list) {
    grid.append(el("dt", undefined, fact.label), el("dd", undefined, fact.value));
  }

  return grid;
}

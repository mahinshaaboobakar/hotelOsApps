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
 * The banner shown when a screen is not reading the property's own data.
 *
 * **Always rendered when the data is recorded.** A person looking at a stay must
 * be able to tell whether they are seeing their hotel; a module that hid the
 * difference is one somebody eventually acts on.
 *
 * @param because the platform's own reason, when ADR 0041 permits showing it
 * @returns the banner
 */
export function standIn(because: string | null): HTMLElement {
  return el(
    "div",
    "stand",
    because
      ?? "Recorded example data — the desktop has no GuestOps client yet, so nothing here is this property's.",
  );
}

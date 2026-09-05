/**
 * Where each of the four numbers comes from — frame 14's left card.
 */

import { el, fill } from "../../chrome/element";
import { card } from "../../chrome/panel";

/**
 * The provenance of the answer, drawn beside it.
 *
 * It is on the screen rather than in a document because the numbers are the
 * seller's justification for a refusal: a receptionist telling a guest the
 * hotel cannot take them needs to know whether that is *we are full*, *a room
 * is broken* or *a manager held these back*, and the three have different
 * answers.
 *
 * @returns the card
 */
export function sources(): HTMLElement {
  const { root, body } = card("Where each number comes from");

  fill(
    body,
    row("Total rooms", "Master Data", "READ, NEVER COPIED"),
    row("Sold", "our own stays holding that type on those dates", null),
    row("Out of order", "EngineeringOps", "HEARD AS AN EVENT"),
    row("Stop-sell", "ours — room type + date range + reason", null),
    el(
      "div",
      "hint",
      "If the out-of-order projection is a few seconds behind, the answer is "
        + "conservative and no number anywhere becomes wrong. That is the "
        + "difference between an event-derived read model and a copy of "
        + "somebody else's table.",
    ),
  );

  return root;
}

function row(label: string, value: string, lock: string | null): HTMLElement {
  const element = el("div", "fr");
  const right = el("div", "v");

  right.append(document.createTextNode(value));

  if (lock !== null) {
    right.append(el("span", "lock", lock));
  }

  element.append(el("div", "k", label), right);
  return element;
}

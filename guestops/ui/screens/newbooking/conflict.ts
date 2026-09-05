/**
 * Assigning a room that already has a stay — frame 14's right card.
 */

import type { RoomConflict } from "../../book";
import { control, el, fill } from "../../chrome/element";
import { card } from "../../chrome/panel";

/**
 * The conflict check, and the reason it is a warning.
 *
 * **It warns and never forbids.** When staff answer *"two different stays"* to
 * a candidate link, the room is genuinely double-booked and that is the truth —
 * a hard block would make a ruled outcome unreachable. So it names the other
 * stay and lets a person decide.
 *
 * The same computation answers both of availability's questions — *how many of
 * this type are free* and *is this room free* — which is why this check runs on
 * every assignment, every move and every extension, in both modes, with no
 * branch.
 *
 * @param conflict the clash, or null when the room is free
 * @returns the card
 */
export function conflict(conflict_: RoomConflict): HTMLElement {
  const { root, body } = card(`Assigning room ${conflict_.room} · the conflict check`);

  const banner = el("div", "ban");
  const text = el("div");

  text.append(
    el("b", undefined, conflict_.headline),
    el("span", "why", conflict_.detail),
  );

  banner.append(text);

  const choices = el("div", "acts");
  choices.append(
    control("btn sm", "Pick another"),
    control("btn sm pri", "Assign anyway"),
  );

  fill(
    body,
    banner,
    choices,
    el(
      "div",
      "hint",
      "It warns and never forbids. When staff answer “two different stays” to a "
        + "candidate link, the room is genuinely double-booked and that is the "
        + "truth — a hard block would make a ruled outcome unreachable. So it "
        + "names the other stay and lets a person decide.",
    ),
  );

  return root;
}

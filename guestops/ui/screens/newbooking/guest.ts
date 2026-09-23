/**
 * The booking flow's third step — the guest, as the desk takes them.
 *
 * The fields are the ones `BookingService.CreateAsync`'s `NewGuest` already
 * takes, so nothing here invents a shape: a name as given, and a way to reach
 * them. **A booking with no name is valid** — the list draws *"Not yet
 * named"* — so the name is the only field this step insists on, and it insists
 * because a person standing at the desk has one.
 *
 * **No second guest yet, and that is visible rather than silent.** The approved
 * frame draws *"＋ add a guest in this room"*. One guest reaches the service
 * today; the control is drawn off with its reason rather than omitted, because
 * an absent control reads as a screen that never offered it.
 */

import { control, el, unavailable } from "../../chrome/element";

/** What the third step produces. */
export interface Guest {
  readonly name: string;
  readonly phone: string | null;
  readonly email: string | null;
}

/**
 * The guest card, with the controls that answer it.
 *
 * `next` is only offered a guest with a name — the card keeps the control
 * enabled and the service refuses an empty one, so the screen never claims a
 * booking was made that was not.
 */
export function guest(
  initial: Guest,
  back: () => void,
  next: (guest: Guest) => void,
): HTMLElement {
  const name = field("Guest name", "text", initial.name, "As the guest gives it");
  const phone = field("Phone", "tel", initial.phone ?? "", "optional");
  const email = field("Email", "email", initial.email ?? "", "optional");

  const card = el("div", "card");
  const body = el("div", "cb");

  const actions = el("div", "row");
  actions.append(
    control("btn", "Back", back),
    control("btn pri", "Review booking", () => next({
      name: name.value.trim(),
      phone: blank(phone.value),
      email: blank(email.value),
    })),
  );

  body.append(
    name.root,
    phone.root,
    email.root,
    unavailable("link", "＋ add a guest in this room",
      "A second guest in the same room is not available from this screen yet."),
    actions,
  );

  card.append(el("div", "ch", "Who is the booking for?"), body);
  return card;
}

function blank(value: string): string | null {
  return value.trim() === "" ? null : value.trim();
}

interface Field {
  readonly root: HTMLElement;
  readonly value: string;
}

function field(label: string, type: string, value: string, hint: string): Field {
  const input = document.createElement("input");
  input.type = type;
  input.value = value;
  input.placeholder = hint;

  const root = el("div", "fld");
  const box = el("div", "inp");
  box.append(input);
  root.append(el("label", undefined, label), box);

  return {
    root,
    get value() {
      return input.value;
    },
  };
}

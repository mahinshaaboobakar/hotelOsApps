/**
 * The confirm step — what will be created, and the one warning that belongs
 * here.
 *
 * ADR 0223, ruled 2026-09-23: treatment **C**. A room type the party does not
 * fit in is shown like any other and can be chosen; the warning arrives **at
 * the moment of commitment, once**, and the booking can still be made.
 *
 * **No capacity mark on the row, and this is the part to read before changing
 * anything here.** The first person who meets an unmarked row will want to add
 * one — that is treatment D, and it lost. D marked the row *and* warned here,
 * which makes the mistake visible twice and prevents it neither time; the owner
 * chose the single warning at the point where a person is committing rather
 * than browsing.
 *
 * **Nothing is recorded on the stay.** C's booking is an ordinary booking, so
 * there is no field for *booked over capacity* — the drawn D carried one and
 * the ruling settled it, which is why its absence is deliberate rather than
 * unimplemented. The overbooking record (`overbooked_knowingly`) is a different
 * fact about rooms rather than people and is untouched by this.
 */

import type { PropertyEnvironment } from "@hotelos/sdk";

import type { TypeAvailability } from "../../book/model";
import { control, el } from "../../chrome/element";
import { span } from "../../chrome/when";

/** What the desk has assembled by the time it reaches the confirm step. */
export interface Booking {
  readonly type: TypeAvailability;
  readonly arrive: string;
  readonly depart: string;
  readonly adults: number;
  readonly children: number;
  readonly guest: string;
  readonly phone: string | null;
}

/**
 * Whether this party is more than the room type sleeps.
 *
 * **`most`, not `included`** — the ceiling with an extra bed is what the room
 * can hold, and a type that sleeps two and takes a third on a bed is not a
 * warning. Master Data holding nothing for the type is **not** a warning
 * either: an absent occupancy is silence, and warning on silence would put a
 * sentence in front of a desk that no fact supports.
 */
export function exceeds(booking: Booking): boolean {
  return booking.type.sleeps !== null
    && booking.adults + booking.children > booking.type.sleeps.most;
}

/**
 * The confirm card: what will be created, the warning where one is owed, and
 * the two controls that answer it.
 *
 * A card that asks a question carries a control that answers it — the owner's
 * rule of 2026-09-23. `Create booking` is never disabled by the warning: C
 * informs, it does not prevent.
 */
export function confirm(
  booking: Booking,
  property: PropertyEnvironment,
  back: () => void,
  create: () => void,
): HTMLElement {
  const card = el("div", "card");
  const body = el("div", "cb");

  body.append(
    row("Guest", booking.phone === null ? booking.guest : `${booking.guest} · ${booking.phone}`),
    row("Room type", `${booking.type.roomType} · one room`),
    row("Dates", span(booking.arrive, booking.depart, property)),
    row("Guests", party(booking.adults, booking.children)),
  );

  if (exceeds(booking)) {
    body.append(el(
      "div",
      "note warn",
      `This room type sleeps ${booking.type.sleeps?.most} and you have entered `
      + `${booking.adults + booking.children} guests. It can still be booked.`));
  }

  const actions = el("div", "row");
  actions.append(
    control("btn", "Back", back),
    control("btn pri", "Create booking", create),
  );

  body.append(actions);
  card.append(el("div", "ch", "Create this booking?"), body);

  return card;
}

/** `2 adults, 1 child` — the count the first step collected, worded here. */
function party(adults: number, children: number): string {
  const people = [`${adults} ${adults === 1 ? "adult" : "adults"}`];

  if (children > 0) {
    people.push(`${children} ${children === 1 ? "child" : "children"}`);
  }

  return people.join(", ");
}

function row(label: string, value: string): HTMLElement {
  const element = el("div", "fr");
  element.append(el("div", "k", label), el("div", "v", value));
  return element;
}

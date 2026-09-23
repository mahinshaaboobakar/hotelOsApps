/**
 * The booking flow — the 05 frames, from dates to the booking that results.
 *
 * The owner approved this flow on 2026-09-19 and ruled its capacity behaviour
 * on 2026-09-23 (ADR 0223, treatment **C**). The steps are the frames':
 *
 * ```text
 * 1  dates and party      what is free is not asked until a person says when
 * 2  room type            every type, with what it sleeps
 * 3  guest                the name the booking is under
 * 4  confirm              the warning, where one is owed, and the two controls
 * 5  done                 the booking, open
 * ```
 *
 * **Why the flow holds its own state rather than the screen re-reading.** A
 * person typing a guest's name has already chosen dates and a type, and a
 * re-read between steps would re-ask the service and could return a different
 * answer mid-sentence. Availability is re-read only when a person asks it to be
 * — `Check again` — and at the write, where the service checks for itself.
 *
 * **Nothing here marks a room type against the party.** That is treatment D and
 * it lost; `confirm.ts` carries the reasoning where somebody would undo it.
 */

import type { HostApi } from "@hotelos/sdk";

import { APP, failureDrawing, load, perform, type Availability, type TypeAvailability }
  from "../../book";
import { el, fill } from "../../chrome/element";
import { failed } from "../../chrome/marks";
import { availability } from "./availability";
import { confirm, type Booking } from "./confirm";
import { guest, type Guest } from "./guest";
import { nights, opening, query, type Query } from "./query";

/** Where the flow is, and what it has collected. */
interface State {
  readonly query: Query;
  readonly types: readonly TypeAvailability[] | null;
  readonly type: TypeAvailability | null;
  readonly guest: Guest | null;
}

/**
 * Run the flow into an element.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element the flow owns
 * @param opened what to do when the booking exists: open it
 * @returns when the first step is drawn
 */
export async function flow(
  host: HostApi,
  into: HTMLElement,
  opened: (bookingId: string) => void,
): Promise<void> {
  let state: State = {
    query: opening(new Date()),
    types: null,
    type: null,
    guest: null,
  };

  const draw = (body: HTMLElement): void => {
    const head = el("div", "fltr");

    head.append(query(state.query, (asked) => {
      state = { ...state, query: asked, type: null };
      void search();
    }, state.types === null ? "Check availability" : "Check again"));

    const stage = el("div", "body unpaged");
    fill(stage, head, body);
    into.replaceChildren(stage);
  };

  const failure = (why: Parameters<typeof failureDrawing>[0], retry: () => void): void =>
    draw(failed(failureDrawing(why, { app: APP, the: "this property's availability" }), retry));

  async function search(): Promise<void> {
    const found = await load<Availability>(host, "reservation.read", "availability", {
      arrive: state.query.arrive,
      depart: state.query.depart,
    });

    if (!found.ok) {
      failure(found.failure, () => void search());
      return;
    }

    state = { ...state, types: found.value.types };
    draw(availability(found.value.types, (chosen) => {
      state = { ...state, type: chosen };
      draw(guest(
        state.guest ?? { name: "", phone: null, email: null },
        () => draw(availability(state.types ?? [], () => {})),
        (taken) => {
          state = { ...state, guest: taken };
          review();
        }));
    }));
  }

  function review(): void {
    const booking = made(state);
    if (booking === null) return;

    draw(confirm(booking, host.property, () => void search(), () => void create(booking)));
  }

  async function create(booking: Booking): Promise<void> {
    const done = await perform<{ created: boolean; bookingId: string | null }>(
      host, "stay.create", "book", {
        roomTypeId: booking.type.roomTypeId,
        arrives: booking.arrive,
        departs: booking.depart,
        adults: booking.adults,
        children: booking.children,
        guest: booking.guest,
        phone: booking.phone,
      });

    // **A refusal is the service's sentence, drawn where the person is** — it
    // is not retried and nothing is patched locally. The booking either exists
    // because the service says so, or the screen says why it does not.
    if (done.refused !== null || done.value === null) {
      draw(el("div", "note", done.refused ?? "The booking was not created."));
      return;
    }

    if (done.value.bookingId !== null) {
      opened(done.value.bookingId);
    }
  }

  draw(el("div", "hint", "Choose the dates and the party, then check what is free."));
}

/** The flow's state as a booking, once it holds every part of one. */
function made(state: State): Booking | null {
  if (state.type === null || state.guest === null) return null;
  if (nights(state.query.arrive, state.query.depart) === 0) return null;

  return {
    type: state.type,
    arrive: state.query.arrive,
    depart: state.query.depart,
    adults: state.query.adults,
    children: state.query.children,
    guest: state.guest.name,
    phone: state.guest.phone,
  };
}

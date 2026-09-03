/**
 * Today at the Desk — the day in four numbers and the next five arrivals.
 *
 * The one widget a receptionist glances at between guests. It answers *"what is
 * the shape of the shift"* and *"who walks in next"*, and nothing else: the
 * canvas draws four counts and five rows, and a sixth row would push the fifth
 * out of a card that has one size.
 *
 * **An arrival without a room shows the gap rather than a guess** — the
 * canvas's own footnote. A stay's anchor is the room *type* and the number is
 * an assignment made at check-in (GUEST-Q2's addendum), so `— unassigned` is
 * the ordinary morning case and the row a person acts on, not an error.
 */

import { connectToHost, type HostApi } from "@hotelos/sdk";

import { read, serve } from "../answer";
import { card, el, label, note, opener, row, stat, stylesheet } from "../card";
import { today as recorded } from "../recorded";

/** One arrival, as the desk reads it. */
interface Arrival {
  guest: string;
  /** Null until a room is assigned — drawn as the gap, never as a guess. */
  room: string | null;
  at: string;
  stay: string;
}

/** The day. Any count may be absent; the domain answers what it can. */
interface Today {
  dueIn: number | null;
  arrived: number | null;
  dueOut: number | null;
  departed: number | null;
  arrivals: readonly Arrival[];
}

connectToHost((host: HostApi) => {
  let root: HTMLElement | null = null;
  let stop: (() => void) | null = null;
  const open = opener(host, () => root);

  async function draw(into: HTMLElement): Promise<void> {
    const answer = await read<Today>(host, "reservation.read", "today", recorded);
    const day = answer.value;

    const { root: frame, body } = card("Today at the Desk");

    const counts = el("div", "sr");
    for (const tile of [
      stat(day.dueIn, "due in"),
      stat(day.arrived, "arrived", "ok"),
      stat(day.dueOut, "due out"),
      stat(day.departed, "departed"),
    ]) {
      if (tile !== null) counts.append(tile);
    }

    body.append(counts, label("Next five arrivals"));

    // Five, because the card has one size and the canvas drew five. A widget
    // that grew with the day would push its own last row out of sight.
    for (const arrival of day.arrivals.slice(0, 5)) {
      body.append(row(
        [
          arrival.guest,
          arrival.room === null
            ? el("span", "rc miss", "— unassigned")
            : el("span", "rc", arrival.room),
          el("span", "rc t", arrival.at),
        ],
        `stay/${arrival.stay}`,
        open,
      ));
    }

    body.append(note(answer.live
      ? "Arrivals without a room show the gap rather than a guess."
      : "Example figures — this desk has no GuestOps data yet."));

    into.replaceChildren(stylesheet(), frame);
  }

  return {
    mount(element) {
      root = element;
      stop = serve(host, element, (surface) => void draw(surface));
    },

    unmount() {
      stop?.();
      stop = null;
      root = null;
    },
  };
});

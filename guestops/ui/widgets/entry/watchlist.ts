/**
 * Watchlist — what nobody was thinking about.
 *
 * **This one never stacks.** Its job is telling a receptionist about the
 * overdue departure they were *not* considering; behind a flip it only works
 * when they already suspected something, which is exactly when they did not
 * need it. A warning nobody flipped to is a warning that did not happen, so the
 * manifest declares `stackable: false` and the shell honours it — nobody can
 * bury this by dragging it into a stack.
 *
 * Every row taps to the stay it is about, not to a list: the person reading this
 * has already decided to act on one guest.
 */

import { connectToHost, type HostApi } from "@hotelos/sdk";

import { read, serve } from "../answer";
import { card, el, label, note, opener, row, stat, stylesheet } from "../card";
import { watchlist as recorded } from "../recorded";

/** A departure that has not happened, and how late it is. */
interface Overdue {
  room: string;
  guest: string;
  due: string;
  late: string;
  stay: string;
}

/** An arrival today with no room yet. */
interface Unassigned {
  guest: string;
  type: string;
  at: string;
  stay: string;
}

interface Watchlist {
  overdueOut: number | null;
  noRoom: number | null;
  notCheckedOut: number | null;
  overdue: readonly Overdue[];
  unassigned: readonly Unassigned[];
}

connectToHost((host: HostApi) => {
  let root: HTMLElement | null = null;
  let stop: (() => void) | null = null;
  const open = opener(host, () => root);

  async function draw(into: HTMLElement): Promise<void> {
    const answer = await read<Watchlist>(host, "reservation.read", "watchlist", recorded);
    const list = answer.value;

    const { root: frame, body } = card("Watchlist");

    const counts = el("div", "sr three");
    for (const tile of [
      stat(list.overdueOut, "overdue out", "bad"),
      stat(list.noRoom, "no room", "warn"),
      stat(list.notCheckedOut, "not checked out", "warn"),
    ]) {
      if (tile !== null) counts.append(tile);
    }

    body.append(counts);

    if (list.overdue.length > 0) {
      body.append(label("Overdue departures, still in house"));

      for (const late of list.overdue.slice(0, 3)) {
        body.append(row(
          [
            `${late.room} · ${late.guest}`,
            el("span", "rc", late.due),
            el("span", "rc late", late.late),
          ],
          `stay/${late.stay}`,
          open,
        ));
      }
    }

    if (list.unassigned.length > 0) {
      body.append(label("Arriving today, no room assigned"));

      for (const waiting of list.unassigned.slice(0, 2)) {
        body.append(row(
          [
            `${waiting.guest} · ${waiting.type}`,
            el("span", "rc t", waiting.at),
            el("span", "rc miss", "—"),
          ],
          `stay/${waiting.stay}`,
          open,
        ));
      }
    }

    // No footer when the data is the property's: the canvas gives this card
    // none, because a watchlist with nothing to caveat should end at its last
    // row rather than explain itself.
    if (!answer.live) {
      body.append(note("Example figures — this desk has no GuestOps data yet."));
    }

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

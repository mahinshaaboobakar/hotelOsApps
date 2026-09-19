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

import { failureDrawing, load } from "@hotelos/sdk";

import { APP } from "../../app";
import { instant } from "../../chrome/when";
import { serve } from "../mount";
import { card, el, label, opener, row, stat, stylesheet, unanswered } from "../card";

/** A departure that has not happened, and how late it is. */
interface Overdue {
  /** Null where the stay holds no room — the service's own absence. */
  room: string | null;
  guest: string;

  /** When it was due out — an ISO instant, or null when never recorded. */
  due: string | null;

  /** How late, as the service computes it (I5) — null until it does. */
  late: string | null;

  stay: string;
}

/** An arrival today with no room yet. */
interface Unassigned {
  guest: string;

  /** Null where the room type's name could not be read. */
  type: string | null;

  /** When they are expected — an ISO instant, or null when never recorded. */
  at: string | null;

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
    const answer = await load<Watchlist>(host, "reservation.read", "watchlist");

    // A read that did not answer IS the card — APPS-Q42. The canvas is
    // 320x384 and does not scroll, so a failure cannot sit above content;
    // it takes the place of it.
    if (!answer.ok) {
      into.replaceChildren(stylesheet(), unanswered(
        "Watchlist",
        failureDrawing(answer.failure, { app: APP, the: "this property's watchlist" }),
        // **No screen makes this card's read** — see occupancy.ts; reported.
        { retry: () => void draw(into), open: () => open("today") },
      ));
      return;
    }
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
            // The parts the service has, joined — a null room drew the word
            // "null" here while the type said the field could not be absent.
            [late.room, late.guest].filter((part) => part !== null).join(" · "),
            el("span", "rc", late.due === null ? instant(null, host.property, "time")
              : `due ${instant(late.due, host.property, "time")}`),
            ...(late.late === null ? [] : [el("span", "rc late", late.late)]),
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
            [waiting.guest, waiting.type].filter((part) => part !== null).join(" · "),
            el("span", "rc t", instant(waiting.at, host.property, "time")),
            el("span", "rc miss", "—"),
          ],
          `stay/${waiting.stay}`,
          open,
        ));
      }
    }

    // **No footer at all now.** The canvas gave this card none when the data
    // was the property's, and an examples footnote when it was not — and the
    // second case no longer reaches here: a read that does not answer draws the
    // failure card instead. What is left ends at its last row, which is what the
    // drawing always wanted.

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

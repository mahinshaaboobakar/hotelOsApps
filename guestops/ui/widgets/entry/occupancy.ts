/**
 * Occupancy — how full the hotel is tonight, and where.
 *
 * A widget you go and look at rather than one that catches you, so it stacks:
 * missing it for an hour costs nothing, and a person checks it when they are
 * deciding whether to sell another room.
 *
 * **By floor is not drawn**, and the canvas says so in its own footer. GuestOps
 * counts rooms by *type* — the type is the stay's anchor (GUEST-Q2) and the
 * floor belongs to Master Data's structural hierarchy. A per-floor figure here
 * would be this application answering a question it cannot honestly compute,
 * which is the rule that decided the row rather than a limitation to apologise
 * for.
 */

import { connectToHost, type HostApi } from "@hotelos/sdk";

import { failureDrawing, load } from "@hotelos/sdk";

import { APP } from "../../app";
import { serve } from "../mount";
import { card, el, label, opener, row, stat, stylesheet, unanswered } from "../card";

/** One room type, and how much of it is sold. */
interface TypeRow {
  name: string;
  rooms: number;
  sold: number;
}

interface Occupancy {
  inHouse: number | null;
  occupied: number | null;
  free: number | null;
  tonight: number | null;
  types: readonly TypeRow[];
}

/**
 * How many room types the canvas fits below the counts and the bar.
 *
 * **A bound, because `now.types` has none.** Room types are configured by the
 * property: three on the fixture desk, and a resort with eight would have had
 * five rows drawn into a body that holds three and cut by `overflow:hidden` —
 * invisibly, since a widget has no scrollbar to hint that something is below.
 * The label carries the cut, so what is shown reads as *the largest three*
 * rather than as every type the hotel has.
 */
const SHOWN = 3;

connectToHost((host: HostApi) => {
  let root: HTMLElement | null = null;
  let stop: (() => void) | null = null;
  const open = opener(host, () => root);

  async function draw(into: HTMLElement): Promise<void> {
    const answer = await load<Occupancy>(host, "reservation.read", "occupancy");

    // A read that did not answer IS the card — APPS-Q42. The canvas is
    // 320x384 and does not scroll, so a failure cannot sit above content;
    // it takes the place of it.
    if (!answer.ok) {
      into.replaceChildren(stylesheet(), unanswered(
        "Occupancy",
        failureDrawing(answer.failure, { app: APP, the: "this property's occupancy" }),
        // **No screen makes this card's read**, so the one opened cannot show
        // this failure's facts — the approved divergence assumes it can.
        // Reported with 0.3.1 rather than papered over here.
        { retry: () => void draw(into), open: () => open("today") },
      ));
      return;
    }
    const now = answer.value;

    const { root: frame, body } = card("Occupancy");

    const counts = el("div", "sr");
    for (const tile of [
      stat(now.inHouse, "in house"),
      stat(now.occupied, "occupied"),
      stat(now.free, "free", "ok"),
      stat(now.tonight, "tonight"),
    ]) {
      if (tile !== null) counts.append(tile);
    }

    body.append(counts);

    // The bar is the sold share of what exists, and it is drawn only when both
    // halves are known: a bar filled from one number and a guess is a picture
    // that lies more convincingly than a wrong figure would.
    if (now.occupied !== null && now.free !== null && now.occupied + now.free > 0) {
      const bar = el("div", "bar");
      const fill = el("i");
      fill.style.width = `${Math.round((now.occupied / (now.occupied + now.free)) * 100)}%`;
      bar.append(fill);
      body.append(bar);
    }

    body.append(label(`The ${String(SHOWN)} largest room types`));

    for (const type of now.types.slice(0, SHOWN)) {
      body.append(row(
        [type.name, el("span", "rc", `${type.rooms} rooms`), el("span", "rc t", String(type.sold))],
        `rooms/${type.name.toLowerCase()}`,
        open,
      ));
    }

    // A footnote on what the card leaves out and why stood here — a note for
    // the developer, removed under the owner's ruling of 2026-09-19: a mock's notes for the developer are never built as screen.

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

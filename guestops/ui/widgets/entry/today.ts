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

import { failureDrawing, load } from "@hotelos/sdk";

import { APP } from "../../app";
import { instant } from "../../chrome/when";
import { serve } from "../mount";
import { card, el, label, note, opener, row, stat, stylesheet, unanswered } from "../card";

/** One arrival, as the desk reads it. */
interface Arrival {
  guest: string;
  /** Null until a room is assigned — drawn as the gap, never as a guess. */
  room: string | null;

  /** When they are expected — an ISO instant, or null when never recorded. */
  at: string | null;

  stay: string;
}

/**
 * The day, from the widget's own read — `desk`.
 *
 * It read `today`, the screen's read, which has never sent these fields: on a
 * property every count was absent and the list empty, and only the harness's
 * fixture had this shape (found 2026-09-19). `DeskView` sends exactly this.
 * Any count may be null — unknown, which the card draws as nothing, never as 0.
 * The four are disjoint: a guest who has arrived is no longer due in.
 */
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
    const answer = await load<Today>(host, "reservation.read", "desk");

    // A read that did not answer IS the card — APPS-Q42. The canvas is
    // 320x384 and does not scroll, so a failure cannot sit above content;
    // it takes the place of it.
    if (!answer.ok) {
      into.replaceChildren(stylesheet(), unanswered(
        "Today at the Desk",
        failureDrawing(answer.failure, { app: APP, the: "today at this property" }),
        // The screen that makes the same read, so it carries the same facts.
        { retry: () => void draw(into), open: () => open("today") },
      ));
      return;
    }
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
          el("span", "rc t", instant(arrival.at, host.property, "time")),
        ],
        `stay/${arrival.stay}`,
        open,
      ));
    }

    body.append(note("Arrivals without a room show the gap rather than a guess."));

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

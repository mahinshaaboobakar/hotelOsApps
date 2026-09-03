/**
 * From the PMS — the feed's pulse, and what it could not place.
 *
 * # This widget exists to make silence visible
 *
 * A feed that stopped sending is precisely what nobody goes looking for, which
 * is why this one never stacks: a warning behind a flip only works when the
 * reader already suspected something, and by then they did not need it.
 *
 * # The row the canvas got backwards, and the ruling that fixed it
 *
 * The approved artboard draws **"Last fact held 09:12"** and admits in its own
 * footer that *a healthy feed leaves no timestamp*. That was the inversion:
 * `HeldFact.ReceivedAt` is written only when a fact **fails**, so a property
 * whose feed is perfectly healthy holds nothing, has no timestamp, and this
 * widget reported "never" exactly when the wire was fine. **It looked worst
 * when things were best.**
 *
 * Ruled 2026-09-03: GuestOps stamps `InboundFeedMark` at arrival, before the
 * decision, so the row below is **"Last fact received"** and a live feed shows a
 * recent time. The silence-visible promise stands and now actually works —
 * silence is an ageing timestamp rather than an absent one.
 *
 * This is a **deliberate divergence from the approved canvas**, carried in the
 * audit as ruled rather than missed.
 */

import { connectToHost, type HostApi } from "@hotelos/sdk";

import { read, serve } from "../answer";
import { card, el, label, note, opener, row, stat, stylesheet } from "../card";
import { pms as recorded } from "../recorded";

/** One fact the Hub could not place, and why. */
interface Held {
  reason: string;
  source: string;
  at: string;
  stay: string;
}

interface Feed {
  newToday: number | null;
  held: number | null;

  /**
   * When this property last heard anything at all — null only when it never has.
   *
   * Absence and an ageing time are different facts and stay different values:
   * a property nobody has ever sent a fact to has not gone quiet.
   */
  lastFactAt: string | null;

  facts: readonly Held[];
}

connectToHost((host: HostApi) => {
  let root: HTMLElement | null = null;
  let stop: (() => void) | null = null;
  const open = opener(host, () => root);

  async function draw(into: HTMLElement): Promise<void> {
    const answer = await read<Feed>(host, "reservation.read", "feed", recorded);
    const feed = answer.value;

    const { root: frame, body } = card("From the PMS");

    const counts = el("div", "sr three");
    for (const tile of [stat(feed.newToday, "new today"), stat(feed.held, "held", "warn")]) {
      if (tile !== null) counts.append(tile);
    }

    body.append(counts, label("Held facts — nothing else is recorded"));

    for (const fact of feed.facts.slice(0, 3)) {
      body.append(row(
        [fact.reason, el("span", "rc", fact.source), el("span", "rc t", fact.at)],
        `attention/${fact.stay}`,
        open,
      ));
    }

    // The ruled row. Absent only when the property has never been sent
    // anything — which the honesty rule says to draw as nothing rather than as
    // "never", because "never" is a claim about a feed that may not exist yet.
    if (feed.lastFactAt !== null) {
      body.append(row(
        ["Last fact received", el("span", "rc t", feed.lastFactAt)],
        "attention",
        open,
      ));
    }

    body.append(note(answer.live
      ? "Amended and cancelled are not drawn — see the report."
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

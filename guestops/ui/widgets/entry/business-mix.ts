/**
 * Business Mix — where today's arrivals came from.
 *
 * Channel and market code, both **in the source's own words**. The segment
 * every hotel reports on is only reportable if it survives the journey
 * unaltered, so nothing here is normalised to a vocabulary this platform
 * invented: `OTA`, `CORP` and `LEIS` are the PMS's spellings and this widget
 * shows them unchanged.
 *
 * It reads `StaySource`, which is where the Hub's `CommercialSegment` lands.
 * The canvas's footer says *"There is no CommercialSegment type"* — true when
 * the artboard was drawn, and not since: DD added the message and this domain
 * carries it. Stale in the good direction, and corrected below.
 *
 * A meal plan is carried on the same record and is **not drawn**: the canvas
 * gives the card two lists and a third would push one off a fixed-size popover.
 * That is a drawing decision, not a data gap — the figure exists.
 */

import { connectToHost, type HostApi } from "@hotelos/sdk";

import { read, serve } from "../answer";
import { card, el, label, note, opener, row, stylesheet } from "../card";
import { mix as recorded } from "../recorded";

/** One line of the mix: a code the source sent, and how many arrived on it. */
interface Line {
  name: string;
  count: number;
}

interface Mix {
  channels: readonly Line[];
  markets: readonly Line[];
}

connectToHost((host: HostApi) => {
  let root: HTMLElement | null = null;
  let stop: (() => void) | null = null;
  const open = opener(host, () => root);

  async function draw(into: HTMLElement): Promise<void> {
    const answer = await read<Mix>(host, "reservation.read", "mix", recorded);
    const mix = answer.value;

    const { root: frame, body } = card("Business Mix");

    body.append(label("Today's arrivals by channel"));
    for (const line of mix.channels) {
      body.append(row(
        [line.name, el("span", "rc t", String(line.count))],
        `arrivals/channel/${line.name.toLowerCase()}`,
        open,
      ));
    }

    body.append(label("By market code"));
    for (const line of mix.markets) {
      body.append(row(
        [line.name, el("span", "rc t", String(line.count))],
        `arrivals/market/${line.name.toLowerCase()}`,
        open,
      ));
    }

    body.append(note(answer.live
      ? "From StaySource — the source's own channel and market code, unaltered."
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

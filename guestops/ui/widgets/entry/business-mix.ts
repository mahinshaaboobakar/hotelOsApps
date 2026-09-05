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

/**
 * How many of each list the canvas fits.
 *
 * **Three, because three fits** — page 56's rule, and the reason it is a
 * constant rather than the length of whatever the domain returned. This widget
 * drew every channel and every market and let `overflow:hidden` swallow the
 * remainder: seven rows into a body that holds five, the last two cut with
 * nothing to say they had been. A row a person cannot see is worse than a row
 * not drawn, because only one of them is honest about being absent.
 *
 * **The cut is stated on each label** — `top 3` — rather than in the footnote
 * below both lists. A footnote covering two lists is a footnote a reader has to
 * carry back up to them; the label is on the thing it applies to, and it leaves
 * the footnote free for what the widget is actually about.
 */
const SHOWN = 3;

connectToHost((host: HostApi) => {
  let root: HTMLElement | null = null;
  let stop: (() => void) | null = null;
  const open = opener(host, () => root);

  async function draw(into: HTMLElement): Promise<void> {
    const answer = await read<Mix>(host, "reservation.read", "mix", recorded);
    const mix = answer.value;

    const { root: frame, body } = card("Business Mix");

    body.append(label(`Arrivals by channel · top ${String(SHOWN)}`));
    for (const line of mix.channels.slice(0, SHOWN)) {
      body.append(row(
        [line.name, el("span", "rc t", String(line.count))],
        `arrivals/channel/${line.name.toLowerCase()}`,
        open,
      ));
    }

    body.append(label(`By market code · top ${String(SHOWN)}`));
    for (const line of mix.markets.slice(0, SHOWN)) {
      body.append(row(
        [line.name, el("span", "rc t", String(line.count))],
        `arrivals/market/${line.name.toLowerCase()}`,
        open,
      ));
    }

    body.append(note(answer.live
      ? "In the source's own words, never normalised."
      : "Example figures — no GuestOps data on this desk yet."));

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

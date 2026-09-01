/**
 * GuestOps's desktop module — the reservation book, as a packaged application.
 *
 * # What this is
 *
 * A **package's** UI, not the shell's: it is built in the package, ships in
 * `ui/`, and runs in its own iframe realm (ADR 0128 §7's addendum). Its only
 * connection to HotelOS is `@hotelos/sdk` and the port the host transfers in —
 * there is no ambient capability here, so no database, no tuple writer and no
 * route past the Hub, because **no name is bound to those things in this
 * realm**.
 *
 * # No framework, deliberately
 *
 * A hosted module *may* bring React — across realms a second copy is no longer
 * a defect. This one does not, for two reasons that are about this repository
 * rather than about taste: the apps repository has no TypeScript build of any
 * kind yet, so a framework would mean inventing a bundler pipeline nothing
 * consumes; and `hello-hotel` — the only shipped example of the contract — is
 * plain DOM. Bringing React is a decision for whoever wires the loader, not one
 * to make first and leave for them.
 *
 * # It is styled, never themed
 *
 * Every colour is a `var()` on the host's injected tokens. That is bound 1 as
 * it can actually be enforced across a realm boundary: an installed application
 * looks like HotelOS because the platform styles it, not because it renders the
 * platform's components. See `chrome.ts` for the token names — and for the one
 * finding about them.
 *
 * # This module composes and holds no screen
 *
 * ADR 0042. `today`, `stay` and `attention` are one file each; what they share
 * is `chrome.ts` for the look and `book.ts` for the single data seam.
 */

import type { Activate, HostApi, HostedModule } from "@hotelos/sdk";

import { rail, stylesheet } from "./chrome";
import { recordedAttention, type Stay } from "./book";
import { attention } from "./screens/attention";
import { stay } from "./screens/stay";
import { today } from "./screens/today";

/** Rendered by the host into the module's own document. */
export const activate: Activate = (host: HostApi): HostedModule => {
  let root: HTMLElement | null = null;

  /**
   * Draw one screen.
   *
   * Every screen is asynchronous because every answer is in another realm. The
   * chrome is drawn first and the screen fills itself in: a module that renders
   * nothing until the platform answers renders nothing at all when the platform
   * is slow.
   */
  function show(screen: string, chosen: Stay | null): void {
    if (root === null) return;

    const frame = document.createElement("div");
    frame.className = "go";

    const counts = { Attention: String(recordedAttention.length) };
    frame.append(rail(screen === "Stay" ? "Today" : screen, counts, (next) => show(next, null)));

    const main = document.createElement("div");
    main.className = "main";
    frame.append(main);

    root.replaceChildren(frame);

    if (screen === "Stay" && chosen !== null) {
      void stay(host, chosen, main, () => show("Today", null));
      return;
    }

    if (screen === "Attention") {
      void attention(host, main);
      return;
    }

    if (screen === "Today") {
      void today(host, main, (picked) => show("Stay", picked));
      return;
    }

    // Bookings and Guests are drawn in the gold design and are not built: their
    // screens are the next slice's, and a tile that opened an empty page would
    // be worse than one that says so.
    main.replaceChildren(soon(screen));
  }

  return {
    mount(element) {
      root = element;
      root.append(stylesheet());
      show("Today", null);
    },

    unmount() {
      root = null;
    },
  };
};

/** What an unbuilt screen says, rather than showing an empty page. */
function soon(screen: string): HTMLElement {
  const head = document.createElement("div");
  head.className = "head";

  const title = document.createElement("div");
  const heading = document.createElement("div");
  heading.className = "ht";
  heading.textContent = screen;

  const sub = document.createElement("div");
  sub.className = "hsub";
  sub.textContent = "Drawn in the gold design; not built in this slice.";

  title.append(heading, sub);
  head.append(title);
  return head;
}

export default activate;

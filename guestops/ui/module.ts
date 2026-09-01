/**
 * GuestOps's desktop module — the reservation book, as a packaged application.
 *
 * # What this is
 *
 * A **package's** UI, not the shell's: built in the package, shipped in `ui/`,
 * and run in its own iframe realm (ADR 0128 §7's addendum). Its only connection
 * to HotelOS is `@hotelos/sdk` and the port the host transfers in — there is no
 * ambient capability here, so no database, no tuple writer and no route past
 * the Hub, because **no name is bound to those things in this realm**.
 *
 * # No framework, deliberately
 *
 * A hosted module *may* bring React — across realms a second copy is no longer
 * a defect. This one does not: the apps repository has no TypeScript build of
 * any kind, so a framework would mean inventing a bundler pipeline nothing
 * consumes, and `hello-hotel` — the only shipped example of the contract — is
 * plain DOM. Bringing React is a decision for whoever wires the loader.
 *
 * # It is styled, never themed
 *
 * Every colour is a `var()` on the host's injected tokens, and only on names
 * the shell publishes (SHELL-Q30). That is bound 1 as it can be enforced across
 * a realm: an installed application looks like HotelOS because the platform
 * styles it, not because it renders the platform's components.
 *
 * # Navigation is the rail and the tabs
 *
 * There is no back button. The rail keeps `Today` lit while a stay is open — a
 * stay is reached *from* the day and belongs to it — so the way back is the way
 * in, which is how the approved design navigates.
 *
 * # This file composes and holds no screen
 *
 * ADR 0042. The three screens are directories of their own; what they share is
 * `chrome/` for the drawing and `book/` for the single data seam.
 */

import type { Activate, HostApi, HostedModule } from "@hotelos/sdk";

import { recordedAttention, recordedToday } from "./book";
import { el } from "./chrome/element";
import { rail, type RailItem } from "./chrome/rail";
import { stylesheet } from "./chrome/styles";
import { attention } from "./screens/attention";
import { stay } from "./screens/stay";
import { today } from "./screens/today";

/** Where the module is. A stay keeps `Today` lit in the rail. */
interface Place {
  screen: "Today" | "Bookings" | "Guests" | "Attention" | "Stay";
  list: string;
  tab: string;
}

/** Who is signed in, drawn at the rail's foot. */
const OPERATOR = { name: "Anitha Menon", where: "Front Office · Avenue Regent" };

/** Rendered by the host into the module's own document. */
export const activate: Activate = (host: HostApi): HostedModule => {
  let root: HTMLElement | null = null;

  // Held, not appended once. `show` replaces the root's children on every
  // screen change, so a stylesheet appended at mount is deleted by the first
  // render — the module then draws itself as an unstyled column, and neither
  // the type-check nor the suite can see it.
  const style = stylesheet();

  let where: Place = { screen: "Today", list: "Arrivals", tab: "Overview" };

  function show(next: Partial<Place>): void {
    if (root === null) return;
    where = { ...where, ...next };

    const frame = el("div", "go");
    const main = el("div", "main");

    frame.append(
      rail(items(), where.screen === "Stay" ? "Today" : where.screen, OPERATOR, (label) =>
        show({ screen: label as Place["screen"], list: "Arrivals" })),
      main,
    );

    root.replaceChildren(style, frame);
    draw(main);
  }

  function draw(main: HTMLElement): void {
    if (where.screen === "Stay") {
      void stay(host, main, where.tab, (tab) => show({ tab }));
      return;
    }

    if (where.screen === "Attention") {
      void attention(host, main);
      return;
    }

    if (where.screen === "Today") {
      void today(
        host,
        main,
        where.list,
        (list) => show({ list }),
        () => show({ screen: "Stay", tab: "Overview" }),
      );
      return;
    }

    main.replaceChildren(unbuilt(where.screen));
  }

  return {
    mount(element) {
      root = element;
      show({});
    },

    unmount() {
      root = null;
    },
  };
};

/**
 * The rail's entries and their counts.
 *
 * Counts come from the same recorded facts the screens read, so the rail cannot
 * claim a number the list does not show. When the client lands they arrive
 * through the same seam.
 */
function items(): readonly RailItem[] {
  return [
    { label: "Today", count: recordedToday.businessDate.split(" ").slice(1).join(" ") },
    { label: "Bookings", count: "218" },
    { label: "Guests", count: "1 904" },
    { label: "Attention", count: String(recordedAttention.length), attention: true },
  ];
}

/** A screen the approved design draws and this slice does not build. */
function unbuilt(screen: string): HTMLElement {
  const head = el("div", "head");
  const title = el("div");

  title.append(
    el("div", "ht", screen),
    el("div", "hsub", "Drawn in the approved design; not built in this slice."),
  );

  head.append(title);
  return head;
}

export default activate;

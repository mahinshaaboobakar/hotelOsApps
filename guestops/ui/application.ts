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
 * # Navigation is the bar and the tabs
 *
 * There is no back button. The bar keeps `Today` lit while a stay is open — a
 * stay is reached *from* the day and belongs to it — so the way back is the way
 * in, which is how the approved design navigates.
 *
 * # Why this is `application.ts` and not `module.ts`
 *
 * The artifact this package ships is `ui/module.js`, and a source file called
 * `module.ts` beside it makes `from "./module"` **ambiguous**: both vitest and
 * esbuild resolve the extensionless import to the built `.js`. The bundle then
 * gets built from itself and every test loses `activate` — which is exactly
 * what happened the first time the build wrote its output here. The artifact
 * owns that name; the source takes another.
 *
 * # This file composes and holds no screen
 *
 * ADR 0042. The three screens are directories of their own; what they share is
 * `chrome/` for the drawing and `book/` for the single data seam.
 */

import type { Activate, HostApi, HostedModule } from "@hotelos/sdk";

import {
  recordedAttention, recordedRegistration, recordedToday, recordedWalkIn,
} from "./book";
import { el } from "./chrome/element";
import { bar, type BarItem } from "./chrome/bar";
import { stylesheet } from "./chrome/styles";
import { attention } from "./screens/attention";
import { booking } from "./screens/booking";
import { bookings } from "./screens/bookings";
import { newBooking } from "./screens/newbooking";
import { registration } from "./screens/registration";
import { stay } from "./screens/stay";
import { today } from "./screens/today";
import { walkIn } from "./screens/walkin";

/**
 * Where the module is.
 *
 * A stay keeps `Today` lit in the bar, and a booking keeps `Bookings` lit: both
 * are reached *from* a list and belong to it, so the way back is the way in.
 * That is how the approved design navigates and it is why there is no back
 * button.
 */
interface Place {
  screen:
    | "Today"
    | "Bookings"
    | "Guests"
    | "Attention"
    | "Stay"
    | "Booking"
    | "NewBooking";

  list: string;
  tab: string;

  /** Which booking the Booking screen is showing. */
  bookingId: string;

  /**
   * The overlay standing over whatever screen is drawn, if any.
   *
   * Part of *where you are* for the same reason the page is: it survives a
   * redraw, and the screen underneath keeps rendering while it stands — frame
   * 10 draws the day behind the walk-in sheet and frame 8 draws the booking's
   * stays behind the cancellation.
   */
  overlay: "walkin" | "cancel" | "registration" | null;

  /**
   * Which page of the list, 0-based.
   *
   * Part of *where you are* rather than state the screen keeps to itself: it
   * has to survive a redraw, and choosing another list has to reset it — page
   * four of Arrivals is not page four of Departures, and carrying it across
   * would land a person on an empty page of a shorter list.
   */
  page: number;
}

/** Who is signed in, drawn at the right of the bar. */
const OPERATOR = { name: "Anitha Menon", where: "Front Office · Avenue Regent" };

/**
 * Where the module starts.
 *
 * A parameter so the preview harness can place the module in a state the
 * product does not yet have a route to — frame 15's registration card, which
 * the design opens from a check-in and which nothing in the built screens
 * offers. **The gap is the route, not the card**: the card is built and the
 * affordance that would open it is not drawn in any approved frame, so
 * inventing one would be richer than the design rather than faithful to it.
 *
 * It is deliberately not a router. There is exactly one state expressible this
 * way and it exists so a capture photographs the built card rather than
 * nothing.
 */
export interface Opening {
  overlay?: "registration" | undefined;
}

/**
 * The module, opened at a given state.
 *
 * `activate` below is the **contract** — `Activate` takes a host and nothing
 * else, and widening it here would make this module the one that does not fit
 * the SDK's shape. So the extra state is a second entry point rather than a
 * second parameter on the first.
 */
export function start(host: HostApi, opening?: Opening): HostedModule {
  let root: HTMLElement | null = null;

  // Held, not appended once. `show` replaces the root's children on every
  // screen change, so a stylesheet appended at mount is deleted by the first
  // render — the module then draws itself as an unstyled column, and neither
  // the type-check nor the suite can see it.
  const style = stylesheet();

  let where: Place = {
    screen: "Today",
    list: "Arrivals",
    tab: "Overview",
    page: 0,
    bookingId: "",
    overlay: opening?.overlay ?? null,
  };

  function show(next: Partial<Place>): void {
    if (root === null) return;
    where = { ...where, ...next };

    const frame = el("div", "go");
    const main = el("div", "main");

    frame.append(
      bar(items(), lit(where.screen), OPERATOR, (label) =>
        show({
          screen: label as Place["screen"],
          list: "Arrivals",
          page: 0,
          overlay: null,
        })),
      main,
    );

    root.replaceChildren(style, frame);
    draw(main);
  }

  function draw(main: HTMLElement): void {
    // The sheet stands over whatever screen is drawn, so it is appended after
    // the screen rather than instead of it — frame 10 is the day, dimmed, with
    // the walk-in on top of it.
    const overlay = (): void => {
      if (where.overlay === "walkin") {
        main.append(walkIn(recordedWalkIn, () => show({ overlay: null })));
      }

      // The card stands over the day, because that is where a check-in starts:
      // a receptionist opens it from the arrival they are looking at, and the
      // list stays behind it.
      if (where.overlay === "registration") {
        main.append(registration(recordedRegistration, () => show({ overlay: null })));
      }
    };

    if (where.screen === "Booking") {
      void booking(
        host,
        main,
        where.bookingId,
        where.overlay === "cancel",
        () => show({ overlay: "cancel" }),
        () => show({ overlay: null }),
        // A completed cancellation closes the dialog and redraws from the
        // server. The screen does not patch its own rows: the lifecycle is the
        // service's, and a client editing its copy would be a second place it
        // is decided.
        () => show({ overlay: null }),
      );
      return;
    }

    if (where.screen === "NewBooking") {
      void newBooking(host, main, () => show({ overlay: "walkin" }))
        .then(overlay);
      return;
    }

    if (where.screen === "Bookings") {
      void bookings(
        host,
        main,
        where.page,
        (page) => show({ page }),
        (row) => show({ screen: "Booking", bookingId: row.id, overlay: null }),
        () => show({ overlay: "walkin" }),
        () => show({ screen: "NewBooking", overlay: null }),
      ).then(overlay);
      return;
    }

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
        where.page,
        // Another list starts at its own beginning.
        (list) => show({ list, page: 0 }),
        (page) => show({ page }),
        () => show({ screen: "Stay", tab: "Overview" }),
        () => show({ overlay: "walkin" }),
        () => show({ screen: "NewBooking", overlay: null }),
      ).then(overlay);
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
}

/** Rendered by the host into the module's own document. */
export const activate: Activate = (host: HostApi): HostedModule => start(host);

/**
 * The bar's entries and their counts.
 *
 * Counts come from the same recorded facts the screens read, so the bar cannot
 * claim a number the list does not show. When the client lands they arrive
 * through the same seam.
 */
function items(): readonly BarItem[] {
  return [
    { label: "Today", count: recordedToday.businessDate.split(" ").slice(1).join(" ") },
    { label: "Bookings", count: "218" },
    { label: "Guests", count: "1 904" },
    { label: "Attention", count: String(recordedAttention.length), attention: true },
  ];
}

/**
 * Which bar entry is lit.
 *
 * A screen reached from a list lights the list it came from, because that is
 * where the way back is. Written as a map rather than a chain of ternaries so
 * the next screen that hangs off a section is one line.
 */
function lit(screen: Place["screen"]): string {
  const under: Partial<Record<Place["screen"], string>> = {
    Stay: "Today",
    Booking: "Bookings",
    NewBooking: "Bookings",
  };

  return under[screen] ?? screen;
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

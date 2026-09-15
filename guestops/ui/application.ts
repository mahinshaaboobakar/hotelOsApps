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

// **Three fixtures and the three screens that drew them are not imported.**
// `firstRun`, `walkIn` and `registration` each took an approved frame's data
// and put it on the property's screen; none of the three has a method behind
// it, so there is nothing to call and nothing true to draw. The screens
// themselves are kept in the tree — they are the drawings Part A audits — and
// they return here with the reads that feed them.
import { load } from "./book";
import { el } from "./chrome/element";
import { bar, type BarItem, type Operator } from "./chrome/bar";
import { cannot } from "./chrome/marks";
import { sheet } from "./chrome/overlay";
import { stylesheet } from "./chrome/styles";
import { attention } from "./screens/attention";
import { booking } from "./screens/booking";
import { bookings } from "./screens/bookings";
import { newBooking } from "./screens/newbooking";
import { setup } from "./screens/setup";
import { stay } from "./screens/stay";
import { today } from "./screens/today";

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
    | "NewBooking"
    | "Setup";

  list: string;
  tab: string;

  /** Which booking the Booking screen is showing. */
  bookingId: string;

  /**
   * Which stay the Stay screen is showing.
   *
   * **It had none, and that was the whole defect.** The row click discarded the
   * row it was handed, `Place` had nowhere to put a stay, the screen's three
   * reads carried no body, and the backend's `activity`, `requests` and
   * `payment` all refuse a request without one — `"this method needs a stay"`.
   * So the anchor screen showed one recorded stay, always, and no path existed
   * by which it could show another.
   */
  stayId: string;

  /** Which section of Setup is showing. */
  section: string;

  /**
   * True while the property's book is being brought in — frame 13.
   *
   * A **state of the installation**, not a screen: it replaces the whole main
   * area whatever the bar says, because there is nothing else to look at until
   * the replay has produced today's arrivals. It is here rather than in a
   * screen for that reason.
   */
  filling: boolean;

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

  /**
   * Frame 13, which the product reaches by being installed on a property whose
   * Hub is holding a queue — a condition no click produces.
   */
  filling?: boolean | undefined;
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

  /**
   * Who is at this desk, once the backend has said.
   *
   * **Null until it answers, and null forever if it cannot.** There is no
   * recorded fallback here and that is deliberate: a read that cannot reach the
   * platform may show recorded facts and say so, but a *person's name* has no
   * stand-in — drawing one attributes every write on these screens to somebody
   * who may not be at the desk. So the fallback passed to `load` is `null`, and
   * the bar draws the sentence instead.
   */
  let operator: Operator | null = null;

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
    stayId: "",
    section: "Registration",
    overlay: opening?.overlay ?? null,
    filling: opening?.filling ?? false,
  };

  function show(next: Partial<Place>): void {
    if (root === null) return;
    where = { ...where, ...next };

    const frame = el("div", "go");
    const main = el("div", "main");

    frame.append(
      bar(items(where.filling), lit(where.screen), operator, (label) =>
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
    // Before anything else: until the book is in, every screen would be a
    // truthful drawing of an empty hotel, which is the one wrong thing to show
    // a property with two thousand reservations waiting.
    if (where.filling) {
      // **The fixture is gone and nothing serves this yet.** It drew
      // `recordedFirstRun` — a progress figure, a count of what had arrived, a
      // source name — none of it measured. A first-run screen is read by
      // somebody waiting to know whether their property's book is coming in,
      // which is the worst possible audience for an invented number.
      //
      // No module method answers a fill, so there is no call to make: this is a
      // gap, reported rather than filled.
      main.replaceChildren(cannot(
        "The book is being brought in",
        "GuestOps cannot yet report how far along this is. Nothing here is "
        + "measured, so nothing is shown.",
      ));
      return;
    }

    if (where.screen === "Setup") {
      void setup(host, main, where.section, (section) => show({ section }));
      return;
    }

    // The sheet stands over whatever screen is drawn, so it is appended after
    // the screen rather than instead of it — frame 10 is the day, dimmed, with
    // the walk-in on top of it.
    const overlay = (): void => {
      if (where.overlay === "walkin") {
        // **A door with nothing on the other side of it, and it is reported as
        // that rather than built.** `stay.create/walkIn` is declared in the
        // manifest and served by `ModuleSurface` — and this sheet rendered
        // `recordedWalkIn` and called `perform` nowhere, so no control in this
        // module could ever reach it. That is not a missing call; it is a
        // capability with no caller, and adding a submit here would be feature
        // work wearing a conversion's clothes.
        main.append(sheet({
          title: "Walk-in",
          subtitle: "not available at this desk yet",
          body: [cannot(
            "A walk-in cannot be taken here yet",
            "GuestOps can record one — the platform serves it — but this sheet "
            + "has nothing to capture a guest with, and it will not show a draft "
            + "belonging to nobody.",
          )],
          foot: null,
          actions: [{ label: "Close", onClick: () => show({ overlay: null }) }],
          onDismiss: () => show({ overlay: null }),
        }));
      }

      // The card stands over the day, because that is where a check-in starts:
      // a receptionist opens it from the arrival they are looking at, and the
      // list stays behind it.
      if (where.overlay === "registration") {
        // Same shape and a worse fixture: a registration card carries a guest's
        // name, document and signature state. `registration.capture` is
        // declared and no module method serves it, so there is nothing to call
        // and nothing true to draw.
        main.append(sheet({
          title: "Registration",
          subtitle: "not available at this desk yet",
          body: [cannot(
            "This registration cannot be opened yet",
            "No method answers a registration card, so GuestOps would be showing "
            + "a guest's details that belong to nobody.",
          )],
          foot: null,
          actions: [{ label: "Close", onClick: () => show({ overlay: null }) }],
          onDismiss: () => show({ overlay: null }),
        }));
      }
    };

    if (where.screen === "Booking") {
      void booking(
        host,
        main,
        where.bookingId,
        where.page,
        (page) => show({ page }),
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
      void newBooking(host, main, where.page, (page) => show({ page }), () =>
        show({ overlay: "walkin" }))
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
      void stay(host, main, where.stayId, where.tab, (tab) => show({ tab }));
      return;
    }

    if (where.screen === "Attention") {
      void attention(host, main, where.page, (page) => show({ page }));
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
        (row) => show({ screen: "Stay", tab: "Overview", stayId: row.id }),
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

      // Asked once, after the first paint — the bar is drawn synchronously and
      // a person should not wait on a name to see their day. It redraws when
      // the answer arrives, and does not when it does not.
      void load<{ name: string | null; where: string | null }>(
        host, "reservation.read", "me",
      ).then((got) => {
        // **No branch for a failed read, because the bar already has one.** An
        // unanswered `me` leaves `operator` null and the bar says the operator
        // is not established — which is what a failure means here. APPS-Q42 asks
        // that a failure not be rendered as data; this renders it as absence,
        // which is the same sentence in the one slot that already had it.
        if (!got.ok) return;

        // A present value, not a non-null one. The first version tested
        // `!== null` and drew `undefined · undefined` on a host that answered
        // without the fields.
        const said = (value: unknown): string | null =>
          typeof value === "string" && value.trim() !== "" ? value : null;

        const name = said(got.value.name);
        const where = said(got.value.where);

        if (name !== null && where !== null) {
          operator = { name, where };
          show({});
        }
      });
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
function items(filling: boolean): readonly BarItem[] {
  // **Every count is `—`, and the dash is the whole point.**
  //
  // It used to be `—` only while the book was being brought in, and a live desk
  // got `Bookings 218` and `Guests 1 904` written into this file, with Today and
  // Attention read off the recorded fixtures. Four numbers nobody counted, in
  // the one place a receptionist glances to see whether anything needs them.
  //
  // Nothing establishes a count here. The bar is drawn before any screen has
  // loaded, the seam is per-screen, and a figure this file holds is a figure
  // that survives being wrong. `—` says *this counts something and the number is
  // not established*, which is exactly the state — and a `0` would be worse,
  // because it says the hotel is empty.
  //
  // The approved frames draw numbers. That is a divergence with a reason and it
  // is on the Part A sheet: the frames were drawn for a bar fed by live counts,
  // and the honest build draws the dash until something feeds it. Setup keeps no
  // count at all, because it counts nothing in any condition.
  const counting: readonly BarItem[] = [
    { label: "Today", count: "—" },
    { label: "Bookings", count: "—" },
    { label: "Guests", count: "—" },
    { label: "Attention", count: "—", attention: true },
    { label: "Setup" },
  ];

  // Five entries where frame 16 draws four: that frame shows Setup in
  // Attention's place, which no other frame does — it is the only one drawn for
  // a general manager, and dropping Attention to match it would remove the entry
  // every other frame carries. Added rather than swapped, and reported.
  return filling
    ? counting.map((item) => (item.label === "Attention" ? { ...item, attention: false } : item))
    : counting;
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

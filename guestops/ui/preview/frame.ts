/**
 * One module instance, in one realm.
 *
 * Each pane of the harness is a real `<iframe>`, because that is the shape the
 * module actually gets (ADR 0128 §7). It matters visually: the module sizes
 * itself with `100vh`, which is its realm's height in production and would be
 * the whole scrolling page if it were mounted into a plain `<div>` — the
 * capture would then show a layout no property will ever see.
 *
 * # It fakes the host and nothing else
 *
 * The identity, the granted capabilities and the answers to `host.call` are
 * this file's. The module's own code, its stylesheet and its token references
 * are the shipped ones, so what appears here is what a property would see.
 *
 * The answers are `book/recorded.ts` — the approved frames' own data — served
 * as though the platform had returned them. That exercises the **live** path,
 * which is the one a property will use; the harness's fourth pane grants
 * nothing and exercises the fallback.
 */

import type { HostApi } from "@hotelos/sdk";

import { start } from "../application";
import {
  recordedActivity,
  recordedAttention,
  recordedAvailability,
  recordedBooking,
  recordedBookings,
  recordedCancelPlan,
  recordedGroup,
  recordedPayment,
  recordedRequests,
  recordedRequestsAlone,
  recordedServicing,
  recordedServicingAlone,
  recordedSetup,
  recordedStay,
  recordedToday,
  recordedTodayConnected,
} from "../book";

/**
 * A host that grants what the manifest requests and answers from the fixtures.
 *
 * `granted` is a parameter so the harness can show the refusal path too: the
 * module renders a stand-in banner when a capability was not granted, and that
 * banner is a design element the audit has to be able to see.
 */
function host(granted: readonly string[]): HostApi {
  return {
    identity: { id: "guestops", version: "0.1.0", capabilities: granted },

    // The host tells a module its property's zone and locale. Both are `null`
    // here on purpose: the SDK types them nullable because a property that has
    // not been configured is a real state, and a double that invented
    // "Asia/Kolkata" would hide every place this module forgets to handle it.
    property: { timezone: null, locale: null },

    call(capability: string, method: string): Promise<unknown> {
      const answers: Record<string, unknown> = {
        // `?connected` is frame 11 — the same screen with a late feed, which
        // is a fact about the property rather than a route.
        today: connected ? recordedTodayConnected : recordedToday,
        setup: recordedSetup,
        attention: recordedAttention,
        stay: recordedStay,
        bookings: recordedBookings,
        // `?group` is frame 9 — the same screen, a booking whose source
        // claimed more rooms than it has sent. Not a route: whether a booking
        // is complete is a fact about the booking.
        booking: group ? recordedGroup : recordedBooking,
        cancelPlan: recordedCancelPlan,
        availability: recordedAvailability,
        activity: recordedActivity,
        payment: recordedPayment,

        // The two neighbour-dependent tabs answer differently for the frames
        // that draw their ABSENT state. `?alone` is the harness's way of
        // reaching 5b — the module cannot be put into that state from the
        // outside, because whether Jobs is installed is a fact about the
        // property rather than a route.
        requests: alone ? recordedRequestsAlone : recordedRequests,
        servicing: alone ? recordedServicingAlone : recordedServicing,
      };

      return method in answers
        ? Promise.resolve(answers[method])
        : Promise.reject(new Error(`unhandled ${capability}/${method}`));
    },

    on(): () => void {
      return () => {};
    },
  };
}

const params = new URLSearchParams(location.search);
const screen = params.get("screen") ?? "today";

/** Frames 5b and 6's absent state — see the answers above. */
const alone = params.get("alone") === "true";

/** Frame 9's incomplete booking. */
const group = params.get("group") === "true";

/** Frame 11 — PMS-connected, with check-ins late. */
const connected = params.get("connected") === "true";

const granted = params.get("granted") === "none"
  ? []
  : ["reservation.read", "stay.override", "registration.capture", "request.handle",
    "desk.configure"];

// The one state with no built route — see `Opening`. Everything else is
// reached by clicking, because a capture of a state the application cannot be
// put into would be a photograph of nothing a property will ever see.
start(
  host(granted),
  screen === "registration"
    ? { overlay: "registration" }
    : screen === "firstrun"
      ? { filling: true }
      : {},
).mount(document.body);

/** Click the first element matching `selector` whose text contains `text`. */
function click(selector: string, text: string): void {
  for (const node of Array.from(document.querySelectorAll<HTMLElement>(selector))) {
    if (node.textContent?.includes(text) === true) {
      node.click();
      return;
    }
  }
}

/**
 * Drive this realm to the screen it was asked for, then say so.
 *
 * The flag is what the capture waits on. A screenshot taken on a timer catches
 * a half-rendered screen often enough to be believed, and a loading state
 * photographs well.
 */
async function drive(): Promise<void> {
  // `.head .tab`, not `.ri`. The rail became the top bar (docs/working/64 §3)
  // and this driver kept the old class, so every `?screen=attention` capture
  // since then has quietly photographed Today. A harness that cannot reach a
  // screen reports nothing — it just shows a different one, convincingly.
  //
  // The steps are AWAITED between clicks, because every screen loads through
  // `load()` and renders in a promise. A synchronous chain of clicks reaches
  // the second one before the first screen exists, finds nothing to click, and
  // photographs whatever was already there — the same silent failure the class
  // name above caused, arrived at a different way.
  for (const step of PATHS[screen] ?? []) {
    click(step.selector, step.text);
    await settled();
  }

  // Two frames: one for the click's own render, one for the screen it opened —
  // RACED AGAINST A TIMER, because a hidden tab paints no frames at all.
  //
  // `requestAnimationFrame` does not fire while `document.hidden` is true, and
  // an automated capture runs the tab in the background more often than not. So
  // the flag this whole discipline waits on could never arrive, and the obvious
  // way out — give up and screenshot on a timer — is the exact thing the flag
  // exists to replace. The race keeps the frame-accurate path when there are
  // frames and still settles when there are none: the DOM is updated
  // synchronously either way, so there is nothing left to wait for.
  const settle = () => document.documentElement.setAttribute("data-ready", "true");

  requestAnimationFrame(() => requestAnimationFrame(() => setTimeout(settle, 40)));
  setTimeout(settle, 400);
}

/** One click on the way to a screen. */
interface Step {
  selector: string;
  text: string;
}

/**
 * How each screen is reached, as clicks a person would make.
 *
 * Driven rather than addressed, deliberately: the module has no router
 * (docs/working/64 §3, and `apps/desktop` has none either), so a capture that
 * jumped straight to a screen would be photographing a state the application
 * cannot actually be put into. Every frame here is reachable from the day.
 */
const PATHS: Record<string, readonly Step[]> = {
  attention: [{ selector: ".head .tab", text: "Attention" }],
  stay: [{ selector: ".tr.act", text: "Rajesh Pillai" }],

  activity: [
    { selector: ".tr.act", text: "Rajesh Pillai" },
    { selector: ".tabs .tab", text: "Activity" },
  ],

  requests: [
    { selector: ".tr.act", text: "Rajesh Pillai" },
    { selector: ".tabs .tab", text: "Requests" },
  ],

  servicing: [
    { selector: ".tr.act", text: "Rajesh Pillai" },
    { selector: ".tabs .tab", text: "Servicing" },
  ],

  payment: [
    { selector: ".tr.act", text: "Rajesh Pillai" },
    { selector: ".tabs .tab", text: "Payment" },
  ],
  bookings: [{ selector: ".head .tab", text: "Bookings" }],
  setup: [{ selector: ".head .tab", text: "Setup" }],
  walkin: [{ selector: ".tabs .btn", text: "Walk-in" }],

  newbooking: [
    { selector: ".head .tab", text: "Bookings" },
    { selector: ".fltr .btn", text: "New booking" },
  ],

  booking: [
    { selector: ".head .tab", text: "Bookings" },
    { selector: ".tr.list.act", text: "Fatima Sheikh" },
  ],

  cancel: [
    { selector: ".head .tab", text: "Bookings" },
    { selector: ".tr.list.act", text: "Fatima Sheikh" },
    { selector: ".title .btn", text: "Cancel" },
  ],
};

/** Let a screen's own promise resolve and its render land. */
function settled(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, 30));
}

setTimeout(() => void drive(), 60);

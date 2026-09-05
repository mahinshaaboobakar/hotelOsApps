/**
 * The built module, rendered — the right-hand pane of every pair.
 */

import { readFileSync } from "node:fs";
import { join } from "node:path";

import { Window } from "happy-dom";

import { start } from "../../application";
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
} from "../../book";

/** One click on the way to a screen. */
interface Step {
  selector: string;
  text: string;
}

/**
 * How each screen is reached, as clicks a person would make.
 *
 * **The same paths the preview harness drives**, deliberately duplicated rather
 * than imported: `frame.ts` is a browser bundle that reads `location.search`
 * and this runs in Node, and making one import the other would drag a browser
 * entry point into a build script. They are two readers of one design, and the
 * gallery would fail loudly — an empty pane — if they disagreed.
 */
const PATHS: Record<string, readonly Step[]> = {
  attention: [{ selector: ".head .tab", text: "Attention" }],
  bookings: [{ selector: ".head .tab", text: "Bookings" }],
  setup: [{ selector: ".head .tab", text: "Setup" }],
  walkin: [{ selector: ".tabs .btn", text: "Walk-in" }],
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

/**
 * Render one screen and give back its markup.
 *
 * @param query the harness's own query — `requests&alone=true`
 * @returns the module's document body, stylesheet included
 *
 * **The shipped `application.ts` against the harness's fake host**, so what
 * appears is what a property would see and not a re-drawing of it. The globals
 * are set before mounting because the module builds elements with `document`,
 * which is a browser's and has to be lent to it here.
 */
export async function render(query: string): Promise<string> {
  const window = new Window({ url: "https://guestops.local/" });
  const document = window.document;

  const previous = globalThis.document;
  (globalThis as { document?: unknown }).document = document;

  try {
    const params = new URLSearchParams(query);
    const screen = query.split("&")[0] ?? "";

    const root = document.createElement("div");
    document.body.append(root);

    start(host(params), opening(screen)).mount(root as unknown as HTMLElement);
    await drive(document, screen);

    return root.innerHTML;
  } finally {
    (globalThis as { document?: unknown }).document = previous;
  }
}

/**
 * Click the way to the screen, letting every render settle around each step.
 *
 * **It settles before the first click as well as after it**, and that is not
 * belt-and-braces. `show()` draws the bar synchronously and the body in a
 * promise, so a click on `.head .tab` lands and a click on `.tr.act` or
 * `.tabs .btn` finds nothing — the row it wants does not exist yet.
 *
 * The first run of this generator did exactly that, and it did not fail: seven
 * panes rendered plain Today, convincingly, under seven different headings.
 * They were caught because they came out **byte-identical**, which is why the
 * caller refuses a pane it cannot tell apart from another.
 */
async function drive(document: Window["document"], screen: string): Promise<void> {
  await settled();

  for (const step of PATHS[screen] ?? []) {
    let clicked = false;

    for (const node of Array.from(document.querySelectorAll(step.selector))) {
      if (node.textContent?.includes(step.text) === true) {
        (node as unknown as HTMLElement).click();
        clicked = true;
        break;
      }
    }

    if (!clicked) {
      throw new Error(
        `'${screen}': nothing matching ${step.selector} says '${step.text}' — `
        + "the screen is unreachable, and a pane drawn anyway would be a "
        + "photograph of a different screen under this one's heading",
      );
    }

    await settled();
  }
}

/** Let a screen's own promise resolve and its render land. */
function settled(): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, 0));
}

/** The two states no click produces — see `application.ts`'s `Opening`. */
function opening(screen: string): { overlay?: "registration"; filling?: boolean } {
  if (screen === "registration") return { overlay: "registration" };
  if (screen === "firstrun") return { filling: true };
  return {};
}

/**
 * The harness's host, answering from the approved frames' own data.
 *
 * `alone`, `group` and `connected` select the *second* state of three screens.
 * They are not routes: whether Jobs is installed, whether a booking is
 * complete, and whether a feed is late are facts about the property, and a
 * gallery that reached them by navigation would be photographing something the
 * application cannot be in.
 */
function host(params: URLSearchParams): Parameters<typeof start>[0] {
  const alone = params.get("alone") === "true";
  const group = params.get("group") === "true";
  const connected = params.get("connected") === "true";

  const answers: Record<string, unknown> = {
    today: connected ? recordedTodayConnected : recordedToday,
    attention: recordedAttention,
    stay: recordedStay,
    bookings: recordedBookings,
    booking: group ? recordedGroup : recordedBooking,
    cancelPlan: recordedCancelPlan,
    availability: recordedAvailability,
    activity: recordedActivity,
    payment: recordedPayment,
    setup: recordedSetup,
    requests: alone ? recordedRequestsAlone : recordedRequests,
    servicing: alone ? recordedServicingAlone : recordedServicing,
  };

  return {
    identity: {
      id: "guestops",
      version: "0.1.0",
      capabilities: [
        "reservation.read", "stay.override", "stay.create",
        "registration.capture", "request.handle", "desk.configure",
      ],
    },

    property: { timezone: null, locale: null },

    call(capability: string, method: string): Promise<unknown> {
      return method in answers
        ? Promise.resolve(answers[method])
        : Promise.reject(new Error(`unhandled ${capability}/${method}`));
    },

    on(): () => void {
      return () => undefined;
    },
  };
}

/**
 * Render one widget, the way the shell's host connects it.
 *
 * **The same handshake `tests/widget-connect.test.ts` guards**, and for the
 * same reason it exists: a widget that is not properly connected renders a
 * styled, silent, empty card, and a capture of one is indistinguishable from a
 * capture of a quiet hotel. The port is answered with a refusal, which is the
 * path a property sees today — every widget draws its recorded facts and says
 * so.
 *
 * @param name the entry under `widgets/entry`
 * @returns the widget's own markup, stylesheet included
 */
export async function widget(name: string): Promise<string> {
  const window = new Window({ url: "https://guestops.local/" });

  // happy-dom's `Window` carries the browser globals a widget uses, but its
  // TypeScript surface does not name them all — the cast is to the shape being
  // borrowed, not a claim that it is a browser.
  // happy-dom's `Window` carries `MessageEvent` and **not** `MessageChannel`;
  // Node's global has the channel. Each is taken from where it exists rather
  // than from where it would be tidier — a realm assembled out of one of them
  // and a guess is a realm that fails at the first port.
  const realm = window as unknown as { MessageEvent: typeof MessageEvent };

  const previous = {
    document: globalThis.document,
    window: (globalThis as { window?: unknown }).window,
    MessageEvent: (globalThis as { MessageEvent?: unknown }).MessageEvent,
  };

  Object.assign(globalThis, {
    document: window.document,
    window,
    MessageEvent: realm.MessageEvent,
  });

  try {
    // **The shipped bundle, evaluated — not the source, imported.**
    //
    // A templated dynamic import is not something esbuild can resolve, so this
    // used to fail at runtime with "module not found in bundle". Static imports
    // would be worse than a workaround: a widget calls `connectToHost` at module
    // scope, so importing all five at the top would run every handshake against
    // whatever `window` happened to be current, once, before any realm exists.
    //
    // Reading `widgets/<name>.js` avoids both and is truer besides: that file is
    // what goes in the package, self-contained with no imports left, and it is
    // the artefact a property actually runs.
    const bundle = readFileSync(
      join(process.cwd(), "widgets", `${name}.js`), "utf8");

    new Function(bundle)();

    const channel = new MessageChannel();

    channel.port1.addEventListener("message", (event: MessageEvent) => {
      const message = event.data as { type?: string; id?: number };
      if (message.type !== "hotelos.call") return;

      channel.port1.postMessage({
        type: "hotelos.result",
        id: message.id,
        ok: false,
        error: { kind: "unavailable", message: "no client yet" },
      });
    });

    channel.port1.start();

    // Dispatched through the realm's own constructor, and handed to the
    // realm's own dispatcher: happy-dom brands its events, so an event built
    // from the ambient globals is not one this window accepts.
    window.dispatchEvent(new realm.MessageEvent("message", {
      data: {
        type: "hotelos.connect",
        contract: 1,
        minContract: 1,
        module: { id: "guestops", version: "0.1.0", capabilities: ["reservation.read"] },
        property: { timezone: null, locale: null },
      },
      ports: [channel.port2],
    }) as never);

    await new Promise((resolve) => setTimeout(resolve, 40));

    const drawn = window.document.body.innerHTML;

    // **Both ports closed, or the process never exits.** A `MessagePort` is a
    // live handle on Node's event loop: the generator wrote its file and then
    // hung forever with nothing left to do. Closed here rather than in the
    // caller, because the channel is this function's and a caller that had to
    // remember would be a caller that forgets.
    channel.port1.close();
    channel.port2.close();

    return drawn;
  } finally {
    Object.assign(globalThis, previous);
  }
}

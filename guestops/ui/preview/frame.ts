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

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { start } from "../application";
import { listState, page, repeated, rowsFor } from "./lists";
import { statedProperty } from "./property";
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
} from "../book/recorded";

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
    // unless the capture's address states them: the SDK types them nullable
    // because a property that has not been configured is a real state, and a
    // double that invented "Asia/Kolkata" would hide every place this module
    // forgets to handle it. `?locale=&tz=` is a choice made by the capture that
    // needs it, visible in its URL — preview/property.ts.
    property: statedProperty(),

    call(capability: string, method: string, params?: unknown): Promise<unknown> {
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

        // The bar's operator. Unanswered, every capture would read "operator
        // not established" against a drawing that names one — seventeen false
        // divergences on a value neither side is wrong about. The drawing's own
        // two, taken from it.
        me: { name: "Anitha Menon", where: "Front Office · Avenue Regent" },
      };

      // **One method fails, by the cause asked for, and nothing else does** —
      // so a screen reached by clicking through two others can still be
      // photographed failing. Failing every call would stop the drive at the
      // first screen and photograph that one, whatever `screen=` said.
      if (failing !== null && method === failing.method) {
        return Promise.reject(new HostCallError({
          kind: failing.kind,
          message: `the harness failed ${method} as ${failing.kind}`,
        }));
      }

      if (!(method in answers)) {
        return Promise.reject(new Error(`unhandled ${capability}/${method}`));
      }

      // `?list=` shapes the five paged answers; everything else is as recorded.
      return Promise.resolve(shaped(method, answers[method], params));
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

/**
 * `?fail=<cause>&at=<method>` — the three failure states of `64b`, reached
 * through the real read.
 *
 * Each cause is the host error kind the SDK maps to it, so the drawing under
 * test comes from `load` and `failureDrawing` exactly as it would on a property
 * rather than from a drawing built here. `forbidden` is the host's refusal, not
 * `granted=none`: an ungranted capability stops the drive at the first screen,
 * and a stay's Payment tab could then never be photographed refused.
 */
const KINDS = {
  unanswered: "unavailable",
  forbidden: "forbidden",
  unadmitted: "local_forbidden",
  ungranted: "user_forbidden",
  undecidable: "model_unavailable",
  faulted: "internal",
} as const;

/** `?list=E0|E1|1P|MP|ML` — see `lists.ts`. */
const list = listState(params);

/**
 * The list each paged method carries, and where its rows live.
 *
 * Booking's page size is written here because its request names none — the
 * screen pages by 12 while the backend, asked for no size, answers 500. That
 * mismatch is under audit (G1/G4), and sizing the dataset by what the screen
 * believes is what lets the harness show it.
 */
const PAGED: Record<string, { key: string; size: (params: unknown) => number }> = {
  today: { key: "rows", size: sized(25) },
  bookings: { key: "rows", size: sized(25) },
  attention: { key: "cards", size: sized(10) },
  booking: { key: "stays", size: () => 12 },
  availability: { key: "types", size: sized(12) },
};

function sized(fallback: number): (params: unknown) => number {
  return (params) => {
    const asked = (params as { pageSize?: unknown } | undefined)?.pageSize;
    return typeof asked === "number" && asked > 0 ? asked : fallback;
  };
}

/**
 * Whether the drive has "deleted" the last page's rows — E1's event.
 *
 * **Set by the drive, never counted from answers.** The first cut shrank the
 * list after its first answer, assuming one read per screen; Today reads
 * `today` twice at start-up, so the list shrank BEFORE the pager was drawn, the
 * pager showed two pages, and the drive pressed page 2 — a real page, not a page
 * past the rows. The rule now says what happens: the list shrinks at the moment
 * the drive presses the last page, as it would after a delete.
 */
let deleted = false;

/**
 * The paged read each screen is audited on — and ONLY that one is re-paged.
 *
 * The first cut re-paged every paged answer, so driving to a booking passed
 * through a Bookings list the same `?list=E0` had emptied, and the drive found
 * no row to open: two cells of the first baseline never reached their screen.
 */
const AUDITED: Record<string, string> = {
  today: "today",
  bookings: "bookings",
  attention: "attention",
  booking: "booking",
  newbooking: "availability",
};

/** A recorded answer, re-paged into the requested list state. */
function shaped(method: string, answer: unknown, params: unknown): unknown {
  const paged = PAGED[method];
  if (list === null || paged === undefined || AUDITED[screen] !== method) return answer;

  const sizes = rowsFor(list, paged.size(params));
  const n = deleted ? sizes.after : sizes.before;

  // Today's paged list is its first — Arrivals, the tab it opens on.
  if (method === "today") {
    const day = answer as { lists: readonly { rows: readonly unknown[] }[] };
    const [first, ...rest] = day.lists;
    if (first === undefined) return answer;
    const { rows, total } = page(repeated(first.rows, n), params);
    return { ...day, lists: [{ ...first, rows, count: String(total) }, ...rest] };
  }

  const record = answer as Record<string, unknown>;
  const all = record[paged.key] as readonly unknown[];
  const { rows, total } = page(repeated(all, n), params);
  return { ...record, [paged.key]: rows, total };
}

const fail = params.get("fail");
const failing = fail !== null && fail in KINDS
  ? { kind: KINDS[fail as keyof typeof KINDS], method: params.get("at") ?? "" }
  : null;

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

  // **A step that matched nothing throws** — `64` §8 clause 3, and the reason
  // is the comment two functions down: this used to return quietly, so a driver
  // whose selector had gone stale photographed a different screen, convincingly.
  // The capture was of a real screen; it was simply not the one asked for, and
  // nothing in the image said so.
  throw new Error(`no ${selector} says "${text}" — the drive could not reach it`);
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
  try {
    for (const step of PATHS[screen] ?? []) {
      click(step.selector, step.text);
      await settled();
    }

    // ML and E1 are the LAST page — reached by pressing it, as a person does.
    // For E1 the list has shrunk since the pager was drawn, so this request
    // asks for a page the list no longer has.
    if (list === "ML" || list === "E1") {
      const numbered = Array.from(document.querySelectorAll<HTMLElement>(".pager .pg"))
        .filter((node) => /^\d+$/u.test(node.textContent ?? ""));
      const last = numbered.at(-1);
      if (last === undefined || numbered.length < 2) {
        throw new Error(`no second page to reach for list=${list} — the drive could not reach it`);
      }
      deleted = list === "E1";
      last.click();
      await settled();
    }
  } catch (error) {
    // **Drawn, and then still marked ready.** Refusing to signal would make the
    // sweep time out, which reads as a broken harness rather than as a screen
    // that could not be reached — and a timeout carries no sentence. This
    // photographs the failure instead, so the capture says which step missed.
    const said = error instanceof Error ? error.message : String(error);
    document.body.replaceChildren();
    const box = document.createElement("pre");
    box.style.cssText = "padding:24px;color:#f87171;font:13px/1.6 monospace;white-space:pre-wrap";
    box.textContent = `drive failed for screen=${screen}

${said}`;
    document.body.append(box);
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
  const settle = () => {
    // **Two signals, and the second is not this harness's own** — `ARCH-Q12`.
    // `data-ready` is what this preview's own captures wait on. The shared
    // sweep waits on `data-review-ready`, and a page that sets only a private
    // signal is a page the merged instrument cannot measure — which is a stream
    // making its surface unauditable by the tool every stream owes. Set beside
    // the private one rather than instead of it, so nothing that already waits
    // on `data-ready` changes.
    document.documentElement.setAttribute("data-ready", "true");
    document.documentElement.setAttribute("data-review-ready", "true");
  };

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

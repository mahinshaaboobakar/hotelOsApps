/**
 * Every surface GuestOps draws, against its recorded read — one walk, shared.
 *
 * Built to GG's shape for Workforce (`tests/surfaces.ts`, `b001bfac`): a check
 * that walks every surface keeps its visited set here, so two checks cannot
 * drift in which screens each bothered to visit, and a new screen is added once.
 * The screens are called directly with no-op navigation; the widgets are
 * mounted the way the shell mounts them, through a connect and a port that
 * answers from the widgets' recorded reads.
 */

import { HostCallError, HOST_CONTRACT_RANGE, type HostApi } from "@hotelos/sdk";
import { vi } from "vitest";

import {
  recordedActivity, recordedAttention, recordedAvailability, recordedBooking, recordedBookings,
  recordedCancelPlan, recordedPayment, recordedRequests, recordedServicing, recordedSetup,
  recordedStay, recordedToday,
} from "../book/recorded";
import { attention } from "../screens/attention";
import { booking } from "../screens/booking";
import { bookings } from "../screens/bookings";
import { newBooking } from "../screens/newbooking";
import { setup } from "../screens/setup";
import { stay } from "../screens/stay";
import { today } from "../screens/today";
import * as widgetReads from "../widgets/recorded";

/** What each screen read answers — the recorded fixtures, keyed by method. */
const ANSWERS: Record<string, unknown> = {
  today: recordedToday, attention: recordedAttention, bookings: recordedBookings,
  booking: recordedBooking, cancelPlan: recordedCancelPlan, availability: recordedAvailability,
  stay: recordedStay, activity: recordedActivity, requests: recordedRequests,
  servicing: recordedServicing, payment: recordedPayment, setup: recordedSetup,
  me: { name: "Anitha Menon", where: "Front Office · Avenue Regent" },
};

/** What each widget read answers. */
const WIDGET_ANSWERS: Record<string, unknown> = {
  desk: widgetReads.desk, occupancy: widgetReads.occupancy, feed: widgetReads.pms,
  mix: widgetReads.mix, watchlist: widgetReads.watchlist,
};

/** A host answering every recorded read, for a property in Kolkata. */
export function surfaceHost(): HostApi {
  return {
    identity: {
      id: "guestops", version: "0.1.0",
      capabilities: ["reservation.read", "desk.configure", "stay.override", "stay.create"],
    },
    property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    call: (_capability: string, method: string) => method in ANSWERS
      ? Promise.resolve(ANSWERS[method])
      : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" })),
    on: () => () => {},
  } as unknown as HostApi;
}

const none = (): void => {};

/** Draws one surface into an element. */
type Draw = (host: HostApi, into: HTMLElement) => Promise<void>;

/** Every screen, and every state of one that shows different text. */
export const SCREENS: readonly (readonly [string, Draw])[] = [
  ["today", (host, into) => today(host, into, "arrivals", 0, none, none, none, none, none)],
  ["bookings", (host, into) => bookings(host, into, 0, none, none, none, none)],
  ["booking", (host, into) => booking(host, into, "b1", 0, none, false, none, none, none)],
  ["booking, cancelling", (host, into) => booking(host, into, "b1", 0, none, true, none, none, none)],
  ["attention", (host, into) => attention(host, into, 0, none)],
  ["new booking", (host, into) => newBooking(host, into, none)],
  ...recordedSetup.sections.map((section) =>
    [`setup · ${section.label}`, (host: HostApi, into: HTMLElement) => setup(host, into, section.label, none)] as const),
  ...["Overview", "Activity", "Requests", "Servicing", "Payment"].map((tab) =>
    [`stay · ${tab}`, (host: HostApi, into: HTMLElement) => stay(host, into, "s1", tab, none)] as const),
];

/** Every widget, by its bundle's entry name. */
export const WIDGETS = ["today", "occupancy", "from-the-pms", "business-mix", "watchlist"] as const;

/** Mount one widget as the shell does, answered from its recorded read. */
export async function mountWidget(name: string): Promise<HTMLElement> {
  document.body.replaceChildren();
  vi.resetModules();
  await import(`../widgets/entry/${name}.ts`);

  const channel = new MessageChannel();
  channel.port1.addEventListener("message", (event: MessageEvent) => {
    const message = event.data as { type?: string; id?: number; method?: string };
    if (message.type !== "hotelos.call") return;

    const method = message.method ?? "";
    channel.port1.postMessage(method in WIDGET_ANSWERS
      ? { type: "hotelos.result", id: message.id, ok: true, value: WIDGET_ANSWERS[method] }
      : { type: "hotelos.result", id: message.id, ok: false, error: { kind: "unavailable", message: "not this test" } });
  });
  channel.port1.start();

  window.dispatchEvent(new MessageEvent("message", {
    data: {
      type: "hotelos.connect",
      contract: HOST_CONTRACT_RANGE.current,
      minContract: HOST_CONTRACT_RANGE.min,
      module: { id: "guestops", version: "0.1.0", capabilities: ["reservation.read"] },
      property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    },
    ports: [channel.port2],
  }));

  await new Promise((resolve) => setTimeout(resolve, 30));
  return document.body;
}

/**
 * Everything on a surface a person can read: its text, and the reasons carried
 * as tooltips and accessible descriptions — those are sentences for staff too.
 */
export function readable(root: HTMLElement): string {
  const reasons = [...root.querySelectorAll("[title], [aria-description]")]
    .flatMap((node) => [node.getAttribute("title"), node.getAttribute("aria-description")])
    .filter((text): text is string => text !== null);

  // A stylesheet's text is inside the element and no person reads it.
  const shown = root.cloneNode(true) as HTMLElement;
  for (const node of shown.querySelectorAll("style, script")) node.remove();

  return [shown.textContent ?? "", ...reasons].join("\n");
}

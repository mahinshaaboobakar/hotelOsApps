/**
 * One module instance, in one realm.
 *
 * Each pane of the harness is a real `<iframe>`, because that is the shape the
 * module actually gets (ADR 0128 §7). It matters visually: the module sizes
 * itself with `100vh`, which is its realm's height in production and would be
 * the whole scrolling page if it were mounted into a plain `<div>` — the
 * capture would then show a layout no property will ever see.
 *
 * The fixtures are the harness's; everything else is the shipped module.
 */

import type { HostApi } from "@hotelos/sdk";

import { activate } from "../module";
import type { Attention, Stay, Today } from "../book";

/** A PMS-connected day, so all four marks appear rather than only two. */
const today: Today = {
  businessDate: "Tue 1 Sep",
  rollsAt: "04:00",
  connected: true,
  arrivals: [
    { id: "a1", guest: "Rajesh Pillai", room: null, roomType: "Deluxe King",
      arrival: "1 Sep", departure: "4 Sep", lifecycle: "Booked", marks: ["pms", "missing"] },
    { id: "a2", guest: "Anna Varghese", room: "412", roomType: "Deluxe Twin",
      arrival: "1 Sep", departure: "3 Sep", lifecycle: "Booked", marks: ["pms"] },
    { id: "a3", guest: "Thomas Kurien", room: "509", roomType: "Suite",
      arrival: "1 Sep", departure: "2 Sep", lifecycle: "Booked", marks: [] },
  ],
  inHouse: [
    { id: "h1", guest: "Joseph Mathew", room: "318", roomType: "Deluxe King",
      arrival: "31 Aug", departure: "2 Sep", lifecycle: "In house", marks: ["pms", "disagrees"] },
    { id: "h2", guest: "Sunita Rao", room: "220", roomType: "Standard",
      arrival: "30 Aug", departure: "3 Sep", lifecycle: "In house", marks: ["override"] },
  ],
  departures: [
    { id: "d1", guest: "Meera Nair", room: "205", roomType: "Standard",
      arrival: "29 Aug", departure: "1 Sep", lifecycle: "In house", marks: ["pms"] },
  ],
};

const attention: readonly Attention[] = [
  { id: "t1", kind: "disagreement", stay: "318 · Joseph Mathew",
    ours: "In house", theirs: "Departed",
    detail: "The desk checked this guest in at 11:04. Opera says they left. The stay keeps the desk's value until somebody clears it — one truth still leaves the application." },
  { id: "t2", kind: "candidate", stay: "412 · Anna Varghese",
    ours: "Created here, 1 Sep – 3 Sep", theirs: "Opera 84119377, 1 Sep – 3 Sep",
    detail: "Same room, overlapping dates. The names rank the list and never link it: staff confirm, or reject and the room is honestly double-booked." },
];

const overridden = today.inHouse[1] as Stay;

function host(granted: readonly string[]): HostApi {
  return {
    identity: { id: "guestops", version: "0.1.0", capabilities: granted },
    call(capability: string, method: string): Promise<unknown> {
      if (method === "today") return Promise.resolve(today);
      if (method === "attention") return Promise.resolve(attention);
      if (method === "stay") return Promise.resolve(overridden);
      return Promise.reject(new Error(`unhandled ${capability}/${method}`));
    },
    on(): () => void {
      return () => {};
    },
  };
}

const params = new URLSearchParams(location.search);
const screen = params.get("screen") ?? "today";
const granted = params.get("granted") === "none"
  ? []
  : ["reservation.read", "stay.override", "registration.capture", "request.handle"];

activate(host(granted)).mount(document.body);

/**
 * Drive this realm to the screen it was asked for, then say so.
 *
 * The flag is what the capture waits on. A screenshot taken on a timer catches
 * a half-rendered screen often enough to be believed, and a loading state
 * photographs well.
 */
function drive(): void {
  const pick = screen === "attention" ? "Attention" : screen === "stay" ? "Sunita Rao" : null;

  if (pick !== null) {
    for (const node of Array.from(document.querySelectorAll<HTMLElement>(".ri, .row"))) {
      if (node.textContent?.includes(pick) === true) {
        node.click();
        break;
      }
    }
  }

  requestAnimationFrame(() =>
    requestAnimationFrame(() => document.documentElement.setAttribute("data-ready", "true")));
}

setTimeout(drive, 60);

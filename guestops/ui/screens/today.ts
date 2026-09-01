/**
 * Today — the front desk day: arrivals, in house, departures.
 *
 * Gold frame 1. **The day is the business day**, not midnight to midnight: it
 * is labelled with the date it rolls on and the hour it rolls at, because a
 * guest checking in at 01:00 belongs to the night before and a screen that said
 * "today" without saying which would be wrong for three hours every night.
 */

import type { HostApi } from "@hotelos/sdk";

import { el, shield, standIn } from "../chrome";
import { load, recordedToday, type Stay } from "../book";

/**
 * Render the day into a container the caller owns.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 * @param open what to do when a person picks a stay
 */
export async function today(host: HostApi, into: HTMLElement,
  open: (stay: Stay) => void): Promise<void> {
  const loaded = await load(host, "reservation.read", "today", recordedToday);
  const day = loaded.value;

  const head = el("div", "head");
  const title = el("div");
  title.append(
    el("div", "ht", "Today"),
    el(
      "div",
      "hsub",
      `Business day ${day.businessDate} · rolls at ${day.rollsAt} · ` +
        (day.connected ? "PMS-connected — Opera writes the lifecycle" : "standalone — this property is the book"),
    ),
  );
  head.append(title);

  const body = el("div", "body");

  if (!loaded.live) {
    body.append(standIn(loaded.because));
  }

  const strip = el("div", "strip");
  strip.append(
    stat(String(day.arrivals.length), `Arrivals · ${unassigned(day.arrivals)} unassigned`),
    stat(String(day.inHouse.length), "In house"),
    stat(String(day.departures.length), "Departures"),
  );
  body.append(strip);

  body.append(section("Arrivals", day.arrivals, open));
  body.append(section("In house", day.inHouse, open));
  body.append(section("Departures", day.departures, open));

  into.replaceChildren(head, body);
}

/** How many arrivals still have no room — the number the desk acts on. */
function unassigned(stays: readonly Stay[]): number {
  return stays.filter((stay) => stay.room === null).length;
}

function stat(value: string, label: string): HTMLElement {
  const element = el("div", "stat");
  element.append(el("b", undefined, value), el("span", undefined, label));
  return element;
}

function section(name: string, stays: readonly Stay[],
  open: (stay: Stay) => void): HTMLElement {
  const element = el("div");
  element.append(el("div", "sec", name));

  if (stays.length === 0) {
    const empty = el("div", "card");
    empty.append(el("p", undefined, `Nothing in ${name.toLowerCase()}.`));
    element.append(empty);
    return element;
  }

  for (const stay of stays) {
    element.append(row(stay, open));
  }

  return element;
}

/**
 * One stay.
 *
 * **The room is a column of its own and may be empty**, because a room-stay is
 * valid without a room: the anchor is the room *type*, and the number is an
 * assignment required at check-in (GUEST-Q2's addendum). An unassigned arrival
 * is the ordinary morning case, not a broken record — so it reads as *needs a
 * room*, never as a blank.
 */
function row(stay: Stay, open: (stay: Stay) => void): HTMLElement {
  const element = el("div", "row act");

  const who = el("div");
  who.append(el("div", "who", stay.guest), el("div", "thin", stay.roomType));

  const room = stay.room === null
    ? shield("missing", "needs a room")
    : el("div", undefined, stay.room);

  const marks = el("div");
  for (const mark of stay.marks) {
    if (mark === "missing") continue;
    marks.append(shield(mark, label(mark)));
  }

  element.append(
    who,
    room,
    el("div", "thin", `${stay.arrival ?? "—"} → ${stay.departure ?? "—"}`),
    el("div", "thin", stay.lifecycle),
    marks,
  );

  element.addEventListener("click", () => open(stay));
  return element;
}

function label(mark: string): string {
  if (mark === "pms") return "from Opera";
  if (mark === "override") return "override";
  return "disagrees";
}

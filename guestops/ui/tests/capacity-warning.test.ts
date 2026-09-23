/**
 * ADR 0223's treatment C: the warning arrives once, at the confirm, and the
 * booking can still be made.
 *
 * **The last two tests are the ruling's "do not do D" made executable.** D
 * marked the too-small row in the list and warned again here; it lost, and the
 * ADR says the first person to meet an unmarked row will want to add one. A
 * sentence in a document does not stop that. A failing test does.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { describe, expect, it } from "vitest";

import type { PropertyEnvironment } from "@hotelos/sdk";

import type { TypeAvailability } from "../book/model";
import { recordedAvailability } from "../book/recorded/availability";
import { availability } from "../screens/newbooking/availability";
import { confirm, exceeds, type Booking } from "../screens/newbooking/confirm";

const KOLKATA: PropertyEnvironment = { locale: "en-IN", timezone: "Asia/Kolkata" };

/** A type that sleeps two, and takes a third on an extra bed. */
const TWIN: TypeAvailability = {
  ...recordedAvailability.types[0]!,
  roomType: "Deluxe Twin",
  sleeps: { included: 2, most: 2, adults: 2, children: 0, extraBed: false, extraBeds: 0 },
};

const booking = (over: Partial<Booking> = {}): Booking => ({
  type: TWIN,
  arrive: "2026-09-03",
  depart: "2026-09-07",
  adults: 2,
  children: 1,
  guest: "Fatima Sheikh",
  phone: "+91 98470 11234",
  ...over,
});

const drawn = (over: Partial<Booking> = {}): HTMLElement =>
  confirm(booking(over), KOLKATA, () => {}, () => {});

describe("the capacity warning", () => {
  it("says it once, at the confirm, with the number the type sleeps", () => {
    expect(drawn().querySelector(".note.warn")?.textContent)
      .toBe("This room type sleeps 2 and you have entered 3 guests. It can still be booked.");
  });

  it("is absent where the party fits", () => {
    expect(drawn({ children: 0 }).querySelector(".note.warn")).toBeNull();
  });

  // The ceiling is what the room can hold, so a third guest on an extra bed is
  // not a warning — warning there would train a desk to click past it.
  it("counts the ceiling rather than what the rate includes", () => {
    const king = { ...TWIN, sleeps: { ...TWIN.sleeps!, included: 2, most: 3 } };

    expect(exceeds(booking({ type: king }))).toBe(false);
    expect(exceeds(booking({ type: king, children: 2 }))).toBe(true);
  });

  // An absent occupancy is silence. A sentence no fact supports is worse than
  // no sentence — ADR 0215's null is not a capacity of zero.
  it("says nothing where Master Data holds no occupancy", () => {
    expect(exceeds(booking({ type: { ...TWIN, sleeps: null } }))).toBe(false);
    expect(drawn({ type: { ...TWIN, sleeps: null } }).querySelector(".note.warn")).toBeNull();
  });

  // C informs, it does not prevent — and a card that asks carries the controls
  // that answer it.
  it("leaves the booking creatable, warned or not", () => {
    for (const card of [drawn(), drawn({ children: 0 })]) {
      const labels = [...card.querySelectorAll("button")].map((b) => b.textContent);

      expect(labels).toEqual(["Back", "Create booking"]);
      expect(card.querySelector("button.pri")?.hasAttribute("disabled")).toBe(false);
    }
  });
});

describe("the room type list, under C", () => {
  // **This is D, and D lost.** The row says what the type sleeps and says
  // nothing about the party — no mark, no dimming, no withheld control.
  it("marks no row against the party", () => {
    const list = availability(recordedAvailability.types);

    // **The positive control, because every assertion below is an absence.**
    // A list that rendered nothing would satisfy all of them and prove the
    // opposite of what they are for.
    expect(list.querySelectorAll(".tr.list:not(.hd)")).toHaveLength(
      recordedAvailability.types.length);

    expect(list.querySelectorAll(".tr.list.dim")).toHaveLength(0);
    expect(list.textContent ?? "").not.toMatch(/party of|too small/i);
  });

  it("still says what each type sleeps, which is not a mark", () => {
    const list = availability(recordedAvailability.types);

    expect(list.textContent ?? "").toMatch(/sleeps \d/);
  });
});

/**
 * Dates, times and counts are drawn in the property's form — page 64 §11–12.
 *
 * The service sends values; each screen formats them with the SDK from
 * `host.property`. Every case here uses one instant chosen so a wrong zone is
 * visible: **23:40 UTC on 31 August is 05:10 on 1 September in Kolkata**, so a
 * screen that drew the wire's own day, or the machine's zone, shows the wrong
 * day as well as the wrong hour.
 *
 * Two properties, because they fail in opposite directions:
 *
 * ```text
 * KOLKATA   en-IN · Asia/Kolkata     the property's own form, the day rolled over
 * UNKNOWN   no locale, no zone       ISO, 24-hour, marked UTC — never a guess (I4)
 * ```
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { describe, expect, it } from "vitest";

import type { PropertyEnvironment } from "@hotelos/sdk";

import type { Activity, CancelPlan } from "../book";
import { day, instant } from "../chrome/when";
import type { DayRow } from "../book/model";
import { activityTab } from "../screens/stay/activity-tab";
import { table } from "../screens/today/table";
import { summary } from "../screens/booking/index";
import { cancel } from "../screens/booking/cancel";
import { recordedCancelPlan } from "../book/recorded/booking";
import { recordedAvailability } from "../book/recorded/availability";
import { availability } from "../screens/newbooking/availability";
import type { TypeAvailability } from "../book/model";

const KOLKATA: PropertyEnvironment = { locale: "en-IN", timezone: "Asia/Kolkata" };
const UNKNOWN: PropertyEnvironment = { locale: null, timezone: null };

/** 23:40 UTC on the 31st — already the 1st, 05:10, in Kolkata. */
const LATE = "2026-08-31T23:40:00.0000000+00:00";

describe("the Activity tab", () => {
  const activity: Activity = {
    filters: [],
    entries: [{
      at: LATE,
      // Deliberately not the reference vendor's name. The service sends the
      // configured integration's display name, so a fixture naming the one
      // system a screen might have hardcoded could not tell the two apart.
      who: { mark: "pms", text: "Northwind" },
      what: "Checked in",
      detail: "stay.arrived",
      disagrees: false,
    }],
  };

  const when = (property: PropertyEnvironment): string[] => {
    const cell = activityTab(activity, property)
      .map((part) => part.querySelector(".ev:not(.hd) .tm"))
      .find((found) => found !== null);
    return [...(cell?.children ?? [])].map((child) => child.textContent ?? "");
  };

  it("draws the instant in the property's zone, so the day rolls over", () => {
    const [date, time] = when(KOLKATA);
    expect(date).toMatch(/\b01\b/);
    expect(date).not.toMatch(/\b31\b/);
    expect(time).toContain("05:10");
  });

  it("draws the marked ISO form where no locale or zone is established", () => {
    expect(when(UNKNOWN)).toEqual(["2026-08-31 UTC", "23:40 UTC"]);
  });
});

describe("an absent instant", () => {
  it("is drawn as the dash, never as today and never as the word null (I2)", () => {
    expect(instant(null, KOLKATA, "time")).toBe("—");
    expect(instant(null, UNKNOWN, "date")).toBe("—");
    expect(day(null, KOLKATA, "day-month")).toBe("—");
  });

  it("is the value's rendering when there is one", () => {
    expect(instant(LATE, UNKNOWN, "time")).toBe("23:40 UTC");
    expect(day("2026-09-01", UNKNOWN, "day-month")).toBe("2026-09-01");
  });
});

describe("Today's nights", () => {
  const row = (arrive: string | null, depart: string | null): DayRow => ({
    id: "s1", guest: "Guest", unnamed: false, contact: null, booking: "BK-1",
    roomType: "Deluxe", room: "214", party: null, arrive, depart, chips: [],
  });

  const drawn = (r: DayRow, property: PropertyEnvironment): string =>
    table([r], 1, () => {}, property).querySelector(".tr.act > div:nth-child(5)")?.textContent ?? "";

  it("composes the range from the two days, in the property's form", () => {
    expect(drawn(row("2026-08-31", "2026-09-02"), UNKNOWN)).toBe("2026-08-31 → 2026-09-02");
    expect(drawn(row("2026-08-31", "2026-08-31"), UNKNOWN)).toBe("2026-08-31 · day use");
    expect(drawn(row("2026-08-31", "2026-09-02"), KOLKATA)).toMatch(/^31 .+ → 02 /u);
  });

  it("draws an unrecorded arrival as the dash, never a guessed day", () => {
    expect(drawn(row(null, "2026-09-02"), KOLKATA)).toBe("—");
  });
});

describe("a booking's own line", () => {
  const line = (
    stays: number,
    arrive: string | null,
    depart: string | null,
    confirmation: string | null = null,
  ): string => summary({ confirmation, stays, arrive, depart }, KOLKATA);

  it("spells the count and draws the span in the property's form", () => {
    expect(line(2, "2026-09-03", "2026-09-07")).toMatch(/^Two stays · 03 .+ → 07 /u);
    expect(line(1, "2026-09-03", "2026-09-03")).toMatch(/^One stay · 03 .+ · day use$/u);
    expect(line(7, "2026-09-03", null)).toMatch(/^7 stays · 03 /u);
  });

  it("is the count alone when no arrival is recorded — never a count beside a dash", () => {
    expect(line(2, null, null)).toBe("Two stays");
  });

  // The owner's ruling on frame 9, 2026-09-20 (A2). The frame had been drawn
  // with a line the view has never sent, and this is the line it sends now.
  it("carries the confirmation number in front, where the source gave one", () => {
    expect(line(1, "2026-08-31", "2026-09-02", "84119377"))
      .toMatch(/^84119377 · One stay · 31 Aug → 02 /u);
  });

  it("starts with the count for a booking created here, which has no number", () => {
    expect(line(1, "2026-08-31", "2026-09-02")).toMatch(/^One stay · 31 Aug/u);
  });
});

describe("the cancel plan", () => {
  const plan = (over: Partial<CancelPlan>): CancelPlan => ({ ...recordedCancelPlan, ...over });

  const drawn = (over: Partial<CancelPlan>, property: PropertyEnvironment): HTMLElement =>
    cancel(plan(over), () => {}, () => {}, property);

  const text = (over: Partial<CancelPlan>, selector: string, property = KOLKATA): string =>
    drawn(over, property).querySelector(selector)?.textContent ?? "";

  it("says what the button does, from the count", () => {
    expect(text({ stays: 2 }, ".note")).toBe("This cancels two stays. One at a time.");
    expect(text({ stays: 1 }, ".note")).toBe("This cancels one stay.");
  });

  it("names the booking, the count and the span", () => {
    expect(text({}, ".dh span")).toMatch(/^BK-4506 · Fatima Sheikh · two stays, 03 /u);
  });

  it("drops a part the booking does not have, rather than a placeholder", () => {
    const subject = { reference: null, guest: null, arrive: null, depart: null };
    expect(text({ subject }, ".dh span")).toBe("two stays");
  });

  it("draws each row's span before its value, in the property's form", () => {
    const rows = [...drawn({}, KOLKATA).querySelectorAll(".fr .v")].map((v) => v.textContent ?? "");
    expect(rows[0]).toMatch(/^03 .+ → 07 .+penalty/u);
    expect(rows[2]).toBe("cancelling within 48 h of arrival, per the booking's terms");
  });

  it("says what returns to inventory, and says why when nothing does", () => {
    const rows = (over: Partial<CancelPlan>): string[] =>
      [...drawn(over, KOLKATA).querySelectorAll(".fr .v")].map((v) => v.textContent ?? "");

    expect(rows({}).at(-1)).toMatch(/^both rooms return to inventory for 03 /u);
    expect(rows({ stays: 1 }).at(-1)).toMatch(/^the room returns to inventory for 03 /u);
    expect(rows({ stays: 4 }).at(-1)).toMatch(/^all four rooms return to inventory for 03 /u);
    expect(rows({ stays: 0 }).at(-1))
      .toBe("nothing returns to inventory — no stay on this booking can be cancelled");
  });
});

describe("a room type's capacity", () => {
  const rowFor = (sleeps: TypeAvailability["sleeps"]): string => {
    const type: TypeAvailability = {
      ...recordedAvailability.types[0]!, sleeps,
    };

    return availability([type]).querySelector(".nm")?.textContent ?? "";
  };

  it("says what the rate includes, and the ceiling where they differ", () => {
    expect(rowFor({ included: 2, most: 3, adults: 2, children: 1, extraBed: true, extraBeds: 1 }))
      .toContain("sleeps 2, up to 3");
  });

  it("says one number where the type has no extra bed", () => {
    expect(rowFor({ included: 2, most: 2, adults: 2, children: 0, extraBed: false, extraBeds: 0 }))
      .toContain("sleeps 2");
  });

  // ADR 0215: null is Master Data holding no row, never a capacity. A desk is
  // about to put a family in the room, so nothing is claimed.
  it("claims nothing where Master Data holds no row", () => {
    expect(rowFor(null)).not.toMatch(/sleeps/);
  });
});

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

import type { Activity } from "../book";
import { day, instant } from "../chrome/when";
import type { DayRow } from "../book/model";
import { activityTab } from "../screens/stay/activity-tab";
import { table } from "../screens/today/table";

const KOLKATA: PropertyEnvironment = { locale: "en-IN", timezone: "Asia/Kolkata" };
const UNKNOWN: PropertyEnvironment = { locale: null, timezone: null };

/** 23:40 UTC on the 31st — already the 1st, 05:10, in Kolkata. */
const LATE = "2026-08-31T23:40:00.0000000+00:00";

describe("the Activity tab", () => {
  const activity: Activity = {
    filters: [],
    entries: [{
      at: LATE,
      who: { mark: "pms", text: "Opera" },
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

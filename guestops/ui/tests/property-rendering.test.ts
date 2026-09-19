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
import { activityTab } from "../screens/stay/activity-tab";

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

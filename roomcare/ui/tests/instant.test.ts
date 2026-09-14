import { formatDay } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { day, nearDay } from "../chrome/instant";
import { host } from "./host";

/**
 * The wrapper's contract, asserted against the SDK rather than against
 * characters — page 64 §11: the form is the locale's, so a literal here would be
 * one locale's output and would drift. What Room Care owns is which style a
 * value gets, and that absent and unestablished render one way everywhere.
 */
describe("Room Care's days", () => {
  const property = host(["roomcare.read"]);

  it("gives a near day the SDK's day-month and a distant one day-month-year", () => {
    for (const iso of ["2026-08-05", "2026-09-05"]) {
      expect(nearDay(property, iso)).toBe(formatDay(iso, property.property, "day-month"));
      expect(day(property, iso)).toBe(formatDay(iso, property.property, "day-month-year"));
      expect(nearDay(property, iso)).not.toBe(day(property, iso));
    }
  });

  it("renders a property with no locale established the same way in both columns", () => {
    const unestablished = { ...property, property: { locale: null, timezone: null } };
    expect(nearDay(unestablished, "2026-09-05")).toBe(day(unestablished, "2026-09-05"));
  });

  it("draws a dash for a day nobody recorded, never today", () => {
    expect([nearDay(property, null), day(property, undefined)]).toEqual(["—", "—"]);
  });
});

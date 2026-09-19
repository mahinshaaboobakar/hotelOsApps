import { describe, expect, it } from "vitest";

import type { Waiting } from "../roster/leave";
import { queue } from "../screens/leave/approvals";

/**
 * The approval queue writes "Casual · 3 days" from the values the service
 * sends — the type and the number of days (LeaveQueueWireTests on the backend).
 * It used to receive the sentence whole, composed in the service's culture.
 */

const property = { timezone: "Asia/Kolkata", locale: "en-GB" } as const;

const texts = (root: HTMLElement): string[] =>
  Array.from(root.querySelectorAll(".row:not(.hd) s"), (one) => one.textContent ?? "");

describe("the approval queue", () => {
  it("writes a leave row's type and days, and a swap row's own words", () => {
    const rows = [
      { who: "Anjali Menon", type: "Casual", days: 3, kind: "Leave",
        dates: { from: "2026-09-14", to: "2026-09-16" } },
      // A type the property has since removed: the row still says what it is.
      { who: "Rahul Nair", type: null, days: 1, kind: "Leave",
        dates: { from: "2026-09-18", to: "2026-09-18" } },
      { who: "Fatima Noor & Irfan Qadri", what: "Swap — accepted, awaiting you", kind: "Swap",
        accepted: "2026-09-12" },
    ] as unknown as Waiting[];

    expect(texts(queue(rows, property))).toEqual([
      "Casual · 3 days", "Leave · 1 day", "Swap — accepted, awaiting you",
    ]);
  });

  it("writes the days in the property's digits", () => {
    const rows = [{ who: "x", type: "Casual", days: 3, kind: "Leave",
      dates: { from: "2026-09-14", to: "2026-09-16" } }] as unknown as Waiting[];

    // Non-empty first: an empty row has no ASCII digit either, and the first
    // draft of this test passed on the old code for exactly that reason.
    const [row] = texts(queue(rows, { timezone: "Asia/Kolkata", locale: "ar-EG" }));
    expect(row).toContain("Casual");
    expect(row).not.toMatch(/[0-9]/);
  });
});

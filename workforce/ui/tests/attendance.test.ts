import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { attendance } from "../screens/attendance";
import { recordedDay } from "../roster/attendance";

/**
 * What the rota planned, in three states the wire now distinguishes.
 *
 * # The column was one string holding three things
 *
 * `posted` arrived as a clock, or the word `rostered`, or null — so a surface
 * could not tell a time it must render from a word it must not, and could not
 * say that word in any language but the service's. It is `rostered` and
 * `postedAt` now (ADR 0175).
 *
 * # And the shift code was never on the wire
 *
 * The screen split `"M 07:00"` on a space and drew a code chip from the first
 * half. `DayComparison.DayRow` carries a department code, a rostered flag and a
 * scheduled start — **no shift code at all** — so only the fixture ever had one
 * and a real property drew an empty chip beside a time. The chip is gone until
 * the wire carries something to put in it.
 */

function host(day: unknown, locale: string | null = "en-GB"): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale },
    call: (_capability: string, method: string) => method === "day"
      ? Promise.resolve(day)
      : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" })),
    on: () => () => {},
  };
}

async function drawn(rows: unknown[], locale?: string | null): Promise<HTMLElement> {
  const main = document.createElement("div");
  await attendance(host({ ...recordedDay, rows }, locale), main);

  return main;
}

const BASE = {
  who: "Anjali Menon", role: "Receptionist", in: null, out: null,
  against: "Absent", tone: "bad", source: null,
};

describe("attendance, what the rota planned", () => {
  it("separates nothing rostered from rostered with no start", async () => {
    const main = await drawn([
      { ...BASE, rostered: false, postedAt: null },
      { ...BASE, who: "Joseph Kurian", rostered: true, postedAt: null },
    ]);

    const cells = Array.from(main.querySelectorAll(".postedcell"))
      .map((one) => one.textContent);

    // Two different facts. The old shape said the second with a word the
    // service chose, and the first with null — and a screen could not tell
    // which of the two an unfamiliar string was.
    expect(cells).toContain("not rostered");
    expect(cells).toContain("rostered");
  });

  it("renders the planned start in the property's own clock", async () => {
    const british = await drawn([{ ...BASE, rostered: true, postedAt: "07:00" }]);
    const american = await drawn(
      [{ ...BASE, rostered: true, postedAt: "07:00" }], "en-US");

    expect(british.querySelector(".postedcell")?.textContent).toBe("07:00");

    // The same value, a different reader. A service that had formatted this
    // would have shipped one property's hour cycle to every property.
    expect(american.querySelector(".postedcell")?.textContent).toBe("07:00 AM");
  });

  it("draws no shift code, because the wire carries none", async () => {
    const main = await drawn([{ ...BASE, rostered: true, postedAt: "07:00" }]);

    // Asserting the ABSENCE, because the defect was an empty chip rather than a
    // missing one: `codeChip("")` renders an element with nothing in it, which
    // reads as a rendering fault rather than as a gap in the data.
    expect(main.querySelector(".postedcell .code")).toBeNull();
  });

  it("counts against what was rostered, not against what parsed", async () => {
    const main = await drawn([
      { ...BASE, rostered: true, postedAt: "07:00", in: "07:02", against: "On time" },
      { ...BASE, rostered: true, postedAt: null },
      { ...BASE, rostered: false, postedAt: null, in: "09:05" },
    ]);

    // Two rostered, one of them present; the third is present and unrostered.
    //
    // The old code counted `posted !== null` and got this RIGHT — but only
    // because the sentinel string `"rostered"` happens to be non-null. The
    // denominator was correct by way of a magic value, so the day somebody
    // replaced that word with an empty string for a tidier screen, a count on
    // another part of the page would have moved. It reads `rostered` now, which
    // is the fact rather than a side effect of how the fact was spelled.
    expect(main.textContent).toContain("1 of 2");
  });
});

import type { HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { recordedWeek } from "../roster/recorded";
import { grid } from "../screens/rota/grid";

/**
 * A rota cell is a button a keyboard reaches, and it says whose day it is.
 *
 * `tests/mouse-only` holds the element type for every surface. This holds what
 * a cell is CALLED: its own text is a code, a "＋" or "gap — cover?", which read
 * aloud names neither the person nor the day it would change.
 */
const property: HostApi["property"] = { timezone: "Asia/Kolkata", locale: "en-GB" };

function cells(): HTMLButtonElement[] {
  const table = grid(recordedWeek.days, recordedWeek.people, property, () => {});
  return Array.from(table.querySelectorAll<HTMLButtonElement>(":scope > button"));
}

describe("rota cells", () => {
  it("are all buttons that submit nothing", () => {
    const all = cells();
    expect(all).toHaveLength(recordedWeek.people.length * 7);
    expect(all.every((one) => one.type === "button")).toBe(true);
  });

  it("name the person, the day and the shift with its hours", () => {
    // Priya's Monday is Morning, 07:00–15:00, in the fixture.
    expect(cells()[0]!.getAttribute("aria-label")).toBe("Priya Thomas · Mon 24 · Morning, 07:00–15:00");
  });

  it("open the picker on the person and day they belong to", () => {
    const opened: [string, number][] = [];
    const table = grid(recordedWeek.days, recordedWeek.people, property,
      (person, day) => { opened.push([person.id, day]); });

    // The second person's third day: a button's click is what a keyboard's
    // Enter and Space dispatch.
    table.querySelectorAll<HTMLButtonElement>(":scope > button")[7 + 2]!.click();
    expect(opened).toEqual([[recordedWeek.people[1]!.id, 2]]);
  });
});

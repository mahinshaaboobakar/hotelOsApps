import { describe, expect, it } from "vitest";

import { SUPERVISOR, settle } from "./host";
import { PLACES, current, pressable, reach } from "./places";
import type { Call } from "./host";

/**
 * No control looks live and does nothing (owner ruling, 2026-09-19: dead
 * buttons found by using the product, after the pages were approved).
 *
 * Every enabled `<button>` on every place a person can reach is pressed, one
 * per fresh mount, and the press must do something observable: make a call,
 * change what is drawn (a redraw, a sheet, a navigation), or scroll something
 * into view. A control that cannot act is drawn off — a `.btn.off` span with
 * its reason, or `disabled` — and is not a button this walk presses. The one
 * exemption is the CURRENT choice (`aria-pressed="true"`, or the pager's page
 * being shown): pressing what is already chosen changes nothing, correctly.
 * That exemption is why a choice with no alternative is tested on its own
 * below — the walk cannot see it. The places are `tests/places.ts`'s, shared with
 * the developer-content walk.
 */
describe("every control a person can press", () => {
  for (const [place, capabilities, section, tab] of PLACES) {
    it(`does something when pressed — ${place}`, async () => {
      const count = pressable(await reach(capabilities, section, tab, [])).length;
      expect(count, "the walk found no controls; it is not measuring this place").toBeGreaterThan(0);
      const dead: string[] = [];
      for (let i = 0; i < count; i += 1) {
        const calls: Call[] = [];
        const root = await reach(capabilities, section, tab, calls);
        const button = pressable(root)[i];
        if (button === undefined) continue;
        if (current(button)) continue;
        const before = root.innerHTML;
        const made = calls.length;
        let scrolled = false;
        const scroll = Element.prototype.scrollIntoView;
        Element.prototype.scrollIntoView = () => { scrolled = true; };
        button.click();
        await settle();
        Element.prototype.scrollIntoView = scroll;
        if (calls.length === made && root.innerHTML === before && !scrolled) dead.push(`${i}: "${button.textContent?.trim()}"`);
      }
      expect(dead, `looks live and does nothing on ${place}`).toEqual([]);
    }, 60_000);
  }
});

describe("a choice with nothing to choose between", () => {
  // Zone is the only grouping the board and the Room states sheet have. Drawn as a pressed chip it looks like one
  // option of several, and pressing it does nothing — so it is said as a label, not offered as a control.
  it("is said, not offered as a control — the board and the Room states sheet group by zone", async () => {
    for (const section of ["Board", "Room states"]) {
      const root = await reach(SUPERVISOR, section, null, []);
      const zone = [...root.querySelectorAll(".body button")].filter((b) => b.textContent?.trim() === "Zone");
      expect(zone, section).toEqual([]);
      expect(root.querySelector(".body")?.textContent, section).toContain("grouped by zone");
    }
  });
});

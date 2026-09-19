import { describe, expect, it } from "vitest";

import { SURFACES, surfaceHost } from "./surfaces";

/**
 * Nothing drawn as a control is anything but a control.
 *
 * Architect's order, 2026-09-19: *"Nothing may look live and do nothing."* Six
 * department and person pickers were `div.sel` — a box, a name and a ▾, with
 * no element a person could press or tab to — on Attendance, People, Reports,
 * the Rota, My Schedule and Teams. None was in any list of dead actions,
 * because a list of actions is a list of the things somebody wired.
 *
 * **Keyed on the classes the stylesheet draws as pressable**, not on `.sel`
 * alone: `.btn`, `.tab` and `.pk` carry `cursor: pointer` in
 * `chrome/styles.ts`, and `.sel` draws the ▾ a person reads as a menu. An
 * element carrying one of those is a promise, and only a real control keeps it
 * — `button` (live, or disabled with its reason by `unavailable()`), `select`
 * or `input`.
 *
 * What it cannot see, stated: a real `button` with no listener. That is held
 * by each write's own test and by the capability ledger, which lists every
 * control with the operation it calls.
 */
const PRESSABLE = ".sel, .btn, .tab, .pk";
const CONTROLS = new Set(["BUTTON", "SELECT", "INPUT"]);

const host = surfaceHost("en-GB");
let seen = 0;

describe("no dead controls", () => {
  for (const [name, draw] of SURFACES) {
    it(`${name} draws no control that is not one`, async () => {
      const main = document.createElement("div");
      await draw(host, main);
      expect(main.querySelector(".fail"), `${name} could not read its fixture`).toBeNull();

      const drawn = Array.from(main.querySelectorAll<HTMLElement>(PRESSABLE));
      seen += drawn.length;

      const dead = drawn
        .filter((one) => !CONTROLS.has(one.tagName))
        .map((one) => `${one.tagName.toLowerCase()}.${one.className} "${one.textContent?.trim()}"`);

      expect(dead).toEqual([]);
    });
  }

  // Positive control: the walk found controls to judge, so a clean run is a
  // result and not a selector that matched nothing.
  it("judged something", () => {
    expect(seen).toBeGreaterThan(20);
  });
});

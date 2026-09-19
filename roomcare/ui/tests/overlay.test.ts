import { describe, expect, it } from "vitest";

import { dialog, sheet } from "../chrome/overlay";

/**
 * Page 64 §9: "The scrim dismisses; the surface does not." A click that lands on
 * the sheet's own body — choosing an option, selecting text — must never close
 * what a person is composing; a click on the dimmed page around it closes it.
 */
describe("an overlay", () => {
  for (const [name, open] of [["sheet", sheet], ["dialog", dialog]] as const) {
    it(`a ${name} stays open when its own surface is clicked, and closes when the scrim is`, () => {
      const frame = document.createElement("div");
      document.body.replaceChildren(frame);
      const overlay = open(frame, "the test's overlay");
      const surface = frame.querySelector<HTMLElement>("[role=dialog]")!;
      const scrim = surface.parentElement!;

      surface.click();
      overlay.body.dispatchEvent(new MouseEvent("click", { bubbles: true }));
      expect(frame.contains(surface)).toBe(true);

      scrim.click();
      expect(frame.contains(surface)).toBe(false);
    });
  }
});

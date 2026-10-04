/**
 * Frame 3's amber band — and the two controls that drew off while the
 * capability they needed was complete.
 *
 * **`ReconciliationService.ClearAsync` carried GUEST-Q3 (2) and (3) in full,
 * with three tests driving it, and no door served it.** The band said *"not
 * available from this screen yet"*, which was true of the screen and false of
 * the application.
 *
 * **The fixture is chosen so the two sides disagree.** Both controls carry a
 * caption and a side, and nothing about either caption says which side it is —
 * so a band that read the English, or that sent one side for both, passes any
 * test asserting only that two buttons exist.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { beforeEach, describe, expect, it } from "vitest";

import type { Banner, ClearSide } from "../book/model";
import { recordedStay } from "../book/recorded";
import { banner } from "../screens/stay/banner";

const settled: { id: string; side: ClearSide }[] = [];

function band(over: Partial<Banner> = {}): HTMLElement {
  return banner(
    { ...(recordedStay.banner as Banner), ...over },
    (id, side) => settled.push({ id, side }),
  );
}

const buttons = (root: HTMLElement): HTMLButtonElement[] =>
  [...root.querySelectorAll("button")];

/**
 * The nth control, asserted to be there.
 *
 * `noUncheckedIndexedAccess` is on, and the honest answer is an assertion
 * rather than a non-null claim: a band that drew one control would otherwise
 * fail on a property access three lines later, naming the wrong thing.
 */
function nth(root: HTMLElement, index: number): HTMLButtonElement {
  const control = buttons(root)[index];

  expect(control, `the band draws a control at ${index}`).toBeDefined();
  return control as HTMLButtonElement;
}

describe("the disagreement band", () => {
  beforeEach(() => { settled.length = 0; });

  it("offers both ways out, live", () => {
    const controls = buttons(band());

    expect(controls.map((one) => one.textContent)).toEqual(["Keep 214", "Take 208"]);
    expect(controls.map((one) => one.disabled)).toEqual([false, false]);
  });

  it("keeps ours, naming the row rather than the stay", () => {
    nth(band(), 0).click();

    expect(settled).toEqual([
      { id: recordedStay.banner?.disagreementId, side: "ours" },
    ]);
  });

  it("takes the PMS's, which is the side that publishes", () => {
    // **The discriminator.** A band that sent one side for both controls would
    // pass every assertion above, and this is the arm that announces a room
    // change to Room Care — GUEST-Q3 (3).
    nth(band(), 1).click();

    expect(settled).toEqual([
      { id: recordedStay.banner?.disagreementId, side: "pms" },
    ]);
  });

  it("reads the side from the action, not from its caption", () => {
    // Reworded captions, sides unchanged: the band still settles the right way.
    // A screen deciding by English would send `ours` for both of these.
    const reworded = band({
      actions: [
        { label: "Take 208", side: "pms" },
        { label: "Keep 214", side: "ours" },
      ],
    });

    nth(reworded, 0).click();
    nth(reworded, 1).click();

    expect(settled.map((one) => one.side)).toEqual(["pms", "ours"]);
  });

  it("still says what it is about, and whose entry stands", () => {
    const text = band().textContent ?? "";

    expect(text).toContain("The PMS disagrees");
    expect(text).toContain("not applied");
    expect(text).toContain("stands everywhere");
  });
});

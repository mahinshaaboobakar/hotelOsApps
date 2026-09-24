/**
 * C2 — correcting from the Activity entry it corrects. The owner's ruling of
 * 2026-09-24.
 *
 * **The service decides which entry the action reaches**, from what the stay
 * is now: a departure on a stay already corrected is history, not a mistake
 * awaiting repair. What is checked here is that the row renders the flag it is
 * given, and that the control sits ON the entry rather than below it.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { beforeEach, describe, expect, it } from "vitest";

import type { PropertyEnvironment } from "@hotelos/sdk";

import type { Activity, ActivityEntry } from "../book";
import { activityTab } from "../screens/stay/activity-tab";

const property: PropertyEnvironment = { timezone: "Asia/Kolkata", locale: "en-IN" };

const pressed: string[] = [];

function entry(over: Partial<ActivityEntry>): ActivityEntry {
  return {
    at: "2026-09-24T07:02:00+05:30",
    who: { mark: "override", text: "Anitha Menon" },
    what: "Checked out",
    detail: "by Anitha Menon",
    disagrees: false,
    mayCorrect: false,
    ...over,
  };
}

function drawn(entries: readonly ActivityEntry[]): HTMLElement {
  const activity: Activity = { filters: [], entries };
  const root = document.createElement("div");
  root.append(...activityTab(activity, property, () => pressed.push("correct")));
  return root;
}

const action = (root: HTMLElement): HTMLButtonElement | undefined =>
  [...root.querySelectorAll("button")].find((b) => b.textContent === "this is wrong");

describe("C2 — correcting from Activity", () => {
  beforeEach(() => { pressed.length = 0; });

  it("offers the action on the entry the service marked", () => {
    const control = action(drawn([entry({ mayCorrect: true })]));

    expect(control).toBeDefined();
    control?.click();

    expect(pressed).toEqual(["correct"]);
  });

  it("offers it on no other entry", () => {
    // Three entries, one correctable — so a screen that put the control on
    // every row, or on the first, fails here. The frame's caption is the
    // rule: "only on entries a correction can reach".
    const root = drawn([
      entry({ what: "Assigned 214 · checked in", mayCorrect: false }),
      entry({ what: "Checked out", mayCorrect: true }),
      entry({ what: "Room Care was told 214 is vacated", mayCorrect: false }),
    ]);

    expect(root.querySelectorAll("button").length).toBe(1);
  });

  it("sits ON the entry, not on a row of its own below it", () => {
    // **The capture found this and no DOM assertion could have.** `.w` stacks
    // its children, so an inline control dropped straight in becomes a
    // full-width row of its own — centred mid-frame, with the byline pushed
    // down. The control and the words share one element; the detail does not.
    const root = drawn([entry({ mayCorrect: true })]);
    const control = action(root);

    expect(control?.parentElement?.className).toBe("ttl");
    expect(control?.parentElement?.textContent).toContain("Checked out");

    // And the detail is a sibling of that element, not inside it.
    expect(control?.parentElement?.textContent).not.toContain("by Anitha Menon");
  });
});

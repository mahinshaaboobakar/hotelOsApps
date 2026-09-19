import { describe, expect, it } from "vitest";

import { stylesheet } from "../chrome/styles";
import { click, settle } from "./host";
import { PLACES, reach } from "./places";

const STYLES = stylesheet().textContent ?? "";

/**
 * A row that opens something has a real button in its main cell — APPS-Q50 (planner, 2026-09-19).
 *
 * Page 64 §2 says a row that opens something is a real `<button>`; §4 says the list is a table, and a table row
 * cannot be a button. The ruled shape meets both: the row keeps its click for a pointer, and its key text is one
 * `button.opener` that a keyboard reaches and a screen reader announces. The button carries no handler of its
 * own — its click is the row's click, bubbled — so a row cannot do one thing for a pointer and another for a key.
 * GuestOps built the same shape first (`b315edf`); Room Care's C8 closes with it.
 *
 * The rows are found through the shared walk (`tests/places.ts`), every place a person reaches, so the check
 * covers the lists that exist rather than the ones somebody remembered — and it asserts it found some.
 */
describe("rows that open something", () => {
  it("each carry exactly one real button, holding the row's key text", async () => {
    let rows = 0;
    const missing: string[] = [];
    for (const [place, capabilities, section, tab] of PLACES) {
      const root = await reach(capabilities, section, tab, []);
      // The Board opens on the Map; its Wall is a list of rows, so it is visited too.
      if (section === "Board") {
        click(root, ".chips button", "Wall");
        await settle();
      }
      for (const row of root.querySelectorAll<HTMLElement>(".body tr.pick, .body table.wall:not(.states) tr.g")) {
        rows += 1;
        const openers = row.querySelectorAll("button.opener");
        if (openers.length !== 1 || (openers[0]?.textContent?.trim() ?? "") === "") missing.push(`${place}: ${row.textContent?.trim().slice(0, 40)}`);
      }
    }
    expect(rows, "the walk found no opening rows; it is not measuring anything").toBeGreaterThanOrEqual(20);
    expect(missing).toEqual([]);
  }, 60_000);

  it("do the row's own thing when the button is pressed — a keyboard gets what a pointer gets", async () => {
    const root = await reach(["roomcare.read", "roomcare.assign", "roomcare.amend", "roomcare.configure", "roomcare.plan"], "Supervision", null, []);
    const opener = root.querySelector<HTMLButtonElement>(".body tr.pick button.opener");
    expect(opener).not.toBeNull();
    opener?.click();
    await new Promise((done) => setTimeout(done, 0));
    await new Promise((done) => setTimeout(done, 0));
    expect(root.querySelector(".body tr.pick"), "pressing a lane row's button opens its room").toBeNull();
  });

  it("show a pointer only where something opens — a zone header on the Room states sheet folds nothing", () => {
    // The wall's zone header folds its zone; the Room states sheet and compact reuse the wall's table and drew the
    // same pointer on a header that does nothing (the page-64 audit's two td cells).
    expect(STYLES).not.toMatch(/table\.wall tr\.g td\{[^}]*cursor:pointer/);
    expect(STYLES).toMatch(/\.opener\{[^}]*cursor:pointer/);
    expect(STYLES).toMatch(/\.opener:focus-visible\{[^}]*outline:2px/);
  });
});

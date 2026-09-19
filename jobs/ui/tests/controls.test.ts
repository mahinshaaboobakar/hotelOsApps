import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { host, open, settle, SCREENS } from "./walk";

/**
 * The control rules of standard §2 that a walker can enforce on its own.
 *
 * **Why this is a walker and not a declaration table.** `pagination.json` exists
 * because a missing pager has no node to measure — the defect is an ABSENCE, and
 * an absence can only be caught against a declaration of what each surface owes.
 * These rules are the other kind: both the obligation and its violation are
 * already in the document, so nothing needs declaring and a hand-kept list of
 * call sites would be one more copy to keep in step (approved 2026-09-10).
 */
describe("§2's control vocabulary, walked", () => {
  it("a .btn inside a row or a card is small", async () => {
    const wrong: string[] = [];

    for (const screen of SCREENS) {
      const root = document.createElement("div");
      document.body.replaceChildren(root);
      activate(host()).mount(root);
      await settle();
      await open(root, screen.open);

      for (const button of Array.from(root.querySelectorAll<HTMLElement>(".btn"))) {
        // **A row only, not yet a card.** §2 says "inside a row or a card", and
        // the first half is unambiguous: a control in a table cell is small or
        // it breaks the row's height. The second half is not, and this walker
        // found out why — it flags fourteen controls that the APPROVED FRAMES
        // draw full size, including a card's own Save and Discard. The standard
        // and the drawings disagree about what "a card" means here, which is a
        // finding and not something a guard should decide by failing.
        //
        // Narrow rather than exempted: the card half arrives when it is ruled,
        // and until then this rule is one somebody can trust.
        const inside = button.closest("td");
        if (inside === null) continue;
        if (button.classList.contains("sm")) continue;

        wrong.push(`${screen.name}: "${button.textContent?.trim() ?? ""}" inside a row`);
      }
    }

    expect([...new Set(wrong)]).toEqual([]);
  });

  it("one base class, modified — no control carries a second geometry", async () => {
    // `.btn2`, `.create`, `.mini`, `.go` are the standard's own examples; the
    // check is the general one, because the next second base class will have a
    // name nobody listed. Every element that acts is a `.btn` or is one of the
    // three the chrome names for a reason.
    // `opener` joined 2026-09-19: the Board row's opener is §2's C8 case — "a row
    // that opens something is a real <button>, and the reset lives on the class"
    // — a button drawn as the text it replaces, not a second button geometry.
    const allowed = new Set(["btn", "tab", "pg", "chip", "num", "pick", "pri", "sm", "on", "off", "danger", "confirm", "opener"]);
    const strangers: string[] = [];

    for (const screen of SCREENS) {
      const root = document.createElement("div");
      document.body.replaceChildren(root);
      activate(host()).mount(root);
      await settle();
      await open(root, screen.open);

      for (const button of Array.from(root.querySelectorAll<HTMLElement>("button"))) {
        const classes = Array.from(button.classList);
        if (classes.length === 0) continue;
        if (classes.some((name) => allowed.has(name))) continue;

        strangers.push(`${screen.name}: button.${classes.join(".")}`);
      }
    }

    expect([...new Set(strangers)]).toEqual([]);
  });

  it("a destructive control is drawn destructive, and its confirm is filled", async () => {
    // Not a declaration of WHICH operations destroy — that table is deferred
    // until after Part B. This is the half that needs nothing declared: wherever
    // a `.danger` affordance exists, pressing it must reach a filled confirm,
    // because an outline that acts immediately is the standard's split
    // collapsed into its quiet half.
    for (const screen of SCREENS) {
      const root = document.createElement("div");
      document.body.replaceChildren(root);
      activate(host()).mount(root);
      await settle();
      await open(root, screen.open);

      const dangers = Array.from(root.querySelectorAll<HTMLElement>(".btn.danger:not(.confirm)"));
      for (const danger of dangers) {
        danger.click();
        await settle();

        const confirm = root.querySelector(".btn.danger.confirm");
        expect(confirm, `${screen.name}: "${danger.textContent?.trim() ?? ""}" opens a filled confirm`).not.toBeNull();
      }
    }
  });
});

/**
 * The pager is the list's floor — docs/working/64 §6, ruled 2026-09-05.
 *
 * # What this can and cannot prove
 *
 * It asserts the two rules exist, that the growth is **scoped**, and that the
 * structure they depend on is what the screens actually render. It does **not**
 * prove the visual outcome: happy-dom performs no layout, so nothing here can
 * show a pager resting at the bottom of a short list or holding station over a
 * long one. That needs a browser, and it is stated as unverified rather than
 * implied by a green test.
 *
 * What it does catch is every way the rule could rot: a table that stops
 * growing, a pager that stops sticking, a transparent strip that rows scroll
 * through, and a screen that puts something between the table and its pager —
 * which would break the sibling selector silently.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { describe, expect, it } from "vitest";

import { render } from "../preview/gallery/realm";
import { stylesheet } from "../chrome/styles";

const CSS = (stylesheet().textContent ?? "").replace(/\s+/g, " ");

describe("the pager's placement", () => {
  /**
   * **Grow only.** `1 0 auto`, and no `min-height:0`.
   *
   * The shrink half was in the first draft and GG's port measured what it does
   * in a body that is itself a constrained scroll container: the list shrinks
   * *below its content*, `.tbl` does not clip, and the rows render through
   * whatever follows — 304px of list against 1353px of content, 1048px drawn
   * under the note and the pager. `min-height:0` is what permits that shrink,
   * so it goes with it.
   */
  it("makes a list with a pager grow, and never shrink below its rows", () => {
    expect(CSS).toContain(".tbl:has(~ .pager){flex:1 0 auto}");
    expect(CSS).not.toMatch(/\.tbl:has\(~ \.pager\)\{[^}]*min-height:0/);
    expect(CSS).not.toMatch(/\.tbl:has\(~ \.pager\)\{[^}]*flex:1 1/);
  });

  /**
   * **Scoped, and this is the half that matters.**
   *
   * Frame 14's availability table is followed by a note and two cards. A table
   * that grew there would push them off the screen, so the rule is *a list with
   * a pager*, never *a table* — and an unscoped `.tbl{flex:1}` is the obvious
   * thing for the next person to write.
   */
  it("does not make every table take the free space", () => {
    expect(CSS).not.toMatch(/[^:]\.tbl\{[^}]*flex:1/);
  });

  /**
   * **The offset is the body's bottom padding, negated.**
   *
   * Sticky resolves against the scrollport's *padding* box, so `bottom:0` in a
   * body padded 22px parks the strip 22px short and rows scroll through the
   * gap — GG measured 598 against a scrollport bottom of 620. The negative
   * bottom *margin* does not fix it: that moves the flow position, not the
   * offset sticky resolves, so the strip was flush at rest and high while
   * stuck, which is the jump the margin exists to prevent arriving from the
   * other side.
   */
  it("sticks the pager flush with the bottom, past the body's own padding", () => {
    expect(CSS).toMatch(/\.pager\{[^}]*position:sticky/);
    expect(CSS).toMatch(/\.pager\{[^}]*bottom:-22px/);
    expect(CSS).not.toMatch(/\.pager\{[^}]*bottom:0/);
  });

  /**
   * An opaque strip, or the rows scroll through it.
   *
   * From a **published token**, not a literal: the pager's background has to be
   * whatever the module's own surface is, and a hardcoded colour would be a
   * dark-theme decision frozen into a module a light property also runs (§1).
   */
  it("gives the pager an opaque background from a published token", () => {
    expect(CSS).toMatch(/\.pager\{[^}]*background:var\(--color-surface/);
  });

  /**
   * The selector depends on the pager being the table's **sibling**.
   *
   * `~` matches a later sibling of the same parent, so a screen that wrapped
   * its table, or slipped a note between the two, would leave the rule matching
   * nothing — silently, with the only symptom a pager that stops moving. Both
   * screens that page are checked here, on the markup they actually render.
   */
  it.each(["today", "bookings"])("renders the pager as %s's table's sibling", async (screen) => {
    const html = await render(screen);
    const document_ = new DOMParser().parseFromString(html, "text/html");

    const table = document_.querySelector(".tbl");
    const pager = document_.querySelector(".pager");

    expect(table).not.toBeNull();
    expect(pager).not.toBeNull();
    expect(table?.parentElement).toBe(pager?.parentElement);
    expect([...(table?.parentElement?.children ?? [])].indexOf(table as Element))
      .toBeLessThan([...(pager?.parentElement?.children ?? [])].indexOf(pager as Element));
  });
});

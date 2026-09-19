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
   * **Only the list scrolls — CORE-Q28**: the list grows into the free space,
   * may shrink, and CLIPS, and the body holding it does not scroll.
   *
   * This asserted the opposite until 2026-09-19 — *"Grow only. `1 0 auto`, and
   * no `min-height:0`"* — because shrink WITHOUT clip rendered rows through
   * whatever followed (GG measured 304px of list against 1353px). That was
   * right about an element that does not clip. CORE-Q28 (2026-09-09) adds the
   * clip, and page 64 §6 is explicit that `min-height:0` comes back with it; the
   * test encoded the superseded contract (ADR 0034), and GuestOps never took
   * the ruling until the page-64 audit measured the document scrolling.
   */
  it("makes the list with a pager the scroll container, and the body not", () => {
    expect(CSS).toMatch(/\.body:has\(> \.pager\)\{[^}]*overflow:hidden/);
    expect(CSS).toMatch(
      /\.body:has\(> \.pager\) > :has\(\+ \.pager\)\{[^}]*flex:1 1 auto;min-height:0;overflow-y:auto/);
  });

  /**
   * **Scoped, and this is the half that matters.**
   *
   * Frame 14's availability table was followed by a note and two cards (removed
   * by the owner's ruling on New booking, 2026-09-19); a booking's stays and the
   * activity list still sit among other content. So the rule is *a list with a
   * pager* or *a body declared unpaged*, never *a table* — and an unscoped
   * `.tbl{flex:1}` is the obvious thing for the next person to write.
   */
  it("does not make every table take the free space", () => {
    // `.tbl` as a whole selector — at the start of a rule — never as the last
    // step of a scoped one (`.body.unpaged > .tbl` is scoped, and allowed).
    const unscoped = /(?:^|[}\n])\s*\.tbl\{[^}]*flex:1/;
    expect(CSS).not.toMatch(unscoped);

    // Proven both ways, so a regex that matches nothing cannot pass for one
    // that matches the right thing.
    expect("}\n.tbl{flex:1 1 auto}").toMatch(unscoped);
    expect("}\n.body.unpaged > .tbl{flex:1 1 auto}").not.toMatch(unscoped);
  });

  /**
   * **No sticky — "nothing scrolls past it now"** (page 64 §6, CORE-Q28).
   *
   * This asserted `position:sticky; bottom:-22px` until 2026-09-19, because
   * sticky resolves against the scrollport's padding box (GG measured 598
   * against 620). True of the mechanism CORE-Q28 replaced: with the list as the
   * scroll container the pager is a plain flex item at the floor, and a sticky
   * strip here is the checklist's G6 failure by name.
   */
  it("keeps the pager a plain floor, never sticky", () => {
    expect(CSS).toMatch(/\.pager\{flex:0 0 auto/);
    expect(CSS).not.toMatch(/\.pager\{[^}]*position:sticky/);
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

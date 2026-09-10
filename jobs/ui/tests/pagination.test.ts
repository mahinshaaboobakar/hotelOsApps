import { describe, expect, it } from "vitest";

import { activate } from "../application";
import declared from "./pagination.json";
import { host, open, settle, SCREENS } from "./walk";

/**
 * The pagination conformance table, enforced rather than kept.
 *
 * The certificate's table is generated from `pagination.json`, and this walks
 * the module to hold that file to the screens: **a list surface with no entry
 * fails, and an entry naming a screen that shows no list fails too.** Seventeen
 * hand-kept rows are honest today and stale the day screen eighteen lands; a
 * walker that fails in both directions is not.
 *
 * What counts as a list surface is deliberately narrow and structural — a
 * `table` with rows, or a timeline — because that is what a pager attaches to.
 * Chips, key/value grids and cards are not lists in the sense §6 governs, and
 * treating them as such would make the guard cry wolf until somebody widened
 * the exception list until it covered everything.
 */
/**
 * Every list surface on the screen as drawn.
 *
 * Three shapes, because Jobs draws lists three ways: a table with rows, a
 * timeline, and a stack of `.wrow` rows — the catalogue's categories and
 * Live's people are the second kind, and a finder that only knew about tables
 * reported the catalogue as listless, which is how a conformance walker passes
 * while covering nothing.
 */
function lists(root: HTMLElement): readonly Element[] {
  const tables = Array.from(root.querySelectorAll("table")).filter(
    (table) => table.querySelectorAll("tr").length > 1,
  );

  const stacks = Array.from(root.querySelectorAll("*")).filter(
    (element) => element.querySelectorAll(":scope > .wrow").length > 1,
  );

  return [...tables, ...Array.from(root.querySelectorAll(".tl")), ...stacks];
}

/** What the screen draws under its lists: a real pager, or nothing. */
function pagerKind(root: HTMLElement): "numbered" | "single-page" | "none" {
  const pager = root.querySelector(".pager");
  if (pager === null) return "none";

  const pages = Array.from(pager.querySelectorAll<HTMLButtonElement>(".pg"))
    .filter((button) => /^\d+$/.test(button.textContent ?? ""));
  return pages.length > 1 ? "numbered" : "single-page";
}

describe("every screen that shows a list is classified", () => {
  it("classifies each declared screen exactly as the certificate says", async () => {
    for (const surface of declared.surfaces) {
      const root = document.createElement("div");
      document.body.replaceChildren(root);
      activate(host()).mount(root);
      await settle();
      await open(root, surface.open);

      expect(lists(root).length, `${surface.screen} draws a list`).toBeGreaterThan(0);
      expect(pagerKind(root), `${surface.screen} draws ${surface.draws}`).toBe(surface.draws);
    }
  });

  it("fails on a list surface nobody classified", async () => {
    // The other direction, and the one that goes stale on its own: every top
    // tab, and every tab within the job, walked and matched against the file.
    const named = new Set(declared.surfaces.map((surface) => surface.screen));
    const unclassified: string[] = [];

    const screens = SCREENS;

    for (const screen of screens) {
      const root = document.createElement("div");
      document.body.replaceChildren(root);
      activate(host()).mount(root);
      await settle();
      await open(root, screen.open);

      if (lists(root).length > 0 && !named.has(screen.name)) unclassified.push(screen.name);
    }

    expect(unclassified).toEqual([]);
  });

  it("holds the finder itself: a screen invented here would be caught", async () => {
    // The guard's own guard. If `lists` stopped seeing tables — a selector
    // change, a wrapper — every screen would look listless and both tests above
    // would pass while covering nothing.
    const root = document.createElement("div");
    document.body.replaceChildren(root);
    activate(host()).mount(root);
    await settle();

    expect(lists(root).length).toBeGreaterThan(0);
    expect(pagerKind(root)).toBe("numbered");
  });
});

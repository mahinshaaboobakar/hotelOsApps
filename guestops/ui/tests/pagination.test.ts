import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";

/**
 * Every screen that draws a list is classified, and a new one fails until it is.
 *
 * # Why this is derived and not a list somebody typed
 *
 * The APPS-Q4 certificate carries a pagination conformance table with one row
 * per list-bearing screen, and the owner's rule for reading it is that **a
 * screen with a list and no row is a finding**. A table checked by hand is true
 * on the day it is written; this walks `screens/` and fails when a screen draws
 * a list the table has not classified — so the eleventh screen is covered the
 * day somebody writes it, not the day somebody remembers to look.
 *
 * Built to the shape GG established for Workforce (`d7ac0dc`), because a second
 * shape would mean two ways of asking one question.
 *
 * # What counts as a list here
 *
 * Two containers, because this module draws lists two ways. `.tbl` is the bare
 * table §4 rules — the day, the bookings, a booking's stays, the availability
 * answer. `.ev` is the activity list, which is a table with three fixed columns
 * and its own class because `.tr.act` already means *a row you can click*.
 *
 * **And a third that is not a table at all**: Attention stacks a card per thing
 * to decide. It is a list by every test that matters — a repeating sequence a
 * person scans — and it would escape a selector that only knew about tables,
 * which is exactly how a screen ends up unclassified.
 */

const SCREENS = join(import.meta.dirname, "..", "screens");

/** The containers this module draws a scannable list in. */
const LIST = /el\(\s*"div"\s*,\s*"(?:tbl|ev)\b/u;

/**
 * Attention's own shape: a card per row of the answer.
 *
 * Matched separately rather than folded into `LIST`, because the pattern is
 * different in kind — a loop over the domain's own items rather than a styled
 * container — and hiding that behind one regex would make the next reader think
 * every list here is a table.
 */
const CARD_LIST = /for \(const item of loaded\.value(?:\.cards)?\)/u;

/**
 * The classification the certificate's table states, screen by screen.
 *
 * `paged` is a list bounded only by how much the property has ever done —
 * §6's test is *is the count a fact, or a moving target*, and both of these
 * answer with a number the wire can produce. Everything else is bounded by a
 * natural key: one booking's stays, one property's room types, one stay's
 * history, one business day's exceptions. A pager on those is furniture.
 */
/**
 * **Ruled 2026-09-09: `64` §8 says every list screen and means it.** The four
 * that read `bounded` did so on the argument that each was bounded by a natural
 * key — a booking's stays, a property's room types, one day's exceptions. That
 * is a property of today's data and not of the screen: a list bounded by one
 * property's room types is unbounded the day a property has four hundred.
 *
 * `stay` is the one that stays `bounded`, and not because it is exempt: its
 * primary read has no backend method at all, so there is nothing to page. It is
 * tracked as its own item rather than as a missing pager, because estimating it
 * as a pager would hide that the anchor screen cannot load.
 */
const CLASSIFIED: Record<string, "paged" | "bounded"> = {
  today: "paged",
  bookings: "paged",

  attention: "paged",
  booking: "paged",
  // Every room type, no pager — the owner's ruling on New booking, 2026-09-19
  // (G7), made on the drawn options for this one screen. It was "paged" under
  // `64` §8's reading; the owner decided otherwise with the screen in front of
  // them.
  newbooking: "bounded",
  stay: "bounded",
};

/** Which screens draw a list, read off the source rather than remembered. */
function listBearing(): string[] {
  const found = new Set<string>();

  for (const entry of readdirSync(SCREENS, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;

    for (const file of readdirSync(join(SCREENS, entry.name))) {
      if (!file.endsWith(".ts")) continue;

      const source = readFileSync(join(SCREENS, entry.name, file), "utf8");

      if (LIST.test(source) || CARD_LIST.test(source)) {
        found.add(entry.name);
      }
    }
  }

  return [...found].sort();
}

/** Which screens call the pager, likewise read rather than remembered. */
function paging(): string[] {
  return listBearing().filter((screen) =>
    readdirSync(join(SCREENS, screen))
      .filter((file) => file.endsWith(".ts"))
      .some((file) => /\bpager\(/u.test(readFileSync(join(SCREENS, screen, file), "utf8"))));
}

describe("the pagination conformance table", () => {
  it("finds the lists, so the guard cannot be vacuously green", () => {
    // The probe that could not fail is this repository's recurring defect. If
    // either pattern stops matching, every assertion below passes over an empty
    // set and reports conformance for nothing.
    expect(listBearing().length).toBeGreaterThanOrEqual(6);
  });

  it("classifies every screen that draws a list", () => {
    const unclassified = listBearing().filter((screen) => !(screen in CLASSIFIED));

    // A screen with a list and no row in the certificate's table is a finding,
    // and this is where it becomes one.
    expect(unclassified).toEqual([]);
  });

  it("classifies nothing that has stopped drawing a list", () => {
    const drawing = new Set(listBearing());
    const stale = Object.keys(CLASSIFIED).filter((screen) => !drawing.has(screen));

    // The other direction, which a hand-kept table never catches: a row that
    // outlived its screen states a conformance nobody can check.
    expect(stale).toEqual([]);
  });

  it("gives the pager to exactly the screens the table says page", () => {
    const declared = Object.entries(CLASSIFIED)
      .filter(([, kind]) => kind === "paged")
      .map(([screen]) => screen)
      .sort();

    // Both directions in one assertion: a bounded screen that grew a pager, and
    // a paged screen that lost one, are the same defect seen from two sides.
    // The second is what the owner rejected Part A for — Today drew no pager
    // and nothing said so.
    expect(paging()).toEqual(declared);
  });

  /**
   * **Rewritten, not deleted — ADR 0034.** It asserted that Attention draws a
   * list and no pager, on the approved frame's authority: frame 12 draws none,
   * because one business day's exceptions is a list a property should not need
   * to page.
   *
   * Ruled otherwise on 2026-09-09: `64` §8 says every list screen and means it.
   * *Bounded by a natural key* is a property of today's data, not of the screen
   * — a list bounded by one property's exceptions is unbounded the day a
   * property has four hundred — and the count is information in its own right.
   */
  it("counts Attention as a list, and as one that pages", () => {
    expect(listBearing()).toContain("attention");
    expect(paging()).toContain("attention");
  });
});

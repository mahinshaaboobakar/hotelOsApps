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
const CARD_LIST = /for \(const item of loaded\.value\)/u;

/**
 * The classification the certificate's table states, screen by screen.
 *
 * `paged` is a list bounded only by how much the property has ever done —
 * §6's test is *is the count a fact, or a moving target*, and both of these
 * answer with a number the wire can produce. Everything else is bounded by a
 * natural key: one booking's stays, one property's room types, one stay's
 * history, one business day's exceptions. A pager on those is furniture.
 */
const CLASSIFIED: Record<string, "paged" | "bounded"> = {
  today: "paged",
  bookings: "paged",

  attention: "bounded",
  booking: "bounded",
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
   * Attention is the row most likely to be got wrong, so it is asserted by name.
   *
   * It is a list, it has a count the bar carries, and the wire could produce a
   * total — so §6's test does not obviously exclude it. What excludes it is the
   * approved frame: frame 12 draws no pager, because the list is one business
   * day's exceptions and a property with enough of them to need a second page
   * has a problem a pager would help it not to look at.
   */
  it("counts Attention as a list, and as one that does not page", () => {
    expect(listBearing()).toContain("attention");
    expect(paging()).not.toContain("attention");
  });
});

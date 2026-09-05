import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";

/**
 * Every screen that draws a list is classified, and a new one fails until it is.
 *
 * # Why this is derived and not a list somebody typed
 *
 * The certificate carries a pagination conformance table with one row per
 * list-bearing screen, and the owner's rule for reading it is that **a screen
 * with a list and no row is a finding**. A table checked by hand is true on the
 * day it is written; this walks `screens/` and fails when a screen draws a list
 * that the table has not classified — so the twelfth screen is covered the day
 * somebody writes it, not the day somebody remembers to look.
 *
 * # What counts as a list
 *
 * A container the module styles as rows one below another. Grids are excluded
 * deliberately and by name: the rota is people by day and the schedule is a
 * calendar, and neither is a thing you page through — §6's question is *is the
 * count a fact*, and a week has seven days whoever asks.
 */

const SCREENS = join(import.meta.dirname, "..", "screens");

/** The containers this module draws a scannable list in. */
const LIST = /el\(\s*"(?:div|table)"\s*,\s*"(rows|tgrid|tnarrow|rep|pgrid)\b/u;

/** A grid is two-dimensional. It is not a list and does not page. */
const GRID = new Set(["rota", "schedule", "duty"]);

/**
 * The classification the certificate's table states, screen by screen.
 *
 * `paged` is the one list bounded by the property's headcount. Everything else
 * is bounded by a natural key — a day, a week, a month, a department, the
 * property's own catalogue — which is §6's own test for when a pager is
 * furniture rather than a control.
 */
const CLASSIFIED: Record<string, "paged" | "bounded"> = {
  people: "paged",

  attendance: "bounded",
  leave: "bounded",
  policy: "bounded",
  printed: "bounded",
  reports: "bounded",
  shifts: "bounded",
  teams: "bounded",
};

/** Which screens draw a list, read off the source rather than remembered. */
function listBearing(): string[] {
  const found = new Set<string>();

  for (const entry of readdirSync(SCREENS, { withFileTypes: true })) {
    if (!entry.isDirectory()) continue;

    for (const file of readdirSync(join(SCREENS, entry.name))) {
      if (!file.endsWith(".ts")) continue;

      if (LIST.test(readFileSync(join(SCREENS, entry.name, file), "utf8"))) {
        found.add(entry.name);
      }
    }
  }

  return [...found].sort();
}

describe("the pagination conformance table", () => {
  it("finds the lists, so the guard cannot be vacuously green", () => {
    // The probe that could not fail is this suite's own recurring defect. If
    // the pattern stops matching, every assertion below passes over an empty
    // set and reports conformance for nothing.
    expect(listBearing().length).toBeGreaterThanOrEqual(7);
  });

  it("classifies every screen that draws a list", () => {
    const unclassified = listBearing().filter(
      (screen) => !(screen in CLASSIFIED) && !GRID.has(screen));

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

  it("gives the pager to exactly the screen the table says pages", () => {
    const paging = Object.entries(CLASSIFIED)
      .filter(([, kind]) => kind === "paged")
      .map(([screen]) => screen);

    const calling = listBearing().filter((screen) =>
      readdirSync(join(SCREENS, screen))
        .filter((file) => file.endsWith(".ts"))
        .some((file) =>
          /\bpager\(/u.test(readFileSync(join(SCREENS, screen, file), "utf8"))));

    // Both directions in one assertion: a bounded screen that grew a pager, and
    // a paged screen that lost one, are the same defect seen from two sides.
    expect(calling.sort()).toEqual(paging.sort());
  });
});

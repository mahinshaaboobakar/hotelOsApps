import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";

/**
 * Every list a widget draws is bounded by a constant, and an unbounded one fails.
 *
 * # The defect this exists to catch
 *
 * A widget's canvas is 320×384 and it does not scroll — page 56's rule, and
 * ADR 0111's scrollbar rule leaves it nothing to hint with. A body that
 * overflows is cut by `overflow:hidden`, silently: the rows are drawn, the
 * reader never sees them, and nothing on screen says they exist. **A row a
 * person cannot see is worse than a row not drawn**, because only one of the
 * two is honest about being absent.
 *
 * Both instances were found by measuring a capture, not by a test. Business Mix
 * drew every channel and every market — seven rows into a body that holds five,
 * clipped by 44px. Occupancy drew `now.types` entire, which is bounded by
 * nothing but how many room types a property configured; three on the fixture
 * desk, and a resort with eight would have clipped in production while every
 * suite stayed green.
 *
 * # Why this is derived rather than a list of the five
 *
 * The certificate's widget table names five widgets today. A sixth added next
 * month inherits the same canvas and the same silence, and a hand-kept list
 * would not know about it. This walks `widgets/entry/` and fails on any
 * iteration over a value it cannot see a bound on — so the sixth widget is
 * covered the day somebody writes it.
 *
 * # What it cannot check
 *
 * **It does not check that the bound is small enough.** That is layout, and
 * layout needs a browser; the measurement lives in the APPS-Q4 certificate,
 * where all ten panes — five drawn, five built — are recorded at
 * `scrollHeight === clientHeight`. This checks the property that made the
 * defect possible: a list drawn without any bound at all.
 */

const ENTRY = join(import.meta.dirname, "..", "widgets", "entry");

/**
 * A `for…of` over something, capturing what is iterated.
 *
 * Widgets draw rows exactly one way — `for (const x of …) body.append(row(…))`.
 * The alternative shapes (`.map`, `.forEach`) are asserted absent below, so
 * this one pattern is the whole surface rather than the part somebody
 * remembered.
 */
const ITERATION = /for \(const \w+ of ([^)]+)\) \{/gu;

/**
 * An iteration that cannot overflow, whatever the domain returns.
 *
 * `.slice(0, N)` is the bound. An inline array literal is the other safe
 * shape — the four stat tiles are written out in the source, so their count is
 * a property of the file rather than of the answer.
 */
const BOUNDED = /\.slice\(0,|^\[/u;

const files = readdirSync(ENTRY).filter((name) => name.endsWith(".ts"));

describe("widget lists are bounded", () => {
  it("finds the five entries, so an empty walk cannot pass", () => {
    expect(files.length).toBeGreaterThanOrEqual(5);
  });

  for (const name of files) {
    const source = readFileSync(join(ENTRY, name), "utf8");

    it(`${name} bounds every list it draws`, () => {
      const unbounded = [...source.matchAll(ITERATION)]
        .map((match) => (match[1] ?? "").trim())
        .filter((iterated) => !BOUNDED.test(iterated));

      expect(unbounded).toEqual([]);
    });

    it(`${name} draws rows only through the checked shape`, () => {
      // `.map` and `.forEach` would append rows past the pattern above, so the
      // guard would pass by not looking rather than by finding nothing.
      expect(source).not.toMatch(/\.(?:map|forEach)\(/u);
    });

    it(`${name} states the bound where the reader sees it`, () => {
      // A cut nobody is told about is the same defect one layer up: the rows
      // are honestly absent and the reader still believes they saw everything.
      if (!source.includes(".slice(0,")) return;
      expect(source).toMatch(/SHOWN|top \$\{|top 3|slice\(0, \d\)/u);
    });
  }
});

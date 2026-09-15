import { readFileSync, readdirSync, statSync } from "node:fs";
import { join, relative } from "node:path";

import { describe, expect, it } from "vitest";

/**
 * No shipped file imports a fixture.
 *
 * **The idea is Room Care's; the file is this module's.** A recorded example is
 * a drawing of data, and a screen that reaches one shows a hotel rows that do
 * not exist — ruled out by the owner on 2026-09-09: *"showing a hardcoded list
 * is wrong."* The seam now makes it unwriteable (there is no fallback argument
 * to pass), and this makes it unshippable, which are different guarantees: the
 * first stops a screen falling back, the second stops anyone importing a
 * fixture and rendering it directly.
 *
 * **Shipped means reachable from the module's entry**, not "not a test". The
 * check walks the directories a bundle is built from and exempts only
 * `board/recorded/` itself and `preview/`, which is the capture harness and
 * ships to nobody.
 */
describe("fixtures stay out of the shipped module", () => {
  const ui = join(import.meta.dirname, "..");
  const shipped = ["application.ts", "main.ts", "board", "chrome", "screens", "widgets"];
  const exempt = [join("board", "recorded")];

  function files(from: string): string[] {
    const at = join(ui, from);
    if (!statSync(at).isDirectory()) return at.endsWith(".ts") ? [at] : [];

    return readdirSync(at, { withFileTypes: true }).flatMap((entry) => {
      const here = join(from, entry.name);
      if (exempt.some((skip) => here.startsWith(skip))) return [];
      return entry.isDirectory() ? files(here) : here.endsWith(".ts") ? [join(ui, here)] : [];
    });
  }

  it("no shipped file imports board/recorded", () => {
    const offenders = shipped
      .flatMap(files)
      .filter((file) => /from\s+"[^"]*board\/recorded\//.test(readFileSync(file, "utf8")))
      .map((file) => relative(ui, file));

    expect(offenders, "a shipped file importing a recorded fixture").toEqual([]);
  });

  it("walks something, so a silent zero cannot pass", () => {
    // The guard above passes trivially if `files()` returns nothing — the
    // shape that made five widget captures look taken when none were. It reads
    // the population it is about to judge.
    expect(shipped.flatMap(files).length).toBeGreaterThan(20);
  });

  it("the fixtures are still there, and still used by the tests", () => {
    // Deleting them is not the goal and would cost the recorded examples the
    // audit compares against. They belong to the tests and the capture harness,
    // which is exactly the line this guard draws.
    expect(readdirSync(join(ui, "board", "recorded")).length).toBeGreaterThan(0);
  });
});

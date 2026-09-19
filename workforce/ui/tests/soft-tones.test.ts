import { readdirSync, readFileSync } from "node:fs";
import { join, relative } from "node:path";

import { describe, expect, it } from "vitest";

/**
 * The soft tones are the shell's, never mixed again here — page 64's palette:
 * *"Prefer them to a local `color-mix` of the same colour."*
 *
 * `--color-ok-soft`, `--color-warn-soft` and `--color-bad-soft` are the status
 * colour at 12% in both themes (`apps/desktop/src/styles.css`). The app surface
 * audit (2026-09-19, P3) found Workforce mixing the same three by hand at 13% —
 * a second definition of one colour, which drifts the day the shell retunes its
 * tint and nothing here is told.
 *
 * **Only 11–13% is refused**, because that is what duplicates the soft tone. A
 * border at 35% or a wash at 6% is a different strength of the status colour,
 * not the soft tone again, and P3 as written does not reach it. `--color-brand`
 * has no soft token, so its mixes are not this rule's either.
 */

const root = process.cwd();

/**
 * Every source file in the module, walked rather than listed: the population is
 * *files that can hold CSS*, and in this module that is any `.ts` — a screen's
 * stylesheet, the chrome, the widgets' sheet, or a rule written inline.
 */
function sources(dir = root): string[] {
  const found: string[] = [];
  for (const entry of readdirSync(dir, { withFileTypes: true })) {
    if (["node_modules", "tests", "preview", "dist"].includes(entry.name)) continue;
    const path = join(dir, entry.name);
    if (entry.isDirectory()) found.push(...sources(path));
    else if (entry.name.endsWith(".ts")) found.push(path);
  }
  return found;
}

const SOFT_AGAIN = /color-mix\(in srgb, var\(--color-(ok|warn|bad)[^)]*\) 1[1-3]%, transparent\)/g;

describe("the soft tones", () => {
  it("walks the stylesheets it exists to check", () => {
    // A walk that quietly found nothing would pass the test below on an empty
    // set. The chrome's sheet and one screen's are named as a floor.
    const names = sources().map((one) => relative(root, one).replace(/\\/g, "/"));
    expect(names).toContain("chrome/styles.ts");
    expect(names).toContain("screens/rota/styles.ts");
  });

  it("are never the status colour mixed again at the soft tone's strength", () => {
    const again: string[] = [];
    for (const file of sources()) {
      const lines = readFileSync(file, "utf8").split("\n");
      lines.forEach((line, at) => {
        for (const hit of line.matchAll(SOFT_AGAIN)) {
          again.push(`${relative(root, file).replace(/\\/g, "/")}:${at + 1} ${hit[0]}`);
        }
      });
    }
    expect(again).toEqual([]);
  });
});

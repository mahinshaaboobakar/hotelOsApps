import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";

import { TOKEN_NAMES } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

/**
 * The token contract, in the module's direction.
 *
 * # Why this test exists
 *
 * `SHELL-Q33` found the shell's own realm writing `var(--color-text)` and
 * `var(--color-scrollbar)` — names never published — and ruled a **derived
 * guard**: enumerate every `var(--…)` a file writes and assert each is in the
 * published record. That guard covers the shell. **This is the same guard on
 * the other side of the boundary**, where an installed application writes the
 * references.
 *
 * # What it would have caught here
 *
 * A first draft of this module used `--r-md`, `--color-warn-soft`,
 * `--color-brand-soft` and `--color-scroll-thumb`, copied from the reference
 * implementation and from the round's own brief. **None of the four reaches a
 * module**: `apps/desktop/src/shell/module-host/tokens.ts` injects
 * `TOKEN_NAMES` and nothing else, so each would have silently resolved to the
 * CSS fallback beside it — the module would look almost right, in colours the
 * platform does not own, and no type-check or capture would say so.
 */
describe("the module's token references", () => {
  /**
   * The module's root.
   *
   * `process.cwd()` rather than `import.meta.url`: under this environment the
   * global `URL` is happy-dom's, and `fileURLToPath` refuses what it returns.
   * Vitest fixes the working directory at the package root, which is the one
   * thing here that is stable in both a run and a watch.
   */
  const root = process.cwd();

  /**
   * Every stylesheet in the module, **discovered rather than listed**.
   *
   * This guard shipped with two filenames written into it and was still passing
   * six stylesheets later — covering a quarter of the module while reporting
   * green. A guard with a hand-maintained list is a guard that stops covering
   * whatever nobody remembered to add, which is the failure it exists to
   * prevent, one level up.
   */
  function stylesheets(): string[] {
    // Derived from a file this module certainly has, because that is the only
    // resolution form that survives here: under happy-dom the global `URL` is
    // the DOM's, and a bare `new URL("..", …)` does not reach `fileURLToPath`.
    const found = ["chrome/styles.ts"];

    for (const screen of readdirSync(join(root, "screens"), { withFileTypes: true })) {
      if (!screen.isDirectory()) continue;

      if (readdirSync(join(root, "screens", screen.name)).includes("styles.ts")) {
        found.push(`screens/${screen.name}/styles.ts`);
      }
    }

    return found;
  }

  /** Every `var(--name` this module writes, derived from the source itself. */
  function referenced(): Set<string> {
    const names = new Set<string>();

    for (const file of stylesheets()) {
      const source = readFileSync(join(root, file), "utf8");

      for (const match of source.matchAll(/var\(--([a-z0-9-]+)/g)) {
        const name = match[1];
        if (name !== undefined) names.add(name);
      }
    }

    return names;
  }

  it("names only tokens the host actually injects", () => {
    const published = new Set<string>(TOKEN_NAMES);
    const unpublished = [...referenced()].filter((name) => !published.has(name)).sort();

    // An unpublished name is not a contract — `tokens.ts`'s own words. A module
    // that writes one gets its fallback forever, which is the failure this
    // asserts against rather than trusts.
    expect(unpublished).toEqual([]);
  });

  it("discovers every stylesheet, so a new screen is covered the day it is written", () => {
    // Both halves matter. If the file list ever shrinks the first test passes
    // vacuously, and if the reference set empties it passes vacuously too.
    expect(stylesheets().length).toBeGreaterThanOrEqual(8);
    expect(referenced().size).toBeGreaterThan(8);
  });

  it("spells the radius the module may actually use", () => {
    // `--radius-panel` is the ONLY published radius. `--r-md` is a real shell
    // variable and stops at the realm boundary, so a module asking for it never
    // matches the platform's corners — the brief's own instruction, corrected
    // against the injection code.
    const names = referenced();

    expect(names.has("radius-panel")).toBe(true);
    expect(names.has("r-md")).toBe(false);
  });
});

import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

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
  const files = ["../chrome/styles.ts", "../screens/rota/styles.ts"];

  /** Every `var(--name` this module writes, derived from the source itself. */
  function referenced(): Set<string> {
    const names = new Set<string>();

    for (const file of files) {
      const source = readFileSync(fileURLToPath(new URL(file, import.meta.url)), "utf8");

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

  it("derives the set from the stylesheets rather than listing it", () => {
    // The guard is only worth having if a rule added tomorrow is covered the day
    // it is written. If this ever reads zero the enumeration has broken and the
    // test above would pass vacuously.
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

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

    // The widgets' sheet — a second *document* rather than a second screen:
    // five bundles share it and none of them is the module. Added the day the
    // directory was, because this guard's own history is exactly the failure of
    // not doing that: it shipped naming two stylesheets and was still green at
    // six.
    if (readdirSync(join(root, "widgets")).includes("styles.ts")) {
      found.push("widgets/styles.ts");
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

  it("injects exactly what the SDK publishes, in the harness", () => {
    // **Derived, not counted.** This file said fourteen and the contract said
    // seventeen for a week: the tone vocabulary joined the published surface
    // and the harness — a hand-written list — had no way to notice. A module
    // using `--color-ok-soft` would have photographed on its fallback while
    // rendering correctly in the product, which is the direction of error
    // nobody chases, because the capture merely looks a little duller.
    const css = readFileSync(join(root, "preview", "tokens.css"), "utf8");
    const declared = new Set(
      [...css.matchAll(/^\s*--([a-z0-9-]+):/gmu)].map((match) => match[1]!));

    expect([...declared].sort()).toEqual([...TOKEN_NAMES].sort());
  });

  it("names only tokens the host injects, or ones it derives itself", () => {
    const published = new Set<string>(TOKEN_NAMES);

    // **A name this module declares is not an unpublished token.** The surface
    // standard is explicit that a colour the published set does not carry is
    // DERIVED rather than declared — `--accent` is the brand gradient, mixed
    // from two published tokens on the module's own root — and a derived colour
    // still follows the theme. Reading those as missing tokens would have this
    // test forbid the very thing the standard requires.
    const declared = new Set<string>();
    for (const file of stylesheets()) {
      const source = readFileSync(join(root, file), "utf8");
      for (const match of source.matchAll(/--([a-z0-9-]+)\s*:/gu)) declared.add(match[1]!);
    }

    const unpublished = [...referenced()]
      .filter((name) => !published.has(name) && !declared.has(name)).sort();

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

  it("closes every function it opens, so no declaration is silently dropped", () => {
    // **Found by the capture, not by this suite.** A regex edit left
    // `color-mix(…, transparent))` in ten declarations: CSS drops a malformed
    // declaration and falls back to whatever rule is beneath it, so brand chips
    // rendered as plain panels. `tsc` cannot see inside a template string, the
    // token check above reads names and not syntax, and happy-dom computes no
    // styles — the browser was the only thing that could tell.
    for (const file of stylesheets()) {
      const css = readFileSync(join(root, file), "utf8");

      for (const declaration of css.split(/[;}]/)) {
        const opened = (declaration.match(/\(/g) ?? []).length;
        const closed = (declaration.match(/\)/g) ?? []).length;

        expect(
          { file, declaration: declaration.trim().slice(0, 70), opened, closed },
        ).toSatisfy(() => opened === closed);
      }
    }
  });

  it("gives no class name to two screens", () => {
    // **Found by a capture, and only by a capture.** Every screen's rules are
    // composed into ONE stylesheet, so a class name is module-wide. `.over` was
    // the leave balance's "overdrawn" modifier and the shift dialog's
    // full-screen scrim at the same time, and the Earned card became an overlay
    // that dimmed the entire screen. Nothing else could see it: the classes are
    // strings, the tokens were all published, and the suite renders no layout.
    const owners = new Map<string, string[]>();

    for (const file of stylesheets()) {
      const css = readFileSync(join(root, file), "utf8");

      // Only the modifier classes a screen invents — the shared chrome is
      // allowed to be referenced everywhere, which is what it is for.
      for (const match of css.matchAll(/^\.([a-z][a-z0-9-]*)(?:[.\s{,:])/gmu)) {
        const name = match[1];
        if (name === undefined) continue;

        owners.set(name, [...(owners.get(name) ?? []), file]);
      }
    }

    const shared = [...owners.entries()]
      .filter(([, files]) => new Set(files).size > 1)
      .map(([name, files]) => `${name}: ${[...new Set(files)].join(" + ")}`);

    expect(shared).toEqual([]);
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

/**
 * The module's half of the token contract.
 *
 * `apps/desktop`'s `tokens.test.tsx` guards the two directions on the platform
 * side: the shell must define every published token, and the realm must consume
 * only published names. **A module is a third direction**, and it was
 * unguarded — the first build of this stylesheet consumed four names the
 * contract never published (`--color-aurora-1`, `--color-aurora-3`,
 * `--color-surface-sunken`, `--r-md`) and every one of them silently took its
 * CSS fallback. The chips, the tab radius and the table's header band had
 * therefore never matched the platform in a real mount.
 *
 * This is that guard, mirrored: read the names out of the stylesheet the module
 * actually ships rather than from a list of what it is believed to use, so a
 * rule added tomorrow is covered the moment it is added.
 */

import { describe, expect, it } from "vitest";
import { TOKEN_NAMES } from "@hotelos/sdk";

import { stylesheet } from "../chrome/styles";

const CSS = stylesheet().textContent ?? "";

/** Every `var(--…)` the stylesheet reads. */
function consumed(): readonly string[] {
  const names = new Set<string>();

  for (const [, name] of CSS.matchAll(/var\(\s*--([a-z0-9-]+)/g)) {
    if (name !== undefined) names.add(name);
  }

  return [...names].sort();
}

/**
 * Every `--…:` the stylesheet defines for itself.
 *
 * A module may declare its own custom properties — that is ordinary CSS, and
 * putting all the derivation in one block is what keeps the rest readable. What
 * it may not do is *consume* a name the host never promised, so the guard
 * subtracts what the file defines from what it reads.
 */
function defined(): readonly string[] {
  const names = new Set<string>();

  for (const [, name] of CSS.matchAll(/(?:^|[;{\s])--([a-z0-9-]+)\s*:/g)) {
    if (name !== undefined) names.add(name);
  }

  return [...names].sort();
}

describe("what the module consumes", () => {
  it("reads only names the contract publishes", () => {
    const own = new Set(defined());

    const unpublished = consumed().filter(
      (name) => !own.has(name) && !(TOKEN_NAMES as readonly string[]).includes(name),
    );

    expect(unpublished).toEqual([]);
  });

  it("actually reads something, so the guard cannot pass on an empty match", () => {
    // A regex that stopped matching would make the assertion above vacuous and
    // green. The realm's guard has the same exposure; this states the floor.
    expect(consumed().length).toBeGreaterThan(8);
    expect(CSS.length).toBeGreaterThan(2000);
  });

  it("defines its own derivations rather than inventing host tokens", () => {
    // Everything the module declares for itself is namespaced, so a reader can
    // tell at a glance which half of the contract a name belongs to.
    const foreign = defined().filter((name) => !name.startsWith("go-"));
    expect(foreign).toEqual([]);
  });
});

describe("what the module hardcodes", () => {
  /**
   * A literal colour cannot follow a theme.
   *
   * The first build carried seventeen `rgba(…)` tints for the chips and
   * washes — every one a dark-theme decision frozen into a module that a light
   * property will also run. They are now `color-mix()` on published tokens.
   *
   * Fallbacks inside `var(--x, #hex)` are exempt and deliberate: a fallback is
   * what keeps the module readable when the host omits a token, which the
   * contract explicitly allows it to do.
   */
  it("carries no colour literal outside a var() fallback", () => {
    const withoutFallbacks = CSS.replace(/var\([^)]*\)/g, "var()");
    const literals = [...withoutFallbacks.matchAll(/rgba?\([^)]*\)|#[0-9a-f]{3,8}\b/g)]
      .map((match) => match[0]);

    expect(literals).toEqual([]);
  });

  /** The tints follow the theme because they are mixed from published colours. */
  it("derives every wash from a published colour", () => {
    const mixes = [...CSS.matchAll(/color-mix\([^;]*?\)(?=;)/g)].map((match) => match[0]);

    expect(mixes.length).toBeGreaterThan(5);
    for (const mix of mixes) {
      expect(mix).toMatch(/var\(--(color-[a-z-]+|go-[a-z-]+)/);
    }
  });
});

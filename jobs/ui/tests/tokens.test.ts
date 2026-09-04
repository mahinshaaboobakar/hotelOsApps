import { readFileSync, readdirSync, statSync } from "node:fs";
import { join } from "node:path";

import { TOKEN_NAMES } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

/**
 * Every colour a module draws must be one of the names the shell publishes —
 * ADR 0106. A module that referenced a token the host does not inject would
 * fall to its own fallback on a real property and look like a different app.
 */
describe("the module's token references", () => {
  const root = process.cwd();

  function sheets(dir = ".", found: string[] = []): string[] {
    for (const entry of readdirSync(join(root, dir))) {
      const path = join(dir, entry);
      if (entry === "node_modules" || entry === "preview") continue;
      if (statSync(join(root, path)).isDirectory()) sheets(path, found);
      else if (entry.endsWith(".ts") && !path.includes("tests")) found.push(path);
    }
    return found;
  }

  function referenced(): Set<string> {
    const names = new Set<string>();
    for (const file of sheets(".")) {
      for (const match of readFileSync(join(root, file), "utf8").matchAll(/var\(--([a-z0-9-]+)/g)) {
        const name = match[1];
        if (name !== undefined) names.add(name);
      }
    }
    return names;
  }

  it("names only tokens the shell publishes", () => {
    const published = new Set<string>([...TOKEN_NAMES, "font-sans"]);
    const unknown = [...referenced()].filter((name) => !published.has(name));
    expect(unknown).toEqual([]);
  });

  it("falls back to the platform's values, never the drawing's", () => {
    // A screen that loses the token contract must degrade to HotelOS, not to
    // the mockup: the fallbacks shipped the drawing's palette (#6b7cff brand,
    // #ff5c7a bad) until the convergence sweep found them.
    const published = new Map<string, string>();
    const css = readFileSync(join(root, "preview", "tokens.css"), "utf8");
    for (const line of css.split(String.fromCharCode(10))) {
      const m = /^\s*(--[a-z0-9-]+)\s*:\s*(.+?);\s*$/.exec(line);
      if (m !== null && m[1] !== undefined && m[2] !== undefined) published.set(m[1], m[2]);
    }
    expect(published.size).toBeGreaterThan(10);

    // A fallback may itself hold brackets — rgb(255 255 255 / 0.07) — so the
    // closing bracket is found by counting, not by the first one seen.
    function fallbacks(source: string): Array<[string, string]> {
      const found: Array<[string, string]> = [];
      for (const match of source.matchAll(/var\((--[a-z0-9-]+)\s*,\s*/g)) {
        const name = match[1];
        if (name === undefined || match.index === undefined) continue;
        let depth = 1;
        let at = match.index + match[0].length;
        const from = at;
        while (at < source.length && depth > 0) {
          if (source[at] === "(") depth += 1;
          else if (source[at] === ")") depth -= 1;
          if (depth > 0) at += 1;
        }
        found.push([name, source.slice(from, at)]);
      }
      return found;
    }

    const flat = (text: string): string => text.replace(/\s+/g, "").toLowerCase();
    const wrong: string[] = [];
    for (const file of sheets()) {
      for (const [name, fallback] of fallbacks(readFileSync(join(root, file), "utf8"))) {
        const want = published.get(name);
        if (want === undefined) continue;
        if (flat(fallback) !== flat(want)) {
          wrong.push(`${name}: ships ${fallback.trim()}, the platform publishes ${want}`);
        }
      }
    }

    expect([...new Set(wrong)]).toEqual([]);
  });

  it("references the tokens the frames actually use", () => {
    const used = referenced();
    for (const name of ["color-surface", "color-ink", "color-line", "color-brand", "color-ok", "color-warn", "color-bad"]) {
      expect(used.has(name)).toBe(true);
    }
  });
});

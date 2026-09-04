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

  function sheets(dir: string, found: string[] = []): string[] {
    for (const entry of readdirSync(join(root, dir))) {
      const path = join(dir, entry);
      if (entry === "node_modules") continue;
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

  it("references the tokens the frames actually use", () => {
    const used = referenced();
    for (const name of ["color-surface", "color-ink", "color-line", "color-brand", "color-ok", "color-warn", "color-bad"]) {
      expect(used.has(name)).toBe(true);
    }
  });
});

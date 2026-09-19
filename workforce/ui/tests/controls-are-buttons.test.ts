import { readFileSync, readdirSync } from "node:fs";
import { join, relative } from "node:path";
import { describe, expect, it } from "vitest";

/**
 * Every `.btn` is a real `<button>` — made with `control()`, never
 * `el("div", "btn …")`.
 *
 * `chrome/element.ts` says why, and has since the first round: *"a div is not
 * focusable, not announced, and not operable from a keyboard. The class is the
 * mockup's; the element is a button."* The app surface audit (2026-09-19, C8)
 * still found `div.btn` controls on Policy and in two sheets — because the rule
 * lived in a comment beside a helper, and nothing refused the other spelling.
 *
 * **By call shape, across every production file**, not by the screens an
 * audit happened to reach: a search keyed on the screens it visited finds only
 * those.
 */

const UI = join(import.meta.dirname, "..");

function sources(directory: string): string[] {
  const found: string[] = [];
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name);
    if (entry.isDirectory() && !["node_modules", "tests", "preview"].includes(entry.name)) {
      found.push(...sources(path));
    } else if (entry.isFile() && entry.name.endsWith(".ts")) {
      found.push(path);
    }
  }
  return found;
}

describe("a control is a button", () => {
  const files = sources(UI);

  it("walks every production file", () => {
    // A walk that quietly found nothing would pass the check below.
    expect(files.length).toBeGreaterThan(50);
  });

  it("never builds a .btn out of a div", () => {
    const offenders: string[] = [];
    for (const file of files) {
      const lines = readFileSync(file, "utf8").split("\n");
      lines.forEach((line, index) => {
        if (/el\(\s*"(div|span)"\s*,\s*"btn\b/u.test(line)) {
          offenders.push(`${relative(UI, file)}:${String(index + 1)}  ${line.trim()}`);
        }
      });
    }
    expect(offenders).toEqual([]);
  });
});

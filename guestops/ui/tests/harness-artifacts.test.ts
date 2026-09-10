// @vitest-environment node

/**
 * Every script a harness page loads is something a build produces.
 *
 * **BB's rule, in the runtime that inherits it silently.** A Part B run either
 * builds what it launches or records the commit of every out-of-process binary
 * it started; `dotnet test --no-build` pins the test assembly and not the
 * services a fixture launches, so a signed certificate was a real run of a
 * binary that no longer existed. Part A has the same shape and no flag to
 * blame: the sweep measures a browser rendering bundles that are **gitignored**,
 * built at whatever moment somebody last ran a script.
 *
 * **This is the check that would have caught the one that was already there.**
 * `preview/widget-frame.js` is gitignored, is bundled output, and is loaded by
 * `preview/widget.html` — the page every widget capture comes from — and
 * **nothing in `package.json` built it.** It sat five days old while its
 * siblings were rebuilt hourly: two SDK contract fixes behind, and short the
 * two `color-scroll-thumb` tokens, on the harness I had used to answer a
 * question about a widget scrollbar.
 *
 * A stale artifact is invisible by construction — it renders, it looks right,
 * and nothing anywhere says which build it came from. So the guard is not
 * *"is it fresh"*, which no test can answer; it is **"is it something a build
 * command produces"**, which is checkable and which the missing one failed.
 */

import { execFileSync } from "node:child_process";
import { readFileSync, readdirSync } from "node:fs";
import { fileURLToPath } from "node:url";
import { join } from "node:path";

import { describe, expect, it } from "vitest";

const UI = fileURLToPath(new URL("..", import.meta.url));

const { provenance } = (await import(
  new URL("../preview/parta/provenance.mjs", import.meta.url).href
)) as { provenance: (root: string, o?: { build?: boolean }) => { artifacts: { path: string }[] } };

/** Every `<script src="...js">` on every harness page. */
function loaded(): { page: string; src: string }[] {
  const found: { page: string; src: string }[] = [];

  for (const page of readdirSync(join(UI, "preview")).filter((f) => f.endsWith(".html"))) {
    const html = readFileSync(join(UI, "preview", page), "utf8");

    for (const m of html.matchAll(/<script[^>]*\ssrc="(?<src>[^"]+\.js)"/gu)) {
      found.push({ page, src: m.groups!.src.replace(/^\.\//u, "") });
    }
  }

  return found;
}

/** Files git does not track — the ones with no provenance of their own. */
function untracked(paths: string[]): string[] {
  return paths.filter((path) => {
    try {
      const out = execFileSync("git", ["ls-files", "--error-unmatch", `preview/${path}`], {
        cwd: UI,
        encoding: "utf8",
        stdio: ["ignore", "pipe", "ignore"],
      });

      return out.trim().length === 0;
    } catch {
      return true;
    }
  });
}

describe("a harness page loads nothing of unknown provenance", () => {
  it("finds the pages and their scripts, so the walk cannot be vacuous", () => {
    const all = loaded();

    expect(all.length).toBeGreaterThan(0);
    expect(new Set(all.map((one) => one.page)).size).toBeGreaterThan(1);
  });

  it("builds every untracked script some page loads", () => {
    // Derived from `package.json`'s own build lines, never a list written
    // beside them — a bundle added tomorrow is covered the day its build line
    // is, and one added without a build line fails here rather than going
    // quietly stale for five days.
    const built = new Set(
      provenance(UI, { build: false }).artifacts
        .map((one) => one.path.replace(/^preview\//u, "")),
    );

    const orphans = untracked([...new Set(loaded().map((one) => one.src))])
      .filter((src) => !built.has(src));

    expect(
      orphans,
      "loaded by a harness page, untracked by git, and produced by no build "
      + "script — so every capture taken from that page rendered whatever was "
      + "last on disk, with nothing able to say when or from what: "
      + orphans.join(", "),
    ).toEqual([]);
  });
});

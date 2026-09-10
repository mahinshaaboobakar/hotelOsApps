import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";

import { describe, expect, it } from "vitest";

/**
 * No harness page may load a bundle the browser is free to serve from cache.
 *
 * `ARCH-Q25`. `frame.html` used to import `./frame.js?build=` plus a query
 * parameter that **nothing in either repository ever passed**, so the module
 * URL was constant and the browser cached it; `widgets.html` had no buster at
 * all. The file carried a comment saying exactly what would happen, and it
 * happened anyway — twice in one afternoon, to the person who had just read it.
 *
 * **A stale capture is indistinguishable from a correct one**: a real screen,
 * rendered properly, of code that no longer exists. A capture is evidence, so
 * this is a guard rather than a note.
 *
 * It is derived from the directory rather than from a list of the two pages
 * that exist today, so a third harness page is covered the day it is written —
 * which is the failure mode a hand-kept list has, and this module has spent the
 * day closing that shape elsewhere.
 */

const PREVIEW = join(import.meta.dirname, "..", "preview");

function pages(): readonly string[] {
  return readdirSync(PREVIEW).filter((name) => name.endsWith(".html"));
}

function body(name: string): string {
  return readFileSync(join(PREVIEW, name), "utf8");
}

/** Every `./thing.js` a page reaches for, however it reaches. */
function bundleReferences(html: string): readonly string[] {
  return [
    ...html.matchAll(/(?:src|import\()\s*=?\s*["'](\.\/[^"']+\.js[^"']*)["']/g),
  ].map((match) => match[1]!);
}

describe("the capture harness", () => {
  it("has pages to check, so a passing run is not an empty one", () => {
    // The instrument first. A guard that walks a directory reports "no
    // violations" just as cheerfully when it walked nothing at all, and this
    // module produced five wrong numbers in one afternoon from exactly that.
    expect(pages().length).toBeGreaterThanOrEqual(2);
  });

  it("never loads a bundle with a plain script src", () => {
    const offenders = pages().filter((name) =>
      /<script[^>]*\ssrc\s*=\s*["']\.\/[^"']+\.js["']/.test(body(name)));

    // A `<script src="./frame.js">` is the un-bustable form: the URL never
    // changes, so the second capture onwards is whatever the browser kept.
    expect(offenders).toEqual([]);
  });

  it("routes every bundle through the deriving loader", () => {
    const wrong: string[] = [];

    for (const name of pages()) {
      const html = body(name);
      for (const reference of bundleReferences(html)) {
        // The loader itself is the one exception, and it is exempt because it
        // is always re-fetched: it carries `?t=` + the clock, costs a few
        // hundred bytes, and leaves nothing to forget.
        if (reference.startsWith("./boot/")) continue;
        wrong.push(`${name} reaches ${reference} directly`);
      }
      if (html.includes(".js") && !html.includes("boot/fresh.js")) {
        wrong.push(`${name} loads a bundle without the loader`);
      }
    }

    expect(wrong).toEqual([]);
  });

  it("refuses the parameter it used to depend on", () => {
    // `?build=` is gone rather than ignored. A parameter that silently does
    // nothing is worse than one that has been removed: whoever passes it
    // believes they are choosing which bundle gets photographed.
    for (const name of pages()) {
      const html = body(name);
      if (!html.includes("boot/fresh.js")) continue;
      expect(html).toContain("refuseBuildParam");
      expect(html).not.toContain("?build=");
    }
  });

  it("derives the tag from the bundle's own bytes, not from a clock", () => {
    const loader = readFileSync(join(PREVIEW, "boot", "fresh.js"), "utf8");

    // The three properties the fix turns on, each asserted where it is written:
    // the bytes are read past any cache, they are hashed, and an unreadable
    // bundle is refused rather than imported untagged.
    expect(loader).toContain('cache: "no-store"');
    expect(loader).toContain('crypto.subtle.digest("SHA-256"');
    expect(loader).toContain("could not be read");

    // And no escape hatch back to the old behaviour.
    expect(loader).not.toMatch(/import\(\s*url\s*\)/);
  });
});

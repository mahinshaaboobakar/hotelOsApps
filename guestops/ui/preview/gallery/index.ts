/**
 * Build the gallery — the composition, and nothing else.
 *
 * Run with `npm run gallery`. It reads the gold file, renders every screen the
 * build has, and writes one self-contained page.
 *
 * **It fails loudly on a missing frame.** A gallery that quietly skipped a
 * screen it could not render would be the same defect the last submission was
 * rejected for: fourteen frames reported as built because nothing had checked
 * that they were.
 */

import { readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

import { frame, goldStyle, heading, PAIRS } from "./frames";
import { page, type Built } from "./page";
import { render, widget } from "./realm";
import { WIDGETS } from "./conformance";

/**
 * Where this package is, not where this file is.
 *
 * `import.meta.url` would be the honest answer in a source tree and is the
 * wrong one here: esbuild bundles this into `preview/gallery.js`, so the
 * module's own location moves two directories and every relative path with it.
 * The working directory is `guestops/ui` because npm runs a script from the
 * package that declares it, which is a contract rather than a coincidence.
 */
const root = process.cwd();

/**
 * What the shell writes into a module's document.
 *
 * Mirrored from `apps/desktop/src/shell/module-host/realm.ts` exactly as
 * `frame.html` mirrors it — the root paints using published names and the body
 * is transparent, so the root's paint shows through rather than offering a
 * second opinion one rule below the one that has it (SHELL-Q33).
 */
const REALM = `
:root { background: var(--color-surface); color: var(--color-ink); }
* { box-sizing: border-box; }
body { margin: 0; font-family: var(--font-sans); background: transparent; color: inherit; }
`;

const source = readFileSync(
  join(root, "../docs/mockups/01-guestops-gold.html"), "utf8");

const tokenCss = readFileSync(join(root, "preview/tokens.css"), "utf8");

const built: Built[] = [];

for (const pair of PAIRS) {
  const drawn = frame(source, pair.number);
  const rendered = await render(pair.query);

  if (rendered.trim() === "") {
    throw new Error(
      `frame ${pair.number} rendered nothing from '${pair.query}' — `
      + "the screen is unreachable, which is a finding rather than a blank pane",
    );
  }

  built.push({
    pair,
    heading: heading(source, pair.number),
    drawn,
    built: rendered,
  });
}

// The five widgets, connected the way the shell connects them. A widget that
// is not properly connected renders a styled, silent, empty card, so an empty
// capture here is refused rather than published — it is indistinguishable from
// a capture of a quiet hotel.
const captures = [];

for (const one of WIDGETS) {
  const html = await widget(one.entry);

  if (html.replace(/<style[^>]*>[\s\S]*?<\/style>/gu, "").trim() === "") {
    throw new Error(
      `widget ${one.entry} rendered nothing — it drew no card, and a blank pane `
      + "in the certificate would say it had",
    );
  }

  captures.push({ entry: one.entry, html });
}

const out = join(root, "gallery.html");
writeFileSync(out, page(built, goldStyle(source), tokenCss, REALM, captures), "utf8");

process.stdout.write(
  `${built.length} pairs, ${captures.length} widget captures → ${out}\n`);

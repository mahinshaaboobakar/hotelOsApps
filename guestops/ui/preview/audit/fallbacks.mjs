// P2 — "A fallback literal is copied from styles.css or it is omitted."
//
// Every `var(--token, literal)` in GuestOps' CSS, against the shell's own
// value for that token in HosPilotOS/apps/desktop/src/styles.css. A fallback
// that disagrees is what a module draws when the realm is not styled — so it
// must be the platform's colour, not a colour somebody remembered.
//
// ```
// node preview/audit/fallbacks.mjs
// ```

import { readdirSync, readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const UI = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const SHELL = resolve(UI, "..", "..", "..", "HosPilotOS", "apps", "desktop", "src", "styles.css");

// The shell's first (default-theme) declaration of each token.
const shell = new Map();
for (const m of readFileSync(SHELL, "utf8").matchAll(/(--color-[\w-]+|--font-[\w-]+)\s*:\s*([^;]+);/gu)) {
  if (!shell.has(m[1])) shell.set(m[1], m[2].trim());
}

const normal = (v) => v.replace(/\s+/gu, "").toLowerCase();

const files = [
  ...readdirSync(join(UI, "chrome", "styles")).map((f) => join(UI, "chrome", "styles", f)),
  join(UI, "widgets", "card.ts"),
];

let used = 0;
const wrong = [];
for (const file of files) {
  const text = readFileSync(file, "utf8");
  for (const m of text.matchAll(/var\((--color-[\w-]+|--font-[\w-]+),\s*([^()]*(?:\([^()]*\))?[^()]*)\)/gu)) {
    used += 1;
    const want = shell.get(m[1]);
    if (want === undefined) {
      wrong.push(`${file.slice(UI.length + 1)}: ${m[1]} is not declared by the shell`);
    } else if (normal(m[2]) !== normal(want)) {
      wrong.push(`${file.slice(UI.length + 1)}: ${m[1]} falls back to "${m[2].trim()}", the shell says "${want}"`);
    }
  }
}

process.stdout.write(`${shell.size} shell tokens read · ${used} fallbacks checked · ${wrong.length} disagree\n`);
for (const w of [...new Set(wrong)]) process.stdout.write(`  ${w}\n`);
process.exit(wrong.length === 0 ? 0 : 1);

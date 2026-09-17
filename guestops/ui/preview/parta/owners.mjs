// Which drawn rule governs each differing node.
//
// ```
// node preview/parta/owners.mjs
// ```
//
// # Why this exists
//
// A ruling names a **role** — *note text is 12px* — and a stylesheet names
// **selectors**. Applying one to the other needs the mapping between them, and
// the obvious shortcut is to search the drawing for the value and change it
// wherever it appears. That is wrong, and measurably so: `11.5px` and `12.5px`
// are used by chips, tabs, timeline columns, banners and hints, **and the build
// uses the same two sizes in most of them.** Only 27 nodes differed. Moving all
// 19 rules that carried those values produced **119 new differences** on nodes
// that had agreed all along — a fix that raises the count was wrong about which
// rule applied.
//
// # Why a real DOM and not a search
//
// The first attempt at this located each node by finding its text in the file
// and walking backwards to the nearest tag. It attributed six date nodes to
// `<button class="tab on">`. **A wrong location looks exactly like a location**,
// and a sweep driven by an unverified owner list edits the wrong rules with full
// confidence — which is how the 119 happened in the first place.
//
// So the document is parsed, the leaves are matched by their own text, and the
// element's real classes are read from the tree. What this still cannot do is
// resolve the cascade: it reports which element a node is, not which rule won.
// That is stated rather than implied — the output is a starting point for a
// person reading the stylesheet, not a list to pipe into an edit.
//
// # It reports what it could not locate
//
// A node whose text matches no leaf is printed as unlocated with its text. A
// mapping that silently covered 22 of 27 would send somebody to change five
// rules and leave five nodes differing, with nothing saying which five.

import { readFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import { Window } from "happy-dom";

import { read } from "./read.mjs";

const UI = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const DRAWING = join(UI, "..", "docs", "mockups", "01-guestops-gold.html");
const OUT = join(UI, ".parta");

/** The classes the two ruled questions are about, by how the sweep reports them. */
const CLASSES = {
  "note-type-scale": (p) =>
    p["font-size"] !== undefined
    && ["11.5px", "12.5px"].includes(p["font-size"][0])
    && p["font-size"][1] === "12px",

  "quiet-text-token": (p) =>
    p["color"] !== undefined
    && p["color"][0] === "rgb(90, 97, 114)"
    && p["color"][1] === "rgb(139, 147, 167)",
};

/** `.a.b` for an element, or its tag where it carries no class. */
function names(node) {
  if (node === null || node === undefined) return "—";

  const className = String(node.className ?? "").trim();
  return className === ""
    ? node.tagName.toLowerCase()
    : `.${className.split(/\s+/u).join(".")}`;
}

const window = new Window();
window.document.write(readFileSync(DRAWING, "utf8"));

/**
 * The leaves of one frame, because a text match is not unique in the document.
 *
 * **Searching the whole page attributed six nodes to `.tab.on > .n`**, the count
 * badge on the Today tab — which carries `31 Aug`, exactly like the timeline
 * rows in frame 4. `find` takes the first match, and the tab comes first. That
 * is the same collision the sweep itself had to fix with tag + text + document
 * order, arriving one layer in: **a text match is a candidate, not an identity.**
 *
 * Each comparison names its frame, and the frames carry ids, so the search is
 * scoped to the subtree the node was actually measured in.
 *
 * **AND THAT IS NOT ENOUGH, WHICH COST A WRONG CORRECTION TO THE OWNER.** The
 * collision this fixed was across frames; the one that remains is *inside* one.
 * Frame 4 carries `<span class="n">31 Aug</span>` on the Today tab and `31 Aug`
 * on several timeline rows, so `find` still takes the badge. Six date nodes were
 * reported as tab badges on that basis, and the report was wrong: changing
 * `.act .tm b` alone closed all ten.
 *
 * **So this reports candidates, not identities.** A repeated string within a
 * frame is exactly where it is wrong while looking right, and the only thing
 * that settled it was changing one rule and counting. Treat the output as where
 * to look, and let the count decide.
 */
function leavesOf(frame) {
  const root = window.document.querySelector(`#f${frame}`);
  if (root === null) return [];

  return [...root.querySelectorAll("*")].filter((node) => node.childElementCount === 0);
}

const owners = new Map();
const unlocated = [];

for (let frame = 1; frame <= 17; frame++) {
  const leaves = leavesOf(frame);

  for (const node of read(join(OUT, `cmp-f${frame}.json`)).differing) {
    const of = Object.entries(CLASSES).find(([, is]) => is(node.properties));
    if (of === undefined) continue;

    const text = node.text.trim();
    const hit = leaves.find((leaf) => (leaf.textContent ?? "").trim() === text);

    if (hit === undefined) {
      unlocated.push(`${of[0]}  f${frame}  ${JSON.stringify(text.slice(0, 50))}`);
      continue;
    }

    const key = `${of[0].padEnd(17)} ${names(hit.parentElement)} > ${names(hit)}`;
    owners.set(key, (owners.get(key) ?? 0) + 1);
  }
}

const located = [...owners.values()].reduce((sum, n) => sum + n, 0);

process.stdout.write(`${located + unlocated.length} nodes in the two ruled classes\n\n`);

for (const [key, count] of [...owners].sort((a, b) => b[1] - a[1])) {
  process.stdout.write(`${String(count).padStart(4)}  ${key}\n`);
}

// **Stated, never omitted.** The columns have to add up here too, or the map
// covers a population nobody chose.
process.stdout.write(`\n${located} located + ${unlocated.length} not located\n`);

for (const one of unlocated) process.stdout.write(`      ${one}\n`);

process.stdout.write(
  "\nThese are ELEMENTS, not winning rules — the cascade is not resolved here.\n"
  + "Read the stylesheet for each before changing anything.\n");

// Classify every difference the shared sweep reported for Room Care, and FAIL on
// any it cannot name.
//
// The machinery below the rules is a COPY of guestops/ui/preview/parta/classify.mjs
// at 943f85e (FF) — the complete-run refusal, the single-key check, the closing
// columns and "an unclassified difference is a build error". Adopted, not
// rewritten, for ADR 0154's reason (see serve.mjs). The RULES and NAMED tables
// are Room Care's: each names a class, says which side moves, and cites what
// decides it; where nothing decides it the class is `adjudicate` and the
// difference stays in the report rather than being explained away.
//
// usage:
//   node preview/parta/classify.mjs .parta

import { readdirSync } from "node:fs";
import { join } from "node:path";

import { FRAMES } from "./frames.mjs";
import { read } from "./read.mjs";

/** A property's pair, or undefined — `p.x` is `[drawn, built]`. */
const was = (p, name, drawn, built) => p[name] !== undefined && p[name][0] === drawn && (built === undefined || p[name][1] === built);
const only = (p, allowed) => Object.keys(p).every((name) => allowed.includes(name));

const SURFACE = "rgb(11, 13, 20)";
const BRAND_8 = "color(srgb 0.505882 0.54902 0.972549 / 0.08)";

const RULES = [
  {
    name: "setup-page-first-set",
    moves: "adjudicate",
    why: "The owner chose the screens page's SECOND set of its twice-declared classes (19f203c5, 2026-09-19: "
       + "'we can go with second'), and the build carries it in its one sheet (86c504f). 02-the-roomcare-setup.html "
       + "declares .mono .tag .pill once, at the FIRST values, so its seven tab frames now differ by that set's "
       + "size and spacing: .mono 12 -> 11, .tag padding 2 -> 1, .pill 11/2px -> 10/1px. 01a drew the screens page "
       + "only, so the choice did not address this page; whether 02 is redrawn at the second set or Setup keeps "
       + "the first is the owner's. (Until 86c504f these were the other way round, on the screens frames, as "
       + "page-redeclares-a-class — 24 there, all closed.)",
    hit: (p, frame) => /^7[a-g]$/u.test(frame) && (
      (was(p, "font-size", "12px", "11px") && only(p, ["font-size", "line-height", "display"]))
      || (was(p, "padding-top", "2px", "1px") && only(p, ["padding-top", "padding-bottom", "line-height"]))
      || (was(p, "font-size", "11px", "10px") && was(p, "padding-top", "2px", "1px")
        && only(p, ["font-size", "line-height", "letter-spacing", "padding-top", "padding-bottom"]))),
  },
  {
    name: "sticky-header-ground",
    moves: "neither",
    why: "The wall's header row sticks while the house scrolls beneath it (page 64 §6, the whole-house view the "
       + "owner ruled), so it carries the surface as its ground; a static frame has nothing under its header. "
       + "The colour is the page's own surface — the same pixels.",
    hit: (p) => only(p, ["background-color"]) && was(p, "background-color", "rgba(0, 0, 0, 0)", SURFACE),
  },
  {
    name: "font-shorthand-line-height",
    moves: "drawing",
    why: "The frame's `font:` shorthand (.mono, .tile) resets line-height to `normal`; the build sets family "
       + "and size and inherits the line-height — page 64 §2's fourth reset exists because of exactly this "
       + "shorthand. Where display also moves, the span sits in a flex row and CSS blockifies it.",
    hit: (p) => p["line-height"]?.[0] === "normal" && only(p, ["line-height", "display"]),
  },
  {
    name: "flex-blockified",
    moves: "neither",
    why: "A span placed in a flex row computes display:block (CSS blockification), or a count's words sit in a "
       + "span inside the block the frame drew; the words and their place are the same.",
    hit: (p) => only(p, ["display"]),
  },
  {
    name: "button-type-size",
    moves: "drawing",
    why: "Page 64 §2 fixes .btn at 13px. The frame's .btn sets font-size:13px and then `font:inherit`, which "
       + "resets it to the body's 14px.",
    hit: (p) => was(p, "font-size", "14px", "13px"),
  },
  {
    name: "selected-row-example",
    moves: "neither",
    why: "The frame draws one row selected (tr.sel) to show the state; nothing is selected when the screen "
       + "opens.",
    hit: (p) => was(p, "background-color", BRAND_8, "rgba(0, 0, 0, 0)") && only(p, ["background-color", "line-height"]),
  },
];

const NAMED = new Map([
  ["1b|off the day", ["adjudicate", "The build dims a blocked row with opacity .3 on ink; the frame sets each cell "
    + "faint (td.dimtd). Close to the eye, not the same value, and the standard names neither."]],
  ["1b|out of order", ["adjudicate", "As `off the day` — the blocked row, dimmed two ways."]],
  ["1b|!", ["drawing", "The build draws the disagreement mark as a warn tag: page 64's four tones make warn "
    + "`needs a decision`, which a disagreement is. The frame draws it faint, and its .tag is the redeclared one."]],
  ["3b|Note", ["drawing", "Page 64 §10: a field's label is 11px, uppercase, .07em — the build's. The frame's "
    + "label takes .08em."]],
  ["4d|V 13:00", ["neither", "Data, not styling: the drawing's G03 is dirty (dark text on red), the recorded "
    + "Coral Cove morning's is clean, and the corner text follows the fill."]],
  ["7e|lobby", ["adjudicate", "Two named causes on one node: the frame draws this row selected "
    + "(selected-row-example), and its .mono is the setup page's first-set 12px where the build carries the "
    + "owner's second, 11px (setup-page-first-set)."]],
  ["6|Suite", ["adjudicate", "The frame draws this row selected and its cell at 12px; the build draws the table at "
    + "the drawing's own table size, 13px (its line 46). Not settled by the standard."]],
]);


const dir = process.argv[2];
if (dir === undefined) {
  process.stderr.write("usage: node preview/parta/classify.mjs <dir-of-cmp-*.txt>\n");
  process.exit(2);
}

// **Read from `--compare --json`, never from the readable report.** The prose
// parse this replaced produced two wrong figures before a right one: `0
// unpaired` from searching for a section the instrument does not print, and a
// `PAIRED (n, m by position)` header a sibling parser read as zero. The report
// stays readable; it is no longer a source.
const nodes = [];
const totals = {
  drawnNodes: 0, builtNodes: 0, paired: 0, identical: 0, differing: 0,
  collapsed: 0, refusedDrawn: 0, refusedBuilt: 0, unpairedDrawn: 0, unpairedBuilt: 0,
};

let key = null;
const open = [];

// **A complete run, or no verdict at all.**
//
// This iterated whatever `cmp-*.json` files it found, so a sweep that stopped
// at frame seven produced "2 differing nodes · every difference is named" and
// exit 0 — a partial run whose columns close, which is the one shape this audit
// cannot afford to read as a result. The expected set is derived from
// `frames.mjs`, the same list the sweep drives from, so the two cannot disagree
// about what complete means.
const found = readdirSync(dir).filter((f) => /^cmp-.+\.json$/u.test(f));
const missing = FRAMES
  .map((frame) => `cmp-${frame.id}.json`)
  .filter((name) => !found.includes(name));

if (missing.length > 0) {
  process.stderr.write(
    `${found.length} of ${FRAMES.length} frames compared — ${missing.length} missing: `
    + `${missing.join(", ")}\n`
    + "A count over a partial run measures how far the sweep got, not the drawings.\n");
  process.exit(2);
}

for (const file of found) {
  const report = read(join(dir, file));

  // Every run in a certificate carries one key, or the runs are not comparable
  // and the number is two measurements wearing one word.
  key ??= report.key;

  if (report.key.description !== key.description) {
    process.stderr.write(
      `${file}: measured under '${report.key.description}' where the rest of this `
      + `run used '${key.description}'. Counts across two keys do not add up.\n`);
    process.exit(2);
  }

  // The instrument's own arithmetic, not a sum computed here.
  if (!report.closes.drawn || !report.closes.built) open.push(report.label);

  for (const name of Object.keys(totals)) totals[name] += report.counts[name];

  for (const one of report.differing) {
    // **The frame's number, not its whole label.** `NAMED` is keyed on the
    // number an earlier round used — `2|holds no room` — and this run labels
    // frames `2 · Bookings`, so every hand-named difference fell through as
    // unclassified and read as two new build errors. Taking the leading token
    // keeps those namings and their reasons attached to the nodes they were
    // written for, instead of re-adjudicating a question already answered.
    const frame = report.label.replace(/^frame /u, "").split(" · ")[0];

    nodes.push({ frame, text: one.text, props: one.properties });
  }
}

if (open.length > 0) {
  process.stderr.write(
    `columns do not close on: ${open.join(", ")}. Every node must land in paired, `
    + "refused or an unpaired list before a count means anything.\n");
  process.exit(2);
}

const counts = new Map();
const unnamed = [];

for (const node of nodes) {
  const rule = RULES.find((r) => r.hit(node.props, node.frame));
  const one = NAMED.get(`${node.frame}|${node.text}`);

  if (rule !== undefined) counts.set(rule.name, (counts.get(rule.name) ?? 0) + 1);
  else if (one !== undefined) counts.set(`named: ${one[0]}`, (counts.get(`named: ${one[0]}`) ?? 0) + 1);
  else unnamed.push(node);
}

process.stdout.write(`${nodes.length} differing nodes\n\n`);
for (const rule of RULES) {
  const n = counts.get(rule.name) ?? 0;
  if (n > 0) process.stdout.write(`  ${String(n).padStart(3)}  ${rule.name.padEnd(20)} ${rule.moves}\n`);
}

for (const [key, n] of [...counts].filter(([k]) => k.startsWith("named:"))) {
  process.stdout.write(`  ${String(n).padStart(3)}  ${"individually named".padEnd(24)} ${key.slice(7)}
`);
}

if (unnamed.length > 0) {
  process.stdout.write(`\nUNCLASSIFIED (${unnamed.length}) — this is a build error:\n`);
  for (const node of unnamed) {
    const shape = Object.entries(node.props)
      .map(([k, [d, b]]) => `${k}: ${d} -> ${b}`).join("; ");
    process.stdout.write(`  frame ${node.frame}  ${node.text}\n      ${shape}\n`);
  }
  process.exit(1);
}

process.stdout.write("\nevery difference is named\n");

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
    hit: (p) => was(p, "background-color", BRAND_8, "rgba(0, 0, 0, 0)") && only(p, ["background-color"]),
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
  ["7e|lobby", ["drawing", "Two named causes on one node, each moving its own side. The frame draws this row "
    + "selected (selected-row-example: neither moves). Its .mono is a `font:` shorthand that resets line-height "
    + "to normal (font-shorthand-line-height: the drawing moves). Until 02 was redrawn at the second set "
    + "(3f1cfbd, the owner's 'match', c69fde42), its size differed too."]],
  ["6|Suite", ["adjudicate", "The frame draws this row selected and its cell at 12px; the build draws the table at "
    + "the drawing's own table size, 13px (its line 46). Not settled by the standard."]],
  // The pairs the re-lock (65762c95) created: tiles and chips are <button> in frame and build, so they pair for
  // the first time. Each is named on its own, with what it differs in and which side moves (2026-09-19).
  ["1a|Attention", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["1a|G09", ["neither", "Data, not styling: the frame's G09 is dirty (a red fill); the recorded Coral Cove G09 is pending policy (dotted, no fill). Its line-height is the frame's font shorthand (the drawing moves). A tile pairs for the first time, since the re-lock"]],
  ["1a|G10", ["neither", "Data, not styling: the frame draws G10 in progress (an ink border); the recorded G10 is not. Its line-height is the frame's font shorthand (the drawing moves). A tile pairs for the first time, since the re-lock"]],
  ["1a|Map", ["build", "PROPOSED: the build moves. A chip, a <button> on both sides since the re-lock (65762c95). The owner approved the second set's .chip, margin 0 6px 6px 0 (19f203c5, 'we can go with second'). The build spaces a chip row with the row's 6px gap and drops the 6px below it, and the difference is visible. Measured below the chip row (preview/audit/chipgap.mjs): board and wall 12px drawn, 10 built; the view switcher 27, 24; Setup's chips 18, 10-12. Display blockified in a flex row (neither)."]],
  ["1a|Sold tonight", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["1a|Wall", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["1a|Zone", ["build", "PROPOSED: the build moves. A chip, a <button> on both sides since the re-lock (65762c95). The owner approved the second set's .chip, margin 0 6px 6px 0 (19f203c5, 'we can go with second'). The build spaces a chip row with the row's 6px gap and drops the 6px below it, and the difference is visible. Measured below the chip row (preview/audit/chipgap.mjs): board and wall 12px drawn, 10 built; the view switcher 27, 24; Setup's chips 18, 10-12. Display blockified in a flex row (neither)."]],
  ["1b|Attention", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["1b|Collapse all", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["1b|Map", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["1b|Sold tonight", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["1b|Wall", ["build", "PROPOSED: the build moves. A chip, a <button> on both sides since the re-lock (65762c95). The owner approved the second set's .chip, margin 0 6px 6px 0 (19f203c5, 'we can go with second'). The build spaces a chip row with the row's 6px gap and drops the 6px below it, and the difference is visible. Measured below the chip row (preview/audit/chipgap.mjs): board and wall 12px drawn, 10 built; the view switcher 27, 24; Setup's chips 18, 10-12. Display blockified in a flex row (neither)."]],
  ["1b|Zone", ["build", "PROPOSED: the build moves. A chip, a <button> on both sides since the re-lock (65762c95). The owner approved the second set's .chip, margin 0 6px 6px 0 (19f203c5, 'we can go with second'). The build spaces a chip row with the row's 6px gap and drops the 6px below it, and the difference is visible. Measured below the chip row (preview/audit/chipgap.mjs): board and wall 12px drawn, 10 built; the view switcher 27, 24; Setup's chips 18, 10-12. Display blockified in a flex row (neither)."]],
  ["4|Guest arrived", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["4|Guest departed", ["build", "PROPOSED: the build moves. A chip, a <button> on both sides since the re-lock (65762c95). The owner approved the second set's .chip, margin 0 6px 6px 0 (19f203c5, 'we can go with second'). The build spaces a chip row with the row's 6px gap and drops the 6px below it, and the difference is visible. Measured below the chip row (preview/audit/chipgap.mjs): board and wall 12px drawn, 10 built; the view switcher 27, 24; Setup's chips 18, 10-12. Display blockified in a flex row (neither). Also: the frame draws it chosen (brand border, ink) where nothing is chosen on arrival (neither)."]],
  ["4|Occupied", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["4|Vacant", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["4c|All", ["build", "PROPOSED: the build moves. A chip, a <button> on both sides since the re-lock (65762c95). The owner approved the second set's .chip, margin 0 6px 6px 0 (19f203c5, 'we can go with second'). The build spaces a chip row with the row's 6px gap and drops the 6px below it, and the difference is visible. Measured below the chip row (preview/audit/chipgap.mjs): board and wall 12px drawn, 10 built; the view switcher 27, 24; Setup's chips 18, 10-12. Display blockified in a flex row (neither)."]],
  ["4c|Compact", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["4c|Dirty", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["4c|Occupied", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["4c|Sheet", ["build", "PROPOSED: the build moves. A chip, a <button> on both sides since the re-lock (65762c95). The owner approved the second set's .chip, margin 0 6px 6px 0 (19f203c5, 'we can go with second'). The build spaces a chip row with the row's 6px gap and drops the 6px below it, and the difference is visible. Measured below the chip row (preview/audit/chipgap.mjs): board and wall 12px drawn, 10 built; the view switcher 27, 24; Setup's chips 18, 10-12. Display blockified in a flex row (neither)."]],
  ["4c|Sold tonight", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["4c|Tap grid", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["4c|Vacant", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["4c|Zone", ["build", "PROPOSED: the build moves. A chip, a <button> on both sides since the re-lock (65762c95). The owner approved the second set's .chip, margin 0 6px 6px 0 (19f203c5, 'we can go with second'). The build spaces a chip row with the row's 6px gap and drops the 6px below it, and the difference is visible. Measured below the chip row (preview/audit/chipgap.mjs): board and wall 12px drawn, 10 built; the view switcher 27, 24; Setup's chips 18, 10-12. Display blockified in a flex row (neither)."]],
  ["4d|Compact", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["4d|Sheet", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["4d|Tap grid", ["build", "PROPOSED: the build moves. A chip, a <button> on both sides since the re-lock (65762c95). The owner approved the second set's .chip, margin 0 6px 6px 0 (19f203c5, 'we can go with second'). The build spaces a chip row with the row's 6px gap and drops the 6px below it, and the difference is visible. Measured below the chip row (preview/audit/chipgap.mjs): board and wall 12px drawn, 10 built; the view switcher 27, 24; Setup's chips 18, 10-12. Display blockified in a flex row (neither)."]],
  ["4e|Compact", ["build", "PROPOSED: the build moves. A chip, a <button> on both sides since the re-lock (65762c95). The owner approved the second set's .chip, margin 0 6px 6px 0 (19f203c5, 'we can go with second'). The build spaces a chip row with the row's 6px gap and drops the 6px below it, and the difference is visible. Measured below the chip row (preview/audit/chipgap.mjs): board and wall 12px drawn, 10 built; the view switcher 27, 24; Setup's chips 18, 10-12. Display blockified in a flex row (neither)."]],
  ["4e|Sheet", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["4e|Tap grid", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["7b|Standard", ["build", "PROPOSED: the build moves. A chip, a <button> on both sides since the re-lock (65762c95). The owner approved the second set's .chip, margin 0 6px 6px 0 (19f203c5, 'we can go with second'). The build spaces a chip row with the row's 6px gap and drops the 6px below it, and the difference is visible. Measured below the chip row (preview/audit/chipgap.mjs): board and wall 12px drawn, 10 built; the view switcher 27, 24; Setup's chips 18, 10-12. Display blockified in a flex row (neither)."]],
  ["7b|Suite", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  ["7e|All", ["build", "PROPOSED: the build moves. A chip, a <button> on both sides since the re-lock (65762c95). The owner approved the second set's .chip, margin 0 6px 6px 0 (19f203c5, 'we can go with second'). The build spaces a chip row with the row's 6px gap and drops the 6px below it, and the difference is visible. Measured below the chip row (preview/audit/chipgap.mjs): board and wall 12px drawn, 10 built; the view switcher 27, 24; Setup's chips 18, 10-12. Display blockified in a flex row (neither)."]],
  ["7e|Without a routine", ["drawing", "A chip, a <button> on both sides since the re-lock (65762c95), so it pairs for the first time. Display: the frame's inline-block computes block in the build's flex row (blockified; neither). Spacing: drawn as the chip's own 6px bottom margin, built as the row's 6px gap; nothing rules which (adjudicate); border: --line in the frame, line-strong in the build, and page 64 \u00a72 gives every .btn line-strong (the drawing moves)"]],
  // Paired since 01c's re-lock (fff2d96b): every control in a frame is a <button>.
  ["4|Record", ["neither", "A key collision, since the re-lock made both buttons: the frame's Record is the enter-a-fact sheet's primary action, and the build's is the room page's Record tab. Two different controls that share a word."]],
  ["4c|Discard", ["drawing", "Page 64 \u00a72 fixes .btn at 13px. The frame's Discard takes its toolbar's 12px through font:inherit (button-type-size, from the other side)."]],
  ["4d|Discard", ["drawing", "As 4c's Discard: 12px from the toolbar, where \u00a72 fixes .btn at 13px."]],
  ["4e|Discard", ["drawing", "As 4c's Discard: 12px from the toolbar, where \u00a72 fixes .btn at 13px."]],
  ["7e|\u2039", ["drawing", "Paired since the re-lock (01c). Border: --line in the frame, and \u00a72 gives every .btn line-strong (the drawing moves). Colour: state, not styling. The recorded Coral Cove areas fit one page, so the build disables both arrows (faint), while the frame draws page 1 of 2 (neither)."]],
  ["7e|\u203a", ["drawing", "As 7e's \u2039: the border is \u00a72's line-strong (the drawing moves). The colour is the recorded single page disabling the arrow (neither)."]],
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

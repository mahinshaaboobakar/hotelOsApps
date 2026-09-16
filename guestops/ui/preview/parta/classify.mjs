// Classify every difference the shared sweep reported, and FAIL on any it
// cannot name.
//
// Its reason for existing, which is DD's rule turned on the artifact that reads
// the audit: an unclassified difference is a build error, not a silent pass. A
// residue nobody has to name is a residue that grows, and the pressure it
// creates is to restructure the drawing until the number falls — which is a
// fidelity audit grading itself.
//
// Every rule below names a class, says which side moves, and cites the section
// or ruling that decides it. A rule that decides nothing is not a rule: where
// the standard is silent, the class is `adjudicate` and the difference stays in
// the report rather than being explained away.
//
// usage:
//   node preview/parta/classify.mjs <dir-of-cmp-*.txt>

import { readdirSync } from "node:fs";
import { join } from "node:path";

import { read } from "./read.mjs";

const INK = "color(srgb 0.909804 0.921569 0.956863";
const BAD = "color(srgb 0.972549 0.443137 0.443137";

/** `--color-ink-faint` and `--color-ink-muted`, as the sweep reports them. */
const FAINT = "rgb(90, 97, 114)";
const MUTED = "rgb(139, 147, 167)";

const RULES = [
  {
    name: "note-type-scale",
    moves: "adjudicate",
    why: "The drawing carries 11.5px and 12.5px for one role; the build carries 12px. "
       + "Neither figure is in 64 §5, so this is the build having made a design decision "
       + "the standard never authorised. APPS-Q35, with GG's §5 finding.",
    hit: (p) => p["font-size"]
      && ["11.5px", "12.5px"].includes(p["font-size"][0])
      && p["font-size"][1] === "12px",
  },
  {
    name: "tint-base",
    moves: "drawing",
    why: "The drawing tints with a literal white from the mock palette; the build tints "
       + "with --color-ink. Same alpha, different white. 64 §1: the mock palettes disagree "
       + "with the shell, and §8 requires a drawing to declare no palette of its own.",
    hit: (p) => p["background-color"]
      && p["background-color"][0].startsWith("rgba(255, 255, 255")
      && p["background-color"][1].startsWith(INK),
  },
  {
    name: "danger-edge-alpha",
    moves: "drawing",
    why: "Same colour, different alpha: the drawing's destructive edge is 35%, the build's "
       + "45%. 64 §2 states the value — color-mix(--color-bad 45%, transparent).",
    hit: (p) => p["border-top-color"]
      && p["border-top-color"][0].startsWith("rgba(248, 113, 113")
      && p["border-top-color"][1].startsWith(BAD),
  },
  {
    name: "confirm-weight",
    moves: "drawing",
    why: "The drawing draws the confirm at 700; 64 §2 says 600.",
    hit: (p) => p["font-weight"] && p["font-weight"][0] === "700" && p["font-weight"][1] === "600",
  },
  {
    name: "header-tracking",
    moves: "drawing",
    why: "0.77px against 0.88px on an 11px header is .07em against .08em. 64 §4 states .08em.",
    hit: (p) => p["letter-spacing"]
      && p["letter-spacing"][0] === "0.77px" && p["letter-spacing"][1] === "0.88px",
  },
  {
    name: "quiet-text-token",
    moves: "adjudicate",
    why: "The drawing uses --color-ink-faint where the build uses --color-ink-muted. Both are "
       + "published, so §1 does not decide it and neither side is off-standard. It is a "
       + "reading of which quiet the text is, and it belongs to the owner.",
    hit: (p) => p["color"] && p["color"][0] === FAINT && p["color"][1] === MUTED,
  },
  {
    name: "frame-heading-collision",
    moves: "neither",
    why: "The drawing carries each frame's H1 title, deliberately — the audit needs to quote "
       + "it. The sweep pairs by text, so a build control whose label equals a frame title "
       + "pairs against that heading: `Walk-in` the button against `Walk-in` the title. Weight "
       + "900 is the discriminator; no control in this module is 900. ARCH-Q12, the "
       + "instrument's convention 1 on input it did not anticipate.",
    hit: (p) => p["font-weight"] && p["font-weight"][0] === "900",
  },
  {
    name: "bar-count-collision",
    moves: "neither",
    why: "The drawing's bar counts are 11px in --color-warn; the build's bar now draws an "
       + "em dash, because nothing establishes a count at bar-render time. So a drawn count "
       + "has no counterpart and pairs against whatever else carries those digits — a stat, "
       + "a room number. A consequence of a declared divergence, not a second one.",
    hit: (p) => p["color"] && p["color"][0] === "rgb(251, 191, 36)",
  },
  {
    name: "link-token",
    moves: "adjudicate",
    why: "The drawing draws an inline link in --color-ink; the build draws it in "
       + "--color-brand. Both are published, so §1 does not decide it — whether an inline "
       + "action is tinted is a design reading and belongs to the owner.",
    hit: (p) => Object.keys(p).length === 1
      && p["color"] && p["color"][1] === "rgb(129, 140, 248)",
  },
  {
    name: "note-layout",
    moves: "drawing",
    why: "The drawing stacks a note's parts; the build lays them out with a gap. Structural, "
       + "and the drawing predates the note carrying a mark beside its text.",
    hit: (p, names) => names.every((n) =>
      ["display", "gap", "padding-top", "padding-bottom", "margin-top", "margin-bottom"].includes(n)),
  },
];

/**
 * The ones that are not a class.
 *
 * Six differences that share nothing with each other, so bucketing them would
 * be inventing a category to make a number fall. Each is named where it is,
 * with which side moves and why — which is what "classified" has to mean, or
 * the rule reduces to sorting the residue into a bin called `other`.
 */
const NAMED = new Map([
  ["10|Deluxe Twin",
   ["drawing", "The drawing gives an availability row a 1px rule, a 2% tint and 9px of "
    + "padding; the build draws it bare at 6px. 64 §4: a list sits bare, no wrapper and no "
    + "fill, and vertical padding shrinks only for a reason you can name — the second line "
    + "these rows carry."]],

  ["11|created here",
   ["adjudicate", "The drawing sets this provenance line italic in --color-ink-faint; the "
    + "build sets it upright in --color-ink beside a mark. Italic is not in §5's density "
    + "table or anywhere else in 64, so the standard does not decide it."]],

  ["15|Save and check in",
   ["declared", "The build draws this disabled — 400 weight, --color-ink-faint, dashed edge, "
    + "which is §2's `.btn.off` — where the drawing draws it available. **First classified as "
    + "the build being wrong, and that was wrong**: the screen states the reason where it "
    + "makes the choice. Nothing on the card captures a signature, a scan or a typed "
    + "correction, so a live Save would write the fixture back. The write path exists and has "
    + "no input to carry — `registration.capture`, which is one of the six approved-and-"
    + "uncalled permissions on the Part B ledger. A declared divergence tied to a known gap, "
    + "not a defect, and it closes when the card can capture."]],

  ["15|shown because UAE is not this property's home country",
   ["drawing", "The drawing tints this explanation with --color-brand; the build draws it "
    + "--color-ink-faint. An explanation is not an action, and §2 reserves the accent for "
    + "controls."]],

  ["16|＋ Close a room type for dates",
   ["adjudicate", "The build gives this a `.btn.sm.pri` treatment — a brand-tinted fill and "
    + "edge — where the drawing draws a plain inline action. §2 defines both; which one an "
    + "additive action in a settings panel takes is a reading."]],

  ["6|Ask for service",
   ["neither", "**A residual collision under tag + text, and one for Z.** These words appear "
    + "twice on BOTH sides — as a control and as a row label. The key distinguishes a "
    + "`<button>` from a `<div>`, but this drawing renders its controls as `<div class=\"btn\">`, "
    + "so the drawing's CONTROL pairs against the build's LABEL: two divs, same words. Tag + "
    + "text narrows the collision class without closing it wherever a mock draws a control as "
    + "a div, which is most mocks. Not a build divergence — the build has both nodes and both "
    + "are right."]],

  ["2|holds no room",
   ["drawing", "The build draws this as a mark: a dashed edge, 3px of padding and a gap for "
    + "the glyph beside it. The drawing draws bare text. The mark vocabulary postdates the "
    + "frame."]],
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

for (const file of readdirSync(dir).filter((f) => /^cmp-.+\.json$/u.test(f))) {
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
  const names = Object.keys(node.props);
  const rule = RULES.find((r) => r.hit(node.props, names));
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

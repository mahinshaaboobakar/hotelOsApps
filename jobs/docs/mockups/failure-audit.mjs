/**
 * The page-64b audit — the failure surface, frame beside capture, by role.
 *
 * ```
 * node jobs/docs/mockups/failure-audit.mjs
 * ```
 *
 * # What it compares, and what it deliberately does not
 *
 * **Only the properties the frame's own rules declare.** `64b` is a design page
 * with its own body type (15px / 1.65), and a property its rule does not set is
 * inherited from that page rather than drawn — comparing it would score the
 * build against the design page's typography instead of against the drawing.
 * Every row below names a frame rule and exactly the declarations in it.
 *
 * **The real screens, failing.** The build side is the capture harness driven
 * with `?fail=<kind>`: the Board and The Board widget, every call refused with
 * the kind `causeOf` maps to each state. What the owner saw on 2026-09-18 was a
 * surface *placed* by a screen, and a page that drew the surface on its own
 * would have measured the half that was right.
 *
 * **Frame order.** 64b draws the screen size as Workforce · forbidden,
 * GuestOps · unanswered, Jobs · faulted, and the widget size in the same order.
 * The words differ where the frame drew another application or where the
 * seam's sentences differ from the drawing's; those are listed as text rows and
 * are not layout failures, because this module does not own either.
 *
 * The instrument is the shared `review-measure.mjs`, probe and crop modes. Both
 * servers are this process's, on port 0, and each proves its identity by title
 * before anything is measured (the port rules of 2026-09-10).
 */

import { execFile } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { pathToFileURL } from "node:url";
import { promisify } from "node:util";

const { serve } = await import(pathToFileURL(
  "C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/jobs/ui/preview/parta/serve.mjs",
).href);

// Async for the reason parta-run gives: the servers live in this process, and a
// blocking child would stop them answering the very probe they were started for.
//
// **A failing step names itself.** On 2026-09-19 a run exited 1 having printed
// no result, and five reruns — the same inputs, including the same
// capture-then-audit sequence — all passed. Its cause is not known. Two things
// are: the error was not silent, it went to stderr and the caller's filter
// dropped it; and every probe and crop launches its own headless Edge, which
// `review-measure.mjs` gives ten seconds to appear, on a machine that had 2.1 GB
// free with the owner's desktop running. So a failure now says which state,
// size and step it was, and what the instrument itself said, on stdout AND
// stderr — the next one is diagnosable from whatever a reader kept.
const run = async (args) => {
  try {
    return (await promisify(execFile)("node", args, { encoding: "utf8", maxBuffer: 16 * 1024 * 1024 })).stdout;
  } catch (failure) {
    const step = args.slice(1, 4).join(" ");
    const said = `${failure.stderr ?? ""}${failure.stdout ?? ""}`.trim() || failure.message;
    const line = `failure-audit: STEP FAILED — ${step}\n  exit ${failure.code ?? "?"}: ${said.slice(0, 600)}`;
    console.log(line);
    console.error(line);
    throw failure;
  }
};

const MEASURE = "C:/Users/Mahin Aboobakker/PycharmProjects/HosPilotOS/scripts/review-measure.mjs";
const WORKING = "C:/Users/Mahin Aboobakker/PycharmProjects/HosPilotOS/docs/working";
const HERE = "C:/Users/MAHINA~1/AppData/Local/Temp/claude/C--Users-Mahin-Aboobakker-PycharmProjects-HotelOsAdmin/1c512277-6433-4d9c-9187-04d8f1d21685/scratchpad/audit64b";
const LEDGER = "C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/jobs/docs/mockups/measured-64b.json";

/** The three states, in the frame's order, with the kind that produces each. */
const STATES = [
  { cause: "forbidden", kind: "forbidden", nth: 1 },
  { cause: "unanswered", kind: "unavailable", nth: 2 },
  { cause: "faulted", kind: "internal", nth: 3 },
];

/**
 * Screen size: frame rule → build selector → the declarations that rule makes.
 *
 * `null` for the frame side of a role the state does not draw (no button on a
 * refusal), so absence on both sides is a match and presence on one is not.
 */
const SCREEN = [
  ["the area", ".body", ".gap", ["display", "justify-items", "align-items", "min-height"]],
  ["the block", ".state", ".gap-state", ["width", "padding-top", "padding-bottom", "text-align"]],
  ["the mark", ".st-mark", ".gap-mark", ["margin-bottom", "color"]],
  ["the glyph", ".st-mark svg", ".gap-mark svg", ["width", "height", "display"]],
  ["the label", ".st-label", ".gap-label", ["font-size", "letter-spacing", "text-transform", "color", "margin-bottom"]],
  ["the sentence", ".st-said", ".gap-said", ["font-size", "font-weight", "letter-spacing", "line-height", "margin-bottom"]],
  ["the why", ".st-why", ".gap-why", ["color", "font-size", "max-width", "margin-bottom"]],
  ["what to do", ".st-do", ".gap-do", ["display", "align-items", "column-gap", "margin-bottom", "flex-wrap"]],
  ["the control", ".st-do .btn", ".gap-do .btn", [
    "font-size", "font-weight", "padding-top", "padding-left", "border-top-left-radius",
    "border-top-color", "background-color", "color",
  ]],
  ["the note", ".st-ask", ".gap-ask", ["color", "font-size"]],
  ["the note's capability", ".st-ask b", ".gap-ask b", ["color"]],
  ["the facts", ".prov", ".gap-facts", [
    "border-top-width", "border-top-color", "padding-top", "display", "column-gap", "row-gap", "font-size",
  ]],
  ["a fact's label", ".prov dt", ".gap-facts dt", ["font-size", "letter-spacing", "text-transform", "color"]],
  ["a fact's value", ".prov dd", ".gap-facts dd", ["margin-left", "color", "font-variant-numeric"]],
  ["the asked-for capability", ".prov dd b", ".gap-facts dd b", ["color", "font-weight"]],
];

/** The words, compared as text — reported, not scored as layout. */
const SCREEN_WORDS = [
  ["label", ".st-label", ".gap-label"],
  ["sentence", ".st-said", ".gap-said"],
  ["why", ".st-why", ".gap-why"],
  ["control", ".st-do .btn", ".gap-do .btn"],
  ["note", ".st-ask", ".gap-ask"],
  ["answer", ".prov dd:nth-of-type(2)", ".gap-facts dd:nth-of-type(2)"],
  ["at", ".prov dd:nth-of-type(3)", ".gap-facts dd:nth-of-type(3)"],
];

/** Widget size: `.card .in` and its children. The frame's mark is a bare div. */
const WIDGET = [
  ["the body", ".in", ".wfail", ["display", "flex-direction", "justify-content", "row-gap"]],
  ["the mark", ".in > div:first-child", ".wfail-mark", ["color"]],
  ["the glyph", ".in svg", ".wfail-mark svg", ["width", "height"]],
  ["the sentence", ".w-said", ".wfail-said", ["font-size", "font-weight", "line-height"]],
  ["the why", ".w-why", ".wfail-why", ["font-size", "color", "line-height"]],
  ["the onward line", ".w-open", ".wfail-open", ["font-size", "color", "margin-top"]],
  // Absent on both sides is the frame's approved divergence, measured.
  ["no facts at this size", "dl", "dl", ["display"]],
  ["no state label at this size", ".st-label", ".gap-label", ["display"]],
];

const WIDGET_WORDS = [
  ["sentence", ".w-said", ".wfail-said"],
  ["why", ".w-why", ".wfail-why"],
  ["onward", ".w-open", ".wfail-open"],
];

mkdirSync(HERE, { recursive: true });

async function probe(url, probes, name) {
  const file = join(HERE, `${name}.probes.json`);
  writeFileSync(file, JSON.stringify(probes));
  const out = await run([MEASURE, url, file, "1440", "1400"]);
  return JSON.parse(out.slice(out.indexOf("{")));
}

function probes(rows, words, scope, side) {
  const all = {};
  for (const [role, drawn, built, props] of rows) {
    all[role] = { sel: side === "drawn" ? `${scope} ${drawn}` : `${scope} ${built}`, props };
  }
  for (const [role, drawn, built] of words) {
    all[`text: ${role}`] = { sel: side === "drawn" ? `${scope} ${drawn}` : `${scope} ${built}`, props: [], text: true };
  }
  return all;
}

/** One state at one size: every declared property, and every word. */
function judge(rows, words, drawn, built) {
  const layout = [];
  for (const [role, , , props] of rows) {
    const d = drawn[role];
    const b = built[role];
    if (d?.missing && b?.missing) {
      layout.push({ role, prop: "(absent)", drawn: "absent", built: "absent", same: true });
      continue;
    }
    if (d?.missing || b?.missing) {
      layout.push({ role, prop: "(presence)", drawn: d?.missing ? "absent" : "present", built: b?.missing ? "absent" : "present", same: false });
      continue;
    }
    for (const prop of props) layout.push({ role, prop, drawn: d[prop], built: b[prop], same: d[prop] === b[prop] });
  }

  const text = words.map(([role]) => {
    const d = drawn[`text: ${role}`];
    const b = built[`text: ${role}`];
    const drawnText = d?.missing ? "(absent)" : d?.text;
    const builtText = b?.missing ? "(absent)" : b?.text;
    return { role, drawn: drawnText, built: builtText, same: drawnText === builtText };
  });

  return { layout, text };
}

const ui = await serve({
  root: "C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/jobs/ui",
  path: "/preview/frame.html",
  title: "Jobs module realm",
});
const frame = await serve({
  root: WORKING,
  path: "/64b-when-a-screen-cannot-read.html",
  title: "64b · When A Screen Cannot Read",
});

const FRAME = `${frame.origin}/64b-when-a-screen-cannot-read.html`;
const BUILT = `${ui.origin}/preview/frame.html`;

const ledger = { schema: 1, frame: "docs/working/64b-when-a-screen-cannot-read.html", states: [] };

try {
  for (const state of STATES) {
    const drawnScreen = await probe(FRAME, probes(SCREEN, SCREEN_WORDS, `.win:nth-of-type(${state.nth})`, "drawn"), `drawn-screen-${state.cause}`);
    const builtScreen = await probe(`${BUILT}?fail=${state.kind}`, probes(SCREEN, SCREEN_WORDS, ".jb", "built"), `built-screen-${state.cause}`);
    const drawnWidget = await probe(FRAME, probes(WIDGET, WIDGET_WORDS, `.card:nth-of-type(${state.nth})`, "drawn"), `drawn-widget-${state.cause}`);
    const builtWidget = await probe(`${BUILT}?widget=the-board&fail=${state.kind}`, probes(WIDGET, WIDGET_WORDS, ".wcard", "built"), `built-widget-${state.cause}`);

    // Pictures, for the reader who wants to see what the numbers say.
    await run([MEASURE, "--crop", FRAME, ".win", String(state.nth - 1), join(HERE, `drawn-screen-${state.cause}.png`), "1440", "3600"]);
    await run([MEASURE, "--crop", `${BUILT}?fail=${state.kind}`, ".jb", "0", join(HERE, `built-screen-${state.cause}.png`), "1260", "640"]);
    await run([MEASURE, "--crop", FRAME, ".card", String(state.nth - 1), join(HERE, `drawn-widget-${state.cause}.png`), "1440", "3600"]);
    await run([MEASURE, "--crop", `${BUILT}?widget=the-board&fail=${state.kind}`, ".wcard", "0", join(HERE, `built-widget-${state.cause}.png`), "320", "260"]);

    ledger.states.push({
      cause: state.cause,
      screen: judge(SCREEN, SCREEN_WORDS, drawnScreen, builtScreen),
      widget: judge(WIDGET, WIDGET_WORDS, drawnWidget, builtWidget),
    });
  }
} finally {
  await ui.close();
  await frame.close();
}

writeFileSync(LEDGER, `${JSON.stringify(ledger, null, 2)}\n`);

let off = 0;
let checked = 0;
for (const state of ledger.states) {
  for (const size of ["screen", "widget"]) {
    const { layout, text } = state[size];
    const wrong = layout.filter((row) => !row.same);
    checked += layout.length;
    off += wrong.length;
    console.log(`\n${state.cause} · ${size} — ${layout.length - wrong.length}/${layout.length} declarations as drawn`);
    for (const row of wrong) console.log(`  ✗ ${row.role} · ${row.prop}: drawn ${row.drawn} · built ${row.built}`);
    for (const row of text.filter((t) => !t.same)) console.log(`  ≠ words · ${row.role}\n      drawn  ${row.drawn}\n      built  ${row.built}`);
  }
}

console.log(`\n${checked - off}/${checked} declared properties as drawn · ledger ${LEDGER}`);
readFileSync(LEDGER);
process.exit(off === 0 ? 0 : 1);

/**
 * Jobs against the app surface checklist — the MEASURED and CAPTURED lines.
 *
 * ```
 * node jobs/docs/mockups/surface-audit.mjs            # every case
 * node jobs/docs/mockups/surface-audit.mjs board      # cases whose name contains "board"
 * ```
 *
 * `HotelOsApps/docs/app-surface-checklist.md` (GG, 3d521ce) says a placement
 * line is `M` or `C`, never a test alone — *"a DOM without layout cannot show
 * where a strip sits"*. The source lines are `ui/tests/standard-source.test.ts`;
 * this is the other half: every screen and widget, in every state the checklist
 * names, read from a rendered DOM by the shared `review-measure.mjs` probe
 * (which reports position and scroll extent since HosPilotOS b75f5221), with a
 * capture of each for the lines a person must look at.
 *
 * **Harness, stated.** Every case runs in the capture harness
 * (`ui/preview/frame.html`): this stream's bearer is refused by the real Kernel
 * (a seeded user the graph does not know — CTX, DD), so a loaded screen cannot
 * be reached there, and a failed one only as one of the six causes. The states
 * are the harness's: `?data=empty|barren|single|last` beside the recorded
 * multi-page example, `?fail=<kind>` for each cause, `?locale=none` for NL.
 *
 * The ledger it writes (`measured-surface.json`) holds every reading, so a
 * judgement can be re-checked against the numbers it was made from.
 */

import { execFile } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";
import { pathToFileURL } from "node:url";
import { promisify } from "node:util";

const { serve } = await import(pathToFileURL(
  "C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/jobs/ui/preview/parta/serve.mjs",
).href);

const MEASURE = "C:/Users/Mahin Aboobakker/PycharmProjects/HosPilotOS/scripts/review-measure.mjs";
const HERE = "C:/Users/MAHINA~1/AppData/Local/Temp/claude/C--Users-Mahin-Aboobakker-PycharmProjects-HotelOsAdmin/1c512277-6433-4d9c-9187-04d8f1d21685/scratchpad/surface";
const LEDGER = "C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/jobs/docs/mockups/measured-surface.json";
const W = "1440";
const H = "900";

const run = async (args) => {
  try {
    return (await promisify(execFile)("node", args, { encoding: "utf8", maxBuffer: 16 * 1024 * 1024 })).stdout;
  } catch (failure) {
    const line = `surface-audit: STEP FAILED — ${args.slice(1, 4).join(" ")}\n  ${`${failure.stderr ?? ""}${failure.stdout ?? ""}`.trim().slice(0, 400)}`;
    console.log(line);
    throw failure;
  }
};

/** What every case reads. Missing is a reading, not an error. */
const PROBES = {
  head: { sel: ".head", props: ["height", "padding-left", "border-bottom-width", "border-bottom-color"] },
  tabOn: { sel: ".head .tab.on", props: ["border-bottom-width", "border-bottom-color"] },
  body: { sel: ".main > .body", props: ["padding-top", "overflow-y", "display"] },
  btn: { sel: ".main .btn:not(.pri):not(.pg):not(.chip):not(.sm):not(.danger)", props: [
    "border-top-width", "border-top-color", "border-top-left-radius", "padding-top", "padding-left",
    "font-size", "color", "background-color", "line-height", "font-family",
  ] },
  btnParent: { sel: ".main .row", props: ["line-height"] },
  pri: { sel: ".main .btn.pri:not(.off)", props: ["background-image", "color", "border-top-color"] },
  sm: { sel: ".main .btn.sm", props: ["padding-top", "padding-left", "font-size"] },
  th: { sel: ".main th", props: ["padding-top", "padding-left", "font-size", "font-weight", "text-transform", "letter-spacing", "color", "border-bottom-width"] },
  td: { sel: ".main td", props: ["padding-top", "border-bottom-width", "vertical-align"] },
  lastTd: { sel: ".main tr:last-child td", props: ["border-bottom-width"] },
  sel: { sel: ".main tr.sel td", props: ["background-color", "box-shadow", "border-left-width"] },
  lbl: { sel: ".main label.lbl", props: ["font-size", "letter-spacing", "text-transform", "color"] },
  sect: { sel: ".main .sect", props: ["font-size", "letter-spacing"] },
  note: { sel: ".main .note", props: ["font-size", "line-height", "color"], text: true },
  mono: { sel: ".main .mono", props: ["font-size", "line-height", "color"], text: true },
  hint: { sel: ".main .hint", props: ["font-size", "line-height", "color"], text: true },
  dim: { sel: ".main .dim", props: ["color"], text: true },
  who: { sel: ".head .who", props: ["color"], text: true },
  tbl: { sel: ".main .tbl", props: ["overflow-y", "flex-grow", "min-height"] },
  table: { sel: ".main .tbl > table", props: [] },
  pager: { sel: ".main .pager", props: ["position", "flex-grow"], text: true },
  gap: { sel: ".main .gap", props: ["display", "justify-items", "align-items"] },
  state: { sel: ".main .gap-state", props: ["width"] },
  mark: { sel: ".main .gap-mark svg", props: ["color", "stroke"] },
  label: { sel: ".main .gap-label", props: ["font-size", "letter-spacing", "text-transform", "color"] },
  said: { sel: ".main .gap-said", props: ["font-size", "font-weight"] },
  why: { sel: ".main .gap-why", props: ["font-size", "color"] },
  whyB: { sel: ".main .gap-why b", props: ["font-weight", "color"] },
  facts: { sel: ".main .gap-facts", props: ["border-top-width"] },
  dlg: { sel: ".main .dlg", props: ["position"] },
};

/** Every screen, loaded, and every list state the checklist names. */
const SCREENS = [
  ["board", "open=0"],
  ["board · selected", ""],
  ["board · E0", "open=0&data=empty"],
  ["board · E1", "open=0&data=barren"],
  ["board · 1P", "open=0&data=single"],
  ["board · ML", "open=0&data=last"],
  ["live", "screen=Live"],
  ["scheduled", "screen=Scheduled"],
  ["scheduled · E0", "screen=Scheduled&data=empty"],
  ["catalogue", "screen=Catalogue"],
  ["settings · clock", "screen=Settings"],
  ["settings · policies", "screen=Settings&view=list"],
  ["settings · new policy", "screen=Settings&view=flow"],
  ["settings · presence", "screen=Settings&tab=Shifts%20%26%20presence"],
  ["settings · who is told", "screen=Settings&tab=Who%20is%20told"],
  ["settings · holds", "screen=Settings&tab=Holds%20%26%20reminders"],
  ["settings · closing", "screen=Settings&tab=Closing%20%26%20rating"],
  ["settings · access", "screen=Settings&tab=Access"],
  ["job · overview", "open=job"],
  ["job · work", "open=job&tab=Work"],
  ["raise", "open=raise"],
  ["raise · filled", "open=raise&fill=raise"],
  ["resolve", "open=resolve"],
  ["board · NL", "open=0&locale=none"],
];

/** Every cause, on every screen that reads — F6. */
const KINDS = [
  ["unanswered", "unavailable"], ["forbidden", "forbidden"], ["unadmitted", "local_forbidden"],
  ["ungranted", "user_forbidden"], ["undecidable", "model_unavailable"], ["faulted", "internal"],
];
const FAILING = [
  ["board", "open=0"], ["live", "screen=Live"], ["scheduled", "screen=Scheduled"],
  ["catalogue", "screen=Catalogue"], ["settings", "screen=Settings"],
  ["job", "open=job&only=job"], ["raise", "open=raise&only=catalogue"],
];

const CASES = [
  ...SCREENS.map(([name, q]) => ({ name, q })),
  ...FAILING.flatMap(([screen, q]) => KINDS.map(([cause, kind]) => ({ name: `${screen} · F6 ${cause}`, q: `${q}&fail=${kind}` }))),
];

const filter = process.argv[2];
const chosen = filter === undefined ? CASES : CASES.filter((c) => c.name.includes(filter));

mkdirSync(HERE, { recursive: true });
writeFileSync(join(HERE, "probes.json"), JSON.stringify(PROBES));

const ui = await serve({
  root: "C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/jobs/ui",
  path: "/preview/frame.html",
  title: "Jobs module realm",
});

// A filtered run MERGES into the ledger rather than replacing it: re-measuring
// one case must not erase the other sixty-four readings a judgement rests on.
const readings = filter !== undefined && existsSync(LEDGER) ? JSON.parse(readFileSync(LEDGER, "utf8")).readings : {};
try {
  for (const { name, q } of chosen) {
    const url = `${ui.origin}/preview/frame.html?${q}`;
    const out = await run([MEASURE, url, join(HERE, "probes.json"), W, H]);
    readings[name] = { q, ...JSON.parse(out.slice(out.indexOf("{"))) };
    await run([MEASURE, "--crop", url, ".jb", "0", join(HERE, `${name.replace(/[^a-z0-9]+/gi, "-")}.png`), W, H]);
    process.stdout.write(".");
  }
} finally {
  await ui.close();
}

writeFileSync(LEDGER, `${JSON.stringify({ schema: 1, viewport: `${W}x${H}`, harness: "jobs/ui/preview/frame.html", readings }, null, 2)}\n`);
console.log(`\n${Object.keys(readings).length} cases measured · ledger ${LEDGER} · captures ${HERE}`);

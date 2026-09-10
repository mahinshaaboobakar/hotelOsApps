/**
 * Part A — every approved frame beside a fresh capture, measured by the shared
 * sweep (`scripts/review-measure.mjs`).
 *
 * # Per frame, not per page — landed 2026-09-10
 *
 * This swept the whole thirteen-frame drawing once and compared it against each
 * built screen. Z's `COLLAPSED` line made the cost visible: **collapsed nodes
 * leave the population before anything is compared**, so 4,209 drawn nodes were
 * dropped as cross-frame repeats and every figure was measured over a drawn set
 * that had silently lost them. A number known to be measured wrong does not go
 * into a certificate to avoid moving it.
 *
 * So each frame is now extracted into its own document — the drawing's own
 * `<style>` with one `.win` inside it — and each pair compares ONE frame with
 * ONE screen.
 *
 * # The frame is chosen by overlap, not by my memory of the page
 *
 * Numbering frames by hand is the mapping error waiting to happen: the drawing
 * is edited, an index shifts, and a pair silently compares the wrong screen.
 * Each built screen is matched to the frame whose text it shares most, and the
 * choice is PRINTED with its score, so a wrong pairing is visible in the run
 * rather than buried in it.
 *
 * ```
 * node parta-run.mjs        # frames, sweeps, compares, writes the ledger
 * ```
 */

import { execFileSync } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const HERE = "C:/Users/MAHINA~1/AppData/Local/Temp/claude/C--Users-Mahin-Aboobakker-PycharmProjects-HotelOsAdmin/1c512277-6433-4d9c-9187-04d8f1d21685/scratchpad/parta";
const FRAMES = join(HERE, "frames");
const MOCKUPS = "C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/jobs/docs/mockups";
const MEASURE = "C:/Users/Mahin Aboobakker/PycharmProjects/HosPilotOS/scripts/review-measure.mjs";
const BUILT = "http://127.0.0.1:8853/preview/frame.html";
const DRAWN = "http://127.0.0.1:8855";

mkdirSync(FRAMES, { recursive: true });

/** Every capture, named for the frame it pairs with rather than the drive that made it. */
const CAPTURES = [
  ["frame-01-board", "01", "?screen=Board"],
  ["frame-02-job-overview", "01", "?open=job"],
  ["frame-02b-job-work", "01", "?open=job&tab=Work"],
  ["frame-02c-job-history", "01", "?open=job&tab=History"],
  ["frame-02d-job-notes", "01", "?open=job&tab=Notes"],
  ["frame-02e-job-links", "01", "?open=job&tab=Links"],
  ["frame-02f-job-rating", "01", "?open=job&job=rated&tab=Rating"],
  ["frame-02g-job-record", "01", "?open=job&tab=Record"],
  ["frame-03-raise", "01", "?open=raise&fill=raise"],
  ["frame-04-resolve", "01", "?open=resolve&fill=resolve"],
  ["frame-05-live", "01", "?screen=Live"],
  ["frame-06-scheduled", "01", "?screen=Scheduled"],
  ["frame-07-catalogue", "01", "?screen=Catalogue"],
  ["frame-08-settings-policy", "02", "?screen=Settings"],
  ["frame-08b-policy-list", "02", "?screen=Settings&view=list"],
  ["frame-08c-policy-flow", "02", "?screen=Settings&view=flow"],
  ["frame-09-presence", "02", "?screen=Settings&tab=Shifts %26 presence"],
  ["frame-10-who-is-told", "02", "?screen=Settings&tab=Who is told"],
  ["frame-11-holds", "02", "?screen=Settings&tab=Holds %26 reminders"],
  ["frame-12-closing", "02", "?screen=Settings&tab=Closing %26 rating"],
  ["frame-13-access", "02", "?screen=Settings&tab=Access"],
];

const PAGES = {
  "01": "01-the-jobs-screens.html",
  "02": "02-the-jobs-settings.html",
};

/** Split a drawing into one document per frame, carrying the page's own style. */
function cut(page) {
  const html = readFileSync(join(MOCKUPS, PAGES[page]), "utf8");
  const style = [...html.matchAll(/<style>([\s\S]*?)<\/style>/g)].map((m) => m[1]).join("\n");
  const wins = html.match(/<div class="win"[\s\S]*?(?=<div class="win"|<h2 class="sec"|$)/g) ?? [];

  return wins.map((win, at) => {
    const name = `${page}-${String(at).padStart(2, "0")}.html`;
    writeFileSync(
      join(FRAMES, name),
      `<!doctype html><html><head><meta charset="utf-8"><style>${style}</style></head><body>${win}</body></html>`,
      "utf8",
    );
    return { name, text: new Set(words(win)) };
  });
}

/** The words a frame or a capture contains, for matching one to the other. */
function words(html) {
  return html
    .replace(/<style>[\s\S]*?<\/style>/g, " ")
    .replace(/<[^>]+>/g, " ")
    .replace(/&[a-z]+;/g, " ")
    .split(/\s+/)
    .filter((word) => word.length > 3);
}


/** The pairing key the instrument printed — quoted with every number it produced. */
const BLANK = String.fromCharCode(10, 10);
function keyOf(report) {
  const at = report.indexOf("PAIRING KEY");
  if (at === -1) return "unstated";
  const end = report.indexOf(BLANK, at);
  return report.slice(at + "PAIRING KEY".length, end === -1 ? undefined : end).replace(/\s+/g, " ").trim();
}

const sweep = (url, out, root) =>
  execFileSync("node", [MEASURE, "--sweep", url, out, "1440", "900", ...(root === undefined ? [] : [root])],
    { encoding: "utf8" }).trim();

const frames = { "01": cut("01"), "02": cut("02") };
for (const page of ["01", "02"]) {
  for (const frame of frames[page]) {
    sweep(`${DRAWN}/${frame.name}`, join(HERE, `drawn-${frame.name}.json`));
  }
}

// Every capture is swept first, because the frame each one belongs to is
// decided against ALL of them at once.
const captured = CAPTURES.map(([name, page, query]) => {
  const built = join(HERE, `built-${name}.json`);
  sweep(BUILT + query, built);
  return {
    name, page, built,
    seen: new Set(JSON.parse(readFileSync(built, "utf8")).flatMap((node) => words(node.text))),
  };
});

/**
 * Which frame each capture is of — assigned globally, best pair first.
 *
 * **Not in capture order.** Greedy down the list let an early capture take a
 * frame a later one matched better, and three pairs came out crossed: Steps
 * against the raise frame, Scheduled against Steps'. Taking the strongest
 * remaining pair anywhere in the matrix is order-independent, which is the
 * property that matters — the mapping must not depend on how I happened to
 * write the list.
 *
 * Jaccard, so a frame is penalised for what it holds that the capture does not.
 * Scoring by the capture's share alone let the busiest frame win three times:
 * it contains most of the chrome every screen shows.
 */
const scores = [];
for (const capture of captured) {
  for (const frame of frames[capture.page]) {
    const shared = [...capture.seen].filter((word) => frame.text.has(word)).length;
    scores.push({
      capture, frame,
      score: shared / Math.max(1, capture.seen.size + frame.text.size - shared),
    });
  }
}
scores.sort((a, b) => b.score - a.score);

const chosenFor = new Map();
const runnerUpFor = new Map();
const takenFrames = new Set();
for (const pair of scores) {
  if (chosenFor.has(pair.capture.name) || takenFrames.has(pair.frame.name)) {
    // The best score this capture did NOT get — what a reader needs to judge
    // whether the winner won by a margin or by a hair.
    if (chosenFor.has(pair.capture.name) && !runnerUpFor.has(pair.capture.name)) {
      runnerUpFor.set(pair.capture.name, pair.score);
    }
    continue;
  }
  chosenFor.set(pair.capture.name, pair);
  takenFrames.add(pair.frame.name);
}

const rows = [];
for (const capture of captured) {
  const chosen = chosenFor.get(capture.name);
  const report = execFileSync(
    "node",
    [MEASURE, "--compare", join(HERE, `drawn-${chosen.frame.name}.json`), capture.built, capture.name],
    { encoding: "utf8" },
  );
  writeFileSync(join(HERE, `compare-${capture.name}.txt`), report, "utf8");

  // **Tolerant of the report's own growth.** This read `PAIRED (n)` exactly, and
  // the instrument grew a suffix — `PAIRED (25, 2 by position)` — the day
  // ARCH-Q12 gained its second key. Every row silently read zero. A parser that
  // demands the whole line is a parser that breaks on an improvement.
  const paired = Number(/PAIRED \((\d+)/.exec(report)?.[1] ?? 0);
  const key = keyOf(report);
  const differs = (report.match(/^ {2}DIFFERS/gm) ?? []).length;
  const collapsed = /COLLAPSED\s+drawn (\d+), built (\d+)/.exec(report);
  const readings = JSON.parse(readFileSync(capture.built, "utf8"))
    .reduce((sum, node) => sum + Object.keys(node.style).length, 0);

  rows.push({
    name: capture.name,
    frame: chosen.frame.name,
    match: Number(chosen.score.toFixed(2)),
    runnerUp: Number((runnerUpFor.get(capture.name) ?? 0).toFixed(2)),
    paired, differs, readings,
    collapsedDrawn: Number(collapsed?.[1] ?? 0),
    collapsedBuilt: Number(collapsed?.[2] ?? 0),
    key,
  });

  console.log(
    `  ${capture.name.padEnd(26)} ${chosen.frame.name}  match ${chosen.score.toFixed(2)}` +
    ` (next ${(runnerUpFor.get(capture.name) ?? 0).toFixed(2)})  ${paired} paired, ${differs} differ`,
  );
}

writeFileSync(join(HERE, "ledger.json"), JSON.stringify(rows, null, 2), "utf8");
const sum = (key) => rows.reduce((total, row) => total + row[key], 0);
console.log(
  `\n${rows.length} pairs · ${sum("readings")} readings · ${sum("paired")} paired · ${sum("differs")} differing` +
  `\ncollapsed: drawn ${sum("collapsedDrawn")}, built ${sum("collapsedBuilt")}`,
);

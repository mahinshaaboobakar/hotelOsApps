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

import { execFile } from "node:child_process";
import { promisify } from "node:util";

// **Async, because the servers now live in this process.** `execFileSync` blocks
// the event loop, so an in-process server cannot answer the very sweep it was
// started for — the run hangs with both halves waiting on each other. The
// blocking form was fine while the servers were separate processes; it stopped
// being fine the moment the port rule moved them in here.
const run = async (args) => (await promisify(execFile)("node", args, { encoding: "utf8", maxBuffer: 64 * 1024 * 1024 })).stdout;
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

import { pathToFileURL } from "node:url";

// A Windows path is not an ESM specifier — `import` takes a URL, and a drive
// letter reads as a scheme. `pathToFileURL` is the difference between a driver
// that runs and one that throws ERR_UNSUPPORTED_ESM_URL_SCHEME.
const { serve } = await import(pathToFileURL(
  "C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/jobs/ui/preview/parta/serve.mjs",
).href);

const HERE = "C:/Users/MAHINA~1/AppData/Local/Temp/claude/C--Users-Mahin-Aboobakker-PycharmProjects-HotelOsAdmin/1c512277-6433-4d9c-9187-04d8f1d21685/scratchpad/parta";
const FRAMES = join(HERE, "frames");
const MOCKUPS = "C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/jobs/docs/mockups";
const MEASURE = "C:/Users/Mahin Aboobakker/PycharmProjects/HosPilotOS/scripts/review-measure.mjs";
// **No port is chosen at all.** The servers run IN THIS PROCESS on port 0, so
// the OS assigns something nothing holds and there is nothing for two streams
// to converge on — GG's `serve.mjs`, adopted rather than rewritten. Each one
// proves its own identity by fetching a page and matching that page's own
// title before this driver is allowed to sweep it.


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
      // The title is the frame's own name — the identity a server proves before
      // this driver is allowed to sweep it, and the name a reader sees if one of
      // these documents is ever opened by hand.
      `<!doctype html><html><head><meta charset="utf-8"><title>Jobs frame ${name}</title>`
      + `<style>${style}</style></head><body>${win}</body></html>`,
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


/**
 * The nodes a sweep produced, and the interfaces it read to get them.
 *
 * **The artefact grew a shape today** (`1e895354`): it was a bare array of
 * nodes and is now `{ schema, reads, skipped, overlong, rows }`, because a
 * sweep that reads `value` as well as text has to say so — a run where value
 * was read is not comparable with one where it was not.
 *
 * An unknown shape throws by name rather than reading as zero. That is the same
 * defect this driver already had once, when `PAIRED (25, 2 by position)` was
 * read as nothing: a consumer that quietly copes with a producer it does not
 * understand reports a catastrophe in the thing being measured.
 */
function swept(file) {
  const artefact = JSON.parse(readFileSync(file, "utf8"));

  if (Array.isArray(artefact)) {
    throw new Error(`${file}: the pre-schema artefact — re-sweep on the current instrument`);
  }

  if (artefact.schema !== 1 || !Array.isArray(artefact.rows)) {
    throw new Error(`${file}: unknown sweep artefact (schema ${String(artefact.schema)})`);
  }

  return artefact;
}

const sweep = async (url, out, root) =>
  (await run([MEASURE, "--sweep", url, out, "1440", "900", ...(root === undefined ? [] : [root])])).trim();

const frames = { "01": cut("01"), "02": cut("02") };

const ui = await serve({
  root: "C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/jobs/ui",
  path: "/preview/frame.html",
  title: "Jobs module realm",
});

const drawn = await serve({ root: FRAMES, path: "/01-00.html", title: "Jobs frame 01-00.html" });

const BUILT = `${ui.origin}/preview/frame.html`;
const DRAWN = drawn.origin;
for (const page of ["01", "02"]) {
  for (const frame of frames[page]) {
    await sweep(`${DRAWN}/${frame.name}`, join(HERE, `drawn-${frame.name}.json`));
  }
}

// Every capture is swept first, because the frame each one belongs to is
// decided against ALL of them at once.
const captured = [];
for (const [name, page, query] of CAPTURES) {
  const built = join(HERE, `built-${name}.json`);
  await sweep(BUILT + query, built);
  captured.push({
    name, page, built,
    seen: new Set(swept(built).rows.flatMap((node) => words(node.text))),
  });
}

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
  // **The JSON is the source; the report is for reading.** Retired rather than
  // hardened, per the contract at `review-measure/report.mjs:18-40`: a prose
  // line is improvable by design, and this driver has already read `PAIRED (25,
  // 2 by position)` as zero once. Both forms render the same object, so nothing
  // is lost by quoting the one that cannot be improved out from under a
  // consumer.
  const report = await run(
    [MEASURE, "--compare", join(HERE, `drawn-${chosen.frame.name}.json`), capture.built, capture.name],
  );
  writeFileSync(join(HERE, `compare-${capture.name}.txt`), report, "utf8");

  const result = JSON.parse(await run(
    [MEASURE, "--compare", join(HERE, `drawn-${chosen.frame.name}.json`), capture.built, capture.name, "--json"],
  ));

  // Schema first, then by name — and never enumerate the buckets. `counts` may
  // gain one and `key` may gain a fact (`valueRead` arrived the day after `key`
  // itself); a consumer holding a closed set breaks on an addition that harms
  // nothing.
  if (result.schema !== 1) {
    throw new Error(`${capture.name}: comparison schema ${String(result.schema)} is not one this driver reads`);
  }

  /** A named field that is absent is an ERROR, never a zero — the one thing to implement rather than inherit. */
  const count = (name) => {
    const value = result.counts?.[name];
    if (typeof value !== "number") {
      throw new Error(`${capture.name}: counts.${name} is absent — that is a contract break, not a zero`);
    }
    return value;
  };

  const paired = count("paired");
  const differs = count("differing");
  const collapsedDrawn = count("collapsed");
  const key = `${result.key.description} (${result.key.step}, ${result.key.changedOn})`;
  const artefact = swept(capture.built);
  const readings = artefact.rows.reduce((sum, node) => sum + Object.keys(node.style).length, 0);

  rows.push({
    name: capture.name,
    frame: chosen.frame.name,
    match: Number(chosen.score.toFixed(2)),
    runnerUp: Number((runnerUpFor.get(capture.name) ?? 0).toFixed(2)),
    paired,
    differs,
    readings,
    identical: count("identical"),
    unpairedDrawn: count("unpairedDrawn"),
    unpairedBuilt: count("unpairedBuilt"),
    collapsedDrawn,
    collapsedBuilt: collapsedDrawn,
    valueRead: `drawn ${String(result.key.valueRead.drawn)} · built ${String(result.key.valueRead.built)}`,
    reads: artefact.reads.join(" + "),
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

await ui.close();
await drawn.close();

/**
 * Part A, re-run — every approved frame beside a fresh capture of the
 * conforming build, measured by the shared sweep.
 *
 * The previous certificate (9ece88a) measured a field drawn at radius 8 with
 * 8px padding on `--color-line`. APPS-Q27 moved those five values to §10 and
 * the mockups with them, so that certificate now describes a build that does
 * not exist. This re-runs the whole audit rather than patching its numbers.
 *
 * ```
 * node parta-run.mjs            # sweeps both sides, compares, writes the ledger
 * ```
 *
 * Two servers are already up: 8853 serves `jobs/ui`, 8854 serves the mockups.
 */

import { execFileSync } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const HERE = "C:/Users/MAHINA~1/AppData/Local/Temp/claude/C--Users-Mahin-Aboobakker-PycharmProjects-HotelOsAdmin/1c512277-6433-4d9c-9187-04d8f1d21685/scratchpad/parta";
const MEASURE = "C:/Users/Mahin Aboobakker/PycharmProjects/HosPilotOS/scripts/review-measure.mjs";
const BUILT = "http://127.0.0.1:8853/preview/frame.html";
const DRAWN = "http://127.0.0.1:8854";

mkdirSync(HERE, { recursive: true });

/**
 * Every pair. The capture is named for the frame it pairs with, never for the
 * drive state that produced it (the sweep's convention 2).
 */
const PAIRS = [
  ["frame-01-board", "01-the-jobs-screens.html", "?screen=Board"],
  ["frame-02-job-overview", "01-the-jobs-screens.html", "?open=job"],
  ["frame-02b-job-work", "01-the-jobs-screens.html", "?open=job&tab=Work"],
  ["frame-02c-job-history", "01-the-jobs-screens.html", "?open=job&tab=History"],
  ["frame-02d-job-notes", "01-the-jobs-screens.html", "?open=job&tab=Notes"],
  ["frame-02e-job-links", "01-the-jobs-screens.html", "?open=job&tab=Steps"],
  ["frame-02f-job-rating", "01-the-jobs-screens.html", "?open=job&job=rated&tab=Rating"],
  ["frame-02g-job-record", "01-the-jobs-screens.html", "?open=job&tab=Record"],
  ["frame-03-raise", "01-the-jobs-screens.html", "?open=raise"],
  ["frame-04-resolve", "01-the-jobs-screens.html", "?open=resolve"],
  ["frame-05-live", "01-the-jobs-screens.html", "?screen=Live"],
  ["frame-06-scheduled", "01-the-jobs-screens.html", "?screen=Scheduled"],
  ["frame-07-catalogue", "01-the-jobs-screens.html", "?screen=Catalogue"],
  ["frame-08-settings-policy", "02-the-jobs-settings.html", "?screen=Settings"],
  ["frame-08b-policy-list", "02-the-jobs-settings.html", "?screen=Settings&view=list"],
  ["frame-08c-policy-flow", "02-the-jobs-settings.html", "?screen=Settings&view=flow"],
  ["frame-09-presence", "02-the-jobs-settings.html", "?screen=Settings&tab=Shifts %26 presence"],
  ["frame-10-who-is-told", "02-the-jobs-settings.html", "?screen=Settings&tab=Who is told"],
  ["frame-11-holds", "02-the-jobs-settings.html", "?screen=Settings&tab=Holds %26 reminders"],
  ["frame-12-closing", "02-the-jobs-settings.html", "?screen=Settings&tab=Closing %26 rating"],
  ["frame-13-access", "02-the-jobs-settings.html", "?screen=Settings&tab=Access"],
];

const sweep = (url, out, root) =>
  execFileSync("node", [MEASURE, "--sweep", url, out, "1440", "900", ...(root === undefined ? [] : [root])],
    { encoding: "utf8" }).trim();

// The drawings are swept once each — every frame of a page in one pass, keyed
// by text, which is what the comparison pairs on anyway.
const drawings = new Map();
for (const [, page] of PAIRS) {
  if (drawings.has(page)) continue;
  const out = join(HERE, `drawn-${page.replace(/\W+/g, "-")}.json`);
  console.log(sweep(`${DRAWN}/${page}`, out, ".win"));
  drawings.set(page, out);
}

const rows = [];
for (const [name, page, query] of PAIRS) {
  const built = join(HERE, `built-${name}.json`);
  console.log(sweep(BUILT + query, built));

  const report = execFileSync(
    "node",
    [MEASURE, "--compare", drawings.get(page), built, name],
    { encoding: "utf8" },
  );
  writeFileSync(join(HERE, `compare-${name}.txt`), report, "utf8");

  const paired = /PAIRED \((\d+)\)/.exec(report);
  const differs = (report.match(/^ {2}DIFFERS/gm) ?? []).length;
  const readings = JSON.parse(readFileSync(built, "utf8"))
    .reduce((sum, node) => sum + Object.keys(node.style).length, 0);

  rows.push({ name, paired: Number(paired?.[1] ?? 0), differs, readings });
  console.log(`  ${name}: ${paired?.[1] ?? "0"} paired, ${differs} differ, ${readings} readings`);
}

writeFileSync(join(HERE, "ledger.json"), JSON.stringify(rows, null, 2), "utf8");
const total = rows.reduce((s, r) => s + r.readings, 0);
const differing = rows.reduce((s, r) => s + r.differs, 0);
console.log(`\n${rows.length} pairs · ${total} readings · ${differing} differing nodes`);

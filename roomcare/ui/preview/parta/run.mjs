// Room Care's Part A: sweep each approved frame and its built screen, and compare them.
//
// ```
// node preview/parta/run.mjs 4c
// node preview/parta/run.mjs            # every frame
// ```
//
// Adapted from `guestops/ui/preview/parta/run.mjs` (FF), not written fresh:
// the same shared instrument (`HosPilotOS/scripts/review-measure.mjs`), the same
// identity-verified servers, the same retry, and the same refusal to compare a
// frame the harness never reached. What differs is Room Care's, and only that:
// two drawings rather than one, a width per frame, the harness's own sentence
// for a missed drive, and a provenance stamp taken before anything is swept.
//
// It measures. It does not say which side is right; every difference goes to
// `classify.mjs`, which names it or fails.

import { execFile } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { promisify } from "node:util";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import { FRAMES, PAGES } from "./frames.mjs";
import { line, provenance } from "./provenance.mjs";
import { serve } from "./serve.mjs";

const UI = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const APP = resolve(UI, "..");
const OUT = resolve(UI, ".parta");
const MEASURE = resolve(APP, "..", "..", "HosPilotOS", "scripts", "review-measure.mjs");

/**
 * The harness's own words when a drive step matched nothing (`preview/frame.ts`).
 * A missed drive is photographed rather than thrown, so it would sweep cleanly
 * and compare as fidelity; this is what excludes it.
 */
const UNDRIVEN = "This capture was not driven to its screen";

// `execFile`, never `execFileSync`: `serve.mjs` runs its server in this process,
// and a synchronous child would block the loop the server answers on (FF's
// deadlock, recorded in their run.mjs).
const run = promisify(execFile);

async function sweep(url, out, width, root) {
  // Retried: the browser launch is a race the instrument sometimes loses, and a
  // third failure is raised rather than swallowed (FF's reason, kept).
  for (let attempt = 1; ; attempt++) {
    try {
      await run(process.execPath, [MEASURE, "--sweep", url, out, String(width), "900", root]);
      return;
    } catch (failure) {
      if (attempt === 3) throw failure;
      process.stdout.write(`    retrying ${root} — attempt ${attempt} started no browser\n`);
    }
  }
}

const only = process.argv[2];
const wanted = only === undefined ? FRAMES : FRAMES.filter((f) => f.id === `f${only}`);

if (wanted.length === 0) {
  process.stderr.write(`no frame '${only}' — ids are ${FRAMES.map((f) => f.id.slice(1)).join(", ")}\n`);
  process.exit(2);
}

mkdirSync(OUT, { recursive: true });

// Built by this run, then stamped: which commit, whether the sources were clean
// at it, and a digest per artifact the harness serves. A figure quoted from this
// run names its build.
const stamp = provenance(UI);
writeFileSync(join(OUT, "provenance.json"), `${JSON.stringify(stamp, null, 2)}\n`);
process.stdout.write(`provenance: ${line(stamp)}\n`);

const unreached = [];
const drawings = {};

for (const [page, { path, title }] of Object.entries(PAGES)) {
  drawings[page] = await serve({ root: join(APP, "docs", "mockups"), path, title });
}

const built = await serve({ root: join(UI, "preview"), path: "/frame.html", title: "Room Care module realm — capture" });

process.stdout.write(`drawings ${Object.values(drawings).map((d) => d.origin).join(" ")}\nbuilt    ${built.origin}\n\n`);

try {
  for (const frame of wanted) {
    const width = frame.width ?? 1220;
    const drawn = join(OUT, `${frame.id}-drawn.json`);
    const shot = join(OUT, `${frame.id}-built.json`);

    await sweep(`${drawings[frame.page].origin}${PAGES[frame.page].path}`, drawn, width, `#${frame.id}`);
    await sweep(`${built.origin}/frame.html?${frame.url}`, shot, width, "body");

    if (readFileSync(shot, "utf8").includes(UNDRIVEN)) {
      process.stdout.write(`  UNREACHED ${frame.id.padEnd(4)} ${frame.drawn}\n`);
      unreached.push(frame.drawn);
      continue;
    }

    const { stdout } = await run(
      process.execPath, [MEASURE, "--compare", "--json", drawn, shot, frame.drawn],
      { maxBuffer: 32 * 1024 * 1024 });

    writeFileSync(join(OUT, `cmp-${frame.id}.json`), stdout);
    process.stdout.write(`  swept ${frame.id.padEnd(4)} ${frame.drawn}\n`);
  }
} finally {
  for (const drawing of Object.values(drawings)) await drawing.close();
  await built.close();
}

if (unreached.length > 0) {
  process.stdout.write(
    `\n${unreached.length} frame(s) could not be reached and were not compared:\n`
    + unreached.map((one) => `  ${one}\n`).join("")
    + "Nothing below is a statement about them.\n");
}

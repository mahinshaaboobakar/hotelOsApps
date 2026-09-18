// Part A: sweep a drawing and its screen, and compare them.
//
// ```
// node preview/parta/run.mjs 1
// node preview/parta/run.mjs            # every frame
// ```
//
// # What this does and does not decide
//
// It measures. It does not say which side is right — a fidelity sweep compares
// two renderings and can never tell you which one is correct, and a stream that
// always moves the drawing has stopped checking the build while one that always
// moves the build has stopped checking the design. Every difference goes to
// `classify.mjs`, which names it or fails.
//
// # Both surfaces are served, and both are identified
//
// `serve.mjs` binds port 0 and matches each page's own `<title>` before it
// returns, so nothing here can measure another stream's harness or its own
// wrong root. Two servers rather than one: the drawing is a document in
// `docs/mockups` and the build is a bundle in `ui/preview`, and serving a
// common ancestor would put the whole repository on a socket.
//
// # The frames are addressed by id, which they did not have
//
// The gold page draws seventeen frames as `<h1>` captions followed by
// `<div class="win">`, with nothing to scope a sweep to one of them. They carry
// `id="f1"`…`id="f17"` in document order now — a measurement affordance that
// changes no pixel, and without which this audit cannot be written at all.

import { execFile } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { promisify } from "node:util";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import { serve } from "./serve.mjs";

const UI = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const APP = resolve(UI, "..");
const OUT = resolve(UI, ".parta");
const MEASURE = resolve(
  APP, "..", "..", "HosPilotOS", "scripts", "review-measure.mjs");

import { FRAMES } from "./frames.mjs";


/**
 * Run the shared sweep, without blocking this process.
 *
 * **`execFile`, never `execFileSync`, and the reason is this file's own
 * design.** `serve.mjs` runs its server *in this process* — that is what makes
 * it identity-verifiable and gives `close()` the handle. A synchronous child
 * blocks the event loop, so the server cannot answer the requests the child is
 * waiting on: Edge loads nothing, the sweep waits its full twenty seconds, and
 * the whole thing deadlocks with no output from either side. The instrument was
 * never at fault; running it directly against the same server swept 124 nodes.
 *
 * An in-process server and a synchronous child cannot both be had.
 */
const run = promisify(execFile);

async function sweep(url, out, root) {
  // **Retried, because the browser launch is a race the instrument sometimes
  // loses.** Edge is spawned and then polled at `/json` for a page target; when
  // that poll gives up, `targets` is undefined and the sweep dies on
  // `Cannot read properties of undefined`. It took out frame 12 of a
  // seventeen-frame run and left eleven comparisons that still added up — a
  // partial run whose columns close is exactly what this audit cannot afford to
  // read as a result.
  //
  // Three attempts, and the third failure is raised rather than swallowed: a
  // sweep that quietly gave up would leave a frame uncompared with no line
  // saying so, which is worse than the crash.
  for (let attempt = 1; ; attempt++) {
    try {
      await run(process.execPath, [MEASURE, "--sweep", url, out, "1220", "900", root]);
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
  process.stderr.write(`no frame '${only}' — ids run f1..f${FRAMES.length}\n`);
  process.exit(2);
}

mkdirSync(OUT, { recursive: true });

/** Frames whose screen the harness could not reach — stated, never silent. */
const unreached = [];

const drawing = await serve({
  root: join(APP, "docs", "mockups"),
  path: "/01-guestops-gold.html",
  title: "GuestOps Gold Mockup",
});

// `frame.html`, because that is the page this sweeps — and its title is its
// own. Verifying against `index.html`'s would prove a page is served and say
// nothing about the one being measured.
const built = await serve({
  root: join(UI, "preview"),
  path: "/frame.html",
  title: "GuestOps module realm",
});

process.stdout.write(`drawing ${drawing.origin}\nbuilt   ${built.origin}\n\n`);

try {
  for (const frame of wanted) {
    const drawn = join(OUT, `${frame.id}-drawn.json`);
    const shot = join(OUT, `${frame.id}-built.json`);

    await sweep(`${drawing.origin}/01-guestops-gold.html`, drawn, `#${frame.id}`);
    await sweep(`${built.origin}/frame.html?${frame.url}`, shot, frame.root);

    // **The harness photographs a failed drive rather than failing**, so a
    // frame that never reached its screen sweeps cleanly and compares as
    // fidelity. Its own words are the signal, and reading them is the
    // difference between measuring a screen and measuring an apology.
    const built_json = readFileSync(shot, "utf8");

    if (built_json.includes("drive failed for screen=")) {
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
  await drawing.close();
  await built.close();
}

if (unreached.length > 0) {
  process.stdout.write(
    `\n${unreached.length} frame(s) could not be reached and were not compared:\n`
    + unreached.map((one) => `  ${one}\n`).join("")
    + "Nothing below is a statement about them.\n");
}

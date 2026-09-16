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

/**
 * Which drawn frame answers which built screen, and what to measure of it.
 *
 * **Written down because it cannot be derived.** The drawing numbers frames by
 * the order a reader meets them and the build names screens by what they are;
 * `5b` is a variant of `5` and `11` is `1` in another state.
 *
 * **The first version invented `?screen=stay&tab=Activity`, and the harness
 * reads no `tab`.** It has `activity`, `requests`, `servicing` and `payment` as
 * screens of their own, each driven by clicking the tab — a table that was
 * already there and that I did not read. Five drawings were measured against
 * one rendering and nothing failed: the columns closed on every frame, and what
 * showed it was five identical built totals of 57. **A harness's capability is
 * a population nobody enumerates; asking in your own vocabulary gets you the
 * default, silently.**
 *
 * `root` is what the sweep measures on the built side, and it is not always the
 * page. Frames 10 and 15 draw a **sheet**; the build draws the sheet over the
 * day, which is correct — page 64 §9 makes the overlay a sibling of `.body`,
 * and the scrim is what makes it an overlay at all. Comparing a sheet's drawing
 * against a sheet-plus-page rendering measures the wrong pair and would report
 * the page behind as sixty-odd divergences forever. **The instrument is scoped
 * to the overlay; the drawings are not grown to suit it** — that would change
 * seventeen approved frames for an instrument's convenience, and whether the
 * frames should show the composited surface is the owner's call, drawn.
 */
const FRAMES = [
  { id: "f1", drawn: "1 · Today", url: "screen=today", root: "body" },
  { id: "f2", drawn: "2 · Bookings", url: "screen=bookings", root: "body" },
  { id: "f3", drawn: "3 · Stay · Overview", url: "screen=stay", root: "body" },
  { id: "f4", drawn: "4 · Stay · Activity", url: "screen=activity", root: "body" },
  { id: "f5", drawn: "5 · Stay · Requests", url: "screen=requests", root: "body" },
  { id: "f6", drawn: "5b · Requests, Jobs absent", url: "screen=requests&alone=true", root: "body" },
  { id: "f7", drawn: "6 · Stay · Servicing", url: "screen=servicing", root: "body" },
  { id: "f8", drawn: "7 · Stay · Payment", url: "screen=payment", root: "body" },
  { id: "f9", drawn: "8 · Cancel", url: "screen=cancel", root: ".dlg" },
  { id: "f10", drawn: "9 · The group", url: "screen=booking&group=true", root: "body" },
  { id: "f11", drawn: "10 · Walk-in", url: "screen=walkin", root: ".sheet" },
  { id: "f12", drawn: "11 · Today, PMS-connected", url: "screen=today&connected=true", root: "body" },
  { id: "f13", drawn: "12 · Attention", url: "screen=attention", root: "body" },
  { id: "f14", drawn: "13 · First run", url: "screen=firstrun", root: "body" },
  { id: "f15", drawn: "14 · New booking", url: "screen=newbooking", root: "body" },
  { id: "f16", drawn: "15 · Registration card", url: "screen=registration", root: ".sheet" },
  { id: "f17", drawn: "16 · Setup", url: "screen=setup", root: "body" },
];

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

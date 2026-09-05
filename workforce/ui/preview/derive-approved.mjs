/**
 * Re-derive the approved widget frames from the canvas the owner signed.
 *
 * # Why this is a script and not five copied files
 *
 * `approved.html` sets each built widget beside the frame it was drawn from,
 * and the whole value of that pairing rests on the frame being *the approved
 * artefact* rather than a copy somebody took once. A copy is not a copy for
 * long: the canvas gained a dated amendment on 2026-09-05 and three Jobs frames
 * are already stale inside it, which is exactly what an un-derived copy looks
 * like a month later.
 *
 * So the five files under `approved/` are **generated**, and this is the only
 * thing that writes them. Run it after any amendment to the canvas sources.
 *
 * # What is stripped, and why only this
 *
 * The canvas wraps every card in its own viewer chrome — a `support.js` the
 * harness cannot serve, and the `<x-dc>` / `<helmet>` elements the viewer
 * defines. Those are the canvas's frame, not the design. **Nothing inside the
 * card is touched**, which is what makes the comparison honest: a derivation
 * that "tidied" the drawing would be comparing the build against something
 * nobody approved.
 */

import { readFile, writeFile } from "node:fs/promises";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const here = dirname(fileURLToPath(import.meta.url));

/** The platform repository, beside this one — the canvas lives there. */
const CANVAS = resolve(here, "../../../../HosPilotOS/docs/working/assets/widgets");

/** The five this application ships. `SHELL-Q35`, owner-approved 2026-09-03. */
const WIDGETS = [
  "ShiftBoard",
  "AttendanceToday",
  "PendingRequests",
  "ComingUp",
  "OnLeave",
];

/** The viewer's own chrome, removed so the harness can serve the file. */
const CHROME = [
  /^\s*<script src="\.\/support\.js"><\/script>\s*$/mu,
  /^<x-dc>$/mu,
  /^<helmet>[\s\S]*?^<\/helmet>$/mu,
  /^<\/x-dc>$/mu,
];

for (const widget of WIDGETS) {
  const source = join(CANVAS, `${widget}.dc.html`);
  let html = await readFile(source, "utf8");

  for (const pattern of CHROME) {
    if (!pattern.test(html)) {
      throw new Error(
        `${widget}.dc.html no longer matches ${pattern} — the canvas's chrome `
        + "changed shape, and a strip that silently matched nothing would ship "
        + "the viewer's elements into the harness.");
    }
    html = html.replace(pattern, "");
  }

  // The card itself must survive. Without this the script could "succeed" on a
  // file it had emptied — the failure mode the derivation exists to rule out.
  if (!html.includes("width: 320px; height: 384px")) {
    throw new Error(`${widget}: no 320x384 card survived the strip.`);
  }

  await writeFile(join(here, "approved", `${widget}.html`), html, "utf8");
  console.log(`  ${widget}.html  derived from ${widget}.dc.html`);
}

console.log(`\n${WIDGETS.length} approved frames derived from the canvas sources.`);

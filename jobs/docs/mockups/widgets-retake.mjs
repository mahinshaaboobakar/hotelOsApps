/**
 * Re-take every widget capture, on today's host.
 *
 * Every widget picture this stream holds was taken on 2026-09-05, against a
 * realm that published seventeen tokens. Two more were published since
 * (`color-scroll-thumb`, `color-scroll-thumb-strong`), so those captures show a
 * module rendering against a host no property runs — **a defect in an
 * instrument invalidates its output backwards, and the fix travels forwards
 * only**. Re-taken rather than re-labelled.
 *
 * It also proves a claim the certificate was making on trust: the harness drove
 * `jobs-now` and nothing else until today, so *"built and captured"* for the
 * other five rested on no capture at all.
 */

import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { execFile } from "node:child_process";
import { join } from "node:path";
import { promisify } from "node:util";
import { pathToFileURL } from "node:url";

const run = promisify(execFile);
const HERE = "C:/Users/MAHINA~1/AppData/Local/Temp/claude/C--Users-Mahin-Aboobakker-PycharmProjects-HotelOsAdmin/1c512277-6433-4d9c-9187-04d8f1d21685/scratchpad/widgets";
const MEASURE = "C:/Users/Mahin Aboobakker/PycharmProjects/HosPilotOS/scripts/review-measure.mjs";
const UI = "C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/jobs/ui";

const { serve } = await import(pathToFileURL(`${UI}/preview/parta/serve.mjs`).href);

mkdirSync(HERE, { recursive: true });

const ui = await serve({ root: UI, path: "/preview/frame.html", title: "Jobs module realm" });

/** The six the manifest declares, and jobs-now's three drawn states. */
const WIDGETS = [
  "the-board", "blocked", "by-priority", "due-soon", "raised-today",
  "quiet", "escalated", "mine",
];

const rows = [];

for (const widget of WIDGETS) {
  const out = join(HERE, `${widget}.json`);
  await run("node", [MEASURE, "--sweep", `${ui.origin}/preview/frame.html?widget=${widget}`, out, "460", "620"]);

  const artefact = JSON.parse(readFileSync(out, "utf8"));
  if (artefact.schema !== 1) throw new Error(`${widget}: sweep schema ${String(artefact.schema)}`);

  const missed = artefact.rows.filter((node) => node.text.includes("not driven"));
  const tokens = new Set(
    artefact.rows.flatMap((node) => Object.values(node.style)).filter((v) => typeof v === "string"),
  );

  rows.push({
    widget,
    nodes: artefact.rows.length,
    reads: artefact.reads.join(" + "),
    drivenCleanly: missed.length === 0,
    firstLine: artefact.rows[1]?.text?.slice(0, 40) ?? "",
  });

  console.log(`  ${widget.padEnd(14)} ${String(artefact.rows.length).padStart(3)} nodes · ${missed.length === 0 ? "driven" : "MISS"} · ${rows.at(-1).firstLine}`);
  void tokens;
}

writeFileSync(join(HERE, "retaken.json"), JSON.stringify(rows, null, 1), "utf8");
await ui.close();
console.log(`\n${rows.length} widget captures re-taken on today's host`);

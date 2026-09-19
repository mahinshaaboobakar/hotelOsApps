// The page-64 audit's driver — every Room Care screen and widget, in every
// state `docs/app-surface-checklist.md` (3d521cef) names, in the capture
// harness, with `probe.js` evaluated in each and a screenshot of each.
//
//   node preview/audit/run.mjs            every case
//   node preview/audit/run.mjs deepclean  the cases whose id contains it
//
// A HARNESS RUN, and says so: Room Care is installed on no Kernel yet (0.1.2 is
// staged for the owner's restart), so no state here was reached on a real one.
// The list states are DERIVED from the recording (`preview/frame.ts`, `?list=`).
//
// Built by the run (Part A's `provenance`, FF's): every artifact rebuilt from a
// named commit and stamped by digest, served on port 0 and proved by identity,
// and a case the drive could not reach is excluded by its own sentence rather
// than measured as though it had been.
//
// One browser for the run, driven over the DevTools protocol — the shared
// `review-measure.mjs` sweeps and compares; it has no mode that evaluates a
// probe and photographs the same page, which is what a checklist line needs.

import { spawn } from "node:child_process";
import { mkdirSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import { line, provenance } from "../parta/provenance.mjs";
import { serve } from "../parta/serve.mjs";
import { CASES, EXCLUDED } from "./cases.mjs";

const UI = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const OUT = resolve(UI, ".audit");
const PROBE = readFileSync(join(UI, "preview", "audit", "probe.js"), "utf8");
const UNDRIVEN = "This capture was not driven to its screen";

const only = process.argv[2];
const wanted = only === undefined ? CASES : CASES.filter((c) => c.id.includes(only));
if (wanted.length === 0) {
  process.stderr.write(`no case matches '${only}'\n`);
  process.exit(2);
}

rmSync(join(OUT, "shots"), { recursive: true, force: true });
mkdirSync(join(OUT, "shots"), { recursive: true });
const stamp = provenance(UI);
writeFileSync(join(OUT, "provenance.json"), `${JSON.stringify(stamp, null, 2)}\n`);
process.stdout.write(`provenance: ${line(stamp)}\n`);

const built = await serve({ root: join(UI, "preview"), path: "/frame.html", title: "Room Care module realm — capture" });

const edge = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const port = 9833 + Math.floor(Math.random() * 400);
const profile = join(process.env.TEMP, `roomcare-audit-${port}`);
const browser = spawn(edge, ["--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run",
  `--remote-debugging-port=${port}`, `--user-data-dir=${profile}`, "about:blank"], { stdio: "ignore" });
const sleep = (ms) => new Promise((done) => setTimeout(done, ms));

let targets = [];
for (let i = 0; i < 60 && targets.length === 0; i++) {
  try { targets = (await (await fetch(`http://127.0.0.1:${port}/json`)).json()).filter((t) => t.type === "page"); } catch { /* not up yet */ }
  if (targets.length === 0) await sleep(250);
}
if (targets.length === 0) throw new Error("the audit's browser never answered");

const ws = new WebSocket(targets[0].webSocketDebuggerUrl);
await new Promise((done) => (ws.onopen = done));
let seq = 0;
const pending = new Map();
ws.onmessage = (m) => { const msg = JSON.parse(m.data); if (msg.id && pending.has(msg.id)) { pending.get(msg.id)(msg); pending.delete(msg.id); } };
const send = (method, params = {}) => new Promise((done) => { const n = ++seq; pending.set(n, done); ws.send(JSON.stringify({ id: n, method, params })); });
const evaluate = async (expression) => {
  const got = await send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true });
  if (got.result?.exceptionDetails) throw new Error(got.result.exceptionDetails.exception?.description ?? "probe threw");
  return got.result?.result?.value;
};
await send("Runtime.enable");
await send("Page.enable");

const results = [];
const unreached = [];
try {
  for (const c of wanted) {
    await send("Emulation.setDeviceMetricsOverride", { width: c.width ?? 1180, height: c.height ?? 760, deviceScaleFactor: 1, mobile: false });
    await send("Page.navigate", { url: `${built.origin}/frame.html?${c.url}` });
    for (let i = 0; i < 80; i++) {
      await sleep(100);
      if ((await evaluate("document.documentElement.getAttribute('data-ready')")) === "true") break;
    }
    await sleep(c.write ? 400 : 250);
    const missed = await evaluate("document.querySelector('[data-missed]')?.textContent ?? null");
    if (missed !== null && missed.startsWith(UNDRIVEN)) {
      unreached.push(`${c.id}: ${missed}`);
      process.stdout.write(`  UNREACHED ${c.id}\n`);
      continue;
    }
    // A write case changes what the overlay would send — the first select to another choice — so its
    // action has something to write; an unchanged form writes nothing and would prove nothing.
    if (c.write) {
      await send("Page.navigate", { url: `${built.origin}/frame.html?${c.url.replace("&write=1", "")}` });
      for (let i = 0; i < 80; i++) { await sleep(100); if ((await evaluate("document.documentElement.getAttribute('data-ready')")) === "true") break; }
      await sleep(250);
      c.before = Number(await evaluate("document.documentElement.getAttribute('data-unanswered-calls') ?? '0'"));
      await evaluate(`(() => {
        for (const s of document.querySelectorAll(".sheet select, .dlg select")) {
          const next = [...s.options].find((o) => o.value !== "" && o.value !== s.value);
          if (next) { s.value = next.value; s.dispatchEvent(new Event("change", { bubbles: true })); break; }
        }
        for (const f of document.querySelectorAll(".sheet input:not([type=checkbox]):not([type=radio]), .sheet textarea, .dlg input:not([type=checkbox]):not([type=radio]), .dlg textarea")) {
          if (f.value === "") { f.value = "12"; f.dispatchEvent(new Event("input", { bubbles: true })); }
        }
        document.querySelector(".sheet .df button:last-child, .dlg .df button:last-child")?.click();
      })()`);
      await sleep(400);
      c.writes = Number(await evaluate("document.documentElement.getAttribute('data-unanswered-calls') ?? '0'")) - c.before;
    }
    if (c.fill) await evaluate(`(() => { for (const f of document.querySelectorAll(".sheet input, .sheet textarea, .dlg input, .dlg textarea")) {
      if (f.type === "checkbox" || f.type === "radio") continue; if (f.value === "") { f.value = "12"; f.dispatchEvent(new Event("input", { bubbles: true })); } } })()`);
    const derived = await evaluate("document.documentElement.getAttribute('data-derived')");
    const probed = await evaluate(`(${PROBE})(${JSON.stringify({ state: c.state, cause: c.cause ?? null, at: /&at=(\w+)/.exec(c.url)?.[1] ?? null, write: c.write === true, writes: c.writes ?? 0, widgets: c.widgets === true })})`);
    const shot = await send("Page.captureScreenshot", { format: "png" });
    writeFileSync(join(OUT, "shots", `${c.id}.png`), Buffer.from(shot.result.data, "base64"));
    results.push({ id: c.id, surface: c.surface, state: c.state, cause: c.cause ?? null, url: c.url, derived, ...probed });
    process.stdout.write(`  ${c.id}\n`);
  }
} finally {
  ws.close();
  browser.kill();
  await built.close();
}

writeFileSync(join(OUT, "results.json"), `${JSON.stringify({ provenance: stamp, harness: true, excluded: EXCLUDED, unreached, results }, null, 1)}\n`);
process.stdout.write(`\n${results.length} cases probed, ${unreached.length} unreached${unreached.length ? `:\n  ${unreached.join("\n  ")}` : ""}\n`);
if (unreached.length > 0) process.exit(2);

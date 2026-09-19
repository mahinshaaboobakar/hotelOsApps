// Drive every GuestOps surface into every state the app surface checklist
// names, and keep what each one rendered — a capture and its measured facts.
//
// ```
// node preview/audit/run.mjs            # every cell
// node preview/audit/run.mjs bookings   # the cells whose surface starts so
// ```
//
// Writes `.audit/<surface>--<state>.png` and `.json`; `rules.mjs` judges them.
//
// # HARNESS, stated on every cell
//
// Every cell here is the capture harness, not a Kernel: the owner's installed
// GuestOps is 0.1.0 and stopped, so no real-Kernel state is reachable yet. The
// harness answers from the approved frames' data, re-paged by the backend's own
// arithmetic for the five list states (`preview/lists.ts`), and fails reads by
// the host kinds the SDK maps to the six causes. Each facts file records
// `source: "harness"` so no cell can be quoted as a Kernel measurement.

import { spawn } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import { serve } from "../parta/serve.mjs";
import { PROBE } from "./probe.mjs";

const UI = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const OUT = join(UI, ".audit");

const LISTS = ["E0", "E1", "1P", "MP", "ML"];
const CAUSES = ["unanswered", "forbidden", "unadmitted", "ungranted", "undecidable", "faulted"];

/**
 * Every surface, the method its read fails at, and whether it is a paged list.
 *
 * A stay's Requests and Servicing are read EAGERLY by the stay page, so their
 * failure is the whole stay screen failing — driven as `stay` failing at that
 * method, never as the tab (there is no tab bar left to click).
 */
const SCREENS = [
  { name: "today", screen: "today", at: "today", paged: true },
  { name: "bookings", screen: "bookings", at: "bookings", paged: true },
  { name: "attention", screen: "attention", at: "attention", paged: true },
  { name: "booking", screen: "booking", at: "booking", paged: true },
  // Unpaged since the owner's G7 ruling (2026-09-19): every room type, only
  // the list scrolling.
  { name: "newbooking", screen: "newbooking", at: "availability" },
  { name: "stay", screen: "stay", at: "stay" },
  { name: "stay-requests", screen: "requests", at: "requests", failAs: "stay" },
  { name: "stay-servicing", screen: "servicing", at: "servicing", failAs: "stay" },
  { name: "stay-activity", screen: "activity", at: "activity" },
  { name: "stay-payment", screen: "payment", at: "payment" },
  { name: "setup", screen: "setup", at: "setup" },
  { name: "cancel", screen: "cancel", at: "cancelPlan" },
  { name: "walkin", screen: "walkin" },
  { name: "registration", screen: "registration" },
  { name: "firstrun", screen: "firstrun" },
];

const WIDGETS = ["today", "occupancy", "from-the-pms", "business-mix", "watchlist"];

function cells(origin) {
  const out = [];
  for (const s of SCREENS) {
    out.push({ id: `${s.name}--ALL`, url: `${origin}/preview/frame.html?screen=${s.screen}`, doc: "page" });
    if (s.paged) {
      for (const state of LISTS) {
        out.push({ id: `${s.name}--${state}`, url: `${origin}/preview/frame.html?screen=${s.screen}&list=${state}`, doc: "page" });
      }
    }
    if (s.at) {
      for (const cause of CAUSES) {
        out.push({ id: `${s.name}--F6-${cause}`, url: `${origin}/preview/frame.html?screen=${s.failAs ?? s.screen}&fail=${cause}&at=${s.at}`, doc: "page" });
      }
    }
  }
  for (const w of WIDGETS) {
    out.push({ id: `widget-${w}--W`, url: `${origin}/preview/widget.html?only=${w}`, doc: "realm", w: 420, h: 460 });
    for (const cause of CAUSES) {
      out.push({ id: `widget-${w}--F6-${cause}`, url: `${origin}/preview/widget.html?only=${w}&fail=${cause}`, doc: "realm", w: 420, h: 460 });
    }
  }
  return out;
}

const only = process.argv[2];
mkdirSync(OUT, { recursive: true });

const site = await serve({ root: UI, path: "/preview/frame.html", title: "GuestOps module realm" });

const edge = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const port = 9333 + Math.floor(Math.random() * 500);
const browser = spawn(edge, ["--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run",
  `--remote-debugging-port=${port}`, "--window-size=1220,760",
  `--user-data-dir=${process.env.TEMP}\\edge-guestops-audit-${port}`, "about:blank"], { stdio: "ignore" });

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
let targets;
for (let i = 0; i < 80 && !targets?.length; i++) {
  try { targets = await (await fetch(`http://127.0.0.1:${port}/json`)).json(); } catch { await sleep(250); }
}
if (!targets?.length) throw new Error("Edge started no page to drive");

const ws = new WebSocket(targets.find((t) => t.type === "page").webSocketDebuggerUrl);
await new Promise((r) => { ws.onopen = r; });
let seq = 0;
const pending = new Map();
ws.onmessage = (m) => {
  const msg = JSON.parse(m.data);
  if (msg.id && pending.has(msg.id)) { pending.get(msg.id)(msg); pending.delete(msg.id); }
};
const send = (method, params = {}) => new Promise((r) => { const n = ++seq; pending.set(n, r); ws.send(JSON.stringify({ id: n, method, params })); });
const evaluate = async (expression) => {
  const r = await send("Runtime.evaluate", { expression, returnByValue: true });
  if (r.result?.exceptionDetails) throw new Error(r.result.exceptionDetails.exception?.description ?? "probe threw");
  return r.result?.result?.value;
};
await send("Runtime.enable");
await send("Page.enable");

const wanted = cells(site.origin).filter((c) => only === undefined || c.id.startsWith(only));
let unreached = 0;

try {
  for (const cell of wanted) {
    await send("Emulation.setDeviceMetricsOverride", { width: cell.w ?? 1220, height: cell.h ?? 760, deviceScaleFactor: 1, mobile: false });
    await send("Page.navigate", { url: cell.url });

    let ready = false;
    for (let i = 0; i < 100 && !ready; i++) {
      await sleep(80);
      ready = await evaluate(`document.documentElement.hasAttribute("data-review-ready")`);
    }
    await sleep(cell.doc === "realm" ? 400 : 120);

    const doc = cell.doc === "realm" ? `document.querySelector("iframe").contentDocument` : "document";
    const facts = await evaluate(`(${PROBE})(${doc})`);
    const shot = await send("Page.captureScreenshot", { format: "png" });

    if (!ready || facts?.reached === false) unreached += 1;

    writeFileSync(join(OUT, `${cell.id}.json`), JSON.stringify({ source: "harness", url: cell.url.replace(site.origin, ""), ready, ...facts }, null, 1));
    writeFileSync(join(OUT, `${cell.id}.png`), Buffer.from(shot.result.data, "base64"));
    process.stdout.write(`${ready && facts?.reached !== false ? "ok  " : "MISS"} ${cell.id}\n`);
  }
} finally {
  ws.close();
  browser.kill();
  await site.close();
}

process.stdout.write(`\n${wanted.length} cells driven, ${unreached} not reached — every cell is HARNESS\n`);
process.exit(unreached === 0 ? 0 : 1);

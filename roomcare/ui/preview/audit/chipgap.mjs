// Where the next thing sits below a chip row — drawn and built — for the 12 "adjudicate" chips' question:
// the frame spaces a chip row with each chip's own bottom margin, the build with the row's gap. If the space
// below the row is the same, the difference is invisible; if not, it is a visible choice for the owner.
//
//   node preview/audit/chipgap.mjs
//
// Measured in headless Edge on the rendered pages: the drawing's frames from the file, the build's screens
// from the capture harness. Prints, per surface, the gap from the chips' lowest bottom edge to the top of the
// first element below them, and each chip row's height.

import { spawn } from "node:child_process";
import { join, resolve, dirname } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

import { serve } from "../parta/serve.mjs";

const UI = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const MOCK = resolve(UI, "..", "docs", "mockups");

const PAIRS = [
  ["1a · the map", "01", "f1a", "screen=board"],
  ["1b · the wall", "01", "f1b", "screen=wall"],
  ["4 · a room", "01", "f4", "screen=room"],
  ["4c · the sheet", "01", "f4c", "screen=sheet"],
  ["4d · the grid", "01", "f4d", "screen=grid"],
  ["4e · compact", "01", "f4e", "screen=compact"],
  ["7b · services", "02", "f7b", "screen=setup&tab=Services %26 minutes"],
  ["7e · areas", "02", "f7e", "screen=setup&tab=Areas"],
];

// In the page: every row that holds chips; for each, its chips' lowest bottom edge, the first element that begins
// below them in document order, and the row's own height.
const MEASURE = (scope) => `(() => {
  const root = ${scope};
  if (!root) return null;
  const chips = [...root.querySelectorAll(".chip, .btn.chip")].filter((c) => c.getBoundingClientRect().height > 0);
  const rows = [...new Set(chips.map((c) => c.parentElement))];
  return rows.map((row) => {
    const mine = chips.filter((c) => c.parentElement === row);
    const bottom = Math.max(...mine.map((c) => c.getBoundingClientRect().bottom));
    const all = [...root.querySelectorAll("*")].filter((e) => !row.contains(e) && !e.contains(row) && e.getBoundingClientRect().height > 0);
    const below = all.map((e) => e.getBoundingClientRect().top).filter((t) => t >= bottom - 0.5).sort((a, b) => a - b)[0];
    const r = row.getBoundingClientRect();
    return { chips: mine.map((c) => c.textContent.trim()).join(" · ").slice(0, 60), rowHeight: Math.round(r.height), gapBelow: below === undefined ? null : Math.round(below - bottom) };
  });
})()`;

const edge = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const port = 9833 + Math.floor(Math.random() * 400);
const browser = spawn(edge, ["--headless=new", "--disable-gpu", `--remote-debugging-port=${port}`, `--user-data-dir=${join(process.env.TEMP, `chipgap-${port}`)}`, "about:blank"], { stdio: "ignore" });
const sleep = (ms) => new Promise((done) => setTimeout(done, ms));
let targets = [];
for (let i = 0; i < 60 && targets.length === 0; i++) {
  try { targets = (await (await fetch(`http://127.0.0.1:${port}/json`)).json()).filter((t) => t.type === "page"); } catch { /* not up */ }
  if (targets.length === 0) await sleep(250);
}
const ws = new WebSocket(targets[0].webSocketDebuggerUrl);
await new Promise((done) => (ws.onopen = done));
let seq = 0;
const pending = new Map();
ws.onmessage = (m) => { const msg = JSON.parse(m.data); if (msg.id && pending.has(msg.id)) { pending.get(msg.id)(msg); pending.delete(msg.id); } };
const send = (method, params = {}) => new Promise((done) => { const n = ++seq; pending.set(n, done); ws.send(JSON.stringify({ id: n, method, params })); });
const evaluate = async (expression) => (await send("Runtime.evaluate", { expression, returnByValue: true })).result?.result?.value;
await send("Emulation.setDeviceMetricsOverride", { width: 1400, height: 1000, deviceScaleFactor: 1, mobile: false });

const built = await serve({ root: join(UI, "preview"), path: "/frame.html", title: "Room Care module realm — capture" });
try {
  for (const [label, page, frameId, url] of PAIRS) {
    const file = page === "01" ? "01-the-roomcare-screens.html" : "02-the-roomcare-setup.html";
    await send("Page.navigate", { url: pathToFileURL(join(MOCK, file)).href });
    await sleep(900);
    const drawn = await evaluate(MEASURE(`document.getElementById("${frameId}")`));
    await send("Page.navigate", { url: `${built.origin}/frame.html?${url}` });
    for (let i = 0; i < 60; i++) { await sleep(100); if ((await evaluate("document.documentElement.getAttribute('data-ready')")) === "true") break; }
    await sleep(250);
    const build = await evaluate(MEASURE('document.querySelector(".rc .body")'));
    console.log(`${label}\n  drawn  ${JSON.stringify(drawn)}\n  built  ${JSON.stringify(build)}`);
  }
} finally {
  ws.close();
  browser.kill();
  await built.close();
}

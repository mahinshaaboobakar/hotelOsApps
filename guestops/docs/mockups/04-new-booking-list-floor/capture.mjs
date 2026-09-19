// G7 for the owner: New booking as built beside three options, each the real
// build with the option's layout injected — measured, then photographed.
// usage: node g7shots.mjs <outdir> <state>   (state: 1P | MP)
import { spawn } from "node:child_process";
import { writeFileSync, mkdirSync } from "node:fs";
import { join } from "node:path";
import { pathToFileURL } from "node:url";

const APPS = "C:/Users/Mahin Aboobakker/PycharmProjects/HotelOsApps/guestops/ui";
const { serve } = await import(pathToFileURL(`${APPS}/preview/parta/serve.mjs`).href);
const [out, stateArg] = process.argv.slice(2);
const state = stateArg ?? "1P";
mkdirSync(out, { recursive: true });

const OPTIONS = {
  locked: "",
  beside: `
    .body:has(>.pager){display:grid;grid-template-columns:minmax(0,1fr) 380px;
      grid-template-rows:auto auto minmax(0,1fr) auto;column-gap:18px;row-gap:14px}
    .body > .fltr{grid-column:1 / -1;grid-row:1}
    .body > .tbl{grid-column:1;grid-row:2 / 4}
    .body > .pager{grid-column:1;grid-row:4}
    .body > .note{grid-column:2;grid-row:2}
    .body > .cols{grid-column:2;grid-row:3 / 5;display:block;overflow:auto;min-height:0}`,
  above: `
    .body > .fltr{order:0} .body > .note{order:1} .body > .cols{order:2}
    .body > .tbl{order:3} .body > .pager{order:4}`,
  // The floor is the header plus WHOLE_ROW (7) rows, measured in the page —
  // written in below, because a row's height is the build's, not mine.
  minheight: `
    .body:has(>.pager){overflow-y:auto !important}
    .body:has(>.pager) > .tbl{flex:1 0 var(--floor) !important;min-height:var(--floor) !important;
      overflow-y:auto !important}`,
};
const FLOOR = `(() => { const t = document.querySelector(".body > .tbl");
  const hd = t.querySelector(".tr.hd"); const r = t.querySelector(".tr:not(.hd)");
  const h = hd.getBoundingClientRect().height + 7 * r.getBoundingClientRect().height;
  document.documentElement.style.setProperty("--floor", Math.round(h) + "px"); return Math.round(h); })()`;

const MEASURE = `(() => {
  const list = document.querySelector(".body > .tbl");
  const body = document.querySelector(".body:has(> .pager)");
  const rows = [...list.querySelectorAll(".tr:not(.hd)")];
  const lb = list.getBoundingClientRect();
  const vh = innerHeight;
  const visible = rows.filter((r) => { const b = r.getBoundingClientRect();
    return b.top >= lb.top - 1 && b.bottom <= Math.min(lb.bottom, vh) + 1; }).length;
  const pager = document.querySelector(".pager").getBoundingClientRect();
  const panels = [".note", ".cols"].map((s) => { const e = document.querySelector(".body > " + s);
    const b = e.getBoundingClientRect(); return { s, top: Math.round(b.top), bottom: Math.round(b.bottom), onScreen: b.bottom <= vh + 1 && b.top >= 0 }; });
  return { rows: rows.length, visible, list: Math.round(lb.height), listScrolls: list.scrollHeight > list.clientHeight + 1,
    bodyScrolls: body.scrollHeight > body.clientHeight + 1, pagerBottom: Math.round(pager.bottom), pagerOnScreen: pager.bottom <= vh + 1,
    listWidth: Math.round(lb.width), panels, range: document.querySelector(".pager > span").textContent };
})()`;

const ui = await serve({ root: APPS, path: "/preview/frame.html", title: "GuestOps module realm" });
const edge = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const port = 9333 + Math.floor(Math.random() * 500);
const proc = spawn(edge, ["--headless=new", "--disable-gpu", "--hide-scrollbars", "--no-first-run",
  `--remote-debugging-port=${port}`, "--window-size=1220,760",
  `--user-data-dir=${process.env.TEMP}\\edge-g7-${port}`, "about:blank"], { stdio: "ignore" });
const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
let targets;
for (let i = 0; i < 80 && !targets?.length; i++) { try { targets = await (await fetch(`http://127.0.0.1:${port}/json`)).json(); } catch { await sleep(250); } }
const ws = new WebSocket(targets.find((t) => t.type === "page").webSocketDebuggerUrl);
await new Promise((r) => { ws.onopen = r; });
let id = 0; const pending = new Map();
ws.onmessage = (m) => { const msg = JSON.parse(m.data); if (msg.id && pending.has(msg.id)) { pending.get(msg.id)(msg); pending.delete(msg.id); } };
const send = (method, params = {}) => new Promise((r) => { const n = ++id; pending.set(n, r); ws.send(JSON.stringify({ id: n, method, params })); });
const evaluate = async (e) => (await send("Runtime.evaluate", { expression: e, returnByValue: true })).result?.result?.value;
await send("Runtime.enable"); await send("Page.enable");
await send("Emulation.setDeviceMetricsOverride", { width: 1220, height: 760, deviceScaleFactor: 1, mobile: false });

const results = {};
try {
  for (const [name, css] of Object.entries(OPTIONS)) {
    await send("Page.navigate", { url: `${ui.origin}/preview/frame.html?screen=newbooking&list=${state}` });
    for (let i = 0; i < 100; i++) { await sleep(80); if (await evaluate(`document.documentElement.hasAttribute("data-review-ready")`)) break; }
    if (name === "minheight") console.log("floor px", await evaluate(FLOOR));
    if (css) await evaluate(`(() => { const s = document.createElement("style"); s.textContent = ${JSON.stringify(css)}; document.head.append(s); return true; })()`);
    await sleep(250);
    results[name] = await evaluate(MEASURE);
    const shot = await send("Page.captureScreenshot", { format: "png" });
    writeFileSync(join(out, `04-g7-${state}-${name}.png`), Buffer.from(shot.result.data, "base64"));
  }
} finally { ws.close(); proc.kill(); await ui.close(); }
writeFileSync(join(out, `04-g7-${state}-measured.json`), JSON.stringify(results, null, 1));
for (const [n, r] of Object.entries(results)) console.log(n.padEnd(10), JSON.stringify({ rows: r.rows, visible: r.visible, list: r.list, listScrolls: r.listScrolls, bodyScrolls: r.bodyScrolls, pagerOnScreen: r.pagerOnScreen, listWidth: r.listWidth, panelsOnScreen: r.panels.map((p) => p.s + ":" + p.onScreen).join(" "), range: r.range }));

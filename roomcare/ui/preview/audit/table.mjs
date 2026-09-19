// The page-64 audit's table: every checklist line × every Room Care surface,
// from a run's results.json, the source walk's output and the judged lines.
//
//   node preview/audit/table.mjs <after dir> [<before dir>]   → markdown on stdout
//
// A cell is the worst verdict any state gave it — FAIL over OPEN over PASS over
// N/A — and `·` where no state asked the line of that surface. The line list is
// read from the checklist itself, so a line cannot go missing from the table.

import { readFileSync } from "node:fs";
import { join, resolve } from "node:path";

import { SCREENS } from "./cases.mjs";
import { DEVIATIONS, JUDGED } from "./judged.mjs";

const [afterDir, beforeDir] = process.argv.slice(2);
const checklist = readFileSync(resolve(process.cwd(), "..", "..", "docs", "app-surface-checklist.md"), "utf8");
const LINES = [...checklist.matchAll(/^\| ([A-Z]\d+) \|/gmu)].map((m) => m[1]);
if (LINES.length !== 86) throw new Error(`read ${LINES.length} lines from the checklist, not 86`);

const COLUMNS = [...SCREENS.map((s) => s.surface), "Setup (its own read)", "Widgets", "Source", "Frames"];
const SHORT = {
  "Board · map": "map", "Board · wall": "wall", "A room": "room", "A room, inspected": "insp", Prepare: "prep",
  "Room states · sheet": "sheet", "Room states · grid": "grid", "Room states · compact": "cmpct", Supervision: "sup",
  "Deep clean": "deep", "My rooms": "mine", "At the door": "door", "Setup › Windows & trigger": "win",
  "Setup › Services & minutes": "svc", "Setup › Rules": "rules", "Setup › Assignment & zones": "zones",
  "Setup › Areas": "areas", "Setup › Deep clean plan": "plan", "Setup › Property-wide access": "access",
  "Setup (its own read)": "setup", Widgets: "wdg", Source: "src", Frames: "frm",
};
const RANK = { FAIL: 5, DEV: 4, OPEN: 3, PASS: 2, NA: 1 };
const MARK = { FAIL: "**F**", DEV: "D", OPEN: "O", PASS: "P", NA: "–" };

function load(dir, when = "after") {
  const run = JSON.parse(readFileSync(join(dir, "results.json"), "utf8"));
  const source = readFileSync(join(dir, "source.txt"), "utf8");
  const cells = new Map();
  const put = (line, column, v, why, where) => {
    const key = `${line}|${column}`;
    const cell = cells.get(key) ?? { v: null, notes: [] };
    if (cell.v === null || RANK[v] > RANK[cell.v]) cell.v = v;
    if (v !== "PASS") cell.notes.push(`${v} ${where}: ${why}`);
    cells.set(key, cell);
  };
  for (const r of run.results) {
    const column = r.surface.includes(" › ") && !r.surface.startsWith("Setup ›") ? r.surface.split(" › ")[0]
      : SCREENS.some((s) => r.surface.startsWith(s.surface)) ? SCREENS.find((s) => r.surface.startsWith(s.surface)).surface : r.surface;
    for (const [line, found] of Object.entries(r.lines)) for (const f of found) put(line, column, f.v, f.why, r.id);
    // The OPEN lines that record rather than judge (64d items 3, 5, 6-7).
    if (r.record.X13 !== undefined) put("X13", column, "OPEN", `the widget card measures ${r.record.X13.join("/")}px tall in this state`, r.id);
    if (r.record.X14 !== undefined && r.record.X14.length > 0) put("X14", column, "OPEN", `a failed widget's onward control reads ${r.record.X14.map((t) => `"${t}"`).join(", ")}; Try again re-reads in place, and Open Room Care opens the board (three widgets), Prepare or Supervision — each draws its own read there, not the widget's failed one`, r.id);
    if (r.record.X15 !== undefined) put("X15", column, "OPEN", r.record.X15, r.id);
  }
  for (const m of source.matchAll(/^([A-Z]\d+)\s+(PASS|FAIL|OPEN|NA)\s+(.*)$/gmu)) put(m[1], "Source", m[2], m[3], "source walk");
  for (const j0 of JUDGED) {
    // A judgement records the state it was made against; a "before" run takes the line's state at that commit.
    const j = when === "before" && j0.before !== undefined ? { ...j0, ...j0.before } : j0;
    if (j.v === "SEE") continue;
    const where = j.where === "screens" || j.where === "overlays" ? SCREENS.map((s) => s.surface) : j.where === "widgets" ? ["Widgets"] : j.where === "frames" ? ["Frames"] : ["Source"];
    for (const column of where) put(j.id, column, j.v, j.why, `judged (${j.how})`);
  }
  // An owner-approved deviation (APPS-Q27): the probe still measures the divergence; the cell says D, with its ruling.
  for (const d of when === "before" ? [] : DEVIATIONS) {
    const cell = cells.get(`${d.id}|${d.column}`);
    if (cell !== undefined && cell.v === "FAIL") { cell.v = "DEV"; cell.notes.push(`DEV approved deviation: ${d.why}`); }
  }
  // D1 is G6's measurement (§5 refers to §6).
  for (const column of COLUMNS) { const g6 = cells.get(`G6|${column}`); if (g6) cells.set(`D1|${column}`, g6); }
  return { run, cells };
}

const after = load(afterDir);
const before = beforeDir === undefined ? null : load(beforeDir, "before");

const out = [];
out.push(`| Line | ${COLUMNS.map((c) => SHORT[c]).join(" | ")} |`, `|---|${COLUMNS.map(() => ":-:").join("|")}|`);
for (const line of LINES) out.push(`| ${line} | ${COLUMNS.map((c) => { const cell = after.cells.get(`${line}|${c}`); return cell ? MARK[cell.v] : "·"; }).join(" | ")} |`);

const unasked = LINES.filter((line) => COLUMNS.every((c) => !after.cells.has(`${line}|${c}`)));
out.push("", `Lines asked of no surface: ${unasked.length === 0 ? "none" : unasked.join(", ")}`);

const fails = [...after.cells].filter(([, cell]) => cell.v === "FAIL");
out.push("", `## Every FAIL after — ${fails.length} cell(s)`, "");
for (const [key, cell] of fails) out.push(`- **${key.replace("|", " · ")}** — ${[...new Set(cell.notes.filter((n) => n.startsWith("FAIL")))].slice(0, 4).join("; ")}`);

const devs = [...after.cells].filter(([, cell]) => cell.v === "DEV");
out.push("", `## D — owner-approved deviations, each labelled where it diverges — ${devs.length} cell(s)`, "");
for (const [key, cell] of devs) out.push(`- ${key.replace("|", " · ")} — ${cell.notes.find((n) => n.startsWith("DEV"))?.replace(/^DEV approved deviation: /u, "")}`);

const opens = [...after.cells].filter(([, cell]) => cell.v === "OPEN");
out.push("", `## OPEN — what is built, recorded and not failed — ${opens.length} cell(s)`, "");
for (const [key, cell] of opens) out.push(`- ${key.replace("|", " · ")} — ${[...new Set(cell.notes.filter((n) => n.startsWith("OPEN")).map((n) => n.replace(/^OPEN [^:]+: /u, "")))].slice(0, 2).join("; ")}`);

if (before !== null) {
  const was = [...before.cells].filter(([, cell]) => cell.v === "FAIL");
  out.push("", `## Before → after — the ${was.length} cell(s) that failed at ${before.run.provenance.source.head.slice(0, 8)}`, "");
  for (const [key, cell] of was) {
    const now = after.cells.get(key)?.v ?? "·";
    const first = cell.notes.find((n) => n.startsWith("FAIL")) ?? "";
    out.push(`- ${key.replace("|", " · ")} → **${now}** — was: ${first.slice(0, 170)}`);
  }
}
process.stdout.write(`${out.join("\n")}\n`);

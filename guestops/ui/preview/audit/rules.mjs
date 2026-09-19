// Judge the measured lines of the app surface checklist over `.audit/*.json`.
//
// ```
// node preview/audit/rules.mjs          # prints the table, writes .audit/verdicts.json
// ```
//
// Every verdict carries the facts it rested on. A line with no cell in a state
// it names is reported as UNASKED, never as a pass — the checklist's own rule.
// Every cell is HARNESS (see run.mjs); nothing here is a Kernel measurement.

import { readdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const OUT = join(resolve(dirname(fileURLToPath(import.meta.url)), "..", ".."), ".audit");

const cells = Object.fromEntries(readdirSync(OUT)
  .filter((f) => f.endsWith(".json") && f !== "verdicts.json")
  .map((f) => [f.replace(/\.json$/u, ""), JSON.parse(readFileSync(join(OUT, f), "utf8"))]));

const px = (v) => parseFloat(v);
const near = (a, b, tol = 1) => Math.abs(a - b) <= tol;

// Token values the build is expected to compute (tokens.css — the published set).
const INK = "rgb(232, 235, 244)";
const MUTED = "rgb(139, 147, 167)";
const FAINT = "rgb(90, 97, 114)";
const BRAND = "rgb(129, 140, 248)";
const WARN = "rgb(251, 191, 36)";
const BAD = "rgb(248, 113, 113)";
const TONE = { unanswered: WARN, forbidden: MUTED, unadmitted: MUTED, ungranted: MUTED, undecidable: BAD, faulted: BAD };

const LISTS = ["today", "bookings", "attention", "booking", "newbooking"];

/** §4's table lines do not apply to a stack of cards (Attention, gold frame 12). */
const TABLES = LISTS.filter((s) => s !== "attention");

/** Rows shown: a table's rows, or a stack's cards. */
const shown = (id, f) => (surface(id) === "attention" ? f.stackCards : f.list.rows);
const surface = (id) => id.split("--")[0];
const state = (id) => id.split("--")[1];
const pick = (pred) => Object.entries(cells).filter(([id, f]) => pred(id, f));

/** One line's verdicts: [cell id, "PASS" | "FAIL" | "OPEN" | "N/A", why]. */
const lines = {};
function judge(line, rows) { lines[line] = rows; }

// ---- §3 navigation ---------------------------------------------------------
judge("N1", pick((id, f) => f.head && state(id) === "ALL").map(([id, f]) => {
  const ok = near(f.head.box.height, 56) && f.head["padding-left"] === "22px"
    && f.head["border-bottom-width"] === "1px"
    && f.activeTab?.["border-bottom-width"] === "2px" && f.activeTab?.["border-bottom-color"] === BRAND;
  return [id, ok ? "PASS" : "FAIL", `bar ${f.head.box.height}px, pad ${f.head["padding-left"]}, rule ${f.head["border-bottom-width"]}, active ${f.activeTab?.["border-bottom-width"]} ${f.activeTab?.["border-bottom-color"]}`];
}));

judge("N8", pick((id, f) => f.body && state(id) === "ALL").map(([id, f]) =>
  [id, px(f.body["padding-top"]) > 0 ? "PASS" : "FAIL", `body padding-top ${f.body["padding-top"]}`]));

// ---- §4 the list ------------------------------------------------------------
const listCells = (states) => pick((id, f) => LISTS.includes(surface(id)) && states.includes(state(id)) && f.list);

/** §4 is about lists drawn as tables; Attention is a stack of cards by design. */
const tableCells = (states) => listCells(states).filter(([id]) => TABLES.includes(surface(id)));
const notATable = (line) => listCells(["1P"]).filter(([id]) => !TABLES.includes(surface(id)))
  .map(([id]) => [id, "N/A", `${line}: a stack of cards (gold frame 12), not a table`]);

judge("L1", [...notATable("L1"), ...tableCells(["1P", "MP"]).map(([id, f]) => {
  const boxed = f.between.filter((b) => b.bg !== "rgba(0, 0, 0, 0)" || b.border !== "0px" || b.radius !== "0px");
  const self = f.list["background-color"] !== "rgba(0, 0, 0, 0)" || f.list["border-top-left-radius"] !== "0px";
  return [id, boxed.length === 0 && !self ? "PASS" : "FAIL", boxed.length ? `wrapped by ${boxed.map((b) => b.cls).join(", ")}` : `bare ${f.list.cls}`];
})]);

judge("L2", tableCells(["1P", "MP", "E0"]).filter(([, f]) => f.th).map(([id, f]) => {
  const t = f.th;
  const ok = t["padding-top"] === "8px" && t["padding-left"] === "10px" && t["font-size"] === "11px"
    && t["font-weight"] === "500" && t["text-transform"] === "uppercase" && t["letter-spacing"] === "0.88px"
    && t.color === FAINT && f.thRow?.["border-bottom-width"] === "1px";
  return [id, ok ? "PASS" : "FAIL", `th ${t["padding-top"]}/${t["padding-left"]} ${t["font-size"]} ${t["font-weight"]} ${t["letter-spacing"]} ${t.color}`];
}));

judge("L3", tableCells(["1P", "MP"]).filter(([, f]) => f.td).map(([id, f]) =>
  [id, f.td["padding-top"] === "10px" && f.tdRow?.["border-bottom-width"] === "1px" ? "PASS" : "FAIL",
    `td padding ${f.td["padding-top"]} ${f.td["padding-left"]} (rule: 10px; a shrink needs its reason — L6)`]));

judge("L5", tableCells(["1P", "ML"]).filter(([, f]) => f.lastRow).map(([id, f]) =>
  [id, f.lastRow["border-bottom-width"] === "1px" ? "PASS" : "FAIL", `last row rule ${f.lastRow["border-bottom-width"]}`]));

// ---- §6 the pager -----------------------------------------------------------
// Read from the range's own span (probe `pager.range`), never from the strip's
// whole text — which ran "of 4" into "10 per page" and read a total of 410.
const range = (text) => { const m = /(\d+)–(\d+) of (\d+)/u.exec(text ?? ""); return m ? m.slice(1).map(Number) : null; };

judge("G3", listCells(["1P"]).map(([id, f]) => {
  const r = range(f.pager?.range);
  const n = shown(id, f);
  const arrows = f.pager?.arrows.filter((a) => a.t === "‹" || a.t === "›") ?? [];
  const ok = r && r[0] === 1 && r[1] === n && r[2] === n && arrows.every((a) => a.disabled);
  return [id, ok ? "PASS" : "FAIL", `"${f.pager?.range}" over ${n} rows; arrows ${arrows.map((a) => a.disabled ? "off" : "ON").join(" ")}`];
}));

judge("G4", listCells(["MP", "ML"]).map(([id, f]) => {
  const r = range(f.pager?.range);
  const n = shown(id, f);
  const ok = r && r[1] - r[0] + 1 === n && r[0] <= r[1];
  return [id, ok ? "PASS" : "FAIL", `"${f.pager?.range}" over ${n} rows`];
}));

// The pager's words AND what the list itself says on that page. Today's first
// E1 capture drew "no rows on this page · 50 in the list" under a table saying
// "Nothing in this list today." — two elements on one screen disagreeing, the
// table's sentence false. A pager-only check passed it.
judge("G5", listCells(["E1"]).map(([id, f]) => {
  const pagerSays = /no rows on this page · \d+ in the list/iu.test(f.pager?.range ?? "") && !range(f.pager?.range);
  const listClaimsEmpty = /nothing|no \w+ (?:match|here|in)/iu.test(f.barrenSays ?? "");
  return [id, pagerSays && !listClaimsEmpty ? "PASS" : "FAIL",
    `pager "${f.pager?.range}"; the list says ${f.barrenSays === null ? "nothing" : `"${f.barrenSays}"`}`];
}));

judge("G6", listCells(["MP", "1P", "E0"]).map(([id, f]) => {
  const listScrolls = f.list["overflow-y"] === "auto" || f.list["overflow-y"] === "scroll";
  const ok = !f.pageScrolls && f.body.scrollHeight <= f.body.clientHeight + 1 && listScrolls && f.pager.position !== "sticky";
  return [id, ok ? "PASS" : "FAIL", `page scrolls ${f.pageScrolls}; body ${f.body.scrollHeight}/${f.body.clientHeight}; list overflow ${f.list["overflow-y"]}; pager ${f.pager.position}`];
}));

judge("G7", listCells(["1P", "E0", "E1", "ML"]).map(([id, f]) => {
  const floor = f.viewport.h;
  const atFloor = near(f.pager.box.bottom, f.body.box.bottom, 2) && near(f.body.box.bottom, floor, 2);

  // A stack of cards has no one list box to grow; the floor half is its test.
  if (!TABLES.includes(surface(id))) {
    return [id, atFloor ? "PASS" : "FAIL", `pager bottom ${Math.round(f.pager.box.bottom)} · window floor ${floor} (a stack: the floor half only)`];
  }

  const rows = f.list.rowsHeight + f.list.headerHeight;
  const grown = f.list.box.height > rows + 1;

  // Content AFTER the pager (New booking: frame 14's note and card). CORE-Q28
  // keeps it on screen, so the list takes only what is left — and where that
  // is less than its rows, a short list hides rows behind a scroll while its
  // range claims them all. Reported as the conflict it is, never as a pass.
  if (f.pager.followedBy !== null) {
    return [id, f.list.box.height + 1 >= rows ? "PASS" : "FAIL",
      `content follows the pager (${f.pager.followedBy}); list ${Math.round(f.list.box.height)}px for ${Math.round(rows)}px of rows — `
      + "frame 14's panels below a paged list against CORE-Q28's 'only the list scrolls'"];
  }

  return [id, atFloor && grown ? "PASS" : "FAIL", `pager bottom ${Math.round(f.pager.box.bottom)} · window floor ${floor}; list ${Math.round(f.list.box.height)}px vs rows ${Math.round(rows)}px`];
}));

judge("C11", pick((id, f) => f.btnPriOff && state(id) === "ALL").map(([id, f]) =>
  [id, f.btnPriOff["background-image"] === "none" && f.btnPriOff["border-top-style"] === "dashed" ? "PASS" : "FAIL",
    `off primary: fill ${f.btnPriOff["background-image"]}, edge ${f.btnPriOff["border-top-style"]}, cursor ${f.btnPriOff.cursor}`]));

judge("G8", pick((id, f) => !LISTS.includes(surface(id)) && state(id) === "ALL" && f.body && !f.pager).map(([id, f]) =>
  [id, !f.pageScrolls && !f.body.clipped ? "PASS" : "FAIL",
    f.pageScrolls ? `the DOCUMENT scrolls (body ${f.body.scrollHeight}px in a ${f.viewport.h}px window) — the bar scrolls away` : `body ${f.body.scrollHeight}/${f.body.clientHeight}, clipped ${f.body.clipped}`]));

judge("G9", listCells(["1P", "MP", "E0"]).map(([id, f]) =>
  [id, f.pager.nextOfList ? "PASS" : "FAIL", `pager follows ${f.pager.prev}`]));

judge("G11", listCells(["E0"]).map(([id, f]) =>
  [id, "OPEN", `E0 draws: "${f.pager?.range}" — 64f unapproved; recorded as built`]));

// ---- §2 controls -------------------------------------------------------------
judge("C1", pick((id, f) => f.btn && state(id) === "ALL").map(([id, f]) => {
  const b = f.btn;
  const ok = b["border-top-width"] === "1px" && b["border-top-color"] === "rgba(255, 255, 255, 0.14)"
    && b["border-top-left-radius"] === "8px" && b["padding-top"] === "7px" && b["padding-left"] === "14px"
    && b["font-size"] === "13px" && b.color === INK && b["background-color"] === "rgba(0, 0, 0, 0)";
  return [id, ok ? "PASS" : "FAIL", `${b["border-top-width"]} ${b["border-top-left-radius"]} ${b["padding-top"]}/${b["padding-left"]} ${b["font-size"]}`];
}));

judge("C2", pick((id, f) => f.btnPri && state(id) === "ALL").map(([id, f]) =>
  [id, /^linear-gradient\(135deg, rgb\(129, 140, 248\)/u.test(f.btnPri["background-image"]) ? "PASS" : "FAIL", f.btnPri["background-image"].slice(0, 48)]));

judge("C5", pick((id, f) => f.btnDanger && state(id) === "ALL").map(([id, f]) =>
  [id, f.btnDanger.color === BAD && /0\.45\)$/u.test(f.btnDanger["border-top-color"]) ? "PASS" : "FAIL", `${f.btnDanger.color} · ${f.btnDanger["border-top-color"]}`]));

// Judged where the rows live. The walk-in and registration sheets draw over
// Today, so their cells count Today's rows — the same defect, owned by Today.
// C8, read as Jobs' board reads it (jobs/ui/screens/board/index.ts): a row that
// opens something is reachable through a real button — the row itself, or its
// key text as an .opener — with the reset on the class. A whole-row button
// would nest a row's own link inside it.
judge("C6", pick((id, f) => f.btnConfirm && state(id) === "ALL").map(([id, f]) => {
  const c = f.btnConfirm;
  const ok = c["background-color"] === BAD && c["border-top-color"] === "rgba(0, 0, 0, 0)" && c["font-weight"] === "600";
  return [id, ok ? "PASS" : "FAIL", `confirm fill ${c["background-color"]}, edge ${c["border-top-color"]}, weight ${c["font-weight"]}`];
}));

judge("L4", pick((id, f) => f.selRow).map(([id, f]) => {
  const s = f.selRow;
  const ok = /^color\(srgb 0\.50\d* 0\.54\d* 0\.97\d* \/ 0\.08\)$|0\.08\)$/u.test(s["background-color"]) && s["box-shadow"] === "none" && s["border-left-width"] === "0px";
  return [id, ok ? "PASS" : "FAIL", `selected row ${s["background-color"]}, shadow ${s["box-shadow"]}, left edge ${s["border-left-width"]}`];
}));

judge("C8",pick((id, f) => state(id) === "ALL" && f.rowButtons.length > 0 && !f.sheet).map(([id, f]) => {
  const reached = f.rowButtons.map((tag, i) => tag === "BUTTON" || f.rowOpeners?.[i] !== null);
  const unreached = reached.filter((ok) => !ok).length;
  const reset = (f.rowOpeners ?? []).filter((o) => o !== null)
    .every((o) => o.border === "0px" && o.align === "left" && o.family);
  return [id, unreached === 0 && reset ? "PASS" : "FAIL",
    `${f.rowButtons.length - unreached} of ${f.rowButtons.length} rows reachable by a real button; opener reset ${reset ? "holds" : "does NOT hold"}`];
}));

judge("C9", pick((id, f) => state(id) === "ALL" && f.controls.length > 0).map(([id, f]) => {
  const off = f.controls.filter((c) => c.lh === "normal"
    || !near(px(c.lh) / px(c.fs), px(c.parentLh) / px(c.parentFs), 0.01));
  return [id, off.length === 0 ? "PASS" : "FAIL", off.length ? off.slice(0, 3).map((c) => `${c.cls || "button"} "${c.text}" ${c.lh}@${c.fs} vs ${c.parentLh}@${c.parentFs}`).join("; ") : `${f.controls.length} controls inherit the factor`];
}));

// ---- §5 density ----------------------------------------------------------------
judge("D3", pick((id, f) => f.notes.some((n) => n.cls !== "fn")).map(([id, f]) => {
  const off = f.notes.filter((n) => n.cls !== "fn" && (n["font-size"] !== "12px" || n["line-height"] !== "19.8px"));
  return [id, off.length === 0 ? "PASS" : "FAIL", off.length ? off.map((n) => `${n.cls} ${n["font-size"]}/${n["line-height"]}`).join(", ") : "notes 12px/19.8px"];
}));

// 64a rules ONE role — the Activity date column, .act .tm b — and says of the
// rest: "--color-ink-faint appears 432 times across 159 files, unclassified …
// deliberately not swept off one ruling about ten nodes." So D4 is judged on
// that role, and faint text elsewhere is recorded OPEN, never failed. The first
// cut of this rule failed every faint .hint: the over-reach 64a warns against.
judge("D4", [
  ...pick((id, f) => f.activityDate && state(id) === "ALL").map(([id, f]) =>
    [id, f.activityDate.color === MUTED ? "PASS" : "FAIL", `.act .tm b ${f.activityDate.color}`]),
  ...pick((id, f) => state(id) === "ALL" && f.notes.some((n) => n.cls === "hint" && n.color === FAINT)).map(([id]) =>
    [id, "OPEN", ".hint computes ink-faint — unclassified faint text, open by 64a's own words"]),
]);

// ---- §13 a screen that cannot read ----------------------------------------------
const f6 = (pred) => pick((id, f) => /^F6-/u.test(state(id)) && f.failure && pred(id, f));
const cause = (id) => state(id).replace(/^F6-/u, "");

judge("X2", f6(() => true).map(([id, f]) => {
  const s = f.failure.stage, b = f.failure.state;
  const centred = near(b.top - s.top, s.bottom - b.bottom, 1.5) && near(b.left - s.left, s.right - b.right, 1.5);
  const width = near(b.width, Math.min(560, s.width * 0.92), 1);
  return [id, centred && width ? "PASS" : "FAIL", `gaps v ${Math.round(b.top - s.top)}/${Math.round(s.bottom - b.bottom)} h ${Math.round(b.left - s.left)}/${Math.round(s.right - b.right)}, width ${Math.round(b.width)}`];
}));

judge("X3", f6(() => true).map(([id, f]) => {
  const m = f.failure.mark;
  return [id, m?.tag === "svg" && m.stroke === "currentColor" && m.color === TONE[cause(id)] ? "PASS" : "FAIL", `${m?.tag} stroke ${m?.stroke} colour ${m?.color} (wants ${TONE[cause(id)]})`];
}));

judge("X4", f6(() => true).map(([id, f]) => {
  const l = f.failure.label;
  const ok = l["font-size"] === "11px" && l["letter-spacing"] === "1.1px" && l["text-transform"] === "uppercase" && l.color === FAINT;
  return [id, ok ? "OPEN" : "FAIL", `11px .1em uppercase faint ${ok ? "hold" : "DO NOT"}; family "${l["font-family"].slice(0, 30)}" — the mono stack is OPEN (64d item 4)`];
}));

judge("X5", f6(() => true).map(([id, f]) =>
  [id, f.failure.said["font-size"] === "19px" && f.failure.said["font-weight"] === "600" ? "PASS" : "FAIL", `${f.failure.said["font-size"]} ${f.failure.said["font-weight"]}`]));

judge("X6", f6(() => true).map(([id, f]) =>
  [id, f.failure.why["font-size"] === "14px" && f.failure.why.color === MUTED ? "PASS" : "FAIL", `${f.failure.why["font-size"]} ${f.failure.why.color}`]));

const BUTTONS = { unanswered: ["Try again"], faulted: ["Copy these details"], undecidable: ["Copy these details"], forbidden: [], unadmitted: [], ungranted: [] };
judge("X7", f6(() => true).map(([id, f]) => {
  const want = BUTTONS[cause(id)];
  const ok = JSON.stringify(f.failure.buttons) === JSON.stringify(want);
  return [id, ok ? "PASS" : "FAIL", `buttons ${JSON.stringify(f.failure.buttons)} (fill and needs-line size OPEN, 64d items 1–2)`];
}));

judge("X8", f6(() => true).map(([id, f]) =>
  [id, JSON.stringify(f.failure.facts) === JSON.stringify(["Asked for", "Answer", "At"]) ? "PASS" : "FAIL", `labels ${JSON.stringify(f.failure.facts)} (the moment's form OPEN, 64d item 8)`]));

// ---- widgets ------------------------------------------------------------------------
judge("X12", pick((id, f) => id.startsWith("widget-") && f.card).map(([id, f]) => {
  const ok = f.card.mark?.tag === "svg" && f.card.mark.w === 20 && f.card.mark.color === TONE[cause(id)]
    && JSON.stringify(f.card.children) === JSON.stringify(["wg", "wf", "wfw", "wo"]) && f.card.facts === 0;
  return [id, ok ? "PASS" : "FAIL", `mark ${f.card.mark?.w}px ${f.card.mark?.color}; ${f.card.children.join(" ")}; facts ${f.card.facts}; "${f.card.open}"`];
}));

judge("X13", pick((id, f) => id.startsWith("widget-") && f.card).map(([id, f]) =>
  [id, "OPEN", `card ${f.card.card.width}×${f.card.card.height} — 64d item 3 (200 vs 384)`]));

// ---- §9 overlays ------------------------------------------------------------------
judge("O1", pick((id, f) => f.sheet && state(id) === "ALL").map(([id, f]) => {
  const s = f.sheet;
  const dialog = s.cls.includes("dlg");
  const ok = dialog
    ? near(s.box.width, 520) && near(s.box.left, f.viewport.w - s.box.right, 1)
    : near(s.box.width, 440) && near(s.box.right, f.viewport.w) && near(s.box.height, f.viewport.h);
  return [id, ok ? "PASS" : "FAIL", `${dialog ? "dialog" : "sheet"} ${Math.round(s.box.width)}×${Math.round(s.box.height)} at ${Math.round(s.box.left)}–${Math.round(s.box.right)}`];
}));

judge("O2", pick((id, f) => f.sheet && state(id) === "ALL").map(([id, f]) =>
  [id, f.sheet.scrim === "absolute" && f.sheet.position !== "fixed" ? "PASS" : "FAIL", `scrim ${f.sheet.scrim}, surface ${f.sheet.position}`]));

judge("O3", pick((id, f) => f.sheet && state(id) === "ALL").map(([id, f]) =>
  [id, f.sheet.siblingOfBody ? "PASS" : "FAIL", f.sheet.siblingOfBody ? "the overlay is a sibling of .body" : "inside .body"]));

// ---- print --------------------------------------------------------------------------
writeFileSync(join(OUT, "verdicts.json"), JSON.stringify(lines, null, 1));

let fails = 0;
for (const [line, rows] of Object.entries(lines)) {
  const tally = rows.reduce((t, [, v]) => ({ ...t, [v]: (t[v] ?? 0) + 1 }), {});
  fails += tally.FAIL ?? 0;
  process.stdout.write(`${line.padEnd(4)} ${rows.length ? Object.entries(tally).map(([v, n]) => `${v} ${n}`).join(" · ") : "UNASKED — no cell in its states"}\n`);
  for (const [id, v, why] of rows.filter(([, v]) => v === "FAIL")) {
    process.stdout.write(`       FAIL ${id}: ${why}\n`);
  }
}
process.stdout.write(`\n${Object.keys(lines).length} measured lines judged over ${Object.keys(cells).length} cells — ${fails} failing cells — every cell HARNESS\n`);

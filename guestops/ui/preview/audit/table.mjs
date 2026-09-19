// Write GuestOps' app surface audit table — every checklist line, per surface.
//
// ```
// node preview/audit/rules.mjs && node preview/audit/table.mjs
// ```
//
// Measured lines come from `.audit/verdicts.json` (rules.mjs over the capture
// run); source, test and capture lines are stated below with their evidence.
// A line is never PASS by omission: one no cell reached says so.

import { readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const UI = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
const measured = JSON.parse(readFileSync(join(UI, ".audit", "verdicts.json"), "utf8"));

/** Lines checked by source (S), test (T) or capture (C) — verdict and evidence. */
const STATED = {
  P1: ["PASS", "T", "tests/tokens.test.ts asserts every name against the SDK's TOKEN_NAMES"],
  P2: ["PASS", "S", "preview/audit/fallbacks.mjs: 176 fallbacks, 0 disagree with styles.css (3 before the fix: two in files of mine)"],
  P3: ["PASS", "S", "washes use --color-*-soft; color-mix edges (45%, 35%, 5%) are not the 12% soft tones"],
  P4: ["PASS", "S", "every hex/rgb in chrome/styles and widgets/card.ts is a var() fallback or a comment"],
  P5: ["PASS", "C", "captures: In house ok, Opera says cancelled warn (needs a decision), Opera doesn't know bad"],
  P6: ["PASS", "S", "--go-shadow = color-mix of --color-surface (chrome/styles/sheet.ts)"],
  C3: ["PASS", "S", "Setup's unavailable Discard is .btn.off (faint, dashed)"],
  C4: ["OPEN", "S", ".btn.sm geometry per §2; the CARD half is APPS-Q43 — recorded, not failed"],
  C7: ["PASS", "S", "no second button base class; .go is the module root, not a button"],
  C10: ["PASS", "S", ".btn.danger.confirm defined once, chrome/styles/shell.ts"],
  N2: ["PASS", "C", "every capture: no vertical rail"],
  N3: ["PASS", "C", "at most two levels: the bar, and a stay's tabs in the body"],
  N4: ["PASS", "C", "no search in the bar; Bookings' search is in the body"],
  N5: ["PASS", "C", "'Anitha Menon · Front Office · Avenue Regent' — three clauses"],
  N6: ["PASS", "C", "no screen prints its section name; Booking and Stay keep the record's title"],
  N7: ["PASS", "C", "Today's sub-line right of the strip; actions right of the view switcher"],
  L6: ["PASS", "S", "the 6px cell padding names its reason at chrome/styles/table.ts (the approved frame)"],
  D1: ["PASS", "M", "see G6"],
  L3: ["DEVIATION", "M", "the 6px cells are an APPS-Q27 approved deviation: the frame as the owner approved it (83e5157; Part A approved 2026-09-05, cb363a8c) draws .tr>div{padding:6px 10px}. Labelled at chrome/styles/table.ts"],
  D2: ["DEVIATION", "M", "th .08em PASS; field label .07em PASS; the section label — .ch, a card's name, 'a group's name' — is .08em as an APPS-Q27 approved deviation: the approved frame (83e5157) draws it so. Labelled at chrome/styles/panel.ts"],
  G7: ["FAIL", "M", "New booking only — frame 14 as approved puts a note and a card under the pager, leaving the list 152px. With the owner as a drawing: docs/mockups/04-new-booking-list-floor.html"],
  D5: ["OPEN", "M", "64c: .note b built at weight 700 — recorded"],
  G1: ["PASS", "S", "all five paged reads send page/pageSize (Booking did not until this round) and the backend pages through Paging.Of"],
  G2: ["PASS", "S+T", "chrome/pager.ts renders pagedView + PAGER_LABELS; tests/pager.test.ts (the arrows' names, the SDK window)"],
  G10: ["PASS", "S", ".pager background var(--color-surface)"],
  O4: ["PASS", "T", "tests/overlay.test.ts — shown failing with the scrim's guard removed"],
  O5: ["PASS", "S+T", "a refused cancel prepends its reason and keeps the dialog open; tests/perform.test.ts holds the words"],
  O6: ["PASS", "S", "the confirm's count comes from plan.stays, never rows.length"],
  O7: ["PASS", "C", "cancel confirms in a dialog; walk-in and registration compose in sheets"],
  F1: ["PASS", "S", "no <input>/<select>/<textarea> anywhere — every field a drawn .inp"],
  F2: ["PASS", "S", ".fld label 11px uppercase .07em faint; .inp 9px 12px, radius 10"],
  F3: ["NOT REACHED", "T", "no form in GuestOps carries a nullable field to draw"],
  I1: ["FAIL", "S", "12 dates/times rendered in the BACKEND with fixed formats — \"d MMM\", \"dd MMM\", \"HH:mm\", \"ddd d MMM HH:mm\" — across 10 Module/*.cs; none through formatInstant/formatDay"],
  I2: ["FAIL", "T", "follows I1: an absent instant is rendered by the service, not the SDK's '—'"],
  I3: ["PASS", "S", "no machine time in the UI"],
  I4: ["FAIL", "T", "follows I1: with no locale the backend still writes '31 Aug', never the marked ISO form"],
  I5: ["PASS", "S", "elapsed figures ('+3h') come from the service"],
  I6: ["PASS", "S", "no locale-dependent example written without its locale in the UI source"],
  U1: ["BLOCKED", "S", "money is blocked on NUM-Q2 (the wire type of an amount; ADR 0175 already rules the screen formats it) — the amounts in PaymentView and CancelPlanView stay server-formatted until it is ruled, labelled at CancelPlanView.cs. Counts are a separate half, converting view by view (Today, Payment, Requests done; Booking, Bookings and the cancel plan remaining)"],
  U2: ["BLOCKED", "T", "follows U1: money on NUM-Q2; counts as U1 says"],
  X1: ["PASS", "M", "72 failure cells: the failure drawn, no list"],
  X9: ["PASS", "T", "tests/failure-surface.test.ts — no refusal routes to a person"],
  X10: ["PASS", "S", "every read failure is failureDrawing's; cannot() draws only states with no read"],
  X11: ["PASS", "T", "tests/failure-surface.test.ts — the model state says no 'you' or 'account'"],
  X14: ["OPEN", "C", "Today's and From the PMS's Open lands on a screen making the same read; Occupancy, Business Mix and Watchlist land on Today, which shows none of their facts (64d item 5)"],
  X15: ["OPEN", "C", "partial failures: a stay's Activity/Payment tab inside a loaded stay; the cancel plan's failure under the booking's table (64d items 6–7)"],
  H1: ["PASS", "T", "tests/tokens.test.ts"],
  H2: ["PASS", "C", "the gold frames' short lists state it"],
  H3: ["PASS", "C", "frames draw dates as the recorded strings they are"],
  H4: ["PASS", "T", "the drive throws on a step it cannot reach (preview/frame.ts)"],
  H5: ["PASS", "S", "both harnesses refuse an unhandled method"],
  H6: ["PASS", "S", "the mockups link ui/preview/tokens.css and declare no copy of the published tokens; 02's private palette points at them (2026-09-19). 04-06 were drawn that way"],
  L4: ["NOT REACHED", "M", "no drive reaches a selected row; the rule in source is tint only (.tr.sel brand 8%)"],
};

const ORDER = ["P1", "P2", "P3", "P4", "P5", "P6", "C1", "C2", "C3", "C4", "C5", "C6", "C7", "C8", "C9", "C10", "C11",
  "N1", "N2", "N3", "N4", "N5", "N6", "N7", "N8", "L1", "L2", "L3", "L4", "L5", "L6", "D1", "D2", "D3", "D4", "D5",
  "G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9", "G10", "G11", "O1", "O2", "O3", "O4", "O5", "O6", "O7",
  "F1", "F2", "F3", "I1", "I2", "I3", "I4", "I5", "I6", "U1", "U2",
  "X1", "X2", "X3", "X4", "X5", "X6", "X7", "X8", "X9", "X10", "X11", "X12", "X13", "X14", "X15",
  "H1", "H2", "H3", "H4", "H5", "H6"];

const surfaceOf = (id) => id.split("--")[0];
const cellsLine = (rows) => {
  const by = {};
  for (const [id, v, why] of rows) (by[v] ??= []).push([id, why]);
  const verdict = by.FAIL ? "FAIL" : by.UNDECIDED ? "UNDECIDED" : by.PASS ? (by.OPEN ? "PASS / OPEN" : "PASS") : by.OPEN ? "OPEN" : by["N/A"] ? "N/A" : "UNASKED";
  const summary = Object.entries(by).map(([v, list]) => `${v} ${list.length}`).join(" · ");
  const fails = (by.FAIL ?? []).map(([id, why]) => `**${id}** — ${why}`);
  const surfaces = [...new Set(rows.map(([id]) => surfaceOf(id)))].join(", ");
  return { verdict, summary, fails, surfaces };
};

let out = `# GuestOps — app surface audit

Generated by \`preview/audit/table.mjs\` from the run it follows. **Every cell is
the capture HARNESS**, not a Kernel: the owner's GuestOps had not been installed
at 0.3.2 when this was measured, so no real-Kernel state was reachable. The harness
answers from the approved frames' data, re-paged by the backend's own arithmetic
for the five list states, and fails reads by the host kind the SDK maps to each
of the six causes.

Against GG's checklist \`docs/app-surface-checklist.md\` (HotelOsApps \`3d521ce\`).
Verdicts: PASS · FAIL · OPEN (the checklist's nine unsettled lines — recorded,
never failed) · N/A (with why) · NOT REACHED (a state no drive reaches — never a
pass) · DEVIATION (APPS-Q27: the frame as the owner approved it differs from the
written standard for this surface; labelled at the site, never an amendment) ·
BLOCKED (waits on a named open question — neither a pass nor a fail).

**Two deviations, L3 and D2** — APPS-Q27: *"repeated or intentional deviations
trigger a standards-amendment question rather than a third, fourth and fifth
exception."* The count is the architect's signal to watch.

| Line | Check | Verdict | Cells / evidence |
|---|---|---|---|
`;

const fails = [];
for (const line of ORDER) {
  if (measured[line]) {
    const c = cellsLine(measured[line]);
    const stated = STATED[line];
    const verdict = stated && stated[0] !== "PASS" ? stated[0] : c.verdict;
    // A deviation's differing cells are not failures, and the count must not say so.
    const summary = verdict === "DEVIATION" ? c.summary.replace(/FAIL/, "DEVIATING") : c.summary;
    out += `| ${line} | M | ${verdict} | ${summary} — ${c.surfaces}${stated ? `. ${stated[2]}` : ""} |\n`;
    // A deviation's cells differ from the standard by an approved artifact —
    // they are listed where they are labelled, not as failures.
    if (c.fails.length && verdict === "FAIL") fails.push([line, c.fails]);
  } else if (STATED[line]) {
    const [verdict, check, why] = STATED[line];
    out += `| ${line} | ${check} | ${verdict} | ${why} |\n`;
    if (verdict === "FAIL") fails.push([line, [why]]);
  } else {
    out += `| ${line} | — | UNASKED | no cell and no statement |\n`;
  }
}

out += "\n## Every failing cell\n\n";
for (const [line, list] of fails) {
  out += `### ${line}\n\n${list.map((f) => `- ${f}`).join("\n")}\n\n`;
}

writeFileSync(resolve(UI, "..", "docs", "app-surface-audit.md"), out);
process.stdout.write(`${ORDER.length} lines written · ${fails.length} with a failing cell\n`);

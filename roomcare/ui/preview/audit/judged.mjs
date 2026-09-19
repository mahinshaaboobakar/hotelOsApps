// The checklist lines no probe measures and no walker reads — each judged once,
// with the evidence named, so the table carries no silent blank. A line here is
// a claim with its instrument beside it: a test (T), a source read (S), a
// capture read beside its frame (C), or a frame review.
//
// `where` is "screens" (every screen), "widgets", "frames", "source", or a list
// of surfaces. A line judged here and also measured by the probe is not listed.

export const JUDGED = [
  { id: "P5", where: "screens", v: "PASS", how: "C", why: "Pills read against their meaning in every loaded capture: ok = done/clean/applied, warn = due/needs a decision, bad = dirty/over, run = in progress; no pill tone contradicts its word" },
  { id: "N7", where: "screens", v: "PASS", how: "C", why: "No screen prints its section's heading; each screen's sub-line sits right of its numbers strip and its actions right of its view-switcher row, as the locked frames draw them" },
  { id: "D1", where: "screens", v: "SEE", see: "G6", why: "§5's list viewport is G6's measurement" },
  { id: "O4", where: "overlays", v: "PASS", how: "T", why: "tests/overlay.test.ts — a sheet and a dialog stay open when their surface is clicked and close on the scrim; proven able to fail by removing the scrim check (982c801)" },
  { id: "O6", where: "overlays", v: "NA", how: "S", why: "No confirm in Room Care is plural: cancel one deep clean, revoke one grant, one supervisor's call — there is no count to carry" },
  { id: "O7", where: "screens", v: "PASS", how: "C", why: "Reading on the page in inline cards (the room's facts, the job's progress), composing in sheets (13), confirming in dialogs (4) — the split §9 measured in Room Care on 2026-09-14" },
  { id: "F1", where: "source", v: "PASS", how: "S", why: "36 field sites in 15 files read: every input, select and textarea feeds a write in its screen (planDeepClean, attempt, extraTime, issue, assign, recordOnBehalf, saveStates, grantManager, saveArea, saveService, savePolicy, saveWindow, assignZone, saveDeepCleanPlan, decide); the board's person select is a view filter, not a field" },
  { id: "F3", where: "source", v: "PASS", how: "S", why: "Room Care's nullable values are numbers and times: a number or time box cannot hold an empty string, so empty is only ever null; the sold-at time draws its placeholder" },
  { id: "I2", where: "source", v: "PASS", how: "T", why: "tests/instant.test.ts — 'draws a dash for a day nobody recorded, never today'" },
  { id: "I4", where: "screens", v: "PASS", how: "C", why: "The NL captures (19 screens, ?nl=1) draw every instant through formatInstant's no-locale form, ISO, 24-hour, marked UTC" },
  { id: "U2", where: "source", v: "PASS", how: "T", why: "tests/number.test.ts — a strip, the pager and a widget with no locale render 1234 ungrouped" },
  { id: "X12", where: "widgets", v: "PASS", how: "T", why: "tests/widget-sheet.test.ts renders every shipped widget in all six causes against the one sheet a widget mounts; the probe finds no facts on any card" },
  { id: "C11", where: "overlays", v: "SEE", why: "measured: every overlay pressed empty (the -empty cases), and every loaded screen's Save; Setup's Save also by tests/save.test.ts" },
  { id: "X15", where: "source", v: "OPEN", how: "S", why: "Partial failures Room Care draws (64d items 6, 7): the bar's own read failing draws in the identity slot (board-map--f6-me); Move rooms and Reassign draw the whole failure inside the sheet when the rooms or the people cannot be read; Copy refuses in place with the SDK's sentence for a room type it could not read" },
  { id: "H1", where: "source", v: "PASS", how: "T", why: "tests/tokens.test.ts — the harness injects exactly TOKEN_NAMES, set equality" },
  { id: "H2", where: "frames", v: "PASS", how: "frames", why: "Every drawn pager's range matches the rows drawn (1–12 of 62 over 12; 3 of 3; 9 of 9; 4 of 4; 1–6 of 6; 2 of 2); the wall, drawn short, says so inside the list" },
  { id: "H3", where: "frames", v: "PASS", how: "frames", why: "Re-locked 65762c95 (01b, owner: C approved): every date in 01 and 02 is named en-GB. Before: 'Marina Bay is 24-hour, day-month', no locale", before: { v: "FAIL", why: "The frames say 'Marina Bay is 24-hour, day-month' and never name the locale (en-GB) of the dates they draw" } },
  { id: "H4", where: "frames", v: "FAIL", how: "frames", why: "Narrowed, not closed. Re-locked 65762c95: tiles and chips are <button> (0 differing pixels, each whole page, 1400 x 16,600; 01: 142 tiles, 46 chips; 02: 10 chips). Still drawn as div/span: 116 tabs, 111 actions (span.btn), 12 pager buttons. The same retagging does NOT render the same for them (measured: 4,284,723 px on 01, 808,917 on 02), so they need a drawn revision for the owner, not a mechanical one", fix: "owner", before: { v: "FAIL", why: "The frames draw tiles, chips and actions as div and span where the build uses button (ARCH-Q20)" } },
  { id: "H5", where: "source", v: "PASS", how: "C", why: "The harness answers an unrecorded call with a refusal, never a stand-in: every write case in this audit drew the refusal (preview/frame.ts)" },
  { id: "H6", where: "frames", v: "PASS", how: "frames", why: "Re-locked 65762c95: --glass is color-mix(ink 3.5%), derived; before, rgba(255,255,255,.035), the frames' one literal", before: { v: "FAIL", why: "Both frames declare --glass: rgba(255,255,255,.035), a literal of their own" } },
];

/**
 * Owner-approved, surface-specific deviations (APPS-Q27: "an owner-locked artifact can establish an approved
 * surface-specific DEVIATION from a written standard"). The probe still measures the divergence; the table
 * shows it as D, with its ruling, never as a pass and never as an open failure. Each is labelled in the code
 * where it diverges.
 */
export const DEVIATIONS = [
  { id: "C4", column: "Room states · sheet", why: "the owner, 01b, 2026-09-19: \"A small\" — Apply to selected stays .btn.sm in the selection dock (screens/states/sheet.ts). APPS-Q43's card half is still open" },
  { id: "C4", column: "Room states · grid", why: "the owner, 01b, 2026-09-19: \"A small\" — Select all N stays .btn.sm on a zone's row (screens/states/grid.ts). APPS-Q43's card half is still open" },
  { id: "L1", column: "Prepare", why: "APPS-Q27: locked frame 2 draws the changes list in a card, and the owner kept it (01b, 2026-09-19, \"B keep\") — labelled at screens/prepare/index.ts" },
];

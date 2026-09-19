# App surface checklist — page 64, derived

**Derived from, at these revisions** — re-derive when any of them moves:

| Source | Revision |
|---|---|
| `HosPilotOS/docs/working/64-the-app-surface-standard.md` §1–§6, §8–§13, change log (30 rows at derivation) | `2da088bd` — its four stale spots corrected at `20b480cf`, which changes no line below |
| `64f` a list with no rows — **with the owner** | `20b480cf` |
| `64a` note scale, quiet token — RULED 2026-09-16 | `2da088bd` |
| `64b` when a screen cannot read — RULED 2026-09-17 | `5c48963b` |
| `64e` five ways a read is refused or unanswered — APPROVED 2026-09-19 | `c07fb269` |
| `64c` emphasis weight in a note — **open question** | `ff8d0732` |
| `64d` where 64b and the standard disagree — **with the owner, nothing ruled** | `325cd3dd` |
| `@hotelos/sdk` `pager.ts` (the empty-list behaviour, `E0` below) | HosPilotOS `51d95951` |

Derived by GG, 2026-09-19, by reading all 1,476 lines of 64 and the five pages —
not from memory of them. Every line below quotes its rule; a line that could not
quote one is not here.

## How to use it

Audit each application against **every line, in every state the line names**.
Record per line: `PASS`, `FAIL` (with the node or file), `N/A` (with why), or
`OPEN` (what the app builds, for a line whose rule is not settled — never fail
an OPEN line). **A line with no state column entry for a state is not a pass in
that state** — it was never asked.

**The states.** A list screen is not audited in one state, because §6's pager
failed in Jobs in exactly the state nobody opened.

| Code | State | How to reach it |
|---|---|---|
| `E0` | empty list — `total` is 0 | fixture or harness with no rows |
| `E1` | empty page of a non-empty list (`barren`) | a page past the rows, e.g. after a delete |
| `1P` | single page — `total` ≤ page size, **and a short one** (3–5 rows) | fixture |
| `MP` | full page of a multi-page list | fixture > page size |
| `ML` | short last page of a multi-page list | last page of `MP` |
| `F6` | failed read, **each of the six causes**: `unanswered` · `forbidden` · `unadmitted` · `ungranted` · `undecidable` · `faulted` | host kinds `unavailable` · `forbidden` · `local_forbidden` · `user_forbidden` · `model_unavailable` · `internal` |
| `W` | every widget, loaded **and** in `F6` | the widget harness |
| `OV` | every overlay open — sheet and dialog — **and its write failing** | drive the screen |
| `FM` | every form, empty **and** filled | drive parameter, asserted reached (§8) |
| `NUL` | nullable values null — timestamps, optional fields | fixture |
| `NL` | a property with no locale and no zone | host `property: { locale: null, timezone: null }` |
| `ALL` | every screen, loaded | — |
| `SRC` | state-independent — a source or DOM-structure check | — |

**How a line is checked.** `M` measured — a computed property read from a
rendered DOM, never inferred from CSS text · `C` capture — a screenshot read
beside the frame · `T` an automated test · `S` a source walk. *A DOM without
layout cannot show where a strip sits* (§6), so placement lines are `M` or `C`,
never `T` alone.

---

## §1 · The palette is the shell's

| ID | Rule, quoted | Check | States |
|---|---|---|---|
| P1 | *"The published tokens `@hotelos/sdk` publishes are the only names an application may use."* | `S` every `var(--x)` in the app's CSS is in `TOKEN_NAMES` or is an app-local name **derived** from published ones | `SRC` |
| P2 | *"A fallback literal is copied from `styles.css` or it is omitted."* | `S` each `var(--token, literal)` literal equals the shell's value for that token in `apps/desktop/src/styles.css` | `SRC` |
| P3 | *"Prefer them to a local `color-mix` of the same colour."* (the soft tones) | `S` no `color-mix` of `--color-ok`/`warn`/`bad` that duplicates `--color-*-soft` | `SRC` |
| P4 | *"Derive it, never declare it"* — a colour the published set does not cover | `S` no hex/`rgb()` outside a `var()` fallback; an uncovered colour is `color-mix` of published tokens | `SRC` |
| P5 | *"Four pill tones, not three: `bad` is over (cancelled, no-show), distinct from `warn` which is needs a decision"* | `C` every pill's tone matches its meaning | `ALL` |
| P6 | *"The contract publishes no shadow. Derive one from `--color-surface`"* | `S` every `box-shadow` colour is derived from `--color-surface`, no `rgba()` literal | `SRC` |

## §2 · One control vocabulary

| ID | Rule, quoted | Check | States |
|---|---|---|---|
| C1 | *"`.btn` border:1px solid var(--color-line-strong); border-radius:8px; padding:7px 14px; font-size:13px; color:var(--color-ink); background:none"* | `M` computed on a rendered `.btn` | `ALL` |
| C2 | *"`.btn.pri` … border-color:transparent; color:var(--color-ink-on-accent); background: the brand gradient"* — *"Settled: `135deg` … written once, as `--accent`, and derived — never a literal"* | `M` computed; `S` one `--accent`, `135deg`, stops brand → `color-mix(brand 62%, bad)` | `ALL` |
| C3 | *"`.btn.off` unavailable — color:var(--color-ink-faint); border-style:dashed"* | `M` | `FM` |
| C4 | *"`.btn.sm` inside a row or a card — padding:2px 8px; font-size:11px"* | `M` geometry; `S` used only inside a row. **The *card* half is OPEN** — `APPS-Q43`, the approved frames draw a card's own controls full size | `ALL` |
| C5 | *"`.btn.danger` destructive, INLINE — color:var(--color-bad); border-color: color-mix(… bad 45% …)"* | `M` | `ALL` |
| C6 | *"`.btn.danger.confirm` the confirm step … FILLED: background:var(--color-bad); color:var(--color-ink-on-accent); border-color:transparent; font-weight:600"* | `M` in the open confirm dialog | `OV` |
| C7 | *"One class, modified. Not `.btn2`, `.create`, `.mini`, `.go`"* | `S` no second base class for a button | `SRC` |
| C8 | *"A row that opens something is a real `<button>`, and the reset lives on the class"* | `S` DOM: every clickable row is a `button`; `M` no UA border, text left-aligned, family inherited | `ALL` |
| C9 | *"`font:inherit` AND `line-height:inherit` on every control"* | `M` a control's computed `line-height` equals its parent's (the 2.5px defect is invisible in a capture) | `ALL` `FM` |
| C10 | *"The destructive twin lives in the chrome, not in a screen."* | `S` `.btn.danger.confirm` is defined once, in the app's chrome stylesheet | `SRC` |
| C11 | *"A primary action with nothing to send is drawn `off`, with the reason beside it — never live-and-refusing"* | `T` a form with nothing to send renders `.btn.pri.off` and a visible reason | `FM` |

## §3 · Navigation

| ID | Rule, quoted | Check | States |
|---|---|---|---|
| N1 | *"An installed application puts its sections in a top bar"* — *"`height:56px`, `padding:0 22px`, one bottom rule, the app mark and name first, sections as tabs with a 2px brand underline on the active one, and the signed-in person pushed right"* | `M` bar box and active underline | `ALL` |
| N2 | *"The platform's own four keep the left rail"* — an installed app draws none | `C` no vertical navigation rail | `ALL` |
| N3 | *"Two-level tabs are approved as drawn … Two levels remain the ceiling"*; *"a view switcher within a section stays in the body"* | `C` at most two tab levels; the second in the body | `ALL` |
| N4 | *"No search box in the bar unless the app has one."* | `C` a bar search exists only over a real search | `ALL` |
| N5 | *"The signed-in person reads `name · department · property`. Three clauses"* | `T` the identity clause's parts; `C` | `ALL` `NUL` |
| N6 | *"A screen does not print its own section name"* — *"A title that names the record stays"* | `C` per screen: no heading repeating the active tab; a record's own title kept | `ALL` |
| N7 | the removed heading's *"sub-line"* goes to *"the right of the numbers strip"*; its *"actions"* to *"the right of the view-switcher row"* | `C` | `ALL` |
| N8 | *"A body with nothing above it is padded on all four sides"* | `M` `.body` top padding > 0 | `ALL` |

## §4 · The list is a table

| ID | Rule, quoted | Check | States |
|---|---|---|---|
| L1 | *"A list sits bare on the page — No wrapper, no fill, no radius, and rows separated by a single rule"*; *"`table` width:100%; border-collapse:collapse; font-size:13px"* | `M` no ancestor between list and body with background, border or radius | `1P` `MP` |
| L2 | *"`th` padding:8px 10px; font-size:11px; weight 500; uppercase; letter-spacing:.08em; color:--color-ink-faint; border-bottom:1px --color-line"* | `M` | `1P` `MP` `E0` |
| L3 | *"`td` padding:10px; border-bottom:1px --color-line; vertical-align:top"* — *"In a flex-based grid … `align-items:flex-start`, with the header row centred"* | `M` | `1P` `MP` |
| L4 | *"The selected row is a tint and nothing else"* — *"`tr.sel` background: color-mix(… brand 8% …)"* | `M` on a selected row; no inset bar | `1P` `MP` |
| L5 | *"The last row keeps its rule."* | `M` last row's `border-bottom` | `1P` `ML` |
| L6 | *"Vertical padding may shrink, and only for a reason you can name."* | `S` any `td` padding below 10px carries its reason in a comment | `SRC` |

## §5 · Density

| ID | Rule, quoted | Check | States |
|---|---|---|---|
| D1 | *"list viewport scrolls within the screen; the pager does not move"* | see G6 | `MP` |
| D2 | *"table header .08em · field label .07em · section label .04em"* — RULED `APPS-Q33` | `M` letter-spacing per **role** | `ALL` `FM` |
| D3 | *"Text that explains rather than reports is 12px. There is one size for it"*; *"Its line-height is `19.8px`"* — RULED `APPS-Q35`, `64a` | `M` on note-role nodes, found by the rule that governs them — *"never by replacing a figure wherever it occurs"* | `ALL` `F6` |
| D4 | *"Where a surface needs quiet text a person still reads, the token is `--color-ink-muted`."* — RULED `64a` | `M` quiet-role nodes compute `ink-muted` | `ALL` |
| D5 | the weight of emphasis inside a note, 500 or 700 — **OPEN, `64c`, unruled** | `M` record the built weight; do not fail | `ALL` `F6` |

## §6 · The pager

| ID | Rule, quoted | Check | States |
|---|---|---|---|
| G1 | *"Paged — `page`/`page_size` → `total` — is the default for every bounded operational list. Cursor is kept for feeds"* — *"no application defines a third"* — RULED `CORE-Q13` | `S` each list RPC uses `common.v1` `PagedRequest`/`PagedResponse` or the cursor pair | `SRC` |
| G2 | *"ONE pager component"* — the words are `PAGER_LABELS`, the arithmetic `pagedView` | `S` the pager renders from `@hotelos/sdk`'s `pagedView`/`PAGER_LABELS`; no hand-written range or labels | `SRC` |
| G3 | *"`showing 1–14 of 14`, with the arrows disabled. A one-page list gets a pager like any other."* | `T` range text and disabled arrows; `C` | `1P` |
| G4 | *"`last` is `first + rows.length - 1`, clamped by `total`. Not `(page + 1) * pageSize`."* | `T` range equals rows shown | `ML` `MP` |
| G5 | *"An empty page says so — no rows on this page · 218 in the list"* | `T` barren text, no range | `E1` |
| G6 | *"The page does not scroll. The heading and the pager both stay put, and the list is the scroll container."* — RULED `CORE-Q28`; the snippet: `.body:has(.pager){overflow:hidden}`, list `flex:1 1 auto;min-height:0;overflow-y:auto`, `.pager{flex:0 0 auto}` *"no sticky"* | `M` body `scrollHeight == clientHeight`; the list is the element that scrolls; no `position:sticky` on the pager | `MP` `1P` `E0` |
| G7 | *"The pager is the list's floor. The list grows to take the free space, so a short list still puts the pager at the bottom"* — RULED 2026-09-05 | `M` pager bottom = body floor **and** list box taller than its rows — *"measured rather than inferred … the pager looks right even when the list has not grown"* | `1P` `E0` `E1` `ML` |
| G8 | *"Scoped to a body that has a pager"* — a rule about lists applied without one *"removes content"* | `M` on every screen **without** a pager, nothing is clipped past the body | `ALL` |
| G9 | the pager is the table's next sibling — *"what `~` depends on and what a wrapper would silently break"* | `T` DOM sibling | `1P` `MP` `E0` |
| G10 | the strip's colour *"from `--color-surface` and never from a literal"* | `S` | `SRC` |
| G11 | **The empty list (`E0`) is not ruled.** §6 rules a single page (G3) and an empty page (G5); `pagedView` returns `empty: true` and says *"A caller draws its own empty state instead"* (`pager.ts:94`). Whether `E0` draws a pager, a count (`0 in the list`), or neither is **OPEN — drawn for the owner in `64f`** | `C` record what `E0` draws and where; G6, G7 still apply to its placement | `E0` |

## §9 · Overlays

| ID | Rule, quoted | Check | States |
|---|---|---|---|
| O1 | *"A sheet enters from the right … A dialog sits in the middle … Same head, body and foot; the scrim decides which"*; `.sheet 440px · full height`, `.dlg 520px` | `M` sheet width/height/edge, dialog width/centre, shared `.dh .db .df` | `OV` |
| O2 | *"`position:absolute`, never `fixed`."* | `M` computed `position` of scrim and surface | `OV` |
| O3 | *"The overlay is a sibling of `.body`, not a child."* | `T` DOM | `OV` |
| O4 | *"The scrim dismisses; the surface does not."* | `T` click inside keeps it open; click on scrim closes | `OV` |
| O5 | *"A refusal keeps the overlay open, carrying the reason."* | `T` a failed write leaves the overlay open with the reason | `OV` `F6` |
| O6 | *"The confirm button is the filled danger control … it carries the count when the operation is plural … a stated field on the payload, never `rows.length`"* | `M` C6 styling; `S` the count's source | `OV` |
| O7 | *"Reading happens on the page, in an inline card. Composing happens in a sheet. Confirming happens in a dialog."* — `RC-Q6` | `C` per surface: what is the person doing when they arrive | `ALL` `OV` |

## §10 · Fields

| ID | Rule, quoted | Check | States |
|---|---|---|---|
| F1 | *"It is a `<div>`, not an `<input>`, until there is a write path behind it that accepts what is typed."* | `T` every `input`/`select`/`textarea` reaches a write call | `FM` |
| F2 | *"`.fld label` 11px · uppercase · .07em · ink-faint"*; *"`.inp` 9px 12px · radius 10 · line-strong border · ink at 2%"* | `M` | `FM` |
| F3 | *"`null` and `""` are different. Null … draws the placeholder"* | `T` null → `.inp.ph`; `""` → an empty value | `FM` `NUL` |

## §11 · Instants

| ID | Rule, quoted | Check | States |
|---|---|---|---|
| I1 | *"Every date, time and duration … is rendered by `@hotelos/sdk`'s `formatInstant`, `formatDay` or `formatDuration`, from the property's own locale and timezone. No module calls `Date` or `Intl` itself."* — `JOBS-Q1(8)` | `S` no `Intl.DateTimeFormat`, `toLocale*String`, or display use of `Date`; every ISO field rendered through the SDK | `SRC` |
| I2 | *"Absent renders `—`, never today."* | `T` null timestamp → `—` | `NUL` |
| I3 | *"Machine time is for machine facts only."* | `S` any machine-time use is a machine fact | `SRC` |
| I4 | *"A property with no locale or zone renders `2026-09-02 13:31 UTC` — ISO, 24-hour, marked"* | `T` | `NL` |
| I5 | *"An elapsed figure comes from the service."* | `S` no subtraction from `new Date()` for display | `SRC` |
| I6 | *"Where an example is a locale's output, write which locale, and derive it rather than typing it."* | `S` date examples in comments name their locale | `SRC` |

## §12 · Numbers

| ID | Rule, quoted | Check | States |
|---|---|---|---|
| U1 | *"Every user-facing number goes through `@hotelos/sdk`'s `formatNumber`, in the property's locale."* — `NUM-Q1`, ADR 0174 | `S` no `toLocaleString`, `Intl.NumberFormat` or `toFixed` on a displayed number | `SRC` |
| U2 | *"When the property's locale is not established, `formatNumber` applies no grouping at all."* — a screen must not substitute one | `T` a grouped-size number renders ungrouped | `NL` |

## §13 · When a screen cannot read — `64b`, `64e`

| ID | Rule, quoted | Check | States |
|---|---|---|---|
| X1 | *"a failed read renders a failure naming the reason, never recorded rows"* | `T` per screen: failure drawn, no list rows | `F6` |
| X2 | `64b` Treatment A: the state *"centred in the window"* (`.body { display:grid; place-items:center }`), `.state { width:min(560px, 92%) }` | `M` centred block and width | `F6` |
| X3 | *"The mark is drawn, not typed"* — *"Line glyphs, one stroke weight, `currentColor`"*; its own line. Colours: `64b` unanswered warn, forbidden dim, faulted bad; `64e` unadmitted and ungranted neutral, undecidable red | `M` an inline `svg` stroked `currentColor`; computed colour per cause | `F6` `W` |
| X4 | `64b` `.st-label`: *"11px; mono; letter-spacing:.1em; uppercase; faint"*, never tinted by the cause | `M` size, spacing, colour. **The mono stack is OPEN — `64d` item 4** (no mono token published) | `F6` |
| X5 | `64b` `.st-said` 19px, 600 | `M` | `F6` |
| X6 | `64b` `.st-why` 14px, dim; `64b`/`64e` bold the clause the drawing emphasises | `M`; `T` the emphasised run is set apart | `F6` |
| X7 | *"The action is what keeps a timeout and a refusal apart"*: retry for `unanswered` only; *"Copy these details"* for a fault (and `64e`'s `undecidable`); a refusal *"names the grant and stops"*. **Button fill OPEN — `64d` item 1**; the *needs…* line's size **OPEN — `64d` item 2** | `T` which control per cause | `F6` |
| X8 | *"The four facts are labelled, never a dotted line"* — Asked for, Answer, At, under a thin rule; the permission set apart | `T` a `dl` with those labels; permission in its own run. **The moment's form (UTC and seconds vs property time) OPEN — `64d` item 8** | `F6` |
| X9 | *"A refusal names the grant and stops. It never routes the reader to a person: no name, no role, and not ask an administrator"* | `T` text of every refusal (`forbidden`, `unadmitted`, `ungranted`) | `F6` |
| X10 | *"No application writes these words itself."* | `S` every failure sentence comes from `failureDrawing` | `SRC` |
| X11 | `64e`: the application's refusal *"names the application"*, the account's *"names the account"*, the model *"names the model and never the person"* | `T` note text per cause; no *you*/*account* in the model state | `F6` |
| X12 | *"At 320px the four facts move to the screen the card opens."* The card: 20px mark, the SDK's `briefSaid`, `brief` and `onward` | `T` render **every widget in every cause** and check each class against the sheet the widget mounts; no facts on the card | `W` |
| X13 | the widget card's height, 200 (`64b`) vs 384 (`SHELL-Q35`) — **OPEN, `64d` item 3** | `M` record | `W` |
| X14 | a widget's *Open* must lead to a screen that shows its facts — **OPEN, `64d` item 5** | `C` record where each widget's *Open* lands, and whether that screen shows the failed read's facts | `W` |
| X15 | a failure inside a screen that loaded — a strip, a panel under a loaded table — **OPEN, `64d` items 6 and 7** (64b draws only whole-screen and widget) | `C` record every partial-failure placement the app has | `F6` |

## §8 · What the app's drawings and harness owe

| ID | Rule, quoted | Check | States |
|---|---|---|---|
| H1 | *"A capture harness injects exactly the published set … assert it as set equality against `TOKEN_NAMES`, never a copied list"* | `T` | `SRC` |
| H2 | *"A frame drawn short says so, inside the list"*; *"a number in a mock is checked against the frame it sits in"* | `C` frame review | frames |
| H3 | *"names the locale of any date it draws"* | `C` frame review | frames |
| H4 | *"A frame is drawn in the element types the build uses"* — `ARCH-Q20`; *"it draws the fixture the harness holds"*; *"the harness reaches the state"* and *"The drive asserts the state was reached"* | `C` frame review; `T` the drive's reached-state assertion | frames `FM` |
| H5 | *"A capture harness refuses; it never renders a stand-in"* | `T` an unhandled route fails the capture | `SRC` |
| H6 | *"declares no `:root` palette of its own"* | `S` frame source | frames |

---

## Mapping — every change-log row, and every ruling in the body

**All 30 change-log rows map to at least one line. 29 carry a rule; the 30th
records the page's creation and carries none** — mapped to the whole list,
stated rather than padded with a line.

| # | Log row (date · §) | Lines |
|---|---|---|
| 1 | 2026-09-16 · 5 · two type rulings, `64a` | D3 · D4 |
| 2 | 2026-09-17 · 13 · a refusal names the grant and stops | X9 |
| 3 | 2026-09-17 · 13 · when a screen cannot read, `64b`, RULED | X1–X8 · X12 |
| 4 | 2026-09-04 · — · page created | *no rule — the source of every line* |
| 5 | 2026-09-04 · 1 · shell's tokens; fallbacks are the shell's | P1 · P2 |
| 6 | 2026-09-04 · 2 · one `.btn`; `135deg` | C1 · C2 · C7 |
| 7 | 2026-09-04 · 3 · 56px top bar; the four keep the rail | N1 · N2 |
| 8 | 2026-09-04 · 3 · no heading repeating the bar; where things go; top padding | N6 · N7 · N8 |
| 9 | 2026-09-04 · 4 · bare table, `vertical-align:top` | L1 · L3 |
| 10 | 2026-09-04 · 6 · `CORE-Q13` | G1 · G2 |
| 11 | 2026-09-04 · 8 · a short-drawn frame states it; numbers checked | H2 |
| 12 | 2026-09-04 · 3 · `name · department · property` | N5 |
| 13 | 2026-09-04 · 1 · fourteen → seventeen, soft tones | P3 |
| 14 | 2026-09-04 · 2 · `.btn.danger` splits | C5 · C6 |
| 15 | 2026-09-05 · 6 · the pager states the rows it has; empty page says so | G4 · G5 |
| 16 | 2026-09-05 · 6 · the pager is the list's floor (sticky — mechanism superseded by row 18) | G7 · G6 |
| 17 | 2026-09-05 · 6 · snippet corrected (superseded by row 18) | G6 · G7 |
| 18 | 2026-09-09 · 6 · only the list scrolls, `CORE-Q28` | G6 · G8 |
| 19 | 2026-09-05 · 6 · draws on a single page | G3 |
| 20 | 2026-09-05 · 9 · sheet composes, dialog confirms | O1–O6 |
| 21 | 2026-09-05 · 10 · a field is drawn until accepted | F1 · F2 · F3 |
| 22 | 2026-09-05 · 2 · a primary with nothing to send is `off` | C11 |
| 23 | 2026-09-05 · 1 · four pill tones | P5 |
| 24 | 2026-09-05 · 1 · no shadow published | P6 |
| 25 | 2026-09-04 · 3 · two-level tabs approved | N3 |
| 26 | 2026-09-05 · 2 · `line-height:inherit` | C9 |
| 27 | 2026-09-05 · 8 · harness injects exactly the set | H1 |
| 28 | 2026-09-14 · 9 · the inline card is the third surface, `RC-Q6` | O7 |
| 29 | 2026-09-14 · 11 · instants | I1–I6 |
| 30 | 2026-09-14 · 8 · a mock names the locale | H3 |

**Rulings in the body, each mapped** — 11, of which three have **no change-log
row** (reported below):

| Ruling | Lines |
|---|---|
| §3 two-level tabs — owner, 2026-09-04 | N3 |
| §3 three clauses — owner, 2026-09-04 | N5 |
| §5 section label `.04em` — RULED `APPS-Q33` | D2 |
| §5 note text 12px — RULED `APPS-Q35` | D3 |
| §5 quiet text `ink-muted` — RULED 2026-09-16 | D4 |
| §6 — RULED `CORE-Q13` | G1 · G2 |
| §6 only the list scrolls — RULED `CORE-Q28` | G6 · G8 |
| §6 where it sits — RULED 2026-09-05 | G7 |
| §13 — RULED 2026-09-17, `64b` | X1–X12 |
| §12 `NUM-Q1`, ADR 0174 — ruled 2026-09-16 | U1 · U2 |
| §8 `ARCH-Q20` — 2026-09-10 | H4 |

**`64a`–`64e`:** `64a` → D3, D4 · `64b` → X1–X8, X12 · `64e` → X3, X7, X9, X11 ·
`64c` → D5 (OPEN) · `64d` → C4's card half is `APPS-Q43`, not 64d; X4, X7, X8,
X13, X14, X15 carry 64d's eight items as OPEN.

**Open lines, never failed:** C4 (card half) · D5 · G11 · X4 (stack) · X7
(fill, *needs…* size) · X8 (the moment) · X13 · X14 · X15.

## What deriving this found about the standard itself

**All four corrected in page 64 at `20b480cf`, keeping what each said** — and
`64f` now draws the empty list for the owner. Kept below as found, because this
file was published saying them.

For the architect, not for the apps — none of these changes an audit result:

1. **The change log has no row for §12** (`NUM-Q1`, ruled 2026-09-16), **none for
   §8's `ARCH-Q20`** (2026-09-10), and **none for `64e`** (approved 2026-09-19).
   A conforming app reading the log — *"Read the log before conforming"* — would
   not learn that any of the three moved.
2. **§13 still says *"Three states, because the platform has exactly three"*** and
   quotes `Cause = "unanswered" | "forbidden" | "faulted"`. Since `d45f028d` the
   SDK has six, and `64e` is approved. The section describes the platform as it
   was.
3. **§6 does not rule the empty list** (G11) — the state Jobs failed in. The SDK
   hands it to each caller, so four apps will draw four answers unless it is
   ruled.
4. **§7 says *"Open: Nothing"*** while `64c` is an open question and `64d`'s eight
   items are with the owner.

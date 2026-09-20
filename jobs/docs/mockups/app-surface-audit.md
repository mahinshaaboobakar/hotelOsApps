# Jobs against the app surface checklist — 86 lines

**Checklist:** `HotelOsApps/docs/app-surface-checklist.md`, GG, `3d521ce` (page 64 and
64a–64e). **Audited:** 2026-09-19, Stream HH. **Source audited:** the tree committed with this
file. **Instruments:** `ui/tests/standard-source.test.ts` (S), `ui/tests/standard-render.test.ts`
and the existing suites (T), `docs/mockups/surface-audit.mjs` → `measured-surface.json` (M, 66
cases), `docs/mockups/failure-audit.mjs` → `measured-64b.json` (64b, 224 declarations), captures
read beside the frames (C).

**Where it ran — stated, not implied.** Every loaded state runs in the capture harness
(`ui/preview/frame.html`). The real Kernel refuses this stream's bearer: `hh@kochi.test` was
seeded by `scripts/seed-user`, which announces nothing, so the graph holds no `property#member`
for it (DD's repair). A loaded screen cannot be reached there at all, and a failed one only as
the refusal. The harness states are `?data=empty|barren|single|last` beside the recorded
multi-page example (12 rows of a 47-row list), `?fail=<kind>` for each of the six causes, and
`?locale=none` for `NL`.

**States.** Lists: the Board in `E0 E1 1P MP ML`; Scheduled in `E0 1P`; Policies `1P`. Failed
reads: seven screens (Board, Live, Scheduled, Catalogue, Settings, a job, Raise) × six causes =
42, plus the six widgets × six causes = 36 (`widget-failure.test.ts`). Every screen loaded: 24
cases, including the selected-row Board, `NL`, and Raise empty and filled.

**Proof of each fix.** Every FAIL below was run against the code before its fix and failed; the
guard that failed stays. Where the fix landed before its guard existed, the guard was run against
the unfixed file or a mutation of it, and the proof names which.

## Result

One bucket per line; the five add to 86.

| | Lines |
|---|---|
| **PASS** | 57 — of which four have an OPEN half, recorded: C4 (card half) · X4 (mono stack) · X7 (fill, *needs…* size) · X8 (the moment); and **O7**: New item compliant with §9 as amended (APPS-Q53), Add a step allowed but not yet a composer — it passed as an APPS-Q27 deviation until §9 was amended on 2026-09-19 |
| **FAIL → fixed in 0.4.2** (each shown failing first) | 14 — C2 · C8 · C11 · D2 · D3 · D4 · G1 · G2 · I3 · I5 · I6 · N4 · P5 · U1 |
| **FAIL → fixed after 0.4.2** (shown failing first; in no package yet) | 1 — N5, with its department clause OPEN on ADR 0203 |
| **FAIL → the frames; drawn for the owner** (APPS-Q44) | 2 — H3 · H6 |
| **OPEN** (recorded, not failed) | 5 — D5 · G11 · X13 · X14 · X15 |
| **N/A** (with the reason) | 7 — G10 · O1–O6 |

## The table

`S` source · `T` test · `M` measured · `C` capture. **F→fixed** = failed, fixed, proof named.

### §1 · The palette

| ID | Result | Surfaces · states | Evidence |
|---|---|---|---|
| P1 | PASS | SRC | `tokens.test.ts`: every `var(--x)` is published or Jobs' own `--accent`, declared once and derived only from published tokens (the test was narrowed to exactly P1's words for it) |
| P2 | PASS | SRC | `tokens.test.ts`: every fallback literal is the platform's value |
| P3 | PASS | SRC | no background `color-mix` of ok/warn/bad duplicating a soft tone. The check was **narrowed** on its first run: it fired on `.btn.danger`'s 45% border, which C5 prescribes |
| P4 | PASS | SRC | no colour literal outside a `var()` fallback |
| P5 | **F→fixed** | ALL | `CANCELLED` took `hold` (a job that resumes); §1: *bad is over (cancelled, no-show)*. The locked frame never draws CANCELLED, so the tone was the build's. Proof: the P5 test fails with the old tone restored. ACCEPTED stays `warn` — the locked frame draws it so |
| P6 | PASS | SRC | no `box-shadow` |

### §2 · Controls

| ID | Result | Surfaces · states | Evidence |
|---|---|---|---|
| C1 | PASS | M · 11 loaded screens with a `.btn` | border 1px line-strong, radius 8, 7×14, 13px, ink, no fill |
| C2 | **F→fixed** | M · 19 screens with a live primary; S | Measured fill correct everywhere; the **source** wrote the 135deg gradient twice (`.mark`, `.btn.pri`) and no `--accent`. Now once, as `--accent`, derived. Proof: the C2 (S) test fails on HEAD's stylesheet (2 gradients) |
| C3 | PASS | S · M | `.btn.off` faint and dashed; the off primary (`.btn.pri.off`) drops the gradient, measured on Settings' clock |
| C4 | PASS · card half OPEN | M · 6 | `.btn.sm` 2×8, 11px. Card half OPEN — `APPS-Q43`; Settings draws `.btn.sm` in two card headers (*All policies*, *Engineering's clock*), recorded as built |
| C5 | PASS | S · T | `.btn.danger` bad text, 45% bad border (`controls.test.ts`) |
| C6 | PASS | T | the confirm step is filled (`controls.test.ts`). Jobs' confirm is inline, not an overlay |
| C7 | PASS | T | one base class (`controls.test.ts`); `opener` admitted as C8's reset class, not a second geometry |
| C8 | **F→fixed** | T · Board rows | Board rows opened on a click no keyboard reaches (`<tr>` listener). The job number is now a real `<button class="opener">`; the row's own click ignores it so the job opens once. Widget rows were already buttons. Proof: the C8 test failed first | · **APPS-Q50 (`723dd42b`), 2026-09-19**: the row's listener on the `<tr>` removed; the job number's button is stretched over the row (`.opener::after`, `tr.pick{position:relative}`). Measured in Edge: the centre of all nine cells hits that row's button, and the next row hits its own
| C9 | PASS | M · 11 screens + the Raise inputs | every control and input inherits family and the 1.5 factor (13px → 19.5px). **The checklist's check misfires here** — see "about the checklist" |
| C10 | PASS | SRC | `.btn.danger.confirm` defined once, in chrome |
| C11 | **F→fixed** | T · Settings clock, policies, new policy 1 and 3 | three live primaries did nothing — Engineering's clock *Save* (and *Discard*), the add-a-step *Add* (and *Cancel*), and *Save policy*, which returned to the list and saved nothing. Now `.btn.pri.off`, disabled, reason beside it. `control()` marks what it wires (`data-acts`) so a check can see it. Proof: failed on the clock first; the widened walk proven by mutation (old primaries restored → *expected false*) |

### §3 · Navigation

| ID | Result | Surfaces · states | Evidence |
|---|---|---|---|
| N1 | PASS | M · all 66 | 56px bar, 22px, one rule, 2px brand underline on the active tab |
| N2 | PASS | C | no left rail |
| N3 | PASS | C | two tab levels (top; Settings' and a job's second). The policy flow's *1 · 2 · 3* is a view switcher in the body, which §3 allows |
| N4 | **F→fixed** | T · ALL | a search box (*Search job number, room, summary…*) that was a `<div>` over no search. Removed. Proof: the N4 test failed first |
| N5 | **FAIL → fixed** · department OPEN | T · ALL, NUL | §3: *name · department · property*. The backend returned `OperatorView("Signed in", <property code>)`, and the recorded *Priya Nair · ENG supervisor* hid it. **Now as Room Care reads it** (the architect's direction, 2026-09-19): the name from `masterdata.staff` by login, the property by name and else by code. **The department is ADR 0203's** (CTX-Q9): sent as `null` and drawn as the words *department not known yet*, never a stand-in. **For a person whose only role is organization-wide, that sentence is now the ruled answer** — `CTX-Q10` was ruled on 2026-09-20 and **withdrawn by its author the same day** (`463b8df7`) on this stream's measurement: ADR 0116 §2 keeps the organization administrator in VibeMind Cloud, Identity records no organization-wide grant, and the graph holds no such tuple, so Context has nothing to resolve it from. **Stopping to ask cost a paragraph instead of a schema**: a field on `StaffContext`, a resolver, a Jobs Context client and a UI clause, all built to display a fact that does not exist property-side. What Context may expose later is property-local — property administrator and general manager Failing first: backend `Me_*` (2) read *"Signed in"*; UI N5 (2) read *"Priya Nair · undefined"* and *"null · undefined"* |
| N6 | PASS | C · every screen | no heading repeats the active tab; Raise's *Raise a job* names the composition, which is not a tab |
| N7 | PASS | C | the strip's sub-line at its right; *Raise a job* at the right of the chips |
| N8 | PASS | M · 24 | body top padding 22px |

### §4 · The list

| ID | Result | Surfaces · states | Evidence |
|---|---|---|---|
| L1 | PASS | S · Board, Scheduled, Policies in 1P/MP | the list (`.tbl`) carries no fill, border or radius. Settings' clock table sits in a card as its locked frame draws — a settings table, not a paged list |
| L2 | PASS | M · 18 | th 8×10, 11px, 500, uppercase, .08em, faint, 1px rule |
| L3 | PASS | M · 15 | td 10px, 1px rule, top |
| L4 | PASS | M · the selected-row Board | a tint, no inset bar, no shadow |
| L5 | PASS | M · 15 incl. ML | the last row keeps its rule |
| L6 | PASS | SRC | no td padded below 10px |

### §5 · Density

| ID | Result | Surfaces · states | Evidence |
|---|---|---|---|
| D1 | PASS | M | = G6 |
| D2 | **F→fixed** | M · 6 screens with a section label | `.sect` was `.12em`; APPS-Q33 rules `.04em`. Labels (`.07em`) and headers (`.08em`) passed. Proof: measured failing (1.32px) before, 0.44px after |
| D3 | **F→fixed** | M · 22 cases | `.note` was 13px; `.mono` and `.hint`, which carry Jobs' explanatory sentences, sat on 18px. Now 12px on 19.8px (64a, APPS-Q35). Proof: measured failing before, passing after |
| D4 | **F→fixed** | M · 44 cases | `.dim` (*clock stopped*, *Nobody at this property holds it*) and the operator `.who` were faint; readable quiet text is `ink-muted` (64a). Proof: measured failing before |
| D5 | OPEN | record | emphasis in a note: `.note b` draws at the UA bold, 700; the failure surface's `.gap-why b` at 600 (64e's declared rule) |

### §6 · The pager

| ID | Result | Surfaces · states | Evidence |
|---|---|---|---|
| G1 | **F→fixed** (Scheduled) · PASS (Board) | S · T | the Board's `ListJobs` uses `PagedRequest`/`PagedResponse`. **Scheduled** took ONE page at `MaxPageSize`, **dropped the total**, and returned a bare array — under a pager that called it the whole list — and re-read every scheduled job unpaged for the dates. Now paged: `{ rows, paging }`, page and size from the caller, dates for the page's jobs only; the screen pages like the Board. Proof: two backend tests failed on the unchanged backend (`Scheduled_is_paged_…` asks for 2 of 3 and needs total 3). Backend 87/87 after |
| G2 | **F→fixed** | S | the pager was hand-written — arithmetic, page list, wording — and `counted()`, written the same morning to unify three lists, was a third copy. Now `pagedView` + `PAGER_LABELS`. Proof: the G2 test failed first |
| G3 | PASS | T · 1P | *1–5 of 5*, both arrows present and disabled |
| G4 | PASS | T · ML | *37–47 of 47* — the rows shown, clamped by the total |
| G5 | PASS | T · E1 | *no rows on this page · 47 in the list*, no range |
| G6 | PASS | M · Board E0/E1/1P/MP/ML/selected, Scheduled E0/MP, Policies | body scrollHeight = clientHeight; the list scrolls; no sticky |
| G7 | PASS | M · same 10 | the pager's bottom is the body's floor, and the list box is taller than its rows (e.g. Policies: 679 against 338) |
| G8 | PASS | M · 14 unpaged screens | no body clips without a pager |
| G9 | PASS | T | the pager follows the list's `.tbl`, which is what `.tbl:has(~ .pager)` needs (`acting.test.ts`) |
| G10 | N/A | — | no sticky strip exists since CORE-Q28 — the pager has no background to colour |
| G11 | OPEN | C · Board E0, Scheduled E0 | draws *no jobs in this list*, both arrows disabled, no page numbers; at the floor (G6, G7 pass). To the owner as 64f |

### §9 · Overlays

| ID | Result | Surfaces · states | Evidence |
|---|---|---|---|
| O1–O6 | N/A | — | Jobs has **no overlay** — no scrim, sheet or positioned dialog. Raise and Resolve are full screens (APPS-Q26 names Raise's exception) |
| O7 | **New item: compliant with §9 (APPS-Q53) · Add a step: not yet a composer** | C | page 64 §9 (`045402b4`) allows an inline composer under seven conditions, and retires the APPS-Q27 deviation label. **New item** now meets all seven — it had no Cancel (condition 3), added and shown failing first. **Add a step** meets none of those that need it to work: Add is off, its fields are drawn, it has no Cancel; it is built to all seven when adding a step is built |

### §10 · Fields

| ID | Result | Surfaces · states | Evidence |
|---|---|---|---|
| F1 | PASS | T · FM | every input-taking form reaches its write: Raise, Resolve, Notes, the Catalogue's curation, a Settings form (`acting.test.ts`); the other Settings forms share the same `saveRow` wiring |
| F2 | PASS | S · M | `.field` 9×12, radius 10, line-strong, ink at 2%; `label.lbl` 11px, uppercase, .07em, faint |
| F3 | PASS | S | a drawn empty value is `.field.ph`; an input's `""` stays empty |

### §11 · Instants

| ID | Result | Surfaces · states | Evidence |
|---|---|---|---|
| I1 | PASS | SRC | no `Intl` / `toLocale*String` |
| I2 | PASS | T · NUL | an absent due draws no date |
| I3 | **F→fixed** | SRC | see I5 |
| I4 | PASS | T · NL | *2026-09-02 13:31 UTC* |
| I5 | **F→fixed** | SRC | Raise drew `Date.now() + allowance` from **this machine's** clock as a due time for a job that did not exist yet. Now *within 60 min* — the item's allowance, as the frame annotates it. Proof: the I3/I5 test failed first |
| I6 | **F→fixed** | SRC | four comments quoted dates without their locale; each now names en-GB (Asia/Qatar). Proof: the I6 test failed first |

### §12 · Numbers

| ID | Result | Surfaces · states | Evidence |
|---|---|---|---|
| U1 | **F→fixed** | SRC | **~40 displayed numbers written with `String(n)`** — the Board's strip, every widget figure, the pager's range and page numbers, tab counts, counts in sentences. Now `formatNumber` in the property's locale. **The checklist's check passed Jobs** (it looks for `toLocaleString` / `Intl` / `toFixed`); the rule says *every*. Proof: a wider test (`String()` allowed only for form values and CSS lengths) fails with one displayed `String()` restored |
| U2 | PASS | S | every displayed number now goes through `formatNumber`, whose `NL` behaviour (no grouping) is the SDK's |

### §13 · When a screen cannot read

| ID | Result | Surfaces · states | Evidence |
|---|---|---|---|
| X1 | PASS | M · 42 | every failing screen draws the surface, never rows |
| X2 | PASS | M · 42 | centred grid, 560px block |
| X3 | PASS | M · 42; T · 36 widget | the stroke colour per cause, 64b and 64e |
| X4 | PASS · stack OPEN | M · 42 | 11px, 1.1px, uppercase, faint. Mono stack OPEN (64d item 4) |
| X5 | PASS | M · 42 | 19px / 600 |
| X6 | PASS | M · 42; T | 14px, muted; the emphasised clause is its own run, bold |
| X7 | PASS · fill and size OPEN | T · 6 causes | retry only for unanswered; *Copy* for a fault and the model; refusals stop. Fill OPEN (64d 1), *needs…* size OPEN (64d 2) |
| X8 | PASS · moment OPEN | T · M | labelled `dl`, rule above, permission its own run. The moment's form OPEN (64d 8) — Jobs draws the property's local time |
| X9 | PASS | T | no refusal names a person or a role |
| X10 | PASS | SRC | no failure sentence outside `chrome/failure.ts` and `widgets/failed.ts` |
| X11 | PASS | T | the model state names no *you* and no *account* |
| X12 | PASS | T · 6 widgets × 6 causes | every class matched to a rule that APPLIES in the widget's own sheet; no facts on a card |
| X13 | OPEN | record | the card is content-height; the frame height is the host's |
| X14 | OPEN | record | every widget's *Open* lands on `jobs:board`, which draws the failed read's facts at screen size when its own read fails |
| X15 | OPEN | record | one partial placement: the Board's figures strip failing inside a board that loaded (captured for 64d) |

### §8 · Drawings and harness

| ID | Result | Surfaces · states | Evidence |
|---|---|---|---|
| H1 | PASS | T | the harness injects exactly the published set (`tokens.test.ts`) |
| H2 | PASS | C · the paged frames | the Board frame draws a full page (*1–12 of 47*); no paged frame draws fewer rows than its count |
| H3 | **FAIL → fixed by redline 6** (`c7ebc945`) | frames | mockups 01 and 02 draw 65 dates and name no locale for them. Revision in `06-the-frame-revision.html`: every date through the SDK in en-GB · Asia/Qatar, labelled; four dates carried 2025's weekday; two shapes no SDK style draws (D1, D2) are the owner's |
| H4 | PASS | T | the drive withholds *ready* on a missed step (`frame.ts`) |
| H5 | PASS | T | the harness rejects a route it does not answer; the screen draws the failure, never a stand-in |
| H6 | **FAIL → fixed by redline 6** (`c7ebc945`) | frames | both frames' `:root` declare 13 names of their own beside the published 19 (`--bad --bg --cyan --defer --dim --faint --glass --grad --indigo --line --ok --text --violet --warn`) |

## Routed, not fixed — each needs a decision that is not Jobs'

1. **N5's department** — waits on ADR 0203 (BB's Context change). Drawn as *not established*.
2. **O7's count — raised and ruled.** Two §9 deviations in one application, and Room Care's L1 a
   third on the same day, were APPS-Q27's *"repeated or intentional deviations trigger a
   standards-amendment question"*. The architect raised it as APPS-Q53; the planner amended §9
   (`8726f39a`, page 64 at `045402b4`), and the two are no longer deviations. The seven conditions
   are audited below, one row each.
3. **H3, H6** — the frame revision, drawn under APPS-Q44 for the owner:
   `jobs/docs/mockups/06-the-frame-revision.html`.


### O7 against §9's seven conditions (APPS-Q53) — one row each

Read from the code at `15373799`; a ✓ is what the code does, not what a person was seen to do (the live walk records that).

| Condition | New item (`catalogue/index.ts`) | Add a step (`policies.ts`) |
|---|---|---|
| 1 · creates or edits an item of the current list | ✓ — an item of the catalogue it sits beside | ✓ — a step of the policy it sits under |
| 2 · clearly associated with the list | ✓ — under the item detail, in the catalogue's column | ✓ — directly under the P1 ladder it adds to |
| 3 · explicit submit and cancel | ✓ — Create item and Cancel (Cancel added 2026-09-19, shown failing first; it closes on nothing else) | ✗ — Add is off and there is no Cancel |
| 4 · keyboard and focus stay accessible | ✓ — real inputs, a select and two buttons, in tab order; measured by the walk, not by a keyboard session | ✗ — its two fields are drawn as text, so a keyboard reaches nothing |
| 5 · validation and error visible | partly — a missing name or code, and a refused write, are said on the Catalogue's line under the page, not inside the composer; what was typed is kept | — nothing can be submitted, so nothing can be refused |
| 6 · nothing destructive or approval-bearing inside | ✓ | ✓ |
| 7 · same permissions as the full-page action | ✓ — written through `job.curate`, as every item write is | — nothing is written |

**New item meets six outright and 5 in part**: the ruling's own gloss on (5) — *"a refused write keeps the composer open and shows the reason in it"* — puts the reason inside the composer, and Jobs says it on the page's line below. That is a finding for the next build, not fixed here. **Add a step meets none of the conditions that need it to work**, and is built to all seven when it is built.

## About the checklist itself — for GG and the architect

1. **U1's check under-reaches its rule.** It looks for `toLocaleString`, `Intl.NumberFormat` and
   `toFixed`; the rule is *every* user-facing number. Jobs passed the check while writing ~40
   displayed numbers with `String(n)`. The same pattern will pass in every app.
2. **C9's check misfires on its rule.** *"A control's computed line-height equals its parent's"*
   fails every control whose font is smaller than its parent's under a unitless line-height — the
   control correctly inherits the *factor* (13px × 1.5 = 19.5px against the row's 21px). The rule
   is about the UA resetting to `normal`; the check should compare factors.
3. **G9's wording.** *"The pager is the table's next sibling — what a wrapper would silently
   break"* predates CORE-Q28, whose own snippet makes the list a `.tbl` — for Jobs a wrapper round
   a `<table>`, since a table cannot scroll. Read as "the list's next sibling", Jobs passes.

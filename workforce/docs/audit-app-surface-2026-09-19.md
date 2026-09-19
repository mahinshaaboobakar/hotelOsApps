# Workforce against the app surface checklist — 2026-09-19

**Audited against** `docs/app-surface-checklist.md` at `be67a19` (derived from
page 64 at HosPilotOS `2da088bd`, corrected at `20b480cf`). **Built from**
HotelOsApps `020f1ef` + this record's harness change, with `@hotelos/sdk` at
HosPilotOS `ff7926fb` (contract v2). Audited by GG.

## Method

* **S — source.** A walk over the 76 production files of `workforce/ui`
  (tests and preview excluded). Each detector reports its population, and the
  one that compares fallback literals was shown to catch a wrong value before
  its zero was believed: its first version compared *spellings* and reported 45
  differences that were one colour written two ways.
* **M — measured.** The harness (`preview/frame.html`) served on
  `127.0.0.1` with an explicit root, identity confirmed by a per-run marker file
  only that root held, and each page refused unless its `<title>` was
  *Workforce module realm* and it reached `data-review-ready`. Computed values
  read from the rendered DOM at 1440×900.
* **T — tests.** Cited only where the test asserts the line's claim.

**States reached.** People in all five list states — full page (25 of 42),
short page (4 of 42), single page (4 of 4), short last page (17: 26–42 of 42),
empty page of a non-empty list — plus the empty list (the first run). **The
harness could not reach three of those five** before this audit; `single`,
`last` and `barren` are added to `preview/frame.ts` in the same commit. Rota in
every one of the six failure causes. The shift, leave, duty, team and
end-posting overlays open.

**Not measured, and said so.** A line marked `—` was not measured in this
audit. It is not a pass.

## Results

`PASS` · `FAIL` · `OPEN` (the rule is unsettled; what is built is recorded) ·
`Q` (a finding the standard does not decide — a question) · `—` (not measured)

| ID | Result | Evidence |
|---|---|---|
| P1 | PASS | S — 326 `var()` uses; every name published or declared by the app |
| P2 | PASS | S — 286 fallbacks on published tokens, 0 differ from `styles.css`'s dark value (compared by value; detector shown to catch `#6b7cff` against `#818cf8`) |
| P3 | **FAIL** | S — five hand-mixed **13%** tints that are the published `--color-*-soft` (12%, `styles.css:85`): `chrome/styles.ts:342, 360, 362` · `screens/duty/styles.ts:19` · `screens/leave/styles.ts:33` |
| P4 | **Q** | S — 13 literals, all in `screens/printed/styles.ts`, deliberately: *"for a monochrome photocopier … a themed print sheet would come out of the machine as grey on grey"* (`:12-14`). §1 has no print exception — a design decision the standard never authorised, the `APPS-Q35` shape |
| P5 | — | |
| P6 | PASS | S — the one `box-shadow` is an inset brand ring, not a shadow; M — the overlay shadow is `--color-surface` at 72% |
| C1 | PASS | M — `1px` `line-strong`, radius 8, `7px 14px`, 13px, `--color-ink`, no fill (Policy, shift, leave, end) |
| C2 | PASS | M — `linear-gradient(135deg, …)` whose second stop computes to `srgb(.683 .509 .771)` = brand 62% + bad 38%; `ink-on-accent`, 600, radius 8 |
| C3 | PASS | M — duty and team sheets draw their primary `.btn.off`, `disabled`, until there is something to send |
| C4 | — | no `.btn.sm` on a reached screen |
| C5 | — | no inline `.btn.danger` on a reached screen |
| C6 | PASS | M — *End posting*: `--color-bad` fill, `ink-on-accent`, border transparent, 600 |
| C7 | PASS | S — `.btn.go` appears only inside a comment (`chrome/styles.ts:57`) |
| C8 | **FAIL** | M — `.btn` is a **`<div>`** on Policy (*＋ New shift*) and in the shift and leave sheets (*Cancel*, *Create shift*, *Raise request*); they act on click and are not focusable or announced. The rule names rows; its reason applies unchanged. People's rows are `button.row` and pass |
| C9 | PASS | S — `font:inherit;line-height:inherit` on `.btn` (`chrome/styles.ts:268`) and inputs (`:472`); M — control and parent both 1.55× their size |
| C10 | PASS | S — the filled danger control is defined once, `chrome/styles.ts:292` |
| C11 | **FAIL** | M + S — *Create shift* and *Raise request* are **live** `btn pri` over fields that accept nothing; both files say so (`policy/dialog.ts:140`, `leave/form.ts:141`). §2: *"drawn `off`, with the reason beside it — never live-and-refusing."* Duty and Teams do it right |
| N1 | PASS | M — bar 56px, `0 22px`, 1px `--color-line` rule; active tab 2px `--color-brand` |
| N2 | PASS | M — the bar spans the top and the body takes the full 1440px; no rail |
| N3–N7 | — | captures not taken in this audit |
| N8 | PASS | M — body padded `14px 26px 22px 26px` |
| L1 | PASS | M — no fill, border or radius around the list |
| L2 | PASS | M — 11px, 500, uppercase, 0.88px (.08em), `ink-faint`, centred, ruled |
| L3 | PASS | M — 13px, `align-items:flex-start`, ruled (flex grid, §4's own spelling) |
| L4 | — | no selected row reached |
| L5 | PASS | M — the last row keeps a 1px rule |
| L6 | PASS | S — `6px 10px` with its reason at the site (`chrome/styles.ts:309-312`) |
| D1 | PASS | see G6 |
| D2 | **FAIL** | M — table header .08em passes; **field label measures .04em (0.44px at 11px), §10 rules .07em** — shift, leave, end |
| D3 | PASS | M — note text 12px / 19.8px, `ink-muted` |
| D4 | PASS | M — quiet text `ink-muted` |
| D5 | OPEN | `64c`; the built weight is not recorded yet |
| G1 | PASS | S — People pages on `page`/`pageSize` → `total` (`backend/src/Module/Views/PeopleView.cs:44`) |
| G2 | PASS | S — `chrome/pager.ts` renders from `pagedView` and `PAGER_LABELS` |
| G3 | PASS | M — single page: *Showing 1–4 of 4 ‹ 1 ›*, both arrows disabled |
| G4 | PASS | M — short last page: *Showing 26–42 of 42*, `›` disabled |
| G5 | **FAIL** | M — the empty page says *No rows on this page · 42 in the list*, right — **and draws the first-run panel "Post your first staff member" over a list of 42 people.** `screens/people/index.ts:64` decides first run on the rows of this page (`postings.length === 0`), not on the list's total |
| G6 | PASS | M — full page: body 747/747 (no page scroll), list 1358/582 scrolling itself, pager `static`; same in short, single, last |
| G7 | **FAIL** in `E1` | M — pager at the floor (900 = body bottom) in full, short, single and last, and the list measurably grew (short: box to 749, last row at 413). **In the empty page the pager sits at 645**, under the first-run panel |
| G8 | — | |
| G9 | PASS, note | M — the pager follows `.panel` (the ownership note), not the list; `.rows:has(~ .pager)` is a general-sibling rule and holds. The standard's *"next sibling"* wording is narrower than its selector |
| G10 | PASS | S |
| G11 | OPEN | `64f` — the empty list draws the first-run panel and **no pager** (`chrome/pager.ts:76`) |
| O1 | **FAIL** | M — the confirm dialog is **440px** (§9: 520) at radius 16 (radius-panel + 2px = 18); no overlay uses a shared head/body/foot (`.dh .db .df`) — children are `div`, `.fld`…, `.acts` |
| O2 | PASS | M — scrim `position:absolute` |
| O3 | PASS | M — scrim is a child of `.main`, outside `.body` |
| O4 | partial | T — `add-member`, `assign-duty`, `form-team` tests; shift and leave untested |
| O5 | — | |
| O6 | PASS | M — styling per C6; the count is not plural here |
| O7 | **FAIL** | M — the shift and leave forms are **composing** and are drawn as a **centred** 440px box, not a sheet from the right at full height |
| F1 | **FAIL** | S — the fields are `div`s, which is right while nothing accepts them — **but they render fixture values as if the desk had chosen them**: the leave form shows *"Brother's wedding — travelling on the 13th."* on a real property. §10: *"A field renders a value the desk has already chosen"* — nobody chose these |
| F2 | **FAIL** | M — label 11px uppercase `ink-faint` passes but tracking .04em (see D2); **`.inp` is `7px 11px`, radius 8, on `--color-surface`** where §10 rules `9px 12px`, radius 10, ink at 2% — the input box the owner ruled to the written standard (`APPS-Q27`) |
| F3 | — | |
| I1 | PASS | S — no `Intl.DateTimeFormat`, `toLocale*String`, or display use of `Date`; T — `iso-never-rendered.test.ts` |
| I2 | partial | T — `policy.test.ts:47` asserts `—` for an unset value |
| I3 | PASS | S — no machine time on a screen |
| I4 | — | fixtures carry `locale: null`; no test asserts the ISO/UTC rendering |
| I5 | PASS | S |
| I6 | — | |
| U1 | PASS | S — the only `toFixed` is CSS geometry (`screens/rota/ribbon.ts:49-50`) |
| U2 | — | |
| X1 | PASS | M — all six causes draw the failure and no rows |
| X2 | PASS | M — `.fail-body` grid, `place-items:center`; the block 560px wide |
| X3 | PASS | M — inline `svg`, `stroke="currentColor"`; amber unanswered · grey forbidden, unadmitted, ungranted · red undecidable, faulted |
| X4 | PASS | M — 11px, 1.1px (.1em), uppercase, `ink-faint`; the mono stack is `64d` item 4, OPEN |
| X5 | PASS | M — 19px, 600 |
| X6 | PASS | M — 14px `ink-muted`; the emphasised run bold in `unadmitted`, `undecidable`, `faulted` |
| X7 | PASS | M — *Try again* for unanswered only; *Copy these details* for undecidable and faulted; no button on a refusal. Fill and *needs…* size are `64d`, OPEN |
| X8 | PASS | M — a `dl`: *Asked for · Answer · At*; the permission bold. The moment's form is `64d` item 8, OPEN |
| X9 | PASS | M — every refusal names what is missing; no name, no role |
| X10 | PASS | S — no failure sentence written by the app |
| X11 | PASS | M — *This application was not granted…*, *This account has not been granted…*; the model's headline has no *you*/*account* |
| X12 | PASS | T — `widget-sheet.test.ts`: every widget in every one of six causes, every class in the sheet the widget mounts |
| X13–X15 | OPEN | `64d` items 3, 5, 6/7 |
| H1 | PASS | T — `tokens.test.ts:102`, set equality with `TOKEN_NAMES` |
| H2, H3, H6 | — | frames not reviewed in this audit |
| H4 | partial | the drive asserts each click reached its screen; three list states added here |
| H5 | — | |

## The count

Derived from the table above by parsing it, not written down separately — and
checked against the checklist's own IDs: all 86 present, none extra.

```text
86 lines = PASS 48 + FAIL 10 + OPEN 5 + Q 1 + partial 3 + not measured 19
```

Four further OPEN items sit inside passing lines (X4's stack, X7's fill and
*needs…* size, X8's moment) and are not separately counted. *(`N3–N7`,
`X13–X15` and `H2, H3, H6` are single rows standing for 5, 3 and 3 lines.)*

**This count was first typed, not derived, and was wrong on four of its six
figures** (45 · 11 · 4 · 20). Parsing the table is what found it.

## What fails, in order of what a person meets

1. **G5 / G7 — page 3 of a 42-person list says nobody is posted.** The first run
   is keyed on the page's rows, not the list's total.
2. **C11 / F1 — two forms offer a live primary over invented values.** Leave and
   shift show fixture text as if entered, under a button that looks like it
   saves.
3. **C8 — controls that are `div`s** on Policy and in two sheets.
4. **O1 / O7 — composing forms drawn as centred boxes**, and the confirm dialog
   at 440, not 520, with no shared head/body/foot.
5. **F2 / D2 — the field box and its label** are not §10's.
6. **P3 — five hand-mixed tints** where the published soft tones exist.

## Questions this raises — not decided here

* **P4:** a printed sheet is not a themed surface. Does §1 get a print exception,
  or does print draw on tokens?
* **G9:** §6's text says the pager is the table's *next* sibling; its selector
  says *a later* sibling. Which is meant?

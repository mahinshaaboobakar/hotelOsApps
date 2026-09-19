# Room Care — the page-64 audit

Run 2026-09-19 by KK against GG's checklist (`docs/app-surface-checklist.md`,
`3d521cef`, 86 lines), every screen and widget, in every state the checklist
names for it.

**A harness run, and every row says so.** No state below was reached on a real
Kernel. *Corrected: this said Room Care was installed on no Kernel. 0.1.2 had
been installed earlier today, and I did not know it, so the audit's real-Kernel
states were never attempted. It stays a harness run, and Part B on 0.1.3 is
the real-Kernel evidence.* The list states (E0, E1, 1P, MP, ML) are **derived from the
recorded Coral Cove morning**: rows repeated to fill a page, and `total` stated. The
harness stamps each derived capture. Failed reads are produced through the
host's own six error kinds, so the SDK's `load` classifies them.

| | commit | provenance |
|---|---|---|
| **before** | `d98785f4` (the audit's tooling, nothing fixed) | clean · 230 cases · 0 unreached |
| **after** | `020f1eff`, all of it committed (the manifest bump `16bdeed` comes after and changes no bundle) | clean · 249 cases · 0 unreached |

## How each line was checked

- **Measured** (`preview/audit/probe.js`): computed styles and boxes on the
  rendered page in headless Edge, one screenshot per case. Placement (§6) is
  measured, never inferred from CSS text.
- **Source walk** (`preview/audit/source.mjs`): the `S` lines, over every
  shipped `.ts` and the backend's projections. It reads the token set and the
  shell's values from HosPilotOS at HEAD. Each detector was run against
  `d98785f4`, the broken state, before its clean result was trusted. G1's
  first pattern missed the very fault it was written for and was corrected.
- **Tests and reads** (`preview/audit/judged.mjs`): the lines no probe can
  measure, each with its instrument named.
- **Cases**: 19 screens × (loaded, no locale, six causes); Setup's own read ×
  six causes; five paged lists × five list states; the widgets loaded and in
  six causes; 19 overlays × (open, filled, write refused, pressed empty).
  **Five states are excluded by reason**: the wall, grid and compact views'
  failed reads (their switcher exists only once the read answers, so the
  failure is the one audited on the map and the sheet), and two overlays
  whose opening row is not in the recording.

**Room Care's roles, for the lines that name a role** (§5: *found by the
rule that governs them*): table header `th` · field label `label.lbl` ·
section label `.sect`, `.card.accent > h3` · note text `.note`, `.legend`,
the timeline's detail line · quiet text the timeline's date and basis line
(the ruled case). 64b's own `.fail-*` classes are §13's.

## The table — every line × every surface, after

`P` pass · `**F**` fail · `O` open (built and recorded, never failed) · `–` not applicable, with
its reason in `results.json` · `·` not asked of that surface in any state. A cell is the
worst verdict any state gave it. Screen columns include their overlays. `setup` is Setup's own
read failing; `wdg` the five widgets; `src` the source walk; `frm` the two locked frames.
Lines asked of no surface: **none**.

| Line | map | wall | room | insp | prep | sheet | grid | cmpct | sup | deep | mine | door | win | svc | rules | zones | areas | plan | access | setup | wdg | src | frm |
|---|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|:-:|
| P1 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| P2 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| P3 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| P4 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| P5 | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · | · |
| P6 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| C1 | P | · | P | · | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| C2 | P | · | P | · | P | P | · | · | P | P | P | P | P | P | P | P | P | P | P | P | · | P | · |
| C3 | · | · | P | · | P | P | P | P | · | P | · | P | P | P | P | P | P | P | P | · | · | · | · |
| C4 | P | P | P | · | P | **F** | **F** | P | P | P | P | P | P | O | O | O | P | P | P | P | · | · | · |
| C5 | · | · | · | · | · | · | · | · | · | P | · | · | · | · | · | · | · | · | P | · | · | · | · |
| C6 | · | · | · | · | · | · | · | · | · | P | · | · | · | · | · | · | · | · | P | · | · | · | · |
| C7 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| C8 | P | **F** | P | · | **F** | **F** | P | **F** | **F** | P | **F** | P | P | **F** | P | P | P | P | P | P | P | · | · |
| C9 | P | P | P | · | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · |
| C10 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| C11 | · | · | P | · | P | P | P | P | P | P | · | P | P | P | P | P | P | P | P | · | · | · | · |
| N1 | P | P | P | · | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| N2 | P | P | P | · | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| N3 | P | P | P | · | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| N4 | P | P | P | · | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| N5 | P | P | P | · | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| N6 | P | P | P | · | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| N7 | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · | · |
| N8 | P | P | P | · | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| L1 | · | · | · | · | **F** | · | · | · | P | P | P | · | · | · | · | · | P | · | · | · | · | · | · |
| L2 | · | · | · | · | P | · | · | · | P | P | P | · | · | · | · | · | P | · | · | · | · | · | · |
| L3 | · | · | · | · | P | · | · | · | P | P | P | · | · | · | · | · | P | · | · | · | · | · | · |
| L4 | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · | · | · | · | · | · | · | · | · |
| L5 | · | · | · | · | P | · | · | · | P | P | P | · | · | · | · | · | P | · | · | · | · | · | · |
| L6 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| D1 | · | · | · | · | – | · | · | · | P | P | P | · | · | · | · | · | P | · | · | · | · | · | · |
| D2 | · | P | P | · | P | P | · | P | P | P | P | P | P | P | · | P | P | P | P | · | · | · | · |
| D3 | P | P | P | · | · | · | P | P | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · |
| D4 | · | · | P | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · |
| D5 | O | · | O | · | O | O | · | · | O | O | O | O | O | O | O | O | O | O | O | O | · | · | · |
| G1 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| G2 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| G3 | · | · | · | · | P | · | · | · | P | P | P | · | · | · | · | · | P | · | · | · | · | · | · |
| G4 | · | · | · | · | P | · | · | · | P | P | P | · | · | · | · | · | P | · | · | · | · | · | · |
| G5 | · | · | · | · | P | · | · | · | P | P | P | · | · | · | · | · | P | · | · | · | · | · | · |
| G6 | · | · | · | · | – | · | · | · | P | P | P | · | · | · | · | · | P | · | · | · | · | · | · |
| G7 | · | · | · | · | – | · | · | · | P | P | P | · | · | · | · | · | P | · | · | · | · | · | · |
| G8 | P | P | P | · | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · | · |
| G9 | · | · | · | · | P | · | · | · | P | P | P | · | · | · | · | · | P | · | · | · | · | · | · |
| G10 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| G11 | · | · | · | · | O | · | · | · | O | O | O | · | · | · | · | · | O | · | · | · | · | · | · |
| O1 | · | · | P | · | P | · | · | · | P | P | · | P | · | P | P | P | P | · | P | · | · | · | · |
| O2 | · | · | P | · | P | · | · | · | P | P | · | P | · | P | P | P | P | · | P | · | · | · | · |
| O3 | · | · | P | · | P | · | · | · | P | P | · | P | · | P | P | P | P | · | P | · | · | · | · |
| O4 | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · | · |
| O5 | · | · | P | · | P | · | · | · | P | P | · | P | · | – | – | – | – | · | P | · | · | · | · |
| O6 | – | – | – | – | – | – | – | – | – | – | – | – | – | – | – | – | – | – | – | · | · | · | · |
| O7 | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · | · |
| F1 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| F2 | · | · | P | · | · | · | · | · | P | P | · | P | · | · | · | P | P | · | P | · | · | · | · |
| F3 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| I1 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| I2 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| I3 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| I4 | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · | · |
| I5 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| I6 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| U1 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| U2 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| X1 | P | · | P | · | P | P | · | · | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| X2 | P | · | P | · | P | P | · | · | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| X3 | P | · | P | · | P | P | · | · | P | P | P | P | P | P | P | P | P | P | P | P | P | · | · |
| X4 | O | · | O | · | O | O | · | · | O | O | O | O | O | O | O | O | O | O | O | O | · | · | · |
| X5 | P | · | P | · | P | P | · | · | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| X6 | P | · | P | · | P | P | · | · | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| X7 | O | · | O | · | O | O | · | · | O | O | O | O | O | O | O | O | O | O | O | O | · | · | · |
| X8 | O | · | O | · | O | O | · | · | O | O | O | O | O | O | O | O | O | O | O | O | · | · | · |
| X9 | P | · | P | · | P | P | · | · | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| X10 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| X11 | P | · | P | · | P | P | · | · | P | P | P | P | P | P | P | P | P | P | P | P | · | · | · |
| X12 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · | · |
| X13 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | O | · | · |
| X14 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | O | · | · |
| X15 | O | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | O | · |
| H1 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| H2 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P |
| H3 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | **F** |
| H4 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | **F** |
| H5 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | P | · |
| H6 | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | · | **F** |

## Each fix, and the check that failed before it

| Line | Failed at `d98785f4` | Fix | Shown failing first by |
|---|---|---|---|
| C2 | the primary fill written twice, no `--accent` | `adea750` `--accent` on `.rc`, derived | source walk at `d98785f4`: "2 135deg fill(s); 0 --accent" |
| C4 | "‹ My rooms", a loose `.btn.sm` above the door's failure | `6655c01` a full `.btn` | the audit, 6 cases (`at-the-door--f6-*`) |
| C8 | widget rows that open nothing showed the pointer | `adea750` only `button.wrow` | the audit, `widgets--w` |
| C9 | inputs and toggles at `line-height: normal` | `adea750` `input,select,textarea{font:inherit;line-height:inherit}` | the audit, 8 screens |
| C11 | Setup's Save live with nothing edited; ten overlay primaries live with nothing to send | `2636e33` `saveLine` watches its fields; `readyWhen` | `tests/save.test.ts` and `tests/ready.test.ts` fail first; the audit's pressed-empty cases |
| D2 | section labels at `.12em` | `adea750` `.04em` (APPS-Q33) | the audit, room page and Property-wide access |
| D3 | legends at 11px, the timeline's detail at 18px line-height | `adea750` 12px / 19.8px (APPS-Q35, 64a) | the audit, 5 surfaces |
| D4 | the timeline's date line in ink-faint | `adea750` ink-muted (64a) | the audit, the room pages |
| G1 | five projections computed their own `Skip(page * size)` | `fbb8423` `Paging.Of` | source walk at `d98785f4`: "5 computing their own" |
| G8 | `.body:has(.pager)` clipped 296px of Prepare | `adea750` `.body:has(> .pager)` | the audit, `prepare--mp` |
| L6 | three tight rows with no reason | `adea750` the reason on the line above each | source walk at `d98785f4` |
| O4 | nothing held it | `982c801` `tests/overlay.test.ts` | the test fails with the scrim check removed |
| U1 | 21 numbers through `String()`, none through `formatNumber` | `6655c01` `whole(host, n)` everywhere | `tests/number.test.ts` failed first ("expected [ '1234', '5678' ] to deeply equal [ '1.234', '5.678' ]") |

## What still fails, and why Room Care alone cannot close it

**The table is not clean, so nothing here goes to the owner as done.** Every
remaining FAIL needs a ruling or an owner's approval, and each is stated as
the question it is:

1. **C8, table rows (7 cells).** §2: *"A row that opens something is a real
   `<button>`"*. §4: *"The list is a table"*. A `<tr>` cannot be a button, and
   Jobs, the §4 baseline, opens its rows the same way (`tr.pick`). Following
   APPS-Q43's precedent (*a guard must not decide a disagreement by failing*),
   this is a question about the standard, not seven Room Care defects: **does
   §2's row rule govern a table row, and if it does, what should a table row
   that opens something be?** It is with the planner as **APPS-Q50**, and the rows stay as they are.
2. **C4, small buttons outside a row or a card (2 cells).** "Apply to
   selected" on the states sheet's toolbar and "Select all N" on the grid's
   zone rows are drawn `.btn.sm` in the locked frames 4c and 4d. §2 says
   *"inside a row or a card"*. Whether a toolbar or a group-header row is a
   *row* is APPS-Q43's ambiguity on the row side. Its card half is already
   with the owner.
3. **L1, Prepare's list in a card.** §4: *"A list sits bare on the page"*.
   Prepare's changes list sits in a card in locked frame 2, and the build
   follows the frame. Under APPS-Q27's hierarchy a locked, certified drawing
   can be a surface-specific deviation, and this is Room Care's first
   instance of this one. **It is the owner's: keep the card (a deviation,
   labelled where it diverges), or draw the list bare.**
4. **H3, H4, H6, the frames.** They name no locale (H3). They draw divs where
   the build uses buttons (H4, ARCH-Q20). They declare one literal of their
   own, `--glass` (H6). The build's moves this round (section labels at
   `.04em`, note text at 12/19.8, the timeline in ink-muted, Save and the
   overlay primaries drawn off with nothing to send) put the frames behind
   the build in those places too. Under APPS-Q44 a revision to a locked frame
   is drawn by the stream and approved by the owner. **KK draws the batch;
   the owner approves it.**

## OPEN — what is built, recorded and not failed — 76 cell(s)

- C4 · Setup › Services & minutes — card half (APPS-Q43): Reorder… · Add a phase · Copy… — .btn.sm inside a card, not a row; card half (APPS-Q43): Reorder… · Add a phase · Copy… · ↑ · ↓ — .btn.sm inside a card, not a row
- C4 · Setup › Rules — card half (APPS-Q43): Reorder… — .btn.sm inside a card, not a row; card half (APPS-Q43): Reorder… · ↑ · ↓ — .btn.sm inside a card, not a row
- C4 · Setup › Assignment & zones — card half (APPS-Q43): Move rooms between zones… — .btn.sm inside a card, not a row
- X4 · Board · map — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · Board · map — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · Board · map — the moment built as "19 Sept, 13:37" (64d item 8)
- D5 · Board · map — emphasis in a note built at 700 (64c)
- X4 · A room — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · A room — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · A room — the moment built as "19 Sept, 13:37" (64d item 8)
- D5 · A room — emphasis in a note built at 700 (64c)
- X4 · Prepare — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · Prepare — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · Prepare — the moment built as "19 Sept, 13:37" (64d item 8)
- D5 · Prepare — emphasis in a note built at 700 (64c)
- X4 · Room states · sheet — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · Room states · sheet — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · Room states · sheet — the moment built as "19 Sept, 13:37" (64d item 8)
- D5 · Room states · sheet — emphasis in a note built at 700 (64c)
- X4 · Supervision — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · Supervision — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · Supervision — the moment built as "19 Sept, 13:37" (64d item 8)
- D5 · Supervision — emphasis in a note built at 700 (64c)
- X4 · Deep clean — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · Deep clean — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · Deep clean — the moment built as "19 Sept, 13:37" (64d item 8)
- D5 · Deep clean — emphasis in a note built at 700 (64c)
- X4 · My rooms — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · My rooms — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · My rooms — the moment built as "19 Sept, 13:37" (64d item 8)
- D5 · My rooms — emphasis in a note built at 700 (64c)
- X4 · At the door — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · At the door — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · At the door — the moment built as "19 Sept, 13:37" (64d item 8)
- D5 · At the door — emphasis in a note built at 700 (64c)
- X4 · Setup › Windows & trigger — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · Setup › Windows & trigger — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · Setup › Windows & trigger — the moment built as "19 Sept, 13:37" (64d item 8)
- D5 · Setup › Windows & trigger — emphasis in a note built at 700 (64c)
- X4 · Setup › Services & minutes — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · Setup › Services & minutes — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · Setup › Services & minutes — the moment built as "19 Sept, 13:37" (64d item 8)
- D5 · Setup › Services & minutes — emphasis in a note built at 700 (64c)
- X4 · Setup › Rules — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · Setup › Rules — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · Setup › Rules — the moment built as "19 Sept, 13:37" (64d item 8)
- D5 · Setup › Rules — emphasis in a note built at 700 (64c)
- X4 · Setup › Assignment & zones — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · Setup › Assignment & zones — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · Setup › Assignment & zones — the moment built as "19 Sept, 13:37" (64d item 8); the moment built as "19 Sept, 13:38" (64d item 8)
- D5 · Setup › Assignment & zones — emphasis in a note built at 700 (64c)
- X4 · Setup › Areas — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · Setup › Areas — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · Setup › Areas — the moment built as "19 Sept, 13:38" (64d item 8)
- D5 · Setup › Areas — emphasis in a note built at 700 (64c)
- X4 · Setup › Deep clean plan — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · Setup › Deep clean plan — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · Setup › Deep clean plan — the moment built as "19 Sept, 13:38" (64d item 8)
- D5 · Setup › Deep clean plan — emphasis in a note built at 700 (64c)
- X4 · Setup › Property-wide access — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · Setup › Property-wide access — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · Setup › Property-wide access — the moment built as "19 Sept, 13:38" (64d item 8)
- D5 · Setup › Property-wide access — emphasis in a note built at 700 (64c)
- X4 · Setup (its own read) — mono stack built as ui-monospace, "Cascadia Mono", Menlo, mo (64d item 4)
- X7 · Setup (its own read) — button fill built as btn pri (64d item 1); button fill built as btn (64d item 1)
- X8 · Setup (its own read) — the moment built as "19 Sept, 13:38" (64d item 8)
- D5 · Setup (its own read) — emphasis in a note built at 700 (64c)
- X15 · Board · map — the bar's own read failed; the identity slot draws "No answer yet · who is signed in"
- G11 · Deep clean — E0 draws "no deep cleans due or under way" in the pager at the list's floor, page button 1 disabled
- G11 · My rooms — E0 draws "no rooms assigned to you today" in the pager at the list's floor, page button 1 disabled
- G11 · Prepare — E0 draws "no changes since the last press" in the pager at the list's floor, page button 1 disabled
- G11 · Setup › Areas — E0 draws "no public areas" in the pager at the list's floor, page button 1 disabled
- G11 · Supervision — E0 draws "no rooms in the lane" in the pager at the list's floor, page button 1 disabled
- X13 · Widgets — the widget card measures 760px tall in this state
- X14 · Widgets — a failed widget's onward control reads "Try again →"; Try again re-reads in place, and Open Room Care opens the board (three widgets), Prepare or Supervision — each draws its own read there, not the widget's failed one; a failed widget's onward control reads "Open Room Care →"; Try again re-reads in place, and Open Room Care opens the board (three widgets), Prepare or Supervision — each draws its own read there, not the widget's failed one
- X15 · Source — Partial failures Room Care draws (64d items 6, 7): the bar's own read failing draws in the identity slot (board-map--f6-me); Move rooms and Reassign draw the whole failure inside the sheet when the rooms or the people cannot be read; Copy refuses in place with the SDK's sentence for a room type it could not read

## Found by the audit, and not lines of the checklist

- **A write that did not succeed claimed "nothing was changed", even when
  nobody answered** (`2535c4a`). The audit pressed every overlay's action
  against a host that answers nothing. A non-answer may have landed, so the
  sentence invited a person to assign a room twice. This is CLAUDE.md's
  false-claim-about-a-write class. It now says the outcome is not known.
  Refusals, which were decided before anything ran, keep "nothing was
  changed".
- **Recording fixtures move when the backend suite runs.** The suite's
  DriveRecording test rewrote all 19 `preview/recorded/*.json` with new
  UUIDs (values identical) during this round's backend run. They were
  restored from HEAD so the audit and Part A evidence stay comparable.

## For GG and the checklist

- **C9 cannot be met by a `<select>` in Chromium.** A select's line-height
  computes `normal` whatever is declared. An explicit `line-height:21px`
  measures `normal` (measured 2026-09-19). Recorded N/A, with that
  measurement, on every select.
- **G6 and G7 on a paged list inside a card.** Prepare's changes list pages
  inside a card among other cards. §6 rules the screen's list, and a card is
  sized by its content, so neither line can hold there. Recorded N/A with
  that reason. The page scrolls as a page, which G8 then measures. (G8
  caught the real defect: `.body:has(.pager)` had clipped 296px of Prepare.)
- **D4's classification.** Page 64 says itself that nobody has classified the
  432 ink-faint uses. Room Care fails D4 only on the ruled case (the
  timeline's date and basis line). The other faint text a person reads (the
  bar's identity, the pager's range, legends, `.dim` cells) is listed per
  capture in `results.json` (`record.faint`), for that classification when it
  comes.
- **I1 and a length of work.** `formatDuration` renders an elapsed clock
  (`HH:MM:SS`). Room Care's `minutes()` renders a configured length ("45 min",
  "1 h 05"), a different value. It stays Room Care's, and its digits are now
  the property's.
- **A preselected choice.** Record an exception opens with its first option
  (DND board) already chosen, so it is a complete act and C11 passes. But the
  sheet answers for the person, the preselection question GG raised on
  2026-09-10. It isn't a checklist line.
- **G11, the zero-row list.** 64f was revised today (`a0eb420d`): *every
  option shows an explicit empty message*. Room Care's E0 already draws one
  in the pager's place ("no deep cleans due or under way"). The line stays
  OPEN until 64f is ruled.

## 0.1.3, cut from a clean HEAD export

Built from **detached worktrees of both repositories**, side by side, so every relative path resolves
inside them and none reaches a shared tree: **HotelOsApps `16bdeed`** and **HosPilotOS `ff7926fb`**.
`dotnet publish` compiled `HotelOS.Common` and `HotelOS.Platform` from the HosPilotOS worktree. The UI
was built there too, with `node_modules` by junction. Both worktrees were removed afterwards, junction
first.

```text
roomcare-0.1.3.hopkg   16,729,943 bytes   sha256 c3485aa282bbbab409bdb2258b9cc9e3a31a0ea80075b805b496e81e5e54c634
signed by              dev-local — verifies under the installation's dev-local anchor, not the other; one flipped byte fails
inside the archive     52 entries declared, 52 in the payload, 0 findings; manifest roomcare 0.1.3
UI bundles             module.js and the five widgets are byte-for-byte what the certificate run (020f1eff) measured
```

**Staged, 2026-09-19.** Part B certifies 0.1.3, the version that carries the fixes; the owner updates
to it from 0.1.2. *Corrected: this said "not staged — the owner installs 0.1.2 at tonight's restart".
That premise was wrong. 0.1.2 was already installed earlier today.* The table is still not clean, so
the audit is not shown to the owner as done.

## Captures

- `page64-audit/after/`: one per remaining FAIL cell, from the certificate run (10). The three frame
  cells are a review of the drawings and have no screenshot.
- `page64-audit/before/`: one per cell that failed at `d98785f4` (28). The source-walk failures
  (C2, L6, G1, U1) are the walk's own output at `d98785f4`, quoted in the fixes table.
- Every case's JSON and screenshot is reproduced by `node preview/audit/run.mjs` from any commit.

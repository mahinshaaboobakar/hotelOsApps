# Room Care Part A — frame beside capture

Run 2026-09-19 by KK. **Built by the run, from `89777a1`, clean** — the
provenance stamp `ui/.parta/provenance.json` names seven artifacts by digest,
and the five widget bundles and `module.js` in **`roomcare-0.1.2.hopkg`**
(sha256 `7b7b6ec0…fdc00a6f`) are those bytes, checked from inside the archive.

**Re-run after the owner chose the second set** (19f203c5, *"we can go with
second"*). The build carries it from `86c504f`. The certificate before this
one, from `71ff149` with 87 named differences, is in this file's history.

## Method

- **The instrument is shared, not private**: `HosPilotOS/scripts/review-measure.mjs`
  sweeps each drawing and each build rendering (Edge, computed styles) and
  compares them; key *tag + normalised text, then document order inside a
  colliding group*, the same on every frame of the run.
- **FF's pipeline, adopted** (`ui/preview/parta/`): identity-verified servers
  on port 0, a sweep retried rather than silently dropped, a frame the harness
  never reached excluded by its own sentence, a partial run refused, columns
  that must close, and an unnamed difference treated as a build error.
- **Every approved frame**: the 21 on the two owner-locked pages, addressed by
  `id="f1a"`…`id="f7g"` — both pages byte-identical to HEAD with the ids
  removed. Frame 1, the paged list, was removed by the owner and has no screen.
- **Each difference is named by a rule**: which side moves, and what decides
  it. Where the standard is silent the class is `adjudicate` and it stays in
  the report.

## Proved to fail before the green was counted

```text
an unknown frame id                     run.mjs exits 2, naming the ids
a run with one comparison missing       classify.mjs exits 2 — "20 of 21 frames compared"
a build style changed (.card h3 → 15px) classify.mjs exits 1 — 2 UNCLASSIFIED on frame 7g
the style restored                      frame 7g re-swept clean from 71ff149, exit 0
the second set, old rules (f7ba0df)     classify.mjs exits 1 — 14 UNCLASSIFIED, all on 7b–7g
```

## Result

```text
drawn 1,657 · built 1,566 · paired 377 (22.8% of drawn) · identical 297 · differing 80
every one of the 80 is named; classify.mjs exits 0; 0 frames unreached
```

| Class | n | Moves | Why |
|---|---|---|---|
| setup-page-first-set | 13 | **adjudicate** | the setup page declares `.mono .tag .pill` once, at the first set; the build carries the owner's second |
| sticky-header-ground | 23 | neither | the wall's header sticks over the scrolling house; same pixels |
| font-shorthand-line-height | 31 | drawing | the frame's `font:` shorthand resets line-height (64 §2's fourth reset) |
| flex-blockified | 2 | neither | a span in a flex row computes block |
| button-type-size | 2 | drawing | 64 §2 fixes `.btn` at 13px; the frame's `font:inherit` makes it 14 |
| selected-row-example | 2 | neither | the frame draws a row selected; nothing is on arrival |
| named, one each | 7 | 2 drawing · 4 adjudicate · 1 neither | in `classify.mjs`, with reasons |

### What the second set closed, and what remains

- **Closed: all 24 `page-redeclares-a-class`**, the screens frames drawn at
  the second set against a build at the first. 20 are now identical. 4 differ
  only in line-height, because the frame's `.mono` is a `font:` shorthand, and
  are counted under that rule (27 → 31).
- **Five of those 24 were never about the redeclared classes.** The rule's
  first clause did not check which other properties differed, so it absorbed
  them. With `.mono` and `.pill` moved they showed, and the build moved to the
  drawing (`f7ba0df`): Prepare's time cell is `.num`, not `.mono` (frame 2);
  the states tables' blocked cell is `.mono`, not `.dim`, and a `td.mono`
  there keeps 11px against `table.wall td` (4c ×2, 4e); the priority pill's
  border is its fill colour (3b). *This certificate's earlier count of 24
  therefore overstated the class question by five.*
- **Remaining: 13 on the setup page, plus one second cause.** The move opened
  them. `02-the-roomcare-setup.html` declares these classes once, at the
  first values, and 01a drew the screens page only, so the owner's choice
  did not address that page. They are `adjudicate` and not the drawing's to
  move by assumption. The build has one sheet: either 02 is redrawn at the
  second set, or Setup keeps the first set under its own scope. 7e's `lobby`
  cell carries this and the selected-row example, so it is named on its own.
- **The other named classes are unchanged**, and so is the paired share,
  because the move changed values, not which nodes pair.

**Moved in the build during the run**, because the drawing was consistent and
the standard silent: the radio's bold at 700, the states tables' 4px rows, the
widget scope at 13px, the *group by* label muted, the card heading's mono
aside at 400 (119 differences became 87). Then, after the owner's choice, the
six classes to the second set (`86c504f`) and the three moves above
(`f7ba0df`): 87 became 80.

## What this does not prove — read before quoting the 22.8%

**The paired share is the finding, not a footnote.** 377 of 1,657 drawn nodes
paired, from 5.9% (the map) to 55.6% (Rules). Two causes, both measured:

- **Element types.** The frames draw tiles, chips and actions as `<div>` and
  `<span>`; the build renders them as `<button>` (frame 1a: drawn `div 94,
  span 33`; built `button 20`). Under a tag-and-text key they cannot pair, so
  the controls a person uses most are the part least measured. ARCH-Q20 (2026-
  09-10) asks frames for measurement to use the build's element types; these
  were drawn on 2026-09-05.
- **Data.** The frames draw The Marina Bay's 250 rooms; the harness renders
  the Coral Cove morning recorded from the real service. Different rooms,
  names and times do not pair by text.

So this certifies the 377 paired nodes and the 87 named differences among
them; about the other 77% of each drawing it says nothing, and does not claim
to. The visual audit of 2026-09-13 (frame beside capture, by eye) covered the
rest and is recorded in chapter 03 §9.

## For the owner, drawn

1. **The screens page contradicted itself. Decided: the second set** (owner,
   2026-09-19, 19f203c5, *"we can go with second"*). The build carries it. **Now
   open: the setup page.** It draws `.mono .tag .pill` at the first set, and 01a
   did not show it. Either 02 is redrawn at the second set, or Setup keeps the
   first in the build under its own scope. That is 13 differences, drawn in
   `setup-page-first-set`. *History: this item said "nine classes" and "the
   build follows the first" until 2026-09-19, and 01a
   (`docs/mockups/01a-the-classes-declared-twice.html`) drew the eleven.*
2. **Measurable frames.** Redrawing the frames in the build's element types
   and the harness's data would take the pairing share toward the
   whole screen. That changes locked drawings, so it is yours to ask for.

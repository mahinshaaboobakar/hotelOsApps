# Room Care Part A — frame beside capture

> **Part A is drawing fidelity only, and is never sign-off** (owner ruling, 2026-09-19). It puts an approved frame
> beside a harness rendering of **recorded fixtures**, not the owner's installed build on the property's real data. It
> says nothing about whether a button works, whether an action is authorized, or how a screen looks with this
> property's data. Those are the frame-versus-live comparison, the capability ledger and Part B, which are the
> evidence. **At this run, Room Care is not installed on the owner's platform** (`platform.packages` holds guestops,
> jobs, openai and workforce), so no live comparison exists yet.

## Run from `07eb133` — the dead controls removed, chip spacing as approved

Run 2026-09-19 by KK against the frames as re-locked at `fff2d96b`, **built by the run from `07eb133`, clean**
(`module.js` `36f0e205…`). This is the next cut's source, not 0.1.3.

```text
drawn 1,657 · built 1,563 · paired 559 (33.7%) · identical 414 · differing 145
every one of the 145 is named; classify.mjs exits 0; 0 frames unreached
```

| Class | n | Moves |
|---|---|---|
| sticky-header-ground | 23 | neither |
| font-shorthand-line-height | 44 | drawing |
| flex-blockified | 20 | neither |
| button-type-size | 21 | drawing |
| selected-row-example | 2 | neither |
| named, one each | 35 | 27 drawing · 5 neither · 3 adjudicate |

**What changed since `28bfdd0a`, and why the counts moved:**

- **The 12 chips moved from build (proposed) to built.** `.btn.chip` has margin 0 6px 6px 0 and `.chips` no gap, the
  value the owner approved in `19f203c5`, accepted by the architect without an owner page. What remains on those
  nodes is a display blockified in a flex row, which the `flex-blockified` rule now catches (12 → 20). One entry,
  `4|Guest departed`, keeps a named reason: the frame draws that chip as the chosen one and the recorded room's
  stay is another.
- **Paired fell by 10 (569 → 559)** because five texts changed in the build and the frames have not followed yet.
  Each was a control that looked live and did nothing, or a drawn-off control with no reason on screen:
  `Zone` (board and sheet) → the label `grouped by zone` · `Discard` drawn off with nothing to discard ·
  `Apply to selected — select rows first` · `Add a phase — the five are the owner's` ·
  `Reassign… — this room's service has ended`. **The drawing moves on these**, and they show here as unpaired
  rather than differing.
- **The 3 adjudicate are unchanged and are not the chips**: `1b|off the day` and `1b|out of order` (a blocked row
  dimmed two ways) and `6|Suite` (a row drawn selected). Each is a design choice for the next owner page.

## Superseded by `07eb133` — the run from `28bfdd0a`

Run 2026-09-19 by KK against the frames **as re-locked at `fff2d96b`**: 01c, approved by the owner, drew every
control in a frame as the build's `<button>` (0 differing pixels over each whole page). **Built by the run, from
`28bfdd0a`, clean.** This measures **the next cut's source, not 0.1.3.** Since 0.1.3 the build has gained the
display-name fix (`df8d44f`) and the model-unavailable sentence (`28bfdd0a`), so `module.js` is `a4666e0c…`, where
0.1.3's was `2a87ed40…`. Part B on 0.1.3 remains that version's own evidence.

### Result

```text
drawn 1,657 · built 1,566 · paired 569 (34.3% of drawn; 427 · 25.8% before 01c) · identical 415 · differing 154
every one of the 154 is named; classify.mjs exits 0; 0 frames unreached
```

| Class | n | Moves |
|---|---|---|
| sticky-header-ground | 23 | neither |
| font-shorthand-line-height | 47 | drawing |
| flex-blockified | 12 | neither |
| button-type-size | 21 | drawing |
| selected-row-example | 2 | neither |
| named, one each | 49 | 30 drawing · 12 **build (proposed)** · 4 neither · 3 adjudicate |

### What 01c's re-lock changed

- **142 more nodes pair.** Tabs, actions and pager buttons are buttons on both sides now. Most of the new
  differences fall under two existing classes: `.btn` at 14px in the frame where §2 fixes 13
  (`button-type-size`, 8 → 21), and a button blockified in a flex row (`flex-blockified`, 2 → 12). **Six are
  named one by one:**
  - frame 4's *Record*, a key collision: a sheet's primary action paired with a tab that shares the word;
  - *Discard* on 4c, 4d and 4e, at the toolbar's 12px where §2 fixes 13 (the drawing moves);
  - 7e's pager arrows: the border is §2's `line-strong` (the drawing moves), and the colour is the recorded single
    page disabling them (neither).
- **The 12 chips that were "adjudicate" now have a proposed side: the build moves.** `preview/audit/chipgap.mjs`
  measured the space below each chip row, drawn against built: the board and wall 12px against 10, the view
  switcher 27 against 24, Setup's chips 18 against 10–12. The owner approved the second set's `.chip` margin of
  `0 6px 6px 0` (`19f203c5`). The build reproduces the horizontal 6px with a flex gap and drops the 6px below the
  row. That's the build short of an approved drawing, not a new design choice, so it isn't on an owner page. It is
  **proposed and not yet built**: it goes into the cut after Part B, if you agree.

### What this does not prove

569 of 1,657 drawn nodes pair (34.3%), from 16.5% (7e) to 68.3% (7c). What remains unpaired is mostly data: the
frames draw Marina Bay, and the harness renders the recorded Coral Cove morning, so rooms, names and times don't
pair by text.

---

## Superseded by the re-lock at `fff2d96b` (01c) — kept, not deleted

*This measured the frames re-locked at `65762c95`. Its figures are right for what it measured.*

Run 2026-09-19 by KK against the frames **as re-locked at `65762c95`** (01b, the owner: *"A small, B keep, C
approved"*), with 02's "match" completed at `a81783e`. **Built by the run, from `b43b949`, clean.** The provenance
stamp names seven artifacts by digest. `module.js` (`2a87ed40…`) and the five widget bundles are the bytes in
**`roomcare-0.1.3.hopkg`** (`c3485aa2…`), which is staged, and Part B certifies it.

### Result

```text
drawn 1,657 · built 1,566 · paired 427 (25.8% of drawn, was 377 · 22.8%) · identical 302 · differing 125
every one of the 125 is named; classify.mjs exits 0; 0 frames unreached
```

| Class | n | Moves |
|---|---|---|
| sticky-header-ground | 23 | neither |
| font-shorthand-line-height | 47 | drawing |
| flex-blockified | 2 | neither |
| button-type-size | 8 | drawing |
| selected-row-example | 2 | neither |
| named, one each | 43 | 25 drawing · 15 adjudicate · 3 neither |

### What the re-lock changed

- **50 more nodes pair.** The frames now draw tiles and chips as the build's `<button>`. H4's retagging rendered
  identically (0 differing pixels over each whole page), so the new pairs are a measurement that is newly possible,
  not a change in the drawing.
- **36 of the new pairs differ, and each is named on its own in `classify.mjs`:**
  - **22 chips, the drawing moves.** Their border is `--line` in the frame, and §2 gives every `.btn` `line-strong`.
    They also carry the next item's two differences.
  - **12 chips, adjudicate.** Every chip is blockified in the build's flex row (neither), and is spaced by its own
    6px bottom margin in the frame but by the row's 6px gap in the build. Nothing rules which spacing is right. One
    of the 12 (frame 4's *Guest departed*) is also drawn chosen, where nothing is chosen on arrival.
  - **2 tiles, data.** G09 and G10 on 1a: the frame's Marina Bay states are not the recorded Coral Cove states.
- **Found by the re-run and closed before naming:** 02's `.chip` and `.strip` were still at the first set. The
  owner's "match" had chosen one set, and my first fold moved only the three classes Part A had then shown. It was
  completed at `a81783e`, which closed the setup chips' padding difference instead of naming it.
- **The re-lock's own records now agree with the build**: `.sect` at `.04em`, the legend and the timeline's detail
  line at 12px / 19.8px. They left no difference to name.

### What this does not prove

The paired share is still the finding: 427 of 1,657 drawn nodes, from 9.1% (7e) to 57.1% (7c). Two causes remain.
The frames draw tabs, actions and pager buttons as `div`/`span`: 239 controls, which is H4's open remainder, drawn
for the owner as its own page. And the frames draw Marina Bay while the harness renders the recorded Coral Cove
morning.

---

## Superseded by the re-lock at `65762c95` — kept, not deleted

*The certificate below measured the frames as they were before 01b's records were folded in. Its figures are right
for what it measured, and they no longer describe the locked frames.*


Run 2026-09-19 by KK. **Built by the run, from `14742a3`, clean** — the
provenance stamp `ui/.parta/provenance.json` names seven artifacts by digest,
and the five widget bundles and `module.js` in **`roomcare-0.1.2.hopkg`**
(sha256 `7b7b6ec0…fdc00a6f`) are those bytes, checked from inside the archive.

**Re-run after the owner chose the second set** (19f203c5, *"we can go with
second"*). The build carries it from `86c504f`. The owner then chose
**"match"** for the setup page (c69fde42, *"i choosed `match`"*). 02 is
redrawn at the second set (`3f1cfbd`), and the build did not change: the
bundles' digests at `14742a3` are those at `89777a1`, so 0.1.2 stands. Earlier
certificates are in this file's history: `71ff149` with 87 differences, and
`89777a1` with 80.

### Method

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

### Proved to fail before the green was counted

```text
an unknown frame id                     run.mjs exits 2, naming the ids
a run with one comparison missing       classify.mjs exits 2 — "20 of 21 frames compared"
a build style changed (.card h3 → 15px) classify.mjs exits 1 — 2 UNCLASSIFIED on frame 7g
the style restored                      frame 7g re-swept clean from 71ff149, exit 0
the second set, old rules (f7ba0df)     classify.mjs exits 1 — 14 UNCLASSIFIED, all on 7b–7g
```

### Result

```text
drawn 1,657 · built 1,566 · paired 377 (22.8% of drawn) · identical 302 · differing 75
every one of the 75 is named; classify.mjs exits 0; 0 frames unreached
```

| Class | n | Moves | Why |
|---|---|---|---|
| sticky-header-ground | 23 | neither | the wall's header sticks over the scrolling house; same pixels |
| font-shorthand-line-height | 39 | drawing | the frame's `font:` shorthand resets line-height (64 §2's fourth reset) |
| flex-blockified | 2 | neither | a span in a flex row computes block |
| button-type-size | 2 | drawing | 64 §2 fixes `.btn` at 13px; the frame's `font:inherit` makes it 14 |
| selected-row-example | 2 | neither | the frame draws a row selected; nothing is on arrival |
| named, one each | 7 | 3 drawing · 3 adjudicate · 1 neither | in `classify.mjs`, with reasons |

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
- **Opened, then closed by "match": 13 on the setup page.** The move opened
  them, because 02 declared these classes once, at the first set, and 01a had
  drawn only the screens page. At `89777a1` they were `adjudicate`. With 02
  redrawn (`3f1cfbd`), all 13 left the rule, which is now removed. 5 are
  identical: the three `.tag`s on 7b, the `.tag` on 7f and the `.pill` on 7g.
  8 differ only in line-height, from the frame's `.mono` `font:` shorthand, and
  are counted under that rule (31 → 39). **None remains for a reason of its
  own.**
- **The one cell with two causes: 7e, `lobby`.** The frame draws its row
  selected (the selected-row example, where neither side moves), and its
  `.mono` shorthand resets line-height (the drawing moves). It is named on its
  own, with both. Until `3f1cfbd` its size differed too. *Once the size
  difference was gone, the `selected-row-example` rule absorbed it, because
  that rule allowed line-height. It is the same fault as the old
  page-redeclares rule. The rule now matches background colour alone, which
  surfaced nothing else.*
- **The other named classes are unchanged**, and so is the paired share,
  because the move changed values, not which nodes pair.

**Moved in the build during the run**, because the drawing was consistent and
the standard silent: the radio's bold at 700, the states tables' 4px rows, the
widget scope at 13px, the *group by* label muted, the card heading's mono
aside at 400 (119 differences became 87). Then, after the owner's choice, the
six classes to the second set (`86c504f`) and the three moves above
(`f7ba0df`): 87 became 80. The drawing's move for "match" (`3f1cfbd`) took it
to 75.

### What this does not prove — read before quoting the 22.8%

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

### For the owner, drawn

1. **The screens page contradicted itself. Decided: the second set** (owner,
   2026-09-19, 19f203c5, *"we can go with second"*). The build carries it.
   **The setup page: decided, "match"** (owner, 2026-09-19, c69fde42, *"i
   choosed `match`"*). 02 is redrawn at the second set (`3f1cfbd`), and its 13
   differences closed. *History: this item said "nine classes" and "the
   build follows the first" until 2026-09-19, and 01a
   (`docs/mockups/01a-the-classes-declared-twice.html`) drew the eleven.*
2. **Measurable frames.** Redrawing the frames in the build's element types
   and the harness's data would take the pairing share toward the
   whole screen. That changes locked drawings, so it is yours to ask for.

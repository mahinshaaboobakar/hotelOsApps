# Room Care Part A — frame beside capture

Run 2026-09-19 by KK. **Built by the run, from `71ff149`, clean** — the
provenance stamp `ui/.parta/provenance.json` names seven artifacts by digest,
and the five widget bundles and `module.js` in **`roomcare-0.1.1.hopkg`**
(sha256 `29bb38a0…0eeea0`) are those bytes, checked from inside the archive.

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
```

## Result

```text
drawn 1,657 · built 1,566 · paired 377 (22.8% of drawn) · identical 290 · differing 87
every one of the 87 is named; classify.mjs exits 0; 0 frames unreached
```

| Class | n | Moves | Why |
|---|---|---|---|
| page-redeclares-a-class | 24 | **adjudicate** | the screens page declares nine classes twice; the setup page once |
| sticky-header-ground | 23 | neither | the wall's header sticks over the scrolling house; same pixels |
| font-shorthand-line-height | 27 | drawing | the frame's `font:` shorthand resets line-height (64 §2's fourth reset) |
| flex-blockified | 2 | neither | a span in a flex row computes block |
| button-type-size | 2 | drawing | 64 §2 fixes `.btn` at 13px; the frame's `font:inherit` makes it 14 |
| selected-row-example | 3 | neither | the frame draws a row selected; nothing is on arrival |
| named, one each | 6 | 2 drawing · 3 adjudicate · 1 neither | in `classify.mjs`, with reasons |

**Moved in the build during the run**, because the drawing was consistent and
the standard silent: the radio's bold at 700, the states tables' 4px rows, the
widget scope at 13px, the *group by* label muted, the tag's first-declared
padding, the card heading's mono aside at 400, the dock's labels at the
first-declared mono (119 differences became 87).

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

1. **The screens page contradicts itself.** Redline 5 added a second,
   unscoped declaration of `.pill .mono .tag .tile .tilegrid .btn .chip
   .legend .strip`, which re-renders every earlier frame at the new values; the
   setup page keeps the first. The build follows the first. Which is intended?
2. **Measurable frames.** Redrawing the frames in the build's element types
   and the harness's data would take the pairing share toward the
   whole screen. That changes locked drawings, so it is yours to ask for.

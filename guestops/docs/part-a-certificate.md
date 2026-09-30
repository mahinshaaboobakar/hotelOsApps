# GuestOps Part A — frame beside capture

> **Part A is drawing fidelity only, and is never sign-off** (owner ruling,
> 2026-09-19). It puts an approved frame beside a harness rendering of **recorded
> fixtures**, not the owner's installed build on the property's real data. **It
> says nothing about whether a button works, whether an action is authorized, or
> how a screen looks with this property's data.** Those are the capability ledger,
> Part B and Part C, which are the evidence.
>
> **And ADR 0358 exists because this document's approval was read as more than it
> says.** The owner's finding of 2026-09-30 — *"from pagination to dropdown & date
> choose and any tab nothing is not perfect"* — is true at the same time as every
> `identical` below, because *"a fidelity sweep cannot find a missing control: it
> measures nodes that exist, and an absence has no node"* (HH, 2026-09-10). A dead
> dropdown, a pager that does not page and a date field that does not open all
> render correctly.

---

## ⚠ The run below is from `2026-09-17` and does NOT describe HEAD

**42 commits have touched `guestops/ui` since it was taken.** Among them, and each
changes what a sweep would now measure:

```text
3ae909d1   the failure card gained a structured drawing and a copy button
277eb6ad   the render-fault guard — a new card state that did not exist
c187dba3   its no-retry assertion
27c878b7   the copy button REMOVED, so a control the run measured is gone
0.3.3 → 0.3.4   two package cuts
```

**So every figure here is true of `2026-09-17` and of nothing else.** It is
recorded rather than deleted because it is the only Part A measurement GuestOps
has, and because a certificate that silently re-rendered would lose the run that
was actually made. **A re-sweep is owed**, and until it exists no cell below may
be quoted as current — *"re-measuring is the tempting move and it manufactures a
fresh, confident, unrelated number"* only where the subject has moved, which here
it has.

---

## Run from `2026-09-17T10:09:59Z` — 17 frames, one instrument

**Provenance recorded by the instrument, not typed beside it.** Each frame's
comparison file carries the invocation verbatim:

```text
drawn   2026-09-17T10:09:59.883Z   .../docs/mockups/01-guestops-gold.html   #f1…#f16
built   2026-09-17T10:10:02.171Z   .../preview/frame.html?screen=…          body
both    1220 × 900
key     "tag + normalised text, then document order inside a colliding group"
        ARCH-Q12 step two, changed 2026-09-10
        comparableWithRunsBefore: FALSE — so a saved run from before that date
        cannot be read across this key, and the instrument says so itself
```

**One key across all 17 frames**, measured — not seventeen private filters. That
matters because *a filter written per stream catches the instances that happen to
look wrong and misses the rest by construction*; the fix belonged in the key.

### The figures, and both columns close

```text
frames                    17        arithmetic closes on 17/17
drawn nodes              892
built nodes              726
paired                   607        of which 114 paired BY POSITION
identical                600
differing                  7        every one named below
unpaired · drawn         277
unpaired · built         115
refused groups             4        an ambiguous key — the instrument declines
refused · drawn            8          to invent a comparison
refused · built            4
collapsed                  0        emitted AT ZERO, so "nothing collapsed" and
                                    "the field vanished" cannot look alike
```

**Checked in both directions rather than reported as components:**

```text
drawn   892 = 607 paired + 277 unpaired + 8 refused     ✓
built   726 = 607 paired + 115 unpaired + 4 refused     ✓
paired  607 = 600 identical + 7 differing               ✓
```

*Closing the arithmetic is the check that finds figures which were never measuring
what their labels said. It cannot catch a run pointed at the wrong documents —
which is why the provenance above is the instrument's own and the frame labels are
listed rather than assumed.*

### The 17 frames

```text
f1  1 · Today                  f10 9 · The group
f2  2 · Bookings               f11 10 · Walk-in
f3  3 · Stay · Overview        f12 11 · Today, PMS-connected
f4  4 · Stay · Activity        f13 12 · Attention
f5  5 · Stay · Requests        f14 13 · First run
f6  5b · Requests, Jobs absent f15 14 · New booking
f7  6 · Stay · Servicing       f16 15 · Registration card
f8  7 · Stay · Payment         f17 16 · Setup
f9  8 · Cancel
```

### Every one of the 7 differing, named

| Frame | Node | What differs |
|---|---|---|
| 2 · Bookings | `<span>` "holds no room" | **ten properties** — `font-size` 12px→11.5px · `font-weight` 400→500 · `line-height` 19.8→17.825px · `padding-top`/`bottom` 0→3px · `border-top` none→1px dashed, and its colour · `display` block→flex · `gap` normal→6px |
| 4 · Stay · Activity | `<div>` "Newest last · times are the property's" | `gap` normal→8px |
| 11 · Today, PMS-connected | `<button>` "＋ New booking" | `margin-bottom` 0→6px |
| 11 · Today, PMS-connected | `<button>` "Walk-in" | `margin-bottom` 0→6px |
| 11 · Today, PMS-connected | `<span>` "Check-ins have not arrived for 5 h 12…" | `margin-top` 0→3px · `display` inline→block |
| 16 · Setup | `<b>` "Overdue is shown, never enforced." | `font-weight` 500→700 |
| 16 · Setup | `<button>` "＋ Close a room type for dates" | `font-weight` 400→500 · `border-top-color` · `background-color` |

**The Bookings span carrying ten differences at once is the shape to notice.** A
classifier keyed on one attribute reports that attribute and is blind to every
other difference on the same node, so **a difference count is a floor** until the
keyed attribute agrees everywhere. Here the whole node is read, which is why one
row carries ten properties rather than appearing as one font-size row.

### The 4 refused groups, and refusing is the correct behaviour

```text
span 4                    drawn 2 · built 1
b 214                     drawn 2 · built 1
div deluxe king           drawn 2 · built 1
div executive suite       drawn 2 · built 1
```

Two drawn nodes share a key and one built node claims it. **The instrument
declines to pair rather than taking the first it meets** — because a prose
quotation of a value paired against the value is a comparison of a note with a
row, and it reads as a real divergence. *A refusal is a finding; a confident wrong
answer is a finding-shaped object that survives review.*

---

## What this does not prove

* **Nothing about HEAD.** 42 commits have touched `guestops/ui` since the run, one
  of which removed a control the run measured. A re-sweep is owed.
* **Nothing about whether a control works.** Every `identical` above is a
  statement about CSS. The owner's dropdown, date, pager and tab findings are
  Part C's, and ADR 0358 exists because those two were conflated.
* **Nothing about the owner's installed build or this property's data.** The
  built side is the capture harness rendering `book/recorded/` fixtures.
* **Nothing about the 277 unpaired drawn and 115 unpaired built nodes.** They are
  reported as their own numbers and are in neither the numerator nor the
  denominator of `identical` — *a set that is neither is invisible to every check
  that adds up*, which is why they are printed rather than folded away.
* **Nothing about a frame nobody swept.** 17 were, and `docs/mockups/` holds eight
  files; frames from `02-flows`, `04`–`08` are outside this run.
* **Nothing that a 0 proves on its own.** `collapsed 0` is a measurement with a
  denominator beside it; read as *nothing was dropped*, not as *nothing was
  looked at*.

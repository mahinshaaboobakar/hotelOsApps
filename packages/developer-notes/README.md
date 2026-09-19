# developer-notes

**Developer notes never reach a screen.** The owner ruled on 2026-09-19 that a
mock carries the screen and notes for the developer, and the notes are never
built as UI. This is the one copy of the guard that enforces it. Every
application's rendered-text walk imports it.

```ts
import { developerNotes, readableText } from "../../../packages/developer-notes/src";

expect(developerNotes(readableText(surface))).toEqual([]);
```

- `readableText(root)` returns what a person can read: each text node on its
  own line, plus `title`, `aria-label`, `aria-description` and `placeholder`.
  Stylesheets and scripts are removed first.
- `developerNotes(text)` returns every note found, named for what it is:
  - register ids, ADRs and section signs (`§`)
  - design-section, row and chapter references, and "design page"
  - snake_case identifiers and correlation ids
  - the platform systems by name

It is the union of three guards: Workforce's `b001bfac`, GuestOps'
`document-citations` (`8aae94e`) and the interim `scripts/developer-content.ts`
(`15e2654`). Where two spelled one shape differently, the wider spelling was
kept.

**What it cannot find:** a rationale in plain words has no shape. Those are
found by reading, and each application's capability ledger records what its
reading found.

`npm test` runs the guard's own controls: every shape planted and found, and
near-miss staff text left alone.

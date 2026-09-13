# A home for the applications' shared UI test-support — proposal

**Stream HH · 2026-09-13 · not a decision.** ADR 0157 gave application-owned
test-support a home for .NET (`packages/HotelOS.Applications.TestSupport`) and
left the TypeScript/JavaScript half without one. Four files across three
applications are the same file:

```text
jobs/ui/preview/boot/fresh.js          adopted from workforce, labelled
workforce/ui/preview/boot/fresh.js     GG's original, d7d084a
jobs/ui/preview/parta/serve.mjs        adopted from guestops, labelled
guestops/ui/preview/parta/serve.mjs    FF's original, 539daf4
```

Both of mine carry *"deleted when the shared home exists"*. This is that
question, brought as a proposal rather than answered by whoever moved first.

## The constraint the .NET half does not have

The two files are not the same kind of thing, and a single answer that ignores
this will be wrong for one of them:

| | loaded by | reached how |
|---|---|---|
| `serve.mjs` | **node**, in the driver process | an `import` — any path on disk works |
| `fresh.js` | **the browser**, inside the harness page | `<script type="module">` fetching `./boot/fresh.js` **over HTTP**, from the served root |

A node package solves the first completely. It does **not** solve the second:
the browser can only fetch what the harness server serves, and the harness
server's root is the application's own `ui/` directory. A shared `fresh.js`
that lives outside that tree is unreachable from the page unless something
puts it inside — a copy at build time, or a second mount on the server.

## Three shapes, with what each costs

**A · `scripts/`, the precedent already here.** `build-module.mjs`,
`build-provider.mjs` and `check-platform-consumers.mjs` are shared JS in this
repository today, referenced by relative path from each application
(`node ../../scripts/build-module.mjs`). Adding `scripts/preview/serve.mjs`
follows a pattern nobody has to be told about.

* costs nothing to set up; no install step, no workspace, no version
* the driver imports it by relative path, exactly as the build already does
* **does not answer `fresh.js`** — a relative path is not an HTTP path

**B · an npm workspace package** — `packages/ui-test-support`, imported as
`@hotelos/app-test-support`.

* the shape ADR 0157 chose for .NET, so the two halves would match
* needs a root `package.json` with workspaces; **there is none today**, and each
  `ui/` installs its own `esbuild`, `happy-dom`, `typescript`, `vitest`
  independently. That is a change to how every application builds, for two files
* still does not answer `fresh.js` on its own

**C · shared source, served rather than copied.** The shared files live in one
place (A's `scripts/preview/` or B's package), and the harness server — which is
already ours, in-process, and mounts a root — **mounts a second root** at a
fixed path:

```text
/            → <app>/ui                     the application's own tree
/_shared/    → <the shared home>            one copy, served not duplicated
```

`frame.html` then imports `/_shared/fresh.js`. Nothing is copied, nothing is
generated, and a fresh checkout has no build step before the harness works —
which is the defect GG's own label warns about, one layer up: a loader written
beside generated bundles is silently uncommitted.

* answers both files with one source
* the mount is ~5 lines in `serve.mjs`, which is itself the shared file
* the one new thing to know is the `/_shared/` path, which the harness page
  states in its own comment

## What I would do, and why it is not mine to decide

**C over B over A**, on the ground that it is the only shape that answers both
files from one source, and that it needs no change to how the applications
build. B's parity with .NET is real but buys a workspace migration for two
files; A is free but leaves `fresh.js` duplicated, which is where this started.

The reason it is a proposal: **B is the shape that matches ADR 0157**, and
matching a ruled shape is a decision about consistency that outranks my
convenience. If the answer is B, the workspace migration is a separate piece of
work with its own blast radius, and it should be planned rather than arrive
inside a test-support commit.

## What lands the day it is answered

* the four copies collapse to one, and both of my labels are honoured by
  deletion rather than by a comment that outlives the question
* `workforce/ui/preview/boot/fresh.js` and `guestops/ui/preview/parta/serve.mjs`
  converge too — GG's and FF's, so the move is theirs to make or to delegate
* the two defects GG paid for inside `fresh.js` — `fetch` and `import()`
  resolving `./` against different bases, and the refusal path that must not
  degrade to an untagged import — get one place to be remembered in, which is
  the argument ADR 0154's addendum 1 records

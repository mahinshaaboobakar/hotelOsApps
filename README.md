# HotelOsApps

**The application repository.** Every installable HotelOS application is
developed here — or in a repository shaped exactly like this one — as a
**separate application, by its own developer, without the platform's core
code**. It binds to HotelOS at exactly one point: the signed `.hopkg`
install.

Ruled by platform ADR 0122 and its same-day addendum (owner direction,
2026-08-28), building on ADR 0121 (an application carries its own
documentation) and ADR 0092 (the `.hopkg` package contract).

## What an application is

One directory per application, each **fully self-contained**:

```text
<app>/
  docs/
    chapters/      the application's design
    decisions/     the application's ADRs — numbered locally from 0001,
                   indexed in their own README.md
  backend/         .NET service per the platform template
  frontend/        the desktop module
  schemas/  migrations/  tests/  assets/
  manifest.yaml
```

## What an application may depend on

The platform's **contracts and SDK** — never its internals:

* `shared/protos` — the contract language
* `HotelOS.Platform` — the .NET SDK
* the manifest schema and package layout of **ADR 0092 (`.hopkg` v1)**
* a locally **installed HotelOS** as the development and test platform

How the protos and SDK are taken is an implementation choice for now — a
sibling-path reference to the platform checkout, or a copied `shared/protos`
— until an SDK-publishing round makes them proper artifacts (ADR 0122
addendum). What an app never does is build, modify or import the platform's
*services*: Kernel, Identity, Master Data, Context. If an app needs something
the contracts don't expose, that is a request to the platform team.

## The deliverable

A **signed `.hopkg`** per ADR 0092 — built from the app directory, signed at
the registry by the vendor (the signing key never enters any repository,
this one included), distributed through the hosted registry, installed by
Software Center. Git is how apps are *developed*; `.hopkg` is the only form
in which an application ever reaches a property.

## Connectors live here too — owner direction, 2026-08-30

> *"Apps and connectors are root folders, and their docs are kept there."*

A connector is developed exactly as an application is: **one root directory
per connector**, self-contained, carrying its own `docs/chapters/` and
`docs/decisions/`, binding to the platform only through the contracts and the
package. What differs is what it is a package *of* — platform `CONN-Q1` rules
the connector package kind — and where its runtime sits: a connector is
installed into the **Integration Hub**, which is platform core and lives in
the platform repository, never here. So the split is:

```text
platform repository     the Integration Hub — inbox, dedupe, normalisation,
                        replay; the connector runtime; the contracts
this repository         each connector — its PMS study, its design, its
                        mapping and its package
```

## Applications and connectors

| Directory | Kind | Status |
|---|---|---|
| `workforce/` | application — Roster (`APPS-Q1`'s name table) | **design complete, awaiting the owner's read** — Stream GG, 2026-08-31. Plan `01-the-workforce-application.md` (revision 2 — nine aggregates, six slices, scope ruled by `WF-Q11`) · study `02-the-current-system-and-the-gaps.md` (all eight seed subjects walked, 59 rows sorted) · gold mockup `docs/mockups/01-workforce-gold.html` (**revision 2 — 12 frames**; frames 1–6 redrawn, five surfaces new) · flows `docs/mockups/02-workforce-flows.html` (seven paths, and `WF-Q16`'s refuse/warn table made visible). **No reference backend exists for this app** (owner, 2026-08-31), as for GuestOps; the study ran on the owner's answers subject by subject, under `WF-Q1`–`WF-Q16` and ADR 0063/0052/0116/0119/0128. **Owner corrections taken at the mockup read:** *Staff Schedule*, not *My Schedule* — Workforce is a manager and HR application, so every request is raised on behalf with `entered_by` recorded, and the staff-facing door **stays** for login-holders (owner, 2026-08-31 — `WF-Q9`(b) whole); plus the shift-creation page the catalogue had been missing. **Backend code is GO** (owner, 2026-08-31) — `backend/` on the GuestOps split, slice 1 first (postings, the `department#posted` writer, the Context resolver); **nothing desktop until round 50 lands**, and gaps go to the register as questions. Routing prerequisite **met** (`PKG-Q39`, all four domains, `fca1b96`). Raised out of the round: **`PKG-Q39`** (ruled — event domains manifest-declared; Workforce declares **four**, `attendance` included) · **`SHELL-Q23`** (print ruled, the file half left open) |
| `pms-oracle/` | connector (working name — `CONN-Q1` and the owner's "which PMS first" decide the final one) | in study — Stream DD; the reference study lands as `pms-oracle/docs/chapters/01-the-oracle-pms-reference-study.md` |
| `guestops/` | application — the Reservations domain of ADR 0089 / Chapter 26 (`APPS-Q1`'s name table) | **design complete, awaiting the owner's read** — Stream FF, 2026-08-31. Scenarios `01-the-front-desk-scenarios.md` (S1–S39) · design `02-the-guestops-design.md` · open questions `03-the-open-questions.md` · gold mockup `docs/mockups/01-guestops-gold.html` · flows `docs/mockups/02-guestops-flows.html` · **readiness `04-the-code-readiness-note.md`**. **No reference backend exists for this app** (owner, 2026-08-31); the round was built from the PMS study's reservation facts, the chapters and the owner's scenarios, under rulings GUEST-Q1–Q8. **Reconciled 2026-08-31 against `CONN-Q11`, `SHELL-Q23`, `WF-Q7/Q8`, `PKG-Q39` and DD's `dto.proto`: nine mismatches reported, none resolved — `PKG-Q39` passes clean (all three domains routed to `GUEST`).** Code is gated on the owner's verification, on `APPS-Q1`'s two prerequisites, and on the contract questions in readiness §1.5 |

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
| `workforce/` | application | in design — chapter `workforce/docs/chapters/01-the-workforce-application.md` · gold mockup `workforce/docs/mockups/01-workforce-gold.html` (awaiting owner approval) |
| `pms-oracle/` | connector (working name — `CONN-Q1` and the owner's "which PMS first" decide the final one) | in study — Stream DD; the reference study lands as `pms-oracle/docs/chapters/01-the-oracle-pms-reference-study.md` |
| `guestops/` | application — the Reservations domain of ADR 0089 / Chapter 26 (`APPS-Q1`'s name table) | in design — Stream FF; the scenario record lands as `guestops/docs/chapters/01-the-front-desk-scenarios.md`. **No reference backend exists for this app** (owner, 2026-08-31); the round is built from the PMS study's reservation facts, the chapters and the owner's scenarios |

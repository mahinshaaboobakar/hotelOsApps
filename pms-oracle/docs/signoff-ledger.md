# pms-oracle — the signoff ledger

**Four columns per component: what is PROVEN · by what EVIDENCE · every GAP ·
with a NAMED owner.** The GuestOps shape.

> **A row whose owner is *"someone"* has not been derived yet.** Every gap below
> names a party or says, in its own words, that it is unassigned — which is
> itself an answer and a different one from *not yet looked at*.

**The three gap classes are kept distinct throughout**, because they have
different remedies and only one of them is a defect:

```text
UNRULED     the planner's or the owner's. Named, never filled
UNBUILT     ours, and whose
UNPROVED    BY CONSTRUCTION (no machine here can reach it) ·
            BY CHOICE (with the reason recorded) · never BY OMISSION
```

---

## The package

| | |
|---|---|
| **PROVEN** | `kind: connector`, `id: pms-oracle`, `version: 0.1.0`, three integrations, `db_connections: 0`. The capability floor admits it — `PackageKind::Connector => Some(CONNECTOR_SINCE)`, `0.1.8` shipping |
| **EVIDENCE** | `manifest.yaml` read in full; `capability.rs`'s floor measured, not quoted |
| **GAP** | **NEVER CUT.** No `pms-oracle*.hopkg` exists anywhere. **UNBUILT, and the blocker is not the dirty tree** — `hopkg` has `keygen` and `sign <package-dir>` and **no pack verb**, and nothing in `scripts/` or `deployment/` assembles a package directory for any package. `oracle-conformance.md` §5 states the sweep's scope |
| **OWNER** | **the assembly step is UNASSIGNED** — it is nobody's today and it is what every connector cut needs first. The cut itself is mine once it exists; the install and the keys are the owner's |

## The three integrations

| | |
|---|---|
| **PROVEN** | `oracle-cloud` polls (`ingress_authentication: []`, correctly); `oracle-onpremise` and `oracle-web` accept push, each declaring `[shared-secret]`. The cloud drain terminates four ways and throws on any status that is neither 200 nor 204 |
| **EVIDENCE** | `manifest.yaml:316/:348/:365`, `:340/:362/:377`; `OhipBusinessEventQueue.cs:79-120` and `:140-161` read in full |
| **GAP** | ⚠ **Chapter 03 §2.4 claims *secret AND allow-list* for both push flavours. One mechanism is declared, not two.** A FACTUAL correction to the chapter — the REFUSED grade survives it, the count does not. **And `oracle-onpremise` declares `onsite-room-status` with no reference provenance at any layer** — `payload-provenance.md` §13 confirms it from the implementations |
| **OWNER** | the §2.4 sentence is the **chapter's author / the architect**, with the measurement here. The `onsite-room-status` declaration is **the declaration's author or the owner** — two readings, and `payload-provenance.md` §5 refuses to pick |

## Capabilities

| | |
|---|---|
| **PROVEN** | four connector-owned capabilities declared and **all four enforced**; `ConnectorDispatcher.cs:82` reads `Implements` and refuses before dispatch. Declared-but-unenforced: **0**. Enforced-but-undeclared in `pms-oracle/`: **0** — the package contains **zero `RequireAsync`**, with a positive control proving the sweep works |
| **EVIDENCE** | `capability-ledger.md`, keyed on the authorization **call** and not door-by-door — the shape FF's `StayLifecycleService` case warns about |
| **GAP** | the Hub's `quarantine.read` is **enforced on the Hub's surface and declared by no connector**, which is the Hub's boundary rather than Oracle's. `IntegrationNotConfigured` has **one** use and page 75 §7 Q4 asks to reclassify it as a Fault |
| **OWNER** | `quarantine.read` — **the Hub's stream**. §7 Q4 — **the planner**, open |

## The Connector Runtime Supervisor

| | |
|---|---|
| **PROVEN** | binary at `target/debug/hotelos-connector-runtime.exe`. **It binds a listener AND registers with the Kernel** — `9e5c2731`, 2026-10-02 — through `packages/platform-mtls`, which `14ffea06` extracted so ADR 0040 had a legal answer for a second Rust process. Its address exists: `15160 / 25160`, `148eff65` |
| **EVIDENCE** | `main.rs:64-66`, `:212`, `:222`, `:232-243`; `kernel.rs:79`, `:88`; `Cargo.toml:97` |
| **GAP** | ⚠ **`main.rs:37-45` still declares the mTLS channel a MEASURED BLOCKER and says *"Nothing in `packages/` holds a Rust equivalent"* — 180 lines above the code that uses it.** A reader meeting that line concludes they are blocked on architecture that landed. Also: `dev.py` does not start it, and `open_session` forwards no frames |
| **OWNER** | **II** — assigned, and the Supervisor is theirs. Reported rather than edited, because the correction belongs with the reason in the owner's own tree |

## The Hub's side, and configuration

| | |
|---|---|
| **PROVEN** | three `PackageLifecycle` handlers live at `Program.cs:399-401`; `SetIntegrationConfiguration` at `service.proto:129`, reached from `src-tauri/commands/integrations.rs` by both Operations Center and Software Center |
| **EVIDENCE** | read at the line — the `CONN-Q13` register row records a revert, and the source does not |
| **GAP** | **an Oracle connector can be INVOKED without the Supervisor and cannot be RUN AS A CONTAINED PROCESS without it.** Two verbs, two answers — `oracle-conformance.md` §4 |
| **OWNER** | the process half is **II**'s with the Supervisor; the dispatch half is **proven today** |

## The reference trace

| | |
|---|---|
| **PROVEN** | six payload kinds cited at `file:line`; three flow passes; seven reference defects and five platform divergences tabled; the `housekeepingStatus → hkStatus` mapping that settled `CLAUDE.md:408` |
| **EVIDENCE** | `payload-provenance.md` §§1–13 |
| **GAP** | **209 of `Reservation.java`'s 251 lines — CLOSED BY §2**, which covers `ohip-reservation`'s field mapping from the model's side. Named rather than left unstated |
| **OWNER** | mine, and closed |

---

## Part A · Part B · Part C

| | |
|---|---|
| **Part A** | **STRUCTURAL.** `CONN-Q20` — no harness can mount a package UI — **the OWNER's, open since 2026-09-10**. UNPROVED **BY CONSTRUCTION** |
| **Part B** | **FORM RULED** — ADR 0369. Platform-served **PASS** against the installed `.hopkg`; the person column **N/A** while the package exposes no human-operated surface, *with the reason at the row*. **UNPROVED BY CONSTRUCTION today**, because §5 establishes nothing assembles a `.hopkg` to exercise. **Never written as *"Connector Part B = N/A"*** — that recreates `CONN-Q15`'s collapse and discards the served proof |
| **Part C** | **UNRULED — `CONN-Q92`.** Both obvious fillings rejected by name. **No `part-c-coverage.md` is written here**, and its absence is this ruling rather than an omission |

**Two of GuestOps' six documents do not apply to a connector, one is forbidden,
and two have no GuestOps counterpart.** `oracle-conformance.md` §7 maps all six
with a reason each, because *"the set is incomplete"* would hide which absences
are mine.

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
| **Part B** | **FORM RULED** — ADR 0369. Platform-served **PASS** against the installed `.hopkg`; the person column **N/A** while the package exposes no human-operated surface, *with the reason at the row*. **STILL UNPROVED, AND THE REASON HAS CHANGED** — this row said *"because §5 establishes nothing assembles a `.hopkg`"*, and **DD's `aca56d4` built `pack-package`, so that reason has expired.** The live reason is different and worse: **`pms-oracle-0.1.0.hopkg` IS cut, signed and in the registry, and ships ONE file where EIGHT are needed** — `PmsOracle.Connector.dll` at 74 KB against 427 KB — *a signed library wearing a connector's name*, which `package list` reports as signed. **Never written as *"Connector Part B = N/A"*** — that recreates `CONN-Q15`'s collapse and discards the served proof |
| **Part C** | **N/A — `CONN-Q92` CLOSED**, ADR 0369 Addendum 1. Not awaiting invention: **a connector is not in ADR 0358's Part C taxonomy at all.** No `part-c-coverage.md` is written, and its absence is the ruling |
| **Connector Certification** | **SEPARATELY REQUIRED — the NINTH row**, beside A/B/C rather than inside them. Chapter 18's eight dimensions, evidence below |

---

## Connector Certification — Chapter 18's eight dimensions

> **The authoritative home for WHAT certification contains is
> `Chapter 18_ complete engineering implementation blueprint.md:661-679`.** `ARCH-Q60`: one live rule, one
> authoritative home. **This ledger cites that chapter and records the EVIDENCE
> per dimension** — it does not restate the definition, and the eight names below
> are pointers into the chapter rather than a second copy of it.

**The eight are a CLOSED REQUIRED set.** *"Connectors require certification"*
followed by that enumeration is **not** permission to certify whichever items
apply conveniently.

> ⚠ **NO EXCEPTION IS CLAIMED HERE.** All eight are recorded as required.
> **Four are UNPROVED with a named reason, and none is reported as
> inapplicable** — because an item that genuinely cannot apply would need its
> own explicit rule from the planner, never a silent disappearance from this
> table.

| | dimension | evidence, or the named gap |
|---|---|---|
| 1 | **Mock APIs** | ✅ four `HttpMessageHandler` fixtures answering as OHIP — `ConnectionTestTests:207` · `Hub:170` · `OhipAccessTokenTests:118` · `OhipBusinessEventQueueTests:252` |
| 2 | **Real sandbox APIs** | ❌ **ZERO.** No sandbox reference anywhere in the package. **UNPROVED BY CONSTRUCTION** — it needs Oracle OHIP sandbox credentials, which are the owner's and exist on no machine here |
| 3 | **Authentication** | ✅ outbound: `OhipAccessTokenTests` · `OhipPasswordGrantTests` · `OhipCredentialTests`. Inbound: `ingress_authentication` declared at `manifest.yaml:340/:362/:377`, asserted by `ManifestDeclarationTests`. ⚠ and `oracle-conformance.md` §2.4 — the chapter claims two mechanisms where **one** is declared |
| 4 | **Retries** | ❌ **REQUIRED AND UNPROVED — `CONN-Q93` RULED, ADR 0369 Addendum 2.** Two obligations, neither met: prove **through the Hub boundary** that this connector behaves per its declared contract **while the Hub performs retry and backoff**, *and* prove it **implements no independent retry or queue mechanism**. ADR 0128 §5 settles **who implements the mechanism** and *"does not settle whether a particular connector interacts correctly with"* it. **Hub certification does not discharge this row** |
| 5 | **Rate limits** | ⚠ **RULED `UNAVAILABLE = 6` — `CONN-Q94`, ADR 0208 Addendum — AND NOT YET EXPRESSIBLE.** `UNREACHABLE` is categorically wrong: *"a 429 proves enough communication occurred to make that claim false."* **The blocker is measured, not assumed**: `HotelOS.Connector`'s `ConnectionTest` offers **five** factories — `Incomplete` · `Reached` · `Refused` · `NotSupported` · `Unreachable` — and **none for `Unavailable`**, so a connector implementing `ITestableConnection` has no named way to return the ruled outcome. *The generated enum DOES carry `Unavailable = 6` (`Dto.cs:460`), so the value exists and only the SDK's surface is missing.* **Reported rather than worked around** — see the blocker note below |
| 6 | **Disconnections** | ✅ six sites, **injected rather than asserted in prose** — `OhipBusinessEventQueueTests:108` asserts the drain throws and `:269` injects *"the connection was reset"*; `ConnectionTestTests:147/:228` · `GuaranteeFetchTests:225` · `Hub:205` |
| 7 | **Webhook processing** | ◐ **PARTIAL.** `accepts_push` declared at `manifest.yaml:339/:361/:376` and **asserted** at `ManifestDeclarationTests:128`; the on-site normalisers are tested. **The DECLARATION is proven; an inbound push through the real ingress is the Hub's path and is not exercised here** |
| 8 | **Offline mode** | ❌ **REQUIRED AND UNPROVED — `CONN-Q93` RULED.** Prove **through the Hub boundary** across the Hub's durable/offline handling **and the restoration of connectivity**, *and* prove this connector **implements no offline queue of its own**. Same ruling and same two halves as row 4 |

**Three dimensions have direct connector-side evidence; one is partial; two are
Hub facilities with no evidence here; one has a single assertion whose
classification is open; one is absent by construction.**

> ⚠ **THE ONE AMBIGUITY, NAMED RATHER THAN RESOLVED.** Dimensions 4 and 8
> are **Hub facilities by ADR 0128 §5**, so their mechanism is not the
> connector's to build. **Whether the Hub's own certification discharges those
> two dimensions for a connector, or whether this ledger owes evidence at the
> Hub boundary, is stated by neither Chapter 18 nor ADR 0128.**
>
> **This is NOT recorded as an exception** — both rows stay required and
> unproved. *The distinction matters: an exception would remove them from the
> set, and naming the ambiguity leaves them in it.*

| | |
|---|---|
| **OWNER** | dimensions 1, 3 and 6 are **PROVEN** and mine. 2 is **the owner's** (sandbox credentials). 5 is mine to raise. 4, 7 and 8 need the **Hub boundary** exercised, which is blocked by the same thing Part B is: `§5` establishes nothing assembles a `.hopkg` |

**Two of GuestOps' six documents do not apply to a connector, one is forbidden,
and two have no GuestOps counterpart.** `oracle-conformance.md` §7 maps all six
with a reason each, because *"the set is incomplete"* would hide which absences
are mine.

# pms-oracle — the signoff ledger

**Four columns per component: what is PROVEN · by what EVIDENCE · every GAP ·
with a NAMED owner.** The GuestOps shape.

> **A row whose owner is *"someone"* has not been derived yet.** Every gap below
> names a party, or says in its own words that it is unassigned — which is
> itself an answer and a different one from *not yet looked at*.

**Every gap carries one of four dispositions, and they are not interchangeable:**

```text
PROVEN      evidence named, and where a guard was shown able to FAIL, that too
UNBUILT     ours, and whose
UNRULED     the planner's or the owner's. Named, never filled
UNPROVED    BY CONSTRUCTION  no machine here can reach it
            BY CHOICE        deliberately not run, WITH THE REASON
            BY OMISSION      nobody looked — a defect, and there are none here
```

---

## The package — PROVEN, and cut and installed since this morning

| | |
|---|---|
| **PROVEN** | `kind: connector`, `id: pms-oracle`, **`version: 0.1.1`**, three integrations, `db_connections: 0`. **Cut, signed, shelved and INSTALLED.** `pms-oracle-0.1.1.hopkg` — **9 files, 427,085 bytes**, `sha256:a430a46b5e0d188e…`, signed `dev-local`; the registry copy is **byte-identical** to the build and both match hopkg's own reported digest |
| **EVIDENCE** | The archive was **derived, not handed a file list** — `pack-package` reported *backend 8 files, 5 named by `deps.json`, 0 absent · floor `pms-oracle-0.1.0.hopkg` 4 entries, 0 not staged*. **And the install is measured at the destination, not inferred**: `packages/installed/pms-oracle/manifest.yaml` reads `version: 0.1.1`, beside `MANIFEST.sig`, `backend/` (8 files) and `ui/module.js`. **The catalogue index now carries it** — 32 entries, both oracle rows — so the platform wrote the entry, exactly as DD measured it does when a file appears |
| **GAP** | **None.** *This row said the index was still at 31 and the entry was the platform's to write. It has been written; the premise expired in the package's favour and is corrected rather than left understating what exists* |
| **OWNER** | mine, and closed. **The assembly step itself was DD's** — `pack-package`, `aca56d4` — without which no connector could be cut at all |

> **The cross-check is the best evidence in the cut**, and it is convergence
> from different evidence rather than either measurement alone: DD measured
> independently that the archive *needs* **eight files and 427 KB**; the signed
> result is **9 entries and 427,085 bytes**. One derivation from `deps.json`,
> one from reading the runtime's requirements, agreeing to the kilobyte.

**And a defect in `pack-package` is reported, not fixed** — its first run creates
the staging **before** validating the publish directory, so its own refusal
leaves `stage-<version>/manifest.yaml` behind and the next run refuses on
residue the first refusal made. *Same shape as a refused lock claim leaving a
0-byte lock: a guard placed after the write it protects is not a guard.*
**OWNER: DD.**

---

## The three integrations

| | |
|---|---|
| **PROVEN** | `oracle-cloud` polls (`ingress_authentication: []`, correctly); `oracle-onpremise` and `oracle-web` accept push, each declaring `[shared-secret]`. The cloud drain terminates four ways and throws on any status that is neither 200 nor 204 |
| **EVIDENCE** | `manifest.yaml:316/:348/:365`, `:340/:362/:377`; `OhipBusinessEventQueue.cs:79-120` and `:140-161` read in full |
| **GAP** | ⚠ **Chapter 03 §2.4 claims *secret AND allow-list* for both push flavours. ONE mechanism is declared, not two** — a factual correction to the chapter; the REFUSED-at-install grade survives it and the count does not. **And `oracle-onpremise` declares `onsite-room-status` with no reference provenance at any layer** — `payload-provenance.md` §13 confirms it from the implementations, and the on-premise flavour sources nothing for it at model, service **or** transport level |
| **OWNER** | §2.4's sentence — **the chapter's author / the architect**, with the measurement here. The `onsite-room-status` declaration — **the declaration's author or the owner**; two readings, and §5 refuses to pick between them |

---

## Capabilities — PROVEN, both directions

| | |
|---|---|
| **PROVEN** | Four connector-owned capabilities declared and **all four enforced**. Declared-but-unenforced: **0**. Enforced-but-undeclared in `pms-oracle/`: **0** — the package contains **zero `RequireAsync`**, with a positive control proving the sweep works. `ConnectorDispatcher.cs:82` reads `Implements` and refuses before any session opens |
| **EVIDENCE** | `capability-ledger.md`, keyed on the authorization **call** and not door-by-door — the shape FF's `StayLifecycleService` case warns about, where a per-method count reported four of five unenforced |
| **GAP** | The Hub's `quarantine.read` is **enforced on the Hub's surface and declared by no connector** — the Hub's boundary, not Oracle's. `IntegrationNotConfigured` has **one** use, and page 75 §7 Q4 asks to reclassify it as a Fault |
| **OWNER** | `quarantine.read` — **the Hub's stream**. §7 Q4 — **the planner**, open |

---

## Diagnostics — PROVEN as written, UNEXERCISED by construction

| | |
|---|---|
| **PROVEN** | **All seven of Chapter 13's fields reach the record.** `operation` · `resource` · `duration_ms` · `result` · `rate_limit` · `authentication` · and `package` carrying the chapter's *connector*, enriched once. **CLEF to stdout**, which the Supervisor drains and attributes. **One record per invocation**, at the one chokepoint every kind passes through |
| **EVIDENCE** | `ConnectorLogging` and `ConnectorObservations` in the SDK; `InvocationDispatch` times the dispatch; **302 tests pass**, including three Theory arms on the one new establishable fact — `429 → true`, `503 → false`, `401 → false`, two *different* outcomes on the false side so a constant `true` fails. The format is **ruled**: ADR 0292 read in full, and the reader's own CLEF keys at `logs/parse.rs` |
| **GAP** | **UNPROVED BY CONSTRUCTION.** `pms-oracle` is installed and **does not run** — nothing can tell the Supervisor to start a connector, so no session opens and none of this has executed outside its own tests. *Said at the site in that form, because an omission invites the next reader to assume somebody covered it.* **And `rate_limit`'s keying cannot be proven today**: only a 429 maps to `UNAVAILABLE`, so *keyed on the status* and *keyed on the outcome* are extensionally identical and no fixture can separate them. The status is preferred because it stays correct when a second cause joins that member |
| **OWNER** | the calls are mine and landed. **Their exercise waits on L7 — II** |

> **`join` and `dedupe_key` keep the correlation id as their `resource`, and the
> reason is named rather than left looking like an oversight**: they act on
> bytes the Hub already holds and this connector knows no name for them.

---

## The reference trace — PROVEN, and it closed two findings without a ruling

| | |
|---|---|
| **PROVEN** | Six payload kinds cited at `file:line`; three flow passes; seven reference defects and five platform divergences tabled; the `housekeepingStatus → hkStatus` mapping that settled an ambiguity in `CLAUDE.md` itself |
| **EVIDENCE** | `payload-provenance.md` §§1–13, scoped to `pms-integrations` and measured in Python — 605 `.java` under the scoped root, 45 on oracle's on-site surface, 87 for the scheduling sweep |
| **GAP** | **None. `Reservation.java`'s remaining 209 lines are CLOSED BY §2**, which covers `ohip-reservation`'s field mapping from the model's side — named rather than left unstated |
| **OWNER** | mine, and closed |

**Two Direction-B findings, both closed, and by different mechanisms:**

| | |
|---|---|
| **finding 1 — the two-part on-site join** | **CLOSED to the reference.** The concept is the source's own word, **`merge`**, not my `join`: `OracleOnPremiseReservationDao.java:21` declares `fetchForCheckInDataMerge`, `…ServiceImpl.java:115` calls it, `:116-119` rejects with `dumbReason("NO VALID DATA FOUND TO MERGE")`, `:122-126` completes `reservationId`, `phone1`, `phone2`, `email` and `departureDate`. *An on-site check-in genuinely arrives incomplete and is completed from a prior message — concept, logic and flow.* **And ours is the better architecture**: we return the half as a first-class outcome for the Hub to pair, where the reference discards it |
| **finding 2 — the polling tiers** | ⚠ **WITHDRAWN as a category error of mine.** I wrote that a frame sits below an ADR in precedence and so could not source a value. **Precedence bites when a frame CONTRADICTS a ruling or stands in for one; here the rulings DELEGATE** — ADR 0246, *"Temporal is the clock and not the policy"*, leaves `CONN-Q12` standing: *"the connector's `NextPollAfter` is the policy"*. **And the connector exercises it**: `OracleCloudAdapter.cs:180`, `QueueDrainInvocation.cs:126`, with `InvocationDispatchTests:162-165` and `:386` asserting the present and absent arms |

---

## ⚠ Three behaviours no chapter states — and these are the only ones left

| | |
|---|---|
| **GAP** | ✅ **CLOSED — SWEPT AGAINST THE REFERENCE, and the row no longer has no owner.** *It read "three behaviours no chapter states... the only row with no owner", which was a measurement of OUR documents when a connector's concept comes from the reference.* **Decision 2 — the partial-drain return — is TRACED**: `OracleCloudEventServiceImpl.java:66` saves each event INSIDE the loop and `:80` returns `null` on a transport failure, so *what has been read is kept, never discarded* is the reference's own principle — and ours is that principle through the Hub's inbox, which ADR 0128 §5 requires. **Decision 1** is ours, a safe optimisation of a traced flow: the reference loops until 204, we stop one call earlier and throw on any other status, fixing its infinite loop. **Decision 3** is a **divergence with the traced alternative now cited** — OHIP supplies `businessEventId.id` and we hash the bytes. *And `PageSize = 20` turns out to match the reference's `limit=20` exactly* · `payload-provenance.md` §14 |
| **EVIDENCE** | `OhipBusinessEventQueue.cs:116`, `:88-98`, `:168-176` — each documented at the line, none in any chapter in either repository, measured with controls |
| **WHY IT MATTERS** | **(2) is a loss-versus-duplication policy and a correctness claim about vendor paging.** If OHIP's 200 ever returned a partial page, returning the earlier pages would silently drop the remainder of the queue. The argument is sound and is stated nowhere a designer would meet it |
| **OWNER** | **unassigned, and that is the finding.** It is the `$extra` half: *nothing in the estate would ever report that these were never designed, because every check asks whether what was EXPECTED is present.* **A design decision for the planner or the chapter's author; not a defect in the code, which is why it is a row and not a blocker** |

---

## The Connector Runtime Supervisor — not mine, and its state recorded

| | |
|---|---|
| **PROVEN** | Binary present. **It binds a listener AND registers with the Kernel** — `9e5c2731` — through `packages/platform-mtls`, which `14ffea06` extracted so ADR 0040 had a legal answer for a second Rust process. Its address exists: `15160 / 25160`, `148eff65`. **And the Kernel half of L7 landed today** — `c475be30`, *"ADR 0372: `AssertAdmission`, because the Kernel issues this and nobody relays it"* |
| **EVIDENCE** | `main.rs:64-66`, `:212`, `:222`, `:232-243`; `kernel.rs:79`, `:88`; `Cargo.toml:97`; `AssertAdmission` in `server/packages/mod.rs` and `read.rs` |
| **GAP** | ⚠ **`main.rs:37-45` still declares the mTLS channel a MEASURED BLOCKER** — *"Nothing in `packages/` holds a Rust equivalent"* — 180 lines above the code that uses it. A reader meeting that line concludes they are blocked on architecture that landed. **And `dev.py` does not start it.** The Supervisor half of L7 is unbuilt |
| **OWNER** | **II**, assigned. Reported rather than edited, because the correction belongs with its reason in their tree |

**And the owner's actual question has two verbs, so two answers:**

```text
INVOKED                    needs a SESSION. IConnectorChannels opens the
                           Connector Protocol session — and Program.cs:213
                           registers NoConnectorRuntime, "a refusal, not a
                           stand-in session". So NO, not today
RUN as a contained process NO. The Supervisor's, and nothing else does it
```

> ⚠ **I withdrew a claim here and it is kept because it was relayed.** I wrote
> *"the Hub dispatches an invocation in-process, so Part B may not be blocked on
> the Supervisor at all."* **It is blocked.** `ConnectorDispatcher.cs:82` is a
> refusal the Hub makes in its own process — true — and the comment four lines
> above says *"before any session opens"*. **The word was there and I read past
> it.** Adjacency mistaken for specificity: true about the refusal, published
> about the dispatch.

---

## The Hub's side, and configuration

| | |
|---|---|
| **PROVEN** | Three `PackageLifecycle` handlers live at `Program.cs:399-401`; `SetIntegrationConfiguration` at `service.proto:129`, reached from `src-tauri/commands/integrations.rs` by both Operations Center and Software Center |
| **EVIDENCE** | Read at the line — the `CONN-Q13` register row records a revert and the source does not |
| **GAP** | None on this side |
| **OWNER** | — |

---

## Part A · Part B · Part C

| | |
|---|---|
| **Part A** | **STRUCTURAL.** `CONN-Q20` — no harness can mount a package UI. **UNPROVED BY CONSTRUCTION.** **OWNER: the OWNER**, open since 2026-09-10 |
| **Part B** | **FORM RULED** — ADR 0369. Platform-served **APPLIES**; the person column **N/A** while the package exposes no human-operated surface, *with the reason at the row*. **The precondition is now met for the first time**: an installable archive exists and is installed. **UNPROVED BY CONSTRUCTION** — no session opens. **OWNER: II**, L7's Supervisor half. **Never written as *"Connector Part B = N/A"*** — that recreates `CONN-Q15`'s collapse and discards the served proof |
| **Part C** | **N/A — `CONN-Q92` CLOSED**, ADR 0369 Addendum 1. **Not awaiting invention**: a connector is not in ADR 0358's Part C taxonomy at all. No `part-c-coverage.md` is written, and its absence is the ruling |

---

## Connector Certification — Chapter 18's eight dimensions

> **The authoritative home for WHAT certification contains is
> `Chapter 18_ complete engineering implementation blueprint.md:661-679`.**
> `ARCH-Q60`: one live rule, one authoritative home. **This ledger cites that
> chapter and records the EVIDENCE per dimension** — it does not restate the
> definition. *Four files in `docs/chapters/` are named "Chapter 18", so the
> citation carries the full title.*

**The eight are a CLOSED REQUIRED set**, and **no exception is claimed**: all
eight are recorded as required, four are unproved with named reasons, and none
is reported as inapplicable.

| | dimension | evidence, or the named gap |
|---|---|---|
| 1 | **Mock APIs** | ✅ four `HttpMessageHandler` fixtures answering as OHIP — `ConnectionTestTests:207` · `Hub:170` · `OhipAccessTokenTests:118` · `OhipBusinessEventQueueTests:252` |
| 2 | **Real sandbox APIs** | ❌ **ZERO.** **UNPROVED BY CONSTRUCTION** — needs Oracle OHIP sandbox credentials, which are **the owner's** and exist on no machine here |
| 3 | **Authentication** | ✅ outbound: `OhipAccessTokenTests` · `OhipPasswordGrantTests` · `OhipCredentialTests`. Inbound: `ingress_authentication` asserted by `ManifestDeclarationTests`. ⚠ and §2.4 above — the chapter claims two mechanisms where one is declared |
| 4 | **Retries** | ◐ **HALF PROVEN, HALF OWED — `CONN-Q93`.** **(a) no independent mechanism: PROVEN** by `DelegationInvariantTests`. **(b) participation at the Hub boundary while the Hub retries: OWED.** ADR 0128 §5 settles who implements the mechanism and *"does not settle whether a particular connector interacts correctly with"* it. **OWNER: II** for the boundary |
| 5 | **Rate limits** | ✅ **BUILT AS RULED — `CONN-Q94`.** `TooManyRequests → UNAVAILABLE`, with the cause and the operator's remedy in the **detail**. `UNREACHABLE` is categorically wrong: *"a 429 proves enough communication occurred to make that claim false."* Three Theory arms, chosen so a constant cannot pass |
| 6 | **Disconnections** | ✅ six sites, **injected rather than asserted in prose** — `OhipBusinessEventQueueTests:108` asserts the drain throws, `:269` injects *"the connection was reset"* |
| 7 | **Webhook processing** | ◐ **PARTIAL.** The **declaration** is proven — `accepts_push` asserted at `ManifestDeclarationTests:128` — and the on-site normalisers are tested. **An inbound push through the real ingress is the Hub's path and is not exercised here.** **OWNER: the Hub's stream**, with L7 |
| 8 | **Offline mode** | ◐ **HALF PROVEN, HALF OWED — same ruling, same two halves as row 4.** **(a) no offline queue of its own: PROVEN** — no persistence of any kind reaches the connector, and `db_connections: 0` is declared beside it. **(b) participation across the Hub's durable/offline handling and the restoration of connectivity: OWED. OWNER: II** |

**The negative invariant is PROVEN and was shown able to fail.**
`DelegationInvariantTests` derives every `backend/**/*.cs` from its own
compiled-in path, asserts a denominator of **≥ 40**, carries an `HttpClient`
**positive control**, refuses **seventeen named primitives** — *because "no
retry" is not checkable and `Task.Delay` is* — and exempts the one paging loop
**by name, as an equality**. **Two probes, both SPLIT**: a `ConcurrentQueue`
insertion fails the primitive test and not the loop test; a second loop fails the
loop test and not the primitive test. *A split is the signature a specific guard
should give.*

---

## The dispositions, closed

```text
PROVEN      the cut and the install · capabilities both directions · the
            reference trace · diagnostics as written · certification 1, 3, 5, 6
            and the (a) halves of 4 and 8
UNBUILT     (b) on 4 and 8 · webhook through the real ingress · dev.py starting
            the Supervisor · main.rs's stale blocker        ALL: II, or the Hub
UNRULED     nothing on this connector's side. CONN-Q92, CONN-Q93 and CONN-Q94
            are all ruled. CONN-Q20 is the OWNER's and is Part A's reason
UNPROVED    BY CONSTRUCTION  Part A · Part B · dimensions 2 and 7 · every
                             logging call, because nothing starts this process
            BY CHOICE        none outstanding — Direction B was named partial
                             by choice and has since been completed
            BY OMISSION      none
```

> **The three undesigned drain decisions are the only row with no owner**, and
> that is stated as the finding rather than left as a blank: nothing in the
> estate can report an undesigned behaviour, because every check asks whether
> what was expected is present.

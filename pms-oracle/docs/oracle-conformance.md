# The Oracle connector — conformance against its own chapters

**What this is.** Chapter 03 is the conformance target, and its §2 makes **nine
claims about our own code**. Every one asserts that a reference defect is
different here, and **a claim about what a system does NOT do has no failure
mode** — the `# Errors` class. This page checks all nine against the
implementation, then runs the opposite direction: behaviours in the code that no
chapter states.

**Owner's choice, 2026-10-03: the WHOLE surface, both directions.**

```text
DIRECTION A   concept -> code     the nine claims, and §1's architecture
DIRECTION B   code -> concept     behaviour nobody designed is as much a
                                  finding as design nobody built
```

---

## 0 · Scope, stated — including what was not reached

**Read in full**: `03-the-oracle-connector-design.md` §§0–2 and §4's gate ·
`OhipBusinessEventQueue.cs` ·
`OracleWebRoomServiceImpl.java` (89) · `manifest.yaml`'s declaration blocks ·
`services/connector-supervisor/src/main.rs` §"What is NOT here yet" and its
`serve`.

**Measured by derived census, not by a handed-down list** — §1 below, and two of
the architect's figures came back different.

**Positive controls**: the `RoomId` sweep returned seven hits in files that *do*
name it, which is what makes the zero-assignments result a measurement rather
than a broken pattern; the `RejectionReason` sweep returned 31 sites across five
reasons.

**NOT REACHED, by choice and with the reason** — Direction B is **complete for
the drain path and partial for the other 40 backend files**. The nine claims
were the named priority and they are where the `# Errors` exposure is; a full
48-file behaviour sweep against three chapters is a second pass. **Named as
partial rather than presented as whole**, because an omission reads as something
a diligent person would have covered.

---

## 1 · The surface, derived — and two of the brief's figures are wrong

```text
                       DERIVED    the brief
Adapters                     3            3
Authentication               7            7
Capabilities                 2            2
Hosting                      9            9
Integrations                 9            9    1 root + Cloud 6 + OnSite 2
Normalisation               11           11
Vocabularies                 6            6
backend/ root                1            -    Program.cs, in no row
                     ---------
backend/ .cs                48           47
tests/ .cs                  28           28
ui/ .ts                      7        1,676
ui/ .tsx                     0            -
```

**⚠ `1,676` is `node_modules`.** The tracked UI is **ten files** — five
TypeScript sources, one test, four configs:

```text
application.ts · chrome.ts · configuration.ts · main.ts · styles.ts
tests/form.test.ts · package.json · package-lock.json · tsconfig.json
vitest.config.ts
```

> **So §4's caution — *"do not treat the 1,676 ts/tsx as in scope by file
> count"* — guards a population that does not exist.** The UI is small enough
> to read entirely, which **widens** what this pass can honestly cover rather
> than narrowing it. Part A stays structural for `CONN-Q20`'s reason — no
> harness can mount a package UI — and that is a statement about *mounting*,
> not about scale.

**And `backend/` root holds one file the breakdown omits.** A census of
subdirectories misses what sits above them; 47 is the sum of the rows and 48 is
the directory.

---

## 2 · DIRECTION A — the nine claims, each checked against the code

**Seven hold as written. Two need correction: one factual, one attributional.**

| | claim | verdict |
|---|---|---|
| 2.1 | no write whose success is in doubt | **holds in substance**, wording over-reaches |
| 2.2 | the drain's terminator is *"not connector code"* | ⚠ **grade earned, mechanism MISATTRIBUTED** |
| 2.3 | nothing here ever sets `room_id` | **holds, and is stronger than claimed** |
| 2.4 | declares secret **and** allow-list for both push flavours | ⚠ **FACTUALLY FALSE** |
| 2.5 | no shipped secrets | **holds** |
| 2.6 | a derivation without a zone fails | **holds** |
| 2.7 | the connector owns no database | **holds** |
| 2.8 | there is no cache here at all | **holds** |
| 2.9 | an unmapped value is a rejection naming it | **holds** |

### ⚠ 2.4 · The factual claim is false, and the grade survives it

The chapter: *"`pms-oracle` declares secret **and** allow-list for both push
flavours."*

```text
manifest.yaml:340   oracle-cloud       ingress_authentication: []
             :362   oracle-onpremise   ingress_authentication: [shared-secret]
             :377   oracle-web         ingress_authentication: [shared-secret]
```

**One mechanism, not two, on both push flavours.** The `[]` on `oracle-cloud` is
correct — it polls and accepts no push.

> **The grade — REFUSED at install — is still earned**, because what the
> platform refuses is a connector declaring *none*, and both push flavours
> declare one. **Only the count is wrong.** A reader auditing our posture from
> this sentence would believe two independent defences stand where one does.

**And the manifest itself flags this field as the one at risk**, at `:279`:
*"`ingress_authentication` is the one field the code does not determine"* — so
it is the one declaration no test can derive, which is exactly where a claim
about it goes unchecked.

### ⚠ 2.2 · The loop IS connector code, and it is a good one

The chapter: *"the drain's terminating condition is the Hub's paging facility,
**not connector code**, and an unexpected status ends the poll with a recorded
error rather than a spin."*

**`OhipBusinessEventQueue.cs:79` is `while (true)`, in the connector.** Four
exits, every one honest:

```text
:103   page is null          OHIP's 204 - "the source's own word rather than
                             on a count we chose"
:116   page.Count < PageSize a short page ends it one call earlier
:145   not 200, not 204      THROWS, naming the status
:155   200 with no items     returns [] - "ends the drain rather than looping
                             forever on it"
:81    cancellation checked INSIDE the loop
```

> **So the second half of the claim is exactly true** — an unexpected status
> ends the poll with an error, which is the precise counterpart to the
> reference's loop that never terminates on a status that is neither 200 nor
> 204. **The first half misdescribes where the termination lives.**

**Why the misattribution matters more than it looks.** A reader checking *"does
our connector contain a drain loop?"* is told it does not. It does — and it is
the one piece of connector code where an unbounded loop against a vendor is
possible. **The bound is OHIP's answer plus the invocation's cancellation token,
not a Hub paging facility**, and the file says so in as many words.

### 2.3 · Holds, and the code is stronger than the sentence

Zero `RoomId` **assignments** in 48 files. The three references are the vendor
records (`OhipHousekeepingRoom:60`, `OhipReservation:23`) and one normaliser:

```csharp
CloudRoomStateNormaliser.cs:39   if (string.IsNullOrWhiteSpace(room.RoomId))
                          :41       return Reject(RejectionReason.MissingRequiredField,
                                                  "roomId", room.RoomId);
                          :60   ExternalId = room.RoomId,
```

**The PMS's room number reaches `ExternalId` and a rejection, and never a
canonical field** — and the rejection carries **the field name and the raw
value**, which is page 75 §1's requirement met at the line rather than claimed.

### 2.5 · Holds — and the three hits are names, not values

```text
OhipCredentials.cs:50  ApplicationKeySecret = "application-key"
                  :53  ClientSecretSecret   = "client-secret"
                  :56  PmsPasswordSecret    = "pms-password"
```

**Three secret *identifiers*, zero secret *values*.** *A search for
`secret = "` finds the constants named for secrets — the explanation containing
the token, for the third time in this audit.*

### 2.6 · Holds, and it refuses the near-miss by name

```text
PropertyClock.cs:61   "An offset such as '+05:30' is not a zone. Rejected
                       explicitly rather than..."
                :68   TimeZoneInfo.TryFindSystemTimeZoneById(...) ? ... : <failure>
```

**An unresolvable zone fails closed**, and an offset — the thing that looks like
a zone and cannot express daylight saving — is refused on its own branch.

### 2.7 · Holds, by an explicit zero

No `database:` block; **`manifest.yaml:147  db_connections: 0`**. The claim is
carried by a declared zero rather than by an absence a reader has to trust.

### 2.8 · Holds — seven dictionaries, and all seven are vocabularies

```text
CloudStayStatus:27 · FrontOfficeCodes:17 · OnSiteStayStatus:55
RoomConditionCodes:35 / :44 / :56 · RoomStayStatusCodes:36
```

**Every one is `static readonly Dictionary<string, …>` with
`StringComparer.Ordinal`** — a compile-time vocabulary, holding no fetched data,
no expiry and no vendor-derived key. **No cache.**

> **And the uniform `StringComparer.Ordinal` is `CONN-Q25` applied in all seven
> places** — where the reference mixes `equals` and `equalsIgnoreCase` on one
> field seventeen lines apart, and its two flavours disagree about which casings
> they accept.

### 2.9 · Holds — five reasons, 31 sites

```text
MissingRequiredField      10
UnknownStatus             10
UnreadableValue            6
PropertyMismatch           4
IntegrationNotConfigured   1   <- page 75 §7 Q4 asks to reclassify this as a
                                  Fault, and the single use is its subject
```

---

## 3 · DIRECTION B — behaviour no chapter states

**Complete for the drain path.** Three decisions are implemented, deliberate,
documented at the line, and **in no chapter**:

| behaviour | at | why it is a Direction-B finding |
|---|---|---|
| **the short-page early exit** | `:116` | *"asking again would be a request whose answer we already know… this stops one call earlier"* — a divergence from the reference's drain-until-204 that §2.2 does not mention while describing termination |
| **the partial-drain return** | `:88-98` | on a transport failure **with items already taken**, it RETURNS them rather than throwing, *"the interim reading documented above"*. That is a loss-versus-duplication policy, and §2.2 states only that loss is inexpressible |
| **the dedupe key taken from the BYTES, not the element** | `:168-176` | *"those are the same bytes a quarantined payload is re-submitted with… so a record drained and later re-submitted cannot become two facts"* — re-submit symmetry, which ADR 0255 §2 covers and no chapter does |

> **The middle one is the consequential one.** *"Nothing was taken by the call
> that failed — a page is read whole or not at all — so what is here is
> intact"* is a **correctness argument about vendor paging**, and it is load-
> bearing: if OHIP's 200 ever returned a partial page, returning the earlier
> pages would silently drop the remainder of the queue. The argument is
> sound and it is stated nowhere a designer would meet it.

**NOT REACHED**: the other 40 backend files, by choice (§0). *A chapter home
exists for the vocabularies, the normalisers and the hosting surface via §1.1–1.3;
whether every behaviour in them is stated there is the second pass.*

---

## 4 · The Supervisor — read and reported, not built

**⚠ The brief's premise expired yesterday, and the file's own header still
declares the blocker.**

```text
BRIEF, today    "binds NO listener - does NOT register with the Kernel"
MEASURED        9e5c2731, 2026-10-02, "Item 2: the Supervisor registers with
                the Kernel - and the interval was never ours to choose"
```

```text
main.rs:64-66    uses listener::BindAddress, registration::keep_registered
       :212      listener::credential(settings.certificates())
       :222      tracing::info!("connector_runtime_listening")
       :232-234  kernel::Kernel::at(settings.kernel(), settings.certificates())
       :236      Endpoint::allocated(profile)
       :243      tokio::spawn(registering)
kernel.rs:79     hotelos_platform_mtls::endpoint(url, Some(certificates))?
         :88     .register_service(pb::RegisterServiceRequest { ... })
```

**It binds and it registers.** `dev.py` still does **not** start it — only
`dev_settings.py:125` carries the port row — so the brief's third clause holds.

### ⚠ And `main.rs`'s own §"What is NOT here yet" contradicts its body

`main.rs:37-45` declares, as a **MEASURED BLOCKER**:

> *"what is missing is the outbound mTLS channel to carry them. ADR 0040 forbids
> this crate building one… and the canonical transport is `hotelos-kernel`'s
> `clients/transport.rs`, which this crate may not depend on. **Nothing in
> `packages/` holds a Rust equivalent.**"*

**`packages/platform-mtls` is that equivalent**, `Cargo.toml:97` depends on it,
and **`main.rs:231` cites the commit by hash**: *"`14ffea06` extracted it from
the Kernel so that rule had a legal answer for a second Rust process."*

> **One file, 180 lines apart, declaring a blocker and resolving it.** This is
> the stale-justification class in its most legible form — *a justification that
> decays into an argument about the thing it justifies* — and the direction here
> is the costly one: **a reader meeting `:37` concludes they are blocked on
> architecture when the architecture landed.**

**Not fixed. It is `services/connector-supervisor/`'s and the Supervisor is II's,
assigned** — reported so the owner of the file can correct it with the reason
recorded, which is how this repository handles a stale sentence in somebody
else's tree.

### Can an Oracle connector be launched and watched without it?

**The owner's actual question, and it has a measurable answer in two halves:**

```text
INVOCATION   YES, without the Supervisor. The Hub dispatches in-process:
             Program.cs:399-401's three PackageLifecycle handlers, and
             ConnectorDispatcher.cs:82 reads `Implements` and refuses
             before dispatch
PROCESS      NO. A supervised child - spawn, contain, watch, restart - is
             the Supervisor's, and nothing else in the estate does it
```

> **So "launched and watched" splits into two different verbs**, and the answer
> differs by verb. Oracle's capabilities are reachable today through the Hub
> without the Supervisor existing; **an Oracle connector running as its own
> contained process is not.**

---

## 5 · The cut — what refuses it, exactly

**`pms-oracle`'s own tree is CLEAN** (`git status --porcelain -- pms-oracle/`
returns nothing). The dirty entries are other packages':

```text
HosPilotOS   6    Cargo.lock · check_wire_parsing.py · InboxCountsTests.cs
                  + three untracked docs.  NONE is mine
HotelOsApps  24   guestops 5 (Permissions.cs, part-b-drive-list,
                  part-c-coverage, signoff-ledger, measured.ts)
                  roomcare 19 (recorded/*.json)
                  NONE is pms-oracle
```

**What refuses the cut is not the dirty tree — it is that nothing assembles a
package directory.**

```text
hopkg's verbs   keygen · sign <package-dir|package.hopkg> --key <path>
                usage.rs, read in full. There is NO pack verb
the key         %USERPROFILE%\.hotelos\dev-keys\, per machine, outside every
                git tree - dev_signing.py, ruled 2026-09-04
the assembly    SWEPT scripts/ and deployment/ for "hopkg" across
                *.ps1 *.py *.mjs Makefile: three hits, all tooling
                (check_source_standards, dev_settings, dev_signing) and one
                installer payload artefact. NO packaging step for any package
```

> **`hopkg sign` signs a *prepared* directory. Nothing in the estate prepares
> one** — the backend publish, the UI module build and the layout that
> `Manifest::parse` expects are not wired into any script.

**So the cut is UNBUILT rather than blocked**, and it is not one command away.
Scope of that claim stated: the sweep was two directories and four file types;
a packer living elsewhere, or as a Rust binary with another name, is outside it.

**And the clean-tree rule's reason does not reach this case anyway** — *"a
signed artefact built from a dirty tree carries somebody's uncommitted work"*,
and an artefact containing only `pms-oracle/` cannot carry GuestOps' or Room
Care's. Recorded because the next person will meet 24 dirty entries and
reasonably stop.

---

## 6 · Every gap, classified three ways, with an owner

**UNRULED — the planner's. Named, never filled.**

| | |
|---|---|
| `CONN-Q92` | a connector's **Part C** is unruled. Both obvious fillings rejected by name: *"N/A"* and *"invoke its Hub API without UI"*. **No part-c-coverage is manufactured here** |
| `ARCH-Q63` | a **platform process** is outside `APPS-Q4`. The Supervisor has no ruled signoff shape; `OpenSession`/`DescribeProcess` was named a candidate and deliberately not ruled in |
| `CONN-Q20` | **the owner's**, open since 2026-09-10 — no harness can mount a package UI, which is why Part A is structural |

**UNBUILT — ours, and whose.**

| | owner |
|---|---|
| a package **assembly** step for any `.hopkg` | **unassigned — the first thing a cut needs**, and it is nobody's today |
| `dev.py` does not start the Supervisor | II, with the Supervisor |
| `main.rs:37-45`'s stale blocker | II, reported here |
| the envelope relay — `open_session` forwards no frames | II, `main.rs:50-54` |
| a guarantee's `AmountTaxBasis` / freshness maximum home | open, per-integration configuration — ADR 0150 |

**UNPROVED — and never by omission.**

| | why |
|---|---|
| **Part B platform-served** | **BY CONSTRUCTION** — it needs the installed `.hopkg`, and §5 establishes nothing assembles one |
| **Part A** | **BY CONSTRUCTION** — `CONN-Q20`, the owner's |
| Direction B over 40 backend files | **BY CHOICE**, reason in §0 |
| ADR 0368 §15's two installed-mode probes | **BY CONSTRUCTION** — no machine here registers `NT SERVICE\HotelOS<X>` |

---

## 7 · The document set, mapped onto GuestOps'

| GuestOps | here | |
|---|---|---|
| `app-surface-audit` | **this page** | a connector's surface is its integrations and capabilities, not screens |
| `capability-ledger` | `capability-ledger.md` | current — ADR 0369's two columns, both prohibitions recorded |
| `part-a-certificate` | **not written** | structural by `CONN-Q20`. Its reason is §6's, not an omission |
| `part-b-drive-list` | **not written** | needs the installed package — §5 |
| `part-c-coverage` | **REFUSED** | `CONN-Q92` forbids both fillings. *Writing one would be manufacturing the thing the ruling rejected* |
| `signoff-ledger` | `signoff-ledger.md` | the row citing this page |
| — | `payload-provenance.md` | §§1–13, no GuestOps counterpart: an application has no vendor to trace |
| — | `conn-q52-connector-evidence.md` | — |

> **Two of GuestOps' six do not apply to a connector and one is forbidden.**
> That is three absences with three different reasons, and collapsing them into
> *"the set is incomplete"* would hide which are mine.

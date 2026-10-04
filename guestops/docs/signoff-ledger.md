# GuestOps — the signoff ledger

**What is proven, by what instrument, and what is not — with every gap owned.**

This is the fifth document and it holds what none of the other four does: the
single sheet that says whether GuestOps can be signed off, and who owns each
thing that stops it. The others are its evidence.

```text
part-a-certificate.md   drawing fidelity, on FIXTURES.  Approved 2026-09-05,
                        cb363a8c - and Part A is NEVER sign-off
part-c-coverage.md      does the API ACCEPT every combination the contract
                        admits?  12 writes, a scratch database, dropped
part-b-drive-list.md    the owner's walk, control by control
capability-ledger.md    what each button does, before testing
```

**The order is ADR 0358's addendum and is not mine to reorder**: A, then C's
data, then B on the installed build.

---

## 0 · THE SIGNOFF — every row, its disposition, its evidence

**A signoff leads this document; the sections below are how each row was
reached.** Dispositions as of 2026-10-03.

| | Row | Evidence |
|---|---|---|
| **PROVEN** | **Capabilities.** 8 declared, 21 enforcement sites, none enforced-but-undeclared, none declared-but-unenforced | derived from the authorization CALL on both sides · §1 |
| **PROVEN** | **Part A fidelity, FLAT at 56** across three rounds of change — `Today.stale`, the count fix, DD's hostDouble over 52 files | 17 frames, 0 unreached, exit 0; arithmetic closes 31+1+1+1+22 · `d4efde97` |
| **PROVEN** | **`ContextNeighbours` reaches Context.** It consumed `Resolution` alone and was refused for fields it never read | `c680ea8a`, build 0/0 |
| **PROVEN** | **The booking's refusal is understood and repaired.** `GetPropertySummary` is user-scoped by ADR 0212; `AtAsync` now reads the platform-scoped `GetPropertyFacts`, and an unset zone returns `StayTime.None` rather than throwing | `136052a3`, build 0/0 in a slot I held |
| **PROVEN** | **ADR 0211's thirteen-day precondition.** A GuestOps → Context call HAS succeeded on this machine | the architect's measurement: `GetOperatingDay` 358 mentions, all 16 user-scoped refusals on `GetPropertySummary`. *Not my stated condition — I had permitting code and no observed success, and was right not to close it on that alone* |
| **PROVEN** | **`Today.stale` no longer throws on the first screen of the walk** | `TodayView` sends five keys and not `stale`; positive control: the same view's `connected` IS sent · `d1d01260` |
| **PROVEN** | **`Reinstate…` is wired to the dialog the owner drew.** The build greyed it on a reason that was TRUE on 2026-09-24 and that `247c854` falsified on 2026-09-28 — `fRI1` draws the dialog to its end | `200fe4da` · `57320c58` · tsc 0, vitest 273, dotnet 0/0, 340 tests. Probe 1 split 1-of-264; probe 2 split 4-of-273, departure arm green |
| **PROVEN** | **The correction dialog is driven by a test at all.** It was driven by none — both arms, including the departure shipped 2026-09-24 — and a mutation found it rather than a reading | `57320c58`; the probe that found it left 264 passing |
| ~~**UNRULED**~~ **→ APPROVED, UNBUILT** | ~~13 capabilities whose controls the design draws enabled and the application cannot perform — **the owner's**~~ **This row was wrong about WHOSE they are.** The owner's `docs/mockups/` IS the authored specification (owner, 2026-10-04), so the design is settled and the deliverable is a build that matches it. **They are mine, not a question** — §5 dispositions each one | the old reading is kept because it is what I sent up as a question, and a reader meeting only the correction cannot see that the menu was offered · `7879f075` |
| **PROVEN** | **The day list's `＋ assign` reaches the sheet.** Two files already asserted the route and nothing wired it | `a36bbc89` · tsc 0, vitest 276; probe 2 failed of 276, a split |
| **PROVEN** | **GUEST-Q3's clear reaches a door**, and **the settings write reaches a door** — both services were complete, tested and callable by nothing | `482d23be` · `b468fc05` · `4b341985` · probes split 2-of-281, 1-of-7, 1-of-8 |
| **UNBUILT / REPORTED** | **§5's census now closes at 4 BUILT · 3 MATCHES · 4 UNBUILT · 2 SURPLUS · 1 CORRECT-BY-CONSTRUCTION · 1 REPORTED · 1 infrastructure = 16** — derived from the table rather than counted, after my first classifier matched `BUILT` inside `UNBUILT` and reported nine | §5 |
| | ⚠ *This said **7 ROWS**, and before that **12 controls**. The row count is derived from §5's table and stated with its buckets; the CONTROL count is not derivable — the setup card greys whatever labels its panel declares.* | |
| | ⚠ *This said **12 controls**. The row count is 8, now 7, and is derived from the table; the CONTROL count is not derivable — the setup card greys whatever labels its panel declares, so a figure stated here would be a number nobody measured.* | |
| **UNRULED** | **One divergence INSIDE the owner's own pages.** `fRI1`'s prose draws the compressed range `19 – 20 Aug`; the owner ruled the LONG form on 2026-09-20 against mockup 07 B, eight days earlier | §5 · the owner's, and asserted in both directions meanwhile so it cannot close quietly |
| **UNRULED** | **The staleness mechanism** that `Today.stale` is drawn for, and nothing computes | planner / owner |
| **UNBUILT** | **No booking has been COMPLETED end to end.** The refusal is repaired; the proof is a run | the owner's, on the installed build |
| **UNBUILT** | **0.3.5 is cut and not installed**, so `protected_personal_data` is unproven at install | **MM** · `sha256:9c8eed08…` |
| **UNPROVED — BY CONSTRUCTION** | the gRPC door's own refusals; every NESTED response field, since the wire/fixture comparison reads top-level keys only; `attention`'s shape, answered through a paging helper | §2a |
| **UNPROVED — BY CHOICE** | the Part C drivers are not re-pointed at the wire. The module door's `Draft` is nine fields with five hardcoded null — a narrower aperture, so re-pointing would silently stop driving most positions | §3 |
| **NEVER — by omission** | none outstanding | — |

> **What this signoff does NOT say.** It does not say a control works — that is
> Part B, and Part B is the owner's walk. Every row is a statement about what was
> measured and by what, and the two UNBUILT rows are what stand between here and
> a signed Part B.

**And the number the owner is asked for is 13, not 22** — narrowed three times,
each time by deriving rather than counting: 22 differing nodes → 21 controls
(one node is label text) → 16 reasons, not the 15 a grep finds, because the
walk-in's is composed by `missing(draft)` at render → **13**, after three turn
out not to be capability gaps at all: a property fact, a data state and an
incomplete form. *They are drawn as what they are rather than excluded silently,
because an exclusion taken on trust is not a decision.*

---

## 1 · Capabilities — keyed on where authorization is ENFORCED

Derived rather than hand-listed: the manifest side read from `manifest.yaml`,
the enforcement side from the authorization **call** (`RequireAsync`) across the
whole bundle. *A door-by-door search finds only the doors somebody thought of.*

```text
declared in manifest.yaml   8
enforcement sites found    21
enforced but not declared   none
declared but not enforced   none
```

| Capability | Sites | Enforced at |
|---|---|---|
| `reservation.read` | 11 | Availability ×2 · BookingRead ×3 · Registration · Reporting · Settings · StayList ×3 |
| `stay.create` | 1 | `BookingService.cs:83` |
| `stay.override` | 3 | `ReconciliationService.cs:71`, `:127` · `StayLifecycleService.cs:363` |
| `stay.assign` | 1 | `StayAssignmentService.cs:55` |
| `registration.capture` | 1 | `RegistrationService.cs:54` |
| `reporting.file` | 1 | `ReportingService.cs:65` |
| `request.handle` | 2 | `StayRequestService.cs:54`, `:170` |
| `desk.configure` | 1 | `SettingsService.cs:85` |

**The lifecycle's five entries share one guard, and it runs first.** `CheckIn`,
`CheckOut`, `Cancel`, `RecordNoShow` and `Correct` each call
`RequireWritableAsync` (`:48`, `:112`, `:174`, `:214`, `:270`), whose first
statement is the `stay.override` check and whose second is the load — *a guard
placed after the write it protects is not a guard, and this one is before.* A
sweep counting `RequireAsync` per public method would have reported four of five
unenforced.

**`guest.amend` was declared in code, in no manifest, and enforced nowhere —
REMOVED 2026-10-03.** `Permissions.cs` held the constant; it appeared in no
`manifest.yaml`, no service and no screen. *Kept in this ledger rather than
deleted from it, because a permission vocabulary that once carried a name is
worth a reader knowing was considered and dropped.*

---

## 2 · The four checks a generic sweep misses

### 2a · Does the harness render the WIRE, or a second contract?

**A second contract, and no compiler can catch it.** The screens receive the
wire as `load<Today>(host, …)` — a **type assertion over `unknown`**. The model
in `ui/book/model/*.ts` is hand-written TypeScript declaring what the server is
believed to send; nothing compares it to the anonymous objects
`backend/src/Module/*View.cs` actually returns.

So the comparison was derived: method→View from `ModuleSurface`'s own routing,
method→fixture from `frame.ts`'s own `answers` map, keys from each literal.

```text
methods routed   30        methods answered by the harness  12
compared         10        agree  9        differ  1        unread  1
```

> **`Today.stale` is consumed by the FIRST screen of the owner's walk and the
> wire never sends it.** `TodayView.cs:68-95` returns `businessDate`, `rollsAt`,
> `connected`, `stats`, `lists`. `Staleness` is on no response anywhere in the
> backend — positive control: the same view's `connected` IS sent, at `:90`, and
> the same screen reads it. The model declared `stale` **required** and the
> screen guarded `=== null`, which `undefined` does not satisfy, so
> `stale(undefined)` read `.headline` of undefined and the screen threw.

**FIXED — the type now says what the wire offers and the guard is `== null`.**
The staleness strip is *drawn* (frame 11, S36 · GUEST-Q4 · R27) and the
feed-staleness mechanism behind it is **unbuilt**; the screen renders no banner
rather than failing, which is what *absent is not degraded* requires.

**⚠ The instrument needed FIVE corrections and four produced finding-shaped
output.** Recorded because a detector is a hypothesis about the fault's shape,
and every wrong version of this one looked like a result:

```text
depth read at END of line   `sections: [` had already opened a bracket, so the
                            key was skipped on BOTH sides -> 8 DIFFERENCES,
                            every one false
C# pattern required `=`     shorthand members (`stats,`) invisible -> `stats`,
                            `lists`, `room` reported as fixture-only
the fixture ternary picked
  the `...spread` variant   only the override keys were read
widening to `=> new` by
  taking min()              picked a nested `.Select(x => new {…})`, and
                            compared fell 9 -> 7: the WRONG DIRECTION
```

*What caught each one: a known-answer control — `recordedToday`'s five keys,
read by eye before the script existed — and watching whether the compared count
moved the wrong way.* **The first version would have sent eight false
divergences against clean work.**

**The count is a FLOOR.** The comparison reads **top-level keys only**, so every
nested field is unmeasured, and `attention` is unread because its view answers
through a paging helper rather than an object literal. *A class keyed on one
level hides every difference below it.*

### 2b · Does any WRITE need something the READ does not send?

**No — all three pairings are closed, and one was closed deliberately.**

```text
version     write RequireWritableAsync(…, version, …)
            read  StayDetailView.cs:114 `version = stay.Version` - whose own
                  comment records that this screen had none, and that a read
                  carrying no version cannot make the concurrency check
roomId      write WalkInCommand / AssignCommand
            read  FreeRoomsView.cs:75 `{ id, number }`
roomTypeId  write BookCommand's Draft
            read  AvailabilityView - "the id that books it"
```

### 2c · `protected_personal_data` — it parses, and what ENFORCES it

```text
at USE      ENFORCED and DEMONSTRATED.  PlatformAdapters.cs:171 RequiredKey
            THROWS when the key is absent and refuses to generate one -
            "a generated key would silently orphan every contact already
            stored."  PROVEN BY THE OWNER'S RUN of 2026-10-02: 8 of 8
            creating calls refused 500, `Pii:FieldKey is not configured`,
            36 occurrences in the captured log.  Unintended, and the
            strongest evidence class available.
at INSTALL  MECHANISM EXISTS, and it is MM's.  services/kernel/crates/kernel/
            src/packages/pii/mod.rs:58 gates on
            `manifest.protected_personal_data`; delivery.rs names the
            variables; and tests/packages_pii_names.rs DERIVES the Kernel's
            constants from PiiProtection.cs by `include_str!`, so a rename on
            either side fails there rather than as an application that starts
            and cannot find its key.
for 0.3.5   UNPROVEN BY CONSTRUCTION.  The declaration ships in 0.3.5
            (a391b50c, sha256:9c8eed08…); the installed build is 0.3.4, whose
            embedded manifest carries no declaration.  Nothing I can run
            closes this - it needs MM's install and the owner's walk.
```

### 2d · Room Care is not installed — does any surface assume four?

**No.** `ServicingView.cs:47` asks Context for the `roomcare` neighbour and
answers **null by construction, labelled in its own words** — *"not a
placeholder for a list that failed to load — it is the absence of a Context read
this application is not yet able to make."* The harness carries `?alone`
variants for the absent-neighbour frames, because whether a neighbour is
installed is a fact about the property rather than a route. `INeighbours.cs:65`
names the domain; nothing counts applications.

---

## 3 · Every gap, classified three ways

| | Gap | Owner |
|---|---|---|
| **BY CONSTRUCTION** | `protected_personal_data` enforced at install for 0.3.5 — needs the install | **MM** |
| **BY CONSTRUCTION** | the gRPC door's own refusals — mTLS `RequireCertificate`, and a test process holds no service identity | — |
| **BY CONSTRUCTION** | every nested field of every response — the comparison reads top-level keys only | **FF** |
| **BY CONSTRUCTION** | `attention`'s response shape — answered through a paging helper, not a literal | **FF** |
| **BY CHOICE** | the drivers are NOT re-pointed at the wire. The module door's `Draft` is **nine fields** and hardcodes `Terms`, `Channel`, `TravelAgent`, `MarketCode`, `MealPlan` to null — a narrower aperture, so re-pointing would silently stop driving most positions | **FF** |
| **BY CHOICE** | `dotnet test` not run for this report — it touches the shared `obj/` and `bin/` and I hold no machine slot. The `stale` change is TypeScript only | **FF** |
| **BY OMISSION — now closed** | nothing compared the fixture to the wire. Closed above: one real difference, fixed | **FF** |
| **BY OMISSION — now closed** | Part C's closing question read as *"the path does not exist"*. Pointed at `--book` | **FF** |
| **DEFECT, mine — CLOSED 2026-10-03** | derived from §E's enumerated rows: **4+3+4+3 = 14**, of which **12 blocked + 2 needing no data** closes at 14. The wrong figure was **13**; the twelve and the two were always right. §E now derives it and both documents say which figure was wrong. *It said:* the control-site count does not close: **13** in `part-b-drive-list.md` §E's heading, **4+3+4+3 = 14** across its subsections, and *"twelve of the thirteen are blocked… and the two that are not"* in Part C — **12 + 2 = 14 against a stated 13**. Three figures, no two agreeing, in the documents the owner walks from. To be **derived from the enumerated rows**, never restated as a fourth number | **FF** |
| **DEFECT, not mine — CLOSED by DD** | re-measured 2026-10-03: `TSC_EXIT=0`, **0 errors, 0 naming `HostApi`**. DD's `051515d9` (the `hostDouble` factory) and `620781b` (61 sites across 52 files) closed it, and the ADR 0168 gate `819d0f7` reports 17 of 17 consumers built. *Its own output says it runs every consumer's BUILD and no consumer's SUITE.* **It said:** **GuestOps' `tsc` is RED AT HEAD** — `TSC_EXIT=2`, 4 errors, all `Property 'log' is missing … required in type 'HostApi'`. `48b0daa7` (2026-10-03 09:21, ADR 0364 item 4b) made `HostApi.log` required at `sdk-typescript/src/module.ts:128`; four GuestOps host doubles do not supply it. **0 of the 4 errors name a file I touched.** `packages/sdk-typescript/src/host-double.ts` is untracked, so the author is mid-remedy — reported, not fixed | **the ADR 0364 logging stream** |
| **OPEN, not mine** | the staleness mechanism `Today.stale` is drawn for and nothing computes | planner / owner |

*`48b0daa7` is ADR 0168's own case: an SDK change whose application consumers sit
in the sibling repository, where no HosPilotOS command can see them.*

---

## 4 · The owner's walk

**`part-b-drive-list.md` is the list, and `--book`'s rows are what it is pressed
against.** Two things to know before starting:

* **Install 0.3.5 — do not reinstall 0.3.4.**
  `sha256:9c8eed08ab409803ec77f5486942f64ea50a694211d5d0ac69d6f2aabf4af6fc`,
  16,381,926 bytes. 0.3.4's embedded manifest carries no PII declaration, so a
  reinstall would fail the way a provisioning bug looks.
* **GuestOps must be restarted to receive the keys** — ADR 0367 §12 provisions
  before launch.

**I do not sign Part B.** Every row above names an owner; none says *someone*.

## 5 · Against the owner's authored pages — the control census

**The reversal this section records.** I had read the mockups as drawings *we*
produce for the owner to choose from, and sent thirteen greyed controls up as a
question. The owner's ruling of 2026-10-04 is that `docs/mockups/` is the
authored specification — *"i need that exact ui and working mechanism what we
designed there"* — so **a greyed control with a reason on it is a specification
OF that reason, and where the build and the page disagree the build moves.**

**What the owner's page actually greys, derived rather than recalled:**

```text
01-guestops-gold.html        27 frames          ids derived from the markup
  btn nodes                  78   ·   3 OFF
  link nodes                 18   ·   0 OFF
                             ---------------
                             96 controls, 3 off nodes = TWO distinct controls

    Export          fC2 and f4, the second carrying its reason in the label:
                    `NEEDS SHELL-Q23'S FILE-SAVE HALF`
    ＋ Raise a job   f5b ONLY — the frame headed *"with Jobs not installed"*.
                    f5, where Jobs IS installed, draws it LIVE
```

> **So the owner's page has exactly ONE permanent capability caveat — a PLATFORM
> gap, named — and one absent-neighbour frame state.** The build's Part A capture
> draws **22** `.btn.off`, from **16** production `unavailable()` call sites.

**Every one of those sixteen, against what the page draws. Conditional and
unconditional are separated, because a table calling a conditional greying
*always off* would be a false row.**

| the build | the owner's page | disposition |
|---|---|---|
| `Export` · `stay/index.ts:313` | **f4 OFF**, reason in the label | **MATCHES.** SHELL-Q23's file-save half — a platform gap, not GuestOps' |
| `＋ Raise a job`, Jobs absent · `:321` | **f6 OFF** — *not f5b, which does not exist* | **MATCHES.** `APPS-Q2`: an absent neighbour loses its capability, never the flow |
| `＋ Log a request` · `requests-tab.ts:64` | **f5 and f6 LIVE** | **MATCHES** — *conditional*, off only where the host serves no method. Flows §10: the request is recorded *"always, installed or not"* |
| `＋ Raise a job`, Jobs installed · `:322` | **f5 LIVE**, `btn pri`, TITLE BAR, tab-scoped by the owner's own words | **IT OPENS A FORM — f5 draws it open.** Not navigation, and not the body's control: `＋ Log a request` logs with NO job. **Two of the form's five fields are carried; `Kind`, `Priority` and `Detail` are carried by nothing, and `Priority` contradicts the flows page** — see below |
| `Ask for service` · `:326` | **f7 LIVE** — *not f6* | **UNBUILT — and the RECORD is unruled.** Flows §10 rules the concept (*"the same shape, from Room Care"*); no servicing write exists and `handOff` hands off to Jobs by name — see below |
| `Open in the PMS` · `:332` | **f8 LIVE as `Open in Opera`** — *not f7* | **THE LABEL IS CORRECT AND RULED — ADR 0212's own Consequences name GuestOps' *"forty-two interim 'the PMS' sentences"* and say they take the connected system's name *"once this ships"*.** The build draws today's ruled state and the page draws the end state. **The CONTROL waits on an ADDRESS**, whose pattern is ruled AND BUILT — see below |
| `Keep 214` · `Take Opera's 208` · `banner.ts` | **f3 LIVE** — `btn sm pri` and `btn sm` | ✅ **BUILT — `482d23be`, `b468fc05`.** And the premise was wrong in my favour: **`ReconciliationService.ClearAsync` carried the whole ruling, with three tests driving it, reachable by NOTHING** — no module arm, no gRPC method, no caller outside `tests/`. Not *mechanism ruled, door missing* but *service built and tested, door missing*. Probes: 2 of 281 (UI, a split), 1 of 7 (the arm removed) |
| `Save` · `Discard` · `setup/index.ts:100-101` | **f17 and fV2 LIVE** — *not f16, whose control is `Save and check in` and which has no `Discard`* | ✅ **THE DOOR IS BUILT — `4b341985`.** `SettingsService.SaveAsync` already had ADR 0356's ruled shape and no module arm, **so no property could ever store a settings row through the desk — and that is why frame 15's capture is 409.** Probe: 1 of 8, a split. **The SCREEN stays off on a PAGE GAP** — see below |
| three `＋ Add` · `setup/card.ts:37` | **fV2 LIVE** — the `Reasons` TAB, and the three `＋ Add` are ONE PER LIST; `＋ Close a room type for dates` is `Stop-sell`'s and `Record a filing` / `Open the list` are `Guest reporting`'s | **UNBUILT, and now PER TAB** — four of the five tabs are documented, one is not, and Stop-sell's documentation conflicts with the owner's description. See below |
| four activity filters · `activity-tab.ts:46` | **f4 LIVE** — Everything `btn sm pri` · Ours · Opera · Other apps | **UNBUILT, mine.** Unconditional |
| `＋ assign` · `today/table.ts` | **LIVE, ×7** | ✅ **BUILT — `a36bbc89`.** A ROUTE, not a capability: the sheet, the overlay arm and the module-door method all existed, and **`overlays.ts:70` and `screens/assign/index.ts:3` each named *"the day list's `＋ assign`"* while the control drew off** — a guarantee-comment PAIR. Probe: 2 failed of 276, a split |
| `Reinstate` · `stay/index.ts:384` fall-through | **fN1b and fRI1 LIVE** — *fRI2 draws no `Reinstate` node* | ✅ **BUILT today** — `200fe4da` |
| the generic fall-through · `:384` | — | **CORRECT, and narrowed rather than removed.** An action nobody drew still draws off with a reason |
| `＋ add a guest in this room` · `newbooking/guest.ts:58` | **ABSENT from the page** | **SURPLUS to the specification.** The build drew a control the owner did not. Honest — it is off and says why — and it is not in the design |
| a servicing night action · `servicing-tab.ts:77` | **ABSENT from the page** | **SURPLUS**, same shape |
| a tag link · `chrome/marks.ts:79` | — | infrastructure, not a control of the design |

> ### ⚠ THE ARITHMETIC DID NOT CLOSE, AND THE WRONG FIGURE TRAVELLED INTO AN ASSIGNMENT
>
> **This said: *"2 match · 1 matches conditionally · 9 unbuilt · 1 built today · 1
> correct by construction · 2 surplus · 1 infrastructure = 17 rows over 16 call
> sites, because `＋ Raise a job` is two sites in one row pair and is counted on
> both arms."*** **Derived from the table itself: 16 rows, of which EIGHT are
> unbuilt.**
>
> ```text
> 2  match                     1  matches conditionally
> 8  UNBUILT  (published as 9)  2  built            (assign joined Reinstate)
> 1  correct by construction    2  surplus          1  infrastructure
> --                            --
> 16 rows, and 16 is the total  — published as 17
> ```
>
> **`＋ Raise a job` genuinely IS two rows** — one matching (Jobs absent) and one
> unbuilt (Jobs installed) — **and I then added one MORE for the pair, and wrote
> a sentence explaining the extra.** *The explanation was composed to justify a
> number that was already wrong, which is why it reads as reasoning.*
>
> **The architect's relay said *"build the nine"*, quoting my figure.** A count
> published beside its own enumeration is checked by nobody unless somebody adds
> the column up — and the author is the one person who has just counted it and
> will not count it again.

### The seven that remain — measured 2026-10-04, each with what it is waiting on

**One of the eight is built. The other seven were measured rather than
estimated**, and they do not share a blocker: three are mine outright, three
need a contract somebody owns, and one is a conflict.

| row | what it needs | whose |
|---|---|---|
| **`＋ Raise a job`**, Jobs installed | the record and the announcement. Flows §10 rules it: *"Request logged on the stay — GuestOps's own record — always"* and *"GuestOps never calls Jobs"*. `RequestCommand` is **already served** under `log`/`note` | **mine** |
| **`Ask for service`** | the same shape from Room Care. Flows §10 rules the shape; `ServicingView` is a **read** and no command answers it | **mine** |
| **the banner's `Keep 214` / `Take Opera's 208`** | a module-door method. `GUEST-Q3` ruled the whole mechanism — both choices take the stay's write permission, both values are kept, and clearing to the PMS's side *"publishes the same correction a room move does"* | **mine** |
| **setup `Save` / `Discard`** | a settings WRITE. `"setup"` serves `SetupView`, a read, and **nothing in the module door writes `GuestOpsSettings`** | **mine, and one question** — see below |
| **the setup card's action blocks** — `＋ Add` ×3, `＋ Close a room type for dates`, `Record a filing`, `Open the list` | the same settings write, plus a stop-sell write and a filing record. `StopSell` and `StayReporting` are **designed** (design §2.7, §5.3) and unbuilt behind the door | **mine, after the write exists** |
| **the four activity filters** | **SPLIT by measurement — see below.** Two are servable today; two turn on a ruling | **two mine, two the owner's** |
| **`Open in the PMS`** → the page's **`Open in Opera`** | a URL nobody owns, and a LABEL that is a fact | **UNRULED** — see below |

#### ⚠ SIX OF THIS TABLE'S FRAME CITATIONS WERE WRONG, AND ONE FRAME DOES NOT EXIST

**Corrected 2026-10-04 by deriving each control's frame from the node's own
position, rather than reading the frames in order.** The CONTROL inventory above
was right — which controls, in which state — and the LOCATIONS were not:

```text
my row said                       derived
f5b   Raise a job OFF             f6    - AND THERE IS NO f5b IN THE PAGE
f5b   Log a request LIVE          f6
f6    Ask for service LIVE        f7
f7    Open in the PMS LIVE        f8    - where the label is `Open in Opera`
f16   Save / Discard LIVE         f17 and fV2 - f16's control is `Save and
                                        check in`, which is a DIFFERENT one,
                                        and f16 has no `Discard` at all
fRI1 · fRI2  Reinstate LIVE       fN1b and fRI1 - fRI2 has no Reinstate node
×7 (no frames named)             f1 ×6 + fN2 ×1 = 7
```

**Already right, and re-measured rather than assumed**: `Export` — `fC2` and
`f4`, both OFF · the banner — `f3` · the four filters — `f4` · the three
`＋ Add` — `fV2`.

> **A wrong count announces itself; a wrong LOCATION looks exactly like a
> location.** Nothing in the old table disagreed with anything — the states were
> right, the totals were right (96 nodes, 3 off), and six rows pointed at frames
> that do not hold what they claimed. **A reader checking one would have found
> the control somewhere and assumed a typo.**

**AND MY FIRST SELF-CHECK PRODUCED 21 FALSE FAILURES**, including rows derived
from the same file minutes earlier. It read the class from a 200-character window
before the label and took the FIRST match, so it reported the PREVIOUS node's
class — it gave `Ours` the class the walk gives `Everything`. *A detector is a
hypothesis about the fault's shape, and this one was wrong by one node; believing
it would have concluded the gold page has almost no controls where I had just
derived 96.*

**The sound check reuses the proven walk** — the node and its own attributes
captured together — and carries both controls in the same run:

```text
positive   96 btn/link nodes, 3 OFF   agrees with the independent census
negative   a label minted this run     0 nodes
claims     23 of 23 corrected rows pass
mismatches 2 - EXACTLY the two citations this table had wrong: f5b and fRI2
```

**One weakness, stated rather than left**: the check matches a SUBSTRING, so
`Save` matches `Save and check in`. That is how `f16` passed at all, and reading
the matched text is what showed it was the wrong control.

#### ＋ Raise a job — the capability is BUILT AND ALREADY REACHABLE; the header is not

**My row called this *"UNBUILT, mine"*. Measured, that is wrong in the same
direction as the banner's:**

```text
RequestCommand      serves `log` with a `handOff` flag, and echoes `handedOff`
                    from what was STORED - Module/RequestCommand.cs:67-88
StayRequestService  LogAsync(scope, stayId, text, handOff, ct)
requests-tab.ts:84  ALREADY DRAWS `Log and raise a job`, LIVE, conditional on
                    `requests.jobsInstalled !== false`
stay/index.ts:322   the HEADER's `＋ Raise a job`, drawn OFF in both arms
```

> **So a desk with Jobs installed can raise a job today.** The thing drawn off is
> a SECOND affordance for a capability that works — *absence of a control is
> evidence about controls, and becomes evidence about capability only after
> following the call to where the thing is actually done.*

**And the divergence is a LAYOUT one the page is explicit about:**

```text
the owner's f5    header  `btn pri`  ＋ Raise a job
                  body    `btn sm`   ＋ Log a request
the build         body    both: `Log` and `Log and raise a job`
```

**What the header control OPENS is drawn nowhere.** `f5` shows it with the
Requests tab already active and the body's input visible; no dialog is drawn for
it, and the flows page draws the flow without one. **Two readings — route to the
tab and focus the input, or open a dialog — and the page draws neither**, so
building either would be a design decision wearing a wiring change. *Reported
rather than chosen: the owner draws.*

#### An undesigned tab is drawn DISABLED with its reason — ADR 0378

**The owner ruled B of three** — drop it, keep it disabled with a reason, or supply a
sentence — and the general shape is the ADR's:

> **A surface element that is drawn and undesigned is drawn DISABLED with a reason
> stating what is absent. Not removed, not enabled, not promised.**

```text
NOT removed    removal discards a deliberate drawing
NOT enabled    nothing stands behind it
NOT promised   "coming soon" is a claim about a mechanism, and none is owed
DISABLED       with a reason STATING WHAT IS ABSENT
```

**The reason, drafted here and the owner's to approve through ADR 0235's temp page:**

> *"This tab holds no settings. Nothing about a stay is configured from here."*

**The alternative the ruling rejects is the silent one** — enabled and inert —
**which is the shape this round measured sixteen times.** §2's C11 already forbade it
for an action with nothing to send; ADR 0378 extends it to a surface element.

##### And the strip itself was a closed loop

```text
the service sent   Registration · Card series · Reporting          THREE
f17 draws          Registration · Guest reporting · Stop-sell ·
                   Stay defaults                                  FOUR
fV2 adds           Reasons                                        FIVE
`Card series`      appears on NO approved page — §2.8 puts the series inside
                   registration
```

**Changing the list from three to five broke no test**, so a strip no property would
render survived in the harness. Both halves are asserted now: the **screen** (a section
with a reason draws disabled with that reason; one without stays live; nothing is disabled
when every section has content) and the **wire** (the door's own answer carries the five
labels in order and three distinct reasons). *A test over the fixture would have been a
claim about a claim.*

**The three reasons differ deliberately.** *Ruled but not built* and *nothing is designed*
have opposite remedies, and one shared sentence would report them alike.

##### The seven stale date ranges, and the ruling neither of us found at first

```text
fN1   :705                      19 – 20 Aug  →  19 Aug → 20 Aug
f9    :1306 :1309 :1310 :1312    3 – 7 Sep   →  3 Sep → 7 Sep
fRI1  :1890 :1893               19 – 20 Aug  →  19 Aug → 20 Aug
```

**The target form is the page's own** — the gold page already held the long form in
39 places, `19 Aug → 20 Aug` and `3 Sep → 7 Sep` among them — so the
seven are stragglers inside a conforming page, which is itself the evidence they are stale
rather than a deliberate alternative.

**Two rulings govern and both are needed**: ADR 0175 (planner, 2026-09-16) puts *"the
order · the separator · the grouping · the hour cycle · the abbreviation
· **the range dash** · the unit"* on the reader's side; the owner's ruling of
2026-09-20 chose which of the reader's two forms. **`when.ts` cited only the second**,
which is how a sweep for *"compressed range"* found nothing across 377 ADR bodies while
the governing decision sat under the word *locale*. It cites both now.

#### ⚠ THE RAISE FORM'S THREE FIELDS — TWO ARE RULED BY AN OWNER RULING I HAD NOT SWEPT

**I reported `Kind`, `Priority` and `Detail` as carried by nothing and `Priority`
as CONTRADICTING the flows page. The sweep of Jobs' own chapters reverses both.**

##### `Priority` is ruled, and f5's select is the ruling rather than a conflict

**`jobs/docs/chapters/02-the-jobs-walkthrough-and-sign-off.md`, `S1-D4` and
§S1.4 — *"How priority is set — RULED, owner 2026-09-02"*:**

```text
Levels   Emergency · High · Normal · Low, plus NOT TRIAGED
1  a person chose one          → that wins, and is recorded as manual
2  the guest flow decides it   → NEEDS PMS OR GUESTOPS INSTALLED
3  the service's own default   → always available
4  nothing matched             → NOT TRIAGED, a real state
```

**Layer 2's table, first row:** *"**Occupied, guest in house** | somebody is
affected right now | **raise it**"* — **which is f5's `Guest in house — urgent`.**
And the section's own rule 1: ***"A human override always wins and always
sticks."*** *That is what makes it a `▾` rather than a label.*

> **So the flows page and the gold page were never in conflict.** *"Jobs …
> assigns, **prioritises**, tracks, closes"* is true — Jobs owns the chain and
> records which layer decided. **GuestOps is layer 2's named prerequisite, and
> the desk changing the select is layer 1.** *My finding was a true reading of
> two true sentences, joined by an assumption neither of them makes.*

##### `Kind` is ruled too — it is Jobs' catalogue, which GuestOps is named as reading

```text
02:822   "Room Care, Maintenance and GuestOps read the CATALOGUE. Nobody but
          Jobs reads the POLICY."
03:177   category  · code · name jsonb · DEPARTMENT_CODE · icon
03:178   item      · category_id · guest_requestable · applies_to · …
02:2188  "A guest-raised job is an ordinary job with raised_via: GUEST_QR, a
          stay link and no chosen assignee — nothing else in Jobs changes"
```

**`Engineering` is a category's `department_code`**, not a GuestOps invention.

##### ⚠ AND THE VOCABULARY I QUOTED IS A REGISTER ROW MISQUOTING AN OWNER RULING

**Measured across all six of Jobs' documents, both vocabularies, whole files:**

```text
Emergency · High · Normal · Low · Not triaged    30 in the walkthrough
                                             5 in 03-the-jobs-design.md
                                             0 in the other four
P1 · P2 · P3 · NOT_TRIAGED                   ONE occurrence IN THE WHOLE SET
                                             03-the-jobs-design.md:733
```

**`JOBS-Q3`'s register row says *"the WALKTHROUGH's `P1 · P2 · P3 · NOT_TRIAGED`
vocabulary … govern"*. The walkthrough contains that string ZERO times**, and
states the owner's five levels thirty. *And `03`'s own :133 lists the five, so the
design chapter contradicts itself at :133 against :733 — in a sentence whose next
clause reads "translating quietly would put two vocabularies in one product".*

> **This is where my wrong figure came from: I read it off the register.** *CC's
> rule — a register row cannot tell a reader it is a compression, and the row's
> reader is the one most likely to re-quote it.* **Not mine to fix: Jobs'
> chapters and the register are not my files, and an owner ruling in conflict
> with a register row is the architect's to reconcile.**

##### The dispositions, and ONE row needs a ruling

```text
What is wrong   → StayRequest.Text                     CARRIED      build
Where           → StayRequestRaised.RoomId, derived     CARRIED      build
Priority        RULED, owner 2026-09-02. What must cross   RULED,
                the wire is the OVERRIDE when the desk     UNBUILT      build
                makes one, so Jobs can record layer 1
Kind            RULED as a CONCEPT — Jobs' catalogue,     RULED,
                GuestOps named as a reader. But GuestOps   MECHANISM
                HAS NO CATALOGUE READ, and inventing one   UNRULED
                is a cross-application coupling decision
Detail          ruled NOWHERE: 0 in the register, 0 in
                379 ADR bodies, absent from every Jobs     UNRULED
                field list
```

**And the open row is already asked, by Jobs, and registered nowhere.**
`01-the-jobs-reference-survey.md`, question 10: *"**Does Jobs carry a
guest-facing surface at all?** … GuestOps owns the guest. **Is the guest's
request a GuestOps object that produces a job by event, with Jobs holding no
guest identity** — and if so, where does the rating live?"*

```text
"guest-facing surface"  register 0   ADR bodies 0
"guest's request"       register 0
"raised_via"            register 0   ADR bodies 0
guest_requestable                    ADR bodies 0
NOT_TRIAGED                          ADR bodies 0
```

**So Jobs' entire priority and catalogue vocabulary lives in its own chapters and
the register, and in NO ADR.** *An ADR-only sweep could not have found any of it
— which is why the gate names the app's own chapters first, and why my earlier
"nothing rules this" was a measurement of the wrong population.*

#### ⚠ ＋ RAISE A JOB OPENS A FORM, AND TWO OF ITS FIVE FIELDS ARE CARRIED

**Three readings of this control were wrong, mine and the architect's, and all
three assumed a CATEGORY the measurement could not establish.**

```text
I measured   actions() is keyed per tab, so the button is drawn only on Requests
I concluded  navigating there is a no-op  -> read it as NAVIGATION
the architect
  concluded  "a frame drawn in its post-NAVIGATION state is the destination"
the owner    "＋ Raise a job only shows when we click the 3 tab - each each
             there on buttons (some have) - so this button not always in in
             the bar"
             -> the title bar's action area IS per tab. My measurement was
                the SPECIFICATION.
and the frame
  settles it f5 draws the RAISE FORM OPEN, twenty lines below the button. It
             is the post-CLICK state, and the destination is a FORM.
```

> **A correct measurement of a mechanism does not say whether the mechanism is
> the specification or the defect, and nothing in the measurement can.** *Third
> instance from me in one day: `0 .inp` read as a missing form when the content
> WAS the data; `actions()` per tab read as a gap when it is the contract; six
> `GetPropertySummary` text hits read as two call sites when all six are prose.*

##### And the two controls are not one — I had that wrong too

**The frame's own caption**: *"A request is a fact about the guest's stay and
lives here whether or not any work follows from it."*

```text
＋ Log a request   in the BODY. Records a request. NO JOB.
＋ Raise a job     in the TITLE BAR, tab 3. Opens the form that CREATES one.
```

*I reported the body control as "the control that actually raises". It logs —
and conflating them is what made the header one look redundant.*

##### The form, read as RENDERED TEXT, against what the record and the event carry

```text
Raise a job   From Rajesh Pillai's stay · room 214 · in house until 4 Sep
What is wrong   "AC not cooling"                       -> StayRequest.Text        ✓
Kind            "Engineering ▾"                        -> CARRIED BY NOTHING
Priority        "Guest in house — urgent ▾"             -> CARRIED BY NOTHING
Where           "Room 214 from the stay"               -> StayRequestRaised.RoomId ✓
                "the room comes from the stay's current assignment"  DERIVED, not entered
Detail          "Guest reports the room is not cooling…" -> CARRIED BY NOTHING
Cancel · Raise job
```

**`StayRequest`** holds `Id · StayId · Text · LoggedBy · LoggedAt · HandedOff ·
CorrelationId · JobId`. **`StayRequestRaised`** carries `RequestId · StayId ·
PropertyId · RoomId · Text · CorrelationId`. *So the flow the form's caption
describes — "records a request on the stay and announces it" — is built, and
`LogAsync(…, handOff: true)` is it.*

##### ⚠ AND `Priority` CONTRADICTS AN APPROVED PAGE — both statements quoted

**`priority` appears EXACTLY ONCE in this application's entire documentation**,
and it is in the flows page:

> **`02-guestops-flows.html`**: *"Announced with the stay and its current room ·
> **GuestOps never calls Jobs** → **Jobs creates JOB-8821, assigns, PRIORITISES,
> tracks, closes**, carrying the stay reference."*

> **`01-guestops-gold.html` `f5`**: a **`Priority ▾`** select, in GuestOps,
> reading *"Guest in house — urgent"*.

```text
the flows page   Jobs prioritises                      -> not GuestOps' field
the gold page    Priority is a field on this form       -> GuestOps collects it
the record       carries no priority                    -> agrees with FLOWS
Jobs' own
  approved
  vocabulary     P1 · P2 · P3 · NOT_TRIAGED             -> and NOT_TRIAGED
                                                           existing means Jobs
                                                           triages; "Guest in
                                                           house — urgent" is
                                                           none of the four
```

**So the gold page's select offers a vocabulary Jobs does not have, for a
decision the flows page gives to Jobs** — and the implementation agrees with the
flows page.

##### What is ruled, and what one sentence would settle

```text
RULED    the button exists, is tab-scoped in the title bar, and opens a form
         - the owner, twice, in their own words
RULED    the flow the form performs - "records a request on the stay and
         announces it", flows §10, and LogAsync(handOff: true) is it
RULED    Where is DERIVED from the stay's current assignment, not entered
CARRIED  What is wrong -> Text · Where -> RoomId
NOT      Kind · Priority · Detail - carried by no record and no event
RULED
CONFLICT Priority, between two approved pages
```

**Not built, and deliberately not built partially.** The owner's rule is *"that
exact UI and working mechanism"*; a form drawing two of five fields is a
different form. *And the sweep says the field set is documented nowhere — the
raise form's fields return ZERO across all four chapters and the flows page,
with `APPS-Q1` as a positive control present in all five and a negative minted
this run absent from all five.*

#### Ask for service — the CONCEPT is ruled and the RECORD is not

`f7`'s header `btn`, live. Flows §10 rules the concept in four words —
*"Servicing across the stay — **the same shape**, from Room Care"* — and the
service layer has no write for it:

```text
StayRequestService   LogAsync · RecordJobAsync · AddNoteAsync
                     and `handOff` hands off to JOBS, by name
ServicingView        a READ
```

> **"The same shape" is a concept, not a contract.** A second hand-off target is
> a schema decision — does a service request reuse `StayRequest` with a target,
> or is it its own record? — and deciding it inside a door would be exactly the
> *implementation detail mistaken for a contract* this estate warns about, in the
> cheaper direction: quietly implemented differently and found much later.

#### ⚠ SETUP IS FIVE TABS, AND MY CENSUS MEASURED ONE CARD — re-read PER TAB

**The owner has ruled A: each tab is a form, and each has its own duty.** My
*"0 `.inp`, 18 `.k`/`.v`"* was a measurement of **one card spanning four tabs'
content**, so the page-gap question I routed upward was malformed — I asked
*"form or values"* about a card that is five forms.

**Re-measured from the markup, per frame:**

```text
f17   Registration(ON) · Guest reporting · Stop-sell · Stay defaults      FOUR
fV2   Registration · Guest reporting · Stop-sell · Reasons(ON) ·
      Stay defaults                                                        FIVE
```

> **And `fV2` IS the per-tab drawing of `Reasons`.** Its **7** value rows are the
> three lists' entries — *Guest cancelled · flight changed 41*, *no penalty
> agreed 18*, *Property cancelled · overbooked 2*, and so on — and its **three
> `＋ Add` controls are one per list.** So `Save` over `＋ Add` is coherent: the
> fields ARE the entries, and a mock showing stored state has no `.inp` to draw.
> **The page gap as I framed it is WITHDRAWN**, from the artefact rather than from
> the correction.

##### Each tab's duty, measured against the app's own chapters — list derived by `ls`

```text
01-the-front-desk-scenarios.md   50.7 KB   (I had named this NOT REACHED)
02-the-guestops-design.md        66.0 KB   (I had read §3-§4 only)
03-the-open-questions.md         27.5 KB
04-the-code-readiness-note.md    20.0 KB
```

| tab | documented where | disposition |
|---|---|---|
| **Registration** | all four chapters · and ADR 0356's eight classified keys | **DOCUMENTED → a build** |
| **Guest reporting** | chapters 01 · 02 · 03 | **DOCUMENTED → a build** |
| **Stop-sell** | chapter 01:178,188 · chapter 02 §5.3 · the gold page ×3 · the flows and new-booking pages | **DOCUMENTED → a build — AND the owner's description today CONFLICTS on one axis** |
| **Reasons** | **ADR 0305**, owner, 2026-09-28 — *three lists, one per operation*; every chapter PREDATES it and none mentions it | **RULED → a build** |
| **Stay defaults** | **the gold page's tab label, twice, and nothing else in either repository** | **ADR 0378 — RULED and BUILT, drawn DISABLED with its reason.** The owner chose B of three, and their own answer to *what is it for* was *"i dont know for what this tab"* — so there is nothing to describe and nothing to promise |

*Controls, in the same run: positive `GUEST-Q` — 58 · 60 · 26 · 26 across the four
chapters; negative `ZQT-4471902` — 0 in all four. And the two zeros were re-measured
on widened terms before being reported, because a zero from a pattern is a
measurement of the pattern: bare `reason` hits all four chapters while ADR 0305's own
words hit none, and `Stay defaults` is absent from every document that is not the
gold page.*

##### ⚠ THE STOP-SELL CONFLICT, with both statements quoted

**OLD — chapter 02 §5.3, the schema in full:**

```text
stop_sell_id · property_id
room_type_id      Master Data ref          <- ROOM TYPE, and no room
from_date · to_date
reason            free text
set_by · set_at
```

**and chapter 01:188, drawing the boundary in as many words:**

> *"Stop-sell is the seller's control, not an inventory fact. **'We choose not to
> sell this type on these dates'** is a commercial decision belonging to whoever
> runs the book; **'this room cannot be used' is EngineeringOps's**, and they are
> different sentences."*

**and the gold page says the same thing three times** — *"our own setting, per room
type and date range"* · *"ours — room type + date range + reason"* · and the drawn
row `Executive Suite · 3 Sep → 7 Sep · 4 rooms · wedding party`, where **`4 rooms`
is the narrative quantity of a type held whole**, matching the availability table's
`Suite … Stop-sell 4`.

**NEW — the owner, today:**

> *"Stop-sell → stopping a room type **or room** from selling set by operator. Then
> can choose room type **or specific room** and from & to date."*

```text
chapter 01 · chapter 02 §5.3 · the gold page ×3   TYPE + DATES
the owner, today                                  TYPE **OR A SPECIFIC ROOM**
                                                  + DATES
```

> **What I still hold from both.** The owner's *who*, *what* and *from & to date*
> match every document. **The per-ROOM axis is carried by no document**, and it
> lands adjacent to the one distinction two chapters draw explicitly — *we choose
> not to sell* versus *this room cannot be used*. **A per-room stop-sell is still
> commercial** (*"do not sell 214 this week"* is not *"214 is broken"*), so it is
> not necessarily that boundary being crossed — **and it widens §5.3's schema, and
> the chapter is the thing an ADR amends rather than the thing that yields
> silently.**

**Reported rather than resolved either way**: building per-room would implement a
sentence three documents contradict, and building type-only would implement a
document the owner has just spoken past.

#### The setup write — ruled in detail, and the question was answered by the page itself

**My row said *"mine, and one question"*: whether the desk's own setup screen
writes property configuration, or whether that is Core Administration's. The
owner's `f17` answers it in its own caption, citing the ADR:**

> *"**Configuration is the application's own** — an application is a bundle of
> UI, backend, schema, permissions, events and configuration (ADR 0051) — and
> **none of it belongs in Master Data, because none of it describes what the
> property is.**"*

**And the sweep found it ruled twice over.** `ADR 0356` records **`GUEST-Q15`** —
*my own question*, ruled by the planner on 2026-09-30:

> *"For application-owned policy **the owning application persists it, validates
> it at the write boundary, and owns its concurrency/version semantics.**"*

```text
SaveAsync   desk.configure at the boundary          ADR 0356's shape, complete
            HomeCountry and ReportingDueHours
              each refused with a reason
            optimistic version, persist + increment
ModuleSurface   NO ARM                              <- the whole gap
```

> **The consequence was not cosmetic, and my own Part B list already recorded
> it:** *"`SaveSettings` — no module method. gRPC only, and no screen calls it ·
> so the row is never saved · `MintCardNumber`'s PreconditionFailed is PERMANENT
> through the desk."* **One missing arm made the registration card unnumberable
> for every property, through the product's own path** — and the ledger's
> *"Save is OFF by design"* was the stale half.

#### ⚠ AND THE SCREEN IS A PAGE GAP, WHICH IS NOT MINE TO DECIDE ALONE

> **⚠ THIS FRAMING IS WITHDRAWN — the owner ruled A, each tab is a form.** The
> census below measured **one card spanning four tabs' content**, so *"form or
> values"* was the wrong question about a card that is five forms. *Kept rather
> than deleted, because the measurement is correct and only the conclusion drawn
> from it was wrong — and a reader meeting the per-tab section alone cannot see
> that the question was asked at the wrong granularity.* **The per-tab reading is
> the section above.**


**The tag census, derived from the owner's own page:**

```text
f16  the registration card   18 .inp  · 18 <label>  ·  0 .k/.v     A FORM
f17  Setup                    0 .inp  ·  0 <label>  · 18 .k/.v     VALUES
fV2  the reason lists         0 .inp  ·  0 <label>  ·  7 .k/.v     VALUES
```

**So the page HAS a convention for a field — `f16` uses it eighteen times — and
`f17` draws none, with `Save` and `Discard` live.** The build's own comment says
the same thing from the other side: *"nothing on this screen is an input (every
field is a drawn `.inp`, §10)"*.

> **`Discard` is the discriminator.** A discard only means something over an
> unsaved edit buffer — so **the page intends fields and draws none**, which is
> a page gap rather than a build gap. *Two readings survive otherwise: `Save`
> persists the values as displayed, or `Save` commits edits in fields nobody
> drew, and the page draws neither outcome.*

**And the screen cannot send an edit today for a second, independent reason**:
`SetupView` answers with rendered `k`/`v` rows, not the raw fields, so a screen
redrawing from it has nothing to put in a `SettingsEdit`. *Whether the read
carries the values is downstream of which values are editable, so it waits on the
same answer.*

#### ⚠ ADR 0356 §6 OWES ME A CLASSIFICATION, AND NOTHING RECORDS IT

> *"Do not perform a mechanical 'remove all eight manifest keys' change based
> solely on their count or present location… classify EACH existing GuestOps key
> with §1's discriminator… **record the classification ALONGSIDE the change** so
> the next application does not rediscover `GUEST-Q15` from precedent."*
> **Owners: FF.**

**Measured: the eight declared manifest keys are the SAME EIGHT FIELDS
`SettingsEdit` writes.**

```text
guestops.home_country                      -> HomeCountry
guestops.registration.signature_required   -> SignatureRequired
guestops.registration.print_on_check_in    -> PrintOnCheckIn
guestops.registration.card_prefix          -> CardNumberPrefix
guestops.reporting.required                -> ReportingRequired
guestops.reporting.applies_to              -> ReportingAppliesTo
guestops.reporting.authority               -> ReportingAuthority
guestops.reporting.due_hours               -> ReportingDueHours
```

**Applying §1's discriminator to each** — *does it configure the application's
platform or runtime attachment, or express how its domain should operate?*

```text
home_country         WHICH GUESTS COUNT AS FROM OUTSIDE    -> application state
signature_required   what a card demands                   -> application state
print_on_check_in    whether the card prints in the flow    -> application state
card_prefix          the GRC series' own shape              -> application state
reporting.required   whether this property files at all     -> application state
reporting.applies_to a selection over a GuestOps vocabulary -> application state
reporting.authority  "Kerala Police - the property names
                      its own"                              -> application state
reporting.due_hours  an offset from arrival, R18            -> application state
```

**None is a credential, an endpoint or a provider choice. All eight are business
policy, and all eight are already written as application state by
`SaveAsync`** — so **one value has two writable homes today**, which is the
duplication the ruling exists to end.

> **This is a classification, not a mechanical removal** — §6 forbids removing
> them *"based solely on their count or present location"*, and each row above
> carries the discriminator's own question and its answer. *The removal is its
> own change with its own consumers (`GetConfig` callers), and it is owed rather
> than done here; nothing in either repository records the classification, which
> is the half §6 asks for by name.*

#### And a divergence between the two doors onto one view — reported, not changed

```text
gRPC    GetSettings     reservation.read   "a receptionist must see the form they
                                            have to fill in without being able to
                                            change what it demands"
module  "setup"         desk.configure     - it is in ConfigureAsync
```

**So through the pane a receptionist cannot READ the Setup screen, while over
gRPC they can.** *The write is correctly `desk.configure` on both. Moving the
module read is a privilege decision rather than a repair, so it is reported.*

#### The activity filters are two rows, not one — and the service's own comment is half right

**The owner's `f4` draws four**: `Everything · Ours · Opera · Other apps`.
**`ActivityView` deliberately serves two**, with its reason written at the site:

> *"Only the filters this projection can honour. `Opera` and `Other apps` are in
> the design and would return nothing here, and a filter that is always empty
> teaches an operator that a source has gone quiet when it was never being
> read."*

**Measured: the projection CAN honour all four.** `Who(actorType, source)` maps
`1 → override` (a person), `2 → pms` (named by the source), `_ → other` — and
`who.mark` is **already on the wire for every entry**. So the axis exists and the
entries carry it.

```text
Everything   every entry                 servable today
Ours         who.mark === "override"     servable today
Opera        who.mark === "pms"          servable — and see the LABEL below
Other apps   who.mark === "other"        servable today
```

> **So *"would return nothing here"* is a claim about a STANDALONE PROPERTY'S
> DATA, not about the projection.** On a PMS-connected property — which the gold
> page draws as `f11` — `actorType = 2` entries exist and the filter works. On a
> standalone one it returns nothing, **which is honest**: the property genuinely
> has no PMS facts, and that is not a source having gone quiet.

**And the fourth filter's LABEL is why this is not simply mine to build.** The
page draws **`Opera`** — the connected system's name — and **ADR 0212 rules that
Context resolves an integration's name.** So the label is a *fact*, not a screen
word, and nobody has ruled whether the filter set is **static four** or **derived
from the integrations this property has connected**. *Composing `Opera` into the
screen would hardcode one vendor into a platform sold to properties running
others.*

#### ⚠ THE SWEEP I NAMED AS NOT REACHED CORRECTED TWO OF MY OWN DISPOSITIONS

**I closed the connector repository's documents, and the result is that I had
been reporting a RULING as a blocker.**

```text
my earlier figure   pms-oracle: 1 document      <- a count of docs/mockups/ ONLY
derived             9 - three chapters, a conformance page, a provenance page,
                    two ledgers, a mockup, an evidence page
```

##### The LABEL is ruled, and the build's `Open in the PMS` is the ruled INTERIM

**`ADR 0212` — planner, 2026-09-20, closing `CONN-Q44`, which the planner had
RETURNED ONCE for want of measured evidence — read in full:**

> **"Context owns the application-facing resolution `integration_id → integration
> name`, sourcing the authoritative installed-integration record from the
> Integration Hub."**

**And its Consequences name this application by name:**

> *"**GuestOps' forty-two interim *'the PMS'* sentences take the connected
> system's own name ONCE THIS SHIPS**, so a property running Oracle OPERA Cloud
> reads `Oracle OPERA Cloud` and one running anything else reads that instead."*

```text
the build       "Open in the PMS"    the ruled INTERIM state
the owner's f8  "Open in Opera"      the state AFTER the Context RPC ships
measured        context/v1/service.proto has NO integration-name RPC, and the
                Context Service has no implementation
rejected by     stamping the name into Provenance · widening the Hub's
0212, by name   user-scoped RPC · APPLICATIONS CALLING THE HUB DIRECTLY
```

> **So the label is RULED, the platform's half is UNBUILT, and the workaround is
> refused by name.** *My row said **"UNBUILT, mine — and the LABEL differs too"**
> and both halves were wrong: it is not unruled, and it is not mine.*

**The same correction applies to the `Opera` activity filter.** Its label is the
same ADR 0212 fact, so today it reads *"the PMS"* — correctly — and takes the
integration's own name when the RPC lands.

##### And the ADDRESS has a pattern that is ruled AND BUILT

**`ADR 0243` — read in full, closing `OPS-Q10` B1–B2 and `OPS-Q13` B3–B5:**

> **B3: "The declared `console_url` values are configuration, so the correct
> direction is to make them available through the existing configuration
> surface"** — *"a configuration-surface correction, not a `GetConsoleAddresses`
> RPC."*
> **B4: "The configured address, never inferred from the running process."**
> **B5: "The field's existence proves that remote observability is a supported
> case"** — and refusing a non-local address *"would contradict the reason
> `console_url` was introduced."*

**B3's five requirements are binding and they LANDED** — CC, `fa397d1c`: *five
machine-sourced keys outside `DECLARED`, answered before `stores()`, refused by
`SetConfig`, `CONFIG_SCOPE_MACHINE` on the wire, blank resolved to absent.* **So
a configured, read-only, database-independent, absent-distinguishable external
console address is a mechanism this platform has, not one it needs.**

```text
1  one key per SECTION, sourced from the LOADED FILE
2  read-only - SetConfig refuses them
3  resolves WITHOUT the database, before get_config opens a repository
4  "not set" distinguishable - console_url = "" is not an address
5  CONFIG_SCOPE_MACHINE, because UNSPECIFIED would mean the declaration's default
```

> **Requirement 1 is where the question actually sits**: *one key per section*,
> and an installed integration is not a platform configuration section. **So the
> narrow question is whether an installed integration declares a sixth such key
> or declares it some other way** — not *"nothing holds a PMS console
> address"*, which is what I reported.

##### And ADR 0362 §4 rules WHEN that pattern transfers, which is the discriminator

> **"ADR 0243 B3's alternative — the existing configuration surface — does NOT
> transfer, because log sources are DERIVED and are CONFIGURED NOWHERE."**

```text
configured somewhere          -> ADR 0243 B3's configuration surface
derived, configured nowhere   -> a served enumeration (ADR 0362 §1)
```

**Applied to the two halves, and they fall on opposite sides:**

```text
a PMS console ADDRESS    a human types where OPERA's web UI lives.
                         Nothing can derive it            -> CONFIGURED -> 0243
the FILTER SET           which integrations this property has connected.
                         Nobody configures it             -> DERIVED   -> 0362
```

**And ADR 0362 §10 states the consumer side**: *"individual applications NEVER
self-declare their logging identity to the UI"*, and consumers do not construct or
merge independent source vocabularies. *So a filter set over integrations would be
a projection the capability serves, not four labels a screen composes.*

> **⚠ BOTH PRECEDENTS ARE IN THE LOGGING AND PLATFORM-CONFIG DOMAINS, AND A
> MECHANISM'S TRANSFERABILITY IS NOT EVIDENCE ABOUT A TAXONOMY'S SCOPE** (II,
> 2026-10-03). **ADR 0212 rules the integration-name resolution for integrations
> specifically, so the LABEL needs no transfer.** The SET and the ADDRESS each
> need one sentence from the planner, and each now arrives with its own precedent
> named rather than as *"nobody has ruled this"*.

##### `Ask for service` — the concept is ruled and the record is still not

**Swept and genuinely absent**, with the populations stated:

```text
flows §10             rules the CONCEPT - "the same shape, from Room Care"
roomcare chapter 01   DESIGNS the receiving side - "a GuestOps object (the
                      guest, the stay, the request) that reaches Room Care"
roomcare's door       13 served methods; NONE receives a request from another
                      application
the register          the only two rows naming `Ask for service` are
                      `ARCH-Q20`'s pairing class - a drawing's control
                      rendered as a div - and say nothing about the record
```

##### And one vocabulary collision, because a reader meets both documents together

```text
the connector's design   "the only OUTBOUND integration of the three" - the
                         poller, which DIALS OUT to fetch
ADR 0128 §4              "v1 connector scope is INBOUND-ONLY" - the direction
                         of DATA
```

**Both true, on different axes** — and a reader meeting the first concludes the
platform writes back. *My own claim survives; its stated reason needed the
distinction.*

#### `Open in Opera` — two things are missing and neither is mine to invent

```text
the LABEL   the integration's name — ADR 0212 rules it a fact Context resolves
the TARGET  a deep link into the PMS. Measured: nothing in this estate holds a
            PMS console address, and v1's connector contract is INBOUND-ONLY
            (ADR 0128 §4), so no connector supplies one either
```

**The build's own comment states the intent honestly** — *"a link, not an
integration: it takes the user to the system that holds the folio and asserts
nothing about what is in it"* — **and a link needs an address.** *Drawing a live
control over an address nobody holds would be the copy-outruns-the-mechanism
class with a click attached.*

#### And the setup write raises one question rather than being blocked by it

`GuestOpsSettings` holds registration, reporting and numbering; `f16` is the
screen the owner asked for, and `ADR 0305` ruled the reason vocabulary's shape.
**What is unstated is whether the desk's own setup screen writes those settings,
or whether property configuration is Core Administration's** — the build's
current sentence says *"settings are shown here and cannot be changed from this
screen yet"*, which is a true description of today and not a design.

---

### The divergence inside the owner's own pages, and why it is not mine to close

`fRI1` draws the stay's dates as **`19 – 20 Aug`** — the compressed range. The
owner ruled the **long** form on **2026-09-20**, against mockup `07` B, and
`chrome/when.ts` carries that ruling at the site: *"do not 'improve' it into a
compressed range"* and *"No shared range formatter is to be added for it."*

```text
2026-09-20   owner rules the long form, mockup 07 B
2026-09-28   fRI1 lands, drawing the short form in its prose
```

**So this is owner ruling against owner drawing, eight days apart, and the
drawing is prose inside a static page rather than a rendered component.** The
build consumes `span()` — the ruled form — and the difference is asserted in
**both** directions in `correct-dialog.test.ts`, so it cannot be closed either
way without meeting the ruling:

```text
expect(line).toContain("→")             the form the owner ruled
expect(line).not.toContain("– 20 Aug")  the form fRI1's prose draws
```

*Reported rather than resolved: matching the page would reverse a ruling, and
ignoring the page would hide a conflict.*

### What the widgets page already says about its own authority

`03-guestops-widgets.html` states the owner's 2026-10-04 ruling in its own
words, written by its author before that ruling existed:

> *"If a frame here is wrong, the build changes. The frames are the
> specification from the moment they are approved, exactly as the seventeen
> are."*

**And it declares what the five deliberately do not do — no writes, no number
the backend cannot honestly compute, no sizing.** So no greyed widget control is
expected, and none is drawn.

# GuestOps Part B — the drive list

> **Scope, and the certificate's first line: 16 reads served against an EMPTY
> store prove the door, authorization, route, query and render — not the logic
> that computes figures from rows.** Empty answers are reported as empty, never
> as correct figures. Cancel is BLOCKED BY ADR 0193 and **not driven**: no
> booking exists to cancel. — *architect's decision, 2026-09-19: run pipe-only as
> soon as 0.3.1 is installed, and certify exactly that. The data-bearing run is a
> second, separate certificate, not a blocker for this one.*

## RE-ANCHORED to `e0361fbf` / 0.3.4 — 2026-09-30

**Prepared 2026-09-18 against `a4b6723` for 0.3.1, and that anchor is superseded.**
What this list drives now:

```text
HotelOsApps      e0361fbf
package          guestops-0.3.4.hopkg  ·  16,375,968 bytes
                 sha256:ca35f385662794e5257c73f92ffb3055ef4336fc6da011e1edf96b3a14835eee
installed at     %LOCALAPPDATA%\HotelOS\packages\installed\guestops
                 51 archive entries, written 2026-09-30T11:28:52.455Z
process          a NEW one — the old served to 11:28:46.391Z on port 54319;
                 51283 first answered 11:28:54.518Z, two seconds after the install
```

### The preamble that stood here was false by the time anyone read it

> *"Prepared 2026-09-18 by FF against `HotelOsApps` at `a4b6723` and platform
> `HosPilotOS` at `885677ec`. **To run once the owner has installed 0.3.1**
> (`guestops-0.3.1.hopkg`, 16,278,129 bytes, sha256 `4248ecf5…fd1ca8`) on the
> fixed desktop. **Nothing below has been run yet; every result cell is empty on
> purpose.**"*

**The last sentence is the one that matters.** The owner walked three tabs on
2026-09-30 while this document said nothing had been run — *a document's claim
about its own state, checked by nobody*, which is the certificate-methodology
class. Kept above rather than replaced, because a reader needs to see which
sentence rotted.

### What the owner's walk of 0.3.4 established — three tabs, three different facts

| Pressed | Platform-served | Reachable by a person | What it says |
|---|---|---|---|
| **Today** | **YES** — `reservation.read/today` answered **200 five times** on the 0.3.4 process, no `Call failed` after `GetOperatingDay` | **NO** — the screen throws | `TypeError: undefined.headline`. The read is served; the render is GuestOps' defect. **And the message the owner read is the render-fault guard working**: it named the fault, said no data is missing, said a retry will not help, and said what to carry |
| **Guests** | n/a | **DECLARED ABSENT** | *"Drawn in the approved design; not built in this slice."* Design-known, **not a defect**, and it cost nothing to say |
| **Setup** | **NO** — 404 | **NO** | The settings deadlock: one absent row 404'd four surfaces including the only screen that could create it. Closed at `7ba3d74d`; **this row is re-driven after the fix, not marked from it** |

**Plus a third fault on Setup the walk found and no drive row predicted:** the copy
button answered *"The clipboard was refused"*. The realm is
`sandbox="allow-scripts"` with no `allow=` and an opaque origin, so
`clipboard-write` is default-deny and the write could only ever reject. **Removed
at `27c878b7`** rather than granted a sandbox token.

**Every other row below is still unrun, and an empty cell is not a pass.**

## What the two columns mean

Both definitions are rulings, quoted rather than paraphrased:

| Column | Meaning | Governs |
|---|---|---|
| **Platform-served** | *"the requested application operation completed according to its operation contract"* — a legitimate empty result is served; an unavailable dependency, a validation refusal or a transport failure is **not** | ADR 0148 |
| **Reachable by a person** | an operator gets to it through a screen or a widget, on the desktop | ADR 0143, 0148 §"The second column stays independent" |

And the scope sentence the certificate must carry, from ADR 0143:

> **Part B does not attest that the owner's production installation
> historically served the operation.**

A third value is used and is neither pass nor fail:

> **BLOCKED BY ADR 0193** — the operation is authorized against a GuestOps-owned
> object type (`stay`) that no mechanism registers yet, so it is refused for
> every caller by construction. Recorded, never scored. **ADR 0193 is RULED and
> UNBUILT**: what blocks these rows is II's manifest schema and CC's composer,
> not an open question.

**The label was *BLOCKED BY AUTHZ-Q37* until 2026-09-19** and changed on the
architect's instruction, because AUTHZ-Q37, Q37a, Q37b and Q37c are all ruled —
ADR 0193 closed Q37 that morning: *"An installable application may declare the
authorization object types it owns in its signed package manifest. The Kernel
registers those declarations when the package is installed/activated."* A label
naming a closed question reads as a ruling still pending; a label naming the
ruled, unbuilt ADR says what the rows are actually waiting for.

**What 0193 does and does not say about GuestOps.** Its worked table names
`jobs` (`job`), `roomcare` (`room_task`) and `workforce`; **it does not name
GuestOps or `stay`.** That GuestOps' permission definitions move into its own
package and it declares `owned_types: stay` with its stay-scoped permissions is
**the architect's statement of 2026-09-19**, applying 0193 — cited as that, not
as 0193's text, until a document records it.

## Where authorization actually happens — measured, not assumed

Read where it is **enforced**, not where it is declared (CLAUDE.md, *a
capability's reachability is established where it is enforced*). Three doors
were read: the module envelope, the gRPC surface and the event consumer.

**The registry says every GuestOps permission is property-scoped.**
`infrastructure/openfga/permissions.yaml` gives all nine `property` and nothing
else, and says why: *"nothing registers a `stay:<id>` object … A `stay` scope is
added beside these when GuestOps registers the objects."*

**The enforcement does not agree.** Of the 18 `RequireAsync` calls in
`backend/src/Application`, **10 check against `ResourceTypes.Stay`**:

```text
stay.override          StayLifecycleService:326 (every lifecycle write)
                       ReconciliationService:71, :127
stay.assign            StayAssignmentService:54
registration.capture   RegistrationService:53
reservation.read       RegistrationService:102      ← a READ, on a stay
reporting.file         ReportingService:65
request.handle         StayRequestService:54, :170
```

The Kernel resolves a permission against the object type's declared scope
(`authz/registry.rs:282`), and none of these declares `stay`, so each is refused
with **InvalidArgument** — *"permission "stay.override" may be checked against
property, but the object is a stay"* — for every caller, however granted. That
is the class AUTHZ-Q37 named and ADR 0193 rules on — built, it resolves these.

*This page first listed the missing `stay` scope as a **second prerequisite**
beside AUTHZ-Q37.* **It is not a second one**: under ADR 0193 GuestOps' own
package carries its stay-scoped permission definitions with `owned_types: stay`,
so the scope arrives with the same piece of work (architect, 2026-09-19). Kept
visible so the correction can be checked.

**How the refusal reaches a screen, recorded as found and not reclassified.**
The Kernel's error is **InvalidArgument**, and the SDK draws `invalid` as its
*faulted* state — *"Service fault"* — for what is an authorization-scope
refusal. Whether it should be `MODEL_UNAVAILABLE` under ADR 0192 is with the
planner (architect, 2026-09-19). The ledger quotes what the run shows.

**No screen read reaches one of the ten.** The only module-door paths into a
stay-scoped check are `CancelCommand` and `WalkInCommand`. `RegistrationService`
(line 102) is reached only by the gRPC door. So every read below is drivable.

**The module door itself checks the capability at property scope**
(`ModuleEnvelope.cs:406`) before any handler runs, which is why the nine read
views with no `RequireAsync` of their own are still guarded.

**Nobody needs a grant.** `model.fga` derives `admin → general_manager → member
→ viewer` on the property, and GuestOps' relations sit on those
(`reservation_viewer: viewer`, the seven writers `: member`, `desk_configurer:
general_manager`). A property or organization admin holds all nine. *This is
the difference from Jobs, whose Part B stood blocked on a grant nobody issued
(ADR 0148 · HH re-scored).*

## Preconditions, each checked at the start of the run and quoted

1. **The package**: Software Center lists GuestOps **0.3.4, Running**. If it
   shows any earlier version or Stopped, stop — nothing below is about it.
   *This read `0.3.1` until 2026-09-30; a precondition naming the wrong version
   is one the owner can satisfy while driving a different build.*
2. **The identity**: the signed-in user is admin on the property. Evidence: the
   Today screen answers rather than drawing *Not permitted*; a refusal there
   ends the run as a precondition failure, not 16 failed rows.
3. **The data**: whether this property's `guestops` store holds any stays. A
   read of an empty store is **served** under ADR 0148 (a legitimate empty
   result), but a certificate made entirely of empty lists proves the pipe and
   not the logic, and it says so rather than implying otherwise.

## A · Reads — module door, `reservation.read` (16 methods)

Each row: drive the screen, quote the answer, then read the same fact back
from the store it came from.

| # | Method | Reached by a person through | Served evidence (quoted) | Read-back |
|---|---|---|---|---|
| A1 | `today` | GuestOps → **Today** (and the *Today at the Desk* widget) | the four counts and the list | stays for the business date, by state, equal the counts |
| A2 | `me` | the bar, top right — name · department · property | the two strings | the staff and property names in Master Data for this user |
| A3 | `attention` | **Attention** tab | the list and its total | the open disagreements and held facts, counted where they live |
| A4 | `occupancy` | *Occupancy* widget | the per-type rows | stays in house by room type |
| A5 | `feed` | *From the PMS* widget | the facts listed | the feed rows it reads |
| A6 | `mix` | *Business Mix* widget | the channel and market lines | stays by channel and by market |
| A7 | `watchlist` | *Watchlist* widget | late and waiting rows | the stays meeting each rule |
| A8 | `bookings` | **Bookings** tab, and page 2 | the page and `showing x–y of N` | bookings counted where they live — **N must not stop at a page size** |
| A9 | `booking` | Bookings → a booking | its stays and summary | that booking's stays |
| A10 | `cancelPlan` | a booking → **Cancel…** (the dialog, not the confirm) | the penalties and consequences | nothing written: the booking re-read unchanged |
| A11 | `availability` | Bookings → **New booking** | the grid | room-type inventory for the dates |
| A12 | `stay` | Today → a guest | the stay page | that stay's row |
| A13 | `activity` | stay → **Activity** | the timeline | that stay's events |
| A14 | `requests` | stay → **Requests** | the list, and Jobs' presence | that stay's requests |
| A15 | `payment` | stay → **Payment** | the folio | that stay's folio rows |
| A16 | `servicing` | stay → **Servicing** | the servicing tab, and Room Care's presence | that stay's servicing facts |

## B · Setup — module door, `desk.configure`

| # | Operation | Reached by a person | Served evidence | Read-back |
|---|---|---|---|---|
| B1 | `setup` (read) | **Setup** tab | the settings shown | the settings row |
| B2 | save settings | **NOT reachable** — `Save` on the Setup bar has no handler (`screens/setup/index.ts:93`); `SaveSettings` exists only on the gRPC door | not driven from a screen | — |

## C · Writes

| # | Operation | Door | Reached by a person | Status |
|---|---|---|---|---|
| C1 | cancel a booking | module, `stay.override` | Bookings → booking → **Cancel…** → confirm | **BLOCKED BY ADR 0193 — and NOT DRIVABLE on this property: the store holds no booking to cancel** (precondition 3, measured). *Planned* as one drive to capture the refusal — expected the InvalidArgument above, drawn by the SDK as *faulted* — and it runs only if a booking reaches the store by a supported path. Read-back: the booking and every stay unchanged — the refusal comes before any write (`StayLifecycleService.RequireWritableAsync` authorizes on its first line, on the first stay) |
| C2 | walk-in | module, `stay.create` → then `stay.assign` + `stay.override` on the stay | **not reachable** — the sheet's *Create and check in* has no handler (`screens/walkin/index.ts:87`; a gap reported earlier, not built) | **BLOCKED BY ADR 0193** for its assign and override halves, and unreachable besides |
| C3 | assign a room | gRPC `AssignRoom`, `stay.assign` | no screen | **BLOCKED BY ADR 0193** |
| C4 | check in · check out · cancel stay · no-show · correct | gRPC, `stay.override` | no screen | **BLOCKED BY ADR 0193** |
| C5 | capture a registration · read it back | gRPC, `registration.capture` · `reservation.read` on a stay | no screen; the card's `Save` captures nothing | **BLOCKED BY ADR 0193** — including the READ at `RegistrationService:102` |
| C6 | record a filing | gRPC `RecordFiling`, `reporting.file` | no screen | **BLOCKED BY ADR 0193** |
| C7 | log a request · add a note | gRPC, `request.handle` | no screen | **BLOCKED BY ADR 0193** |
| C8 | reconciliation (clear · accept) | none — `ReconciliationService` is registered and called by nothing | no door at all | **not reachable by any door**; its two stay-scoped checks would also block |

`stay.create` on its own (`CreateBooking` on gRPC) is property-scoped and is
not blocked by ADR 0193 — but it has no screen, and a booking made on the
owner's live property is a write this list does not propose without the
owner's yes.

`guest.amend` is **not a row**: no version since 0.1.0 declares it — checked at
0.3.4 — and nothing in the backend calls it; `Permissions.GuestAmend` is an unused
constant. *Stated by what the manifest does rather than by a version number, so it
does not go stale on the next cut.*

## D · The consumer

| # | Operation | Door | Status |
|---|---|---|---|
| D1 | `job.created` → a request learns its job | NATS, `JobCreatedHandler` → `StayRequestService.RecordJobAsync` (no authorization — a consumer has no person) | **cannot occur** until C7 can log a request for a job to answer |

## Method — the read-back instrument

> **An instrument the ARCHITECT CHOSE, 2026-09-19 — not a ruling.** Told to the
> owner, who may overrule it. Nobody should read this row later as ruled.

```text
docker exec hotelos-postgres psql -U postgres -d hotelos
    inside BEGIN READ ONLY … COMMIT
    SELECT only
    the development cluster only — NEVER port 15432
    every query recorded WORD FOR WORD in the ledger, beside its result
```

Why it crosses no boundary, in the architect's words: it never reads the sealed
application credential, so ADR 0156's boundary is not crossed, and it is not
test code, where CLAUDE.md's *no SQL in a test* applies.

**Measured before trusting it — is the dev cluster the property the drive
writes to?** The installed product's Kernel (`C:\ProgramData\HotelOS\config\
hotelos.toml`) is configured for **15432**, and a read-back of a different
database than the one written would measure nothing. Asked of the instrument
itself, 2026-09-19:

```sql
BEGIN READ ONLY;
SELECT package_id, version, state, updated_at FROM platform.packages ORDER BY package_id;
COMMIT;
```
```text
guestops  | 0.1.0 | stopped | 2026-09-17 10:14:31+00
jobs      | 0.4.1 | running | 2026-09-19 04:24:48+00
openai    | 1.0.0 | stopped | 2026-09-04 11:25:45+00
workforce | 0.3.0 | running | 2026-09-19 04:24:46+00
```

That is the installation the architect described (*"the installed GuestOps is
0.1.0 (stopped)"*), so **the owner's property is on the development cluster and
the instrument reads the database the drive will write.** The 15432 Kernel is a
separate installed product that does not host GuestOps. Re-checked at the start
of the run: after the install this row must read **0.3.4 · running**.

## Precondition 3, measured before the run — the store is empty

```sql
BEGIN READ ONLY;
SELECT 'bookings', count(*) FROM guestops.bookings
UNION ALL SELECT 'room_stays', count(*) FROM guestops.room_stays
UNION ALL SELECT 'guests', count(*) FROM guestops.guests
UNION ALL SELECT 'stay_disagreements', count(*) FROM guestops.stay_disagreements
UNION ALL SELECT 'held_facts', count(*) FROM guestops.held_facts
UNION ALL SELECT 'settings', count(*) FROM guestops.settings
UNION ALL SELECT '__migrations', count(*) FROM guestops.__migrations;
COMMIT;
```
```text
bookings 0 · room_stays 0 · guests 0 · stay_disagreements 0 · held_facts 0 · settings 0 · __migrations 2
```

Three consequences, stated now so the certificate does not discover them:

1. **Every read in A answers empty.** That is *served* under ADR 0148 (a
   legitimate empty result), and the certificate says what it therefore proves:
   **the pipe — door, authorization, route, query, render — and not the logic**
   that computes a count from rows.
2. **C1 cannot be driven.** There is no booking to press *Cancel…* on, so the
   refusal cannot be captured as evidence. It stays BLOCKED BY ADR 0193 and
   **not driven**, never *driven and refused*.
3. **No supported path on this property creates a booking.** `stay.create` is
   gRPC-only, the walk-in sheet's submit has no handler, and no PMS connector
   is installed (the four rows above are all there is). A row written by hand
   would start the world outside every supported path — the fixture rule of
   ADR 0166 — so none is written.

A first query guessed the table name `guestops.stays`; it does not exist
(`room_stays` does). Read-only, so nothing happened; recorded because a guess
that errors is a query like any other.

## The second certificate — a supported path to a booking, and what walk-in measured

The data-bearing run needs a booking to reach the store by a supported path.
Two routes exist and both are further off (architect, 2026-09-19): **the PMS
connector feeding** — the Oracle milestone after *Test connection* — or **the
walk-in submit wired.** *No row is written by hand* (ADR 0166).

**Walk-in, MEASURED 2026-09-19 before anyone builds its handler — it is NOT one
transaction.** `WalkInCommand` calls `BookingService.CreateAsync`, then
`StayAssignmentService.AssignAsync`, then `StayLifecycleService.CheckInAsync`,
and `CreateAsync` commits on its own (`SaveChangesAsync`, line 56); nothing in
GuestOps or the platform's module door opens a transaction around the three.

Run, not read: a service-suite test on a per-run scratch database (ADR 0157),
the real `EventAppender` into the real event store, and an authorizer answering
as the Kernel does — property scope answered, `stay` refused with
`authz/registry.rs:282`'s sentence:

```text
checks asked         stay.create on property -> stay.assign on stay
refused with         permission "stay.assign" may be checked against property, but the object is a stay
committed bookings   1
committed stays      1   [Booked, walk_in=True]   -- no room, never checked in
committed assignments 0
committed events     3   [guest.created, reservation.created, stay.created]
```

**So a refused walk-in leaves a half-written booking and announces it**: three
events reach the outbox, and every consumer is told a guest, a booking and a
walk-in stay exist for somebody the desk turned away. The only double in the
run is the authorizer; which exception it throws cannot change the answer,
because `CreateAsync` has committed before the second check is asked.

**The measurement was not kept as a test** — it asserts nothing about what
*should* happen, and a committed test recording a defect as its expectation
would lock in the behaviour it was written to question. It was run twice and
removed.

> **SUPERSEDED the same day — RC-Q8a and RC-Q8b-2 (ADR 0193).** The promise
> below, *"a refused walk-in leaves nothing"*, was withdrawn: it *"was never an
> architectural decision"*. A walk-in is now two authorized operations — phase 1
> creates and commits the stay, phase 2 (assign + check-in, all or nothing) is
> authorized when it starts — and **a refused phase 2 leaves a booked, room-less
> stay and tells the desk so**. `WalkInAtomicityTests` became
> `WalkInPhasesTests`, rewritten under ADR 0034. What follows is kept as the
> record of the one-transaction version.

**FIXED in source for GuestOps' next version (architect's assignment,
2026-09-19): a refused walk-in now leaves nothing.** Create, assign and check-in
run in one transaction with their events. The checks cannot all be asked first
— `stay.assign` and the check-in are asked of the stay the create mints, and
asking at property scope would widen the grant — so the transaction is the
mechanism, and the reason is written at the site.

**The positive control found a second defect nobody had driven into: an ALLOWED
walk-in could never succeed.** Check-in was asked for `stay.Version + 1`, but the
assign had already bumped the tracked instance, so it asked for a version one
past the row's and threw `ConcurrencyException` every time. It now passes the
version the assign returned.

`WalkInAtomicityTests`, committed, asserting the correct behaviour — red at HEAD,
then green:

```text
                          at HEAD (unfixed)                              fixed
refused at stay.assign    (1, 1, 1, "guest.created, reservation.created,   (0, 0, 0, "")
                                      stay.created")
refused at check-in       (1, 1, 1, "guest.created, reservation.created,   (0, 0, 0, "")
                                      stay.assigned, …")  — room kept too
allowed                   ConcurrencyException: has changed since         1 booking · 1 stay
                          version 3 was read                              in house, events incl.
                                                                          stay.assigned, stay.arrived
```

Figures are *(bookings, stays, guests, events)*. Full backend suite 139/139.

*This said "Not demonstrated: a mutation removing only `CommitAsync` … reasoning,
not a run" until the architect asked for the run.* **Now shown**, in a detached
worktree at `e53a439` (contains `f13e47c`): with only `CommitAsync` removed the
mutant compiles and the allowed case fails **Expected (1, 1, 1) · Actual
(0, 0, 0)** while both refused cases still pass; restored, 3/3; worktree removed.

**THE ALLOWED ROW IS PROVEN ONLY AGAINST A STAND-IN AUTHORIZER — on the real
platform it cannot pass** (architect, 2026-09-19). ADR 0061: *"Canonical entity
lifecycle events are the source of authorization object registration. The
Kernel's authorization subsystem consumes them and materialises the graph."* A
stay's tuples come from `stay.created` after the event is relayed — **after this
transaction commits** — so `stay.assign` on the new stay, asked inside it, finds
no tuples and is refused every time. Today it is refused a step earlier:
`stay` is not among the 13 types the Kernel registers
(`events/registration/mod.rs:142`), and no GuestOps permission declares a
`stay` scope. **The row proves the transaction commits when allowed — nothing
about whether a walk-in can be.** Acting on an object in the step that creates it
is Room Care's `RC-Q8` shape; the architect has put both to the planner as one
question. ~~Kept whatever the answer: a refused walk-in leaves nothing.~~ *Withdrawn by RC-Q8a — see the note above.*

**The handler is still not wired**: its assign and check-in steps are
stay-scoped and wait on ADR 0193 being built — and on that question
(architect, 2026-09-19).

**Found beside it, unattributed and left alone**: three per-run application
roles on the development cluster — `hotelos_app_guestops_071c86a5`, `_44c7833f`,
`_6e0f62b9` — with no scratch database behind them. **This session's runs add
none and leave none** (role list before and after a run: 3 and 3, identical),
so the teardown works and they are older runs that never reached it. Not
dropped: they cannot be attributed, and a cluster role is not mine to remove on
a name match.

## E · The 13 control sites — and the Part C combination each is pressed against

> **MOVED HERE FROM `part-c-coverage.md` — 2026-10-01**, on the owner's
> correction that *"Part C is a DATA DRIVER… it finishes when the data exists, not
> when a control has been pressed."* **Creating the shape is Part C's; pressing the
> control is Part B's**, so the sites belong in this list and the combination
> belongs beside each one.
>
> **The pairing is the point.** A pager pressed against one page of rows renders
> and does not page, and *that passes Part A completely* — so each row below names
> the shape that makes the press mean something, and whether Part C has created it
> yet. **A row whose combination is `NOT DRIVEN` cannot be driven here**, and that
> is a Part C dependency rather than a Part B failure.

**Derived: the page sizes are read from the screens, not assumed — they are not
all 25.** `instant(…, "date")` is **formatting, not a control**, and is excluded;
counting a formatter as a date field would put sites in this list that nobody can
press.

### E1 · Pagers — 4 sites, each needs MORE ROWS THAN ITS OWN PAGE

| Site | `PAGE` | Part C combination needed | Part C status | Passes when |
|---|---|---|---|---|
| `attention:98` | **10** | ≥ 11 stays in an attention state | **NOT DRIVEN** — needs `CreateBooking` + a disagreement path | page 2 reached, and page 2 holds stays page 1 did not |
| `booking:112` | **12** | one `CreateBooking` with ≥ 13 `stays` — the `stays` repeated field at *many* | **NOT DRIVEN** — `CreateBooking` undriven | the booking's own stay list pages |
| `bookings:78` | **25** | ≥ 26 `CreateBooking` calls | **NOT DRIVEN** | page 2 reached, ordering stable across the turn |
| `today:119` | **25** | ≥ 26 stays arriving on ONE business day | **NOT DRIVEN** | page 2 reached, **and the strip's count is not the page's length** |

**The last column is the point.** *A total taken from a capped read stops growing
at the cap* — so the count beside the pager is read against the rows, not against
what one page returned. **`today:119` is the row where that bites**: the strip's
four counts and the list come from one read, and a count that equals 25 forever is
reporting a limit and calling it a quantity.

### E2 · Tabs — 2 `tabs()` callers plus the bar

| Site | Part C combination needed | Part C status | Passes when |
|---|---|---|---|
| `stay:169` | **three stays**: one with activity, requests, servicing and payment data; **one with none of them**; one with more than fits | **NOT DRIVEN** — needs `LogRequest`, `AddNote`, `RecordFiling` | renders in all three, and an empty tab says *nothing here yet* **with why** rather than drawing blank |
| `today:91` | stays across `StayLifecycle`'s **8 members**, via `CorrectStay.to` — the single write enum | **NOT DRIVEN** — `CorrectStay` undriven, and 0 of 8 enum cases | every view switches, counts agree with the list below, and `UNSPECIFIED` is **REFUSED** |
| the top bar | **none** — no data needed | n/a | five tabs change screen |

**The empty-tab row is the one Part A cannot see at all.** A tab that draws
nothing and a tab that says *nothing here yet, because this stay has no requests*
are the same zero nodes to a fidelity sweep and different screens to a person.
*That is defect 2's fix being driven, not re-asserted.*

### E3 · Dropdowns — 4 sites, one shared control

| Site | Choices come from | Part C combination needed | Part C status |
|---|---|---|---|
| `registration:242` | `AcceptedIdTypes` | `SaveSettings` with **0, 1 and 3** accepted types — the empty·single·many axis landing on a control | **BLOCKED — see the correction below.** Part C drove all three; the OWNER cannot set any of them |
| `assign:226` | `free.rooms` | Master Data rooms through **its own API** (ADR 0166), with 0, 1 and many free | **BLOCKED** — Master Data's, not GuestOps' |
| `walkin:397` | room-type options | the same, via room types | **BLOCKED** — same |
| `bookings/filters:49` | a static filter set | **none** | n/a — asserts every member listed and the chosen one applied |

### ⚠ I WROTE THAT PART C UNBLOCKED THIS, AND IT DOES NOT — corrected 2026-10-02

**The row above said `CREATED, 2026-10-01 — PartCSettingsDriver, all three`.** That was
a true measurement of something narrower than the sentence it went into: Part C drove
`SaveSettings` **through the service, in a scratch database that is dropped when the run
ends** (`GuestOpsScratch` creates `hotelos_guestops_test_<guid>` and discards it on
dispose). *No row Part C creates ever reaches the owner's property.*

```text
Part C proves   the API accepts or refuses every combination   a CAPABILITY claim
Part B needs    rows on the OWNER'S property, by a supported    a DATA claim
                path
```

**And the owner has no path to this one.** `screens/setup/index.ts:100` disables Setup's
`Save` deliberately, with its reason beside it — §2, C11 — because **`SaveSettings` is
reachable only through the gRPC door**: `desk.configure` maps exactly one method,
`setup`, which is a read. So on a fresh property:

```text
no settings row            →  Setup renders (GUEST-Q15) and its Save is OFF
                              by design
SaveSettings               →  no module method. gRPC only, and no screen calls it
so the row is never saved  →  MintCardNumber's PreconditionFailed is PERMANENT
                              through the desk, and the registration card can
                              never be captured on the owner's property
```

**My 409 did not create that** — before GUEST-Q15 the card 404'd on an unconfigured
property, so capture was unreachable then too. It made the refusal legible rather than
reachable. *But B2 already recorded that Save has no handler, and I cited Part C's
coverage as if it closed the gap anyway.*

> **So of the 13 control sites, the number the owner can drive today is TWO** — the top
> bar's tabs and `bookings/filters:49`, both of which need no data — **and not three.**
> `registration:242` joins the eleven.

**`registration:242` is still the one the owner met**, and it is the control whose
failure is now fully traced. Its choices were empty because the card never rendered — the
settings deadlock — so this row is driven *after* `7ba3d74d` and proves both
halves: **the list is populated, and the chosen value is what is written.** Press
it against each of the three combinations: **0 accepted types is the interesting
one**, because an empty dropdown is indistinguishable from a broken one on screen
and only the zero case says which.

**⚠ And `registration:242` has a precondition — now stated ON THE SCREEN.** On a
property with no settings row the card screen renders (GUEST-Q15) and **capture is
refused 409** — *"a registration card cannot be numbered until this property's
GuestOps settings have been saved"* (`SettingsService.cs`, 2026-10-01). So **save
Setup before driving any registration row**, and B1 comes before E3.

*This paragraph said only that the owner would hit the refusal, which was true and
is no longer the whole of it.* **Owner decision B, 2026-10-01**: the card-number
field now reads **"Set up GuestOps first"** and draws no number, where it used to
draw a prospective `GRC-1` the capture would refuse. **So E3 has a drive row of
its own**: open the card on an unconfigured property and the field says what to
do — *the screen prevents the 409, the boundary still refuses it, and neither
stands in for the other.* Both are worth pressing, in that order.

### E4 · Date fields — 3 sites, one shared control

| Site | Part C combination needed | Part C status | Passes when |
|---|---|---|---|
| `newbooking/query:88` | arrival/departure at each boundary, **and a rejection either side** — departure before arrival, a same-day pair | **NOT DRIVEN** — `CreateBooking` undriven | takes a valid date, refuses an invalid one **with a reason**, round-trips |
| `walkin:377` | the same on the walk-in sheet | **NOT DRIVEN** | as above — and the sheet's submit has no handler (C2) |
| `field.ts:173` | the **7** date-kind card boxes — `date_of_birth`, `id_expiry`, `passport_issue`, `passport_expiry`, `visa_issue`, `visa_expiry`, `arrived_in_country_on` | **NOT DRIVEN** — `CaptureRegistration` undriven | each opens, takes a date, and the value survives `CaptureRegistration` → `GetRegistration` **unchanged** |

**`chrome/field.ts` is ONE control for both the dropdowns and the date fields**
(`:147` and `:173`), so a defect there shows on both — which is why the owner
reported them together. **Measured at `e0361fbf` the control is sound**: it builds
the `<select>`, a blank option carrying the placeholder, one option per choice,
sets `.value` and wires `change` → `onChange`. **So an empty `choices` is the
failure mode, not a broken control** — and the one cause of that recorded here is
the settings deadlock.

### What a result cell in E may say

```text
PASSED       the control did the thing, against data Part C created
FAILED       it did not, and the row names what happened
NOT DRIVEN   no attempt — never a pass, and never an empty cell
BLOCKED      the Part C combination does not exist yet, or a named
             precondition outside GuestOps (Master Data rooms)
```

**12 of the 13 are `BLOCKED` today, and Part C finishing did not change that** —
corrected 2026-10-02, having first written 11 with `registration:242` drivable. **Only
the top bar's tabs and `bookings/filters:49` need no data.** Everything else needs rows
on the owner's property, and the two supported paths to one are still absent: the PMS
connector feed, or the walk-in submit wired. *Part C drove all 12 writes and left no row
behind, by construction — its databases are dropped.*

> **The useful question for the next round is therefore not "is Part C done".** It is
> **how data reaches the property the owner walks** — and nothing in Part C answers it.

## What the certificate will say it did not prove

- **the atomicity of a refused multi-stay cancel** — the refusal lands on the
  first stay, so the case that would test it cannot occur until C1 unblocks;
- **anything on the gRPC door** — no stream holds a caller with GuestOps'
  connector identity on this property;
- **installation history** — ADR 0143;
- **any control whose Part C combination is `NOT DRIVEN`** — §E counts them: 11
  of 13. A control pressed against one shape says nothing about the next, which
  is the blindness Part C exists to close and has closed for one write of twelve.

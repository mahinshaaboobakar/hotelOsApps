# GuestOps Part B — the drive list

> **Scope, and the certificate's first line: 16 reads served against an EMPTY
> store prove the door, authorization, route, query and render — not the logic
> that computes figures from rows.** Empty answers are reported as empty, never
> as correct figures. Cancel is BLOCKED BY ADR 0193 and **not driven**: no
> booking exists to cancel. — *architect's decision, 2026-09-19: run pipe-only as
> soon as 0.3.1 is installed, and certify exactly that. The data-bearing run is a
> second, separate certificate, not a blocker for this one.*

Prepared 2026-09-18 by FF against `HotelOsApps` at `a4b6723` and platform
`HosPilotOS` at `885677ec`. **To run once the owner has installed 0.3.1**
(`guestops-0.3.1.hopkg`, 16,278,129 bytes, sha256 `4248ecf5…fd1ca8`) on the
fixed desktop. Nothing below has been run yet; every result cell is empty on
purpose.

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

1. **The package**: Software Center lists GuestOps **0.3.1, Running**. If it
   shows 0.1.0 or Stopped, stop — nothing below is about 0.3.1.
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

`guest.amend` is **not a row**: 0.3.1 no longer declares it (0.1.0 did), and
nothing in the backend calls it — `Permissions.GuestAmend` is an unused
constant.

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
of the run: after the install this row must read **0.3.1 · running**.

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
**Not demonstrated**: a mutation removing only `CommitAsync` — the positive
control should catch it (it would find 0 bookings), which is reasoning, not a run.

**The handler is still not wired**: its assign and check-in steps are
stay-scoped and wait on ADR 0193 being built (architect, 2026-09-19).

**Found beside it, unattributed and left alone**: three per-run application
roles on the development cluster — `hotelos_app_guestops_071c86a5`, `_44c7833f`,
`_6e0f62b9` — with no scratch database behind them. **This session's runs add
none and leave none** (role list before and after a run: 3 and 3, identical),
so the teardown works and they are older runs that never reached it. Not
dropped: they cannot be attributed, and a cluster role is not mine to remove on
a name match.

## What the certificate will say it did not prove

- **the atomicity of a refused multi-stay cancel** — the refusal lands on the
  first stay, so the case that would test it cannot occur until C1 unblocks;
- **anything on the gRPC door** — no stream holds a caller with GuestOps'
  connector identity on this property;
- **installation history** — ADR 0143.

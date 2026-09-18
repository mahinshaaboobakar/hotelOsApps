# GuestOps Part B — the drive list

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

> **BLOCKED BY AUTHZ-Q37** — the operation is authorized against a GuestOps-owned
> object type (`stay`) that no mechanism registers, so it is refused for every
> caller by construction. Recorded, never scored.

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
is the AUTHZ-Q37 class, with a **second prerequisite**: even once object types
are admitted, `permissions.yaml` has to gain the `stay` scope its own comment
promises.

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
| C1 | cancel a booking | module, `stay.override` | Bookings → booking → **Cancel…** → confirm | **BLOCKED BY AUTHZ-Q37.** Driven once to capture the refusal as evidence: expected the InvalidArgument above, surfaced by the SDK as a *faulted* state. Read-back: the booking and every stay unchanged — the refusal comes before any write (`StayLifecycleService.RequireWritableAsync` authorizes on its first line, on the first stay) |
| C2 | walk-in | module, `stay.create` → then `stay.assign` + `stay.override` on the stay | **not reachable** — the sheet's *Create and check in* has no handler (`screens/walkin/index.ts:87`; a gap reported earlier, not built) | **BLOCKED BY AUTHZ-Q37** for its assign and override halves, and unreachable besides |
| C3 | assign a room | gRPC `AssignRoom`, `stay.assign` | no screen | **BLOCKED BY AUTHZ-Q37** |
| C4 | check in · check out · cancel stay · no-show · correct | gRPC, `stay.override` | no screen | **BLOCKED BY AUTHZ-Q37** |
| C5 | capture a registration · read it back | gRPC, `registration.capture` · `reservation.read` on a stay | no screen; the card's `Save` captures nothing | **BLOCKED BY AUTHZ-Q37** — including the READ at `RegistrationService:102` |
| C6 | record a filing | gRPC `RecordFiling`, `reporting.file` | no screen | **BLOCKED BY AUTHZ-Q37** |
| C7 | log a request · add a note | gRPC, `request.handle` | no screen | **BLOCKED BY AUTHZ-Q37** |
| C8 | reconciliation (clear · accept) | none — `ReconciliationService` is registered and called by nothing | no door at all | **not reachable by any door**; its two stay-scoped checks would also block |

`stay.create` on its own (`CreateBooking` on gRPC) is property-scoped and is
not blocked by AUTHZ-Q37 — but it has no screen, and a booking made on the
owner's live property is a write this list does not propose without the
owner's yes.

`guest.amend` is **not a row**: 0.3.1 no longer declares it (0.1.0 did), and
nothing in the backend calls it — `Permissions.GuestAmend` is an unused
constant.

## D · The consumer

| # | Operation | Door | Status |
|---|---|---|---|
| D1 | `job.created` → a request learns its job | NATS, `JobCreatedHandler` → `StayRequestService.RecordJobAsync` (no authorization — a consumer has no person) | **cannot occur** until C7 can log a request for a job to answer |

## Open before the run — one question, with the facts

**Which instrument reads the `guestops` store back on the owner's property?**
The database credential is sealed. ADR 0156's words are about **tests** —
*"Tests must not read, export, or otherwise bypass the installed secret-store
boundary"* — and a Part B run is not a test; but nothing grants it an exception
either, and deciding that one exists is not mine. The reads in A can be compared
against a second door (the gRPC `ListStays` over the same store), but that is
**the same code read twice**, not a read-back, and the certificate would say so.
Until an instrument is ruled, the read-back column records *not measured here*,
never a value.

## What the certificate will say it did not prove

- **the atomicity of a refused multi-stay cancel** — the refusal lands on the
  first stay, so the case that would test it cannot occur until C1 unblocks;
- **anything on the gRPC door** — no stream holds a caller with GuestOps'
  connector identity on this property;
- **installation history** — ADR 0143.

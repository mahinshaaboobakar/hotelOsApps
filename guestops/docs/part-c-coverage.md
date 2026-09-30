# GuestOps Part C — the input space, driven through GuestOps' own API

> **Part C is neither Part A nor Part B, and it exists because neither could see
> what the owner found** (ADR 0358, owner ruling 2026-09-30, `APPS-Q65`).
>
> ```text
> Part A   an approved frame beside a harness rendering of FIXTURES
>          → drawing fidelity. NEVER sign-off (owner, 2026-09-19)
> Part B   the owner walks the installed build, pressing every action
> Part C   GuestOps' OWN APIs create data, in every combination the wire
>          admits, and every control is operated against it
> ```
>
> **The blindness both share is structural.** Part A compares two renderings and
> *"a fidelity sweep cannot find a missing control — it measures nodes that exist,
> and an absence has no node"* (HH, 2026-09-10). Part B presses controls against
> whatever rows the property already holds. **Neither drives an input space**, so
> a control that works for one value and fails for the next is invisible to both.
> A dead dropdown, a pager that does not page and a date field that does not open
> all render correctly.

**Derived at HotelOsApps `e0361fbf`, from `backend/src/protos/hotelos/guestops/v1/{service,dto}.proto`
and `backend/src/Module/ModuleSurface.cs`. Not written out** — ADR 0358 §2
requires the set to come from the contract, because *"a combination set written
out by hand is a hand-kept list, with this repository's usual result."*

**No run is recorded below.** The first run is owed and the cluster it needs is
down — see §"Why nothing has been driven yet", which is dated, so a reader can
tell a standing gap from a stale sentence.

---

## Two populations, stated separately, and they are never summed

This is the first thing the document must get right, because adding these two
numbers invents a denominator that describes nothing:

```text
population 1   the gRPC input space      12 writes · 114 leaf field positions
               what a caller can send. Answers NONE of the four things the
               owner pressed

population 2   the module surface        32 methods · 7 handlers · 13 control
                                         sites
               what a person touches. This is what the owner pressed
```

**The owner's finding was pagination, dropdowns, date choosers and tabs — all
four live in population 2.** Population 1 is necessary and is not the population
that answers them. A Part C covering only the first would be honest about what it
drove and silent about what was actually pressed.

---

## Population 1 — the gRPC input space

```text
18 RPCs                    12 writes · 6 reads
58 direct request fields   across the 12 writes
  2 are messages           CreateBooking.stays → NewStay (30 fields, nested:
                             NewGuest · CommercialTerms · Money ×2 · TaxBasis)
                           CaptureRegistration.card → RegistrationEdit (29)
114 leaf positions         12 of them the shared RequestContext, so 102 are
                             GuestOps' own
```

The 12 writes: `CreateBooking` · `AssignRoom` · `CheckIn` · `CheckOut` ·
`CancelStay` · `RecordNoShow` · `CorrectStay` · `CaptureRegistration` ·
`RecordFiling` · `LogRequest` · `AddNote` · `SaveSettings`.

### Where ADR 0358's axes actually bite — measured, not assumed

**ADR 0358 qualifies two of its axes with *"where the wire admits it"*, and the
qualifier is load-bearing. A coverage document that claimed an axis this wire
cannot express would be a claim about its own method, checked by nobody** — so
each limit is stated with the number behind it.

**The two absences are drivable at 8 of 114 positions, and in exactly one
scalar.** ADR 0358 asks for *"the two absences where they differ (never sent ·
sent empty)"*. proto3 gives a plain scalar **implicit presence**, so for 106
positions *never sent* and *sent empty* are **the same bytes** and no driver can
distinguish them. The eight that can:

```text
CreateBooking.stays                              message   — inherent presence
CreateBooking.stays.guests                       message
CreateBooking.stays.guests.is_primary            optional bool  ← the ONLY scalar
CreateBooking.stays.terms                        message
CreateBooking.stays.terms.amount                 message
CreateBooking.stays.terms.penalty_amount         message
CreateBooking.stays.terms.cancellation_deadline  Timestamp
CaptureRegistration.card                         message
```

*Seven of the eight have presence because they are messages or a well-known type,
which is a property of protobuf rather than a decision anybody made about
GuestOps. Only `is_primary` was declared `optional` deliberately.*

**The enum axis is 11 member cases, not 24.** GuestOps declares five enums with
24 members between them, and **one appears on a write request**:

```text
StayLifecycle   8 members incl. UNSPECIFIED   CorrectStayRequest.to   ← the only one
TaxBasis        3 members                     nested, via Money in NewStay.terms
StayView        5 members                     ListStaysRequest — a READ filter
TimeBasis       4 members                     StayTime — a response type
AbsenceReason   4 members                     Absence — a response type
```

`AbsenceReason`'s three named absences — `NOT_SUPPLIED`,
`NOT_AVAILABLE_FROM_SOURCE`, `UNREADABLE` — are the gap rule already in
GuestOps' own contract. **They are read, not driven**: nothing a caller sends
chooses them, so Part C asserts what the wire returns rather than covering an
input.

**`UNSPECIFIED` is a case, not a gap.** Both write enums carry it, so *"an
unmapped value where the wire admits one"* is satisfied by sending zero — and a
refusal is the expected answer for `CorrectStay`, which is asserted rather than
assumed.

**Empty · single · many is five fields on the write path:**

```text
CreateBooking.stays                       repeated NewStay
CreateBooking.stays.guests                repeated NewGuest
SaveSettings.required_for_home_country    repeated string
SaveSettings.required_for_visitors        repeated string
SaveSettings.accepted_id_types            repeated string
```

*The last three are the stored selections GUEST-Q15 ruled application state.
Part C drives them wherever they live; the ruling does not change their shape on
the wire.*

**Page 2 exists on exactly one gRPC surface.** `ListStaysRequest/Response` carry
`PagedRequest`/`PagedResponse`. The other three repeated responses —
`ListStaysResponse.stays`, `GetAvailabilityResponse.availability`,
`ListOutstandingFilingsResponse.filings` — are **unpaged**, so they take empty ·
single · many and have **no page-2 axis at all**. The pagers the owner found are
in population 2 and are counted there.

---

## Population 2 — the module surface, which is what was pressed

**32 methods across 7 capability handlers** — derived from `ModuleSurface.cs`'s
switch arms, not from the manifest's permission list:

```text
reservation.read       20   today desk me attention occupancy feed mix watchlist
                            bookings booking cancelPlan noShowPlan correctPlan
                            availability rooms stay activity requests payment
                            servicing
stay.override           5   cancel checkIn checkOut noShow correct
stay.create             2   walkIn book
registration.capture    2   card capture
stay.assign             1   assign
request.handle          1   note
desk.configure          1   setup
```

### The 13 control sites, and the two shared controls behind them

```text
pagers       4   attention:98 · booking:112 · bookings:78 · today:119
                 shared control: chrome/pager.ts
tabs         2   stay:169 · today:91, plus the top bar's own strip
                 shared control: chrome/panel.ts
dropdowns    4   assign:226 · registration:242 · walkin:397 · bookings/filters:49
                 shared control: chrome/field.ts:147
date fields  3   newbooking/query:88 · walkin:377 · field.ts:173
                 shared control: chrome/field.ts:173
```

**`instant(…, "date")` is formatting, not a control**, and is excluded — counting
a formatter as a date field would inflate this list with sites nobody can press.

**`chrome/field.ts` is ONE control for both dropdowns and date fields**, so a
defect there breaks both at once — which is the pair the owner reported together.
Measured at `e0361fbf`: the control itself is sound. It builds the `<select>`, a
blank option carrying the placeholder, one option per choice, sets `.value` and
wires `change` → `onChange`. **So a useless dropdown is an empty `choices`, not a
broken control** — and one cause of that is recorded below.

---

## What Part C covers that the other two cannot

```text
a pager      reaches page 2, and page 2 holds what page 1 did not
a dropdown   opens, lists every member, and the chosen one is what is WRITTEN
a date field opens, takes a date, rejects the ones it should, and ROUND-TRIPS
a tab        renders with data, with none, and with more than fits
```

Every one needs data existing in more than one shape, which is exactly what a
fixture and a property's current rows both fail to supply.

## How the data is created, and three things that are forbidden

**Through GuestOps' own API** — ADR 0358 §4 and ADR 0228:

* **not the database.** A direct write emits no `{type}.created`, so no
  authorization tuple is materialised and GuestOps is refused its own data.
* **not the platform's API standing in for GuestOps'.** The point is to exercise
  *this application's* write path, its validation and its events.
* **not a fixture file.** A fixture is a claim about what the service sends, and
  where nothing checks that claim the harness is a closed loop.

Master Data and Identity data GuestOps references still enters through **their**
APIs — ADR 0166: a fixture enters the model the way production does.

**A combination the API refuses is a finding at the moment it is refused**, not a
defect met four screens later.

---

## The reference project — cited per flow, at `file:line`

> Concept, logic and flow from the Java reference; implementation and
> architecture ours. An incomplete reference is a finding, never a licence to
> invent.

```text
C:\Users\Mahin Aboobakker\Documents\HotelOs-References
  reference\IdeaProjects\IdeaProjects\     64 Maven modules · 24,208 .java
    guest-management-server                GuestOps' — 295 .java excl. target/,
                                           69 front-desk-named, co.reservation.*
```

*A `find` across that tree exceeds 120 s; scope to the module.* Each flow row
below will carry the reference's own `file:line`, or say that the reference does
not cover it — which is a finding about the reference and changes whose gap it
is.

---

## Why nothing has been driven yet — 2026-09-30

**The dev PostgreSQL is unreachable, and the cause is Docker's engine rather than
the cluster.** `docker ps` answers `500 Internal Server Error`
(`dockerDesktopLinuxEngine v1.54`); the port proxy still listens, so:

```text
127.0.0.1:25432   TCP connects, NO protocol reply in 5 s
::1:25432         connection REFUSED
localhost:25432   connection REFUSED        — resolves to ::1 first
```

*Measured with a Postgres `SSLRequest` probe rather than read off a driver error.
The IPv4-hangs / IPv6-refuses split is the proxy binding IPv4 only — **it is not a
PostgreSQL state**, and reading it as one is a layer too low. That correction is
the architect's.*

**Waiting on the owner restarting Docker Desktop.** This section is dated so that
a reader meeting it can tell a standing condition from a sentence that rotted —
this application's own drive list carries *"nothing below has been run yet"* over
three tabs the owner has since walked, and that is the failure to avoid here.

## What this document does not prove

* **Nothing about drawing fidelity.** That is Part A, and Part A is not sign-off.
* **Nothing about the owner's installed build.** Part C runs against a provisioned
  Kernel; ADR 0143 — *"a certificate establishes reproducible platform
  capability, not historical behaviour"*.
* **Nothing about the 106 positions where the wire cannot distinguish the two
  absences.** Stated above with the number, because a silent omission reads as
  coverage.
* **Nothing about population 2 from population 1's figures, or the reverse.** The
  two are not summed and neither stands in for the other.
* **Nothing that has not been run.** Every row below is empty until a run fills
  it, and an empty row is not a pass.

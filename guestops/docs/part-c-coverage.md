# GuestOps Part C — the data, created through GuestOps' own API

> **Part C is a DATA DRIVER, not a harness** — owner correction to ADR 0358,
> relayed 2026-10-01: *"call the apps apis and put data — with all combination."*
>
> ```text
> Part A   an approved frame beside a harness rendering of FIXTURES
>          → drawing fidelity. NEVER sign-off (owner, 2026-09-19)
> Part C   GuestOps' OWN APIs create data, in every combination the wire admits
>          → it finishes when THE DATA EXISTS
> Part B   the owner walks the installed build, pressing every control against
>          the data Part C created
> ```
>
> **So Part C has no `WebApplication`, no bearer token and no HTTP harness, and it
> does not press anything.** Its deliverable is rows. **Operating a control is Part
> B's**, which is why the 13 control sites that were listed here have moved into
> `part-b-drive-list.md`, each carrying the Part C combination it should be pressed
> against.
>
> **The owner corrected the ADR, not the measurement.** Every axis limit below is
> kept verbatim from the version that listed controls, because those numbers are
> what the contract says and they did not change when the deliverable did.

**Derived at HotelOsApps `e0361fbf`, from `backend/src/protos/hotelos/guestops/v1/{service,dto}.proto`.
Not written out** — ADR 0358 §2 requires the set to come from the contract,
because *"a combination set written out by hand is a hand-kept list, with this
repository's usual result."*

---

## The result column is the DATA, and these are the only four things it may say

```text
CREATED       the combination exists as rows, written through the API
REFUSED       the API declined it, and the row names the sentence it declined with
NOT DRIVEN    no attempt. Never a pass, and never an empty cell
BLOCKED       a named precondition outside GuestOps
              (e.g. Master Data rooms, which need their own API first)
```

**`REFUSED` is a result, not a failure.** A validator exists to decline, so a
refusal with the right sentence is the combination passing. **A refusal nobody
expected is the finding** — and it is a finding *at the moment it is refused*, not
a defect met four screens later.

**An empty cell is `NOT DRIVEN` and is written out**, because this application's
own drive list carried *"nothing below has been run yet"* over three tabs the
owner had already walked, and an unlabelled blank is how that happened.

---

## Progress — 3 of 12 writes driven, 2026-10-01

```text
SaveSettings          DRIVEN   20 combinations · PartCSettingsDriver
CreateBooking         DRIVEN   23 combinations · PartCBookingDriver
CaptureRegistration   DRIVEN   by RegistrationCardTests — cited, not duplicated
the other 9 writes    NOT DRIVEN
```

Run 2026-10-01 at Debug on the dev cluster, `dotnet test` and not `--no-build`, so
the figures describe the tree rather than a filesystem.

**`CaptureRegistration` is driven by a suite that already existed, and writing a
driver for it would have been a second characterisation suite** — ADR 0054, and the
no-duplication rule. `A_body_with_no_values_is_refused` **is** presence position 8,
with the reason stated at the test (*"an absent `values` and a card whose every box
is blank are different things"*); `Every_box_the_card_draws_survives_the_save`
drives all 28 fields including the seven date boxes, each on a different day 37
apart, so a command writing the passport's expiry into the visa's is caught. **And
`RegistrationEdit` has no repeated field at all** — `document_refs` is a `string` —
so the empty·single·many axis does not apply to it. *Part C's job here was to find
that out, not to re-assert it.*

**Three of 12 is not a quarter of the input space**, and the table below is what
says so.

### ⚠ The eight presence positions are SEVEN inputs and one response-only field

**This document said eight, and driving them is what corrected it.**
`stays.terms.cancellation_deadline` is reachable from `CreateBookingRequest`, which
is why it was counted — **and it is not an input.**

```text
CommercialTerms.CancellationDeadline(arrival, zone)   COMPUTES it, from
                                                     CancelOffsetDaysFromArrival
                                                     and CancelDropTime
PaymentView.cs:145                                   reads it — a RESPONSE
GuestOpsGrpcService.Bookings.cs:66  ToCommand         maps twelve fields and
                                                     NOT this one
```

> **One `CommercialTerms` message serves request and response, so a caller can set
> a derived field and the service silently ignores it.** The derived-projection
> rule's own case — *"the API has nowhere to put them, which is stronger than
> validating and rejecting, because a client cannot express the mistake"* — and
> here it has somewhere.

**Recorded as a contract finding, not fixed.** Removing a field from a message
shared by both directions is a proto change: `buf breaking`, ADR 0168's
cross-repository consumer check, and a decision about whether the write gets its
own message. *The driver asserts the derivation instead, so the day anybody wires
the sent value through, a test fails.*

### And the amount-presence fold is at a layer this driver does not reach

`ToMoney` collapses *absent* and *present-without-currency* to the same answer —
R19, deliberate and labelled: *"storing zero would make a free stay and an unstated
rate the same row."* **It is a `private static` of the gRPC service.** This driver
calls `BookingService` in-process and hands it a `Money` directly, so **those two
positions are expressible and the fold is not exercised here.** What is driven is
the service's half: an unstated amount reaches storage *present and unstated*,
asserted in two parts because `IsStated ?? false` is false both when the amount is
stored unstated and when it was dropped — *the first version of that test could not
tell the two outcomes apart.*

---

## The input space — 12 writes, 114 leaf positions

```text
18 RPCs                    12 writes · 6 reads
58 direct request fields   across the 12 writes
  2 are messages           CreateBooking.stays → NewStay (30 fields, nested:
                             NewGuest · CommercialTerms · Money ×2 · TaxBasis)
                           CaptureRegistration.card → RegistrationEdit (29)
114 leaf positions         12 of them the shared RequestContext, so 102 are
                             GuestOps' own
```

| Write | Driven | Its densest axis |
|---|---|---|
| `SaveSettings` | **20 combinations** | 3 repeated fields · 2 refusals · 2 booleans |
| `CreateBooking` | **23 combinations** | **7 of the 7 input presence positions** · 2 repeated · the nested 30 |
| `CaptureRegistration` | **by `RegistrationCardTests`** | the nested 29 · 7 date boxes · `card` presence |
| `CorrectStay` | NOT DRIVEN | **the only write enum** — `StayLifecycle`'s 8 members |
| `CheckIn` · `CheckOut` · `AssignRoom` · `CancelStay` · `RecordNoShow` · `RecordFiling` · `LogRequest` · `AddNote` | NOT DRIVEN | scalars and their refusals |

### Where ADR 0358's axes actually bite — measured, not assumed

**ADR 0358 qualifies two of its axes with *"where the wire admits it"*, and the
qualifier is load-bearing. A coverage document that claimed an axis this wire
cannot express would be a claim about its own method, checked by nobody** — so
each limit is stated with the number behind it.

**The two absences are expressible at 8 of 114 positions — of which 7 are INPUTS —
and in exactly one
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

**This axis is now DRIVEN, and the count it was reported under was wrong.** It read
*0 of 8* until 2026-10-01. Driving it corrected both halves:

```text
stays · guests · is_primary · terms          DRIVEN   PartCBookingDriver
amount · penalty_amount                      expressible here; the FOLD is at
                                             the gRPC ToMoney, not reached
cancellation_deadline                        NOT AN INPUT — see the correction
                                             above. A response-only field the
                                             request can express
CaptureRegistration.card                     DRIVEN   RegistrationCardTests
```

**The arithmetic, closed:**

```text
8   expressible on the wire
−1  cancellation_deadline — a response-only field, not an input
 7  INPUT positions
     5  driven to their full distinction   stays · guests · is_primary · terms · card
     2  driven to the SERVICE's half only  amount · penalty_amount
 7  ✓
```

**A count is not a coverage claim**, and the two rows of five and two are why this
section names each position instead of totalling them. *The first draft of this
paragraph said "six of them, and two of those six", which adds up to nothing —
caught by making the column close.*

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
CreateBooking.stays                       repeated NewStay       NOT DRIVEN
CreateBooking.stays.guests                repeated NewGuest      NOT DRIVEN
SaveSettings.required_for_home_country    repeated string        CREATED 0·1·3
SaveSettings.required_for_visitors        repeated string        CREATED 0·1·3
SaveSettings.accepted_id_types            repeated string        CREATED 0·1·3
```

*The last three are the stored selections GUEST-Q15 ruled application state.
Part C drives them wherever they live; the ruling does not change their shape on
the wire.*

**`many` is three, not two.** Two distinguishes empty from non-empty and nothing
more; a screen that renders the first and last of a list while dropping the middle
passes at two. **And the three lists are driven with *different* values as well as
the same ones** — nine identical lists prove each is persisted and nothing about
whether they are separate, so a service writing one list into all three would pass
every count.

**Page 2 exists on exactly one gRPC surface.** `ListStaysRequest/Response` carry
`PagedRequest`/`PagedResponse`. The other three repeated responses —
`GetAvailabilityResponse.availability`, `ListOutstandingFilingsResponse.filings`
and the stay list's siblings — are **unpaged**, so they take empty · single · many
and have **no page-2 axis at all**.

**The pagers the owner found are screens, not RPCs.** Four of them, at page sizes
`10 · 12 · 25 · 25`, and each needs more rows than its own page. **Part C's job is
to create those rows; pressing the pager is Part B's** — the requirement is
recorded in `part-b-drive-list.md` against each site.

---

## The 18 refusals — the population, and what a driver owes each one

**Derived from `grep -rn "throw new InvalidRequestException" backend/src/Application`:
18 throw sites across 8 files.** A separate grep for the type's *mentions* returns
19; both figures are stated because a mention is not a throw, and the gap between
them is where a hand-kept count goes wrong.

```text
SettingsService.cs:90    home_country not 2 chars          REFUSED, driven I · IND · ""
SettingsService.cs:97    reporting_due_hours <= 0          REFUSED, driven -1 · 0
the other 16             NOT DRIVEN
```

**Every refusal is driven beside its accepting counterpart.** A validator that
refused everything would satisfy a refusal-only test — so `IN` is driven with
`I`/`IND`/`""`, and `1`/`24` with `-1`/`0`. **`1` and not only `24`**: 24 is the
declared default, so a validator written `< 24` would pass a test that used it.

---

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

### And the driver calls the service, which is the API in-process

`PartCSettingsDriver.cs` calls `SettingsService.SaveAsync`, not a socket. **That
is the application's own write path — its authorization call, its validation, its
upsert, its version and its events** — and it is what ADR 0228 asks for. *Stated
rather than left: an in-process call to the service is not the same as a gRPC
round trip, so this slice says nothing about serialization, the envelope or the
module door.* Those are the module-surface tier's, which GuestOps now has
(`backend/tests/ModuleSurface.cs`) and which is **not** Part C.

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
will carry the reference's own `file:line`, or say that the reference does not
cover it — which is a finding about the reference and changes whose gap it is.

**`SaveSettings` has no reference row**, and that is a finding rather than an
omission: GuestOps' settings are this platform's own shape — GUEST-Q15 ruled the
eight keys application state on 2026-09-30 — so there is nothing in the Java
reference to cite. *Named here so the empty cell is not read as unfinished work.*

---

## What this document does not prove

* **Nothing about any control.** Part C creates data; it presses nothing. The 13
  control sites and the combination each needs are in `part-b-drive-list.md`.
* **Nothing about drawing fidelity.** That is Part A, and Part A is not sign-off.
* **Nothing about the owner's installed build.** This slice ran against the dev
  cluster; ADR 0143 — *"a certificate establishes reproducible platform
  capability, not historical behaviour"*.
* **Nothing about the amount-presence FOLD.** `ToMoney` is the gRPC service's
  private static; the driver calls `BookingService` in-process and never passes
  through it. Two of the seven input positions are therefore driven to the
  service's half only, which the arithmetic above states.
* **Nothing about the 106 positions where the wire cannot distinguish the two
  absences.** Stated with the number, because a silent omission reads as coverage.
* **Nothing about 14 of the 18 refusals** — `CreateBooking`'s two are now driven
  with their counterparts — or about the single write enum's 8 members.
* **Nothing about the gRPC wire.** The driver calls the service in-process. The
  envelope, the token and the JSON a screen receives are the module-surface tier's,
  and that is also why the amount fold is out of reach here.
* **Nothing about `cancellation_deadline` as an input**, because it is not one. The
  driver asserts the derivation; whether the request should carry the field at all
  is an open contract finding above.
* **Nothing that has not been run.** 3 of 12 writes are driven and the other 9 say
  so by name.

# 02 · The GuestOps design — the reservation book, as an installable application

**Status:** design, 2026-08-31. Stream FF, deliverable 2 of the GuestOps round
— brief `docs/working/45-the-guestops-round.md` §3.2, **in the platform
repository**. Written in parallel with the gold mockup (`docs/mockups/01-…`)
by owner direction of the same day.
**Under:** the owner's rulings **GUEST-Q1** (two modes, staff-may-override),
**GUEST-Q2** + its addendum (the group of room-stays; the anchor is the room
*type*, the room number an assignment), **GUEST-Q3** (the standing override is
the one answer that leaves the application), **GUEST-Q4** (no second mode; a
matching fact confirms silently), **GUEST-Q5** (the PMS-unknown stay and its
staff-confirmed link), **GUEST-Q6** (the book plus commercial terms; the folio
is Finance's), **G360-Q1** (the Guest360 boundary) and **APPS-Q1** (the name,
and the prerequisites).
**Built on:** the scenario record beside this page (`01-…`, S1–S39) and the
PMS study's requirements (`../../../pms-oracle/docs/chapters/02-…`, R1–R28).
**Every design decision below traces to an `S<n>` or an `R<n>`**, and where it
traces to neither it is marked as an implementation choice carrying no
authority.

---

## 0 · What this page is

The scenario record said what must be handled. This page says **how**: the
schema, the state machine, the events, the read views, the permissions, the
tree and the slices. It is a design under review, not a decision record — the
rulings are in the platform register, and an ADR is written when the planner
rules on what §15 reports.

Three things it deliberately does not contain: **no folio** (GUEST-Q6 —
Finance's), **no merge logic** (G360-Q1 — Guest360's), and **no code**. The
brief gates code behind the owner's verification of the mockup and behind the
application-caller authorization round (§14).

---

## 1 · What GuestOps is, in one paragraph

GuestOps is the installable application that owns **reservations, room-stays
and guest identity** — the domain ADR 0089 §CTX-Q2 named and left unbuilt, and
the one the Integration Hub's deferred queue has been filling for. It is the
book in a property with no PMS, and the PMS's counterpart in a property with
one: the same data model either way, differing only in **who writes the stay's
lifecycle** (GUEST-Q1). It owns the `guestops` schema, publishes
`reservation.*` and `stay.*` facts, serves the Context Service's guest chain
through domain-owned read views, and touches no other application's tables.

---

## 2 · The domain

Field lists below are the design's proposal; types are indicative, and the
**canonical contract is the proto** in `shared/protos` (ADR 0026 — there is no
`Contracts/`). Every identifier is a **UUIDv7 of ours**; no PMS identifier is
ever a primary key.

### 2.1 · `Booking` — the group

```text
booking_id          UUIDv7
property_id         the property holding these legs — Master Data ref
expected_stay_count int?   R9 — "noOfRooms: 3" with one room sent (S3, S30)
is_complete         bool?  the SOURCE's assertion, not our arithmetic
origin              staff | pms
created_at

booking_external_ref
  booking_id · integration_id · identifier_kind · external_id
```

**`group_ref` is retired — GUEST-Q9 (M5), adopting the wire.** A booking has
several typed identifiers for the same reason a stay does (R10), and
`GUEST-Q8`'s *minting is the mapping* applies unchanged: the group gets our
UUIDv7 and its source identifiers are recorded beside it, in the same
transaction. A single scalar could hold only one of them and would silently
pick a winner.

**`is_complete` is carried, not computed.** `BookingGroup.is_complete`
(`dto.proto:462`) is the source telling us whether it has sent the whole group
— *"a source that says this group is complete knows something we cannot
compute"*. Our own `expected_stay_count` versus the stays we hold answers a
different question: *how much of what was promised has arrived*. Both are kept
because S30 needs both sentences.

**The group is deliberately thin.** GUEST-Q2 rules that *every operation
happens to a stay, never to the group*, so `Booking` carries identity, the
expectation, and nothing operational. `expected_stay_count` is nullable
because a source may not say — *"three expected, one known"* and *"one known,
expectation unstated"* are different, and collapsing them loses S30's whole
point.

**A group that spans properties is not modelled here** (S4, S32). This
installation holds its own legs; the booking's **source identifiers** are what
make the onward legs *sayable* without being queryable — the same reference
the other property's PMS knows the group by — and no cross-installation read
exists (GUEST-Q2's edge-first rider).

### 2.2 · `RoomStay` — the anchor

```text
stay_id             UUIDv7
booking_id          → Booking
property_id         Master Data ref
room_type_id        Master Data ref — the ANCHOR (GUEST-Q2 addendum)
current_room_id     Master Data ref, nullable — a DERIVED PROJECTION of the
                    open Assignment row; the service resolves it and the API
                    has nowhere to put it (CLAUDE.md §"Clients never write a
                    derived projection")
arrival_at          Timestamp value object (see §2.9)      R12·R13·R14
departure_at        Timestamp value object
lifecycle           Waitlisted | Pending | Booked | InHouse |
                    Departed | Cancelled | NoShow          §3
absences            rows of {field, reason, raw_value}     R6·R9·R25·S11
walk_in             bool — how the guest arrived           S13
pms_unknown         bool — who knows about them (GUEST-Q5) S5
business_date       the property's operating day this stay's arrival
                    belongs to — attached, never computed here (§8)
row_version         optimistic concurrency
```

**`walk_in` and `pms_unknown` are two columns because they are two facts**
(GUEST-Q5's ratified rider): one is how the guest arrived, the other is who
knows about them. A walk-in entered in the PMS is not PMS-unknown; a phone
booking taken here during an upgrade is PMS-unknown and not a walk-in. One
flag would lose the walk-in ratio, which every hotel reports on.

**Absence is a separate axis from `lifecycle`, and this is R1's lesson applied
to the stay.** R1 is about a room's four independent statuses; the same mistake
is available here — *"checked in, room not yet reported"* (R6, S11) is not a
lifecycle state, it is a complete lifecycle with an incomplete record.
Collapsing them produces a status vocabulary that grows one value per kind of
missing field, and the discarded axis cannot be recovered.

**The shape is the wire's — GUEST-Q9 (M7), adopting `Absence`
(`dto.proto:325`):**

```text
absence   field       "assignment" · "guest.phone" · "terms"
          reason      not_supplied | not_available_from_source | unreadable
          raw_value   what arrived, when the reason is unreadable
```

The closed set this design had — `no_assignment`, `party_unnamed`, … —
collapsed three different sentences into one flag. *The source sent nothing*,
*this integration cannot send it at all*, and *it arrived and we could not read
it* differ in whether anyone should be alerted, whether a connector needs
fixing, and whether replay would help — which is R26's rejected-versus-
superseded distinction losing its neighbour. **And `raw_value` is the field
that lets a vocabulary grow deliberately**: an unrecognised status names itself
instead of being guessed at years later.

### 2.3 · `Assignment` — the room, over time

```text
assignment_id   UUIDv7
stay_id         → RoomStay
room_id         Master Data ref
assigned_at · assigned_by
released_at     null while current
reason          initial | move | upgrade | correction
```

**A separate table, because the move is a fact and not a value.** R8 requires
a room change to be distinguishable from an update to the stay; GUEST-Q2's
addendum makes the room an assignment; S14 needs *both* rooms at the moment of
the move. One `room_id` column on the stay would answer *"where are they"* and
destroy *"where were they"* — and Room Care flips two axes on a move, so it
needs the pair.

`RoomStay.current_room_id` is the projection of the open row, resolved by the
service. The create and move messages carry no such field.

**An upgrade is an assignment — GUEST-Q8 (b).** Putting a guest booked into a
Deluxe King in an Executive Suite is a **higher-type room with the terms
unchanged**, so it is `stay.room_changed` like any other assignment (R8), with
`reason = upgrade` recording why.

```text
assignment   the ROOM changed · booked type and terms stand
             → stay.room_changed
amendment    the BOOKED TYPE or the TERMS themselves changed
             → stay.amended
```

**The line is what changed, not what the guest got.** A free upgrade at the
desk leaves the sale exactly as booked — the rate, the group's expected types
and every later availability calculation still read the booked type, which is
why the anchor must not move. A guest who *buys* the suite has amended their
booking, and then the anchor does move. Treating the free upgrade as an
amendment would silently rewrite what was sold.

### 2.4 · `StayGuest` — the party

```text
stay_id · guest_id · is_primary bool?  · added_at · source
```

`is_primary` is **nullable on purpose**: R11 records that the source produces
reservations where *no* guest is marked primary, and the reference hard-failed
on exactly that. *"Nobody is marked primary"* is a state, not an absence of
data, and `false` everywhere says something different from `null` everywhere.

An empty party is valid — *"not yet named"* (GUEST-Q2, S2).

### 2.5 · `GuestIdentity` and `ContactPoint`

```text
GuestIdentity
  guest_id      UUIDv7
  property_id
  name_given · name_family · name_as_given   R11 — a name is not one field
  preferences   person-scoped, durable across stays        S19
  origin        staff | pms
  created_at

ContactPoint
  contact_id · guest_id
  kind          phone | email
  value_ct      ciphertext                                 PII
  value_index   HMAC of the normalised form — indexed, exact match only
  tech_type · use_type   R11 — a phone is a TYPED choice among several
  is_primary    bool?    — same nullable reasoning as the party
  source        staff | pms
```

**Masked by default, revealed by a click, and the reveal is recorded** —
GUEST-Q7, ruled rather than asked, as standard practice for stored contact
PII. A contact renders as `+91 98470 •••• 12`; a staff member holding the
**stay's write permission** reveals it in one action; and the reveal is
written to the platform's audit — who, which guest, when. No new permission
and no bespoke table: the permission that lets a person act on the stay is the
one that lets them ring the guest, and the audit model is Chapter 26's.

**Recording the reveal is the part that does the work.** Masking alone is a
speed bump — every receptionist needs real numbers all day, so a policy that
made reveal rare would be worked around within a week. What makes a thousand-row
harvest different from a late-arrival phone call is that one of them leaves a
trail.

**GuestOps now owns the phone blind index**, which ADR 0089 §CTX-Q2 left
waiting for *"the domain that owns the phone number"*. The mechanism is the
platform's, already designed (`docs/working/05-secrets.html` §"Searchable
encrypted PII"): `aes_gcm_encrypt(field_key, e164(phone))` beside
`hmac_sha256(index_key, e164(phone))`, exact match only, no prefix search, and
the field key versioned rather than destroyed on rotation. This is what makes
Context's *phone → guest* chain a single index seek instead of a table scan of
decryptions.

**No `person_id` is stored here.** G360-Q1 gives Guest360 the person-graph;
GuestOps holds `guest_id`, and Guest360 maps `guest_id → person_id` in its own
schema. That is precisely what lets a merge *re-point the person and rewrite
no stay* — and why a stay's link survives a merge it never hears about.

**Preferences live on the guest, notes live on the stay** (S19). The
distinction is not decorative: a preference should be true next time, a note
dies with the stay.

### 2.6 · `CommercialTerms` — GUEST-Q6's half of the line

```text
stay_id
rate_code · rate_name
amount            Money (§2.9)                              R19
guarantee_code · guarantee_description
on_hold · reserves_inventory · is_default   R18's flags
deposit_offset_from_booking     interval                    R18
cancel_offset_from_arrival      interval                    R18
cancel_drop_time                time of day                 R18
penalty_amount    Money · penalty_nights int · penalty_basis R18
```

**Offsets, never resolved deadlines.** R18: an offset survives the arrival
date changing and a resolved timestamp does not, and *"a cancellation deadline
that silently stops matching its reservation is a chargeable error"*. The
deadline is computed when it is displayed.

### 2.6b · The stay's commercial and source detail — GUEST-Q7

The owner widened C3 from *booking source* to **every significant field the
PMS sends on a reservation**. The study's §3.2 is the list the flat flavours
actually carry, and this is the **kept set** — named explicitly, because *a
field is kept by decision, never by accident of the payload*:

```text
source            direct · OTA · corporate · walk-in · the source's own code
travel_agent      as sent — a reference, not a profile (§C9)
market_code       the segment every hotel reports on
meal_plan         EP · CP · MAP · AP, or the source's own code
guest_counts      adults · children — R9's counts, separately
rate_code         with the terms, §2.6
```

**And what is not modelled is retained rather than discarded:**

```text
source_detail     the remaining significant fields of the inbound fact,
                  kept as given, with the integration that sent them
```

This is the R25 lesson turned around. The reference **dropped** a reservation
that had no phone and **fabricated** an email when a downstream field demanded
one — two ways of losing the truth. Retention is the third option: what we
have not yet modelled is kept as it arrived, so the day it earns a column the
history is there, and nobody is inventing it retrospectively.

**Two limits, so retention does not become a dumping ground.** `source_detail`
is **never read to drive behaviour** — a field that decides something gets
modelled first — and it is **not a second copy of the raw payload**, which is
the Hub's and stays there (ADR 0128 §5, store-before-process).

**No folio.** No charge, no payment, no settlement, no invoice, no
night-audit posting — GUEST-Q6 puts all of it in Finance's later round, and
records the accepted consequence: a standalone property cannot settle a guest
in v1.

### 2.7 · `StayRequest`, `StayNote`, `Registration` — the rest of the desk

```text
StayRequest
  request_id · stay_id
  text · logged_by · logged_at
  handed_off    bool — announced for another application to act on
                S18: not every request becomes work

StayNote
  note_id · stay_id · text · author · at        this stay only

Registration
  stay_id · grc_no                      the property's own series
  name_as_on_id · date_of_birth · nationality
  address_line · city · state · country · postcode
  id_type       the PROPERTY's configured list, seeded for its country —
                never a fixed enum in the product (§2.8)
  id_number · id_issuer · id_expiry
  arriving_from · proceeding_to · purpose_of_visit
  vehicle_number                        optional; many properties record it
  document_refs the platform's media/asset references, never blobs here
  signature_ref · signed_at · captured_by

Registration · the from-outside block   shown when the guest's nationality
                                        is not the property's HOME COUNTRY
  passport_number · passport_issue · passport_expiry · passport_place
  visa_type · visa_number · visa_issue · visa_expiry
  arrived_in_country_on · port_of_arrival
```

**The field list is the design's proposal and the property decides what is
required** — owner, 2026-08-31 (*"card we can go with your idea"*), with the
required set configurable **separately for domestic and foreign guests**
(§2.8). A field a property does not use is not deleted from the model; it is
simply not required, because a card is a legal-ish record whose shape differs
by property and whose history must stay readable.

```text
StayReporting                            S19b — the filing obligation
  stay_id · required_by       a date, from the property's policy
  state       needed | filed | not_required
  filed_at · filed_by · authority · reference
```

**Three rules the design commits to here, and the third is the important one:**

* **The obligation is a property policy** (§2.8), never a hardcoded law. A
  property that takes no foreign guests, or is not in a jurisdiction that asks,
  configures it off and no screen mentions it.
* **The record is of a filing, not a submission.** v1 records that a person
  filed, when, with which authority and under what reference — which is
  genuinely useful even when the filing is done by hand on a government portal.
* **HotelOS does not submit it.** Sending data to an external authority is an
  **integration**, and the constitution routes every integration through the
  Integration Hub as a connector — *"no hardcoded integrations"*. An automatic
  filing is therefore a connector with its own owner, credential and round,
  and it is a **distinct capability class** rather than one more push beside
  room status — *a legal assertion, no silent retry, and the receipt is part
  of the record* (recorded on `CONN-Q5`'s register row, 2026-08-31;
  `03-the-open-questions.md` §B6).

**`reference` is the receipt, and that is why this record exists ahead of the
connector.** A filing is a legal assertion, so what the authority gives back is
part of the record and not a log line — the row is the property's evidence that
it complied. The shape therefore does not change when submission is automated:
a person files and records the receipt today; the connector files and records
the same receipt later, on the same row.

**And the flag never gates anything.** A stay with an outstanding filing checks
in, is served and checks out — S19b, applying S9's ruling to our *own*
obligation rather than a neighbour's capability.

### 2.8 · The application's own configuration

An application is a bundle — *UI + backend + schema + migrations + permissions
+ events + **configuration** + lifecycle* (ADR 0051). This is GuestOps's, and
it is not Master Data's: none of it describes what a property *is*.

```text
registration    home_country   ← decides who counts as "from outside"
                the required-field set, TWICE: home-country guests and
                guests from outside, set separately
                accepted id types — the property's list, seeded for its
                country, never a fixed enum in the product
                signature required · print on check-in
                the grc_no series: prefix, reset rule, next number
reporting       required? · who it applies to (from outside | every guest)
                the authority's name · the deadline, as an offset from arrival
```

**Nothing here names a country, and that is a hard rule.** This application is
sold into India and the GCC and will be sold further; a hotel in Kochi and a
hotel in Dubai must run the same build, each treating the other's nationals as
guests from outside. So *"foreign"* is never a fixed meaning in the product —
it is **nationality ≠ `home_country`**, and every list that would otherwise
encode one country's practice (accepted ID types, the required sets, the
authority, the deadline) is the property's to set.

The deadline is an **offset**, for R18's reason: *"within 24 hours of arrival"*
survives the arrival moving, and a stored date does not.

**A request is GuestOps's, and the work is not.** *"What needs doing"* is
Jobs' domain (APPS-Q1), so raising a job from the stay page **records the
request and announces it** — Jobs creates the job, assigns it and owns its
status. GuestOps stores no job id, no assignee and no job state; the boundary
is the constitution's, not a preference.

`Registration`'s field list is the design's proposal and **the property
decides which of them are required** — twice over, for home-country guests and
for guests from outside (§2.8). What a jurisdiction demands differs by country
and by property, so the product proposes a shape and never a legal minimum.

### 2.9 · Two value objects the whole schema depends on

```text
Money      amount_minor  int64      never a float, never a string   R19
           currency      char(3)
           tax_basis     gross | net | unknown

Timestamp  at            timestamptz
           basis         observed | source_expected | derived_from_clock
```

**`Money` carries three things or it is not an amount** (R19). The reference
wrote a `float` beside a `currency` that was always null, and later wrote
Oracle's *before-tax* figure and Apaleo's *gross* figure into one column — so
its stored revenue means a different thing per connector and nothing records
which. `tax_basis` is the third field that makes the number recoverable, and
`unknown` is a legitimate value: the source sometimes does not say, and
guessing is R25's fabrication.

**`Timestamp.basis` is R12–R14 in one field.** A date the source gave, plus
the property's configured check-in clock, produces a *derived* time; a
check-in performed at the desk produces an *observed* one; a PMS in-house stay
carries its *expected* arrival as the only time available. An arrival-time
report that cannot tell them apart measures the reservation rather than the
guest (R13, S22).

**The two source-date columns are dropped — GUEST-Q9 (M6), and this is the
specification DD asked for.** This design had stored `arrival_date` beside
`arrival_at`: two columns free to disagree, with no rule saying which wins —
the defect it warns about everywhere else. **GuestOps needs no separate date
field on the wire**, on one condition:

> **A `TIME_BASIS_DERIVED` timestamp must be constructed in the property's
> IANA zone, so that its own date component *is* the date the source gave.**

That is what makes the date exactly recoverable rather than approximately
recoverable, and R16 already requires the zone — *an offset cannot express
daylight saving, so a stored offset is wrong for half the year*. Built in UTC
or from an offset, a derived timestamp near midnight carries the **wrong date**
and R12's whole distinction is lost silently, in the way that looks like
correct data. `arrival_date` is therefore a projection of `arrival_at`,
computed and never stored.

---

## 3 · The stay state machine, and R7's one rule

### 3.1 · The states

```text
  Waitlisted ┐
  Pending    ┘──► Booked ──────► InHouse ────────► Departed
      │             │              ▲                   │
      │             └──► NoShow    └───────────────────┘
      └──────────────┴──► Cancelled    staff correction only (S24)
```

**Waitlisted and Pending are pre-confirmation states — GUEST-Q9 (M1).** They
are `StayLifecycle`'s values 7 and 8 (`dto.proto:186-187`), they are real
on-site Oracle statuses (R5's vocabulary), and **waitlist is a first-class
reservation state in every major PMS: the desk must see a waitlisted booking
as waitlisted.** Both outcomes this design had been unable to avoid are
refused — mapping one to `Booked` shows a confirmed booking that is not one,
and rejecting it loses a real record (R25's first failure).

Lifecycle rank, which is the only ordering the rule needs:

```text
Waitlisted 0 · Pending 0   <   Booked 1   <   InHouse 2   <   Departed 3
Cancelled and NoShow are terminal exits from any pre-arrival state
```

**Two states share rank 0, and the rule needs one added sentence.** A fact of
*equal* rank and a *different* state is applied — a waitlist clearing to
pending is a lateral move inside pre-confirmation, and it is real. A fact of
equal rank and the *same* state with the same values is the idempotent case
(§3.2). Nothing else changes: a check-in arriving for a stay we hold as
`Waitlisted` is rank 2 over rank 0, so it applies, and **the intermediate
`Booked` is never invented** — the confirmation happened somewhere we did not
see, and R25 forbids fabricating it.

**`DUE_OUT` is deliberately not a state.** `dto.proto:184` carries it and this
model composes it — `InHouse` plus a `departure_date` of today — because
`CONN-Q11` ruled exactly this one level up: *a room-level duplicate of
`StayLifecycle` would be two vocabularies for one axis*. Adding a sixth
lifecycle value for a fact two existing fields already state would be that
mistake, one level down. **Upheld by GUEST-Q9.**

`Cancelled` and `NoShow` are **business facts about the stay**, not ADR 0062
lifecycle states: `active` / `deleted_at` say whether a record exists, and a
cancelled reservation exists (S25, S27). The verbs are `RecordNoShow` and
`Cancel`, in ADR 0062's own idiom.

### 3.2 · R7's one rule, written once

> **An inbound fact is applied only if its lifecycle rank is greater than or
> equal to the rank the stay already holds. A fact of lower rank is not
> applied: it is recorded as a contradiction on the stay. An antecedent that
> never arrived is recorded as *not observed* and is never synthesised.**

That single paragraph answers all of R7's family, and the reference's three
simultaneous mechanisms — `forceCheckIn`, `directCheckout`, and a commented-out
replay that would have injected a check-in that never happened — reduce to it:

| The situation | What the rule does |
|---|---|
| `checked_out` for a stay we have never seen (S12) | create the stay directly in `Departed`; `arrival_at` is absent and `completeness` carries `no_arrival_time`. **Nothing is invented** (R25) |
| `checked_in` arriving after `Departed` | rank 1 < 2 → not applied to the lifecycle; it *fills* `arrival_at` if that is unknown, and is recorded |
| `cancelled` for an in-house stay (S26) | rank below `InHouse` → **a recorded contradiction**, and the guest stays served |
| the same fact twice (replay, §8) | equal rank, same values → idempotent; nothing changes and nothing is published |
| **a waitlist clearing to pending** | equal rank, **different state** → applied. A lateral move inside pre-confirmation is real, and rank cannot order two states that neither precedes (GUEST-Q9) |
| **a check-in for a stay we hold as `Waitlisted`** | rank 2 over rank 0 → applied. `Booked` is **not** inserted: the confirmation happened where we could not see it, and inventing it is R25's fabrication |

**The one deliberate exception, named:** a **staff correction** may move the
lifecycle backwards (S24 — checked out in error at 07:00 and still asleep in
the room). It takes the stay's write permission, is recorded as a correction
with who and when, and publishes the correcting fact. Inbound facts are
monotonic; people are not, and pretending otherwise would make the erroneous
check-out permanent.

**A contradiction is cleared by the same mechanism as a disagreement**
(§4) — the stay's write permission, two named values, both kept. That is
deliberate reuse: two clearing mechanisms would drift, and GUEST-Q3 already
settled what clearing means.

---

### 3.3 · The day roll — GUEST-Q7

The business day rolls at the property's boundary, and **nothing in the
platform rolls with it**: ADR 0128 §6 leaves the night-audit transition
without an owner. In a PMS-connected property that costs nothing — the PMS
runs its audit and the no-show arrives as a fact. In a standalone property a
stay that never arrived would sit in `Booked` for ever, the arrivals list
would keep yesterday's guests, and *no-show* would be a number nobody records.

So GuestOps runs a property-local day roll, and what it does is deliberately
small:

```text
at the boundary    yesterday's business day closes
it FLAGS           stays that were due in and never arrived
it MARKS NOTHING   no-show is a business fact a person records
                   (S27, ADR 0062's RecordNoShow)
```

**Flag, never mark**, for the reason APPS-Q1 gives about cleaning: a
consequence is a policy, not an automatic act. A guest who arrived at 23:50
and was checked in at 00:10 is not a no-show, and only a person at the desk
knows that.

**Where this belongs is a platform question, not a GuestOps one.** If a Night
Audit owner is ever named (ADR 0128 §6 anticipates one), the roll moves there
and this becomes a consumer. The design puts it in `Background/` and says so,
rather than claiming the night audit.

---

## 4 · Override, disagreement, confirmation — the PMS-connected mode

### 4.1 · The record

```text
StayDisagreement
  disagreement_id · stay_id
  aspect        lifecycle | assignment | dates          — a small closed set
  our_value     what the stay holds
  pms_value     what the inbound fact said
  raised_at
  override_actor · override_at     who wrote ours, and when   GUEST-Q1
  state         standing | confirmed | cleared_ours | cleared_pms
  cleared_by · cleared_at · cleared_side
```

`aspect` is closed and small on purpose: an open vocabulary here becomes a
per-field diff engine, and the desk cannot act on *"eleven fields differ"*.

### 4.2 · The three rules that follow, all GUEST-Q3 and GUEST-Q4

> **1 · One truth leaves the application.** While a disagreement stands, the
> **standing override** is the answer — on the board, to every consumer, and
> through Context. The disagreement is a **flag on that one answer**, never a
> second answer.

Room Care is never told the guest is in two rooms; Context resolves
*guest → reservation → room* to the override's room. The reasoning, recorded
with the ruling: a recorded override is a person looking at the guest, the
inbound fact is automation and possibly stale — and if the PMS silently won,
GUEST-Q1's *"staff can override"* would be a suggestion.

> **2 · A matching fact confirms silently.** An inbound fact whose value
> equals the standing override settles it as `confirmed`, recorded and
> surfacing nothing. **Only differing values are a disagreement** (GUEST-Q4).

This is what keeps the mechanism from being decorative. S36's feared twenty
reconciliations become the two that are real, because agreement arriving late
is not work.

> **3 · Clearing is the stay's write permission**, choosing *keep ours* or
> *take the PMS's*, recorded with who, when and which side — and **both values
> kept in history**. Clearing to the PMS's side emits the same correction fact
> a room move does, so Room Care re-plans from the event stream as always.

No `disagreement.*` event is published. A disagreement is a fact about our
records, not about the hotel; the **correction** is the business fact, and
ADR 0016 Part 2 is explicit that we publish the fact and not the process.

### 4.3 · The mode is not an authorization boundary

GUEST-Q4 removed the second mode, and the consequence for this design is worth
stating because it removes a whole class of code: **there is no mode flag that
changes who may write.** The same permissions apply in both modes. What
changes is only what a write *means* — in a property with an active PMS
connector, a lifecycle write on a PMS-known stay is recorded as an override.

The outage is a **per-capability staleness signal** (R27), rendered as a
banner and nothing else: *"PMS feed silent since 09:00 — your entries stand."*
It gates no operation. A switch keyed on a signal R27 proved unreliable would
flip the desk's meaning mid-shift on a false trigger.

### 4.4 · The PMS-unknown stay and its candidate link — GUEST-Q5

```text
StayLinkCandidate
  candidate_id · local_stay_id
  inbound_fact_id      the held fact, not a second stay — see below
  rank_score           name similarity MAY RANK
  state                proposed | confirmed | rejected
  decided_by · decided_at
```

```text
candidate test    same room + OVERLAPPING DATES
name similarity   may rank · may NEVER link
confirmed         one stay · the PMS identifiers mapped on (§7)
rejected          two stays, honestly — a double-booked room is then
                  the truth, not an artefact
undecided         sits on Attention, like any disagreement
```

**An inbound fact that raises a candidate is held, not applied**, and no
second stay is created until the candidate is decided. *(Implementation
choice, carrying no authority beyond the ruling it serves.)* The alternative —
create the PMS's stay, publish it, then merge — announces a stay to every
consumer that we intend to withdraw, and there is no honest merge event for
that. Holding keeps the ruling's two outcomes clean: confirm applies the held
fact to the local stay, reject creates the second stay and applies it there.
Further facts for that PMS stay queue behind the candidate and apply wherever
the decision sends them.

**Why the link is never automatic.** The reference resolved this by
correlating on `(companyId, siteId, surname, firstName, arrivalDate)` — entity
resolution by name, inside the connector, against its private copy (the Oracle
connector design §3.1). A wrong match silently merges two guests' stays, which
is worse than a duplicate — the same reasoning G360-Q1 gives for guest merges.

---

## 5 · Availability — an answer GuestOps computes, never a table someone feeds

**GUEST-Q7 (owner, 2026-08-31): both modes are fully v1, so availability is
v1 work** — and the ruling's shape is the design constraint, not just its
scope:

> **Room-type availability is computed from GuestOps's own stays plus the
> facts it already hears. The room-level conflict check applies in both modes.
> Stop-sell is a GuestOps setting per room type and date range — the seller's
> control, not inventory ownership.**

That sentence is what keeps this from becoming a second Master Data. GuestOps
does not *own* inventory; it **answers a question** from things it already has.

### 5.1 · The three inputs, and where each comes from

```text
the rooms of a type        Master Data — canonical, read, never copied
the stays on those dates   OURS. Booking · RoomStay · Assignment
rooms out of order         EngineeringOps, CONSUMED BY EVENT into a local
                           read model — never authoritative, never queried
                           from their schema, and stale-tolerant by design
stop-sell                  OURS — §5.3
```

**The out-of-order projection is the one to read carefully.** It is a
projection built from events this application already receives, not a copy of
another application's table, and **EngineeringOps remains the owner**: if the
projection is behind, availability is conservative for a few seconds and no
number anywhere becomes wrong. That distinction — *event-derived read model*
versus *duplicated master data* — is the line the constitution's
no-duplicated-master-data rule actually draws, and it is why this needs no new
inventory owner.

### 5.2 · Two questions, two answers, two costs

```text
"is room 214 free on these dates?"        THE CONFLICT CHECK
  → an overlap query over Assignment. Cheap, exact, and it runs in
    BOTH modes on every assignment and every move

"how many Deluxe Kings are sellable on 3 Sep?"   ROOM-TYPE AVAILABILITY
  → rooms of the type
      − stays holding that type on the date
      − rooms out of order (the projection)
      − stop-sell
```

### 5.2a · Which lifecycle states hold inventory — a consequence of GUEST-Q9

*"Stays holding that type"* was unambiguous with five states and is not with
seven, so it is written down rather than left to whoever writes the query:

```text
holds a room      Booked · InHouse · Pending
holds nothing     Waitlisted · Cancelled · NoShow · Departed
```

**`Waitlisted` holds nothing, and that is the whole point of a waitlist** — the
hotel is full, and the booking is a queue position rather than a room. Counting
one against inventory would make a full hotel look oversold and hide the free
room that comes back on a cancellation.

**`Pending` holds one, on the conservative reading**, because under-selling by
one room is recoverable at the desk and over-selling is not — the same
principle §5.1 states for a lagging projection. The source can settle it
properly: R18's guarantee carries a **`reserveInventory`** flag, which is
precisely *"does this booking hold a room"* asked by the system that knows.
That flag is part of M2's commercial-terms block, so **when DD's fact grows,
`Pending` reads the flag instead of the default** — recorded here so the
refinement is not re-derived.

**The conflict check warns; it does not forbid.** GUEST-Q5 ruled that a
double-booked room can be *the truth* — when staff answer *"two different
stays"* to a candidate link, the second stay is real and the room is genuinely
double-booked. A hard block would make a ruled outcome unreachable. So the
check names the other stay and lets a person decide, which is the same shape
as every other decision in this design.

**Availability is a number the desk sees, not a lock on a button.** In a
PMS-connected property Opera is still where the booking is made, and our
number is informational; in a standalone property it is the number the sale is
made on. Same computation, and no mode branch — GUEST-Q4's rule holds here too.

### 5.3 · `StopSell` — the seller's control

```text
stop_sell_id · property_id
room_type_id      Master Data ref
from_date · to_date
reason            free text — "renovation", "block for the wedding party"
set_by · set_at
```

Stop-sell is **not** an inventory fact and does not say a room is unusable —
EngineeringOps says that, through out-of-order. It says *we choose not to sell
this type on these dates*, which is a commercial decision belonging to whoever
runs the book. It subtracts from the computed answer and from nothing else.

### 5.4 · What this deliberately is not

* **Not a rates or yield engine.** No pricing by occupancy, no minimum stay,
  no closed-to-arrival. Those are revenue-management concepts and they need an
  owner this platform has not named.
* **Not an allocation or allotment model** — blocks held for a travel agent,
  release-back rules. Named here so a later reader knows it was considered.
* **Not a booking engine.** Nothing here faces a guest or a channel.
* **Not authoritative over EngineeringOps or Master Data.** Both are read;
  neither is copied.

---

## 6 · The events

Business facts, never process events (ADR 0016 Part 2). Names are proposed
here and are **subject to the register's event-subject stability rule** — a
name changes when the capability changes, never because an implementation
moved.

```text
reservation.created        the group exists: booking_id, its source
                           identifiers, expected count, is_complete, origin
reservation.expectation_changed   R9 — "three expected" became "four"

stay.created               booked · room type · dates · group · party
stay.amended               dates, party, terms — NEVER the room (R8, S28)
stay.assigned              the first room assignment                    S8
stay.room_changed          the assignment moved — the ruled name, brief §2.4
stay.arrived               checked in, with the arrival Timestamp + basis
stay.departed              checked out
stay.cancelled
stay.no_show
stay.corrected             a staff correction that moved the lifecycle
                           backwards, or cleared a disagreement to the
                           PMS's side (§3.2, §4.2)
stay.request_raised        the desk handed a guest request to whoever does
                           the work — GUEST-Q11, ruled 2026-09-01. Carries a
                           correlation id; the reply is EVT-Q3's correlated
                           event, never a call back                       S18

guest.created              a guest identity record now exists
guest.updated              name or contact points changed
```

**Consumers, from the scenarios:**

| Fact | Who acts on it |
|---|---|
| `stay.arrived` | Room Care (the room is occupied) · EngineeringOps (work here now disturbs a guest) · Guest360 (history) |
| `stay.room_changed` | Room Care (**both** rooms' axes flip) · Jobs/EngineeringOps (open work on either room) · Guest360 · GuestOps's own registration (brief §2.4) |
| `stay.departed` | Room Care — which **decides for itself** whether that becomes a task; cleaning is policy-driven and a checked-out room becoming a task is a hotel policy, never an automatic consequence (APPS-Q1, S21) |
| `stay.cancelled` · `stay.no_show` | the room returns to inventory; Guest360; analytics |
| `guest.*` | Guest360 — the person-graph is built over these, and rewrites none of them |
| `stay.request_raised` | Jobs — which **decides for itself** whether the request becomes a job, creates it, assigns it and owns its status. It replies with a fact carrying the same correlation id plus the job's identifier, and GuestOps stores that on consumption. An uninstalled Jobs means *"no job yet"*, never a hang (APPS-Q2) |

**Two things GuestOps does not publish**, and each removal is deliberate:
no `disagreement.*` (§4.2), and **no group-level cancellation** — GUEST-Q2
rules that every operation happens to a stay, so cancelling a booking is *n*
`stay.cancelled` facts and a consumer never has to expand a group.

**`events.append(tx, event)` is a local write in the caller's transaction**
(CLAUDE.md, and it is not optional here): the stay change and its announcement
commit together, or a crash between them keeps the check-in and loses the
event.

---

## 7 · Identifiers — minting *is* the mapping

Every identifier GuestOps mints is a **UUIDv7 of ours**. Opera's confirmation
number is never a key.

R10 is the complication: an OHIP reservation carries `reservationIdList[]`,
each entry a `{id, type}` pair — *there is not "the reservation id"*. So the
stay carries typed external references:

```text
StayExternalRef
  stay_id · integration_id · identifier_kind · external_id
```

**`CONN-Q8` was ruled on 2026-08-31 and its v1 restriction is withdrawn** —
the mapping key now carries the identifier kind, so the Hub no longer maps
only one kind per entity type. *(Corrected 2026-08-31 on Stream DD's sweep,
which found this paragraph one day stale.)*

**Nothing here changed as a result**, and that is the point worth keeping:
`identifier_kind` was modelled while the restriction was still in force, so
that the ruling would cost no remodelling. A design that had encoded the
restriction — one external reference per stay, with the kind implied — would
be migrating today.

### 7.1 · The gap dissolves — GUEST-Q8

The question this page asked was *"the Hub has nothing to map to before
GuestOps mints the canonical id — so whose table is this?"* **Ruled, planner,
2026-08-31: minting is the mapping.**

```text
an inbound fact arrives, carrying external references
        │
        ├─ a reference we know  →  it names the stay. Apply the fact
        │
        └─ none of them known   →  CREATE the stay AND its
                                   StayExternalRef rows,
                                   IN ONE GuestOps TRANSACTION
```

There was never a moment needing a pre-existing canonical id, because the id
and its external references are **born together**. The problem was an
assumption — that mapping is a lookup performed *before* the entity exists —
and it dissolves rather than being bridged.

**And the split it fixes, platform-wide:**

| | |
|---|---|
| **master entities** | resolve through **ADR 0016's mapping** — Core's, and unchanged. A PMS room number → `masterdata.room_id` is exactly this |
| **operational entities** | their typed identifiers **ride on the fact** for the owning domain to record. **The Hub keeps no reservation-id table** |

That is why the Hub was never the right home: a mapping table exists to
resolve an id somebody else owns, and nobody owns a stay's id but us.

**One transaction, and it matters.** The stay and its references commit
together with the event — the same rule `events.append(tx, event)` follows.
A crash between minting the stay and recording what it came from would leave a
stay nothing could ever match again, and the next inbound fact would create a
duplicate.

**This is the second time the defensive identifier-kind bet paid.** `CONN-Q8`'s
ruling cost no remodelling, and this one is prose rather than a migration —
because the references were modelled as a typed set from the start instead of
as one column that assumed a single kind.

---

## 8 · What the Hub's deferred queue drains into

The Hub has been normalising reservation and guest facts since the connector
round and holding them **`deferred`**, with `business_date` and provenance,
because their owning domain did not exist (ADR 0128; connector brief §12–§14).
This application is that domain.

```text
Hub inbox (deferred)  ──replay, event order──►  GuestOps
                                                   │
                          ┌────────────────────────┤
                          ▼                        ▼
                 §3.2's one rule            §4's disagreement rules
```

Four properties this design commits to, each one already required by something:

* **GuestOps's first day is not an empty book.** The backlog arrives in event
  order; the state machine's rank rule (§3.2) makes an out-of-order history
  land correctly rather than needing the Hub to sort it.
* **Replay is idempotent by construction.** Consumers deduplicate on
  `event_id` and discard a stale `entity_version` (Chapter 21); §3.2's rule
  makes a re-applied fact a no-op that publishes nothing.
* **`business_date` arrives attached** — the Hub computes it from
  `operating_day(occurred_at, boundary)` and **GuestOps never computes it**
  (ADR 0128 §6). The desk's day, the arrivals list and the night auditor's
  view all read that value (S10, S20).
* **A staff-created stay has no `business_date` from the Hub**, so GuestOps
  asks the Context Service for `operating_day(now, boundary)` at the moment of
  creation. It still never computes the boundary itself.

**Which facts publish is Hub configuration, not connector code** — so
switching the reservation family from `deferred` to published is an operator
action the day this application installs, and the connector is untouched.

---

## 9 · What Context asks, and what GuestOps answers

ADR 0089 §CTX-Q1 fixes the shape: Context owns no tables, writes nothing, and
reads **stable domain-owned views** through EF Core keyless entities —
**each contributing domain owns its view's definition and compatibility.**
Three views, and they are a published contract of this application:

```text
v_guest_contact_index     guest_id · property_id · kind · value_index
                          the blind index — exact match, no plaintext
                          → phone → guest                              §2.5

v_stay_current            stay_id · property_id · booking_id
                          guest_id(s) · room_id · room_type_id
                          arrival · departure · lifecycle
                          → guest → reservation → room
                          → room → who is in it now

v_stay_room_reference     room_id · open_stay_count
                          → ADR 0062's deletion-reference check:
                            a room with an open stay is not deletable

v_stay_room_day           property_id · room_id · business_date
                          stay_id · relation
                          → arriving | staying_over | departing
                          → CONN-Q11's composition. Approved GUEST-Q9
```

**The fourth view exists because `CONN-Q11` named this domain as the source.**
The room fact deliberately cannot say whether an in-house entry is an arrival
or a stayover — *"arrival-ness is a stay-level truth the stay facts already
carry with their dates"* — so a consumer composes it from stays through
Context. `v_stay_current` answers *which stays are current* and cannot answer
*which stays touch **this room** on **this business date***, which is the
date-ranged question the composition actually asks. It is derived entirely from
stays this application already holds, and its shape is R2's: **several stays
touch one room on one day** — one departed this morning, one is staying over,
one arrives this afternoon.

Three rules that come with them:

* **The view is the contract; the tables behind it stay free to change.** That
  is CTX-Q1's whole point, and it is why the design can rework `Assignment`
  later without a Context release.
* **The blind index view exposes no plaintext.** Context resolves a phone by
  computing the same HMAC and seeking; it never receives a number it did not
  already have.
* **When GuestOps is not installed, Context degrades** — `sources` / `degraded`
  per ADR 0089, exactly as it does today. Installing this application is what
  turns the guest chain on, and nothing else changes in Context.

`vip_status` stays out (ADR 0089 §CTX-Q3, deferred): no authoritative
definition of VIP exists, and this round does not invent one.

### 9.1 · What GuestOps asks *of* Context — and what does not exist yet

The three views above are what this domain **answers**. The stay page also
**asks**, and this is the direction the design cannot satisfy today.

```text
the stay page needs                        from        exists?
────────────────────────────────────────────────────────────────
jobs raised from this stay                 Jobs        NO
  — the mockup's "Requests & jobs" tab
servicing across this stay, per night      Room Care   NO
  — the mockup's "Servicing" tab
the room's readiness before check-in       Room Care   NO
  — DISPLAY ONLY: S9 rules it is never a gate
```

**The rule is not in doubt; the resolver is.** *"Context over joins"* — an
application never reads another's tables, and a cross-domain relationship is
obtained through the Context Service. Both panels are therefore Context
questions, and Context v1 ships room, rooms, staff, asset and property
summary and nothing else (ADR 0089 §"The v1 scope this fixes"). There is no
*stay → jobs* and no *stay → servicing* resolver, and the contributing
domains — Jobs, Room Care — would each have to own a read view the way §9
has GuestOps own three.

**And none of them may gate an operation — ruled, owner, 2026-08-31:**

> **An application's own flow is never gated on another application being
> installed. An absent dependency loses its *capability*, never the *flow*.**

That rule reaches past this section and is the reason the design has exactly
one hard gate: **check-in refuses an unassigned stay** (S8), because the
assignment is GuestOps's own fact. Room readiness, open jobs and cleaning
state are *displayed* when their owner is present and the resolver exists, and
are simply absent otherwise. A cross-application gate would make an
*installable* application effectively mandatory.

Three consequences the design accepts rather than works around:

* **Nothing is stored here.** GuestOps never keeps a job id, a job status, a
  cleaning record or a room's readiness. When the resolver arrives, the panels
  light up; until then they are absent, and no data has to be migrated.
* **Absence is normal, not degraded.** Jobs and Room Care are *installable*.
  A property without Jobs still has a guest complaining about the air
  conditioning, so the **request** is GuestOps's own record always, and only
  the raising and the status are conditional (the mockup draws both states).
* **This is reported, not designed around** — §15, finding 4. Inventing a
  direct read into Jobs' schema to make a tab work would break the one rule
  the whole platform's modularity rests on.

**How the resolvers arrive — `CTX-Q4`, ruled 2026-08-31.** Context's surface
**grows routinely**; ADR 0089's v1 list was the set that satisfied its
principle on the day it was written, never a closed enumeration, and the
**principle is the gate**: a resolver exists for an entity with a real owner
and a read contract.

```text
one delivery, by the CONTRIBUTING DOMAIN's round
  the read view  ·  the Context RPC  ·  its stated constraint
```

So **Jobs' and Room Care's rounds inherit these three as scoped work**, and
each arrives whole rather than as a view that waits for somebody to notice it
needs an RPC. **The constraint that travels with all three is A1's:
display-only** — recorded in the register rather than only here, so a
readiness resolver can never be built as a gate by an author who never read
this page.

---

## 10 · Permissions

ADR 0007 naming — one per capability, the verb naming what it lets a person
do:

```text
reservation.read        the four lists, the stay page, the group page
stay.create             create a booking and its stays — standalone, and
                        the PMS-unknown stay in a connected property (GUEST-Q5)
stay.write              the lifecycle: check in · check out · cancel ·
                        no-show · correct. THE SAME PERMISSION makes an
                        override, clears a disagreement and resolves a
                        contradiction (GUEST-Q3 · §3.2)
stay.assign             assign a room · move a room
guest.write             identity records, contact points, preferences
registration.capture    the registration card, the documents, the signature
reporting.file          recording that a guest filing was made (S19b) —
                        separate from capture, because it is an assertion
                        about an external obligation, not about our record
guestops.configure      the application's own settings (§2.8): the required
                        fields, the grc series, the reporting policy
request.manage          guest requests and their hand-off to Jobs
```

**There is no `disagreement.clear`.** GUEST-Q3 ruled that clearing belongs to
the stay's write permission — author-only fails across shifts, supervisor-only
escalates a routine reconciliation — and a separate permission would
re-introduce the escalation the ruling refused.

**Application access is per user and gates the launch** (ADR 0116 §5): a
receptionist without GuestOps never sees the tile. GuestOps is *installable*,
so the §5 authority-unavailable exception does **not** cover it — an
unreachable authorization store withholds this application, by design.

---

## 11 · The application bundle

Per the apps repository charter and CLAUDE.md's .NET template. **Nothing in
this tree is created in this phase** — the brief gates code, and this is the
proposal it will be built against.

```text
guestops/
  docs/            chapters · decisions · mockups          ← exists today
  backend/
    src/
      Domain/          Booking · RoomStay · Assignment · GuestIdentity
                       CommercialTerms · Money · Timestamp · the state machine
      Application/     the operations: book · assign · move · check in ·
                       check out · cancel · no-show · correct · clear
      Infrastructure/  EF Core, the reservations schema, the read views
      Grpc/            the service surface — one file per subject (ADR 0042)
      Events/          publishing the facts; consuming the Hub's
      Background/      the arrivals/departures day roll, staleness watch
    tests/             characterisation — ADR 0054
  frontend/        the desktop module: the four lists, stay, group, guest
  schemas/  migrations/  tests/  assets/  manifest.yaml
```

**The five standards bind before a file exists** (0042/0038/0036/0037/0027):
the composition root holds no subject, one file per subject, 300 lines hard,
and the module layout is verified against the real tree before any file is
created.

**Tests:** characterisation in the service's own project; **one test per
boundary class** in the platform's E2E suite (ADR 0054 — a rule goes in the
service suite, a connection goes in E2E); an absent database **fails** the run
(ADR 0053).

---

## 12 · Slices

| | Ships | Unblocks |
|---|---|---|
| **1 · The book** | Booking · RoomStay · Assignment · party · guest identity · the bookings list · the four day lists · the stay's overview and activity · standalone writes · cancel · the state machine · `stay.*` facts · the three views — **plus GUEST-Q7's four: availability and stop-sell (§5), the room-level conflict check, the day roll (§3.3), and the kept source set (§2.6b)** | a standalone property can run its front desk **and know what it has left to sell**; **Context's guest chain turns on** |
| **2 · The PMS mode** | the deferred queue drains · overrides · disagreements · silent confirmation · candidate links · the staleness banner | the Hub's backlog stops accumulating; Oracle properties go live |
| **3 · The rest of the desk** | commercial terms · registration · requests · notes · preferences · the group page's full behaviour | *"every other guest operation"* (GUEST-Q1) is complete |
| **4 · The neighbours** | the *stay → jobs*, *stay → servicing* and *stay → readiness* panels | **not ours to schedule — `CTX-Q4`, 2026-08-31.** Each resolver is one delivery by its contributing domain's round (view + RPC + constraint); **Jobs' and Room Care's rounds inherit the three as scoped work.** This slice is the panels lighting up, and needs no code here beyond rendering |

**Slice 1 grew on 2026-08-31 and it grew deliberately.** GUEST-Q7 put both
modes fully in v1, which makes availability slice-1 work rather than a later
convenience — a standalone property that cannot say what is free is not a
property that can open. The four items are additive to the schema and none of
them reopens a ruled decision.

**The stay's activity view is slice 1, and it is assembled rather than
stored**: GuestOps's own rows come from its event stream, and another
application's rows are resolved live. That is why slice 4 can arrive later
without the page being rebuilt — an absent resolver contributes no rows, and
nothing is orphaned when an application is uninstalled.

Slice 1 is strictly first, and the reason is not sequencing convenience:
**slice 2's disagreement rules are meaningless without a book to disagree
with**, and slice 1's views are what every other application reaches this
domain through.

---

## 13 · What this design deliberately does not do

* **No folio** — GUEST-Q6. No charge, payment, settlement, invoice or
  night-audit posting.
* **No merge logic** — G360-Q1. No `person_id`, no candidate person, no
  automatic linking of guests.
* **No write-back to the PMS** — `CONN-Q5`, ADR 0128 §4. Nothing this
  application records reaches the PMS in v1, and §4 exists because that is
  true.
* **No second mode** — GUEST-Q4.
* **No room state.** Room Care owns cleanliness and readiness; GuestOps
  announces occupancy and asserts nothing about cleaning (S21).
* **No cross-installation query** for a multi-property group — GUEST-Q2.
* **No `vip_status`** — ADR 0089 §CTX-Q3.

---

## 14 · The gates — named, not worked around

| | State | What it blocks |
|---|---|---|
| **The application-caller authorization round** | **Open** — APPS-Q1: no .NET application principal exists | GuestOps's service calling the Kernel and Context as itself. **The whole backend**, and the brief gates code behind it |
| **The registry-driven shell** | **Open** — APPS-Q1: `PLATFORM_APPS` is hardcoded | the desktop module appearing without editing the shell |
| **ADR 0061 authorization materialisation** | **Ruled, unbuilt** — nothing writes tuples today | any stay-level authorization object. v1 needs none (property + application scope), so this is named rather than depended on |
| **`CONN-Q8` — the identifier kind** | **Ruled 2026-08-31**; the v1 restriction is withdrawn | nothing — §7's `id_kind` was modelled before the ruling, so it cost no migration |
| **A platform print surface** | **`SHELL-Q23` — raised 2026-08-31**, seconded by Workforce's rota sheet; two applications made it a platform question rather than one app's inconvenience | the registration card the guest signs (§2.7, gold mockup frame 15). The button stands, and the round's reason is quoted in the row: **a front desk that cannot produce the card the guest signs is not deployable** |
| **The reservation-identifier mapping's home** | **Ruled — GUEST-Q8, 2026-08-31: minting is the mapping** (§7.1) | nothing. The Hub keeps no reservation-id table |
| **§15 (e) — check-in into an unreleased room** | **Ruled — owner, 2026-08-31** | nothing, and **no configuration either**: GuestOps never refuses. An absent dependency loses its capability, never the flow; readiness is display-only when Room Care and the resolver are both present (§8.1) |
| **§15 (g) — the registration card's contents** | **Open** | nothing: carried as a **records list** — `grc_no`, documents, signature — whose statutory fields the owner fills in |
| **Finance** | **Not started** | settlement in a standalone property, knowingly (GUEST-Q6) |

---

## 15 · Findings — reported, not resolved

**1 · The constitution's event examples name the wrong aggregate.** CLAUDE.md
§"Event-first architecture" lists `reservation.checked_in` and
`reservation.checked_out` among its examples. Under GUEST-Q2 a *reservation is
a group* and checking in happens to a **room-stay**, so those two subjects
name an operation that cannot occur — a group does not check in (S23: there is
no such thing as checking out a group). This design publishes `stay.arrived`
and `stay.departed`. The list is illustrative and an example given in passing
is not a ruling, but the constitution should not carry a subject the model
forbids. **CLOSED — corrected in `CLAUDE.md`, 2026-08-31:** the examples now
read `stay.arrived` / `stay.departed`, with the reasoning kept as a dated note
so the correction cannot read as silent drift later. Raised rather than fixed
from here, because the constitution is the architect's file and this round has
no authority over it.

**2 · Where does the reservation ↔ PMS-identifier mapping live? — CLOSED,
GUEST-Q8, 2026-08-31: minting is the mapping** (§7.1). Asked once for both
designs on Stream DD's sweep, and answered once. The finding is kept rather
than deleted because *what was wrong with the question* is the reusable part:
it assumed mapping must be a lookup performed **before** the entity exists,
and for an operational entity that assumption is what created the gap. Master
entities resolve through ADR 0016 (Core's, unchanged); an operational entity's
typed identifiers ride the fact and are recorded by the domain that mints the
id, in the same transaction.

**3 · Chapter 26's `GuestContext` includes `vip_status`. CLOSED —
2026-08-31**: the chapter's head note now carries a fourth item marking it
**deferred, not decided**, with CTX-Q3's reasoning quoted.

**And VIP's definition is parked here deliberately.** ADR 0089 assigns it to
this domain, so it belongs to a **later GuestOps round** and is defined from
the owner's answer to *what makes a guest VIP at this hotel* — loyalty, spend,
a manager's flag, a corporate account. This design does not carry it, does not
propose it, and does not guess it: a `bool` whose meaning nobody has fixed is
the exact shape CTX-Q3 exists to prevent.

**4 · The stay page needs two Context resolvers that do not exist** — *stay →
jobs* and *stay → servicing* (§8.1). The owner asked for both panels and both
are right: a front desk is asked *"has anyone been in my room?"* and *"is
someone coming about the AC?"*, and the answers live in Room Care and Jobs.
The platform rule is settled — Context answers cross-domain questions, and an
application never reads another's tables — so what is missing is **two
contributing-domain read views and their Context RPCs**, which are those
applications' rounds and not this one's. Named here so the mockup's two
cross-application tabs are read as *drawn, not built*.

**CLOSED — `CTX-Q4`, 2026-08-31.** Context's surface grows routinely and the
**principle is the gate**, not v1's list; each resolver arrives as **one
delivery** — read view, RPC and stated constraint — from the contributing
domain's round, and Jobs' and Room Care's rounds inherit these three as scoped
work. **A1's display-only constraint is recorded in the register with them**,
so a readiness resolver cannot be built as a gate by an author who never read
this design (§9.1).

**5 · "Payment information" and GUEST-Q6 — closed, 2026-08-31.** The owner
asked for payment information on the stay; GUEST-Q6 had ruled the folio out of
v1 the same day, and the mockup's frame 7 drew the line rather than choosing a
side. **Ruled: GUEST-Q6 is not widened.** The stay page ships **band one
only** — the commercial terms — with band two dashed and, in a connected
property, an **"Open in Opera" link**: *a link is honest where a number would
be a promise.* The **connector balance-fetch is recorded as a candidate
capability for the connector's second round** (it is a read, but v1's inbound
contract excludes it), and the standalone folio stays Finance's.

**Findings still open are carried in `03-the-open-questions.md`**, together
with twelve scope items this round found while drawing — availability being
the largest, and named rather than assumed.

---

## 16 · What this page does not contain

* **No code, and no created tree.** §10 is a proposal; the brief gates the
  build.
* **No proto.** The contract language is `shared/protos`, and the protos are
  written with the code, not before the mockup is verified.
* **No answer to §15 (e) or (g)** of the scenario record, and no anticipation
  of one.
* **Nothing copied.** Every PMS fact is cited to `R<n>` in the requirements
  page beside `pms-oracle/`, which cites the read-only reference outside both
  repositories.

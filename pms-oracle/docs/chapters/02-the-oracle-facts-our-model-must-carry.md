# 02 · The Oracle facts our model must carry

**Status:** requirements, 2026-08-30. Stream DD, written from the class C
findings of the gap analysis — `docs/working/42b-gaps-against-our-design.md`
§4, **in the platform repository**.
**Source of fact:** the reference study, `01-the-oracle-pms-reference-study.md`,
beside this page.

**Why it lives here.** These are properties of the PMS. They are true whichever
way `CONN-Q3` is ruled — whether a PMS connector belongs to Chapter 10's MCP
Router, Chapter 21's Integration Hub, diagram 83's Integration Orchestrator, or
more than one of them. A platform question blocks the Hub's design; it does not
block writing down what the source actually does. The ten *"our design already
covers it"* findings stay in `42b`, because those are statements about the
platform.

---

## 0 · What a requirement on this page is, and is not

Each entry has three parts and always in this order:

```text
FACT          what the PMS does, from the study, with its citation
EVIDENCE      the line that proves it
REQUIREMENT   what the normalisation model must be able to express
```

**A requirement here says what must be expressible. It never says how.** No
field names, no types, no proto, no table, no enum. That is deliverable 3 and
it is blocked on `CONN-Q3`; a page that quietly named fields would be a design
wearing a requirements heading, and would pre-empt a ruling nobody has made.

The test each requirement had to pass: *if the model cannot express this, does
a real hotel operation become wrong or impossible?* Anything that failed that
test is in §9 as an observation, not here as a requirement.

Every requirement is traceable: **`R<n>`** carries the `42b` finding it comes
from and the study section that establishes it.

**Citations** to the reference are relative to the study's §0 root, which is
read-only and outside both repositories.

---

## 1 · Room state

### R1 · A room has four independent statuses and they must not be collapsed
*(42b G-C1 · study §5.2)*

**Fact.** OHIP's housekeeping view and the on-site push agree on four separate
axes: the front-office status (vacant / occupied), the housekeeping room status
(dirty / clean / inspected), the housekeeping department's own status, and a
**list** of reservation statuses.

**Evidence.** `cloud/models/HousekeepingRoomInfo.java:50-56` —
`reservationStatusList`, `frontOfficeStatus`, `housekeepingRoomStatus`,
`housekeepingStatus`; `web/dto/mongo/OracleWebRoomStatusInfo.java:21-25`.

**Requirement.** The model must carry occupancy and cleanliness as
**independent** values. A room that is vacant and dirty, occupied and clean, or
vacant and out of order must each be representable, and no normalisation step
may reduce the four to a single room status. This is the one modelling mistake
on this page that cannot be undone downstream: once collapsed, the discarded
axis cannot be recovered from the event.

### R2 · A room's reservation status is a list, because several stays touch one room on one day
*(42b G-C2 · study §5.1)*

**Fact.** The reservation-status axis arrives as a list per room —
`[NotReserved, NotReserved, Departed, StayOver, Arrived]` is a room somebody
left this morning, somebody is staying over in, and somebody arrives into
today.

**Evidence.** `cloud/models/HousekeepingRoomInfo.java:50`; the reference
reduced it to its last element at `cloud/services/OracleCloudBaseService.java:108`,
and a `main()` demonstrating exactly that input was left in the file at `:149-159`.
The on-site flavour delivers the same fact as a comma-separated string, of
which the first element was taken (`web/services/OracleWebBaseService.java:12-13`).

**Requirement.** The model must allow a room to carry **more than one stay
relationship at the same time**. A single-valued "who is in this room" cannot
express a departure and an arrival on one day, which is the ordinary condition
of a sold hotel.

### R3 · Two derived states are operationally real, and one needs a field only the PMS has
*(42b G-C5 · study §5.2)*

**Fact.** Two states drive actual housekeeping work and are computed from the
four axes plus one more field:

```text
VACANT_REFRESH   vacant + arrival expected + (clean | inspected)
                 a room that sat empty still needs freshening before arrival
STRIP_LINEN      occupied + due out + dirty + not blocked again today
                 strip it, because nobody arrives into it tonight
```

**Evidence.** `web/services/OracleWebBaseService.java:60-66`. `STRIP_LINEN`
depends on `nextBlockedAt` — **when the room is next sold** —
`web/services/OracleWebRoomServiceImpl.java:52-53`. The same rule ran as a
daily sweep over every vacant clean or inspected room at 05:55 in the
property's morning
(`modules/housekeeping/service/background/HousekeepingBackgroundService.java:50`,
`:91-103`).

**Requirement.** The model must carry **when the room is next sold**. It is a
forward-looking fact that exists only in the PMS's inventory, cannot be derived
from any current status, and without it a correct `STRIP_LINEN` decision is not
computable. Whether HotelOS derives these two states or receives them is a
design question; carrying their inputs is not.

### R4 · A room-shaped thing may not be a room
*(42b G-C6 · study §5.2)*

**Fact.** Room types carry a `pseudoRoom` flag. Pseudo rooms are PMS
bookkeeping constructs — house accounts, group masters — and are not physical
rooms.

**Evidence.** `cloud/models/HousekeepingRoomInfo.java:63`.

**Requirement.** The model must be able to mark a room-shaped record as **not a
physical room**, so that occupancy counts, housekeeping assignment and any
mapping to `masterdata.rooms` can exclude it. A pseudo room mapped to a
canonical room is a permanent data error, because the canonical room does not
exist.

---

## 2 · The stay, and its lifecycle

### R5 · The status vocabulary is per flavour, is case-unstable, and includes the empty string
*(42b G-C3 and G-C4 · study §5.1)*

**Fact.** Four vocabularies for one vendor:

```text
OHIP reservation    Reserved · InHouse · CheckedOut · Cancelled · NoShow
OHIP room-level     Reserved · Arrived · StayOver · Departed · NotReserved
OHIP housekeeping   Inspected · Clean · Vacant · Occupied · Dirty
                    OutOfOrder · OutOfService · ""
on-site reservation Due In / DUE IN / OT · Checked In / CHECKED IN
                    CHECKED OUT · CANCELLED · DUE OUT · PENDING · WAITLIST
on-site room codes  DI · CL · IP · OO · OS  |  VAC · OCC
```

**And the empty string is a value, not an absence.** A room whose OHIP
housekeeping status is `""` is a **pick-up** room — a real housekeeping state,
mapped as such.

**Evidence.** `cloud/services/OracleCloudBaseService.java:90-105`, `:107-124`,
and `:126-147` — where `case "": return "PICK_UP";` sits among the seven named
values; `onPremise/services/OracleOnPremiseReservationServiceImpl.java:65-66`,
`:86`, `:114`, `:131`, `:146`, `:185`, `:212-214`;
`web/services/OracleWebBaseService.java:32-58`.

**Requirement.** The mapping from source status to normalised state is a
property of the **flavour**, not of the vendor. The model must not assume one
vocabulary per PMS brand, and must not assume the source vocabulary is
case-stable. **And it must not treat an empty source value as "unknown" by
default** — here it carries a specific meaning, and a normaliser that maps
blank to null loses a state the housekeeping floor works from.

### R6 · Two casings of one status can be two different messages that must be joined
*(42b G-C3 · study §5.1)*

**Fact.** On the on-premise flavour, `"Checked In"` and `"CHECKED IN"` are not
the same message differently cased. They are two feeds carrying **different
fields** — one supplies phone, email and departure date, the other supplies the
room number — and a check-in is complete only when both have arrived.

**Evidence.** Separate branches at
`onPremise/services/OracleOnPremiseReservationServiceImpl.java:114-129` and
`:131-144`; the correlation that joins them, each casing searching for the
other, at `onPremise/dao/mongo/OracleOnPremiseReservationDaoImpl.java:50-74`;
the fields each contributes at `:123-126` and `:140-141`.

**Requirement.** The model must allow **one business fact to be assembled from
more than one source message**, and must therefore be able to represent a
partially-known stay that is not yet publishable. A pipeline that maps one
inbound message to one outbound event cannot express this source at all.

### R7 · Out-of-order arrival must be representable, with one rule
*(42b G-C15 · study §5.5)*

**Fact.** The first event seen for a stay can be its check-out. The reference
met this and answered it **three times** without removing any answer:
per-flavour `forceCheckIn` / `forceCheckout` flags, `directCheckIn` /
`directCheckout` fallbacks, and a commented-out replay that would have injected
a check-in before the check-out.

**Evidence.** The flags at `cloud/services/impl/OracleCloudReservationServiceImpl.java:126`,
`onPremise:101-102`, `web:193-194`; the fallbacks at `onPremise:368-383` and
`web:317-349`, the latter marked `//Todo remove after data flow is ok`
(`onPremise:258-259`); the commented replay at `cloud:105-109`.

**Requirement.** The model must define what a stay's state is when its
antecedent event was never received, and it must define it **once**. Three
mechanisms for one condition is how the reference arrived at a state where
nobody could say which path a given stay took.

### R8 · A room change is not a room update
*(42b G-C16 · study §5.5)*

**Fact.** A guest moving room mid-stay is a distinct business fact from a
correction to the same stay's details. The reference's downstream had four
verbs — check-in, check-out, **change**, update — and the branch distinguishing
change from update, on whether the room number differed, was commented out.

**Evidence.** The four verbs at
`common/services/InstioGuestEntryServiceImpl.java:86`, `:114`, `:142`, `:170`;
the distinguishing branch at `web/services/OracleWebReservationServiceImpl.java:225-231`.

**Requirement.** The model must distinguish a **room change** from an update to
a stay. Housekeeping and Maintenance react to the former and not to the latter;
folding them together publishes an event whose consumers cannot tell whether a
room was vacated.

### R9 · One source message may describe one room of a multi-room reservation
*(42b G-C14 · study §3.2)*

**Fact.** The on-site flavours carry `noOfRooms` — which may say three — and a
payload describing exactly one room. Both flavours carry a written comment that
this is a source limitation, not a modelling choice.

**Evidence.** `onPremise/services/OracleOnPremiseReservationServiceImpl.java:292-293`
and `web/services/OracleWebReservationServiceImpl.java:351-352`; the count
field at `onPremise/dto/mongo/OracleOnPremiseReservation.java:37`. The
reference's response was to mint a per-room identifier by string concatenation
that always produced `-1` (`cloud:211`, `onPremise:308`, `web:367`).

**Requirement.** The model must express a reservation of *n* rooms whose rooms
arrive separately, and must be able to say that a stay's room set is
**incomplete**. It must not require a per-room identifier the source does not
provide.

---

## 3 · Identity

### R10 · A reservation and a guest each carry several typed identifiers
*(42b G-C13 · study §3.2, §5.4 — refines ADR 0016)*

**Fact.** An OHIP reservation carries `reservationIdList[]`, each entry a
`{id, type}` pair; a guest profile carries `profileIdList[]` in the same shape.
There is not "the reservation id" — there are several, distinguished by type.

**Evidence.** `cloud/dto/mongo/Reservation.java:31`, `:53-60` (reservation),
`:117`, `:122-127` (profile).

**Requirement — and it lands on an accepted ADR.** ADR 0016 makes an external
mapping unique on `(property_id, integration, entity_type, external_id)` and
bijective in both directions. That shape holds, but `entity_type` must be able
to distinguish **which external identifier of an entity** is being mapped, not
merely which kind of entity. Two ids of one reservation, mapped under one
`entity_type`, collide on a constraint that is doing exactly its job.

This is a refinement of an accepted ADR, and therefore a **finding to report,
not a change to make**. Registered as `CONN-Q8` (architect, 2026-08-30) — an
amendment to ADR 0016: the mapping key gains the identifier kind. This page
holds its evidence; the ruling is the planner's.

**Ruled as proposed — planner, 2026-08-31** (ADR 0128 §8; ADR 0016 carries the
amendment). The mapping identity becomes
`(entity_type, identifier_kind, external_id)`, property-scoped and **bijective
within the three-part key**, with `identifier_kind` **connector-declared** —
the external system defines what its identifiers mean, and HotelOS needs no
universal vocabulary of them. The invariant:

> **Within a property and identifier kind, an external identifier maps to
> exactly one canonical HotelOS entity.**

So every one of OHIP's typed identifiers maps, each under its own kind, rather
than one being nominated primary while the rest ride along unmapped. What the
kinds *are* for Oracle is still unknown from the reference — see §9, where the
`type` values are parsed and never read — and they are the connector's to
declare rather than the platform's to guess.

### R11 · Guest identity inside a reservation is a search, and every step of it can fail
*(42b G-C12 · study §5.4)*

**Fact.** Reaching the guest's name takes four filters across four lists, each
of which may be empty and each of whose flags may be false everywhere:

```text
the guest    reservationGuests[]  where primary == true
the name     personName[]         where nameType == "Primary"
the address  addressInfo[]        where address.primaryInd
the phone    telephoneInfo[]      where telephone.primaryInd
the email    emailInfo[]          where email.primaryInd
```

**Evidence.** `cloud/services/impl/OracleCloudReservationServiceImpl.java:176-208`;
the two hard failures when the primary guest or primary name is absent at
`:178` and `:181`. Phones additionally carry `phoneTechType` and `phoneUseType`
(`cloud/dto/mongo/Reservation.java:212-214`), so "the guest's phone number" is
a typed choice among several.

**Requirement.** The model must not assume a reservation has exactly one
guest, that the guest has exactly one name, or that a contact detail is
singular or untyped. It must be able to express *"no primary guest was
marked"* as a state, because the source produces it.

---

## 4 · Time

The six problems of study §5.3 as six requirements. They are listed together
because they compound: a single reservation can exercise all six.

### R12 · A source sends dates; a hotel operates on datetimes
*(42b G-C11a)*

**Fact.** `arrivalDate` and `departureDate` carry no time. A usable timestamp
exists only after combining the date with the **property's** configured
check-in or check-out clock time, in the **property's** zone.

**Evidence.** `cloud/dto/mongo/Reservation.java:69-71`; the combination at
`cloud:227-228`, `onPremise:321-322`, `web:380-381`; the two clock times as
property configuration at `cloud/dto/jpa/OracleCloudProperty.java:35-37`.

**Requirement.** The model must distinguish a **date the source gave** from a
**timestamp we computed**, and must record that the computation depended on
property configuration. A consumer that cannot tell them apart will treat an
inferred 14:00 arrival as an observed one.

### R13 · Expected times stand in for actual times
*(42b G-C11b)*

**Fact.** For a stay in house or departed, the timestamps available are
`reservationExpectedArrivalTime` and `reservationExpectedDepartureTime` — the
PMS's *expectation*. The status says the guest is in the room; the timestamp
says when they were due.

**Evidence.** `cloud/dto/mongo/Reservation.java:94-98`; used as the check-in
and check-out times at `cloud:231`, `:235-236`.

**Requirement.** The model must distinguish **expected** from **actual**. This
is not pedantry: an arrival-time report built from expected times measures the
reservation, not the guest, and the two differ by hours.

### R14 · Which clock to read depends on the status
*(42b G-C11c)*

**Fact.** Four statuses, four different rules for the same two fields:

```text
booking    arrival date + property check-in time  |  departure date + check-out time
checkIn    expected arrival time                  |  departure date + check-out time
checkOut   expected arrival time                  |  expected departure time
cancelled  as booking, plus a cancellation time from lastModifyDateTime
```

**Evidence.** `cloud/services/impl/OracleCloudReservationServiceImpl.java:226-242`.

**Requirement.** Timestamp derivation is **state-dependent**, and the model
must let a connector express that without a consumer needing to know it. A
consumer must never have to re-derive a time from a status.

### R15 · Wire formats are per call site, not per source
*(42b G-C11d)*

**Fact.** Three formats inside one integration —
`yyyy-MM-dd HH:mm:ss.S` (OHIP), `yyyy-MM-dd'T'HH:mm:ss` (on-site) and
`dd-MM-yy` (the next-blocked date) — each hardcoded where it is parsed, with a
fourteen-format sniffer behind them for callers that did not know.

**Evidence.** `cloud:171`, `:231`, `:235-236`, `:241`; `onPremise:156`, `:169`,
`:239`; `web:61-62`; `web/services/OracleWebRoomServiceImpl.java:53`; the
sniffer at `co/instio/global/utils/DateUtils.java:70-77`.

**Requirement.** Format is a property of the **field**, not of the connector.
The model must not assume one source speaks one format, and normalisation must
fail loudly on an unparseable value rather than fall through to a guess — the
sniffer returned `null` on failure and every caller carried on.

### R16 · The property time zone is mandatory and must never be defaulted
*(42b G-C11e)*

**Fact.** A blank time zone silently became `Asia/Kolkata`: the three-argument
conversion fell through to a two-argument overload with that zone hardcoded.
The daily housekeeping sweep hardcoded the same zone for **every** property
regardless of where it was.

**Evidence.** `co/instio/global/utils/DateUtils.java:171-184`;
`modules/housekeeping/service/background/HousekeepingBackgroundService.java:50`.

**Requirement.** A property's time zone is **required input, not a default**.
Every timestamp derived without one must be refused, not approximated. This is
the failure with the widest blast radius on the page: it is silent, it is
plausible, and it moves every derived time by a fixed offset that looks like
correct data.

**Amended 2026-08-31 from the cross-vendor survey (`docs/working/42c` §2).**
A UTC **offset is not a time zone, and must not satisfy this requirement.**
Cloudbeds supplies `propertyZoneOffset` — an offset — rather than an IANA zone
(`cloudbeds/models/PropertyBasic.java:41`,
`cloudbeds/repositories/mysql/PropertyBasicRepository.java:25`). An offset
cannot express daylight saving, so a stored offset is wrong for half the year
in any property that observes it, and it is wrong in exactly the way this
requirement exists to prevent: silently, and while still looking like correct
configuration. The model must require an IANA zone; where a source can supply
only an offset, that is a connector-level gap to be recorded and resolved
against the property's configured zone — never accepted in its place.

### R17 · The business date is a distinct field, and its owner is unruled
*(42b G-C11f, and 42b G-A4 → `CONN-Q6`)*

**Fact.** Every OHIP reservation carries `createBusinessDate` — the hotel's
operating day, which rolls at night audit and is not the calendar date. The
reference stored it and never used it.

**Evidence.** `cloud/dto/mongo/Reservation.java:51`.

**Requirement — carried, pending a ruling.** The model must leave room for the
property's business date on a reservation, because the source supplies it and
a PMS reconciles against it. **What it must not do is invent an owner for it.**
`CONN-Q6` asks whether the platform has a business date and which domain
establishes it; until that is ruled, this page records the field as *carried
from the source*, and nothing derives from it.

---

## 5 · Commercial terms

### R18 · Guarantee, deposit and cancellation terms are structured, and their deadlines are offsets
*(42b G-C20 · brief §2's "guarantee types" · study §5.5)*

**Fact.** A guarantee is not a policy string. It carries a code, a short
description, `onHold` and `reserveInventory` flags, a `defaultGuarantee` flag,
a **deposit policy** whose deadline is an offset **from the booking date**, and
a **cancellation penalty** whose deadline is an offset **from arrival** plus a
drop time — each with an amount expressed as a basis type, a number of nights
and a currency.

**Evidence.** `cloud/models/OracleCloudReservationGuarantees.java:13-27`
(codes and flags), `:50-52` (`offsetFromBookingDate`), `:75-79`
(`offsetFromArrival`, `offsetDropTime`), `:85-92` (`basisType`, `nights`,
`currencyCode`). Fetched per property and arrival date at
`cloud/services/impl/OracleCloudGuaranteeServiceImpl.java:58-59`, and only for
reservations still in `Reserved` (`cloud:112-113`).

**Requirement.** If the model carries these at all, it must carry the
**offsets**, not resolved timestamps. An offset from arrival survives the
arrival date changing; a resolved deadline does not, and a cancellation
deadline that silently stops matching its reservation is a chargeable error.
The reference kept two pre-formatted human strings and discarded the structure
(`cloud:243-248`).

**Where these land is not this page's to say.** They are commercial terms of a
reservation, and the Reservations/GuestOps domain that would own them is ruled
but unbuilt (`CTX-Q2` / ADR 0089, and brief §4). Recorded here as a source
fact.

### R19 · Money must be typed, and currency travels with it
*(42b G-C21 · study §3.2, §6)*

**Fact.** The same amount is a `String` on the two on-site flavours, parsed
with `Float.parseFloat` at the point of use, and an `int` on the cloud flavour.
The normalised shape had a `currency` field that nothing ever populated, and
the only currency code in the whole reservation payload sits inside the
guarantee's amount block.

**Evidence.** `onPremise/dto/mongo/OracleOnPremiseReservation.java:63` and
`web/dto/mongo/OracleWebReservation.java:67` (String), parsed at
`onPremise:357`, `web:420`; `cloud/dto/mongo/Reservation.java:102`
(`amountBeforeTax` as `int`); the unpopulated `currency` at
`common/models/InstioReservation.java:53`; `currencyCode` at
`cloud/models/OracleCloudReservationGuarantees.java:91`.

**Requirement.** An amount must never be a string or a bare number in the
model, and must never appear without its currency. The reference's
`totalAmount` is a `float` with a `currency` that is always null — an amount
whose meaning cannot be recovered.

**And the tax basis travels with the amount — amended 2026-08-31 from the
cross-vendor survey (`docs/working/42c` §2).** The cloud amount is explicitly
**before tax** (`amountBeforeTax`, `cloud/dto/mongo/Reservation.java:102`).
Apaleo's is explicitly **gross** — `totalGrossAmount`
(`apaleo/models/ReservationDetailed.java:70`, read at
`apaleo/services/ApaleoReservationServiceApaleo.java:289`). **Both were written
into the same downstream `totalAmount` field**, so the reference's stored
revenue means a different thing depending on which connector produced the row,
and nothing anywhere records which.

That is silent revenue corruption: the figures reconcile against nothing, and
no later reader can tell a net row from a gross one. **An amount carries three
things or it is not an amount** — the value, its currency, and whether tax is
included. A model that carries two of the three has an amount whose meaning
cannot be recovered, which is the same defect as the null `currency` above,
one level less obvious.

(Apaleo also truncates with `.intValue()` at `:289`, discarding the minor units
it had been given — a fourth way to lose the meaning of a number.)

---

## 6 · How change arrives

### R20 · A notification is not a record
*(42b G-C7 · study §1.2, §5.6)*

**Fact.** Across three mechanisms — OHIP's polled business-event queue, the
webhook providers, and the on-site push — **only the push carries the record**.
OHIP business events carry `moduleName`, `actionType`, `primaryKey`,
`createdDateTime` and `hotelId`; the webhooks carry an entity id. Everything
else requires a read-back.

**Evidence.** `cloud/models/BusinessEventResponse.java:28-42`; the read-back at
`cloud/services/impl/OracleCloudEventServiceImpl.java:97` and
`cloud/services/impl/OracleCloudReservationServiceImpl.java:139`;
`apaleo/resources/ApaleoHookResource.java:34-37`;
`cloudbeds/resources/HookResource.java:50`.

**Requirement.** **Notify-then-fetch must be a first-class shape**, not an
implementation detail inside one connector. The model must be able to represent
a change that is known to have happened and whose content has not yet been
retrieved — and, because the fetch can fail, must be able to represent one
whose content could not be retrieved at all.

### R21 · The event type does not tell you what happened
*(42b G-C8 · study §5.5)*

**Fact.** OHIP emits `UPDATE RESERVATION` for a check-in, for a check-out and
for a genuine edit alike. The reference re-read the reservation and discarded
the event unless the status was still `Reserved`, with the reason written in:
`//skipping update events of checkIn and checkouts`.

**Evidence.** `cloud/services/impl/OracleCloudEventServiceImpl.java:99-104`;
the comment at `cloud/services/background/OracleCloudBackgroundService.java:71`.

**Requirement.** The **fetched state**, not the source event type, determines
the business fact published. This is the same rule ADR 0016 Part 2 already
states from the other direction — *emit the business fact, not the process* —
and the source is the reason it is not optional here: a connector that mapped
`UPDATE RESERVATION` to an update event would publish an update when a guest
checked in.

### R22 · A drained queue is consumed, and cannot be re-read
*(42b G-C7 · study §1.2, §4)*

**Fact.** The OHIP business-event queue is read destructively — the same URL is
requested until it answers `204`, which terminates only because the server
removes what it hands over. When processing failed after the drain, the events
were gone from the source.

**Evidence.** `cloud/services/impl/OracleCloudEventServiceImpl.java:50-79`;
the failure path that loses them at `:81-84`.

**Requirement.** For a destructive source, **durability of the received
notification is the connector's responsibility at the moment of receipt** —
there is no re-fetch. The model must distinguish a source that can be re-read
(a webhook the sender retries; a record fetched by id) from one that cannot.
This is the requirement that makes Chapter 21's *"every incoming event is
stored before processing"* load-bearing rather than merely prudent, and it is
why it must hold for polled sources and not only for pushed ones.

### R23 · Source paging is the source's, and its position is state
*(42b G-C9 · study §4, §7.3)*

**Fact.** OHIP returns `totalPages`, `offset`, `limit`, `hasMore` and
`totalResults` on the housekeeping view, and takes a fixed `limit` with no
cursor on the event queue. Our own API standard mandates **cursor** pagination
(Chapter 27 §Pagination) — which governs what we expose, not what we consume.

**Evidence.** `cloud/models/HousekeepingRoomInfo.java:14-22`; the fixed limit at
`cloud/services/impl/OracleCloudEventServiceImpl.java:53`. The reference parsed
every paging field and then took `.get(0)` (`OracleCloudHousekeepingServiceImpl.java:82`).

**Requirement.** A connector must be able to **persist its position in a
source's own paging scheme**, offset-based or otherwise, across restarts. Note
that this cannot be one scheme per connector either: within the cloud flavour
alone, the housekeeping view and the event queue page differently.

### R24 · Polling cadence is per property, per time of day, and enablement has three levels
*(42b G-C10 · study §1.2, §3.5)*

**Fact.** The cadence was tuned and the tuning survives as commented
annotations — every three hours, tightening to every fifteen minutes between
14:00 and 16:00, expressed in the **property's** time zone, with two zones
tried. Enablement is three independent switches: the property is enabled, the
customer's configuration is enabled, and a separate flag turns event polling
off without disabling the property.

**Evidence.** `cloud/services/background/OracleCloudBackgroundService.java:46-58`;
the three-level predicate at `cloud/dao/mysql/OracleCloudPropertyRepository.java:14-19`,
and the `fetchEvents` flag at `cloud/dto/jpa/OracleCloudProperty.java:43`.

**Requirement.** Polling schedule is **per-property configuration expressed in
the property's time zone**, and must permit different intervals at different
times of day — the check-in window needs a tighter one. Suspending a
connector's *polling* must be possible without disabling the *property*: the
reference needed that flag, and a design with a single on/off switch will grow
one.

---

## 7 · Absence, and failure

### R25 · Missing data is neither dropped nor invented
*(42b G-C18 · study §5.5)*

**Fact.** The reference did both, on different flavours. A non-booking
reservation with no phone and no email was **dropped silently**. On the web
flavour, when a property had `forceInsertIntoCrm` set, a synthetic email
address was **fabricated** from the guest's first name against a fixed domain,
so that a mandatory downstream field could be satisfied.

**Evidence.** The drop at `cloud/services/impl/OracleCloudReservationServiceImpl.java:120-125`
and `:264-267`; the same rule commented out on the push flavour at
`onPremise:283-288`; the fabrication at
`web/services/OracleWebReservationServiceImpl.java:58-60`, behind the flag at
`web/dto/jpa/OracleWebProperty.java:35`.

**Requirement.** The model must have an **optionality model** — it must be able
to say *"this stay has no contact detail"* and remain valid. A required field
the source cannot always fill produces one of these two outcomes, and both are
worse than the absence: one loses a real stay, the other writes a guest record
that is not true. Chapter 10's six-field `Reservation` has no optionality model
at all, which is what makes this an input rather than an observation.

### R26 · A rejected record and a superseded record are different things
*(42b G-C17 · study §3.4)*

**Fact.** One mechanism carried both. `dumbReason` held
`NO RESERVATION ID` and `BLANK ROOM NO` — a human must look — alongside
`CHECKOUT SUCCESS` and `RESERVATION CANCELLED` — this record was correctly
superseded and is kept for audit. Same collection, same untyped string field.

**Evidence.** `onPremise/services/OracleOnPremiseReservationServiceImpl.java:361-366`;
`web/services/OracleWebReservationServiceImpl.java:424-431`; the reason
vocabulary is enumerated in study §3.4.

**Requirement.** The model must distinguish **could not be processed** from
**processed and superseded**. They differ in retention, in whether anyone is
alerted, and in whether replaying them is correct or harmful — replaying a
superseded record re-applies a stale state.

### R27 · Health is per capability as well as per connector
*(42b G-C19 · study §4)*

**Fact.** The one aggregate the reference built counted bookings received,
arrivals, check-ins, due-outs and check-outs, with two booleans —
`checkInsDown` and `checkOutsDown`. A connector can be authenticated, polling
and green while **check-ins specifically** have stopped arriving.

**Evidence.** `common/models/IntegrationInfo.java:8-21`.

**Requirement.** The model must express liveness **per capability**, not only
per connector. Chapter 10's seven health signals are all connector-level; the
failure a hotel actually notices is that no check-in has arrived since 09:00
while every connector-level signal is green.

---

## 8 · One requirement that is about the connector, not the reservation

### R28 · Three flavours of one vendor are three sources
*(study §1, §8.7 — **attached to `CONN-Q1`** by the architect, 2026-08-30, as
the fact that the packaging unit is the integration, not the brand)*

**Fact.** `oracle-cloud`, `oracle-onpremise` and `oracle-web` differ in
transport, direction, credential model, capability set and status vocabulary.
They share a status parser and nothing else.

**Requirement.** Wherever the platform records *which source a fact came from*,
these are **three identities, not one**. This is not merely a packaging
opinion: ADR 0020 has the Kernel validate an asserted connector identifier
against a closed set registered by the Integration Hub, so the identity is a
platform-visible value with a constraint behind it — and `oracle` as a single
registered identifier would make the provenance of every Oracle event
ambiguous, which is the exact failure ADR 0020 exists to prevent.

---

## 9 · Known unknowns — what the reference never established

Recorded so that a later reader does not mistake the reference's silence for
the PMS's simplicity. **None of these is a requirement**, because the study
cannot support one.

* **The `type` vocabulary of the typed identifiers (R10) is never read.**
  `reservationIdList[].type` and `profileIdList[].type` are parsed and stored;
  no line in the reference compares either against a literal. What values OHIP
  actually emits is unknown from this source.
* **The business-event vocabulary is one value deep.** `"UPDATE RESERVATION"`
  is the **only** `actionType` literal anywhere in the reference, and
  `"Reservation"` the only `moduleName` literal — both verified by search
  across all ten providers. Every other module's events were fetched, stored
  and dropped. The full vocabulary, and what OHIP emits for a *new* reservation
  as opposed to an update, is not established here.
* **No flavour writes a reservation back to the PMS**, so nothing in the
  reference establishes what OPERA requires to accept one. If `CONN-Q5` rules
  write-back in scope, that shape must come from the vendor's documentation,
  not from this study.
* **Online check-in was never implemented** — the endpoint exists and its
  handler is empty — so the fields a guest supplies at online check-in, and how
  they reach the PMS, are unknown.
* **Room traces were accepted and discarded**, so the trace model is known only
  from the request shape: a room, a date and free text.
* **The reference had no concept of event provenance, ordering or versioning**,
  so it offers no evidence about how OHIP orders concurrent changes to one
  reservation. That is a question for the vendor's documentation and, for our
  side, ADR 0016 Part 2 and the Event Store's `entity_version`.

---

## 10 · What this page does not contain

* **No model.** No field names, no types, no proto, no schema, no enum. The
  normalisation model is deliverable 3 in the platform repository, and it is
  blocked on `CONN-Q3`.
* **No platform findings.** The ten *"our design already covers it"* items stay
  in `docs/working/42b-gaps-against-our-design.md` §3, where they are
  statements about the platform rather than about the PMS.
* **No answer to an open question.** `CONN-Q3` through `CONN-Q7` are with the
  planner; R17 carries a field and declines to name its owner, and R18 records
  commercial terms without placing them in a domain.
* **Nothing copied.** Every citation points into the read-only reference
  outside both repositories; no source, schema or fixture has been transcribed.

### Closing note — a traceability gap in `42b`, reported not patched

Every class C finding of `42b` §4 is carried here: G-C1→R1, G-C2→R2,
G-C3 and G-C4→R5, G-C5→R3, G-C6→R4, G-C7→R20 and R22, G-C8→R21, G-C9→R23,
G-C10→R24, G-C11a–f→R12–R17, G-C12→R11, G-C13→R10, G-C14→R9, G-C15→R7,
G-C16→R8, G-C17→R26, G-C18→R25, G-C19→R27.

**Two requirements here had no `42b` number, because `42b` §4 under-listed
them:** R18 (guarantee, deposit and cancellation terms) and R19 (money and
currency). Both are class C by the brief's test — PMS facts the model must
carry — and the guarantee half is named explicitly in brief §2's list of what
to extract. They were established in the study (§5.5, §3.2, §6) and lost
between it and the gap analysis's numbered list.

**Closed.** Reported rather than patched, and then ruled: the architect
patched `42b` on 2026-08-30 before the planner package, adding **G-C20**
(guarantee terms → R18) and **G-C21** (money and currency → R19) and moving the
class C count 19 → 21. The reasoning is worth keeping: `42b` is *evidence* for
the planner's questions rather than part of the package's text, so correcting
the evidence before it is read is the honest act — and leaving a known gap
because a page had been accepted would be the opposite.

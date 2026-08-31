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
rules on what §14 reports.

Three things it deliberately does not contain: **no folio** (GUEST-Q6 —
Finance's), **no merge logic** (G360-Q1 — Guest360's), and **no code**. The
brief gates code behind the owner's verification of the mockup and behind the
application-caller authorization round (§13).

---

## 1 · What GuestOps is, in one paragraph

GuestOps is the installable application that owns **reservations, room-stays
and guest identity** — the domain ADR 0089 §CTX-Q2 named and left unbuilt, and
the one the Integration Hub's deferred queue has been filling for. It is the
book in a property with no PMS, and the PMS's counterpart in a property with
one: the same data model either way, differing only in **who writes the stay's
lifecycle** (GUEST-Q1). It owns the `reservations` schema, publishes
`reservation.*` and `stay.*` facts, serves the Context Service's guest chain
through domain-owned read views, and touches no other application's tables.

---

## 2 · The domain, nine aggregates

Field lists below are the design's proposal; types are indicative, and the
**canonical contract is the proto** in `shared/protos` (ADR 0026 — there is no
`Contracts/`). Every identifier is a **UUIDv7 of ours**; no PMS identifier is
ever a primary key.

### 2.1 · `Booking` — the group

```text
booking_id          UUIDv7
property_id         the property holding these legs — Master Data ref
group_ref           the group identifier, carried from day one (GUEST-Q2)
expected_stay_count int?   R9 — "noOfRooms: 3" with one room sent (S3, S30)
origin              staff | pms
created_at
```

**The group is deliberately thin.** GUEST-Q2 rules that *every operation
happens to a stay, never to the group*, so `Booking` carries identity, the
expectation, and nothing operational. `expected_stay_count` is nullable
because a source may not say — *"three expected, one known"* and *"one known,
expectation unstated"* are different, and collapsing them loses S30's whole
point.

**A group that spans properties is not modelled here** (S4, S32). This
installation holds its own legs; `group_ref` is what makes the onward legs
*sayable* without being queryable, and no cross-installation read exists
(GUEST-Q2's edge-first rider).

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
arrival_date        date, as the source gave it            R12
departure_date      date, as the source gave it            R12
arrival_at          Timestamp value object (see §2.8)      R12·R13·R14
departure_at        Timestamp value object
lifecycle           Booked | InHouse | Departed | Cancelled | NoShow   §3
completeness        a set of what is missing — see below   R6·R9·S11
walk_in             bool — how the guest arrived           S13
pms_unknown         bool — who knows about them (GUEST-Q5) S5
business_date       the property's operating day this stay's arrival
                    belongs to — attached, never computed here (§7)
row_version         optimistic concurrency
```

**`walk_in` and `pms_unknown` are two columns because they are two facts**
(GUEST-Q5's ratified rider): one is how the guest arrived, the other is who
knows about them. A walk-in entered in the PMS is not PMS-unknown; a phone
booking taken here during an upgrade is PMS-unknown and not a walk-in. One
flag would lose the walk-in ratio, which every hotel reports on.

**`completeness` is a separate axis from `lifecycle`, and this is R1's lesson
applied to the stay.** R1 is about a room's four independent statuses; the
same mistake is available here — *"checked in, room not yet reported"* (R6,
S11) is not a lifecycle state, it is a complete lifecycle with an incomplete
record. Collapsing them produces a status vocabulary that grows one value per
kind of missing field, and the discarded axis cannot be recovered.

```text
completeness ⊆ { no_assignment, party_unnamed, no_arrival_time,
                 no_contact, terms_unknown }
```

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
amount            Money (§2.8)                              R19
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
  stay_id · grc_no
  document_refs   the platform's media/asset references, never blobs here
  signed_at · captured_by
```

**A request is GuestOps's, and the work is not.** *"What needs doing"* is
Jobs' domain (APPS-Q1), so raising a job from the stay page **records the
request and announces it** — Jobs creates the job, assigns it and owns its
status. GuestOps stores no job id, no assignee and no job state; the boundary
is the constitution's, not a preference.

`Registration`'s contents are deliberately a short list plus references:
scenario-record §15 (g) is open, and what an Indian property must legally
capture for domestic and foreign guests is the owner's knowledge. The shape
holds; the field list grows when (g) is answered.

### 2.8 · Two value objects the whole schema depends on

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

---

## 3 · The stay state machine, and R7's one rule

### 3.1 · The states

```text
                 ┌──────────► Cancelled        (terminal)
                 │
   Booked ───────┼──────────► NoShow           (terminal)
      │          │
      │          └──────────► InHouse ────────► Departed
      │                          ▲                  │
      └──────────────────────────┴──────────────────┘
                     staff correction only (S24)
```

Lifecycle rank, which is the only ordering the rule needs:

```text
Booked 0   <   InHouse 1   <   Departed 2
Cancelled and NoShow are terminal exits from Booked
```

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
| the same fact twice (replay, §7) | equal rank, same values → idempotent; nothing changes and nothing is published |

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
confirmed         one stay · the PMS identifiers mapped on (§6)
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

## 5 · The events

Business facts, never process events (ADR 0016 Part 2). Names are proposed
here and are **subject to the register's event-subject stability rule** — a
name changes when the capability changes, never because an implementation
moved.

```text
reservation.created        the group exists: group_ref, expected count, origin
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

**Two things GuestOps does not publish**, and each removal is deliberate:
no `disagreement.*` (§4.2), and **no group-level cancellation** — GUEST-Q2
rules that every operation happens to a stay, so cancelling a booking is *n*
`stay.cancelled` facts and a consumer never has to expand a group.

**`events.append(tx, event)` is a local write in the caller's transaction**
(CLAUDE.md, and it is not optional here): the stay change and its announcement
commit together, or a crash between them keeps the check-in and loses the
event.

---

## 6 · Identifiers, and the one thing this design cannot settle

Every identifier GuestOps mints is a **UUIDv7 of ours**. Opera's confirmation
number is never a key.

R10 is the complication: an OHIP reservation carries `reservationIdList[]`,
each entry a `{id, type}` pair — *there is not "the reservation id"*. So the
stay carries typed external references:

```text
StayExternalRef
  stay_id · integration_id · id_kind · external_id
```

**`CONN-Q8` is open and this design lives inside its v1 restriction:** until
the mapping key gains the identifier kind, the Hub maps **one identifier kind
per entity type** — the connector's declared primary — and the others ride on
the fact as references. `id_kind` exists here so that the day `CONN-Q8` is
ruled, nothing is remodelled.

**What this page cannot settle, and reports instead (§14, finding 2):**
whether that table is GuestOps's or the Hub's. The brief says PMS identifiers
are external ids in mapping tables and that the PMS-id mapping is the Hub's
(ADR 0016) — which is plainly right for *rooms*, where Master Data owns the
canonical id and the Hub resolves it during Enrich. It cannot be applied
unchanged to a **stay**, because the canonical id does not exist until this
application mints it, so the Hub has nothing to map to when the first fact
arrives. The design proposes GuestOps holding the reservation-side references
and answering *"which stay is this fact about"* itself — the same reasoning
that makes GUEST-Q5's candidate link a domain decision — and asks for a
ruling rather than assuming one.

---

## 7 · What the Hub's deferred queue drains into

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

## 8 · What Context asks, and what GuestOps answers

ADR 0089 §CTX-Q1 fixes the shape: Context owns no tables, writes nothing, and
reads **stable domain-owned views** through EF Core keyless entities —
**each contributing domain owns its view's definition and compatibility.**
Three views, and they are a published contract of this application:

```text
v_guest_contact_index     guest_id · property_id · kind · value_index
                          the blind index — exact match, no plaintext
                          → phone → guest                              §2.5

v_stay_current            stay_id · property_id · booking_id · group_ref
                          guest_id(s) · room_id · room_type_id
                          arrival · departure · lifecycle
                          → guest → reservation → room
                          → room → who is in it now

v_stay_room_reference     room_id · open_stay_count
                          → ADR 0062's deletion-reference check:
                            a room with an open stay is not deletable
```

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

### 8.1 · What GuestOps asks *of* Context — and what does not exist yet

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
  — scenario-record §15 (e), carried as configuration
```

**The rule is not in doubt; the resolver is.** *"Context over joins"* — an
application never reads another's tables, and a cross-domain relationship is
obtained through the Context Service. Both panels are therefore Context
questions, and Context v1 ships room, rooms, staff, asset and property
summary and nothing else (ADR 0089 §"The v1 scope this fixes"). There is no
*stay → jobs* and no *stay → servicing* resolver, and the contributing
domains — Jobs, Room Care — would each have to own a read view the way §8
has GuestOps own three.

Three consequences the design accepts rather than works around:

* **Nothing is stored here.** GuestOps never keeps a job id, a job status, a
  cleaning record or a room's readiness. When the resolver arrives, the panels
  light up; until then they are absent, and no data has to be migrated.
* **Absence is normal, not degraded.** Jobs and Room Care are *installable*.
  A property without Jobs still has a guest complaining about the air
  conditioning, so the **request** is GuestOps's own record always, and only
  the raising and the status are conditional (the mockup draws both states).
* **This is reported, not designed around** — §14, finding 4. Inventing a
  direct read into Jobs' schema to make a tab work would break the one rule
  the whole platform's modularity rests on.

---

## 9 · Permissions

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

## 10 · The application bundle

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

## 11 · Slices

| | Ships | Unblocks |
|---|---|---|
| **1 · The book** | Booking · RoomStay · Assignment · party · guest identity · the bookings list · the four day lists · the stay's overview and activity · standalone writes · cancel · the state machine · `stay.*` facts · the three views | a standalone property can run its front desk; **Context's guest chain turns on** |
| **2 · The PMS mode** | the deferred queue drains · overrides · disagreements · silent confirmation · candidate links · the staleness banner | the Hub's backlog stops accumulating; Oracle properties go live |
| **3 · The rest of the desk** | commercial terms · registration · requests · notes · preferences · the group page's full behaviour | *"every other guest operation"* (GUEST-Q1) is complete |
| **4 · The neighbours** | the *stay → jobs* and *stay → servicing* panels | **blocked on finding 4, ratified 2026-08-31 as "drawn, not built"** — two contributing-domain read views and their Context RPCs, owned by Jobs' and Room Care's rounds |

**Two v1 items this table does not yet carry, because they are not ruled:**
the **room-level double-booking guard** and the **standalone day roll**
(`03-the-open-questions.md` §C1a, §C2). Both are recommended for slice 1 and
neither is built on a recommendation.

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

## 12 · What this design deliberately does not do

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

## 13 · The gates — named, not worked around

| | State | What it blocks |
|---|---|---|
| **The application-caller authorization round** | **Open** — APPS-Q1: no .NET application principal exists | GuestOps's service calling the Kernel and Context as itself. **The whole backend**, and the brief gates code behind it |
| **The registry-driven shell** | **Open** — APPS-Q1: `PLATFORM_APPS` is hardcoded | the desktop module appearing without editing the shell |
| **ADR 0061 authorization materialisation** | **Ruled, unbuilt** — nothing writes tuples today | any stay-level authorization object. v1 needs none (property + application scope), so this is named rather than depended on |
| **`CONN-Q8` — the identifier kind** | **Open** (an amendment to ADR 0016) | nothing now; §6's `id_kind` exists so the ruling changes no model |
| **The reservation-identifier mapping's home** | **Reported, §14** | which service answers *"which stay is this fact about"* |
| **§15 (e) — check-in into an unreleased room** | **Open** | nothing: carried as **property configuration** (refuse · warn · record), defaulting to *warn*, and absent entirely when Room Care is not installed |
| **§15 (g) — the registration card's contents** | **Open** | nothing: carried as a **records list** — `grc_no`, documents, signature — whose statutory fields the owner fills in |
| **Finance** | **Not started** | settlement in a standalone property, knowingly (GUEST-Q6) |

---

## 14 · Findings — reported, not resolved

**1 · The constitution's event examples name the wrong aggregate.** CLAUDE.md
§"Event-first architecture" lists `reservation.checked_in` and
`reservation.checked_out` among its examples. Under GUEST-Q2 a *reservation is
a group* and checking in happens to a **room-stay**, so those two subjects
name an operation that cannot occur — a group does not check in (S23: there is
no such thing as checking out a group). This design publishes `stay.arrived`
and `stay.departed`. The list is illustrative and an example given in passing
is not a ruling, but the constitution should not carry a subject the model
forbids. **Reported for the architect; not silently resolved either way.**

**2 · Where does the reservation ↔ PMS-identifier mapping live?** §6 states
the problem: ADR 0016's mapping is bijective on a canonical id that, for a
stay, does not exist until GuestOps mints it, so the Hub cannot map the first
inbound fact. Either the Hub completes its mapping from `stay.created`
(carrying the external reference), or GuestOps owns the reservation-side
references outright. The design proposes the latter and does not assume it.
**A question for the register — the architect's number to claim.**

**3 · Chapter 26's `GuestContext` includes `vip_status`.** ADR 0089 §CTX-Q3
excluded it from v1. The chapter's head note now marks three superseded parts;
this is a fourth, smaller one, and it is only a documentation reconciliation.
**Reported.**

**4 · The stay page needs two Context resolvers that do not exist** — *stay →
jobs* and *stay → servicing* (§8.1). The owner asked for both panels and both
are right: a front desk is asked *"has anyone been in my room?"* and *"is
someone coming about the AC?"*, and the answers live in Room Care and Jobs.
The platform rule is settled — Context answers cross-domain questions, and an
application never reads another's tables — so what is missing is **two
contributing-domain read views and their Context RPCs**, which are those
applications' rounds and not this one's. Named here so the mockup's two
cross-application tabs are read as *drawn, not built*. **A question for the
register.**

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

## 15 · What this page does not contain

* **No code, and no created tree.** §10 is a proposal; the brief gates the
  build.
* **No proto.** The contract language is `shared/protos`, and the protos are
  written with the code, not before the mockup is verified.
* **No answer to §15 (e) or (g)** of the scenario record, and no anticipation
  of one.
* **Nothing copied.** Every PMS fact is cited to `R<n>` in the requirements
  page beside `pms-oracle/`, which cites the read-only reference outside both
  repositories.

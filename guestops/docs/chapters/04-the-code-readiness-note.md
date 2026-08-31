# 04 · Code readiness — the reconciliation, and what I would build

**Status:** readiness note, 2026-08-31. Stream FF. **No code is written on the
strength of this page**; it exists so the architect can say *start* against
something specific.
**Reconciled against** everything that landed after this round stood down:
`CONN-Q11`, `SHELL-Q23`, `WF-Q7`, `WF-Q8`, `PKG-Q39`, and DD's
`shared/protos/hotelos/integration/v1/dto.proto` (469 lines, the normalised
fact).
**Rule for §1:** every mismatch was reported with its evidence, and every
resolution was marked a proposal. **All nine were ruled the same day as
`GUEST-Q9` / `GUEST-Q10`**, and §1 now records the ruling beside each — what
was applied to this design, what DD applies to the wire, and the two findings
that were mine to fix.

---

## 0 · The result in one paragraph

**Nine mismatches, two of them against my own artifacts, and one clean pass —
all closed.** The clean pass is `PKG-Q39`: every subject GuestOps plans to
publish routes to the `GUEST` stream, and none dead-letters. `GUEST-Q9` then
ruled every difference: **the fact grows rather than this scope shrinking**
(M2, M3), **waitlist and pending become real states here** (M1), and the rest
was a joint sweep in which each side adopted what the other had right. The two
findings against my own work are mine to fix.

```text
applied to this design      M1 · M5 · M6 · M7 · M8 · the fourth view
DD applies to the wire      M2 · M3 · M4 · M6's zone condition
mine to fix in the mockup   frame 4's Export · frame 7 (stands once M2 lands)
off this stream             B1's fifteen further places → GUEST-Q10
```

---

## 1 · The reconciliation

### 1.1 · `PKG-Q39` — no unrouted subject · **clean**

The check `PKG-Q39` asks for. Measured, not assumed:

```text
streams.rs SPECIFICATION, stream GUEST:
  "property.*.guest.>"        "property.*.stay.>"      "property.*.reservation.>"
```

All three domains this application publishes into are claimed, so **no subject
GuestOps plans to publish is unrouted** and none can silently dead-letter. The
routing keys on the **domain segment**, and `jetstream.rs`'s guard list says so
in the Workforce comment — *"the ACTIONS here are illustrative and Workforce
will name its own; what the stream claims is the DOMAIN segment"*.

Two notes for the moment code starts, neither a blocker:

* The application names `domain.action`; **the Kernel builds the subject**
  (`subjects.rs:24-35` → `property.<property_id>.<domain>.<action>.v<version>`).
  So this design's `stay.arrived` is correct at the application layer, and no
  GuestOps code ever writes a NATS subject.
* `jetstream.rs`'s guard list pre-names `reservation.checked_in` and
  `guest.arrived` for this unbuilt domain. They route (the domain is claimed)
  and this application will publish neither. Whether the guard should carry
  GuestOps's real actions is CC's call when wiring starts — **raised, not
  assumed**.

### 1.2 · `CONN-Q11` — this design is named as the source, and its view set is short

> *"A consumer that needs the distinction on one screen composes it from the
> stays via Context (CTX-Q4's shape)."* — CONN-Q11

The ruling makes **GuestOps's stay facts the source** for arrival-versus-
stayover, which the room fact deliberately cannot say. That is the right
split and this design agrees with it.

**The mismatch is in §9's view set.** `v_stay_current` answers *"which stays
are current"*. The composition CONN-Q11 points at needs *"which stays touch
**this room** on **this business date**, and in what relation"* — arriving,
staying over, departing. That is a date-ranged question, and `v_stay_current`
cannot answer it.

**APPROVED — `GUEST-Q9`.** A fourth domain-owned view, added to design §9.

```text
v_stay_room_day    property_id · room_id · business_date
                   stay_id · relation: arriving | staying_over | departing
```

It is derived entirely from stays this application already holds, and it is the
shape R2 requires — *several stays touch one room on one day* — which is
exactly what the room fact's `repeated reservation_statuses` carries on the
other side of the boundary.

### 1.3 · `SHELL-Q23` — and a control I drew without flagging it

The print half is recorded with this round's registration card cited. **The
file-save half stays open** — GG's correction: *"the month-end payroll sheet is
a file a packaged frontend hands to the user to save, a different shell
capability from a print dialog."*

**My own finding against my own mockup:** frame 4 (the stay's Activity tab)
carries an **`Export`** button. That is a file-save, which is the half of
`SHELL-Q23` that is still open — and unlike the balance in frame 7, **I drew it
without a caveat**. Two controls in the same mockup needed the same treatment
and only one got it.

**RULED — mine to fix, and fixed:** the `Export` control now carries the same
caveat the balance got. Two controls needed the same treatment and only one had
it.

### 1.4 · `WF-Q7` and `WF-Q8` — no impact, and one line worth adopting

**`WF-Q7` (zone on the Posting): no impact.** GuestOps holds no zone, posts
nobody, and reads no allocation. The row's own sentence confirms the direction
of travel — *"Room Care reads 'who has zone 3' via Context, allocation input
never a gate (CTX-Q4, APPS-Q2)"* — which is this round's `APPS-Q2` applied by
another stream.

**`WF-Q8` (MOD is a span): no impact, but its rule is worth stating here.**
*"Who is MOD right now"* is the clock against the span — **derived, never
stored**, no `is_current_mod` flag and no nightly job. Read carelessly that
would indict this design's stored projections, so the line is worth drawing:

```text
a CLOCK-DEPENDENT value      never stored — it goes stale on its own
  who is MOD now · the current business date · availability
a PROJECTION of sibling      may be stored — it changes only when its
columns in the same row      source row changes, in the same transaction
  current_room_id · arrival_date from arrival_at
```

This design already sits on the right side of both. `RoomStay.business_date` is
a **stamped historical fact** — the operating day an arrival belonged to — not
the rolling current date, which stays derived (ADR 0128 §6).

### 1.5 · The wire contract — six differences

Field-for-field against `dto.proto`. **All six ruled as `GUEST-Q9`**, each
recorded beside its finding.

#### M1 · Three lifecycle values the model cannot hold — *the significant one*

```text
dto.proto:173-188   BOOKED · CHECKED_IN · CHECKED_OUT · CANCELLED · NO_SHOW
                    DUE_OUT (6) · WAITLISTED (7) · PENDING (8)
design §3.1         Booked · InHouse · Departed · Cancelled · NoShow
```

* **`DUE_OUT` is not a gap.** It is *in house and leaving today*, which this
  model composes from `InHouse` + `departure_date` — and composing rather than
  adding a value is precisely `CONN-Q11`'s ruling one level up. A fact arriving
  as `DUE_OUT` maps to `InHouse`.
* **`WAITLISTED` and `PENDING` are a gap.** They are real on-site Oracle
  statuses (R5's vocabulary: *"PENDING · WAITLIST"*), they are neither
  `Booked` nor anything else this model has, and §3.2's rank rule has no rank
  for them. A fact carrying one **cannot be applied today**, and the honest
  outcomes are both bad: map it to `Booked` and the desk sees a confirmed
  booking that is not one, or reject it and lose a real record (R25's first
  failure).

**RULED — `GUEST-Q9`: they join the state machine as pre-confirmation
states**, at rank 0 below `Booked`. *Waitlist is a first-class reservation
state in every major PMS — the desk must see a waitlisted booking as
waitlisted.* Both named bad outcomes are refused. Applied in design §3.1, with
two consequences worked out there rather than left implicit: **two states share
a rank**, so a same-rank move to a *different* state is applied while the
identical fact stays idempotent; and **`Waitlisted` holds no inventory**
(§5.2a), because a waitlist is a queue position rather than a room and counting
one would make a full hotel look oversold.

**`DUE_OUT` upheld as argued** — composed, not a sixth value, `CONN-Q11` one
level up.

#### M2 · Commercial terms cannot arrive over the wire

`RoomStayFact` carries `Money total_amount` (`dto.proto:428`) and **nothing
else commercial**. No guarantee code or flags, no deposit offset, no
cancellation offset, no drop time, no penalty amount or nights.

`GUEST-Q6` ruled the stay's commercial terms **into v1**, and R18 established
them as source facts that must be carried as offsets. Over this contract they
cannot be carried at all.

**And this indicts my own mockup, not just the proto.** Frame 7 marks the rate
`FROM OPERA` and shows guarantee, deposit policy and cancellation penalty
beside it. **In a PMS-connected property those three rows can only ever be
staff-entered** — the screen asserts a provenance the wire cannot deliver.

**RULED — `GUEST-Q9`: the fact grows, this scope does not shrink.**
`GUEST-Q6` ruled the substance and DD materialises it on the wire — a
commercial-terms block per R18. **Frame 7 therefore stands as drawn**, and its
`FROM OPERA` marks become true when the block lands. Design §2.6 is unchanged.

#### M3 · `GUEST-Q7`'s kept source set is not on the wire

```text
ruled kept set     source · travel_agent · market_code · meal_plan
                   guest_counts · rate_code
on RoomStayFact    adults (9) · children (10)          — the counts only
```

`GUEST-Q7` widened this deliberately — *"store every significant field the PMS
sends on a reservation (source, travelAgent, marketCode, mealPlan, study §3.2's
full list)"* — and the study confirms the flat flavours carry all four on the
row. **They are absent from the fact**, so the ruling cannot be honoured for
PMS-sourced stays.

`source_detail` has the same problem one level out: there is no carrier for
the fields the contract has not modelled. `Absence` (`dto.proto:325`) records
what is *missing*; nothing records what arrived and was not mapped.

**RULED — `GUEST-Q9`: the fact grows.** The kept set joins `RoomStayFact` and
`source_detail` travels with it, so `GUEST-Q7` is honoured for PMS-sourced
stays as it already is for staff-created ones. Design §2.6b is unchanged —
and the contract gains the **retention** half of R25, having had only the
discard half.

#### M4 · `walk_in` cannot arrive

No field on `RoomStayFact`. A walk-in raised in the PMS reaches GuestOps
indistinguishable from a booking made three weeks earlier, and S13's rule is
that the flag is unrecoverable if not set when the stay is created. Staff-
created walk-ins are unaffected.

**RULED — `GUEST-Q9`: the wire gains it.** A walk-in is something the source
can state, so it is stated rather than inferred. S13's rule holds on both
paths — the flag is set when the stay is created or it is unrecoverable.

#### M5 · The booking group's identity is modelled twice, differently

```text
design §2.1     group_ref            a single scalar
BookingGroup    repeated ExternalRef · expected_room_stays · is_complete
```

Two differences. The wire is **right and this design is under-specified**: a
group has several typed identifiers for the same reason a stay does (R10), and
`GUEST-Q8`'s *minting is the mapping* applies identically — the group gets our
UUIDv7 and a set of typed references. Separately, the wire **carries**
`is_complete` where §2.1 derives incompleteness from `expected_stay_count`.

**RULED — `GUEST-Q9`: this design adopts what the wire has right.** Applied in
§2.1 — `booking_external_ref` rows, `group_ref` retired, and `is_complete`
carried as the source's assertion beside our own `expected_stay_count`, because
the two answer different questions and S30 needs both.

#### M6 · Two date fields the wire does not have

§2.2 stores `arrival_date`/`departure_date` (*"as the source gave it"*)
**and** `arrival_at`/`departure_at`. The wire carries only `FactTime`
(timestamp + `TimeBasis`), and the date is the timestamp's own date component.

Storing both invites exactly the defect this design warns about elsewhere: two
columns that may disagree, with no rule saying which wins.

**RULED — `GUEST-Q9`: applied, with a condition specified back to DD.** The
two source-date columns are dropped and the date is a projection of the
timestamp — **provided a `TIME_BASIS_DERIVED` timestamp is constructed in the
property's IANA zone**, so its own date component *is* the source's date. Built
in UTC or from an offset it carries the wrong date near midnight and R12's
distinction is lost silently (R16). Design §2.9 carries the condition.

#### M7 · `completeness` is poorer than `Absence`

```text
design §2.2   completeness ⊆ { no_assignment, party_unnamed, … }
Absence       field · reason { NOT_SUPPLIED · NOT_AVAILABLE_FROM_SOURCE ·
                               UNREADABLE } · raw_value
```

The wire distinguishes *the source sent nothing*, *this integration cannot
supply it*, and *it arrived and was unreadable* — and carries the raw value in
the last case. This design's closed set collapses all three, which is R26's
distinction (*rejected* versus *superseded*) losing a neighbour.

**RULED — `GUEST-Q9`: `completeness` adopts `Absence`'s triple.** Applied in
§2.2. The closed set collapsed three sentences that differ in whether anyone
is alerted, whether a connector needs fixing, and whether replay helps.

#### M8 · `id_kind` versus `identifier_kind`

`dto.proto:53` names it `identifier_kind`; this design had `id_kind`. Same
concept, `CONN-Q8`'s. **RULED — `GUEST-Q9`: one spelling, the contract's.**
Applied throughout §2.1 and §7.

### 1.6 · B1's correction did not reach as far as I reported it

**Reported against myself.** B1 named `CLAUDE.md`, and `CLAUDE.md` was
corrected. A repository sweep now finds `reservation.checked_in` /
`reservation.checked_out` alive in **fifteen further places**:

```text
shared/protos/hotelos/events/v1/events.proto:63      the contract language's own doc
services/kernel/.../events/subjects.rs:24            the subject builder's doc
services/kernel/.../store/events/append.rs:31        the append doc
services/kernel/.../tests/jetstream.rs:312           the pre-named guard list
docs/chapters/Chapter 12 …:258                       the subject-hierarchy table
docs/chapters/Chapter 27 …:514,516                   API standards
docs/chapters/Chapter 16 …:454 · 23:704 · 25:395
docs/api-standards.md:733-734
docs/data-model.md:705,707,743,756,758
docs/architecture/41-check-in-event-propagation.md:24,32
```

None of it breaks routing — the domain segment is what routes, and
`reservation` is claimed. What it does is teach every future reader the shape
`GUEST-Q2` forbids, which is the reason B1 was raised at all.

**The lesson is mine:** I reported the occurrence I found rather than sweeping
for the class. *"Reconcile new answers against existing docs"* means grepping
the repository, not citing the file you happened to be reading. **Reported for
the architect; nothing outside `guestops/` touched.**

---

## 2 · The schema I would build

`guestops`, per CLAUDE.md's canonical list. **As designed in `02-…` §2**,
with §1.5's eight proposals unresolved and therefore **not** incorporated.

> **Corrected 2026-08-31.** This page and `02-…` §2 both said `reservations`,
> and both cited CLAUDE.md for it. CLAUDE.md says `guestops`; so does
> `03-schemas.sql`, which names the error outright — *"`guestops`, `roomcare`
> and `jobs` — never `reservations`, `housekeeping` or …"* — and `02-roles.sql`
> provisions `hotelos_owner_guestops` and nothing by the other name. The
> implementation carried the wrong name for one afternoon and could never have
> migrated; it is `guestops` in code, and the citation that licensed the mistake
> is the point worth keeping. `APPS-Q3` covers schemas too.

```text
booking                 group identity · expectation · origin
booking_external_ref    proposed, M5

room_stay               the anchor: room type, dates, lifecycle,
                        completeness, walk_in, pms_unknown, business_date
stay_external_ref       typed source identifiers — minted with the stay,
                        one transaction (GUEST-Q8)
assignment              room over time; the move is a row, not a value
stay_guest              party membership · is_primary nullable
guest_identity          the person as this property knows them
contact_point           ciphertext + HMAC blind index
commercial_terms        rate · guarantee · offsets · penalty (GUEST-Q6)
stay_source             the kept set + source_detail (GUEST-Q7)
stay_request · stay_note · registration · stay_reporting
stay_disagreement       override · confirmed · cleared, both values kept
stay_link_candidate     the PMS-unknown join (GUEST-Q5)
stop_sell               room type + date range (GUEST-Q7)
room_out_of_order       an EVENT-DERIVED read model, never authoritative
```

Three rules the migrations are written under: **UUIDv7 everywhere**; **no
foreign key leaves this schema** — `property_id`, `room_id`, `room_type_id`
and `staff_id` are Master Data references carried as values; and **the event
and its state change commit together** (`events.append(tx, event)`).

## 3 · The proto surface

`shared/protos/hotelos/guestops/v1/` — `service.proto`, `events.proto`,
`dto.proto`, per the constitution's *no handwritten API contracts*. **This
application's protos are not `dto.proto`'s**: the integration contract is what
the Hub hands us, and ours is what the desk and other applications call.

Two rules that fall out of §1: **nothing in our proto re-declares an
integration type** — `Money`, `FactTime`, `ExternalRef` are `hotelos.integration.v1`'s
and are imported, not copied (the no-duplicated-shared-code rule) — and
**no message carries a NATS subject**, because the Kernel builds those.

## 4 · The events

Unchanged from `02-…` §6, and all routed (§1.1):

```text
reservation.created · reservation.expectation_changed
stay.created · stay.amended · stay.assigned · stay.room_changed
stay.arrived · stay.departed · stay.cancelled · stay.no_show · stay.corrected
guest.created · guest.updated
```

No `disagreement.*` — a disagreement is a fact about our records, not about
the hotel; the **correction** is the business fact.

## 5 · The .NET layout

The platform template (ADR 0026), under the five standards, and **verified
against the real tree before any file is created** (ADR 0037):

```text
guestops/backend/
  src/
    Domain/          RoomStay · Booking · Assignment · GuestIdentity
                     CommercialTerms · the state machine + R7's one rule
    Application/     book · assign · move · check in · check out · cancel
                     no-show · correct · clear · availability · the day roll
    Infrastructure/  EF Core · the guestops schema · the three read
                     views (four, if §1.2's is taken) · the blind index
    Grpc/            one file per subject; the composition root holds none
    Events/          publish our facts; consume the Hub's and out-of-order
    Background/      the day roll · the staleness watch
  tests/             characterisation, with a recording authorizer and a
                     recording event appender (ADR 0054)
```

E2E adds **one test per boundary class** in the platform suite, never a second
characterisation suite; an absent database **fails** the run (ADR 0053).

## 6 · What must be true before the first file

1. The owner verifies the two HTML pages.
2. **`APPS-Q1`'s two prerequisites have a plan of record** — the registry-driven
   shell, and the application-caller authorization round. Without the second,
   this service cannot call the Kernel or Context as itself.
3. **DD's proto change lands** — M2's commercial-terms block, M3's kept set and
   `source_detail`, M4's `walk_in`, and M6's zone condition on a derived
   timestamp. §2.6, §2.6b and §2.2 are written to receive them; building the
   readers before the fields exist is the one thing here that would be rework.
4. The architect says start.

**What is no longer waiting:** M1 unblocked the state machine, and M5–M8 are
applied. Nothing in §2 is now written against an unanswered question.

**Nothing on this page is a decision.** Where it proposes, it says so.

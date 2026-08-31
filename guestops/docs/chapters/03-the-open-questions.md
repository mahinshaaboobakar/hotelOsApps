# 03 · What is still open — the GuestOps round's handoff

**Status:** questions, 2026-08-31. Stream FF, deliverable 4 of the GuestOps
round — brief `docs/working/45-the-guestops-round.md` §3.4, **in the platform
repository**.
**No numbers are claimed here.** `GUEST-Q7…` are claimed in the platform
register by the architect before use. Everything below is stated as a question
with the repository facts that ground it, and **nothing below is resolved by
anticipating an answer**.
**Already ruled and not repeated:** GUEST-Q1 (two modes, staff-may-override),
GUEST-Q2 and its addendum (the group of room-stays; the anchor is the room
type, the room number an assignment), GUEST-Q3 (the standing override is the
one answer that leaves the application), GUEST-Q4 (no second mode; a matching
fact confirms silently), GUEST-Q5 (the PMS-unknown stay and its
staff-confirmed link), GUEST-Q6 (the book plus commercial terms; the folio is
Finance's).

---

## 0 · How this page is organised

Three groups, because they go to three different people:

```text
A  the owner        operational judgments only a hotelier can make
B  the planner /    contradictions and gaps in the platform's own record
   architect
C  scope            things the design found while drawing, each with a
                    v1 / next-version recommendation the owner decides
```

Group C carries **the architect's recommendation** in each row. A
recommendation is not a ruling and carries no authority — it exists so the
owner is choosing between stated positions rather than starting from a blank
page.

---

## A · For the owner — still open from the scenario record

### A1 · Does GuestOps refuse a check-in into a room Room Care has not released?
*(scenario record §15 (e), S9)*

**The facts.** Cleaning is **policy-driven, not event-driven** (APPS-Q1,
owner) — a checked-out room becoming a task is a hotel policy, never an
automatic consequence. Room Care is an *installable* application and may not
be present at all, and the *stay → room-readiness* Context resolver does not
exist yet either (§B4). So GuestOps can neither assume it can ask nor assume
the answer would bind.

**Carried, not blocked.** The design holds this as **property configuration** —
*refuse · warn · record* — defaulting to **warn**, and absent entirely when
Room Care is not installed. Nothing is built that a ruling would have to undo.

**The question.** Does a property get that choice, or does the platform take
one position?

### A2 · What must the registration card capture, and is there a statutory report behind it?
*(scenario record §15 (g), §12)*

**The facts.** The hotelier reference carries a `grcNo`; the design holds
`Registration` as a short list plus document references and grows it when this
is answered. What an Indian property is legally required to record — and what
it must file for **foreign nationals** — is the owner's knowledge and is
nowhere in this repository.

**Why this one is not merely a field list.** If a statutory filing exists, it
is a **deployment obligation, not a feature**: a property cannot legally
operate without it, and it would move from "slice 3" to "slice 1" the moment
that is confirmed. It is flagged here rather than left to be discovered.

---

## B · For the planner and the architect — the platform's own record

### B1 · The constitution's event examples name the wrong aggregate

`CLAUDE.md` §"Event-first architecture" lists `reservation.checked_in` and
`reservation.checked_out`. Under **GUEST-Q2** a reservation is a **group**, and
checking in happens to a **room-stay** — there is no such thing as checking in
a group (S23). The design publishes `stay.arrived` and `stay.departed`.

The list is illustrative and an example given in passing is not a ruling, but
the constitution should not carry a subject the ruled model forbids.
**Reported for reconciliation; not resolved either way by this round.**

### B2 · Where does the reservation ↔ PMS-identifier mapping live?

ADR 0016 makes an external mapping unique and bijective on
`(property_id, integration, entity_type, external_id)` ↔ a **canonical id**.
That works for a room, where Master Data owns the canonical id and the Hub
resolves it during Enrich. It cannot transfer unchanged to a **stay**: the
canonical id does not exist until GuestOps mints it, so the Hub has nothing to
map when the first inbound fact arrives.

Either the Hub completes its mapping from `stay.created` carrying the external
reference, or **GuestOps owns the reservation-side references outright**. The
design proposes the latter — answering *"which stay is this fact about"* is a
domain decision, the same reasoning that makes GUEST-Q5's candidate link
staff-confirmed — and does not assume it.

Related and already open: **`CONN-Q8`** (the mapping key gains the identifier
kind, R10). The design carries `id_kind` so that ruling changes no model.

### B3 · Chapter 26's `GuestContext` still carries `vip_status`

ADR 0089 §CTX-Q3 excluded it from v1 — *"unknown business definition → bool →
becomes architecture"*. The chapter's head note now marks three superseded
parts; this is a fourth, smaller one. **A documentation reconciliation.**

### B4 · Two Context resolvers the stay page needs, and whose rounds they are

*(ratified 2026-08-31 as "drawn, not built"; recorded here so the work has an
address)*

```text
stay → jobs         Jobs' contributing read view + a Context RPC
stay → servicing    Room Care's contributing read view + a Context RPC
stay → readiness    Room Care's — also what A1 would need
```

The rule is settled and was deliberately not broken: an application never
reads another's tables, and a cross-domain relationship comes from the Context
Service (ADR 0089 §CTX-Q1 — each contributing domain owns its view). So what
is missing is not a decision but **two contributing read views**, owned by
Jobs' and Room Care's rounds. Slice 4 of the design is blocked on them.

---

## C · Scope — found while drawing, each with a recommendation

### C1 · Availability. *"Do we have a Deluxe King on 3 September?"*
**Recommendation: v1, in a limited form — and it is the largest known gap.**

Nothing in the design answers it. In a **PMS-connected** property Opera owns
inventory and the desk books there, so the gap is survivable. In a
**standalone** property GuestOps *is* the book — and a book that cannot say
whether a room type is free on a date will be used to double-sell the hotel in
its first week.

Two different things sit behind one word, and they are not the same size:

```text
room-level conflict     is room 214 already assigned to an overlapping stay?
                        — computable from Assignment alone. SMALL
type-level availability how many Deluxe Kings are sellable on 3 Sep?
                        — needs the room inventory, out-of-order rooms
                        (Maintenance's), and a stop-sell concept. LARGE
```

**The recommendation is to take the first now and rule on the second.** The
conflict check is a query over data the design already holds and stops the
worst outcome. Type-level availability needs owners for facts this application
does not have — out-of-order rooms are EngineeringOps's (ADR 0056), and
nothing in the platform owns *sellable inventory* at all.

**And the guard warns rather than forbids.** GUEST-Q5 already ruled that a
double-booked room can be *the truth* when a candidate link is rejected — so a
hard block would make a ruled outcome unreachable.

### C2 · The day roll, and who marks a no-show in a standalone property
**Recommendation: v1 for standalone; it is a hole, not a feature.**

`ADR 0128 §6` puts the **business-date boundary** in Property Registration and
derives the current date in Context — and says the night-audit transition
event has **no owner yet** (*"a future Night Audit owner — not yet defined"*).

In a PMS-connected property this does not bite: the PMS runs its night audit
and the no-show arrives as a fact. In a **standalone** property nothing rolls
the day, so a stay that never arrived sits in *Booked* forever, the arrivals
list keeps yesterday's guests, and no-show is a number nobody records.

**The design's position, offered rather than assumed:** GuestOps runs a
property-local **day roll** that *flags* unarrived stays for staff and marks
nothing itself — consistent with APPS-Q1's rule that a consequence is a
policy, not an automatic act. Whether that is GuestOps's or a future Night
Audit owner's is the planner's, and the answer changes where the code goes,
not whether it is needed.

### C3 · Booking source, market segment and channel
**Recommendation: v1 — carry it, do not compute it.**

Every PMS carries where a booking came from (direct · OTA · corporate ·
travel agent · walk-in) and every hotel reports on it. The design carries none
of it today.

The argument is the **walk-in flag's** argument, and it has already been
accepted once: a fact that arrives with the reservation and is not recorded at
the moment it arrives is **unrecoverable later**. Carrying a source code costs
one field; reconstructing six months of channel mix does not happen.

### C4 · An upgrade — is it an assignment, or an amendment?
**Recommendation: needs a ruling; small, and it will be wrong once if guessed.**

GUEST-Q2's addendum makes the **room type** the anchor and the **room number**
an assignment. So when the desk puts a guest booked into a Deluxe King into an
Executive Suite, two readings are available and the design does not choose:

```text
an assignment   the room changed; the booked type is what was sold
an amendment    the stay's type changed; the guest now has a Suite
```

It matters beyond vocabulary: the **rate**, the **group's expected room
types**, and any later availability calculation (C1) all read the type. The
mockup's `Assignment.reason` carries `upgrade` as a value, which is the
narrower reading, and it is marked as an implementation choice rather than a
ruling.

### C5 · Pseudo rooms and house accounts
**Recommendation: v1 boundary check; no feature.**

R4: room types carry a `pseudoRoom` flag, and pseudo rooms are PMS bookkeeping
constructs — house accounts, group masters — that are **not physical rooms**.
Mapping one to a canonical room is a permanent data error, because the
canonical room does not exist.

The mapping is the Hub's, so the *check* is the Hub's. What is unstated is what
**GuestOps** does when a stay arrives against one anyway: the design's position
is that it is **unmappable** (the Hub's second outcome) and never reaches this
application. Worth confirming rather than assuming, because the failure is
silent and permanent.

### C6 · Who may see a guest's full phone number and email?
**Recommendation: needs a ruling before slice 1 ships.**

The design encrypts contact points and indexes an HMAC of the normalised phone
(§2.5) — that protects the **store**. It says nothing about the **screen**, and
the gold mockup masks by default (`+91 98470 •••• 12`) purely as a drawing
convention, with no rule behind it.

A front desk plainly needs the number to call a guest about a late arrival. A
thousand-guest history is a different thing. There is a permission vocabulary
to hang this on (`guest.write`, `reservation.read`), and no ruling that says
whether reading a contact is one of them.

### C7 · Reinstating a cancelled stay
**Recommendation: v1, small.**

The cancel dialog says either stay can be reinstated separately, and nothing
in the design describes it. It is a staff correction in the §3.2 sense —
backwards movement, the stay's write permission, recorded — but the room may
have been sold in between, which is C1 again.

### C8 · The registration card has to be printed and signed
**Recommendation: v1 for the capability; the content waits on A2.**

The guest signs a card at the desk. The design captures `grc_no`, documents
and a signature, and **nothing in the platform prints anything** — there is no
print surface in any chapter, ADR or mockup in this repository. This is
plainly a platform capability rather than GuestOps's own, and it is named here
because a front-desk application that cannot produce the card the guest signs
is not deployable in an Indian hotel.

### C9 · Company, travel agent and "who is paying"
**Recommendation: next version — with one field kept now.**

A corporate booking is made *by* a company or a travel agent, and the bill
goes to them. Full company/TA **profiles** are CRM's — Guest360 in the suite
(APPS-Q1) — and the billing half is Finance's (GUEST-Q6).

What is cheap and unrecoverable is the same argument as C3: **the booker's
name and reference as they arrived on the reservation**. Carry the text now,
link it to a profile when one exists.

### C10 · A rooming list for a group
**Recommendation: next version.**

S2's three-room booking with *"colleagues, names to follow"* is handled — the
parties are unnamed and valid. Naming twenty of them one sheet at a time is
not, and a bulk rooming-list import is a real convenience for a hotel that
takes conference business. It changes no model, which is exactly why it can
wait.

### C11 · *"This guest has stayed here before"* — without Guest360
**Recommendation: v1, and the boundary is what makes it safe.**

G360-Q1 gives Guest360 the person-graph, and the design stores no `person_id`.
But GuestOps can answer a strictly narrower question from its own records:
*"this **guest identity record** has three stays"* — same `guest_id`, no
inference, no merge, no claim that two records are one person.

That is genuinely useful at check-in and it does not trespass: the moment
Guest360 exists, its answer replaces this one and no data moves. Drawing it
requires no ruling, but it is listed so the owner sees the line being drawn.

### C12 · Reporting
**Recommendation: next version.**

Occupancy, arrivals/departures, walk-in ratio, no-show ratio, channel mix.
Enterprise Analytics is a platform component and the four day lists are the
operational report v1 needs. Every number above is derivable from facts v1
publishes — **provided C3 is taken now**, which is the only reason this row
is here at all.

---

## D · Recommendations, gathered

| | Subject | Recommendation |
|---|---|---|
| C1a | room-level double-booking guard | **v1** — small, and stops the worst outcome |
| C1b | type-level availability / inventory | **needs a ruling** — no owner exists for sellable inventory |
| C2 | the day roll and no-show in standalone | **v1** — a hole, not a feature |
| C3 | booking source / market segment | **v1** — carry it or lose it |
| C5 | pseudo rooms are unmappable | **v1** — a boundary check, no feature |
| C7 | reinstate a cancelled stay | **v1** — small |
| C8 | the registration card printed | **v1 capability**, content waits on A2 |
| C11 | *"three stays on this record"* | **v1** — narrower than Guest360, no overlap |
| C4 | upgrade: assignment or amendment | **ruling first** — small, wrong once if guessed |
| C6 | who may see a full phone number | **ruling first** — before slice 1 ships |
| A1 | check-in into an unreleased room | **carried as configuration** — blocks nothing |
| A2 | the registration card's contents | **owner** — possibly a deployment obligation |
| C9 | company / travel agent profiles | **next**, keeping the booker's text now |
| C10 | rooming-list import | **next** |
| C12 | reporting | **next**, and it depends on C3 |

**Nothing in group C is being built on the strength of a recommendation.** The
design chapter's slices stand as written until the owner rules; this page
exists so that the choice is visible rather than made silently by whoever
writes the first migration.

---

## E · What this page does not contain

* **No new answers.** Every ruled question is in the register; every open one
  is stated here without a preferred outcome dressed as a fact.
* **No numbers.** `GUEST-Q7…` are the architect's to claim.
* **Nothing copied.** PMS facts cite `R<n>` in the requirements page beside
  `pms-oracle/`, which cites the read-only reference outside both
  repositories.

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
                    — BOTH RULED, 2026-08-31. Kept for the reasoning
B  the planner /    contradictions and gaps in the platform's own record
   architect
C  scope            things the design found while drawing, each with a
                    v1 / next-version recommendation the owner decides
```

**Groups A and C are closed** — every owner question this round raised has
been answered: six as **GUEST-Q7** and the upgrade boundary as **GUEST-Q8 (b)**,
all on 2026-08-31. **B1 and B2 are closed too** — B2 by GUEST-Q8 (a), minting
is the mapping; B1 by a correction to `CLAUDE.md` itself. What remains is
**B3–B6**: four items of the platform's own record, going to the planner and
the architect **one at a time**.

The rows are kept rather than deleted because the *reasoning* behind a ruling
is the part that stops it being re-litigated in six months.

**And a process instruction arrived with GUEST-Q7 that binds this page**, and
every page like it:

> **Where a modern standard exists, rule it — do not ask.**

C6 is the worked example. *"Who may see a guest's full phone number?"* was
raised here as a question; it should have been **applied** — masked by
default, revealed deliberately, the reveal recorded — because that is settled
practice for stored contact PII and there was nothing property-specific to
decide. A question that has a standard answer costs a round trip and teaches
the reader that the matter is genuinely open, which it was not. The test
before a row is added here: *is this a judgment about this business, or a
solved problem I have not looked up?*

Group C carries **the architect's recommendation** in each row. A
recommendation is not a ruling and carries no authority — it exists so the
owner is choosing between stated positions rather than starting from a blank
page.

---

## A · For the owner — still open from the scenario record

### A1 · Does GuestOps refuse a check-in into a room Room Care has not released?
*(scenario record §15 (e), S9)* — **RULED, owner, 2026-08-31: no.**

> **An application's own flow is never gated on another application being
> installed. An absent dependency loses its *capability*, never the *flow*.**
> *"If Jobs is not installed we cannot create a job. If there is no Room Care,
> the cleaning process cannot be tracked. Check-in and check-out are
> GuestOps's responsibility."*

**And the ruling is wider than the question.** It answers A1 — check-in
proceeds, readiness is display-only where Room Care and the resolver both
exist — and it states a **platform principle** that binds every application
round, not this one. It is recorded here because this round asked; **the
architect's register row and its number are outstanding**, and §B5 below says
why that matters beyond GuestOps.

**What it removed from the design:** the *refuse · warn · record* property
configuration is gone entirely, not defaulted. There was never a policy to
configure — there was a gate that should not exist.

**The distinction the design keeps:**

```text
gate on OUR OWN facts        check-in refuses an unassigned stay (S8)
gate on ANOTHER app's facts  never — it would make an installable
                             application effectively mandatory
```

### A2 · What must the registration card capture, and is there a statutory report behind it?
*(scenario record §15 (g), §12, S19b)* — **RULED, owner, 2026-08-31.**

**The card:** *"we can go with your idea."* The design proposes the field list
(§2.7 of the design chapter) — name as on the ID, date of birth, nationality,
permanent address, the identity document and its number, arriving from and
proceeding to, purpose of visit, vehicle, signature, and a separate
**foreign-national block** for the passport, the visa, the arrival in country
and the port — and **the property configures which of them are required**,
domestic and foreign separately (§2.8).

**The report:** *"setup screen … if need they will use."* So it is a
**per-property capability, not a hardcoded law**: the policy, the flag on a
stay that needs filing, and the record of a filing that was made — with the
authority, the reference, the person and the time.

**What this deliberately did not become.** Not a compulsory workflow every
property must satisfy, and not an assumption that HotelOS submits anything.
The obligation differs by jurisdiction and by property, and a platform that
hardcoded one country's rule would be wrong everywhere else.

**And the flag is a to-do, never a gate** — S19b, applying A1's ruling to our
*own* obligation: an outstanding filing does not stop a check-in.

---

## B · For the planner and the architect — the platform's own record

### B1 · The constitution's event examples name the wrong aggregate

`CLAUDE.md` §"Event-first architecture" lists `reservation.checked_in` and
`reservation.checked_out`. Under **GUEST-Q2** a reservation is a **group**, and
checking in happens to a **room-stay** — there is no such thing as checking in
a group (S23). The design publishes `stay.arrived` and `stay.departed`.

The list is illustrative and an example given in passing is not a ruling, but
the constitution should not carry a subject the ruled model forbids — and it
is the **most-read document in the repository**, so a new stream designing
against reservations would copy the shape it saw there.

**CLOSED — corrected in `CLAUDE.md`, 2026-08-31.** The examples now read
`stay.arrived` / `stay.departed`, with the reasoning kept as a dated note. The
boundary held: the finding was raised, not fixed from this round, because the
constitution is the architect's file.

### B2 · Where does the reservation ↔ PMS-identifier mapping live?
**RULED — GUEST-Q8, planner, 2026-08-31: minting is the mapping.** Asked once
for both designs on Stream DD's sweep, and answered once.

**The question was built on a wrong assumption, and that is the reusable
part.** It assumed mapping must be a **lookup performed before the entity
exists** — true for a room, where Master Data owns the canonical id and the
Hub resolves it during Enrich, and false for a stay. The first inbound fact
carrying an unknown external reference **creates the stay and its
`StayExternalRef` rows in one GuestOps transaction**: the id and its
references are born together, so there is never a moment that needs a
pre-existing canonical id. The gap dissolves rather than being bridged.

```text
master entities        ADR 0016's mapping — Core's, unchanged
operational entities   typed identifiers ride the fact; the owning
                       domain records them as it mints the id
                       → the Hub keeps NO reservation-id table
```

A mapping table exists to resolve an id somebody else owns, and nobody owns a
stay's id but GuestOps. That is why the Hub was never the right home.

Related and now **ruled**: **`CONN-Q8`** (the mapping key gains the identifier
kind, R10) was closed on 2026-08-31 and its v1 restriction withdrawn. The
design already carried `id_kind`, so the ruling cost no remodelling — which is
the argument for modelling defensively against an open question rather than
encoding its restriction.

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

**A1's ruling narrows what they are for.** All three resolvers are
**display-only**: none of them may gate a GuestOps operation, so their absence
costs a panel and never a flow. That is worth stating where the work is
scheduled, because a resolver commissioned as *"the check-in readiness check"*
would be built as a gate.

### B6 · An automatic guest filing is an outbound connector, and v1's connector contract is inbound-only

A2 leaves GuestOps holding the policy, the flag and the record of a filing —
and **not** the submission. Sending guest data to a police or immigration
system is an integration, and the constitution is unambiguous: *"no hardcoded
integrations — all integrations must use the Integration Hub"*, with a
connector as the unit (`CONN-Q1`, ADR 0128 §2).

Two facts about the platform meet here, and neither is a blocker for A2's v1
but both decide what the next step costs:

```text
ADR 0128 §4   v1 connector scope is INBOUND-ONLY. Write-back — anything
              this platform sends outward — is a separate connector
              capability in a later round
a filing      is outbound by nature. There is no inbound half of it
```

So a *"file with the authority"* button is not a small addition to GuestOps.
It is the **first outbound connector** the platform would build, and it would
land on the write-back capability that `CONN-Q5` deliberately deferred — with
a credential, an authority-specific format, a retry story and an audit
obligation heavier than a PMS push, because a filing is a legal assertion.

**Reported so the sequence is visible**: v1 records what a person filed, which
is useful on its own and costs nothing; the automatic filing waits for the
outbound connector round and is that round's decision, not this one's.

### B5 · A1's ruling is a platform principle, and it has no home yet

The owner's answer to A1 — *an application's own flow is never gated on
another application being installed; an absent dependency loses its
capability, never the flow* — is **not specific to GuestOps**. It binds Jobs,
Room Care, EngineeringOps, Guest360 and every application after them, and it
is the operational half of what ADR 0116 §5 already implies by making every
application per-user gateable: an application a property has not installed, or
a user cannot open, must not be able to stop somebody else's work.

Nothing in the record states it. `CLAUDE.md` §"Modular platform" and ADR 0051
§"An application is a bundle" describe modularity structurally; neither says
what happens at run time when the neighbour is missing.

**Reported for a register row and a number** — the architect's, not this
round's. Recorded in this application's pages meanwhile so that the design is
not carrying an unattributed rule.

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
**RULED — GUEST-Q8 (b), 2026-08-31: an assignment.** A higher-type room with
the **terms unchanged** is `stay.room_changed` like any assignment (R8); it
becomes an **amendment** only when the booked type or the terms themselves
change. The test is *what changed*, not *what the guest got* — a courtesy
upgrade leaves the sale as booked, and treating it as an amendment would
rewrite what was sold. Carried in design §2.3 and scenario S28.

*The original framing is kept below, because it is why the answer is not
obvious.*

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

| | Subject | Outcome |
|---|---|---|
| C1a | room-level double-booking guard | **RULED — GUEST-Q7: slice 1, both modes.** Warns and names the other stay; never forbids, because GUEST-Q5 made a double-booked room a possible truth |
| C1b | type-level availability | **RULED — GUEST-Q7: slice 1, and no new inventory owner.** *Computed* from our own stays, Master Data's rooms, out-of-order **consumed by event**, and stop-sell. Design chapter §5 |
| C2 | the day roll and no-show in standalone | **RULED — GUEST-Q7: slice 1.** It **flags** unarrived stays and marks nothing; §3.3 |
| C3 | booking source / market segment | **RULED — GUEST-Q7, and widened:** keep **every significant field** the PMS sends — source, travel agent, market code, meal plan, guest counts — named as a kept set, with the rest retained as `source_detail` rather than discarded. §2.6b |
| C6 | who may see a full phone number | **RULED — GUEST-Q7, as standard practice:** masked by default, one-click reveal on the stay's write permission, **the reveal recorded**. §2.5 |
| C8 | the registration card printed | **RULED — GUEST-Q7: the button stands.** The platform has no print surface; **its register row is raised when this reaches wiring**, not now |
| C5 | pseudo rooms are unmappable | **v1** — a boundary check, no feature |
| C7 | reinstate a cancelled stay | **v1** — small |
| C11 | *"three stays on this record"* | **v1** — narrower than Guest360, no overlap |
| C4 | upgrade: assignment or amendment | **RULED — GUEST-Q8 (b): an assignment**, unless the booked type or the terms change |
| A1 | check-in into an unreleased room | **RULED — never refuses; no configuration.** The principle needs a register row (§B5) |
| A2 | the registration card, and guest reporting | **RULED — the field list is the design's, the required set is per property; reporting is a setup screen with a recorded filing.** The automatic submission is an outbound connector (§B6) |
| C9 | company / travel agent profiles | **next**, keeping the booker's reference now (C3 keeps it) |
| C10 | rooming-list import | **next** |
| C12 | reporting | **next** — and C3's widening is what makes it possible later |

**Seven of the twelve were ruled within a day of being raised**, six into
slice 1 and C4 as a boundary. The three still marked *v1* are the design's own
small items and need nobody's decision; the three marked *next* are scope the
owner has already placed.

**Group C is closed.** What remains in this document is group B — the
platform's own record — and it goes to the planner and the architect one item
at a time.

---

## E · What this page does not contain

* **No new answers.** Every ruled question is in the register; every open one
  is stated here without a preferred outcome dressed as a fact.
* **No numbers.** `GUEST-Q7…` are the architect's to claim.
* **Nothing copied.** PMS facts cite `R<n>` in the requirements page beside
  `pms-oracle/`, which cites the read-only reference outside both
  repositories.

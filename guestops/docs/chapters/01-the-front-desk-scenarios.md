# 01 · The front desk scenarios — what GuestOps must be able to do

**Status:** scenario record, 2026-08-31 — accepted by the owner, then amended
the same day to carry five rulings: the **GUEST-Q2 addendum** (a stay's anchor
is the room *type*; the room number is an assignment required at check-in),
**GUEST-Q3** (while a disagreement stands, the standing override is the one
answer that leaves the application), **GUEST-Q4** (there is no second mode —
PMS-writes-first always; a matching fact confirms silently), **GUEST-Q5**
(staff may create a PMS-unknown stay; the join is a staff-confirmed link,
never a match) and **GUEST-Q6** (v1 is the book plus the stay's commercial
terms; the folio is Finance's, later). Stream FF,
deliverable 1 of the GuestOps round — brief
`docs/working/45-the-guestops-round.md` §3.1, **in the platform repository**.
**Authority.** There is **no chapter for GuestOps**. Chapter 26 states the
ownership boundary (*"guests are operational entities"*) and sketches a
six-field `Reservation`; ADR 0089 §CTX-Q2 rules that the guest and the
reservation belong to *this* domain and to no other. The design authorities
are therefore: the register's owner rulings — **GUEST-Q1** (two modes,
staff-may-override), **GUEST-Q2** (the group of room-stays), **G360-Q1** (the
Guest360 boundary), **APPS-Q1** (the name and the prerequisites) — ADR 0128
(what the Hub delivers), ADR 0116 §5 (per-user application access) and
ADR 0129 (the session policy), and the PMS study's reservation facts
(`../../../pms-oracle/docs/chapters/02-the-oracle-facts-our-model-must-carry.md`,
R2, R5–R11, R18–R26).
**Source of fact for anything PMS-shaped:** that requirements page, which
cites the read-only reference outside both repositories. **Nothing is copied
from it.**

---

## 0 · What a scenario on this page is, and is not

Each entry has the same four parts, always in this order:

```text
SITUATION      what happens at a real front desk, in a sentence a
               receptionist would recognise
STANDALONE     who does what when this property has no PMS connector
PMS-CONNECTED  what arrives through the Hub, and what staff may still do
EXPRESSIBLE    what the application must be able to represent for the
               situation to be handled honestly
```

**A scenario says what must be handled. It never says how.** No field names,
no types, no proto, no table, no state names, no event subjects. That is
deliverable 2 (`02-the-guestops-design.md`), and a page that quietly named a
field would be a design wearing a scenario heading.

Where a situation has no answer in the record, it carries **`OPEN`** and its
subject is listed in §15. **`OPEN` is never resolved here by choosing.**

Scenarios are numbered **`S<n>`** so the design and the mockups can cite them
the way this round cites `R<n>`.

---

## 1 · The vocabulary, fixed once

Ruled by **GUEST-Q2** (owner, 2026-08-31) and used in this exact sense
everywhere below. The words matter because every vendor uses them
differently — `docs/working/42c` §2 found that the level a PMS calls a
*"reservation"* inverts between vendors.

| Word | Means here |
|---|---|
| **booking** *(= reservation)* | the **group**. What the guest thinks they made. Carries a group identifier from day one |
| **room-stay** | the **anchor**. One property · one room **type** · one date range · its own guest set. **Every operation happens to a room-stay, never to the group** |
| **the assignment** | the **room number** on a stay. Absent at booking, chosen the night before or at the desk, changeable until and during the stay — a room move *is* an assignment change (R8) — and **required at check-in**: a person cannot be checked into no room. GUEST-Q2 addendum, 2026-08-31 |
| **the party** | the guests attached to one room-stay. May be *"not yet named"* (GUEST-Q2), may be several people, may have nobody marked primary (R11) |
| **guest identity record** | GuestOps's record of a person as known to this property. **Not a person-graph** — linking and merging are Guest360's (G360-Q1) |
| **mode** | PMS-connected or standalone, decided per property by whether a PMS connector is active (GUEST-Q1) |
| **override** | a staff write to a stay whose lifecycle the PMS owns, recorded as one: who, when, and what the PMS said |
| **disagreement** | a later PMS fact that contradicts a standing override. **Recorded, never a silent overwrite** (GUEST-Q1, amended) |
| **the day** | the property's **business day**, whose boundary is Property Registration's configuration and whose current value is derived, never stored (ADR 0128 §6). Not midnight to midnight |

The one word this page does not use for the anchor is Apaleo's *"reservation"*
for a leg — GUEST-Q2's rider: it collides with every other vendor's meaning.

---

## 2 · The two modes, and the single sentence that separates them

> **The difference is who may write the stay's lifecycle. It is never what is
> stored.** — GUEST-Q1

```text
                       booking · check-in · check-out      every other
                                                           guest operation
standalone             staff, in GuestOps                  staff, in GuestOps
PMS-connected          the PMS, through the Hub            staff, in GuestOps
                       — staff may override, and the
                         override is recorded as one
```

Two consequences that shape every scenario below, and are not re-argued in
each one:

* **PMS-connected is not read-only.** GUEST-Q1's amendment (owner,
  2026-08-31): *"staff can do things even if the PMS is connected — they can
  override our system."*
* **Nothing GuestOps records reaches the PMS in v1.** Write-back is a
  separate connector capability in a later round (`CONN-Q5`, ADR 0128 §4). So
  an override is a HotelOS fact about a stay the PMS still believes it owns,
  and the two will disagree until a human reconciles them in the PMS. **This
  is the single most important thing the screens must not hide.**

---

## 3 · The board — what the front desk day actually is

Everything in §4–§12 is reached from one of four lists, and the lists are this
application's face the way the rota is Workforce's:

```text
Arrivals     stays due in on the business day, and what is missing on each
             (unnamed party · no room assigned · no ID captured)
In house     stays currently occupying a room, with today's departures
             separated from tomorrow's
Departures   stays due out on the business day, and those already gone
Attention    the honest list — disagreements, incomplete groups, stays with
             a PMS fact we could not apply, PMS-unknown stays with an
             unconfirmed candidate link (GUEST-Q5). A returning feed's
             differences group as ONE OUTAGE BATCH, never as twenty
             rows (GUEST-Q4)
```

Above the four, when the feed has gone quiet, a **staleness banner** rather
than a mode: *"PMS feed silent since 09:00 — your entries stand."* It is
informational, it is per capability (R27 — check-ins can stop while the
connector is green), and it changes no rule about who may write (GUEST-Q4).

**The Attention list is not a defect log.** Every one of its members is an
ordinary condition of running a hotel against a PMS feed (R20, R25, R26), and
a design with nowhere to put them would do what the reference did: drop one,
and fabricate the other (R25).

---

## 4 · The booking arrives

### S1 · A one-room booking, taken at the desk
**SITUATION.** A caller wants a room for two nights. The receptionist takes a
name, a phone number, and the dates.

**STANDALONE.** Staff create the booking: a group of exactly one room-stay.

**PMS-CONNECTED.** This is the PMS's operation. The desk takes it in the PMS;
it reaches GuestOps as a normalised fact through the Hub. Staff creating it in
GuestOps instead is **S5's case**, not this one.

**EXPRESSIBLE.** A group of one. The degenerate group must not be a special
case — *"every operation happens to a room-stay"* means the one-room booking
and the fifty-room booking take the same path.

### S1b · *"Do we have anything on the 3rd?"*
**SITUATION.** Before any of S1 can happen, someone has to answer the question
the caller actually asked. And a week later the manager closes the Deluxe
Kings for two nights because a wedding party has them.

**STANDALONE.** GuestOps is the book, so GuestOps answers — and if it cannot,
the property sells the same room twice in its first week.

**PMS-CONNECTED.** Opera answers, and the desk books there. Our number is
still shown, because the desk reads our screens all day.

**EXPRESSIBLE.** Ruled by the owner, 2026-08-31 (GUEST-Q7): **both modes are
fully v1, and availability is an answer GuestOps computes — never a table
somebody else must feed.**

```text
"is room 214 free on these dates?"    a conflict check over our own stays.
                                      Runs in BOTH modes, on every
                                      assignment and every move
"how many Deluxe Kings on 3 Sep?"     rooms of the type (Master Data)
                                      − stays holding it (ours)
                                      − out of order (EngineeringOps's,
                                        heard as events)
                                      − stop-sell (ours, per type + dates)
```

**Two things this must not become.** It must not become a **second inventory
owner** — the rooms are Master Data's and out-of-order is EngineeringOps's,
and both are read or heard, never copied as a source of truth. And the
conflict check must **warn rather than forbid**: GUEST-Q5 already ruled that a
double-booked room can be *the truth*, so a hard block would make a ruled
outcome unreachable. It names the other stay and lets a person decide.

**Stop-sell is the seller's control, not an inventory fact.** *"We choose not
to sell this type on these dates"* is a commercial decision belonging to
whoever runs the book; *"this room cannot be used"* is EngineeringOps's, and
they are different sentences.

### S1c · The hotel is full and the guest wants the date anyway
**SITUATION.** Christmas week is sold out. The caller asks to be told if
anything frees up. Somewhere else, a booking is taken but not yet confirmed —
awaiting a deposit, or a corporate approval.

**STANDALONE and PMS-CONNECTED.** Both. Oracle's on-site flavours send both
states on the wire (R5's vocabulary: *"PENDING · WAITLIST"*).

**EXPRESSIBLE.** Ruled by the architect, 2026-08-31 (GUEST-Q9): **waitlisted
and pending are first-class states**, sitting *before* a booking rather than
inside it.

```text
Waitlisted   a queue position. HOLDS NO ROOM
Pending      awaiting confirmation. Holds a room, unless the source's
             own guarantee terms say it does not
```

**The desk must see a waitlisted booking as waitlisted.** The two ways this
design could previously have handled it were both refused: showing it as
`Booked` puts a confirmed booking on the board that nobody confirmed, and
refusing the fact loses a real record (R25's first failure).

**And the availability consequence is the one to keep.** A waitlist exists
*because* the hotel is full, so counting it against inventory would make a full
hotel look oversold — and would hide the room that a cancellation gives back,
which is the exact moment the waitlist is for.

### S2 · A booking of several rooms, made at once
**SITUATION.** Three rooms, same dates, one payer, one of the three guests
named and the other two *"colleagues, names to follow"*.

**STANDALONE.** Staff create the group and its three stays together; two
parties are *not yet named*.

**PMS-CONNECTED.** The same fact arrives from the PMS — see S3, because it
usually does **not** arrive at once.

**EXPRESSIBLE.** A party that is not yet named is a valid party, not a missing
one (GUEST-Q2). It must never be filled with a placeholder — R25's
fabrication is a guest record that is not true.

### S3 · The group arrives one room at a time *(R9)*
**SITUATION.** The PMS says the booking is for three rooms and sends one.

**STANDALONE.** Does not occur.

**PMS-CONNECTED.** Oracle's on-site flavours carry `noOfRooms: 3` with a
payload describing exactly one room, and both flavours carry a written comment
that this is a source limitation rather than a modelling choice (R9). The
other two may arrive minutes later, tomorrow, or never.

**EXPRESSIBLE.** A group must be able to say **"three expected, one known"**
and remain valid and workable in that state. The desk can check the one known
stay in. What must not happen is the reference's answer — minting sibling
stays the source never sent (R9's per-room identifier that always produced
`-1`).

### S4 · The booking that spans two properties
**SITUATION.** A guest's itinerary: two nights here, three at the group's
other hotel, in one booking made once.

**STANDALONE / PMS-CONNECTED.** Both. This property's GuestOps holds **only
its own legs**; the group identifier rides along (GUEST-Q2's edge-first
rider), and **no cross-installation query is built**.

**EXPRESSIBLE.** A group with stays this installation cannot see. The desk
must be able to be told *"this booking continues elsewhere"* from the
identifier alone, and must never be shown a fabricated onward leg. Whether it
can be told anything *more* than that is Federation's question (ADR 0115,
parked), not this round's.

### S5 · A booking created here while a PMS is connected — *ruled*
**SITUATION.** The PMS is the property's book, and the desk creates a
reservation in GuestOps anyway — a walk-in at 23:00, a phone booking while
the PMS is being upgraded, a manager who prefers our screen.

**PMS-CONNECTED.** GUEST-Q1's amendment lets staff **override** a stay. A
booking the PMS has never heard of is not an override of anything: there is no
PMS value to stand against, no disagreement to flag and no confirmation to
arrive.

**EXPRESSIBLE. GUEST-Q5 (2026-08-31): staff may create it**, and the first
half of that was already the owner's — GUEST-Q1's own words, *"all guest
operations are done here by staff"*. The stay carries one honest mark:

```text
PMS-unknown     a PERMANENT, VALID state — not a pending one.
                Write-back is deferred (CONN-Q5), so some stays will
                simply never be known to the PMS
```

**And the join, when the PMS does eventually send its own version:**

```text
candidate test   same room + OVERLAPPING DATES
name similarity  may RANK candidates · may never LINK them
the link         STAFF-CONFIRMED, never automatic
same             one stay · the PMS identifiers mapped on (ADR 0016)
different        two stays, honestly — a double-booked room is then
                 the truth, not an artefact
unconfirmed      sits on Attention, like any disagreement
```

**Why the link is never automatic.** The reference solved exactly this by
correlating on `(companyId, siteId, surname, firstName, arrivalDate)` — entity
resolution by name, inside the connector, against its private copy (the Oracle
connector design §3.1). A fuzzy match here would rebuild that, and a wrong
match silently merges two guests' stays.

### S6 · A booking with commercial terms attached *(R18, R19)*
**SITUATION.** The booking is guaranteed by a card, has a deposit due seven
days after booking, and a cancellation penalty of one night if cancelled
within 48 hours of arrival.

**PMS-CONNECTED.** Oracle carries all of this structurally: codes, flags, a
deposit deadline as an **offset from the booking date**, a cancellation
deadline as an **offset from arrival** plus a drop time, and an amount with a
basis, a number of nights and a currency (R18).

**EXPRESSIBLE. GUEST-Q6 (2026-08-31) rules v1's line:**

```text
IN v1     the book, plus the stay's COMMERCIAL TERMS — the rate, the
          guarantee and cancellation offsets, every amount with its
          currency and tax basis
LATER     the FOLIO — posting, payments, settlement, invoicing, the
          night-audit posting. Finance's domain, a later round
```

The terms are carried as **offsets**, never resolved timestamps — an offset
survives the arrival date changing and a resolved deadline does not, and a
cancellation deadline that silently stops matching its reservation is a
chargeable error (R18). An amount carries **three** things or it is not an
amount: the value, its currency, and whether tax is included (R19).

**The consequence is accepted knowingly and recorded with the ruling: a
standalone property cannot settle a guest in v1.** The first deployments are
PMS-connected, where the bill is the PMS's and write-back is deferred anyway
(`CONN-Q5`).

---

## 5 · The arrival

### S7 · An ordinary check-in
**SITUATION.** The guest arrives, is identified, signs the registration card,
is given a room and a key.

**STANDALONE.** Staff check the stay in: confirm the party, capture ID and the
registration card, **assign the room if it is not already assigned — the
check-in cannot proceed without one** (S8) — and record the actual arrival
time.

**PMS-CONNECTED.** The check-in **arrives as a fact**. The registration card,
the ID and the guest's details are still GuestOps's work — they are *"every
other guest operation"* (§12).

**EXPRESSIBLE.** The moment the stay occupies the room, Room Care must be able
to learn the room is occupied and EngineeringOps that work in it now disturbs
a guest. GuestOps announces the fact; it never calls another application (the
constitution, §"Event-driven communication").

### S8 · The room is not assigned yet — *ruled*
**SITUATION.** The booking is for a room *type*. The room number is chosen the
night before, or at the desk while the guest is standing there, or changed for
an upgrade at that moment.

**STANDALONE and PMS-CONNECTED.** Both, and it is the ordinary case rather
than the exception: Oracle's own reject vocabulary contains `BLANK ROOM NO`
(R26), so the source produces roomless stays too — **a model that refused them
would refuse the PMS.**

**EXPRESSIBLE.** Ruled as a **GUEST-Q2 addendum, 2026-08-31**: the anchor's
*"one room"* is precisely one room **type**, and the room number is an
**assignment** on the stay. So:

```text
booked          stay exists · room type · dates · party · no assignment
assigned        a room number, chosen the night before or at the desk
                — and changeable, right through the stay (S14)
checked in      requires an assignment · a person cannot be in no room
```

Three consequences the design inherits. A stay is **valid and workable with no
room**, so the arrivals list must be usable when half of it is unassigned.
**Assignment is its own operation** with its own audit — an upgrade at the desk
is an assignment, not an amendment (S28). And **check-in refuses an unassigned
stay**, which is the one hard gate this scenario creates.

### S9 · The room is not ready — *ruled*
**SITUATION.** The guest is at the desk at 11:40 and the room is dirty.

**EXPRESSIBLE. GuestOps never refuses.** Owner, 2026-08-31, answering §15 (e)
with a rule that reaches further than this scenario:

> **An application's own flow is never gated on another application being
> installed. An absent dependency loses its *capability*, never the *flow*.**
> *"If Jobs is not installed we cannot create a job. If there is no Room Care,
> the cleaning process cannot be tracked. Check-in and check-out are GuestOps's
> responsibility."*

So the check-in proceeds and is recorded. Where Room Care is present and the
resolver exists (§B4 of `03-the-open-questions.md`), the desk may be **shown**
that the room is not released — a display, never a gate. Where Room Care is
absent, the question simply has no answer and nothing about the check-in
changes.

**The distinction this draws, and it is the one to keep:**

```text
GuestOps gates on its OWN facts        check-in refuses an unassigned
                                       stay — S8's one hard gate
GuestOps never gates on ANOTHER
application's facts                    room readiness · open jobs ·
                                       cleaning state
```

A cross-application gate would make an *installable* application effectively
mandatory, which is the opposite of what a modular platform is for.

### S10 · The early arrival, across the business-day boundary
**SITUATION.** A guest booked for the 15th walks in at 02:30 on the 15th. The
property's business day rolls at 04:00, so the current business date is the
**14th**.

**EXPRESSIBLE.** The day the desk works to is the property's business day
(ADR 0128 §6 — `operating_day(timestamp, boundary)`, derived and never
stored). The arrival must record **when it actually happened** and **which
business day it belongs to**, and those are two facts. A stay arriving before
the boundary belongs to the *previous* day's arrivals, which is what the night
auditor expects and what a calendar-date list gets wrong every night.

### S11 · The check-in that arrives in two pieces *(R6)*
**SITUATION.** The PMS tells us a guest checked in. It does not yet say into
which room.

**PMS-CONNECTED.** On Oracle's on-premise flavour `"Checked In"` and
`"CHECKED IN"` are two feeds carrying **different fields** — one supplies
phone, email and departure date, the other supplies the room number — and the
check-in is complete only when both have arrived (R6).

**EXPRESSIBLE.** A stay must be able to be **partially known** and still be
displayed honestly: *"checked in, room not yet reported"*. The desk sees the
gap rather than a blank that looks like data.

**And the check-in gate of S8 does not apply here** — ratified with GUEST-Q3,
2026-08-31. That gate is on the
*operation staff perform* — GuestOps will not let a receptionist check a guest
into no room. An inbound PMS fact is not that operation: it is a report of
something that already happened elsewhere, and refusing it would drop a real
stay for failing a rule of ours (R25's first failure). So an arrived-but-unassigned
stay is **accepted, marked incomplete, and shown on the Attention list** until
the second message supplies the room.

### S12 · The stay whose first news is its departure *(R7)*
**SITUATION.** The first fact GuestOps ever receives about a stay is its
check-out. Nothing preceded it.

**PMS-CONNECTED.** The reference met this and answered it **three times**
without removing any answer — per-flavour force flags, direct fallbacks marked
*"Todo remove after data flow is ok"*, and a commented-out replay that would
have injected a check-in that never happened (R7).

**EXPRESSIBLE.** The state of a stay whose antecedent was never received must
be defined, and defined **once** — R7's requirement, and the brief assigns it
to the design (deliverable 2: *"the stay state machine with R7's one rule"*).
This page records only the constraint the answer must meet: **an arrival that
was never observed is never invented** (R25). Not `OPEN`; assigned.

### S13 · The walk-in
**SITUATION.** Someone with no booking asks for a room tonight.

**STANDALONE.** One action: create the stay and check it in, flagged as a
walk-in — the hotelier reference carries exactly such a flag (brief §3.1).

**PMS-CONNECTED.** The desk normally raises it in the PMS and it arrives
through the Hub. **It may also be raised here** (S5, GUEST-Q5) — marked
PMS-unknown, joined later by a staff-confirmed link if the PMS sends its own
version. This is the walk-in at 11:00 during S36's outage: the PMS is
unreachable, and refusing the stay would leave a room physically occupied
while Room Care plans to clean it as vacant and Context answers *room → no
stay* for a guest asleep in it.

**EXPRESSIBLE.** A stay whose booking and arrival are the same moment, marked
as such — the walk-in ratio is a number every hotel reports on, and it is
unrecoverable if the flag is not recorded when the stay is created. The
walk-in flag and the PMS-unknown mark are **two different facts**: one is how
the guest arrived, the other is who knows about them.

---

## 6 · In house

### S14 · The guest moves room *(R8)*
**SITUATION.** The air conditioning fails on the second night and the guest is
moved to another room.

**STANDALONE and PMS-CONNECTED.** Both. In PMS-connected mode the move is
usually a PMS fact; a move the desk makes at 01:00 and enters into the PMS
afterwards is an override until the PMS catches up.

**EXPRESSIBLE.** A room move **is an assignment change** (GUEST-Q2 addendum,
S8) — and it is **its own fact**, never an update to the stay (R8). The two
statements do not compete: the assignment is the thing that changes, and the
change is announced as a move rather than folded into an amendment. The
register records this as ruled, with `stay.room_changed` named
and its consumers listed (brief §2.4): Room Care (both rooms' axes flip),
GuestOps's own folio and registration, Jobs/EngineeringOps (open work on
either room), Guest360 (history). Folding the move into an update publishes
something whose consumers **cannot tell whether a room was vacated** — which
is what the reference did when it commented out the branch that distinguished
them.

### S15 · The stay is extended
**SITUATION.** The guest asks for one more night.

**EXPRESSIBLE.** Two outcomes, and the second is the one that gets forgotten:
either the room is free tomorrow and the stay's departure moves, or **the room
is already sold to somebody else** and the extension needs a move (S14) or a
refusal. *"When is this room next sold"* is a forward-looking fact that cannot
be derived from any current status (R3).

**This is S1b's conflict check, in its second use** — extending a stay is
asking whether the room is free for one more night, which is the same question
the desk asked before assigning it. One computation, two places, and the same
answer: it warns and names the other stay rather than refusing.

### S16 · The stay is shortened
**SITUATION.** The guest leaves a day early.

**EXPRESSIBLE.** The departure date moves; the room becomes sellable a day
sooner; Room Care learns the room's departure day changed. In PMS-connected
mode a shortened stay usually arrives as a plain update, and the **fetched
state** — never the source's event type — determines what GuestOps records
(R21: Oracle emits `UPDATE RESERVATION` for a check-in, a check-out and an
edit alike).

### S17 · The party changes
**SITUATION.** The colleague whose name was *"to follow"* arrives; a second
guest joins a room; the person who booked is not the person sleeping there.

**EXPRESSIBLE.** A stay's party is a **set**, not a field. It may be empty, it
may have several members, and it may have **nobody marked primary** — the
source produces all three (R11), and the reference hard-failed on two of them.

### S18 · The guest asks for something
**SITUATION.** An extra pillow, a late checkout, a taxi at six, a doctor.

**EXPRESSIBLE.** A request is attached to a **stay**, is visible to the desk
while the guest is in house, and is what the desk hands to Jobs — *"what needs
doing"* is Jobs' domain (APPS-Q1), so a request that becomes work becomes a
Jobs job. Whether GuestOps raises that job itself or only records the request
is design work; the boundary is not. **GuestOps does not own work execution.**

### S19 · A note about the guest, and a preference
**SITUATION.** *"Do not disturb before 10"*; *"high floor, away from the
lift"*; *"complained about noise last stay"*.

**EXPRESSIBLE.** The distinction is not decorative: a **note** is about this
stay and dies with it; a **preference** is about the person and should be true
next time. The person is Guest360's (G360-Q1). GuestOps holds the stay-scoped
note; the durable preference belongs with the guest identity record and is
surfaced by whatever owns the person-graph.

### S19b · The guest is from outside, and somebody has to be told
**SITUATION.** A guest checks in on a passport that is not of the property's
own country. The property has an obligation to report the stay to an
authority — and a property elsewhere, or one whose jurisdiction does not ask,
has no such obligation at all.

**"From outside" is never a fixed meaning.** The property sets its **home
country**, and a guest is from outside when their nationality is not it. The
same build serves a hotel in Kochi treating an Emirati guest this way and a
hotel in Dubai treating an Indian guest this way — **no country is written
into the product**, and neither is any authority, deadline or ID list.

**STANDALONE and PMS-CONNECTED.** Both. The obligation follows the property
and the guest, never the PMS.

**EXPRESSIBLE.** Ruled by the owner, 2026-08-31: **a setup screen, and the
property uses it if it needs it.** So three things must be representable, and
a fourth must not be assumed:

```text
the policy        per property: is reporting required, for whom
                  (guests from outside the home country · everyone),
                  and by when
the flag          this stay needs reporting, and has not been
the record        it was filed — when, by whom, with what reference
NOT assumed       that HotelOS submits it. See below
```

**Submitting to an external authority is an integration**, and the
constitution routes every integration through the Integration Hub as a
connector — never hardcoded into an application. So v1 records the filing that
a person performed; an automatic submission is a connector capability with an
owner, a credential and a round of its own, and this scenario does not invent
one (`03-the-open-questions.md` §B6).

**And the flag is a to-do, never a gate.** A guest whose filing is outstanding
is still checked in, still served, still checked out — S9's rule, applied to
our own obligation rather than a neighbour's: the platform tells the desk what
is owed and stops nothing.

### S20 · Day use
**SITUATION.** A room sold from 10:00 to 18:00 on the same day. Airport hotels
sell a large share of their inventory this way.

**EXPRESSIBLE.** A stay whose arrival and departure fall on **one business
day**, which must therefore not be filtered out by any list built on
*"departure after arrival"*. The room is then sold again the same night —
R2's fact: a room carries more than one stay relationship on one day.

---

## 7 · The departure

### S21 · An ordinary check-out
**SITUATION.** The guest settles and leaves at 08:40.

**STANDALONE.** Staff check the stay out.

**PMS-CONNECTED.** The check-out arrives as a fact.

**EXPRESSIBLE.** The stay ends; Room Care learns the room is vacated and
**decides for itself** whether that becomes a cleaning task — *"a checked-out
room becoming a task is a hotel policy, never an automatic consequence"*
(APPS-Q1, owner). GuestOps announces the departure and asserts nothing about
cleaning.

### S22 · Departure is expected, not observed *(R13)*
**SITUATION.** The board says the guest departed at 11:00. Nobody saw them
leave at 11:00; 11:00 is the property's check-out time.

**EXPRESSIBLE.** **Expected and actual are different facts and must not share
a field** (R13, R12). A source that gives a date, plus a property's configured
clock time, produces a *computed* timestamp; a stay checked out at the desk
produces an *observed* one. An arrival-time report built from expected times
measures the reservation, not the guest, and the two differ by hours.

### S23 · Half the group leaves
**SITUATION.** Three rooms; two depart on Thursday and one stays until
Saturday.

**EXPRESSIBLE.** **Departure is per stay** (GUEST-Q2: every operation happens
to a room-stay). The group page shows two gone and one in house; there is no
such thing as checking out a group.

### S24 · The stay is checked out and must be reopened
**SITUATION.** The guest was checked out in error at 07:00 and is still asleep
in the room.

**EXPRESSIBLE.** The correction is a **recorded correction**, never a
rewriting of history — the room was announced vacated and consumers acted on
it. In PMS-connected mode this is an override in the strictest sense: the PMS
says departed and the desk says otherwise. Whether the correction is available
to every user or to a supervisor is a permission question for the design's
two-mode split.

---

## 8 · Cancellation, no-show, and amendment

### S25 · Cancelled before arrival
**SITUATION.** The guest cancels two days out.

**EXPRESSIBLE.** A cancelled stay is not a deleted stay — the cancellation
time matters (R14 records that the source derives it from the last-modified
timestamp), the penalty is **computable from the terms v1 carries** and
**charged nowhere in v1** (GUEST-Q6: the folio is Finance's), and the room
returns to inventory. Cancellation is a **business fact about the stay**, not the
platform's lifecycle vocabulary: ADR 0062's `active` / `deleted_at` says
whether a record exists, and a cancelled reservation exists.

### S26 · Cancelled **after** check-in
**SITUATION.** A cancellation arrives from the PMS for a stay GuestOps has in
house.

**PMS-CONNECTED.** Not hypothetical: statuses arrive out of order (R7's
family), and a night audit lagging produces exactly this shape.

**EXPRESSIBLE.** The application must be able to hold *"the PMS says
cancelled, the room is occupied"* as a **stated contradiction**, with the
guest still served, rather than picking one side and discarding the other.

**GUEST-Q3 does not decide this one, and is not stretched to.** Its precedence
rule is override-versus-PMS, and where the desk performed the check-in this
*is* an override (S33) and GUEST-Q3 governs. But where the check-in itself
arrived from the PMS and the same PMS later cancels it, there is no override
and no second party: it is one source contradicting itself, which is R7's
out-of-order family. **That belongs to the design's one rule for out-of-order
facts** (S12), not to a precedence question. Recorded here so the design does
not reach for GUEST-Q3 and find it does not fit.

### S27 · No-show
**SITUATION.** Nobody arrived. At night audit the stay is marked a no-show.

**STANDALONE.** Staff mark it, or the property's own end-of-day does.

**PMS-CONNECTED.** It arrives as a fact.

**EXPRESSIBLE.** A no-show is a **business event**, not a lifecycle verb
(ADR 0062, whose worked example is `RecordStaffExit`). It is chargeable and it
is reportable, and it must be distinguishable from a cancellation for both
reasons.

### S28 · An amendment that is not a room change *(R8)*
**SITUATION.** A misspelled surname; a corrected phone number; a rate change;
the arrival moved by a day.

**EXPRESSIBLE.** An update to the stay — and consumers that react to a **room
change** must not be woken by it. R8 is the requirement; the reference had
four verbs downstream and the branch distinguishing change from update was
commented out.

**And the case that sits exactly on the line: the upgrade.** Ruled by the
planner, 2026-08-31 (GUEST-Q8): a better room with the **terms unchanged** is
an **assignment**, not an amendment — it becomes an amendment only when the
booked type or the terms themselves change.

```text
free upgrade to a Suite        an assignment. The sale stands as booked
the guest BUYS the Suite       an amendment. The booked type moves
```

The test is *what changed*, not *what the guest got*. The rate, the group's
expected room types and every availability calculation read the **booked**
type, so treating a courtesy upgrade as an amendment would quietly rewrite
what was sold.

---

## 9 · The group page

### S29 · A complete group at one property
Three stays, one group, all known. The page exists so that *"where is the rest
of my party"* has an answer; every action on it is still an action on one
stay.

### S30 · An incomplete group, drawn honestly *(R9)*
**SITUATION.** `noOfRooms: 3`, one stay known.

**EXPRESSIBLE.** The page states **"1 of 3 rooms known"** in words. The two
unknown rooms are not drawn as rows, not drawn as placeholders, and not
counted in occupancy. The brief requires the mockup to draw this case (§3.3) —
a group page that can only display complete groups is a group page that will
be wrong on its first Oracle property.

### S31 · Partial check-in
**SITUATION.** Two of the three guests have arrived.

**EXPRESSIBLE.** A group has no single arrival state. The page shows per-stay
status; any summary is a count, never a status.

### S32 · A group whose other stays are at other properties *(S4)*
**EXPRESSIBLE.** *"This booking continues elsewhere"*, from the group
identifier alone, with nothing invented about the other legs and no
cross-installation query (GUEST-Q2's edge-first rider).

---

## 10 · Override and disagreement — first-class states

These four are the heart of PMS-connected mode, and are the scenarios most
likely to be designed away by accident.

### S33 · A plain override
**SITUATION.** The PMS says the guest is due in. They are standing at the desk
and the PMS is not reachable. The receptionist checks them in **here**.

**EXPRESSIBLE.** The stay records the override, **who** made it, **when**, and
**what the PMS said at that moment** (GUEST-Q1, amended). The desk can see
that this stay is not what the PMS believes, and that does not disappear
because a screen was refreshed.

### S34 · The PMS catches up and agrees
**SITUATION.** An hour later the PMS sends the check-in.

**EXPRESSIBLE.** The override is **settled as confirmed, silently** —
GUEST-Q4 (2): the confirmation is recorded (*PMS confirmed, 15:30*) and
surfaces nothing to the desk, because agreement arriving late is not work.
That there *was* an override survives — it is how anybody later explains a
check-in time that differs by an hour (S22's expected-versus-actual, again).

The rule this fixes for the whole application: **only differing values are a
disagreement.** A design that flagged every late-arriving confirmation would
bury the two real ones in twenty.

### S35 · The PMS catches up and disagrees — *ruled*
**SITUATION.** The desk checked the guest into 214. The PMS later says the
guest is in 208.

**EXPRESSIBLE.** GUEST-Q1 ruled this a **recorded disagreement, never a silent
overwrite** — what it keeps. **GUEST-Q3 (2026-08-31) rules what it says**, in
three parts:

```text
1  while the disagreement stands, the standing OVERRIDE is the answer
   — on the board, to every application, and through Context.
   One truth leaves the application; the disagreement is a FLAG on it,
   never a second answer
2  clearing belongs to the stay's WRITE permission — the same permission
   that makes an override — choosing "keep ours" or "take the PMS's",
   recorded (who · when · which side), both values kept in history
3  clearing to the PMS's side emits the same correction event a room move
   does, so Room Care re-plans from the event stream as always
```

The reasoning is recorded with the ruling and is worth keeping, because it is
the rule that decides every future case of its shape: **a recorded override is
a person looking at the guest; the inbound fact is automation, possibly
stale** — and if the PMS silently won, GUEST-Q1's *"staff can override"* would
be a suggestion. Author-only clearing fails across shifts; supervisor-only
escalates a routine reconciliation.

**What this settles for the consumers.** Room Care is never told the guest is
in two rooms — it hears 214, and hears a correction if the desk later takes
the PMS's side. Context resolves *guest → reservation → room* to 214. The
receptionist sees 214 with a disagreement mark, and clearing it is a two-value
choice, not free text.

### S36 · The connector is down for six hours — *ruled*
**SITUATION.** The PMS feed stops at 09:00. Nobody notices until the arrivals
list looks thin at 14:00 — a connector can be authenticated, polling and green
while **check-ins specifically** have stopped arriving (R27).

**EXPRESSIBLE.** **GUEST-Q4 (2026-08-31) removes the fork rather than choosing
a branch: there is no second mode.**

```text
1  PMS-writes-first AT ALL TIMES. An override means one thing in every
   condition, and the screen always says the same true thing:
   your action stands
2  an inbound fact that MATCHES a standing override settles it silently
   as CONFIRMED — recorded, surfacing nothing. Only differing values
   are a disagreement
3  the outage is visible as per-capability STALENESS, not as a mode:
   "PMS feed silent since 09:00 — your entries stand"
4  the backlog lands in event order; matches confirm, differences flag
   per GUEST-Q3, and Attention groups them as ONE OUTAGE BATCH
```

Why no mode switch, recorded because it is the reasoning a later reader will
want: a switch keyed on a signal **R27 proved unreliable** would flip the
desk's meaning mid-shift on a false trigger, and a person-declared switch needs
exactly the noticing this scenario shows did not happen.

**And this is what keeps the mechanism from being decorative.** A disagreement
is *values that differ*. The desk put the guest in 214 and Opera later says
214 — that is agreement arriving late, not work. The twenty reconciliations
this scenario feared become the two that are real.

*(The walk-in at 11:00 with the PMS unreachable is answered by **GUEST-Q5**:
the desk creates it here, marked PMS-unknown, and a staff-confirmed link joins
it to the PMS's version if one ever arrives — S5, S13.)*

---

## 11 · The guest at the desk

### S37 · The booking's email is a placeholder
**SITUATION.** The booking carries `noreply@…` or a channel-generated address,
and a phone number that belongs to the travel agent.

**EXPRESSIBLE.** *"At booking time it is often a dummy email or phone"* —
G360-Q1, the owner's founding input. **Neither phone nor email is a unique
key**, and a junk identifier must never link two people. GuestOps records what
it was given; it does not decide who the person is.

### S38 · *"Same guest, third stay"*
**SITUATION.** At check-in the screen should say this person has stayed twice
before.

**EXPRESSIBLE.** That sentence is **Guest360's answer surfaced in GuestOps's
screen** (G360-Q1; brief §2.3). GuestOps owns stays and guest identity
records; the person-graph, the suggested links and the merges are Guest360's,
and a merge **re-points the person and rewrites no stay**. There is no merge
logic in this application, and when Guest360 is not installed the screen
simply does not make the claim.

### S39 · Two records that are probably one person
**SITUATION.** Phone-only last March, email-only this August.

**EXPRESSIBLE.** They are **two guests** until somebody says otherwise
(G360-Q1: *"we treat them as separate guests — but staff must be able to merge
them"*). What GuestOps must not do is guess; what it must not prevent is the
later merge — so a stay's link to a person has to survive that person being
re-pointed.

---

## 12 · Every other guest operation — what GUEST-Q1 left to this round

GUEST-Q1 ruled that in PMS-connected mode staff perform *"every other guest
operation"* here, and said the round would establish what those are. From
§4–§11, they are:

```text
registration       the registration card and its number (the hotelier
                   reference's grcNo), identity documents, signature
the party          who is actually in the room                        S17
requests           what the guest asked for, and its hand-off to Jobs  S18
notes              stay-scoped remarks                                 S19
preferences        person-scoped, surfaced from the person's owner     S19
room assignment    choosing the room, subject to §15 (b)               S8
arrival & departure detail   actual times, walk-in flag, early/late    S10, S22
the stay's own corrections   overrides and their reconciliation        §10
```

Both of those are now ruled: the folio's line is GUEST-Q6, and the
registration card's contents are §15 (g) — **the design proposes the field
list and the property configures which of them are required** (owner,
2026-08-31: *"card we can go with your idea"*), with guest reporting as a
**setup screen a property uses if it needs it** (S19b).

---

## 13 · What PMS-connected mode cannot do, and must say so

Recorded here because it is what a screen will be tempted to imply:

* **No write reaches the PMS.** Not a check-in, not a room assignment, not a
  guest detail (`CONN-Q5`, ADR 0128 §4).
* **A notification is not a record** (R20). A change may be known to have
  happened with its content not yet retrieved — and the fetch can fail
  outright, which must also be representable.
* **A fact may be unmappable** — the PMS knows a room Master Data has not been
  told about (ADR 0128's four outcomes; connector brief §14). That is an
  operator's work queue, and such a stay cannot be shown as if it were in a
  known room.
* **Facts held before this application existed replay into it.** The Hub
  normalises reservation and guest facts today and holds them **deferred**,
  with their business date and provenance, and they replay the day this domain
  ships (connector brief §12–§13). GuestOps's first day is therefore not an
  empty book, and replay is idempotent by construction — the design must not
  assume a clean start.

---

## 14 · Known unknowns — recorded so silence is not read as simplicity

* **No reference backend exists for this application** (owner, 2026-08-31).
  Everything above comes from the PMS study, the chapters and the owner's
  scenarios. Where Workforce could check a design against a working system,
  this round cannot.
* **What OPERA emits for a *new* reservation is not established** — the study
  found `UPDATE RESERVATION` as the only action type anywhere in the reference
  (chapter 02 §9). So *"the booking arrives"* is better evidenced for the
  on-site flavours than for the cloud one.
* **Online check-in was never implemented** in the reference, so what a guest
  supplies before arriving, and how it reaches a PMS, is unknown here.
* **Nothing establishes how a PMS orders concurrent changes** to one
  reservation. The Event Store's `entity_version` is our side of it; the
  source's side is a vendor-documentation question.
* **The group identifier's shape across vendors is not surveyed.** GUEST-Q2
  rules that it is carried from day one; whether every source supplies one is
  a question for the connector's mapping, not for this page.

---

## 15 · What only the owner can answer

Listed in the order they will be asked, **one at a time in chat**. No numbers
are claimed here — `GUEST-Q3…` are claimed in the platform register by the
architect before use (brief §3.4) — and no scenario above is resolved by
anticipating an answer.

**All seven are ruled**, and are struck through below rather than deleted so
the scenarios that cite them still resolve. What remains open in this round is
no longer an owner question: it is the platform's own record — four findings
and twelve scope items in `03-the-open-questions.md`, for the planner and the
architect.

| | Subject | Why it cannot be settled here |
|---|---|---|
| ~~**(a)**~~ | ~~May staff create a stay the PMS has never seen?~~ (S5, S13) | **RULED — GUEST-Q5, 2026-08-31: yes** — GUEST-Q1's *"all guest operations are done here by staff"* already granted it. The stay carries **PMS-unknown**, a permanent valid state rather than a pending one. The join to a later PMS version is a **staff-confirmed link, never an automatic match**: candidates are *same room + overlapping dates*, name similarity may rank and may never link, and an unconfirmed candidate sits on Attention |
| ~~**(b)**~~ | ~~Is a room-stay valid **without a room**?~~ (S8) | **RULED — GUEST-Q2 addendum, 2026-08-31: yes.** The anchor's *"one room"* is one room **type**; the room number is an **assignment**, absent at booking, changeable through the stay, and **required at check-in**. Carried in §1's vocabulary and S8; the letter is kept rather than re-lettered so every citation above still resolves |
| ~~**(c)**~~ | ~~On a standing disagreement, which value do the board, Room Care and Context see — and who clears it?~~ (S35) | **RULED — GUEST-Q3, 2026-08-31.** The standing **override** is the answer everywhere while the disagreement stands; the disagreement is a flag on the one truth, never a second answer. Clearing belongs to the stay's **write permission**, choosing *keep ours* or *take the PMS's*, recorded with both values kept. Clearing to the PMS's side emits the same correction event a room move does. The S11 gate note is ratified in the same row |
| ~~**(d)**~~ | ~~When the connector is down, is the property still PMS-writes-first, or its own book until the feed returns?~~ (S36) | **RULED — GUEST-Q4, 2026-08-31: there is no second mode.** PMS-writes-first at all times; a matching inbound fact settles an override **silently as confirmed**, so only differing values are a disagreement; the outage shows as per-capability **staleness**, not as a mode; the backlog lands in event order and groups as one outage batch. The S26 boundary is ratified in the same row |
| ~~**(e)**~~ | ~~Does GuestOps refuse a check-in into a room Room Care has not released?~~ (S9) | **RULED — owner, 2026-08-31: no, and the rule is platform-wide.** *An application's own flow is never gated on another application being installed; an absent dependency loses its capability, never the flow.* Check-in and check-out are GuestOps's responsibility. Room readiness may be **displayed** when Room Care is present and the resolver exists — never a gate. The number is the architect's to claim |
| ~~**(f)**~~ | ~~Is the folio — charges, deposits, guarantee and cancellation terms — in GuestOps v1?~~ (S6, S25) | **RULED — GUEST-Q6, 2026-08-31:** v1 is **the book plus the stay's commercial terms** (rate, guarantee and cancellation offsets, every amount with currency and tax basis). The **folio** — posting, payments, settlement, invoicing, night-audit posting — is **Finance's domain, a later round**. Accepted knowingly: a standalone property cannot settle a guest in v1, and the first deployments are PMS-connected. The walk-in / PMS-unknown distinction is ratified in the same row |
| ~~**(g)**~~ | ~~What must the registration card capture, and is there a statutory report behind it?~~ (§12, S19b) | **RULED — owner, 2026-08-31.** The card: *"we can go with your idea"* — the design proposes the field list and **the property configures which fields are required**, for domestic and foreign guests separately. The report: **a setup screen, used by properties that need it** — the policy, the flag and the record of a filing are GuestOps's; **the submission to an authority is an integration** and therefore a connector, not built here (§B6 of `03-the-open-questions.md`) |

---

## 16 · What this page does not contain

* **No model.** No fields, no types, no schema, no proto, no state names, no
  event subjects. That is `02-the-guestops-design.md`.
* **No merge logic.** The person-graph is Guest360's round (G360-Q1).
* **No answer to an open question.** All seven §15 subjects were ruled on
  2026-08-31 and are carried in §1, §3, §12, S5, S6, S8, S9, S11, S13, S19b,
  S25, S34, S35 and S36. S26 records where GUEST-Q3 deliberately does **not**
  reach, and S19b records what a filing obligation is **not** allowed to
  become — a gate.
* **No screens.** The gold mockup and the flows are deliverable 3, and they
  are drawn from *this* page's scenarios — a frame that draws a capability no
  scenario here describes is a finding, not a plan.
* **Nothing copied.** Every PMS fact is cited to `R<n>` in the requirements
  page beside `pms-oracle/`, which cites the read-only reference outside both
  repositories.

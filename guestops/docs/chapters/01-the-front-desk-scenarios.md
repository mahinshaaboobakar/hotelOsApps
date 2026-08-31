# 01 · The front desk scenarios — what GuestOps must be able to do

**Status:** scenario record, 2026-08-31 — accepted by the owner, then amended
the same day to carry two rulings: the **GUEST-Q2 addendum** (a stay's anchor
is the room *type*; the room number is an assignment required at check-in) and
**GUEST-Q3** (while a disagreement stands, the standing override is the one
answer that leaves the application). Stream FF,
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
             a PMS fact we could not apply, stays the PMS has never seen
```

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

### S5 · A booking created here while a PMS is connected — **OPEN**
**SITUATION.** The PMS is the property's book, and the desk creates a
reservation in GuestOps anyway — a walk-in at 23:00, a phone booking while
the PMS is being upgraded, a manager who prefers our screen.

**PMS-CONNECTED.** GUEST-Q1's amendment lets staff **override** a stay. A
booking the PMS has never heard of is not an override of anything: it is a
stay that exists only here, that write-back cannot deliver (`CONN-Q5`), and
that the PMS's night audit will never reconcile.

**EXPRESSIBLE.** If it is allowed, a stay must be able to say *"the PMS does
not know about me"*, and the day a matching PMS fact arrives it must be
possible to recognise it as the same stay rather than a second one.
**`OPEN` — §15 (a).**

### S6 · A booking with commercial terms attached *(R18, R19)*
**SITUATION.** The booking is guaranteed by a card, has a deposit due seven
days after booking, and a cancellation penalty of one night if cancelled
within 48 hours of arrival.

**PMS-CONNECTED.** Oracle carries all of this structurally: codes, flags, a
deposit deadline as an **offset from the booking date**, a cancellation
deadline as an **offset from arrival** plus a drop time, and an amount with a
basis, a number of nights and a currency (R18).

**EXPRESSIBLE.** If GuestOps carries these at all, it carries the **offsets**,
never resolved timestamps — an offset survives the arrival date changing and a
resolved deadline does not, and a cancellation deadline that silently stops
matching its reservation is a chargeable error (R18). And an amount carries
**three** things or it is not an amount: the value, its currency, and whether
tax is included (R19). Whether v1 carries money at all is
**`OPEN` — §15 (f)**.

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

### S9 · The room is not ready — **OPEN**
**SITUATION.** The guest is at the desk at 11:40 and the room is dirty.

**EXPRESSIBLE.** Room Care's cleaning is **policy-driven, not event-driven**
(APPS-Q1, owner, 2026-08-31) and Room Care is an *installable* application
that may not be installed at all. So GuestOps cannot assume it can ask, and
must not assume the answer would be binding. Whether a check-in into an
unreleased room is refused, warned, or simply recorded is a hotel policy.
**`OPEN` — §15 (e).**

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
through the Hub. Raising it in GuestOps instead is **S5**.

**EXPRESSIBLE.** A stay whose booking and arrival are the same moment, marked
as such — the walk-in ratio is a number every hotel reports on, and it is
unrecoverable if the flag is not recorded when the stay is created.

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
be derived from any current status (R3). In standalone mode GuestOps holds
that answer itself, because it is the book.

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
timestamp), the penalty may be chargeable (S6), and the room returns to
inventory. Cancellation is a **business fact about the stay**, not the
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

**EXPRESSIBLE.** The override is **settled**: the two sources now agree and
the stay is ordinary again. That there *was* an override survives — it is how
anybody later explains a check-in time that differs by an hour (S22's
expected-versus-actual, again).

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

### S36 · The connector is down for six hours — **OPEN**
**SITUATION.** The PMS feed stops at 09:00. Nobody notices until the arrivals
list looks thin at 14:00 — a connector can be authenticated, polling and green
while **check-ins specifically** have stopped arriving (R27).

**EXPRESSIBLE.** Every desk action during the outage is either an override
against a stale picture, or the property is temporarily its own book. The
choice changes what reconciliation means when the feed returns, and it changes
what the desk is *told* — the mode is not a badge, it is the difference
between *"you are correcting the PMS"* and *"you are the record"*.
**`OPEN` — §15 (d).**

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

Two of these cannot be scoped without the owner: what a registration card must
capture (**§15 (g)**), and whether the folio and money are in v1 at all
(**§15 (f)**).

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

**Two are ruled** and are struck through below rather than deleted, so the
scenarios that cite them still resolve. **Five remain open**, and three of
them — (a), (d), (f) — are load-bearing for the design: they decide the
schema's shape and the write paths. (e) and (g) are carried as property
configuration and as a records list, and block nothing.

| | Subject | Why it cannot be settled here |
|---|---|---|
| **(a)** | May staff create a stay the PMS has never seen? (S5, S13) | Write-back is out of scope, so such a stay never reaches the PMS and its night audit never reconciles it. GUEST-Q1 permits *overrides* of PMS-managed stays; creating one is not an override of anything |
| ~~**(b)**~~ | ~~Is a room-stay valid **without a room**?~~ (S8) | **RULED — GUEST-Q2 addendum, 2026-08-31: yes.** The anchor's *"one room"* is one room **type**; the room number is an **assignment**, absent at booking, changeable through the stay, and **required at check-in**. Carried in §1's vocabulary and S8; the letter is kept rather than re-lettered so every citation above still resolves |
| ~~**(c)**~~ | ~~On a standing disagreement, which value do the board, Room Care and Context see — and who clears it?~~ (S35) | **RULED — GUEST-Q3, 2026-08-31.** The standing **override** is the answer everywhere while the disagreement stands; the disagreement is a flag on the one truth, never a second answer. Clearing belongs to the stay's **write permission**, choosing *keep ours* or *take the PMS's*, recorded with both values kept. Clearing to the PMS's side emits the same correction event a room move does. The S11 gate note is ratified in the same row |
| **(d)** | When the connector is down, is the property still PMS-writes-first, or its own book until the feed returns? (S36) | Changes what reconciliation means afterwards, and what the desk is told at the time |
| **(e)** | Does GuestOps refuse a check-in into a room Room Care has not released? (S9) | Cleaning is policy-driven (APPS-Q1), and Room Care is installable — it may be absent entirely |
| **(f)** | Is the folio — charges, deposits, guarantee and cancellation terms — in GuestOps v1? (S6, S25) | The source carries the structure (R18, R19) and the brief names a folio as a room-move consumer, but no ruling scopes money into this application |
| **(g)** | What must the registration card capture, and is there a statutory report behind it? (§12) | The hotelier reference has a `grcNo`; what an Indian property is legally required to record, for domestic and foreign guests, is the owner's knowledge and not the record's |

---

## 16 · What this page does not contain

* **No model.** No fields, no types, no schema, no proto, no state names, no
  event subjects. That is `02-the-guestops-design.md`.
* **No merge logic.** The person-graph is Guest360's round (G360-Q1).
* **No answer to an open question.** Seven subjects are listed in §15; two —
  (b) the roomless stay and (c) the standing disagreement — were ruled on
  2026-08-31 and are carried in §1, S8, S11 and S35. The other five are not
  resolved above, and none is anticipated. S26 records where GUEST-Q3
  deliberately does **not** reach.
* **No screens.** The gold mockup and the flows are deliverable 3, and they
  are drawn from *this* page's scenarios — a frame that draws a capability no
  scenario here describes is a finding, not a plan.
* **Nothing copied.** Every PMS fact is cited to `R<n>` in the requirements
  page beside `pms-oracle/`, which cites the read-only reference outside both
  repositories.

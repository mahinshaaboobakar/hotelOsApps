# 02 · The Room Care walkthrough — for sign-off, one question at a time

> **This page is the plain-language design of Room Care, written to be ruled
> on one scenario at a time.** Chapter 01 is the engineering survey with
> file-and-line evidence; this is the same ground in the owner's language,
> arranged so each decision can be taken in a sentence and then closed.
> Nothing is built until every section is signed off and the page is locked.

---

## Status

**Phase 2 opened 2026-09-05.** Phase 1 (the survey) was ratified whole by the
architect the same day; the register row is **`RC-Q1`**, and the survey's
fourteen questions are `RC-Q1(1)–(14)` in survey order.

| | Ruled / routed | Open here |
|---|---|---|
| `RC-Q1(1)` design of record | **yes** — survey + this walkthrough + the design chapter (the `JOBS-Q1(1)` shape) | — |
| `RC-Q1(2)` page 48 vs the brief | **the brief governs**; page 48 amended to match; the owner's scenario study is this page's spine | — |
| `RC-Q1(3)` minibar | decision proposed under the owner's delegation (one Inventory application when needed — minibar items are its billable category; Room Care records the act only) — **awaiting the owner's approval** | **S1** |
| `RC-Q1(4)` amenities · linen · carts | the same decision as S1 — Inventory owns the goods, Room Care records the act — **awaiting the owner's approval** | **S2** |
| `RC-Q1(5)` public areas | | **S3** |
| `RC-Q1(6)` inspection | **owner-ruled**: checklists and inspections are a **separate application**; Room Care decides *whether* (ADR 0044's row), requests by event with a correlation id, applies the outcome to `condition`; the suite grows by one app, named at its own brief, reference `hotel-inspection-server` | — |
| `RC-Q1(7)` reconciliation policy · the PMS-only hotel | | **S4** |
| `RC-Q1(8)` CONN-Q11 | **accepted provisionally** — the scenario pass (S0) is the final check | S0 |
| `RC-Q1(9)` guest preference | | **S5** |
| `RC-Q1(10)` `events.proto:95` | **routed to CC** — not this round's | — |
| `RC-Q1(11)` diagram 42's permission name | **verify here before anything cites it** | S6 |
| `RC-Q1(12)` permissions | | **S6** |
| `RC-Q1(13)` escalation | **parked with Jobs' twin** — one answer will serve both | — |
| `RC-Q1(14)` AI allocation | | **S7** |

### The method ruling that binds this page — owner, 2026-09-05

> *"the implementation flow in java is not good… we can only take the concept
> how a room cleaning should go."* — **concept-only carry.**

So this page presents **concepts and scenarios, never the reference's
mechanisms.** Where the survey cites a reference flow, this page re-derives
the behaviour from the requirement, and the reference appears only as the
defect that motivates it — *"the last system dropped a checkout that arrived
at 16:05"*, never *"the last system's window check."* A section that
explains how the old software did something has failed this rule.

### How this page is used

```text
for each section, in the architect's order:
        one plain scenario the owner can rule on in a sentence
        the proposal, stated
        the reasoning, one paragraph
                ↓
        the owner rules · the ruling is recorded in the section, dated
                ↓
        section marked SIGNED OFF · next section

when S0–S7 are signed off:
        LOCK the page → the design chapter (03) is written from it
```

A ruling recorded here is an **owner ruling with its date**; the architect
carries it into the register before any code.

---

## S0 · The standing proposal — how a room cleaning should go

**Carried in as the spine** (architect, 2026-09-05: *"a strong spine — carry
it in as the standing proposal"*). This is the concept; each S-section below
settles one part of it. The owner's scenario study runs against this section
last, as the final check on `RC-Q1(8)`.

### The charter, verbatim — owner, 2026-08-31

> *"not every hotel follows instant cleaning; checkout marks the room dirty,
> and if no guest arrives into it today it has no priority and is cleaned
> tomorrow, or by a custom click action — there are so many special
> conditions in hotels."* A checked-out room becoming a task is **a hotel
> policy, never an automatic consequence.**

### The concept

```text
WHAT ROOM CARE HEARS
  the room's state as the PMS or the desk sees it — occupied/vacant · dirty/clean/inspected ·
     the stays touching it today · WHEN IT IS NEXT SOLD
  a stay departing or arriving
  who is posted to which zone today (Workforce, read through Context)

WHAT ROOM CARE OWNS
  the room's CONDITION — dirty · clean · inspected — set by Room Care, announced by Room Care
  the room's exceptions — do-not-disturb · service refused · sleep-out · strip linen · long stay …
  the property's CLEANING POLICY
  the room TASK and its phases, the attendant's time on it, its history

THE POLICY, per property, per service
  services: morning service · turn-down · night — each a time range (may cross midnight)
  cleaning kinds: the property's own list (touch-up · light · full · deep · long-stay · skip …)
  per room type × kind: minutes · credits · inspection required? · which checklist
  which kinds run on which weekdays · rooms with a fixed kind · the guest's own preference
  priority: SOLD TONIGHT first, then due-out, then stay-over — configurable

THE DECISION — one function, its inputs and its answer always recorded
  room state + exceptions + operating day + service  →  a task (kind · phases · priority · minutes)
                                                     or  PENDING — visible, "tomorrow" or one click
  nothing is ever dropped; a room outside every rule is a room the supervisor can see

THE TASK — one per room per operating day per service
  phases in order: [pre-clean] → clean → [post-clean] → inspection (requested from the inspection app)
  one state per phase, one declared set of moves; the attendant's timer accumulates across pauses
  exceptions are outcomes: refused / DND / sleep-out end the task honestly, with the reason
  three DND days in a row → an event; Jobs raises the security job on it

ASSIGNMENT
  candidates = attendants posted to the room's zone today · capacity from Workforce
  strategies a property picks: continuity · same sector · lowest load
  "nobody available" is an outcome the supervisor sees, never silence · manual override always

THE DAY
  rolls on the property's operating day (night audit), never on a clock elsewhere
  each service closes when the property says it does; what closes is recorded as closed-by-policy
  quick-clean / overdue checks and service triggers are durable schedules

WHAT ROOM CARE SAYS
  room.cleaned · the inspected / dirty-again result · exception events · inspection requested
  the room's condition to Context, for every other application to read

THE TWO SCREENS
  attendant: my rooms · start · pause · end · exception · photo · ask for extra time
  supervisor: the board by state · the pending queue · assign · alerts · policy setup
```

### What the reference proved, by failing

Four defects from chapter 01 that S0 is shaped against — named as defects,
not as mechanisms: the last system **never marked a room clean itself** and
waited for the PMS (F1); it **discarded** a checkout that arrived outside its
cleaning hours (F2); it **ended every hotel's day at one UTC instant** (F3);
and it **could not say "nobody was available"** — an unassignable room looked
identical to one nobody had looked at (F15).

**Open for the owner in S0:** nothing yet — the scenario pass comes after
S1–S7, and CONN-Q11's provisional acceptance stands until then.

---

## S1 · The minibar — `RC-Q1(3)`

### The scenario

An attendant doing the full clean of 214 finds two waters and a chocolate
missing from the minibar, refills them from the cart, and moves on. The guest
in 214 must be charged; the floor's minibar stock is down by three; the room
must be at par for tonight's arrival if 214 turns over.

### The proposal

**Room Care carries no minibar in v1** — no item list, no prices, no stock, no
charge. What it may carry, later and only as an event, is the attendant's act:
*"during the clean of 214, refilled 2 × water, 1 × chocolate"* — quantities,
no price, no guest — for Inventory and the folio owner to consume. Until
Inventory exists, the minibar is recorded where the hotel records it today.

### The reasoning

The refill is genuinely something the attendant does during service, which
is why the old system put it in housekeeping — and then had to invent a price,
a guest reference and a stock figure inside the housekeeping database, where
none of them had an owner. The charge belongs to the guest's folio, the stock
to Inventory, the catalogue and prices to Procurement; Room Care would be
copying three other domains' facts to hold one act. The platform's rule is
that an application owns what is *happening* to a room, and a bill is not
happening to the room. The old system recorded a **priced consumption that
was never posted anywhere** (survey F32) — a charge only housekeeping knew
about — which is what carrying the whole thing here produces. An event with
quantities keeps the attendant's act honest and gives the owners something to
consume; nothing else is lost by leaving it out.

### How the discussion went — 2026-09-05

The owner asked where the minibar goes if there is no Inventory application
— there is none in `APPS-Q1`'s suite, and this page should not have leaned
on one. Taken apart, a refill is three facts with three owners: the
attendant's act (service — Room Care), the guest's charge (a fact about the
stay — GuestOps), and the stock (nobody's, in v1). The owner then delegated
the architectural call — *"what way more secure and correct, more
flexible… there is no app like inspection in the suite, but we add it if
needed"* — and it is taken on the inspection precedent.

### Decision — architectural, under the owner's delegation, 2026-09-05 (revised the same day)

**One Inventory application, added to the suite when briefed, owns every
good the property stocks — minibar items are its billable category; Room
Care records only the act.** First written as "a Minibar application"; the
owner asked whether minibar and inventory are really different, and they are
not — revised to one application, which S2 had already reached.

```text
Inventory app   every good the property stocks and issues — towels, toiletries, tea, chemicals,
                and minibar items (the category flagged BILLABLE ON CONSUMPTION)
                owns: catalogue · prices · par per room type · pantry stock · the attendant's cart
                (loaded / returned) · counts to and from laundry
                consumes Room Care's acts; billable item → inventory.charge_due (stay · lines)
Room Care       the act on the task: items (from Inventory's list, via Context) · quantities · who ·
                when  →  roomcare.room.restocked   (no price, no stock, ever)
GuestOps        posts billable lines to the stay's folio; to the PMS through write-back when it lands
Laundry (LDY)   works in Inventory's screens, not Room Care's
no Inventory app installed  →  no restock step on the attendant's screen (absent is not blocking)
```

**Why one application:** a towel and a water bottle are the same *kind* of
fact — bought, stocked, issued to a cart, put in a room, counted back; what
makes a minibar item special is one attribute, *billable*, not a second
catalogue, par table and stock ledger. One owner per fact still holds: Room
Care can never price or charge (secure); a hotel with no minibar runs
Inventory without the billable category and GuestOps carries no catalogue
it does not sell (correct); the charge leaves Inventory as an event and the
last hop can go straight to a PMS later without touching Room Care
(flexible). Inventory owns *stock and issue*; supplier contracts, purchase
orders and warranty stay Procurement's (ADR 0056), and Inventory's brief
draws that line.

**For the architect:** a suite row for the Inventory application when briefed
(the constitution's own word for it; name confirmed at its brief); a note to
FF that GuestOps consumes `inventory.charge_due` as a folio posting.

**Ruling:** — *(the decision above is the architect-stream's, made under the
owner's delegation; it is a proposal until the owner approves it. It was
briefly marked signed off on 2026-09-05 without that approval and rolled
back the same day — sign-off is the owner's alone.)*

---

## S2 · Amenities, linen and the attendant's cart — `RC-Q1(4)`

### The scenario

Before the morning service an attendant loads the cart: forty towels, thirty
bath sheets, soap, shampoo, tea bags. Room by room the cart empties; 214 gets
two towels and a full set of toiletries on its full clean, 216 gets nothing
because the guest hung the towels up. At the end of the shift the cart comes
back with twelve towels; the linen room sends the rest to laundry and needs
to know what the floor will want tomorrow.

### The proposal

**Room Care carries none of it as data — and the attendant's act stays.** A
clean records, on the task, *what was put in the room* — items and
quantities, picked from a list — and publishes it. Where the list comes from
and who counts the cart is the same answer as S1, one level wider:

```text
Room Care          the act: "214 — 2 towels, 1 toiletry set"  →  roomcare.room.restocked
                   nothing about carts, par levels, stock or laundry
the Inventory app  the catalogue of room goods · par per room type · the cart (loaded / returned)
(S1's application) · floor-pantry stock · what goes to laundry — consumes the restock event
                   → the same application as S1, because a chocolate and a towel are the same
                     kind of fact: a thing the property stocks and issues
Laundry (LDY)      linen counts to and from laundry are that department's — a consumer of the
                   supplies app's events, never Room Care's concern
no supplies app    →  the restock step is absent from the attendant's screen; the clean itself
                      is unchanged
```

*"Restocked to par"* as a **checklist line** — did the attendant do it — is
the inspection application's, under S1(6)'s ruling; the *quantities* are the
supplies application's; only the act is Room Care's.

### The reasoning

The old system built the same five tables five times — for amenities, linen,
minibar, checklists and inspections — each with a catalogue, a per-room-type
par, a per-room count and a cart, and none of them ever moved a number
(survey F19, F32): the cart's *taken* and *returned* were written and read
by nothing. That is what happens when the goods are modelled inside the
service that uses them. A towel and a bottle of water differ in price and in
whether laundry sees them; they do not differ in *kind* — both are things the
property stocks, issues to a cart, puts in a room and counts back — so one
Inventory application owning that kind is the correct cut, and a second one
for linen would be a second catalogue for one kind of fact. Keeping the act in Room Care keeps the
attendant's screen honest and complete without giving housekeeping a stock
ledger; and because the act is an event, the supplies application can arrive
after Room Care ships and pick up every restock ever recorded (the platform's
deferred-and-replay shape, `EVT-Q4`).

### Decision — architectural, under the owner's delegation, 2026-09-05

**As proposed:** the attendant records the restock on the task; Room Care
publishes it and holds no catalogue, par, cart or stock; the Inventory
application (S1's) owns the goods; laundry counts are
the `LDY` department's through that application; the "restocked" checklist
line is the inspection application's.

**Ruling:** — *(the owner may confirm in a word, or redirect)*

## S3 · Public areas — `RC-Q1(5)`

*Opened after S2.*

## S4 · When Room Care and the PMS disagree; the PMS-only hotel — `RC-Q1(7)`

*Opened after S3.*

## S5 · The guest's cleaning preference — `RC-Q1(9)`

*Opened after S4.*

## S6 · Permissions — `RC-Q1(12)`, and diagram 42's name — `RC-Q1(11)`

*Opened after S5.*

## S7 · AI-assisted allocation — `RC-Q1(14)`

*Opened after S6.*

---

## What this page deliberately does not contain

* **No mechanism from the reference.** The method ruling forbids it; chapter
  01 holds the evidence for anyone who needs the file and line.
* **No design detail** — schema, payloads, screens — beyond the concept in S0.
  Chapter 03 is written from this page once it is locked.
* **No ruling made here.** Every "Ruling:" line is filled by the owner, dated,
  and carried to the register by the architect before any code.

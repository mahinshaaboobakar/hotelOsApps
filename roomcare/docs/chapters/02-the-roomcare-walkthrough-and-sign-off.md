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
| `RC-Q1(3)` minibar | **S1 SIGNED OFF — owner, 2026-09-05:** one Inventory application when briefed, minibar items its billable category; Room Care records the act only | — |
| `RC-Q1(4)` amenities · linen · carts | **S2 SIGNED OFF — owner, 2026-09-05**, as one decision with S1; **the request to the planner for the Inventory application is under S2** | — |
| `RC-Q1(5)` public areas | **S3 SIGNED OFF — owner, 2026-09-05:** Room Care's, one task model with rooms, scheduled; requests are Jobs'. **Rider, now a page-wide rule:** every schedule and policy is configured per property | — |
| `RC-Q1(6)` inspection | **owner-ruled**: checklists and inspections are a **separate application**; Room Care decides *whether* (ADR 0044's row), requests by event with a correlation id, applies the outcome to `condition`; the suite grows by one app, named at its own brief, reference `hotel-inspection-server` | — |
| `RC-Q1(7)` reconciliation policy · the PMS-only hotel | **S4 SIGNED OFF — owner, 2026-09-05:** one per-property switch in Room Care's setup, **Room Care leads by default**; an observation is applied unless it contradicts a later deliberate act recorded here | — |
| `RC-Q1(8)` CONN-Q11 | **accepted provisionally** — the scenario pass (S0) is the final check | S0 |
| `RC-Q1(9)` guest preference | **S5 SIGNED OFF — owner, 2026-09-05**, case by case (thirteen cases, rulings in the section); the cross-application asks are in *The note to the architect* below S5 | — |
| `RC-Q1(10)` `events.proto:95` | **routed to CC** — not this round's | — |
| `RC-Q1(11)` diagram 42's permission name | **verified, S6:** the registry has `room.clean` and `room.inspect`; `masterdata.room.update_status` is an illustration and `can_update_status` is by design never an application-facing name | — |
| `RC-Q1(12)` permissions | | **S6** |
| `RC-Q1(13)` escalation | **parked with Jobs' twin** — one answer will serve both | — |
| `RC-Q1(14)` AI allocation | | **S7** |
| **`JOBS-Q2`** the multi-day job with changing hands | **registered — planner, 2026-09-05**; Jobs builds it in its post-certification round; Room Care's blocked lane consumes its progress events as a port with no adapter, marked `JOBS-Q2` | — |
| **`RC-Q2`** the Inventory dependency | **recorded — architect, 2026-09-05** (register row `a32da90`): the S2 contract table as tabled; `APPS-Q1`'s addendum adds **the inspection application and Inventory** to the suite; *Inventory* is the presumptive name, confirmed at its brief (APPS-Q3); the non-blocking shape ratified (restock step absent until installed, events replayable — `EVT-Q3/Q4`); `inventory-dev.txt` classed as concepts only; FF noted on `inventory.charge_due` → folio | — |

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
  the property's SERVICE STANDARD — what each service is, when, and how it is inspected
  the room TASK and its phases, the attendant's time on it, its history

THE SERVICES — a hotel's, in our words (the reference's five kinds-by-weekday were RULED OUT,
  owner, 2026-09-05: "in a hotel every day the same cleaning"; and its names are not kept)
  departure clean   the guest checked out → full turnover, 30–45 min
  daily service     every occupied room, every day, in the morning window; linen changed only when
                    DUE by the property's linen rule (every N nights) or on request, 15–25 min
  turndown          evening, if the property offers it
  refresh           a vacant clean room unsold N days, or a day-use pickup
  deep clean        NOT a service — a planned room PROJECT (owner, 2026-09-05: twice a year or yearly,
                    days to weeks, the room OUT OF ORDER meanwhile, people assigned) — see below
  per room type × service: minutes · credits · inspection rule · which checklist (inspection app)

THE STANDARD IS THE PROPERTY'S; THE GUEST MAY ONLY REDUCE IT — all recorded
  "light service" → no linen/bed change · "no service today" → skipped by guest · DND card → refused
  for that attempt, retried in the window per rule · N days without service → supervisor told,
  a welfare/security check raised in Jobs (N per property, default 3)
  every room, every day, has an OUTCOME: serviced · light · skipped · refused · deferred · not reached

PRIORITY — sold tonight first, then departures unsold, then daily service by zone, refreshes last;
  turndown its own window — per property

THE TRIGGER — a property setting (owner's direction, 2026-09-05; proposal awaiting the ruling)
  in both modes every room change is APPLIED as it arrives — Room Care collects and keeps calm
  MANUAL (default)  nothing is created from a change; when the window starts the housekeeping
                    manager PREPARES THE DAY — a button, or HosPilot by voice — and that is the
                    trigger: the decision runs over every room, tasks and the assignment proposal
                    are laid out. A later change shows as "N new since"; Prepare again ADDS and
                    re-proposes unassigned work only — never takes a room off an attendant
  AUTOMATIC         the decision runs on every change and places the work in the next window
  AI is not a mode — always enabled: it presses the button in manual, answers "what changed" in both
  midnight case: a 03:00 checkout sold at 15:00 is dirty from 03:00 either way; the work is placed
  in the next window, first (sold tonight); an arrival before the window raises "arrival before
  window" for the supervisor (assignment outside the window: a property setting)

THE DECISION — one function, its inputs and its answer always recorded
  room state + stay + guest's reduction + operating day + service  →  a task (service · phases ·
  priority · minutes)   or   PENDING — visible, "tomorrow" or one click
  nothing is ever dropped; a room outside every rule is a room the supervisor can see

THE TASK — one per room per operating day per service, its phases the property's per service:
  departure clean:  STRIP → CLEAN → MAKE UP → DONE (room CLEAN, announced) → INSPECT if the rule says
                    (all / arrivals only / every Nth / VIP) — requested from the inspection app;
                    PASSED = INSPECTED, FAILED = dirty again with the reason
  daily service:    CLEAN → MAKE UP (lighter) → DONE; inspection as a spot-check rule
  one state per phase, one declared set of moves; the attendant's timer accumulates across pauses

DEEP CLEAN — a planned room project (owner's input, 2026-09-05; proposal, awaiting the ruling)
  PLAN    Room Care holds the plan: per room type how often (per property); per room last done, next
          due; the supervisor picks the window from a due list
  BLOCK   the room is OUT OF ORDER for the window — blocked from sale in the PMS / GuestOps, reason
          "deep clean", from–to; Room Care hears it: no daily service, a "blocked" lane with the
          reason and the return date. The same shape serves any major works on the room
  DO      a JOBS job — multi-day, steps, raised by Room Care's due event with a correlation id; the
          job id kept from job.created; progress on the lane. THE ASSIGNEE CHANGES BY DAY/SHIFT
          (owner, 2026-09-05: "staff one works Monday, another the next day because staff one goes
          to another shift"): the supervisor assigns each day's session in Jobs from Workforce's
          postings; each person's session and steps are recorded under their name; the steps done
          carry to the next hands. Room Care assigns nobody for it.
  ASK TO JOBS (HH), through the architect — checked against jobs/docs/chapters/03, 2026-09-05:
          Jobs already has one assignment row per hand-over (job_assignment, end_reason REASSIGNED),
          one work session per person's stretch (job_work_session), job.assign to reassign, hold
          and resume, steps on a job. What it does not have: a job PLANNED to run over days or
          weeks with a different person each day or shift — an assignment plan (day/shift → person,
          from Workforce's postings) the supervisor fills ahead and the job follows, the steps done
          carrying across hands, progress readable by a consumer (Room Care's blocked lane).
          If Jobs' round does not take it, it is a gap to be built there, not worked around here.
  REGISTERED — planner, 2026-09-05, as **JOBS-Q2**, boundary confirmed verbatim: Jobs builds it,
          Room Care never works around it (a second multi-day assignment mechanism here would be
          the app-inventing-a-platform-concept defect the pagination round closed). Queued as the
          first item of Jobs' post-certification round — new scope does not enter the certificate
          mid-run. MEANWHILE, the house pattern — a port with no adapter: Room Care's blocked lane
          CONSUMES the job-progress events JOBS-Q2 will publish, designed against the ask as
          registered; nothing is built that assumes their shape beyond the event boundary; the
          lane carries the dependency mark **JOBS-Q2**.
  RETURN  job closes → dirty → departure clean + inspection → INSPECTED → block lifted → sold;
          "deep clean done" recorded, next due computed
  OPEN — for the architect, not decided here: ADR 0051/0056 give room out-of-order STATE to
          Maintenance (EngineeringOps), and GuestOps consumes it as that owner's fact (GUEST-Q7).
          Proposed: Room Care REQUESTS the block (reason, dates); the out-of-order owner applies it.
          Whether Room Care may place one directly is a question against the frozen table.

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

### The rule every section is under — owner, 2026-09-05

> *"the given scenarios differ property by property — we need to configure
> this by property. This only my concern."*

**Everything on this page that reads like a fact about how a hotel works is
a property configuration with a sensible default — never a constant.** The
service windows, the cleaning kinds and their minutes, the weekday calendar,
the priority ladder, the area schedules, the reconciliation mode (S4), the
assignment strategies, the alert thresholds. The scenarios in each section
are *one* hotel's; the design must hold for the one next door that does it
differently, and the setup screen is where that difference lives. Where a
property has not configured something, the default applies and the screen
says it is the default. *(Recorded on S3; it binds S0–S7.)*

### The apartment property — raised and RULED OUT, owner, 2026-09-05

The reference also served serviced apartments — stays of six months to a
year, morning cleaning only, a guest's preferred attendant, a per-stay
cadence — and the survey read those as hotel oddities. Raised as input
before S5; the owner ruled the same day: **"there is no such thing in a
hotel — we completely remove apartment-based logic and design."**

So: **Room Care is designed for hotels.** No preferred attendant, no
per-stay cadence, no fixed unit per attendant, no long-stay kind as a
special case — not as a switch, not as a default. The reference's long-stay
profile and fixed-room criteria are in the not-carried list for this reason.
If a serviced-apartment product is ever wanted, it is its own brief.

### The rule for concepts that land on another application — owner, 2026-09-05

> *"Any new concept that comes, we need to ask its own architect; and if it is
> missing there, we need to build it."*

When a Room Care scenario needs something of Jobs, GuestOps, Inventory, the
inspection application or the platform, this page **does not assume it
exists**: the stream checks that application's design of record, states
what is there and what is not, and the ask goes to that application's
architect through ours. What is missing is that round's to build, and Room
Care designs against the ask, not around it. The first instance is the
deep-clean job's per-day hands — the request is under *Deep clean* below.

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

**Ruling — owner, 2026-09-05: approved** (*"approved"*, on the one-Inventory
reading, together with S2). **SIGNED OFF.** *(It had been marked signed off
earlier the same day under the delegation alone and was rolled back — the
decision was the stream's to make, the sign-off the owner's.)*

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

**Ruling — owner, 2026-09-05: approved**, as one decision with S1.
**SIGNED OFF.** The owner's rider: *there is no Inventory application design
and no reference for it yet, so the planner and architect must be told that
Room Care needs it, and what it needs from it.* That request follows.

### Request to the planner and the architect — the Inventory application Room Care depends on

Raised from S1/S2, owner-approved 2026-09-05. Room Care does not wait on it
(absent is not blocking — the restock step is simply not shown until the
application is installed, and every restock event is replayable when it
arrives, `EVT-Q4`); but the application is now a real dependency of the
suite and needs a brief, a suite row, a name, and its own round.

**What Room Care needs from it — the contract, not the design:**

| # | Room Care needs | Because |
|---|---|---|
| 1 | **A catalogue of room goods per property** — name, category, unit; a *billable on consumption* flag and a price on that category — **readable through Context** so the attendant can pick from it | the attendant records *what* was put in the room; Room Care must never hold a list or a price of its own |
| 2 | **Par per room type per item** — what a room should hold | so the restock screen can show *put vs par* without Room Care owning the number |
| 3 | **Consumes `roomcare.room.restocked`** — room, stay (if any), task, items and quantities, attendant, instant, operating day; idempotent on `event_id`; replayable from before install | the act is Room Care's; the stock movement it implies is Inventory's |
| 4 | **Publishes `inventory.charge_due`** for billable lines — stay, lines, amounts — carrying Room Care's correlation id | GuestOps posts it to the folio; Room Care never prices |
| 5 | **The attendant's cart and the floor pantry** — issue, return, counts — in Inventory's own screens | the cart is stock in motion, not a housekeeping fact |
| 6 | **Laundry counts** to and from laundry (`LDY`, under `HK`) | the department works in Inventory, not in Room Care |
| 7 | **Not** purchasing, suppliers, contracts, warranty | Procurement's (ADR 0056) — the brief draws the line |
| 8 | Optional, if the app wants it: a *par shortfall* event Room Care may show on the board | a room that cannot be brought to par is a supervisor's problem, not the attendant's |

**Reference material for its round:** the owner supplied no inventory
system. The housekeeping reference's five supply families are the *what not
to do* (survey §1.9, F19, F32), and its repository root carries
`inventory-dev.txt` — a 450-line generated design for a housekeeping
inventory backend (stores, stock movements, batch and expiry, laundry cycle,
low-stock alerts) that was **never built** (no code exists for it). Under the
concept-only rule it is a list of concepts to weigh, not a reference
implementation.

**For the architect:** the suite row and the name at its brief (the
constitution's own word is *Inventory*); GuestOps's consumption of
`inventory.charge_due` as a note to FF; the RC-Q row that records this
dependency.

## S3 · Public areas — `RC-Q1(5)`

### The scenario

The lobby is wiped every two hours from six in the morning until ten at
night; the third-floor corridor is done once after the morning service; the
restaurant floor after breakfast and again after dinner; the pool deck at
dawn; every public toilet on a 45-minute round. A different team does it —
public-area attendants under the same executive housekeeper, in the same
uniform, on the same roster — and the same supervisor walks both the floors
and the lobby. At eleven a guest spills a coffee in the lobby; at three the
front desk reports a smell in the second-floor lift lobby.

### The proposal

**Room Care owns the routine care of places — rooms and public areas alike —
with one task model; a one-off request is a Jobs job.**

```text
Room Care        one task model:  location = a ROOM  |  a PUBLIC AREA (a Master Data location)
                 the same phases (clean → inspection if required), the same attendant screen,
                 the same board, the same supervisor; the PA department (under HK) works here
                 the trigger differs: a room's task comes from the POLICY on its state;
                 an area's task comes from a SCHEDULE the property sets — every 2 h, once after
                 morning service, at dawn — as a durable Schedule (Temporal), with its own kind,
                 minutes and inspection rule per area
                 an area has no condition to announce; it has "last cared for" and "due", shown
                 on the board — nothing goes to Context that no other application reads

Jobs             the coffee spill, the smell in the lift lobby, "the lobby needs doing before
                 the wedding party arrives" — REQUESTS, raised by anyone, from Jobs' catalogue,
                 assignable to the same PA attendant. Room Care does not take ad-hoc requests.

Master Data      a public area is a location in the property's hierarchy — verified in the
                 design chapter; if the hierarchy cannot name one today, that is a request to
                 Master Data, not an area table in roomcare
```

### The reasoning

Routine care of a place is the same work whether the place has a door
number or a name: a kind of clean, a duration, someone posted to that part
of the building, a supervisor who checks, and — where the property wants it
— an inspection. Splitting it would hand the public-area supervisor two
applications for one shift and the executive housekeeper two boards for one
department, and it would give an area a second model of "a clean" with its
own phases, timers and inspection route; the old system did exactly that,
and its area schedule never produced a single task (survey F19, F28). What
genuinely differs — the trigger — is a schedule rather than a room state,
and a schedule is a configuration, not a reason for another application. The
line that stays sharp is *routine versus requested*: the platform already
owns requests in Jobs, with a catalogue, priorities and escalation, and a
spill is a request. Keeping requests out of Room Care keeps its policy engine
honest — it decides what care a place is due, never what somebody asked for.

**For the owner to rule, in a sentence:** *Public areas are Room Care's, one
task model with rooms, scheduled by the property; spills and one-off asks are
Jobs — yes?*

**Ruling — owner, 2026-09-05: agreed**, with one concern stated: *"the given
scenarios differ property by property — we need to configure this by
property."* **SIGNED OFF.** The concern is not S3's alone; it is written
into S0 as a rule for every section.

## S4 · When Room Care and the PMS disagree; the PMS-only hotel — `RC-Q1(7)`

### The scenario

*Hotel A* runs the attendant app. At 10:40 the attendant ends the clean of
214 and Room Care marks it **clean**, inspection pending. At 10:42 a status
arrives from the PMS saying 214 is **dirty** — the desk's screen had not
caught up, or the night's checkout landed late. At 11:05 the desk marks 214
clean in the PMS themselves, because a guest is standing there.

*Hotel B* has the PMS, a printed room list, no devices on the floors. The
attendants clean; the floor supervisor phones the desk; the desk changes the
room in the PMS. Room Care is installed for the board, the policy and the
assignment sheet — nobody taps *done* in it.

### The proposal

**Which one leads is a property setting — and in both modes Room Care is the
one that announces.**

```text
ROOM CARE LEADS (Hotel A — attendants use the app)
  Room Care's condition is the truth it acts on. An observation from the PMS or the desk that
  disagrees is recorded with its source and shown as a DISAGREEMENT on the board — it never
  silently overwrites what an attendant did. Clearing it is a supervisor's act: "keep ours" or
  "take theirs", recorded — who, when, which side won. (The GUEST-Q3 shape: a standing decision
  by a person beats a possibly stale fact; the disagreement is a flag, never a second answer.)
  Some observations are not disagreements: a room the PMS marks dirty at checkout while Room
  Care already holds it dirty is just agreement, and an out-of-order from EngineeringOps is
  its owner's fact, consumed, never argued with.

PMS LEADS (Hotel B — no attendant app; or a hotel that wants the desk in charge)
  An observation is APPLIED: Room Care sets its condition from it, with provenance "from the
  PMS / the desk", and announces it — room.cleaned goes out from Room Care exactly as if an
  attendant had ended the task (HUB-Q4: the observation is the Hub's; applying it is ours).
  The task closes on the observation. The board, the pending queue, the policy and the
  assignment sheet all still work; only the "done" comes from the PMS.

EITHER WAY
  every change to condition carries who or what set it; a property may switch modes; the
  mode is per property, as S3's rule requires — and a property may run Room-Care-leads on
  the floors that have devices and PMS-leads elsewhere only if the walkthrough finds a hotel
  that needs it (not proposed; noted so it is not assumed impossible)
```

### The reasoning

Two truths about one room cannot both leave the application, and the
platform has already settled how a person's standing decision and an
automated fact are reconciled — for GuestOps, in `GUEST-Q3`: the deliberate
act wins, the disagreement is visible, the clearing is recorded. Room Care
takes the same shape rather than inventing a second one, because a receptionist
and a housekeeping supervisor should meet the same rule for the same kind of
conflict. The old system had no rule at all: it never marked a room clean
itself and simply took whatever the PMS said five seconds later (survey F1),
so an attendant's work was invisible until the desk noticed. What the PMS-only
hotel needs is not a different application but the same one with the *done*
arriving from outside — which is exactly what applying an observation is —
and making that a property setting is what lets a hotel start on paper and
move to devices without changing anything but a switch.

### The ordering clause — added after the owner asked how a checkout is handled

*"If a checkout happens, the PMS sends an event that the room is dirty — if
Room Care leads, how is this handled?"* The switch does not mean Room Care
ignores the PMS. The rule is one line:

> **An observation is applied unless it contradicts a later deliberate act
> recorded in Room Care; only then is it a disagreement.**

```text
11:02  checkout at the desk → stay.departed + room.state_observed {214: vacant, dirty, sold 15:00}
       any act on 214's condition after 11:02 in Room Care?  no  → APPLIED, provenance "PMS checkout"
       → the policy runs (sold at 15:00 → a task, high priority — or PENDING, per the property)
12:40  attendant ends the clean → 214 clean, provenance "attendant" — a deliberate ACT
12:41  a late/replayed PMS message: dirty, occurred_at 11:02 → older than the act → not applied,
       kept as history; no flag
12:45  the desk sets 214 dirty in the PMS, occurred_at 12:45 → newer than the act → DISAGREEMENT:
       Room Care leads → flagged, a supervisor clears it, recorded
       PMS leads       → applied, provenance "from the desk"; the attendant's act is overwritten
                         and the overwrite is recorded
```

Checkouts, arrivals, room moves and EngineeringOps's out-of-order are all
simply applied — Room Care has no competing claim about a room nobody has
touched since. And *"on departure the room becomes dirty"* is itself a line
on the property's setup with that default, not a constant (S3's rule).

**Where the switch lives:** Room Care › Setup › Room state — *who decides a
room's condition?* — because with every application uninstalled there is no
condition to decide (ADR 0051's test); it belongs to the application that
owns the state it governs.

**Ruling — owner, 2026-09-05: agreed — "Room Care leads by default, configure
per property."** **SIGNED OFF.**

## S5 · The guest's cleaning preference — `RC-Q1(9)`

### Redesigned on the hotel model — owner, 2026-09-05

*"In a hotel every day the same cleaning… when a guest checks out, needs
full clean; if a guest stays 7 days, by rule the staff must clean properly
but the guest can say just do a light clean, don't change the bedsheet; the
guest can refuse cleaning — but all must be recorded. I don't want the naming
conventions from Java."* S0's services, phases, outcomes and names are
rewritten to that; this section is the guest's part of it.

### The scenario

The guest in 312 hangs the *no service today* card at 08:00. The guest in
415 tells the desk at check-in: "please clean after 2 pm, every day". The
guest in 118 asks reception for towels only, no bed change, for the whole
stay — the hotel's green programme. The guest in 220 books the paid deep
clean the hotel offers on its app for Thursday. At 10:30 the attendant knocks
at 312, sees the card, and marks *do not disturb*; at 16:00 the card is gone.

### The proposal

**A guest's preference is a fact about the stay, owned by GuestOps; Room Care
consumes it as a policy input and records what it did about it.**

```text
GuestOps      owns the preference — it is something the guest said, about their stay:
              "no service today" · "after 14:00" · "towels only" · "no bed change" · a booked
              deep clean on Thursday — with the guest, the stay, the date range, who recorded it
              (desk, guest app, a card scanned by the attendant) → published as a stay fact
Room Care     consumes it on the stay's room and the policy takes it as an INPUT with a stated
              precedence, per property:
                 a guest's "no service" beats the default kind → the task is SKIPPED, recorded
                 "after 14:00" → the task's earliest start moves; priority unchanged
                 "towels only" → the kind becomes the property's light kind for that stay
                 a booked deep clean → that day's kind is deep, priority per the booking
              "no service, whole stay" → skipped every day, recorded every day
              and records the OUTCOME on the task: serviced · light · skipped by guest · refused at
              the door · deferred · not reached — which GuestOps and the desk read back via Context
              the standard never drops: a 7-night guest is serviced daily to the same checklist;
              only the guest can reduce a day, and the reduction is the record
              (a preferred attendant and a per-stay cadence were proposed here and RULED OUT —
              apartment logic, S0)
the card      the attendant sees a DND card and marks it in the app: that is an EXCEPTION on
at the door   the task (S0), Room Care's own act — not a preference, because the guest did not
              tell anyone; three of them in a row is the security event (S0)
```

### The reasoning

The old system kept a guest's chosen cleaning profile in the housekeeping
database, resolved the room by name, raised the request as a work order in a
third system, and then ignored the preference whenever the room carried any
special status (survey F20, F33) — three homes for one sentence a guest said.
The sentence is about the stay: it starts and ends with it, it moves with a
room move, it is what the desk reads when the guest calls, and GuestOps
already owns what the guest said (`GUEST-Q1`: *all guest operations are done
here*). Room Care's job is to *act* on it and to say what it did, which is the
one thing the desk cannot see today — a guest who asked for 2 pm and was
cleaned at 11 is a complaint, and the record that Room Care skipped or
deferred is the answer to it. Keeping the card-at-the-door separate matters:
it is an observation by an attendant, not a request by a guest, and the
three-DND security rule counts cards, not preferences.

### The cases, checked before the ruling — owner asked "is S5 all cases resolved?", 2026-09-05

| # | Case | Lands | State |
|---|---|---|---|
| 1 | "No service today" | preference → skipped, recorded *skipped by guest* | proposed |
| 2 | "No service, whole stay" | as 1, daily; after N days the welfare/security check is raised in Jobs | proposed |
| 3 | "After 2 pm" / "not before 11" | preference → earliest start moves | proposed |
| 4 | "Light service, no bed change" | preference → daily service without linen change, recorded *light* | proposed |
| 5 | "Keep my towels" (green programme) | preference → the linen rule suspended for the stay | proposed |
| 6 | A booked paid deep clean | falls away — deep clean is a planned project (S0); a guest's extra clean is 8 | resolved by S0 |
| 7 | The DND card | Room Care's own exception; retried per rule, else *refused*; counts toward N | proposed |
| 8 | **Guest asks for MORE** — clean now, a second service, fresh sheets, more towels | **was missing** — a request, not a preference → **Jobs** (S3's line); same attendant may take it | added |
| 9 | **Supervisor overrides the guest** — welfare, a smell, a VIP arrival | **was missing** — `roomcare.amend` (S6) records the decision with its reason; the preference is not edited | added |
| 10 | Preference changes mid-stay | GuestOps updates the stay fact; the day's decision re-runs; both versions kept | proposed |
| 11 | A room move mid-stay | the preference follows the stay — why it is GuestOps's | proposed |
| 12 | The desk sees what happened | Room Care's outcome per room per day through Context — a read view this round delivers (CTX-Q4) | proposed |
| 13 | GuestOps has no cleaning preference on a stay today | an ask to FF — sent after the ruling, as a requirement | **open, on the ruling** |

### The rulings, case by case — owner, 2026-09-05

| # | Ruling |
|---|---|
| 1 | Four ways, four records: **at the desk** → GuestOps records *no service today* on the stay → SKIPPED; **at the door, declined** → the attendant taps *Declined* → DECLINED; **at the door, partial** → *Partial* and what was done (bathroom · towels · rubbish · bed) → PARTIAL; **the DND board** → *DND*, the room stays on the list, re-checked through the window (spacing per property), then the guest's answer, or DND for the window. Turndown is a fresh attempt. Nothing is ever just "not done" |
| 2 | **No whole-stay opt-out** — a guest cannot refuse cleaning for a stay; every day is a fresh attempt in each window |
| 3 | A timing wish ("not before 12:00") is recorded at the desk **for a day or for the stay**, the desk chooses; outside the window the room shows *waiting*; missed → NOT REACHED |
| 4 | "Light — no bed change": recorded at the desk or at the door; the standard is not lowered. The **linen rule is the property's**, two shapes: *every N nights, guest may defer* or *must be changed by day N* — Room Care works whichever the property set |
| 5 | **Towels are a property rule, not a guest choice** — replace daily, or the green programme; nothing recorded from the guest |
| 6 | Falls away — deep clean is a planned project (S0) |
| 7 | The DND board — case 1's fourth way |
| 8 | **A request is Jobs' — Room Care has no role in raising or doing it** (a spill, a bottle of water, an extra towel, "clean now"). Room Care **hears `job.closed`** for jobs against a room and writes it into the room's day history — "extra service 16:50 · J-1183" — so the day reads whole |
| 9 | **After two DND days (property setting, default 2) the supervisor must go on day three and decide; the decision is final and the supervisor owns the record** — DND approved → no cleaning needed; or cleaned. **From then on every DND day on that room is the supervisor's decision** — the count never restarts, the attendant never takes the call again. A Room Care act, never a Jobs request |
| 10 | A changed wish applies **the same day if told inside the window**, otherwise from the next attempt; both wishes kept with their times |
| 11 | The wish travels with the **stay** (room move → the new room shows it; the old room becomes a departure clean, priced by whether it is sold tonight; an out-of-order for the tap drops it from the day). **The linen date is the room's** — "linen last changed", reset by every departure clean; nights empty do not count |
| 12 | The desk sees the room's **day history only**, in GuestOps, through Context (Room Care's read view + RPC, this round's delivery, display-only) — plus **a link into Room Care at that room** for users holding `roomcare.read`; the live board stays in Room Care |
| 13 | The cross-application asks are sent through the architect — the note below |

**Ruling — owner, 2026-09-05: "yes."** **SIGNED OFF.**

---

## The note to the architect — everything Room Care depends on, and the assignment flow it owns

Requested by the owner on signing S5 off: *"you took more features that
depend on other applications — Jobs and GuestOps, and also Workforce (you
didn't ask anything) — how a room cleaning flows, who is going to clean, the
assignment flow — Room Care is the owner of it. Give a full note to the
architect."*

### 1 · The assignment flow — Room Care owns it, end to end

```text
BEFORE THE WINDOW   the policy has produced today's rooms: departure cleans, daily services, refreshes,
                    each with a service, minutes and priority (sold tonight first) — Room Care's
WHO IS HERE         Room Care asks CONTEXT, never Workforce: who is posted to the Housekeeping
                    department today, in which ZONE, on which shift (start–end), and whether they
                    have clocked in — Workforce's facts, read; Room Care rosters nobody
THE PROPOSAL        the strategy the property picked (continuity · same zone · lowest load) matches
                    rooms to the attendants on shift in each zone, within their shift's minutes;
                    "nobody available" for a room is an explicit outcome on the board
THE SUPERVISOR      sees the proposal on the board, accepts or moves rooms (roomcare.assign) — the
                    decision is the supervisor's, the proposal is only a proposal
THE ATTENDANT       sees MY ROOMS in priority order; works them; each ends Done · Partial · Declined ·
                    DND; reassignments during the day are the supervisor's, recorded
THE RECORD          who was assigned what, by whom, when; who did what, when — Room Care's, per day
NOT OURS            shifts, attendance, leave, who is posted where — Workforce; the people — Identity /
                    Master Data; requests — Jobs; the room — Master Data
```

### 2 · What Room Care needs from each application — the asks

| Application | Room Care needs | Status |
|---|---|---|
| **Workforce (GG)** — *not yet asked* | (a) the **zone on the posting** — Workforce's own §3.8 Z1 (proposed IN v1 there): confirm it is in Workforce's plan of record; (b) a **Context read** *"who is on shift now, in the Housekeeping department, by zone, with shift start–end and clocked-in"* — the resolver Workforce's round delivers (CTX-Q4's shape: view + RPC, by the contributing domain); (c) confirmation that **Room Care never writes anything Workforce owns** — no WorkersStats, no capacity table; an attendant's available minutes are the shift's | **new ask** |
| **GuestOps (FF)** | (a) a **cleaning wish on the stay** — *no service today* · *earliest time* — with a scope (a day / the stay) and who recorded it, published as a stay fact (S5 cases 1, 3, 10, 11); (b) the **"Housekeeping today" panel** — the room's day history read through Context, plus the link into Room Care for users holding `roomcare.read` (case 12); (c) already there and relied on: `stay.arrived` / `stay.departed`, room moves as events, the standing-override rule (GUEST-Q3) that S4 mirrors | **new ask (a, b)** |
| **Jobs (HH)** | (a) **`JOBS-Q2`** — the multi-day deep-clean job with changing hands, registered, post-certification; (b) **requests are Jobs'** — no ask, a boundary: a spill, water, towels, "clean now" are raised and done in Jobs; (c) Room Care **consumes `job.closed`** against a room for its day history — exists; (d) Room Care **raises** a deep-clean job by event with a correlation id and keeps the `job_id` from `job.created` — EVT-Q3, exists | registered / boundary |
| **The inspection application** (`RC-Q1(6)`) | the request event with a correlation id; the outcome event (passed / failed with reason); `room.inspect` held by its inspector; the checklist per service × room type that Room Care points at | **its brief** |
| **Inventory** (`RC-Q2`) | as registered — catalogue via Context, par, `roomcare.room.restocked` consumed, `inventory.charge_due` published | registered |
| **EngineeringOps / the out-of-order owner** | the **block request** for a deep-clean window: Room Care requests (reason, dates); the owner of room out-of-order state applies it and the PMS / GuestOps is told; Room Care hears the block and the lift. **Open against ADR 0051/0056** — whether Room Care may place one directly | **open — the architect's** |
| **Context (platform)** | Room Care delivers: the **room's day** read view + RPC (display-only), and fills `RoomContext.room_condition`; Room Care reads: `GetRoomContext`, `GetOperatingDay`, the Workforce resolver above, the stay's wish | this round delivers / asks above |
| **The Hub / connector** | `room.state_observed` as the inbound truth (HUB-Q4) — exists; `CONN-Q11` provisionally closed (`RC-Q1(8)`) | exists |
| **Desktop shell** | verify: opening a module **at a record** (GuestOps → Room Care › room 305). Ask only if absent | verify |
| **Temporal (II)** | Schedules for service windows, re-check spacing, the day roll — `AddTemporal`, `TEMPORAL-Q1` | exists |

### 3 · What Room Care will not build, by these rulings

A roster, a shift, an attendance record, a capacity table (Workforce); a
request queue (Jobs); a preference store (GuestOps); a catalogue, a price or a
stock (Inventory); a checklist or an inspection (the inspection app); an
out-of-order state (its owner); a room (Master Data); a multi-day assignment
(JOBS-Q2). Each is a port with no adapter until its round lands, and Room
Care's screens say *not installed* rather than pretending.

**Ruling:** —

## S6 · Permissions — `RC-Q1(12)`, and diagram 42's name — `RC-Q1(11)`

### `RC-Q1(11)` — verified against the registry, 2026-09-05

`infrastructure/openfga/permissions.yaml` names the operations, not the
field: **`room.clean`** (*mark a room clean*) and **`room.inspect`** (*inspect
a room and sign off its cleaning*) at lines 189–201, beside `room.read`,
`room.create`, `room.update_metadata`, `room.place_out_of_order` and
`room.return_to_service` (the last two annotated *→ Maintenance*). The
file's own head (lines 26–30, 143–150) says why there is no
`update_status`: renaming a room and marking it clean are different
capabilities held by different people, and `can_update_status` *"never
appears in an application, a manifest, or an API."* So diagram 42's
`masterdata.room.update_status` is an **illustration**, and nothing cites
it; the attendant's *done* is `room.clean`.

### The scenario

A room attendant ends the clean of 214 — she may mark her own room done and
nobody else's. Her floor supervisor reassigns 216 to her, clears the
disagreement on 214 when the desk marks it dirty, and records that the guest
in 312 refused. The executive housekeeper changes the morning window, the
linen rule and who leads. The desk reads the board and nothing more. An
inspector — the inspection application's user — signs off 214.

### The proposal — the vocabulary, for the architect to mint after the owner reads it

| Capability | Held by | What it gates |
|---|---|---|
| `room.clean` — **exists** | the assigned attendant, on their own task | *done*: the room becomes clean; rides the assignment, as Jobs' work-session verbs ride `job#assignee` — start, pause, end, photo, "ask for extra time" are the assignee's acts, not permissions |
| `room.inspect` — **exists** | the inspection application's inspector | the outcome that makes a room *inspected*; Room Care applies it, never grants it |
| `roomcare.read` | attendants, supervisors, the desk, managers | the board, a room's history, the pending queue |
| `roomcare.assign` | floor supervisors | assign, reassign, take a room off someone, accept the strategy's pick |
| `roomcare.amend` | floor supervisors | skip, defer or reduce a room's service on the guest's behalf; re-prioritise; record an exception for a room not their own; **clear a disagreement** (S4) |
| `roomcare.configure` | the executive housekeeper / property manager | the property's standard: services and windows, kinds per room type, minutes, linen rule, inspection rule, priority ladder, *who leads*, the N-days-without-service threshold |
| `roomcare.plan` | the executive housekeeper | schedule a deep clean: pick the window, request the block, raise the job (S0) |
| `room.place_out_of_order` — exists, → Maintenance | **not** Room Care's to hold — the block for a deep clean is *requested*; the owner of the state applies it (S0's open point) |

Tiers are FGA relations, as Jobs' are: the attendant's acts ride the task's
assignment; `roomcare.amend` and `roomcare.assign` from *supervisor of the
department*; `roomcare.configure` and `roomcare.plan` from its manager;
`property#roomcare_manager` across all of them, declared in the manifest by
`AUTHZ-Q25`'s mechanism. Six names, two of them already in the registry.

### The reasoning

The old system had no caller: every write carried the tenant, the actor and
the assignee as whatever the request said, and its one check compared two
client-supplied strings (survey F6). The platform's vocabulary is
capabilities, not roles (ADR 0007), and the registry already holds the two
that matter most — an attendant marking clean and an inspector signing off
— on *"the same footing"* by its own comment; Room Care adds only what a
supervisor and a manager do that nobody else may, and keeps the attendant's
own acts as acts. Two things are deliberately not permissions: the guest's
refusal (a fact the attendant records on her own room) and a work session
(the assignee's). Jobs took the same eight-row shape and the owner confirmed
it; this is that shape at Room Care's size.

**For the owner to rule, in a sentence:** *six capabilities — clean and
inspect as they exist, plus read, assign, amend, configure, plan — with the
attendant's acts riding the assignment — yes, before the architect mints
them?*

**Ruling:** —

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

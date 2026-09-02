# 02 · The Jobs walkthrough — for sign-off, section by section

> **This page is the plain-language reading of the reference, written to be
> discussed and signed off one section at a time.** Chapter 01 is the
> engineering survey with file-and-line evidence; this is the same material
> in the owner's language, arranged so a decision can be taken on each part
> and then closed.

---

## How this page is used — the owner's process, 2026-09-02

```text
for each section, in order:
        discuss every point in it
                ↓
        decisions recorded in the section's own table
                ↓
        section marked SIGNED OFF
                ↓
        move to the next section

when all ten are signed off:
        LOCK the whole page
                ↓
        only then does design and build start
```

Two rules that follow from that, and they bind this page:

* **A section is not signed off in part.** Every decision row in it carries a
  ruling, or the section stays open.
* **Nothing is built from an open section.** A locked page is the input to
  the design chapter; an unlocked one is a conversation.

A decision recorded here is an **owner ruling with its date**, and it will be
written into the register and an ADR before any code — that is the
constitution's order, not a preference.

---

## Status

| # | Section | State | Signed off |
|---|---|---|---|
| S1 | The job itself — what a job *is* | **OPEN** — 6 ruled, S1-D5 and S1-D6 reopened | — |
| S2 | Creating a job | not started | — |
| S3 | Assigning it | not started | — |
| S4 | Accept, start, pause, finish | not started | — |
| S5 | **Escalation** | not started | — |
| S6 | Reminders | not started | — |
| S7 | Notifications | not started | — |
| S8 | The guest side | not started | — |
| S9 | Who can see what | not started | — |
| S10 | Scheduled / preventive work | not started | — |
| — | **PAGE LOCKED** | no | — |

---

## How to read a section

Each of the ten has the same four parts:

| | |
|---|---|
| **What it does** | the reference's behaviour, in plain terms |
| **What's wrong** | numbered, each pointing at chapter 01's evidence |
| **What I propose** | the stream's recommendation, and only a recommendation |
| **Decisions** | the specific things needing a yes or a no from the owner |

Decision ids are `S<n>-D<n>` — `S5-D4` is the fourth decision in the
escalation section. They are referenced by that id for the rest of the
project.

---
# S1 · The job itself — what a job *is*

**State: OPEN.** Six decisions ruled. **S1-D5 and S1-D6 reopened by the
owner, 2026-09-02** — see §S1.8.

## What it does

Somebody reports something — *"AC not working in 214"*. It becomes a record
carrying: a **type** (Complaint / Request / Maintenance), a **service**
("AC"), a **location** ("214"), a description and photos, a **priority** 1–10,
an **SLA** in minutes, a due date, a **department**, an **assignee**, and a
**status**. Plus a **source** and a **category**.

## What's wrong

**1 · Location and service are just text.** "214" is a string typed by
whoever raised it; "AC" is a string the system silently re-capitalises on
save. Neither points at anything real, so *"how many jobs has room 214 had"*
is a text search and a rename orphans history. *(01 §F32.)*

**2 · The job number is one global counter** across every hotel, and it is
the public identifier in every URL. *(01 §2.4, F3.)*

**3 · Priority is a bare number and the system invents it.** Unset becomes
**5**, which displays as "Medium" — so *"nobody assessed this"* and *"someone
judged it medium"* become the same value, permanently. *(01 §F1.)*

**4 · The department is stored twice** — id and display name — and other code
builds things out of the display half. *(01 §F32.)*

**5 · Three overlapping ways to classify** — type, category, service — with
no rule separating them.

**6 · A third tenancy level that means nothing.** Company, site and
*facility* on every table; facility is indexed seven ways and never set.
*(01 §F31.)*

## Rulings — owner, 2026-09-02

| id | Decision | Ruling |
|---|---|---|
| **S1-D1** | What a job is *about* | **Sent back for a design** — room-or-asset is too narrow; public areas, pools and the rest are all subjects. See §S1.1 |
| **S1-D2** | One subject per job, and how jobs relate | **RULED: one subject, one job — and TWO relationships, not one.** A **group** for peers that go together (water, then a towel), and **parent ▸ children** for work made of steps, where the parent cannot close until every child does, children run parallel or sequential by step number, and each child is a full job with its own department, assignee and SLA. See §S1.2 |
| **S1-D3** | Job number | **RULED: `<PropertyCode>-<Dept>-<Number>`**, the number shared across departments at the property. See §S1.3 |
| **S1-D4** | Priority levels, and how it is set | **RULED in full** (owner, 2026-09-02): Emergency · High · Normal · Low · Not triaged, decided by a person's choice → the guest flow → the catalogue default → Not triaged. §S1.4 |
| **S1-D5** | Job types | **Sent back for a design** — see §S1.5 |
| **S1-D6** | Who owns the service list | **Sent back for a design** — see §S1.6 |
| **S1-D7** | Drop `category` | **RULED: dropped.** |
| **S1-D8** | Tenancy levels | **RULED (delegated to the stream): one scope column, `property_id`.** Organization and facility dropped. See §S1.7 — this is the one place the stream did not simply follow the owner's instinct, and the reason is on the record |

---

## S1.1 · What a job is about — the design

**The platform has already solved this, and it named Jobs while doing it.**

`masterdata.locations` is **one tree** covering every place in a property.
Buildings, floors and rooms are typed nodes with extension tables; corridors,
lobbies, restaurants, pools, plant rooms, terraces and back-of-house are
nodes and nothing more. `Location.cs`'s own remarks give the reason:

> *"The alternative — locations holding only the places the other tables
> cannot — puts a branch in every consumer: `if room: join rooms else: join
> locations`. The Context Service, **Work Orders**, Inventory and the AI
> Runtime would each carry it forever."*

So the design is two fields, and no branch anywhere:

```text
job.location_id   REQUIRED    WHERE the work is
                              a room · the pool · a corridor · the lobby
                              · a plant room · a whole floor · a whole block
                              — every one of them is a node in the same tree

job.asset_id      OPTIONAL    WHAT the work is on, when it is a thing
                              the lift · an air handler · a room's AC unit
                              — and every asset already knows its own location
```

Four rules that follow:

1. **`location_id` is always set.** There is no job without a place.
2. **If `asset_id` is set, `location_id` is seeded from the asset's location
   at creation and then stored on the job.** Not derived on read — assets are
   moved, and the job happened where it happened.
3. **A job may sit at any level of the tree.** *"Deep clean the whole second
   floor"* is a job on the floor node. This is free, because it is one tree.
4. **Nothing about the place is copied onto the job** — no room number, no
   floor name. "Room 214 · Second Floor · Main Block" is resolved through
   Context when the job is displayed.

**Two things the owner needs to answer, and one is not ours to decide:**

* The platform's place list today is: building · floor · room · corridor ·
  lobby · restaurant · pool · plant_room · terrace · back_of_house. **It has
  no gym, spa, salon, banquet hall, car park, garden, kids' club or laundry
  room.** The list is deliberately cheap to extend — its own comment says
  *"hotels invent places; a new type is a constraint edit"* — but it is
  **Master Data's list, not ours**. Which places are missing for your
  properties? That goes to the platform as a request.
* Should a job be allowed at building level, or is floor the coarsest
  sensible? (Recommendation: allow it; a generator room or a whole annexe
  block is a real subject.)

### The place types to request — owner approved, 2026-09-02

The kinds of place are **fixed in the platform** — a database CHECK
constraint built from a constant list, so a hotel cannot add one and a
property cannot invent one. That is deliberate, and the platform chose text
with a check rather than a database enum precisely so that adding a kind is
cheap: *"hotels invent places. A new type is a constraint edit, not a
migration with a lock in it."*

**What exists today:** building · floor · room · corridor · lobby ·
restaurant · pool · plant_room · terrace · back_of_house.

**To request of Master Data** — grouped so the ask is one change, not eight:

```text
guest-facing places      gym · spa · salon · banquet_hall · meeting_room
                         bar · kids_club · business_centre

outdoor and grounds      garden · beach · sports_court · driveway

service and support      kitchen · laundry_room · store_room · staff_area
                         parking · stairwell · service_area
```

Three notes on the list:

* **A lift is not a place, it is an asset.** It has a manufacturer, a serial
  number and a service history — `masterdata.assets`, sitting *in* a
  building. Adding it as a location kind would give it two homes.
* **`plant_room` already covers** generator, boiler and pump rooms; a
  separate kind for each would fragment the same idea.
* **The reception desk is in the lobby**, not beside it. `lobby` is enough.

This goes up as one request, alongside the property-code shape rule from
§S1.3. Neither blocks this round.

---

## S1.2 · How jobs relate to each other — the design

Owner, 2026-09-02, in two parts. **There are two different relationships, and
conflating them is what the reference did.**

```text
GROUP        these two happen to go together
             water, then a towel, same room, same guest
             peers · no dependency · closing one closes nothing

PARENT ▸ CHILD    this work is MADE OF these steps
             "make good the water leak in 214" is a plumbing fix, a
             carpet dry, and an inspection of the ceiling below
             a hierarchy · children can block each other
             THE PARENT CANNOT CLOSE UNTIL EVERY CHILD IS CLOSED
```

The reference has only one relationship — "affiliation" — and it closes the
children when the parent closes, which is the wrong direction for both cases.

### Part 1 · The group

A group, not a parent, for the water-and-towel case: delivering the water
does not deliver the towel, and neither is a step of the other.

```text
Group
 ├── KOC-HK-412   water    → Ramesh    Done 09:12
 └── KOC-HK-413   towel    → Ramesh    Open
```

* **Joins automatically** on: same requester + same location + inside a
  window (default 30 minutes, per property). Staff may link and unlink by
  hand, and that is recorded.
* **Carries the handler** — a job joining a group goes to the group's current
  handler *if that person is on shift and the department matches*. Water is
  In-Room Dining; a towel is Housekeeping. Where a property runs one runner
  for everything, that is a property setting.
* **One card on the runner's screen** — *"Room 214 — 2 items"* — so one trip
  is one trip.
* **Each job keeps its own status, SLA and completion. Closing one closes
  nothing else.**

### Part 2 · Parent and children

Owner's requirement, verbatim: *a parent has N children · the parent cannot
close until the children close · they may be parallel or sequential · we
assign anyone or one person · the behaviour may differ for each.*

**A child is a full job, not a checklist tick.** It has its own number, its
own type, its own service, its own department, its own assignee, its own SLA
and its own status. That is not decoration — a child that belongs to
Engineering must appear in Engineering's queue exactly like any other job,
be escalated on Engineering's policy, and be timed against Engineering's SLA.
A lightweight sub-task cannot do any of that.

```text
PARENT   KOC-HK-500   "Room 214 — water leak, make good"      Waiting on children
   │
   ├─ step 1   KOC-ENG-501   stop the leak              ENG   → Suresh    Done
   ├─ step 1   KOC-HK-502    move guest belongings      HK    → Ramesh    Done
   │                         ↑ same step number = run in PARALLEL
   ├─ step 2   KOC-HK-503    dry and deep clean         HK    → Ramesh    In progress
   │                         ↑ higher step = BLOCKED until step 1 is done
   └─ step 3   KOC-ENG-504   inspect the ceiling below  ENG   → Suresh    Blocked
```

**Parallel and sequential, with one field.** Each child carries a **step
number**. Same number = parallel. Higher number = waits for every lower
number to finish. That is the whole mechanism; there is no dependency graph
to draw and no cycle to detect.

**A blocked child is a real state, and its clock has not started.** It is
visible and it is assigned, but its SLA does not begin until it is released.
Otherwise step 3's deadline burns down while step 1 is still being worked —
which is how a system reports a breach against a person who was never able
to start.

**Closing.**

```text
parent is Done      when NO child is still open
                    (a child that was cancelled does not hold the parent)
parent is Closed    a person closes it — as for any job (see S4)
```

Closing never cascades downward. **Cancelling does** — cancelling a parent
cancels its open children with the parent's reason, because the work is no
longer wanted. That is the one direction where cascade is right, and it is
the opposite of what the reference does.

**Assignment — three modes, chosen when the parent is broken down:**

| Mode | Who gets the children | Use |
|---|---|---|
| **One handler** | every child to the same person | a small property, one runner |
| **By department** | each child routes normally to its own department | the default, and what makes multi-trade work possible |
| **Named per child** | the person breaking it down picks each | a supervisor who knows exactly who |

**Escalation — and this is the part that goes wrong if it is not decided
now.** Children escalate on **their own** clocks, against their own
department's policy. The parent escalates only on the **overall** deadline —
because the guest is still waiting even while every individual step is inside
its own SLA. **A parent's escalation names the child that is holding it up.**
Without that split you either get five alerts for one problem, or no alert at
all while three steps each sit just inside their limits.

### Where parents come from

1. **By hand** — a supervisor looks at a job and says *"this needs three
   trades"* and breaks it down.
2. **From a template** — *"banquet hall changeover"* is always the same six
   steps in the same order, to the same departments. This is where the value
   is operationally, and the reference has nothing like it.

### Part 3 · When HosPilot plans the children — **DEFERRED**, owner 2026-09-02

> **Deferred, not dropped.** The owner set this aside for now. It is kept here
> because the model must not preclude it, and because it is the strongest
> argument for two things this round decides anyway: **templates** and
> **children being full jobs**. Nothing is built for it in this round.

The owner's case: somebody tells the AI *"room 214 AC dead"*, and it should
**create the parent, check whether a guest is in the room, and plan the
children.**

This is not a separate feature. It is the same parent-and-children model with
a different author, and it settles two of the open questions above by
requiring them.

```text
"room 214 ac dead"
        │
        ├─ resolve the place           Context → location L3, Room 214
        ├─ is anyone staying there?    Context → occupied, checked out 11:00
        ├─ pick the plan               a TEMPLATE: "AC not cooling"
        └─ raise
              PARENT   KOC-ENG-118   AC not cooling — Room 214   Emergency
                ├─ step 1  diagnose the unit            ENG
                ├─ step 1  offer the guest a room move  FO     ← only because occupied
                └─ step 2  repair or replace            ENG
```

**Four rules this puts on the design:**

1. **The AI uses the ordinary way in.** Same API, same validation, same
   numbering, same permissions. There is no agent back door — an agent that
   can create a job the normal rules would refuse is a hole, not a feature.
2. **An agent-raised job says so.** `source` records HosPilot and the actor
   is the agent's identity, so a report can always separate *"jobs we raised"*
   from *"jobs the assistant raised"*, and a bad plan is traceable.
3. **The occupancy check is Context's, never ours.** Jobs does not learn who
   is in a room. And when GuestOps is not installed the check simply does not
   answer — the job is still raised, without the room-move step. *Absent is
   not blocking* (ADR 0116 §5).
4. **The plan is a template.** *"AC not cooling"* is a named breakdown with
   its steps, departments and step numbers. The agent chooses and fills a
   template; it does not invent a work plan from nothing. That is what makes
   the result reviewable — a supervisor can see *which* plan was used.

**Two things this changes in the questions above.** Template support (g) was
"later"; the AI case needs it, so it moves into the first release as a
**model and a seeded set**, with the editing screens still able to come
later. And a child being a **full job** (a) stops being a preference — the
"offer the guest a room move" step belongs to Front Office and must land in
Front Office's queue.

**The open question, and it is the important one.** The platform's AI chain
puts an **approval step in front of every AI write** (ADR 0130). So:

> Does an AI-planned job wait for a person before it exists?

The stream's recommendation, and it splits the two halves:

```text
the PARENT is created immediately        a dead AC at 02:00 must not wait
                                         for someone to approve recording it

the CHILD PLAN is proposed               the supervisor sees "HosPilot
                                         suggests 3 steps", and accepts or
                                         edits with one tap

no answer within N minutes               the plan accepts itself, and says it
                                         did — because at 02:00 nobody may be
                                         looking, and doing nothing is worse
                                         than doing the obvious thing
```

That needs the owner's ruling, and an architect's confirmation that it fits
ADR 0130's approval-for-writes.

### Part 4 · Depth and counting — RULED, owner 2026-09-02, checked against the field

The owner asked for these two to be checked against how modern systems
actually do it before being settled. They were, and both answers hold.

#### Depth — **one level. No grandchildren.**

| System | Depth below the work item |
|---|---|
| **Jira** | **one** — a sub-task cannot have a sub-task. Twenty years, deliberate; the levels they later added (Initiative, Epic) went *above*, never below |
| **ServiceNow** | **one** — Incident ▸ Incident Tasks · Change ▸ Change Tasks |
| **Salesforce Field Service** | **one** — Work Order ▸ Line Items |
| **SAP PM** | two, and they are **different objects** — order ▸ operation — not recursion |
| **IBM Maximo** | unlimited, and it is the one everyone describes as unmanageable |
| **Asana** | unlimited, and it is the most-complained-about thing in the product: work disappears three levels down |

The pattern is consistent: **where a second level exists it is a different
kind of object, not the same object nested.** The two products that allow
arbitrary depth are the two people complain about, for the same reason — a
job nobody can see is a job nobody does.

**And the modern direction is not deeper trees.** Linear, Height and Shortcut
all moved the other way: a flat list with **explicit order and dependencies**
rather than nesting. Which is precisely what the step number already is.

So: **one level**, with two escape valves so nothing is ever trapped:

```text
a child turns out to need breaking down
        → ADD MORE STEPS TO THE SAME PARENT
          the list gets longer, never deeper

it is genuinely separate work
        → a NEW JOB, linked as related  (the group relation, Part 1)
```

And **templates are how depth is reached without a deep tree**: the six steps
of a banquet changeover live in a named plan, not in a hierarchy somebody
built by hand at 22:00.

#### Counting — **two numbers, and never one that adds them**

| System | How it counts |
|---|---|
| **Jira** | velocity counts stories; **sub-tasks are excluded**; epics are tracked on their own burndown |
| **ServiceNow** | metrics are per task; the parent carries **its own** resolution time |
| **IBM Maximo** | cost and labour **roll up** to the parent; counts stay per work order, with an explicit include-children switch |
| **Zendesk** | side conversations never count as tickets |

The same rule everywhere: **count the unit of work once, at the level where
the work happens, and report the container separately as a container.**
Nobody adds them together.

For a hotel there are genuinely two questions, and they have two answers:

```text
"how much work did we get through?"     count LEAVES   84 pieces of work
"how many issues did we resolve?"       count ROOTS    61 guest issues
```

A standalone job with no children is **both a leaf and a root** — counted
once in each, and correctly in each.

**And the two clocks line up with the two counts**, which is what makes this
worth doing rather than merely tidy:

```text
the PARENT's clock     how long the GUEST waited        →  the GM's number
the CHILDREN's clocks  how fast each TEAM responded     →  the head of
                                                           department's number
```

**The rule, written down so no screen breaks it later: never display a single
figure that adds parents and children.** That number answers no question, and
it is the number a dashboard reaches for first.

### Open for the owner

| | Question | Recommendation |
|---|---|---|
| a | Is a child a **full job** (own number, own queue, own escalation) or a lightweight sub-task? | full job — your "different behaviour for each" requires it |
| b | **One level only**, or may a child have children? | **RULED: one level, no grandchildren** (owner, 2026-09-02, after the field check in Part 4) |
| c | When the last child finishes, does the parent **close by itself** or wait for a person? | it becomes **Done** by itself; a person **Closes** it |
| d | Does **cancelling a parent** cancel its open children? | yes, with the reason carried down |
| e | Reporting — parents, children, or both? | **RULED: two numbers — leaves for workload, roots for outcomes — and never one that adds them** (owner, 2026-09-02; Part 4) |
| f | Does the parent have **its own SLA**, separate from the children's? | yes — that is the guest's clock, and it is what the parent escalates on |
| g | **Templates** — in the first release, or later? | later, but the model must allow them from day one |
| h | Can a job be in a **group** and also be a **child**? | yes; they are independent relations, and neither affects the other |
| i | The group's join window — 30 minutes, or shorter? | **RULED: the group stays** (owner, 2026-09-02). The window length is still open |
| j | Should a group cross departments and still go to one person? | a property setting, **off** by default — see below |

**(j) in plain terms.** A guest asks for water and a towel. Water is Food &
Beverage; a towel is Housekeeping. Two departments.

```text
a large hotel      two people. The F&B runner carries water from one store,
                   the housekeeping attendant carries linen from another.
                   Sending one person to fetch both is not how it works

a small hotel      one person walks to the room once. Sending two is silly
or a villa resort
```

So the setting reads: *"may one person be handed a job from another
department when they are going to that room anyway?"*

```text
OFF (default)   each job goes to its own department, always
ON              whoever takes the first one gets both — but only if
                they hold permission for both departments
```
---

## S1.3 · The job number — the design

**Ruled format:** `<PropertyCode>-<Dept>-<Number>`. **One counter per
property, shared by every department** — confirmed by the owner, 2026-09-02:
*"if HK-01, then ENG-02, not ENG-01."*

```text
KOC-HK-01     towel to 214              Housekeeping
KOC-ENG-02    AC not cooling, 214       Engineering      <- 02, not 01
KOC-HK-03     turndown 305              Housekeeping
KOC-FB-04     water to 214              Food & Beverage
KOC-ENG-05    lift making a noise       Engineering
```

The department letters say **who it went to**; the number says **which job it
is at this hotel**. One counter means that when somebody says *"job 4"*, there
is exactly one job 4 — which is the whole reason not to give each department
its own counter.

**The property code already exists.** `masterdata.properties.Code` is there
today — *"Operator-facing — `kochi-001`. Never used for routing."* **No Core
Administration change is needed.** The owner's concern about group-level
reporting is exactly what it is for.

Three rules:

1. **The number is stamped when the job is created and never recomputed.** It
   is a label, not a projection. If a property is later renamed or recoded,
   old numbers keep the old prefix — which is correct: that job *was* raised
   at `KOC`.
2. **The department segment is the root canon code** — `HK`, `ENG`, `FB`,
   `FO`, `SEC` — not the leaf. The canon has 45 codes and a hierarchy
   (`PLUM` and `HVAC` sit under `ENG`; `LDY` and `PA` under `HK`). A number
   built from leaves is unreadable and changes meaning when a job is
   re-routed one level.
3. **Rerouting a job does not renumber it.** A job that starts as `HK` and
   turns out to be engineering keeps `KOC-HK-412` and *shows* Engineering as
   its department. A number that changes is worse than one that is stale,
   because people write them down.

### How the property code is configured — measured, and one conflict

Set in **Core Administration** when the property is registered. Measured in
`masterdata.properties`:

```text
required · unique within the organization · max 50 characters
MUST BE LOWERCASE   — enforced by a database constraint:
                      ck_properties__code_lowercase :  code = lower(code)
no shape or length rule beyond that
```

**The conflict, reported rather than worked around.** The owner asked to keep
the short **uppercase** convention (`KOC`, `GOA`). **The platform forbids an
uppercase property code** — it is a CHECK constraint, not a convention.

The resolution needs no platform change: **the stored code stays lowercase
(`koc`), and the job number upper-cases it when it is stamped** — giving
`KOC-HK-04`. The number is its own string written once at creation (§S1.3
rule 1), so nothing is derived on read and nothing disagrees.

**One small request to send up with the location types.** There is *no length
or shape rule* on a property code — `kochi-marine-drive-main` is legal today,
and would produce `KOCHI-MARINE-DRIVE-MAIN-HK-04`. A short-code rule (3–8
characters, letters and digits) would keep job numbers readable. Same class
of platform request as the missing place types, and equally small.

**Open for the owner:**
* What happens to a job raised before its department is known — a guest
  request that has not been routed yet? Options: hold the number until it is
  routed, or stamp a neutral segment (`GEN`) and never change it. The stream
  recommends **routing at creation always**, so the case does not arise —
  which depends on S1.6's catalogue carrying a default department per
  service.

---

## S1.4 · How priority is set — RULED, owner 2026-09-02

**Levels:** Emergency · High · Normal · Low, plus **Not triaged**.

**How it is decided** — the owner's chain, written out:

```text
1  a person chose one                         → that wins, and is recorded as manual
2  the guest flow decides it                  → needs PMS or GuestOps installed
3  the service's own default                  → always available
4  nothing matched                            → NOT TRIAGED, a real state
```

Where a property has **no PMS and no GuestOps**, layer 2 never fires and the
chain is exactly what the owner described: *a person chose it, else the
service default, else not triaged.* Nothing breaks and nothing waits — the
absent-neighbour rule (ADR 0116 §5) doing its job.

**Every job records which layer decided it**, so *"why is this Emergency?"*
always has an answer.

### Layer 2 — what "the flow" actually tells us

This is the layer worth building, and it is the one thing the reference could
never do. The room's place in the guest cycle changes what a fault *means*:

| The room is… | What it means | Effect |
|---|---|---|
| **Occupied, guest in house** | somebody is affected right now | raise it |
| **Arriving today, still vacant** | it must be right before check-in | raise it, and **the deadline is the arrival time**, not a fixed SLA |
| **Due out today** | the guest is leaving in hours | lower — unless the guest complained, and then it is not lower at all |
| **Vacant, nothing booked** | nobody is affected | normal or low |
| **Out of order already** | unsellable either way | planned work, not urgent work |

Two things fall out of that table which the reference cannot express:

* **A deadline can come from the flow, not from a fixed SLA.** *"AC in 214,
  arrival at 14:00"* is due at 14:00 — not in 45 minutes because the
  catalogue says 45 minutes.
* **A complaint from a departing guest is still a complaint.** Layer 2 lowers
  priority for a due-out room, and a guest actually complaining overrides
  that — because the guest is standing there.

**Two rules about the rules:**

1. **A human override always wins and always sticks.** Nothing re-runs later
   and quietly undoes it. Who changed it, and why, is recorded.
2. **Priority never drifts upward as a deadline approaches.** That is
   escalation's job. Mixing the two is exactly how the reference ended up
   with `ESCALATED` sitting inside its *status* field.

---

## S1.5 · What kind of job it is — a better design than the reference's

**The owner's direction, 2026-09-02:** *type and service are the Java
system's concepts; propose something better if there is one.* There is.

### What is actually wrong with `workOrderType`

The reference's type field is doing **three unrelated jobs at once**:

```text
1  gating features        a checklist is allowed only if type = MAINTENANCE
2  gating escalation      escalation is skipped if type = MAINTENANCE
3  classifying reports    "how many complaints this month"
```

Three different questions wearing one field. That is why it needs a special
case at the top of the escalation engine and another in the checklist
validator — and why adding a fourth type would mean finding every place the
third one is named.

### The better cut: **what will make this finished?**

A type should answer exactly one question — *what does "done" mean?* — and
nothing else. That gives **four**:

| Intent | The work is | Done when |
|---|---|---|
| **Deliver** | bring something, or perform a small service | it has been delivered |
| **Fix** | restore something to working order | it works again |
| **Check** | go and assess — the output is a **finding**, not a repair | the assessment is recorded |
| **Prepare** | make a place ready for use | it is ready, before the time it is needed |

### And the thing my own earlier proposal got wrong

Last round the stream proposed five, separating **Complaint** from **Fault**
— the same repair, but a complaint is not finished until somebody tells the
guest. That distinction is real. **It is not a type.**

*"Is somebody waiting for an answer"* is a **fact about the job** — whether
it has a requester — not a kind of work. So:

```text
a Fix with a guest requester      = what the reference calls a Complaint
a Fix with no requester           = what it calls a Fault
a Deliver with a guest requester  = a Request
anything raised by a schedule     = Planned  (that is the SOURCE, not the type)
a Check                           = an Inspection
```

**Every one of the reference's five is expressible, and none of them needs
its own type.** The closing rule — *tell the person who asked* — comes from
**there being a requester**, which is testable, rather than from a type
somebody remembered to set correctly.

**"Maintenance" disappears entirely**, and with it both special cases: an
unplanned repair is a **Fix**, scheduled servicing is a **Fix** whose source
is a schedule, and neither needs to be excluded from anything.

### The four intents, checked against the field

ITIL — which every service system in the table above implements — settles on
**four** as well, and three of them line up exactly:

```text
ITIL                        ours
Service Request   ────────  Deliver     bring something
Incident          ────────  Fix         restore it to working
Problem           ────────  Check       investigate; the output is a finding
Change            ────────  (not ours)
                            Prepare     make a place ready for use
```

**Two deliberate differences, and both are because a hotel is not an IT
department:**

* **`Change` is dropped.** ITIL's Change is *modify the environment under
  control* — a release, a firewall rule. A hotel does not raise work orders
  to change its own configuration.
* **`Prepare` is added, and it is large.** Making a room ready before an
  arrival, setting a banquet hall for an event, turning down a suite — this
  is a substantial share of a hotel's daily work and ITIL has no shape for
  it. Its "done" test is unlike the others: **ready by a time**, not fixed.

So four either way, and the two that differ differ for a reason that can be
stated. That is a better sign than matching ITIL exactly would have been.

### What is deliberately not a type

* **Incident** — an injury, a theft, a fire alarm. Different lifecycle,
  different confidentiality, a legal record, often no repair at all. An
  incident *causes* jobs; it is not one. Security's, when that application
  exists.
* **Guest / Staff / PMS / Scheduled** — that is `source`, already its own
  field.

### The recommendation that follows

**The type list is fixed in the product, not hotel-editable.** Every intent
needs behaviour written for it, and an intent nobody wrote behaviour for is
one that silently does nothing — which is precisely what happened to
`DEVICE` in the reference.

---

## S1.6 · The catalogue — a better design than the reference's

**The owner's direction, 2026-09-02:** same as S1.5 — propose something
better. There is, and it makes S1.5 simpler rather than adding to it.

### What is actually wrong with `WOServicePreference`

Two faults, and the second is the interesting one.

**One · the entry is also the policy.** A single document holds the service's
name **and** its department, its default assignee, its SLA, its priority, its
keywords, its icon and its tracking mode. So the *vocabulary* — which Room
Care, Maintenance and GuestOps all need — cannot be shared without also
sharing Jobs' *behaviour*.

**Two · one list is being asked to be two different things.**

```text
a MENU          towel · water · turndown · extra pillow
                something a person asks for, and somebody brings

a SYMPTOM LIST  AC not cooling · tap leaking · lift noisy
                something that is wrong, points at a kind of equipment,
                and has to be diagnosed before it is fixed
```

A menu item has a quantity and a delivery. A symptom points at an asset type
and needs a diagnosis. Holding both in one shape is why the reference's entry
has so many fields that are empty most of the time.

### The better design: one catalogue, and the entry carries its intent

Rather than a menu and a symptom list — or one shapeless list — **every entry
declares which of S1.5's four intents it is.** That single field is what lets
one list serve both purposes without either being bent.

```text
code                 TOWEL              AC_NOT_COOLING
name                 Extra towel        AC not cooling
INTENT               Deliver            Fix
owning department    HK                 ENG
guest may request    yes                yes
targets              —                  asset type: HVAC
```

**And then the job's type is not chosen at all — it comes with the entry.**

That is the real gain. Nobody picks *"Request"* and then *"towel"*; they pick
**towel**, and the system knows it is a Deliver, owned by Housekeeping, that
a guest may raise. A whole class of mistake — a delivery logged as a repair —
becomes unrepresentable rather than merely discouraged.

### Where each half lives

```text
THE CATALOGUE — what a job can be about         Core Administration, shared
    code · name · translations · icon
    INTENT (Deliver | Fix | Check | Prepare)
    owning department · guest-requestable · targets an asset type
    active / inactive

THE POLICY — what we do about it                Jobs' own, per property
    default priority · default SLA
    which escalation policy applies
    the auto-assignment rule
```

Room Care, Maintenance and GuestOps read the **catalogue**. Nobody but Jobs
reads the **policy**.

### Same in every hotel, or different? — both, and the platform has ruled this shape once already

The department canon (ADR 0119, ADR 0116 §4) solved the identical problem:

```text
the catalogue    organization-wide, shipped with the product
                 each property ACTIVATES what it offers
                 a property may RENAME for display — the code never moves
                 → a group's reports can never fragment

the policy       per property, always
                 Kochi's AC response time is not Goa's
```

The accepted cost is the same one the canon accepted: a hotel needing an
entry the catalogue lacks waits for it to be added centrally — **which is why
the canon shipped 45 departments rather than six, and why this list must ship
generously too.**

### The escape hatch, because a catalogue must not become a straitjacket

A guest says something nobody has listed. The rule:

* **"Something else"** is always available, with free text.
* It routes to a default department, is marked **uncatalogued**, and is
  **Not triaged** by definition — there is no entry to take a priority from.
* A supervisor can **promote** a frequent free-text into a real catalogue
  entry, and the platform can show *"you have raised 'mosquito net' 40 times
  this month"*.

The reference reached for the same need with a `keywords` fuzzy match on the
service name. This is the honest version of it: the gap is visible, counted,
and closable, rather than guessed at.

### Checked against the field — owner's instruction, 2026-09-02

The owner asked for the same treatment the depth question got: *is this
actually a better design, or just a different one — check how modern systems
manage it.* Checked. The proposal is the mainstream answer, and the check
produced two refinements it did not have.

#### One list, availability per site — not a list per site

| System | How the catalogue is scoped |
|---|---|
| **ServiceNow Service Catalog** | **one catalogue**, and items are made available per location, company or user group by *criteria* — never by duplicating the item |
| **Jira Service Management** | **request types** live centrally in a project and are exposed through portals |
| **Freshservice** | one service catalogue, categories, per-item visibility rules |
| **ITIL itself** | the Service Catalogue is defined as *a single authoritative list*, with a business view and a technical view. **The discipline exists because fragmented lists destroy reporting** |
| **Hotel systems** — Knowcross, Alice, Quore, hotelkit | ship a standard task list; a property enables and renames. Alice keeps brand-level templates above property lists |

Every one of them: **one list, scoped availability.** Nobody duplicates the
list per site, and ITIL names the reason out loud — it is the same reason the
platform's department canon is organization-wide.

#### The entry decides the behaviour — and this is not an invention

| System | The mechanism |
|---|---|
| **Jira Service Management** | a customer picks a **Request Type** ("I need a new laptop"); it maps to an **Issue Type** (Service Request) which carries the workflow. **The user-facing noun determines the internal type.** |
| **ServiceNow** | a Catalog Item carries the flow that runs when it is ordered |
| **Zendesk** | the ticket **form** decides the fields and the triggers |
| **Salesforce Field Service** | a **Work Type** carries estimated duration and required skills |

So *"nobody picks Request and then towel; they pick towel"* is exactly
Atlassian's request-type-to-issue-type mapping, and exactly ServiceNow's
item-to-flow. **The proposal is the industry's shape, arrived at
independently.**

#### Two refinements the check produced

**One · aliases on every entry.** The modern front door is a text box or a
voice, not a category tree. ServiceNow, JSM and Freshservice all carry search
synonyms on catalogue items for exactly this. So an entry carries its
**aliases**:

```text
AC_NOT_COOLING     aliases:  ac · a/c · aircon · air conditioning ·
                             not cooling · ac not working · room hot
```

That is what makes *"room 214 ac dead"* resolve to a real entry rather than
falling into free text — and it is the honest version of the reference's
`keywords` field, which was reaching for the same thing and used it for fuzzy
service matching instead.

**Two · duration and SLA are different things, and they split differently.**
Salesforce Field Service puts **estimated duration** and **required skills**
on the Work Type — the shared object — and that is right, because *how long
changing a lightbulb takes* is the same in Kochi and in Goa. But *how fast we
promise to do it* is a promise a property makes.

```text
CATALOGUE (shared)     how long the work TAKES      estimated duration
                       what it NEEDS                skills, parts, a checklist
POLICY (per property)  how fast we PROMISE          SLA
                       how important it is here     default priority
                       who does it here             routing · escalation
```

That split is sharper than the first draft, and it is what makes planning
possible later: a shift's capacity is the sum of durations, and no property
can distort it by editing its own SLA.

### The recommendation, with the evidence behind it

**One organization-wide catalogue · activated per property · renameable for
display · with the policy per property.** Four independent reasons, and they
do not depend on each other:

1. **ITIL defines a service catalogue as one authoritative list**, and names
   fragmentation as the failure it exists to prevent.
2. **ServiceNow, JSM and Freshservice all scope by availability, never by
   duplication.**
3. **The platform has already ruled this exact shape once** — the department
   canon (ADR 0119, ADR 0116 §4): a group-wide list, activated per property,
   renameable for display, the code never moving.
4. **The escape hatch removes the usual objection.** The standard complaint
   against a central list is *"my hotel needs something yours does not"*.
   "Something else" answers it: the gap is raised, routed, counted, and
   promotable into a real entry — so a hotel is never blocked, and the
   platform learns what it is missing rather than guessing.

### This still needs an architect's ruling

It puts a new object in Core Administration, and it defines a vocabulary
three other applications will read. It goes up as one of this round's
questions.

**Open for the owner, and it decides how hard that ruling is:** are your
properties' lists genuinely the same today, or does each hotel run its own?
If they already differ, activation-and-rename has to absorb that, and the
stream would want to see a real example of the difference before proposing
it.

## S1.7 · Tenancy — why one column stays

The owner's direction was to drop company, site and facility, because a
property runs its own local database. **Facility and organization are
dropped. `property_id` stays**, and this is the one place the stream did not
simply follow, so the reason is on the record:

* **The platform's own rule is one scope column, never none.** Every Master
  Data table is either property-scoped or organization-scoped — *"exactly one
  scope per master table… never both and never neither"*. `Property`'s own
  documentation is blunter: *"Every operational row carries `property_id`,
  every session is scoped to one, and a cross-property leak would be a
  serious breach — so it gets more than one defence: repositories filter
  explicitly, and Row Level Security backstops the query where someone
  forgot."*
* **The event bus needs it.** `EVT-Q4`'s ruling makes every subject
  property-scoped — `property.{id}.…` — with the id learned from the Kernel
  at registration. A job event with no property on the row cannot be
  published on the right subject.
* **Backup, restore and group reporting need it.** A property's data is
  restored into, and rolled up by, a system that holds more than one
  property. A row that cannot say which hotel it belongs to is a row that
  cannot be restored safely.

So: **one column, `property_id`. Not three.** Which is what the owner's
instinct was reaching for — the reference's three levels are two more than
anyone needs, and the third of them was never written at all.
## S1.8 · The owner's challenge — "this comes from the Java reference; you just enhanced it"

**Owner, 2026-09-02, on S1-D5 and S1-D6.** The challenge is correct and it is
taken here without qualification.

### What was inherited, and what was actually derived

| | Where it came from | Verdict |
|---|---|---|
| `location_id` on one tree | **the platform's Master Data**, whose own comment names Work Orders as the reason | derived from the platform, not from the reference |
| one counter per property | the reference numbers per *company*; the owner changed it | the owner's |
| the priority chain | the reference has a constant `5`; the flow layer exists nowhere in it | derived |
| parent ▸ children, step numbers, a blocked clock | the reference cascades the wrong way | derived |
| **the four intents** | **a narrowing of the reference's `workOrderType`** | **inherited shape, improved** |
| **the service catalogue** | **`WOServicePreference`, split in two and tidied** | **inherited shape, improved** |

The two the owner reopened are exactly the two that were **improvements to
the reference's shape rather than answers to the question the reference was
trying to answer.** And the field check that followed compared the *improved
version* against other systems — which tests whether the polish is
conventional, not whether the object should exist at all.

### The first-principles read: the catalogue is four different things

Asking *what is this list actually for* — once per intent — gives four
answers that are not variants of one thing:

```text
DELIVER   a MENU of what the hotel offers
          towel · water · extra pillow · turndown

          wants  quantity · sometimes a price · a stock consequence
          home   the offering / inventory model

FIX       a SYMPTOM LIST — and a symptom belongs to A KIND OF EQUIPMENT
          "not cooling" is a failure mode of an HVAC unit, not a
          hotel-wide menu entry
          wants  to hang off an asset type, with cause and remedy
          home   the asset model. This is exactly what Maximo's
                 failure-class hierarchy is, and it is per asset class
                 for precisely this reason

CHECK     a CHECKLIST DEFINITION — an ordered set of things to assess
          wants  steps · pass or fail · evidence
          home   a checklist object

PREPARE   a STANDARD — what "ready" looks like for this kind of place
          wants  steps, and a time it must be ready BY
          home   also a checklist object
```

**This is stronger than the earlier "menu versus symptom list" split**, which
noticed two of the four and then re-merged them under an `intent` field. That
field was papering over the seam rather than naming it.

### Why one list still ships, and what the claim becomes

**Three of the four homes do not exist.** There is no inventory application,
Maintenance is not built, and there is no checklist object. Four lists now
would put three of them inside Jobs — the exact mistake the platform has
ruled against twice: *a field does not live somewhere because its real owner
has not shipped yet* (ADR 0051, ADR 0056).

So the artefact is unchanged and **the claim about it is different**:

> **The `intent` field is not a classifier. It is the seam the list will
> split along.** `Fix` entries move to asset-type failure modes when
> Maintenance ships; `Check` and `Prepare` move to the checklist object when
> it exists; `Deliver` moves to the offering model when there is one. **A job
> already raised survives the split**, because it stored the resolved values
> — never a live pointer into a list that moved.

That is the same "stamp, do not derive" rule as the job number, applied to
the catalogue.

### What cannot be answered from here

**The four intents have never been tested against real hotel work.** They
were derived from *"what does done mean"* — a sound test — and they land near
ITIL. But the stream reached them from the reference and from other
industries, and neither of those is this owner's operation.

Four jobs the stream can already see do not fit cleanly:

```text
"wake-up call at 06:00"           an action at a moment; no artefact left
"escort the guest to the villa"   a service performed; nothing delivered
"guest left a watch"              custody and a record — not a repair,
                                  not a delivery
"move the guest to room 310"      as much a state change in the PMS as work
```

`Deliver` is being stretched to mean *"perform a service"*. That is the
signal that the cut is in the wrong place, or that a fifth intent exists.

**What settles this is data, not another framework.**

> **The stream is asking the owner for the real work list — twenty to forty
> job titles as they are actually raised at the properties today, in the
> operators' own words.**

The design is then tested against them in the open: each title placed, and
every one that does not fit named. Either the cut holds, or it moves. That is
the only honest way to answer *"is this better, or is it the Java concept
wearing new words"* — and it is not something the stream can invent.

**Until that list exists, S1-D5 and S1-D6 stay open and S1 is not signed
off.**

---

## S1.9 · WITHDRAWN — the stream over-engineered and mis-answered

**Owner, 2026-09-02:** *"still you are over-engineering. Each app's ownership
we already documented — **any jobs from anywhere are handled by the Jobs
app**. My question and what you answered are different."*

**Both corrections accepted. This section is withdrawn, not revised.**

### What was wrong

1. **The premise was factually wrong.** The stream proposed *"Jobs is the
   exception handler — Room Care owns the routine, Maintenance owns the
   planned"*. **That is not the design.** Application ownership is already
   documented, and **Jobs handles every job, whatever raised it** — a guest,
   a colleague, Room Care, Maintenance, a schedule, a sensor. Room Care and
   Maintenance decide *that work is needed*; the work itself is a job.
2. **It re-opened a settled boundary** instead of answering the question, and
   then asked the owner to re-rule it. The "where does Jobs end" question in
   that section is withdrawn with it.
3. **It was over-engineered.** A three-axis fact model replacing a type field
   is a bigger idea than the problem needs.

### What the question actually was

> *"Type and service came directly from the Java reference. We cannot mirror
> the Java design and architecture. Take the overall idea, concept and
> features — then redesign it in a better way. Sometimes add features,
> sometimes drop some."*

Not *"should these objects exist"*. They exist. **Take what the reference was
reaching for, keep the good, fix the broken, add what it lacks, drop what it
should never have had.** That is done in §S1.10 and §S1.11.

### What survives from the withdrawn section

Two things, and only because they are concrete rather than philosophical:

* **`PROVIDED` covers acts, not only items** — a wake-up call and an escort
  are jobs, and *"deliver"* does not describe them. Carried into §S1.10.
* **Aliases for search** — carried into §S1.11.
---

## S1.10 · Job type — the redesign

### What the reference has

```java
private String workOrderType;   // "COMPLAINT" · "REQUEST" · "MAINTENANCE"
private String category;        // free text — "PLANNED", "UNPLANNED", …
private String source;          // "PMS" · "FEEDBACK" · "HK" · "PPM" · "INSPECTION"
```

Three free-text fields, no constraint on any of them, used interchangeably by
different callers.

### What is broken, and it is not abstract

| | |
|---|---|
| **`type` is a `String`** | `"MAINTENANCE"`, `"Maintenance"` and `"maintenance"` are three types. Nothing stops any of them |
| **`type` gates behaviour by name** | `if (!"MAINTENANCE".equals(type))` in the checklist validator; `if ("MAINTENANCE".equalsIgnoreCase(type)) skip escalation`. Add a type and you must find every string comparison |
| **`category` overlaps `type`** | nobody can say which to use, so both get used |
| **The three types are not the same kind of thing** | Complaint and Request describe *who is unhappy*; Maintenance describes *what department does it*. Mixing those two axes is why `MAINTENANCE` needed two special cases |
| **No form or closure differences are expressible** | a complaint needs a complainant and a "we told them" step; the reference has neither |

### The redesign

**A fixed set of five, as an enum, chosen because each one has genuinely
different behaviour** — not because five is a nice number. If two values
behave identically they are one value.

| Type | Raised when | What is different about it |
|---|---|---|
| **REQUEST** | somebody wants something | has a **requester**; closing means *telling them* |
| **COMPLAINT** | somebody is **dissatisfied** | has a requester **and a recovery step**; feeds guest-satisfaction reporting; never auto-closes |
| **FAULT** | something is broken, nobody is waiting yet | may carry an **asset**; may take the room out of service (via Maintenance) |
| **TASK** | work sent by a person or another application | **no external requester** — this is how Room Care, Maintenance, a schedule or a sensor raise work |
| **INSPECTION** | go and assess | the output is a **finding**, not a repair; carries a checklist; may *raise* other jobs |

**`TASK` is the answer to *"any job from anywhere"***. Room Care saying *clean
214 after a spill*, Maintenance saying *service the chiller*, a schedule, a
sensor, an integration — all arrive as a `TASK`. Jobs does not care who
decided the work was needed.

### What changes against the reference

```text
ADDED     TASK          the reference has no way to say "work, no requester"
                        — Room Care and Maintenance work was crammed into
                        MAINTENANCE, which is a department, not a type
ADDED     INSPECTION    the reference has checklists and inspections and no
                        type for them; it gates them on MAINTENANCE instead
ADDED     COMPLAINT is distinct from REQUEST in BEHAVIOUR, not only in name
                        — a recovery step and a mandatory "we told them"
SPLIT     FAULT out of MAINTENANCE
                        "broken" is not the same as "engineering does it"
DROPPED   MAINTENANCE   it named a department, and departments are already a
                        field. Both of its special cases go with it
DROPPED   category      three overlapping classifiers become one
KEPT      source        it answers a different question — WHERE the job came
                        from, not WHAT it is. Now a closed list, not free text
```

### The rules that make it hold

1. **It is an enum, not a string.** No casing, no typos, no unknown values.
2. **Nothing branches on the type by name.** Behaviour hangs off the
   catalogue entry and the job's own fields — never `if type == X` scattered
   through the code. That is what made the reference's two special cases
   possible.
3. **A hotel cannot add a type.** Every value needs behaviour written for it,
   and a value with no behaviour silently does nothing — which is exactly
   what happened to `DEVICE` in the reference.
4. **The type is normally filled in by the catalogue entry**, not chosen —
   see §S1.11. A person picks *"extra towel"*, not *"REQUEST"*.

---

## S1.11 · The job catalogue — the redesign

### What the reference has

```java
@Document(collection = "work_order_service_preference")
class WOServicePreference {
    String service;                        // "Ac" — the DISPLAY NAME, and the KEY
    Set<String> keywords;
    String type; String departmentId; String department;
    AssigneeType assigneeType; String assigneeId;
    int priority; Integer sla; String trackMode; String icon;
    boolean sameForAllKeyword;
    HashMap<String,String> keywordAssignee;
    String companyId; String siteId;
}
```

And on the job itself, `service` is a `String`, rewritten on save by
`WordUtils.capitalize(trimToEmpty(service), ' ', '-', '.')`.

### What is broken, concretely

| | |
|---|---|
| **The display name is the key** | change "Ac" to "Air Conditioning" and every existing job's `service` no longer matches any entry. Routing, SLA and reporting all break at once, silently |
| **The key is *normalised* too** | what you typed is not what is stored. Two people produce two entries |
| **One flat list** | a hotel with 300 entries has an unusable picker and no way to group them |
| **`keywordAssignee`** | a `HashMap<String,String>` of keyword→person, propped up by a `sameForAllKeyword` flag. Routing written as a lookup table inside a config document |
| **No language** | one name, one language, for a floor with multilingual staff |
| **No scoping** | "extra towel" is offered for the lobby and the plant room |
| **Vocabulary and promise in one document** | Room Care cannot read *what things are called* without also reading *Jobs' SLA* |
| **Nothing about the work itself** | no duration, no skill, no parts, no "photo required" — so no planning is possible, ever |

### The redesign

**Two objects, and the split is along a line that has a test:**
*would this value be the same at another hotel in the group?*

```text
CATALOGUE ENTRY — what the thing IS            same across the group
    id            UUID                     the key. Never a name
    code          TOWEL_EXTRA              stable, for integrations and reports
    name          { en: "Extra towel",     display, RENAMEABLE, per language
                    ml: "…" }
    category      HOUSEKEEPING             groups the picker — one level only
    job type      REQUEST                  §S1.10 — filled in, not chosen
    department    HK                        who owns it by default
    aliases       towel · extra towel ·    search, and how free text resolves
                  more towels
    applies to    guest room · public area · asset type: HVAC
    guest may request      yes / no
    needs a checklist      yes / no
    photo on completion    required / optional / none
    skill required         (optional)
    typical duration       5 min           HOW LONG IT TAKES — the same anywhere

PROPERTY POLICY — what WE PROMISE about it     per property
    default priority · SLA · escalation policy
    auto-assignment rule
    chargeable · price
    active here            yes / no          this is "activation"
    display name override                    this is "rename"
```

### Every change against the reference, and why

```text
FIXED   the key is a UUID; code is stable; name is display and renameable
        → renaming an entry can never break a job, a report or a route.
          This is the reference's single worst defect and it is one line

FIXED   the name is no longer rewritten on save
        → what the operator typed is what is stored

ADDED   category, one level                     a 300-item flat picker is unusable
ADDED   name per language                       multilingual floors
ADDED   applies-to scope                        "extra towel" never offered for a plant room
ADDED   typical duration                        the first thing needed for capacity planning
ADDED   skill required                          real assignment instead of a stored person id
ADDED   photo on completion                     asked for constantly in hotel operations
ADDED   needs a checklist                       replaces gating checklists on type = MAINTENANCE
ADDED   chargeable + price                      late checkout, laundry, minibar — the reference
                                                cannot express money at all
ADDED   aliases                                 proper search; the honest version of `keywords`

DROPPED keywordAssignee + sameForAllKeyword     routing is a rule, not a lookup table
                                                buried in a config document
DROPPED assigneeId on the entry                 a stored person leaves, goes off shift, or is on
                                                leave. Routing resolves a ROLE at the moment of
                                                assignment
DROPPED trackMode                               belongs with tracking, not with the noun
MOVED   priority · SLA                          to the property policy — they are promises,
                                                and a promise is local
KEPT    icon                                    it earns its place on a picker
```

### One list or one per hotel — answered by the split, not by choosing

The split makes the question stop being a choice:

```text
the ENTRY      organization-wide          "Extra towel" means the same in every hotel
                                          → a group's report has one row, not four
the POLICY     per property               Kochi promises 15 minutes; Goa promises 30
activation     per property               Goa offers it; the city hotel does not
rename         per property, display only the code never moves
```

This is the department canon's shape (ADR 0119, ADR 0116 §4) — the platform
has ruled it once already for the identical problem.

### Free text is never blocked

A guest asks for something not in the list. The job is raised with free text,
routed to the category's default department, marked **uncatalogued**, and
**counted**. A supervisor can promote a frequent free text into a real entry
— and the platform can show *"'mosquito net' was raised 40 times this
month"*.

The reference's `keywords` fuzzy match was reaching for this and used it to
guess. Counting the gap is better than guessing at it.

### The name

**`service` is dropped as a word.** In a hotel "service" already means
hospitality, and a "service preference" is not a preference. The object is
the **job catalogue**, and an entry is a **catalogue entry**.

---

## S1.12 · The two statuses — owner's question, 2026-09-02

> *"How are we going to handle the status of a work order? We need two —
> **jobStatus** and **status** (to manage entity lifecycle: ACTIVE, DELETED,
> etc.)."*

**Agreed, and the reference already has both** — `WorkOrderStatus` and
`EntityStatus`. It gets both wrong, in different ways, and one of them has a
platform ruling waiting for it.

### What the reference has

```java
enum WorkOrderStatus { NEW, OPEN, ON_HOLD, IN_PROGRESS, ESCALATED,
                       WAITING, CLOSED, REMOVED }
enum EntityStatus    { ACTIVE(10), INACTIVE(20), CANCELLED(80), DELETED(100) }
```

Plus **five booleans on the job** doing the same work as the first enum:
`accepted`, `started`, `waiting`, `guestAcknowledged`, `reopened`.

What actually happens:

* `ESCALATED` and `REMOVED` **are never written by anything.**
* `EntityStatus` is set to `ACTIVE` at creation and **no endpoint ever
  changes it** — so `CANCELLED` and `DELETED` are unreachable.
* The five booleans disagree with the enum, because different code paths
  maintain each.
* `ON_HOLD` means three different things (§S4).

### Axis 1 · `job_status` — where the work is

Nine values, and each one earns its place by behaving differently:

```text
NEW           raised, nobody assigned — in the pool
ASSIGNED      given to a person or team, not yet taken up
ACCEPTED      the assignee has taken it on
IN_PROGRESS   work is happening; the timer is running
PAUSED        the assignee stopped — OUR delay. THE SLA CLOCK KEEPS RUNNING
ON_HOLD       blocked on something outside our control — a part, a guest's
              Do Not Disturb, an earlier step. THE SLA CLOCK STOPS
                                  … and the row carries the REASON
DONE          the work is finished, awaiting verification
CLOSED        verified and signed off                          final
CANCELLED     will not be done                                 final
```

**Why `PAUSED` and `ON_HOLD` are both needed** — this is the one pair worth
defending: the difference is **whose fault the delay is**, and therefore
whether the guest's clock keeps running. A technician on a break does not
stop the guest waiting. A missing spare part does. Collapsing them makes
every SLA report either too harsh or too kind, permanently.

**What is gone:** `OPEN` (it meant *assigned* and *reopened* and *not closed*
in different places) · `ESCALATED` (escalation is a fact **about** a job, not
a state **of** it — it is why the reference has an escalation status nothing
writes) · `WAITING` (renamed `ON_HOLD` with a reason) · `REMOVED` (that is
the second axis) · **and all five booleans.**

### Axis 2 · the record's own state — and here the platform has already ruled

**ADR 0062:**

> *"`active` is the canonical lifecycle flag. `deleted_at` is logical
> removal. There is no third lifecycle column and no `archived` state."*
> The verbs are **Deactivate / Reactivate** — never Archive / Restore.

```text
active = true,  deleted_at = null    Active
active = false, deleted_at = null    Inactive
deleted_at != null                   Deleted (soft)
row removed                          Purged
```

So **the platform's answer is not an enum** — it is a boolean plus a
timestamp. A `status` column holding `ACTIVE / INACTIVE / CANCELLED /
DELETED` is exactly the shape ADR 0062 removed.

**And for a job, one of the two is meaningless.** A job is never
*deactivated* and *reactivated* — that is for a room closed for renovation or
a staff member on long leave. So Jobs carries:

```text
deleted_at · deleted_by      soft deletion. Null means live
                             NO `active` flag — nothing would ever set it
```

### The distinction the reference gets wrong, and it matters

**`CANCELLED` belongs to the work, not to the record.** The reference puts it
in `EntityStatus` beside `DELETED`, as though they were neighbours. They are
not:

```text
CANCELLED   a BUSINESS OUTCOME. The job existed and we decided not to do it.
            The guest cancelled; the fault fixed itself; it was a duplicate.
            Fully reportable. It stays in every count of "raised", and
            "cancelled %" is a number a manager wants to see.

DELETED     an ADMINISTRATIVE ACT. The job should never have existed —
            raised in error, or removed under a data-protection request.
            Leaves every operational count. Recoverable by an administrator.
            Always audited: who, when, why.
```

Putting them in one enum makes *"we chose not to do this"* and *"this should
not exist"* the same fact. They are the two most different things on the job.

### The shape, in full

```text
job_status    enum, 9 values, an explicit transition table
              every change publishes a job.* event in the same transaction
deleted_at    timestamp, null = live
deleted_by    who did it
delete_reason required — a deletion with no reason is not auditable
```

Two more rules, both of which the reference breaks:

* **Every list and every count filters `deleted_at IS NULL` by default.** The
  reference's `EntityStatus` filter defaults to `ACTIVE` and is passed in by
  the *client*, so a caller can ask for deleted rows.
* **`job_status` never moves without a transition being legal.** The
  reference permits any value to any value, so `NEW → CLOSED` and
  `CLOSED → IN_PROGRESS` are both accepted today. The table is in §S4.

### One thing to confirm with the architect

ADR 0062 is written for **master entities**. A job is operational, not master
data. The stream is following its shape anyway — a platform that says
*"`active` + `deleted_at`, never a lifecycle enum"* should not have an
application inventing one — but **whether ADR 0062 binds an application's own
tables, or is only a convention there, is worth one line from the architect**
rather than assumed.


## Decisions — round 1 close

| id | Decision | Ruling |
|---|---|---|
| **S1-D1** | A job carries `location_id` (any node in the one tree) and an optional `asset_id`. The missing place kinds go up as one request | **RULED** (owner, 2026-09-02) — §S1.1 |
| **S1-D2** | One subject; a **group** for peers; **parent ▸ children** for a breakdown, with step numbers, blocked children, and no close until all children are done | *design proposed — §S1.2 — ten details open (a–j)* |
| **S1-D3** | `<PropertyCode>-<RootDept>-<Number>`, number property-wide, stamped once | **RULED** — two details open in §S1.3 |
| **S1-D4** | Emergency · High · Normal · Low · Not triaged, decided by: a person chose it → the guest flow (PMS/GuestOps) → the catalogue default → Not triaged | **RULED** (owner, 2026-09-02) — §S1.4 |
| **S1-D5** | **Deliver · Fix · Check · Prepare** — four intents, not five types. Complaint-vs-Fault falls out of *is there a requester*; "Maintenance" disappears with both its special cases | **REOPENED** by the owner, 2026-09-02 — the shape is inherited from the reference's `workOrderType`, and four real hotel jobs do not fit it. **REDESIGNED — §S1.10.** Five values as an enum, each earning its place by behaving differently: REQUEST · COMPLAINT · FAULT · TASK · INSPECTION. `MAINTENANCE` dropped (it named a department), `category` dropped, `source` kept as a closed list |
| **S1-D6** | One organization-wide catalogue, activated per property, renameable for display; the entry carries its **intent**, its **aliases** and how long the work **takes**, so the job's type is never chosen separately. The **promise** (SLA, priority, routing, escalation) is Jobs', per property. Plus a counted, promotable "something else" | **REOPENED** by the owner, 2026-09-02. The first-principles read says the list is **four different things**, and **REDESIGNED — §S1.11.** UUID key, stable code, renameable per-language name; one category level; applies-to scope; duration, skill, photo-on-completion, chargeable; aliases. `keywordAssignee`, `sameForAllKeyword`, the stored assignee and `trackMode` all dropped. Entry organization-wide, promise per property |
| **S1-D7** | `category` dropped | **RULED** |
| **S1-D8** | One scope column, `property_id` | **RULED** — reasoning in §S1.7 |

| **S1-D9** | **Two statuses.** `job_status` — 9 values with a transition table, replacing the reference's 8-value enum *and* its five booleans. Record state is **`deleted_at`, not an enum** (ADR 0062's shape), and **`CANCELLED` is a job outcome, not a record state** | *design proposed — §S1.12 — awaiting owner* |

**Sign-off:** **NOT SIGNED OFF.**

The stream marked this section signed off on 2026-09-02 and **the owner had
not signed it.** That was the stream's error, recorded here rather than
quietly corrected: a sign-off is the owner's act, and a page that records one
that did not happen is worse than a page with an open section.

Six decisions carry rulings — D1, D2, D3, D4, D7, D8. **D5 and D6 were
reopened** on the owner's challenge (§S1.8) and **redesigned from the
reference in §S1.10 and §S1.11**. §S1.9 is **withdrawn** — the stream
over-engineered it and got its premise wrong. **A ninth decision, S1-D9, is
added**: the two statuses (§S1.12), at the owner's request.

---

# S2 · Creating a job

**State: not started**

## What it does

Four ways in: the staff app, the guest app, an internal request screen, and a
message from another system. The service saves the job and then fires a
background message; a background worker fills in the defaults afterwards —
type, department, assignee, SLA, priority — from configuration.

## What's wrong

**1 · The job is handed back before it is finished.** The screen receives a
job with no priority, no SLA, no department and no assignee; a moment later a
background worker fills them in. Refresh quickly and you see a half-built
job. *(01 §3.1.)*

**2 · That worker can fail silently.** Every step is wrapped so errors are
logged and ignored. A job can sit with no assignee and no SLA indefinitely
and nothing flags it. *(01 §F8.)*

**3 · You cannot record work that already started.** If a technician began at
09:00 and the supervisor logs it at 10:00, the system forces the start time
to **one minute in the future**. Every retrospectively-logged job — which in
a hotel is most of them — has a wrong SLA clock. *(01 §F36.)*

**4 · Almost nothing is required.** Only the type is mandatory (and a service
unless the type is Maintenance). A job can be created with no location, no
description and no reporter.

## What I propose

* **One transaction.** The job is complete when it is created, defaults
  included. Anything that cannot be resolved is left visibly empty, not
  filled in later by a worker that may not run.
* **Backdating is allowed**, marked as logged-after-the-fact so reports can
  separate it, with the SLA measured from the real start.
* A short mandatory set, so a job is never useless: type, service, subject
  (room or asset), and who is reporting it.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S2-D1** | A job is complete when created — no background fill-in | yes | *open* |
| **S2-D2** | Is backdating allowed, by whom, and how far back? | yes; supervisor and above; 7 days | *open* |
| **S2-D3** | What is mandatory at creation? | type · service · subject · reporter | *open* |
| **S2-D4** | Which ways in do we support at launch? | staff app · another application by event · scheduled. Guest via GuestOps (see S8) | *open* |
| **S2-D5** | May a job exist with no assignee — an open pool the department picks from? | yes, this is the normal case | *open* |

**Sign-off:** _pending_

---

# S3 · Assigning it

**State: not started**

## What it does

A job goes to a person, a team, or a **device** (a shared tablet with shift
slots). Staff can also take an unclaimed job themselves ("capture").
Configuration can auto-assign by service.

## What's wrong

**1 · Assigning to a device crashes the background worker.** "Device" is a
first-class option with its own screens — and the assignment handler throws
on it. The error is swallowed. The assignment saves, but the clock, the
escalation setup and the notification never happen. *(01 §F12.)*

**2 · Reassigning wipes the state flags** — accepted, started, waiting are
all cleared, and different bits of code clear them again at other moments.
*(01 §F10.)*

**3 · Auto-assignment does not check whether the person still works there**,
is on shift, or is on leave. It writes whatever id the configuration holds.

## What I propose

* **Person or team only.** Drop the device idea entirely — a shared tablet is
  a *login*, not an assignee. Who did the work is the person signed in.
* **Reassignment is an event with a before and an after**, not a reset. The
  previous assignee's time stays attached to them.
* **Auto-assignment resolves through Workforce** — the person posted to that
  department, on shift now. If nobody is, the job stays in the pool and that
  is itself visible.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S3-D1** | Assignee is a person or a team — device dropped | yes | *open* |
| **S3-D2** | May a job be assigned to a *department* rather than a person — a queue? | yes; this is the pool | *open* |
| **S3-D3** | Keep self-assignment from the pool ("capture")? | yes | *open* |
| **S3-D4** | Keep auto-assignment by service configuration? | yes, but resolved through Workforce, not a stored id | *open* |
| **S3-D5** | Who may reassign — the assignee, the supervisor, or both? | supervisor always; assignee may hand back to the pool | *open* |
| **S3-D6** | Does reassignment reset the SLA clock? | no — the guest has been waiting since it was reported | *open* |

**Sign-off:** _pending_

---

# S4 · Accept, start, pause, finish

**State: not started**

## What it does

The assignee accepts the job, starts a timer, may pause, ends the timer, and
closes it. There is also a separate "waiting" state for when a job is parked
— waiting for a part, waiting for the guest to leave the room.

## What's wrong

**1 · The state is stored twice and the two copies disagree.** There is a
status (New / Open / On Hold / In Progress / Escalated / Waiting / Closed /
Removed) **and** five separate yes-no flags — accepted, started, waiting,
guest-acknowledged, reopened. Different code updates different ones.
*(01 §F10.)*

**2 · "On Hold" means three different things** — accepted-but-not-started, a
paused timer, and just-came-back-from-waiting. A report cannot separate them.

**3 · Two of the eight statuses are never written.** "Escalated" and
"Removed" exist in the list and nothing ever sets them.

**4 · Ending a timer leaves the job marked as still running.** Two pieces of
code write the same row in the same breath and the second undoes the first.
*(01 §F11.)*

**5 · Any status can jump to any other.** New straight to Closed. Closed back
to In Progress. There are no transition rules at all.

**6 · "Reopened" is counted two different ways**, and one of them trusts what
the phone app sent rather than what is in the database — so the reopen count
on a report is not reliable. *(01 §F9.)*

## What I propose

A single status with a written table of legal moves, and the three meanings
of "On Hold" separated:

```text
        New            raised, nobody assigned
         │
      Assigned         given to a person or team, not yet taken up
         │
      Accepted         the assignee has taken it on
         │
    In Progress   ←→   Paused        timer running / timer stopped
         │              Waiting      parked on something outside our control
         │
        Done            the work is finished
         │
       Closed           verified and signed off

     Cancelled          raised in error, or no longer needed  (from any state)
```

* **Done and Closed are different.** "The technician says it's fixed" and
  "the supervisor agrees it's fixed" are two facts, and hotels need both.
  Whether a hotel *requires* the second is a setting.
* **Waiting stops the SLA clock; Paused does not.** Waiting for a spare part
  is not the hotel's fault; a technician taking a break is.
* **Reopening** is one event with one definition, taken from stored state,
  never from what the client sent.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S4-D1** | The status list above — confirm or amend | as drawn | *open* |
| **S4-D2** | Do we separate **Done** from **Closed**? | yes, with a per-property setting for whether verification is required | *open* |
| **S4-D3** | Who may close — the assignee, or must a supervisor verify? | configurable; default supervisor verifies | *open* |
| **S4-D4** | Does **Waiting** stop the SLA clock, and does **Paused**? | Waiting stops it; Paused does not | *open* |
| **S4-D5** | Reopen — who may, within what window, and is it the same job or a linked new one? | supervisor and above; 7 days; same job, cycle counter increments | *open* |
| **S4-D6** | Cancel — who may, and is a reason mandatory? | supervisor and above; reason mandatory | *open* |

**Sign-off:** _pending_

---

# S5 · Escalation

**State: not started** · *the largest section, and the reference's most
decayed subsystem*

## What it is supposed to do

A guest reports the AC at 09:00. Nobody picks it up. Somebody senior should
find out — and if they do not act either, somebody more senior after that.

The reference has **four clocks**:

| Clock | Starts at | Fires if |
|---|---|---|
| **Not assigned** | the job is raised | nobody has been given it |
| **Not accepted** | it is assigned to a team | nobody on the team took it |
| **Not started** | somebody accepted it | they never began the work |
| **Not closed** | the SLA start time | the work is still not done |

Each clock has **five rungs** — in practice **Supervisor → HOD → Regional
Manager → Cluster → CRO**. Each rung says: *N minutes after the anchor, tell
these people, by these channels.*

**That structure is the best idea in the reference and I would keep it
whole.** Four clocks and a ladder is how hotels actually escalate.
Everything below is about the machinery around it, which is where it fails.

## How it actually runs today

```text
1. anything at all changes on the job — even a note
2. load the escalation policy from a JSON FILE ON THE SERVER'S DISK,
   named after the property
3. DELETE every scheduled escalation for this job
4. recalculate all the deadlines from scratch
5. schedule only the EARLIEST one as a timer, and push the rest into
   that timer's payload as a blob of JSON text
6. when it fires: run it, then re-schedule the next one from the blob
```

## What's wrong — one by one

**1 · The policy is a file on a disk.** To set escalation up for a new hotel,
somebody copies a file onto a server. Not a screen, not a database. Add a
second server and forget to copy the files, and that server escalates
nothing. *(01 §F5.)*

**2 · A missing file disables escalation in silence.** No file → one error
line in a log → the function returns → **that property has no escalation at
all**, and nothing in the product says so. You find out when a guest
complains that nobody came.

**3 · The "which services does this apply to" filter is backwards.** Leaving
the service list empty reads naturally as *"applies to everything"*. It
actually means **"applies to nothing"**. A policy written by someone who
skipped an optional field looks configured and does nothing. *(01 §F6.)*

**4 · Every change demolishes and rebuilds the whole chain.** Someone adds a
note at 11:00 and all scheduled escalations for that job are deleted and
recalculated. If the rebuild fails part-way — missing file, any error — the
old ones are already gone and the new ones never arrive. The job is now
silently unwatched.

**5 · Anything overdue is thrown away.** If the server was down 14:00–18:00,
every escalation due in that window is found to be in the past and
**dropped**, with an "info" log. Nothing records *"these forty jobs should
have been escalated and were not."* And the comment above that code claims it
deliberately drops missed triggers while the code one screen below is set to
**fire them immediately** — the comment and the code disagree. *(01 §F7.)*

**6 · Once an escalation has fired for a job it can never fire again.** The
duplicate check asks *"has this kind ever fired for this job?"* So: job
escalates Monday, gets closed, gets **reopened** Tuesday, then ignored for
two days — and it never escalates again, because it "already did".
*(01 §F35.)*

**7 · The pending escalations live inside a timer's payload.** They are a
JSON string in a scheduler row. So *"which jobs are about to escalate this
afternoon?"* is a question this system cannot answer. And because step 3
deletes the timer on every change, that queue is destroyed and rebuilt
constantly.

**8 · Recipient lists are split on the hyphen.** The code splits recipients
on comma, slash **and hyphen** — so any identifier containing a hyphen (a
UUID, for instance) is shredded into fragments, none of which exist.
*(01 §F25.)*

**9 · Roles are found by gluing strings together.** A supervisor is looked up
by taking the department's **display name** and appending `_SUPERVISOR`.
Rename "Housekeeping" to "Rooms" in the interface and that escalation
silently reaches nobody. *(01 §F32.)*

**10 · Two named people at one customer receive every escalation email in
production.** Two email addresses written into the source code, added to the
recipient list of every escalation at every property. *(01 §F24.)*

**11 · Night handling runs on India's clock, for everyone.** Between 20:00
and 08:00 — hard-coded **Asia/Kolkata** — the on-duty manager is added, and
only for the third rung. There is a `TODO` in the code saying to use the
property's timezone. *(01 §F22.)*

**12 · Maintenance jobs are excluded from escalation entirely**, by one
string comparison at the top of the function.

**13 · SMS and WhatsApp escalation are configurable and disconnected.** You
can switch on SMS escalation; the message is built; the line that sends it is
commented out. Nothing tells the hotel. *(01 §F20.)*

**14 · The SLA-pause arithmetic is patched in three places**, because a job
can leave "Waiting" through several doors and each patch catches what the
others missed.

**15 · Three of the four escalation designs in the code are dead.** A
Mongo-based engine with percentage-of-SLA triggers — its only caller is
commented out. A rules engine with conditions and actions — nothing calls it.
A fully documented ten-rule escalation matrix that exists only as a comment
on a class nobody creates. **Their configuration screens are all still
live** — an operator can build rules that will never run. *(01 §F21.)*

## What I propose

**Keep:** the four clocks, the ladder of rungs, per-service and per-type
filters, and the idea that the clock stops while a job is legitimately
waiting on something outside the hotel's control.

**Change, in order of how much it matters:**

**1 · The policy is data in our own schema, edited in the console.** Per
property, versioned, with a record of who changed it and when. Never a file.

**2 · A property with no policy is a visible state** — shown in the console
and in a health signal: *"Escalation is not configured for this property."*
Silence is never the answer.

**3 · Store the schedule as rows, not inside a timer.** One row per
*(job, clock, rung)* carrying its due time and its outcome —
**pending / fired / cancelled / missed**. Everything then becomes possible:

```text
"what is about to escalate this afternoon?"      a query
"what did we miss during Tuesday's outage?"      a query
"why did nobody hear about job 412?"             the row says so
the timer's only job                             wake me at the earliest pending row
```

**4 · Recompute, do not demolish.** A change recalculates the due times of
*pending* rows. Fired rows stay fired. Cancelled rows record why they were
cancelled — *"the job was assigned before the deadline"*.

**5 · A missed escalation is recorded as missed**, with its reason (platform
down, no policy configured). Whether we then fire it late is the owner's call
— see `S5-D4`. Recording it is not optional either way.

**6 · Deduplication is per cycle, not per lifetime.** Reopen the job and the
clocks start again.

**7 · Recipients are roles resolved at the moment of firing**, through
Workforce: *"who is the duty supervisor for Housekeeping at this property
right now?"* Never a list of ids in a config file, never a string built from
a display name, never a person's email address in the source. **If nobody is
posted to that role, that is itself an escalation** — go straight to the next
rung and say why.

**8 · The property's own timezone, always, with no fallback.** Night windows,
quiet hours and business days come from the property's calendar. A hotel in
Dubai does not run on Kolkata's clock.

**9 · The SLA clock is a list of intervals, not a running total.** Worked
09:00–09:30, waiting 09:30–11:00, worked 11:00–11:20. Sum it when asked. Then
*"why was this job late"* has an answer you can show a guest.

**10 · A channel you cannot deliver on is refused when you configure it**,
with a reason — not accepted and silently dropped at send time.

**11 · And the question underneath all of it: escalation may not belong to
Jobs.** Room Care, Maintenance and GuestOps will each want *"nobody acted
within N minutes, tell someone senior"*. If we build it inside Jobs, the
second application copies it and the two drift. My instinct is that this is a
**platform capability** and Jobs is its first user — but that is a decision
above this application, and it is `S5-D7`.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S5-D1** | Keep the four clocks — not assigned, not accepted, not started, not closed | yes, all four | *open* |
| **S5-D2** | How many rungs, and are the names fixed or per-property? | five rungs; names configurable per property, ladder shape fixed | *open* |
| **S5-D3** | Escalation policy stored in the database and edited in the console | yes | *open* |
| **S5-D4** | After an outage, what happens to escalations that were due? | fire the **highest** rung that was missed, mark the rest skipped, record all of them | *open* |
| **S5-D5** | Deduplication per cycle — a reopened job escalates again | yes | *open* |
| **S5-D6** | Recipients are roles resolved at fire time. What if nobody holds the role? | go to the next rung immediately and say why | *open* |
| **S5-D7** | Does escalation belong to Jobs, or is it a platform capability shared with Room Care, Maintenance and GuestOps? | platform — needs an architect ruling | *open* |
| **S5-D8** | Do escalations pause overnight, or continue through the night? | continue, but the *recipient* changes to whoever is on duty | *open* |
| **S5-D9** | Are Maintenance-type jobs escalated? (the reference excludes them) | yes, with their own policy — planned work has different deadlines | *open* |
| **S5-D10** | Which channels at launch? | email + in-app notification. SMS/WhatsApp only when genuinely wired | *open* |
| **S5-D11** | Can a *single job* be escalated by hand, outside the policy? | yes — a supervisor can escalate now, and it is recorded as manual | *open* |

**Sign-off:** _pending_

---

# S6 · Reminders

**State: not started**

## What it does

Two automatic kinds, and one manual.

**Waiting reminders** — when a job is parked until 15:00, it nudges the
assignee 5 minutes before, again at the time, then the supervisor 5 minutes
after, then the HOD 15 minutes after.

**Progress reminders** — once work starts, it nudges the assignee at 50 %,
75 % and 100 % of the SLA, then the supervisor at 125 %, then the HOD at
150 %.

**User reminders** — a member of staff sets their own reminder on a job.

## What's wrong

**1 · User-set reminders never fire.** They are created, saved and
scheduled — and the function that handles them is **empty**. The whole
feature exists except the part that does something. *(01 §F21.)*

**2 · The thresholds are hard-coded.** 50/75/100/125/150 and −5/0/+5/+15 are
in the source. No hotel can change them.

**3 · The timing uses the server's clock**, not the property's. *(01 §F22.)*

## What I propose

Keep both automatic kinds — the 50/75/100 ladder is genuinely useful and it
is the difference between a job being late and somebody noticing it is *going
to be* late. Make the thresholds configurable. Either build user reminders
properly or take the button away; a button that does nothing is worse than no
button.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S6-D1** | Keep waiting reminders, and are the offsets configurable? | keep; configurable per property | *open* |
| **S6-D2** | Keep progress reminders, and are the percentages configurable? | keep; configurable per property | *open* |
| **S6-D3** | User-set reminders — build them or drop them? | build, but not in the first release | *open* |
| **S6-D4** | Are reminders part of escalation, or separate? | separate — a reminder goes to the person doing the work, an escalation goes over their head | *open* |

**Sign-off:** _pending_

---

# S7 · Notifications

**State: not started**

## What it does

Four channels — email, in-app push, SMS, WhatsApp. Four layers of preference
decide who gets what: company → property → department → individual, plus a
separate set of preferences for guests.

## What's wrong

**1 · SMS and WhatsApp do not work.** Built completely — preferences,
templates, senders, a six-provider list, a fifteen-state delivery tracker —
and every "send" line is commented out. *(01 §F20.)*

**2 · Emails are not emails.** The system writes the message body to an HTML
file on the server's disk and sends **a link to that file**. Guest names,
phone numbers and job details sit in files on a web server, reachable without
logging in, never deleted. *(01 §F18.)*

**3 · The system keeps its own copy of every user's name, email and phone**,
taken once and never refreshed. Change your email in the main system and you
keep receiving mail at the old one. *(01 §F19.)*

**4 · An unresolvable user is addressed as "manager"** — literally that word,
in the message. *(01 §F4.)*

## What I propose

One notification path through the platform. Jobs says *"this happened, these
roles should know"*; the platform decides the channel and holds the
addresses. Jobs stores nobody's contact details — it asks, every time. An
email is an email.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S7-D1** | Which channels at launch? | email + in-app notification | *open* |
| **S7-D2** | How many preference layers do we actually need? | two — property default, and the individual's override | *open* |
| **S7-D3** | Jobs holds no contact details for anyone | yes | *open* |
| **S7-D4** | Who is notified on each event, by default? | needs a table from the owner — see the discussion | *open* |

**Sign-off:** _pending_

---

# S8 · The guest side

**State: not started**

## What it does

Guests raise jobs from a guest app, acknowledge that the work is done, follow
a public tracking page showing the stages, and leave a 1–5 rating with a
comment. A low rating alerts the on-duty hosts.

## What's wrong

**1 · The tracking page is completely broken and reports success.** Both of
its read operations crash on **every single call** — the real lookup was
commented out and the variable left empty with the code that uses it still
there. The controller catches the crash and returns **HTTP 200 OK** with the
error text in the data field. So no monitor ever noticed: the service looks
healthy while the page has never worked. *(01 §F2.)*

**2 · The "guest posts an update" endpoint does nothing and replies
"Sucesss"** — their spelling. The method body is empty.

**3 · A guest is authenticated by putting their id in a header.** No
password, no token, no credential of any kind. Knowing or guessing someone's
id signs you in as them. *(01 §F15.)*

**4 · There are two guest-satisfaction records** — a "rating" table and a
"feedback" table, both holding 1–5 and a comment. *(01 §F26.)*

## What I propose

**GuestOps owns the guest.** A guest request arrives as an event and becomes
a job; Jobs holds no guest identity, no guest login and no guest-facing page.
The tracking page and the rating are GuestOps' surfaces, reading the job's
public state through the platform.

The reason is not tidiness. Every guest-facing feature inside Jobs is a
second place that needs to know who a guest is, and the reference shows what
that costs.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S8-D1** | Does Jobs have any guest-facing surface at all? | no — GuestOps owns it | *open* |
| **S8-D2** | Where does the rating live — Jobs or GuestOps? | GuestOps; Jobs learns of it by event | *open* |
| **S8-D3** | Is a guest tracking page in scope for the first release? | not for Jobs; GuestOps' call | *open* |
| **S8-D4** | Does a guest acknowledgement affect the job's state? | it records a fact; it does not close the job | *open* |

**Sign-off:** _pending_

---

# S9 · Who can see what

**State: not started** · *most of this is platform law rather than our
choice, but two things are genuinely ours*

## What's wrong today

**1 · Reading, updating and patching a job has no tenancy check at all.** Job
ids are sequential numbers; change the number in the address and you read
another hotel's job. The correctly-scoped database query **exists in the code
and is never called once.** *(01 §F3.)*

**2 · Search takes the company id from the request body**, not from your
login. Send someone else's company id and you get their jobs.

**3 · Eighteen of the twenty-three screens' APIs are completely
unauthenticated.** A security rule was written without a path list, so it
silently matches everything remaining and permits it — including an endpoint
that lists work orders for whatever company id you type. *(01 §F16.)*

**4 · `admin` / `password` is in the source in three places** and it
satisfies the security check. The database password, the platform key and
three sets of message-broker credentials are committed too. *(01 §F17.)*

**5 · Sessions are cached for ten minutes.** Revoke someone's access and they
keep working for another ten minutes. *(01 §F14.)*

## What I propose

Every read scoped by your session, never by anything in the request. Every
operation through the platform's authorization. No credentials in the
repository. Jobs caches no security decision — it asks the platform, every
time.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S9-D1** | By default, whose jobs can a member of staff see — their own, their department's, or the whole property's? | their department's; supervisors see the property | *open* |
| **S9-D2** | Can a job be restricted — a complaint about a staff member, say? | yes, a restricted flag visible only to the raiser and management | *open* |

**Sign-off:** _pending_

---

# S10 · Scheduled and preventive work

**State: not started**

## What it does

*"Service the lift every first Monday at 06:00."* A schedule creates a job
each time it fires.

## What's wrong

**1 · If creating the job fails, the failure is swallowed and the schedule
moves on regardless.** A preventive job that was never created looks exactly
like one that was. *(01 §F8.)*

**2 · A schedule set for all seven days of the week can never run.** The
generated timing expression is invalid, the error is caught and logged, the
schedule saves, and it never fires — silently. *(01 §F34.)*

**3 · Every generated job carries the same fixed paragraph of English** as
its description.

## What I propose

This may not be ours at all. The platform's own list treats **Maintenance**
and **PPM** as applications separate from Jobs, and the field-ownership rules
put *"what maintenance does this asset need"* in Maintenance and *"what work
is being performed"* in Jobs.

My recommendation: **Maintenance decides what is due and announces it; Jobs
creates the job.** That keeps the schedule where the asset knowledge is, and
keeps Jobs as the place work is executed.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S10-D1** | Does Jobs hold schedules, or does Maintenance announce and Jobs create? | Maintenance announces; Jobs creates | *open* |
| **S10-D2** | Is scheduled work in scope for the first release of Jobs? | no — Jobs must be able to *receive* it, but not schedule it | *open* |

**Sign-off:** _pending_

---

# Lock

**The page is not locked.** It locks when all ten sections carry a sign-off,
and nothing is designed or built from it before then.

| | |
|---|---|
| Sections signed off | 0 of 10 |
| Page locked | no |
| Locked on | — |

When it locks, three things follow, in this order and no other:

1. every ruling recorded in the platform's question register;
2. an ADR for the decisions that change the platform rather than only this
   application — at minimum `S5-D7` (whose escalation is it) and `S1-D6`
   (who owns the service taxonomy);
3. the Jobs design chapter, written against the locked page.

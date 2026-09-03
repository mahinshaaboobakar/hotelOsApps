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
| S1 | The job itself — what a job *is* | **SIGNED OFF** | 2026-09-03 |
| S2 | Creating a job | **SIGNED OFF** | 2026-09-03 |
| S3 | Assigning it | **SIGNED OFF** | 2026-09-03 |
| S4 | Accept, start, pause, finish | **SIGNED OFF** | 2026-09-03 |
| S5 | **Escalation** | **OPEN** | — |
| S6 | Reminders | not started | — |
| S7 | Notifications | not started | — |
| S8 | The guest side | not started | — |
| S9 | Who can see and manage a job | **DESIGNED** — owner's four levels, 5 decisions | — |
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

**State: SIGNED OFF — owner, 2026-09-03.**

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

---

## S1.13 · The redesign, hotel domain first

> **Renamed 2026-09-03 (§S1.14 ·1):** *issue › symptom* became **category › item** — "issue" does not fit a request for water. The identifiers below carry the new names; the prose keeps the old words where it quotes the reference.

**Owner, 2026-09-02:** *"Redesign it, based on our hotel domain. Take concepts
from the reference."* An interview was offered instead and declined — the
owner wants the design, not questions. Written here.

### What the reference was reaching for — the concepts worth keeping

Strip the Java shape away and `workOrderType` + `WOServicePreference` were
trying to do three things:

```text
1  let a non-expert raise work without knowing who does it
2  get the work to the right department, with the right urgency
3  make it countable afterwards
```

Those three are kept. **Everything about how the reference did them is
replaced.**

### How a hotel actually thinks about work

Not in "types". A hotel has **departments** — that structure already exists
in Master Data and every operator knows it. Inside a department, a hotel has
**issues** it deals with every day: the AC is not cooling, the guest wants a
towel, the lift is stuck, room 214 needs a deep clean. And every issue is
either **guest-facing** — a guest is waiting or affected — or **back of
house**.

That is the whole vocabulary an operator uses. The redesign uses it and
nothing else.

### The design

```text
JOB
  where         location_id                              (settled, S1.1)
  what          issue                                    <- replaces "service"
  who does it   department        derived from the issue, overridable
  for whom      guest (a stay)    optional. Present = guest-facing
  how urgent    priority          from the flow           (settled, S1.4)
  service failure?      glitch flag + recovery fields     <- replaces "COMPLAINT"
  how it was resolved   resolution action                 <- NEW; the reference has nothing
  where it came from    source    guest app · front desk · Room Care ·
                                  Maintenance · schedule · sensor · integration
```

**There is no `type`.** Every job the reference typed is expressible:

```text
reference               here
COMPLAINT               an issue + a guest + the glitch flag
REQUEST                 an issue + a guest
MAINTENANCE             an issue whose department is ENG
a Room Care task        an issue whose department is HK, no guest
an inspection           not a job — a CHECKLIST RUN that raises jobs (below)
```

### The issue list — the reference's "service list", rebuilt for a hotel

**Organised by department, because that is how a hotel is organised.** Not a
flat list of 300, not an abstract "category".

```text
ENGINEERING
  AC              not cooling · noisy · leaking · not switching on
  Lighting        bulb out · flickering · switch broken
  Plumbing        tap dripping · no hot water · drain blocked · WC not flushing
  Lift            stuck · noisy · not stopping level
HOUSEKEEPING
  Extra items     towel · pillow · blanket · toiletries       (guest-requestable)
  Room condition  not cleaned · smell · stain · insects
  Deep clean      after spill · after checkout · periodic
FRONT OFFICE
  Room move · Late checkout · Luggage · Wake-up call
IN-ROOM DINING
  Water · Tea/coffee · Ice · Order follow-up
```

Two levels: **category › item**. "AC" is the issue; "not cooling" is the
symptom. The symptom is what routes to the *right* engineer and what makes
the fault history useful — "this unit has failed *not cooling* four times"
is a sentence only a symptom level can produce.

Each issue carries:

```text
department                who does it — the hotel's own structure
guest-requestable         appears in the guest app, or staff-only
standard resolution time  the hotel's own phrase — not "SLA"
applies to                guest room · public area · asset type
resolution actions        the closing vocabulary — see below
aliases                   so "aircon", "a/c", "ac not working" all land here
```

### The concept the reference does not have: the resolution action

The reference closes a job with a status and free text. **A hotel needs to
know what fixed it.** Every issue carries its own short list of resolution
actions, and closing picks one:

```text
AC · not cooling         filter cleaned · gas recharged · thermostat replaced ·
                         unit replaced · no fault found · referred to vendor
Plumbing · tap dripping  washer replaced · tap replaced · tightened
Extra items · towel      delivered
```

This is where the value is, and the reference has none of it:

* **"No fault found"** becomes visible and countable — the single most
  useful number in engineering.
* **"Referred to vendor"** is a resolution, not a dead end.
* **Asset history becomes real** — an AC unit with *gas recharged* three
  times this year is a unit that needs replacing, and nobody has to read
  free text to know it.
* **The closing is structured** — a technician taps one line instead of
  typing, and a manager can report on it.

The concept is HotSOS's *Issue × Location × Action* — the core model of the
largest hotel work-order product — and it is the part worth taking.

### The concept the reference mis-filed: the service failure

The reference made `COMPLAINT` a **type**, which meant the same broken AC was
a different kind of job depending on who noticed. **A complaint is not a kind
of work. It is a fact about a guest.**

So: any job with a guest attached may be marked a **service failure** (hotel
word: a *glitch*). The flag brings its own fields:

```text
glitch             yes / no
recovery owed      apology · upgrade · compensation · manager visit
recovery done      by whom, when
guest satisfied    yes / no / not asked
```

The **work** — fixing the AC — is the job. The **failure** — the guest was
let down — is recorded on it and rolls up to guest-satisfaction reporting.
One job, both facts, no type.

### Inspections are not jobs — they raise jobs

A supervisor inspecting ten rooms, an engineer's daily round, a fire-safety
walk: these are **checklist runs**, and a failed line **raises a job**
("214 · Lighting · bulb out"). The run is a small separate object. A one-off
*"go and look at 214"* is just a job whose issue is *Inspect*.

The reference typed inspections as jobs and gated its checklist feature on
`type = MAINTENANCE`. Neither survives.

### Worked example — one guest, one evening

```text
19:40  guest calls: "AC not working, and I need extra towels"

  KOC-ENG-441   issue AC › not cooling · room 214 · guest Mr Rao (stay 8812)
                department ENG · priority HIGH (occupied, complaint) · glitch: yes
  KOC-HK-442    issue Extra items › towel · room 214 · guest Mr Rao
                department HK · priority NORMAL
                -> grouped with 441 (same guest, same room, 30-min window)

20:05  HK delivers        442 resolved: delivered
20:30  ENG resolves       441 resolved: gas recharged
                          recovery: fruit platter sent · manager visited 20:45
                          guest satisfied: yes

Next morning:
  ENG · AC · not cooling · gas recharged      <- asset 214-AC now has 3 this year
  glitches 1 · recovered 1 · satisfied 1
  HK guest requests 1 · average 25 min
```

No type was picked at any point. Every number a manager wants exists.

### How the concept is kept — the data, owner's question 2026-09-03

**Owner:** *"I liked this concept. On resolve, ask for the resolution list
based on the symptom, plus a plain text box. In future we predict an AC needs
replacing when the same symptom and the same resolution recur. My doubt is
how we keep this concept."*

**Answer: the vocabulary lives in the catalogue; the job stores three ids.**
Everything that can be renamed, reordered or extended lives in the
catalogue. Everything that must never change lives on the job.

```text
THE CATALOGUE — the vocabulary, editable, group-wide

  issue            id · code AC · name {en,…} · department ENG ·
                   guest_requestable · applies_to (room | public area |
                   asset_type HVAC) · standard_resolution_minutes · active

  symptom          id · category_id → AC · code NOT_COOLING · name · aliases · active

  resolution       id · code GAS_RECHARGED · name
                   category_id     → AC        (null = universal: "no fault found",
                                              "referred to vendor", "other")
                   item_id   → optional  (null = every symptom of that issue)

THE JOB — three ids, stamped once, never derived

  category_id         → AC
  item_id       → not cooling
  asset_id         → 214-AC            (Master Data; when the issue is about a thing)
  location_id      → room 214
  …
  resolution_id    → gas recharged     (null until resolved)
  resolution_note  free text           the plain text box — always available
  resolved_at · resolved_by
```

**Ids, never names.** Rename "AC" to "Air Conditioning", reorder the
symptoms, retire a resolution — every job ever raised still points at the
same thing. That is the reference's worst defect (§S1.11) not recurring.

### On resolve — what the technician sees

```text
Resolve KOC-ENG-441   AC › not cooling

  ( ) filter cleaned          ← resolutions for AC, symptom "not cooling" first
  ( ) gas recharged
  ( ) thermostat replaced
  ( ) unit replaced
  ( ) no fault found          ← universal
  ( ) referred to vendor      ← universal
  ( ) other                   ← universal; makes the note MANDATORY

  Note  [                                          ]   ← always there
```

Two rules:

* **The list is per issue, filtered by symptom.** A resolution mapped to a
  symptom shows first; one mapped only to the issue shows after; universal
  ones last. One table, no duplication.
* **"Other" is counted.** A note under "other" that appears often is promoted
  into a real resolution — the same loop as an uncatalogued issue. The list
  grows from what technicians actually type, not from what somebody guessed
  in a workshop.

### The prediction — a query, not a model

```sql
SELECT asset_id, item_id, resolution_id, count(*)
FROM   jobs.jobs
WHERE  resolved_at > now() - interval '12 months'
GROUP  BY asset_id, item_id, resolution_id
HAVING count(*) >= 3
```

*"214-AC · not cooling · gas recharged · 3 times in 12 months"* is a row of
that result. It exists on day one because the three ids exist on day one.

Later, a small **recurrence rule** table makes it a recommendation rather
than a report:

```text
issue AC · symptom not cooling · resolution gas recharged · 3 in 12 months
      → "replacement candidate"
issue Plumbing · any symptom · any resolution · 5 in 6 months
      → "inspect the riser"
```

That table is Maintenance's when Maintenance exists; until then it is a Jobs
report. Nothing on the job changes either way.

### The asset — what makes the prediction about a *unit* and not a room

The recurrence is only as good as `asset_id`. So:

* When an issue's `applies_to` is an asset type and the location holds
  **exactly one** asset of that type, it is attached automatically. Room 214
  has one AC unit; "AC not cooling in 214" gets `asset_id = 214-AC` without
  anyone choosing.
* **Several** of that type in the location → the technician picks at
  resolve time (a plant room with four pumps).
* **None registered** → the job carries the location only, and the
  recurrence works per location — weaker, still useful, and it tells
  Maintenance which assets to register.

### The full flow, as data — owner's request, 2026-09-03

One job, start to finish. Only the fields that matter for the concept.

**0 · What exists before anyone calls — the catalogue**

```json
{ "category":      { "id": "I-AC",  "code": "AC", "name": "AC", "department": "ENG",
                  "applies_to": "asset_type:HVAC", "standard_minutes": 45 },
  "items":   [ { "id": "S-NC", "category": "I-AC", "code": "NOT_COOLING", "name": "not cooling",
                    "aliases": ["ac not working", "room hot", "aircon"] },
                  { "id": "S-NZ", "category": "I-AC", "code": "NOISY",       "name": "noisy" } ],
  "resolutions":[ { "id": "R-FC", "category": "I-AC", "item": "S-NC", "name": "filter cleaned" },
                  { "id": "R-GR", "category": "I-AC", "item": "S-NC", "name": "gas recharged" },
                  { "id": "R-UR", "category": "I-AC", "item": null,   "name": "unit replaced" },
                  { "id": "R-NF", "category": null,   "item": null,   "name": "no fault found" },
                  { "id": "R-OT", "category": null,   "item": null,   "name": "other" } ] }
```

**1 · 19:40 — the guest calls. Front desk types "ac not working 214".**
The alias resolves it to AC › not cooling. Room 214 has one HVAC asset, so
it attaches itself. Mr Rao is in the room, so it is guest-facing and the
flow sets priority.

```json
{ "job_id": "…uuid…", "job_number": "KOC-ENG-441",
  "location_id": "L-214", "asset_id": "A-214-AC",
  "category_id": "I-AC", "item_id": "S-NC",
  "department": "ENG", "guest_stay_id": "STAY-8812", "raised_via": "STAFF_APP",
  "priority": "HIGH", "priority_decided_by": "FLOW",
  "glitch": true,
  "job_status": "NEW", "deleted_at": null,
  "raised_at": "19:40" }
```

**2 · 19:42 → 20:05 — routed and worked.** Only `job_status` moves; each
move is an event.

```json
{ "job_status": "ASSIGNED",    "assignee": "Suresh", "at": "19:42" }
{ "job_status": "ACCEPTED",    "at": "19:44" }
{ "job_status": "IN_PROGRESS", "at": "19:58" }
```

**3 · 20:30 — Suresh resolves.** The screen lists resolutions for AC,
"not cooling" first, universals last, and a note box. He taps one and types.

```json
{ "job_status": "DONE",
  "resolution_id": "R-GR", "resolution_note": "low gas, recharged. check again in a month",
  "resolved_by": "Suresh", "resolved_at": "20:30",
  "recovery": { "owed": "fruit platter", "done_by": "Duty Manager", "done_at": "20:45",
                "guest_satisfied": true } }
```

**4 · 20:50 — the supervisor closes it.**

```json
{ "job_status": "CLOSED", "closed_by": "Priya", "closed_at": "20:50" }
```

**5 · Next morning — the report reads the ids, never the names.**

```json
{ "asset_id": "A-214-AC", "item_id": "S-NC", "resolution_id": "R-GR",
  "count_12_months": 3 }
```

→ *"214-AC · not cooling · gas recharged · 3 times this year — replacement
candidate."* If "AC" were renamed "Air Conditioning" tomorrow, this row is
unchanged.

**What was never on the job:** a type · a category · the word "AC" ·
the word "gas recharged" · an SLA. All of those are looked up from the ids
when displayed.

### Against the reference, in one table

| Reference concept | Here |
|---|---|
| `workOrderType` — Complaint / Request / Maintenance | **gone.** Department + guest link + glitch flag cover every use |
| `service` — a flat string list, display name as key | **category › item**, two levels, organised by department, UUID-keyed, aliases |
| `category` | gone |
| `source` | kept, closed list |
| routing baked into the entry (assignee id, keyword map) | **department on the issue**; the person is resolved by S3's rules at assignment time |
| priority + SLA on the entry | **standard resolution time** on the issue; priority from the flow (S1.4) |
| status + free text on close | **resolution action** — structured, per issue, reportable |
| complaint as a type | **glitch flag + recovery fields** on any guest-facing job |
| inspection as a type; checklist gated on MAINTENANCE | **checklist run** — a small separate object that raises jobs |
| `trackMode` | dropped from the issue; guest tracking is GuestOps' surface (S8) |

**Added:** the symptom level · resolution actions · glitch and recovery ·
checklist runs · guest-requestable per issue · aliases · applies-to.
**Dropped:** type · category · assignee-on-the-entry · keyword routing ·
trackMode.

### What is the owner's to decide

| | | Recommendation |
|---|---|---|
| **a** | Is the word **issue** right for both faults and requests? | it is the hotel-industry word (HotSOS); rename freely |
| **b** | Is the issue list **group-wide with per-property activation**, as departments are? | yes — same reason, same ruling shape |
| **c** | Does the **glitch / recovery** record live on the job, or in GuestOps with a link? | on the job for v1; GuestOps reads it |
| **d** | Are **resolution actions** mandatory on close, or optional? | mandatory for ENG issues, optional for HK requests — set per issue |
| **e** | Is a **checklist run** in the first release? | no — but a job must be raisable *from* one when it exists |

---

## S1.14 · Round 2 — the owner's seven points, 2026-09-03

### 1 · The word — "issue" is wrong for a request; the pair is **category › item**

*"Someone asks for water — we can't say that is an issue and a symptom."*
Correct. The catalogue needs a pair of words that read naturally for a fault
**and** a request. **Category › Item** is the plainest pair, and it is
literally what the picker shows:

```text
category            item
AC                  not cooling · noisy · leaking · filter change (PPM)
Plumbing            tap dripping · no hot water · drain blocked
In-room dining      water · tea/coffee · ice
Extra items         towel · pillow · blanket
Room move           guest request · upgrade · noise
```

*"AC, not cooling"* and *"In-room dining, water"* both read as a category and
an item. The job stores `category_id` and `item_id`. `resolution` keeps its
name. **§S1.13 is read with these words** — its `issue`/`symptom` identifiers
are renamed there; nothing else about it changes. The owner may rename the
pair again; the ids do not care.

### 2 · Statuses rephrased, and every person is an id

The time log (point 3) makes *paused* a fact about the **worker**, not the
job — Suresh's tea break does not change what state the job is in. So
`PAUSED` leaves `job_status` and lives in the time log. Eight remain:

```text
RAISED        created; nobody has it yet                          clock runs
ASSIGNED      given to a person or a team                         clock runs
ACCEPTED      the assignee has taken it up                        clock runs
IN_PROGRESS   work has begun — a session exists, running or paused   clock runs
ON_HOLD       blocked by something outside our control            CLOCK STOPS
              a part · guest DND · an earlier step — reason required
RESOLVED      a resolution has been recorded                      clock stops
CLOSED        verified by a supervisor                            final
CANCELLED     will not be done — reason required                  final
```

`DONE` → **`RESOLVED`**, because that is the act (§S1.13). `NEW` →
**`RAISED`**. Every person on every table is **`user_id`** — Master Data's
staff id, never a name: `created_by_user_id`, `assigned_to_user_id`,
`assigned_to_team_id`, `resolved_by_user_id`, `closed_by_user_id`,
`changed_by_user_id`. Names are looked up for display, as catalogue names are.

### 3 · Live tracking of work — the time log

The reference's `WorkOrderTimeLog` had the right idea and left
`started=true` after every timer ended (01 §F11). Rebuilt as sessions:

```text
job_work_session
  id · job_id · user_id
  started_at
  ended_at         null  =  RUNNING NOW
  end_reason       PAUSE · STOP · REASSIGNED · AUTO_STOPPED
  minutes          computed at end, stored
```

```text
START     opens a session; job → IN_PROGRESS if not already.
          If this user has a session running on another job, that one is
          paused first — one person works one job at a time.
PAUSE     closes the session, reason PAUSE. Job stays IN_PROGRESS.
RESUME    opens a NEW session. The old row is never reopened.
STOP      closes the session, reason STOP, and opens the resolve screen.
          Resolving also closes any session still open — nothing is left
          running by accident.
```

**The live board is a query:** every session with `ended_at IS NULL` is
*someone working right now* — who, on what, where, since when. Always
correct, because it is not a copy of anything.

**Derived, never stored on the job:** labour minutes = `SUM(minutes)`;
running now = any open session; worked by = distinct `user_id` — two people
on a lift job are two rows, not an `executedById` column.

**Two clocks, kept apart.** The *SLA clock* follows `job_status` and stops
only on `ON_HOLD`. The *labour clock* is the sessions. A tea break pauses the
second and not the first. The reference had one clock trying to be both.

### 4 · Not one wide table — split by what each thing is

The reference's `work_orders` has 40 columns and five satellite tables split
by nothing in particular (two identical contact tables; followers in two
places). Split by **logic** — one table, one fact:

```text
jobs.job                    the job itself — what, where, for whom, priority, status
    job_id · number · property_id
    category_id · item_id · asset_id · location_id
    guest_stay_id · source · priority · priority_decided_by · glitch
    job_status · scheduled_for · created_by_user_id · created_at
    reopen_count · deleted_at · deleted_by_user_id · delete_reason

jobs.job_assignment         who it was given to — a history, not a column
    job_id · assigned_to_user_id | assigned_to_team_id · assigned_by_user_id
    assigned_at · ended_at · end_reason (REASSIGNED · HANDED_BACK · RESOLVED)
    assignment_mode (MANUAL · AUTO · INHERITED)

jobs.job_status_history     every transition — the audit trail
    job_id · from_status · to_status · changed_by_user_id · at · reason

jobs.job_work_session       the time log (point 3)

jobs.job_resolution         the closing fact, one per cycle
    job_id · cycle · resolution_id · note · resolved_by_user_id · resolved_at
    closed_by_user_id · closed_at

jobs.job_recovery           only when glitch = true
    job_id · recovery_owed · recovery_done_by_user_id · done_at · guest_satisfied

jobs.job_note               comments, mentions, and the activity feed
    job_id · user_id · text · mentions[] · at

jobs.job_attachment         photos — at creation and at resolution
    job_id · media_id (Master Data media) · stage (RAISED · RESOLVED) · user_id · at

jobs.job_link               group and parent-child (S1.2)
    job_id · related_job_id · relation (GROUP · PARENT) · step · linked_by_user_id

jobs.job_concern_history    when concern changed, to what, why, who became accountable (S5)

catalogue (Core Administration, group-wide)
    category · item · resolution
jobs.property_item_policy   the per-property promise: priority default,
                            standard minutes, escalation policy, active, display name
```

**The rule that keeps it split:** a column goes on `job` only if it is a
fact about the job *as a whole* that has exactly one value. Anything with a
history (assignment, status, work) or that only some jobs have (recovery,
links) is its own table. That is what stops `job` growing back to forty
columns.

### 5 · PPM — planned in Engineering, executed in Jobs

*"An AC needs its filter changed every three months. We plan it in the
Engineering app's PPM section; when the time comes, an entry is created there
**and** a job is created; the execution is in Jobs."*

```text
ENGINEERING (Maintenance) — PPM section
  plan        asset 214-AC · "filter change" · every 3 months · dept ENG
                                  (the item is a catalogue item: AC › filter change)
  when due    creates ppm_occurrence  P-77   planned_for 2026-12-01
              appends event  maintenance.ppm.due
                { occurrence_id: P-77, asset_id: A-214-AC,
                  category_id: AC, item_id: FILTER_CHANGE,
                  planned_for, correlation_id: C-1 }

JOBS
  consumes    maintenance.ppm.due            (a declared subscription — EVT-Q4)
  creates     KOC-ENG-512
                raised_via: ENGINEERING_PPM · category AC · item filter change · asset 214-AC
                location 214 (from the asset) · no guest · priority from policy
                scheduled_for 2026-12-01 · origin_app maintenance · origin_ref P-77
  appends     job.created  { job_id, correlation_id: C-1 }
              ← Maintenance learns the job's id from THIS, never by calling Jobs
  … assigned · accepted · sessions · resolved (filter replaced) · closed — as any job
  appends     job.closed   { job_id, correlation_id: C-1, asset_id,
                             resolution_id: FILTER_REPLACED, resolved_at }

ENGINEERING
  consumes    job.closed with its correlation
              marks P-77 done · writes the asset's service history
              schedules the next occurrence  +3 months
```

Four rules, all platform law rather than choices:

* **No call in either direction.** An event with a correlation id each way
  (constitution §6, ADR 0116 §5). A blocking call would make one application
  mandatory for the other.
* **Maintenance absent** → nothing in Jobs changes; there are simply no PPM
  jobs. **Jobs absent** → the occurrence sits in Maintenance as *awaiting
  execution* and says so. Neither blocks the other.
* **The PPM job is an ordinary job.** Same number series, same statuses,
  same sessions, same resolve screen, same reports. `raised_via: ENGINEERING_PPM` and the
  `origin` reference are the only difference — "any job from anywhere".
* **The item is a catalogue item**, so *filter change* is reportable beside
  *not cooling* on the same asset — and the recurrence query in §S1.13 sees
  both.

### 6 · Assignment — the dropdown, the flip, and the fallback

**Corrected by the owner, 2026-09-03** — the first draft got two things
wrong: it let "all users" include people off shift, and it filtered by
*today*. Both are wrong. **Every list comes from Workforce, and every list
shows only people working on the job's execution date.**

*"The default dropdown always shows the category's department users (from
Workforce). The user can flip to related departments, or all users. Any
dropdown is from Workforce — only those working, based on the work order's
execution date."*

```text
THE DATE that filters every list
    the job's execution date = scheduled_for, or today if it is for now
    a job raised at 22:00 for tomorrow morning shows TOMORROW's shift

default            users posted to the category's department
                   AND on shift on the execution date        (Workforce)
flip 1             related departments — the canon's parent and children:
                   HK also shows LDY and PA; ENG also shows HVAC, ELEC, PLUM
                   AND on shift on the execution date
flip 2             all users at the property
                   AND on shift on the execution date
                   — never anyone off shift or on leave, in any list

sorted             fewest open jobs first, then name

no one chosen  ->  AUTO: first of the DEFAULT list
                   -> ASSIGNED · assignment_mode AUTO · assigned_by system
default list empty (nobody in that department on shift that day)
                   -> the job stays RAISED in the department's pool,
                      AND that is an escalation condition (S5): "not
                      assigned" counts from now, and the next rung is told
                      nobody was available
```

Two things this deliberately does **not** do, both of which the reference
did: nothing stores a person on the catalogue entry (`assigneeId` is gone —
a stored person leaves, changes shift, goes on leave), and nothing routes by
keyword (`keywordAssignee` and `sameForAllKeyword` are gone). The item's
department plus Workforce on the execution date is the whole rule. Jobs
**reads** Workforce through Context; it never rosters anyone (Workforce round
ruling, 2026-08-31).

### 7 · The catalogue screen appears in Core Administration only while Jobs is installed

*"The catalogue lives in Core — fine — but do not always show it. Only when
Jobs is installed, because it is common to GuestOps too."*

The platform already has the mechanism: an installed package **declares what
it contributes**, and Core Administration shows a section only while a
package declaring it is installed (the manifest-declared pattern of
PKG-Q39 / AUTHZ-Q25 / CFG-Q1).

```text
jobs/manifest.yaml
  contributes:
    core_administration:
      - section: job_catalogue        category · item · resolution
```

Jobs installed → the section is there. Uninstalled → gone, data kept (the
archived-schema pattern). GuestOps reads the same catalogue for what a guest
may request and contributes **no** editing screen — one owner of the screen,
several readers of the data.

**One gap recorded beside the ruling, for the architect:** GuestOps installed
without Jobs needs a guest-requestable list and nothing contributes the
screen to edit it.

### 8 · "Anything I missed?" — **all seven required**, owner 2026-09-03

| | Missing | Why it matters |
|---|---|---|
| **a** | **`scheduled_for`** — a job for later: *"turndown at 21:00"*, *"after checkout"* | otherwise every job is "now"; the PPM flow already needs it. Already on `job` (§4) |
| **b** | **Photos** at raise and at resolve — in the tables, not yet in the flow | before/after is the most-requested proof in engineering |
| **c** | **Notes and mentions** — the reference's activity feed | how a technician says "need a part, back at 3" |
| **d** | **Parts used** on resolve | the third gas recharge should record the gas; Inventory's when it exists, a structured note until then |
| **e** | **Shift handover** — a job open at 23:00 | hotelkit has an object for it; the rule *"who owns it now"* is unwritten |
| **f** | **Guest access** — occupied room, guest asleep or DND | `ON_HOLD: GUEST_DND` covers the state; *who decides to enter* is not written |
| **g** | **Reopen** — S4-D5 ruled who and when; what it does to sessions, resolution and the number is not | the `cycle` column on `job_resolution` is the hook |

**All seven are required** (owner, 2026-09-03). None changes the tables in §4;
each becomes a row in the design chapter.

### 9 · Who raises a job — staff now, guests next release

*"Jobs are raised by staff and by guests. Staff, directly from our app. For
guests, next release: a separate app generates a guest app and a QR; the
guest scans the QR and raises a job. Guests know nothing about users or
departments — they just tap **need water**, **need a burger**, **AC down**."*

```text
STAFF   the Jobs app. The full form: category › item, location, asset,
        priority (or let the flow set it), assignee (or AUTO), photos, notes.
        raised_via: STAFF_APP · raised_by_user_id: the member of staff

GUEST   the guest app, next release. No form — BUTTONS.
        The QR encodes the ROOM. The guest sees only items marked
        guest_requestable, as pictures and words in their language.
        One tap = one job.

        what the tap produces
          location_id      from the QR's room
          guest_stay_id    the current stay in that room — via Context/GuestOps
                           (no GuestOps installed → no stay link; the job is
                           still raised, for the room)
          category · item  from the button
          raised_via       GUEST_QR
          priority         the flow decides (occupied room, guest waiting)
          assignee         never chosen by the guest — AUTO, always
          glitch           set if the item is a fault, not a request

        what the guest never sees
          departments · users · priority · SLA · who is coming
        what the guest may see, later
          "someone is on the way" · "done" — GuestOps' surface (S8), read
          from job.* events, never from a Jobs screen
```

**Jobs' side is ready for this now.** A guest-raised job is an ordinary job
with `raised_via: GUEST_QR`, a stay link and no chosen assignee. The catalogue's
`guest_requestable` flag is what the guest app renders; nothing else in Jobs
changes when the guest app arrives.

**Two things belong to the guest-app round, not this one, and are recorded
so they are not assumed:**

* **What a QR scan authenticates.** A guest is not a user, and the
  reference's answer — an id in a header — is the thing never to repeat
  (01 §F15). Every operation passes Kernel authorization; what credential a
  scanned QR produces is the architect's and the guest-app round's.
* **"Need a burger."** In the first release a food order is a job to In-room
  dining. When a POS / ordering application exists it takes the order and
  raises the job itself — "any job from anywhere" — and the guest app's
  button does not change.

---

## S1.15 · The field vocabulary — nothing reads like the reference

**Owner, 2026-09-03:** *"`source: GUEST_APP` — I need to rephrase all fields
from Java."* Correct: `source` and its values are the reference's. This is
the whole pass, field by field, so a reader of our schema never meets the
Java system's words. **The left column is what the reference has; the right
is ours and is final unless the owner renames it.**

### `work_orders` → `jobs.job`

| Reference | Ours | Note |
|---|---|---|
| `id` (Long, auto-increment) | `job_id` (UUIDv7) | never exposed as a business identifier |
| `companyWOId` | `job_number` | `KOC-ENG-441`, stamped once |
| `companyId` · `siteId` · `facilityId` | `property_id` | one scope column |
| `comment` (500) | `summary` | one line — what the raiser said |
| `description` (1000) | `details` | the long text |
| `workOrderType` · `category` · `service` | `category_id` · `item_id` | no type |
| `location` (text) | `location_id` | Master Data's tree |
| — | `asset_id` | the reference has none |
| `priority` (1–10, default 5) | `priority` | **kept — a universal word, not a Java one**; the values are ours: EMERGENCY · HIGH · NORMAL · LOW · NOT_TRIAGED |
| — | `priority_decided_by` | MANUAL · FLOW · CATALOGUE · NONE — which layer set it |
| `slaDuration` (minutes) | `resolve_by` | a **time**, computed from the policy and the flow — not a duration to be recomputed |
| `startTime` + `startTimeInMillis` | `scheduled_for` | one field, one type |
| `dueDate` + `dueDateMillis` | `resolve_by` | same as above; two Java fields become one |
| `departmentId` + `department` | `department_code` | the canon code, one field |
| `assigneeType` · `assignedToId` · `executedById` | — | on `job_assignment` and `job_work_session` |
| `source` (PMS · FEEDBACK · HK · PPM …) | `raised_via` | STAFF_APP · GUEST_QR · ROOM_CARE · ENGINEERING_PPM · HOSPILOT · CHECKLIST · INTEGRATION |
| `referenceId` | `origin_app` + `origin_ref` | which application raised it, and its own id for the thing (`maintenance` · `P-77`) |
| `guestReferenceId` | `guest_stay_id` | a stay, resolved through Context |
| `initiatedById` + `addedByStaff` | `raised_by_user_id` | null when a guest raised it; `raised_via` says so |
| `checklistId` · `inspectionId` | `checklist_run_id` | later; one field |
| `workOrderStatus` | `job_status` | 8 values, §S1.14 ·2 |
| `accepted` · `started` · `waiting` · `guestAcknowledged` · `reopened` | — | gone; state lives in `job_status` and the tables |
| `reopenCount` | `cycle` | starts at 1; reopen increments |
| `followers` | — | `job_watcher`, designed with notifications (S7) |
| `images` | — | `job_attachment` |
| `status` (`EntityStatus`) | `deleted_at` · `deleted_by_user_id` · `delete_reason` | ADR 0062's shape |
| `createdOn` · `updatedOn` · `version` | `created_at` · `updated_at` · `version` | the platform's own names (`MasterEntity`) |
| — | `glitch` | the reference had a type instead |

### The satellite tables

| Reference | Ours | What changed |
|---|---|---|
| `work_order_activity` (from/to as free text) | `job_status_history` **+** `job_note` | a transition and a comment are two things |
| `work_order_time_log` (`startReason` · `endReason` · `timeInMinutes`) | `job_work_session` (`started_at` · `ended_at` · `end_reason` · `minutes`) | resume never reopens a row |
| `work_order_escalations` | `job_escalation` | S5 |
| `work_order_reminders` | `job_reminder` | S6 |
| `work_order_follower` + `followers` | `job_watcher` | one place, S7 |
| `tbl_work_order_affiliation` | `job_link` | group **and** parent, with `step` |
| `work_order_cix` | `checklist_run_id` on the job | later |
| `work_order_rating` · `work_order_feedback` · `work_order_track` | — | GuestOps' surfaces (S8) |
| `work_order_summary` (15 derived columns) | — | derived on read; no table |
| `work_order_contact_info` · `work_order_customer_info` | — | Context answers |
| `wo_sequence` (per company) | `property_job_sequence` | per property |
| `wo_device` | — | dropped with device assignment |

### The catalogue

| Reference | Ours |
|---|---|
| `WOServicePreference.service` (name as key) | `category` · `item` — UUID keys, `code`, `name` per language |
| `keywords` · `keywordAssignee` · `sameForAllKeyword` | `item_alias` — search only, never routing |
| `type` on the entry | — |
| `departmentId` + `department` on the entry | `category.department_code` |
| `assigneeType` · `assigneeId` on the entry | — |
| `priority` · `sla` on the entry | `property_item_policy.default_priority` · `standard_minutes` |
| `trackMode` · `icon` | `icon` kept on `category`; `trackMode` gone |
| — | `resolution` · `item_resolution` |

### `raised_via` — the values, since `source` was the trigger

```text
STAFF_APP          a member of staff, from the Jobs app
GUEST_QR           a guest, from the generated guest app (next release)
ROOM_CARE          Room Care raised it (found during cleaning)
ENGINEERING_PPM    a planned-maintenance occurrence became due
CHECKLIST          a failed line on a checklist run
HOSPILOT           the assistant raised it (deferred)
INTEGRATION        a connector — PMS, sensor, other
```

`raised_via` says **how it arrived**; `origin_app` + `origin_ref` say **whose
record it came from**. A PPM job has both: `raised_via: ENGINEERING_PPM`,
`origin_app: maintenance`, `origin_ref: P-77`.

---

## S1.16 · Who raised it, who it is for, and when it may start — owner, 2026-09-03

### 1 · `raised_by_user_id` is wrong, because a guest can raise a job

Staff and guests both raise jobs; a column named for users misdescribes half
of them. And *who raised it* is not the same fact as *who it is for* — a
receptionist raises a job **for** Mr Rao; Mr Rao raises one for himself.

```text
WHO RAISED IT
  raised_via          STAFF_APP · GUEST_QR · ROOM_CARE · ENGINEERING_PPM · …   (ruled)
  raised_by_kind      STAFF · GUEST · APPLICATION
  raised_by_id        STAFF        → the staff user_id
                      GUEST        → the guest's stay_id
                      APPLICATION  → the application's name (maintenance · roomcare)

WHO / WHAT IT IS FOR
  guest_stay_id       the guest concerned, if any — set whether staff or the
                      guest raised it
  origin_app          the application whose record started it     maintenance
  origin_ref          that application's own id for it              P-77
```

**Every raiser has a reference id.** For PPM it is `origin_ref = P-77`. For a
guest it is the **stay**. *(Owner asked for the reservation id — see the next
point.)*

### 2 · Reservation or stay — the platform has already chosen

The owner asked for `reservation_id` on a guest-raised job. The platform's
guest model (`GUEST-Q2`) is:

```text
reservation      the booking — may cover several rooms
  └─ stay        one room, one occupancy — what a QR's room resolves to
```

Check-in, check-out and *"who is in room 214 right now"* all happen to the
**stay**, and a reservation with two rooms has two stays. So the job stores
**`guest_stay_id`**, and **the reservation is one step away through it** —
Context answers *"which reservation is this stay?"* on every screen that
needs it.

Storing `reservation_id` beside it would be a copy of a derivable fact, which
the platform forbids (*clients never write a derived projection*). If the
owner wants the reservation *shown* on the job, it is shown — from the stay.

### 3 · `scheduled_for` and `startTime` were the same thing — one field, one state

*"Both mean: until that time arrives, the job is blocked; no action can be
taken on it."* Agreed. The reference kept both (`startTime` **and**
`dueDate`, each twice); here there is one field and it produces a state:

```text
scheduled_for      null  = now.  Otherwise the moment the job becomes live.

SCHEDULED          a job whose scheduled_for is in the future
                   visible · not actionable · SLA clock NOT started
                   the only moves: reschedule · cancel
                   when the time arrives → RAISED (or ASSIGNED, if pre-assigned)
                   and the clock starts then
```

So `job_status` gains one value and is nine again — but this one is earned:
a job for tomorrow morning must not sit in today's pool, must not escalate
tonight, and must not be started early by mistake. Without the state, every
list and every clock has to remember to check a date.

**Who may change the time:** the raiser, and anyone holding **manage** on the
job (S9). A worker cannot move their own start time forward; a supervisor
can.

### 4 · Corrections, owner 2026-09-03 — the guest is two ids, and the location comes with the stay

*"We need stay_id. But we have the CRM app (called something else here —
already documented), so we keep user_id and crm_id (the field name will
follow the app), and stay_id, and the location — from the stay we get it."*

The CRM application is **Guest360** (APPS-Q1: *"Guest360 = CRM — the
guest-profile/CRM application"*; its register prefix is `G360`). So the guest
on a job is **two ids, for two different facts**:

```text
guest360_id     WHO the guest is — the person, across every visit
                the field is named for the app, as the owner asked
guest_stay_id   WHICH visit — this room, this occupancy
                the QR's room resolves to it; check-in/out happen to it (GUEST-Q2)
location_id     defaulted FROM the stay's room when a guest raises the job,
                and kept as its own column — a guest may report the pool
```

**Why both, in one line each:** *"Mr Rao complains about the AC every visit"*
needs `guest360_id`; *"who is in 214 tonight"* needs `guest_stay_id`. And
Guest360's own founding input (G360-Q1) says guest identities **merge** —
phone-only and email-only guests later found to be one person — so the job
stores the `guest360_id` it was given, and Guest360 owns the merge lineage.
The reservation, when a screen needs it, is one step from the stay.

**The raiser, restated with these names:**

```text
raised_by_kind    STAFF · GUEST · APPLICATION
raised_by_id      STAFF → user_id · GUEST → guest360_id · APPLICATION → app name
```

`S1-D18` is closed by this: **stay, not reservation — and the person beside
it.**

### 5 · `SCHEDULED`, confirmed — and clearing it, and what escalation anchors to

*"What is the status of a scheduled job? A manager can set that field to
null too. And the escalations are based on this time."*

```text
status while waiting     SCHEDULED     (§S1.16 ·3)

set scheduled_for = null → the job goes live NOW
                           SCHEDULED → RAISED, or ASSIGNED if pre-assigned
                           who: the raiser, or anyone with MANAGE in scope

every clock anchors at   live_at = scheduled_for, or created_at if null
    SLA                  resolve_by counts from live_at
    escalation           "not assigned after N minutes" counts from live_at
    labour               sessions cannot open before live_at
```

So a job raised at 22:00 for 07:00 escalates at 07:15, not at 22:15 — the
reference reached for exactly this with `applyStartByFloor` (01 §3.4) and
then anchored its four clocks to four different fields. Here there is one
anchor and every clock reads it.

### 6 · Correction, owner 2026-09-03 — no `guest360_id`; the raiser and the creator are two people

*"We don't need `guest360_id` on the job — `raised_by_id` already carries a
user id or a guest id. And a rare case: a guest phones the front office and
the staff member creates the job. The creator is staff; the owner is the
guest. Reports of jobs raised by guest / by staff must count that as guest."*

```text
raised_by_kind       STAFF · GUEST · APPLICATION     WHOSE request it is  ← reports count THIS
raised_by_id         user_id | guest id (Guest360's) | app name
created_by_user_id   who ENTERED it — null when the guest did it themselves by QR
guest_stay_id        the visit, when a guest is involved
```

The phone call: `raised_by_kind: GUEST · raised_by_id: <guest> ·
created_by_user_id: <receptionist> · raised_via: STAFF_APP`. The report
*"raised by guests"* counts it; the audit trail still says who typed it.
`guest360_id` as a separate column is withdrawn — the guest's identity is
`raised_by_id` when the kind is GUEST, and `guest_stay_id` when the job is
about an occupied room but staff raised it.

### 7 · Where things are configured — measured, with one conflict to report

| What | Where | State |
|---|---|---|
| May this user open Jobs? | Core Administration → User → Applications card → Identity `SetApplicationAccess` | **built** |
| Which departments is this user in? | Workforce postings (ADR 0116 §6) | **built** |
| Property membership · general manager | Identity `SetPropertyAssignment` · `SetGeneralManager` | **built** |
| **Which Jobs powers does this user hold?** | **No surface exists.** Identity has exactly those three grant RPCs and no per-user, per-permission one | **open — the architect's** |
| The catalogue — category · item · resolution | Core Administration → Job catalogue (manifest-contributed, §S1.14 ·7) | designed |
| **The policy — SLA, default priority, escalation policy, per item, per property** | **the Jobs app's own Settings** — it is Jobs' behaviour, and no other application reads it | designed |

**The conflict, reported not resolved.** `infrastructure/openfga/permissions.yaml`
**already declares five Jobs permissions**, on a `job` object:

```text
job.read          → job#viewer
job.create        → job#can_create
job.assign        → job#can_assign
job.complete      → job#can_close
job.approve_cost  → job#can_approve_cost
```

and its own header rules that *scope* is the policy layer's — *"one relation
is repointed in `model.fga` and nothing else moves: no permission renamed."*
So S9's six proposed names collide with five pre-declared ones, and S9's
*scope* axis (own · department · property) is **not a permission at all** on
this platform — it is what the relations on `job`, `department` and
`property` resolve. The likely mapping, offered as a question and not
written into anything: execute ≈ `job.complete` (+ read), manage ≈
`job.assign` + `job.complete`, administer ≈ the property admin; and
`job.approve_cost` is a power the owner has not yet named. **S9 is amended to
say so, and the reconciliation goes up.**

### 8 · The rest of the Java feature set, and how far S1 is from sign-off

*"Still in Java there are more features — escalation levels and such."*
Yes, and none of them is S1's. S1 is *what a job is*. The rest is where it
was placed on the first day:

```text
escalation levels, chains, night handling      S5
waiting / progress reminders                   S6
notifications, followers (job_watcher)         S7
guest tracking, rating, acknowledgement        S8
scheduled / PPM                                S10  (the flow itself is S1-D12)
checklists                                     after S10; a job is raisable FROM one
```

**S1 stands at 19 decisions: 17 ruled.** Open: **D10** work sessions,
**D11** the split tables, **D12** the PPM flow — all three read by the owner
without objection. **One "yes" to those three signs S1 off.**

### 9 · No null that means something — one **actor** shape, used everywhere

*"`created_by_user_id` — null when the guest did it by QR: suggest another
design."* Right: a null carrying a fact is a fact nobody can query. The fix is
one value type, and it is never null:

```text
actor = { kind: STAFF | GUEST | APPLICATION,  id }
        STAFF → user_id · GUEST → Guest360's guest id · APPLICATION → app name
```

Every "who" on every table is an actor — `created_by`, `raised_by`,
`assigned_by`, `resolved_by`, `closed_by`, `changed_by`, `linked_by`. **Both
`created_by` and `raised_by` are always set**, and when one person did both
they hold the same value:

```text
guest by QR          created_by GUEST/g-rao     raised_by GUEST/g-rao
phoned-in request    created_by STAFF/u-priya   raised_by GUEST/g-rao
staff's own job      created_by STAFF/u-suresh  raised_by STAFF/u-suresh
PPM                  created_by APP/maintenance raised_by APP/maintenance
```

*"Raised by guests"* is `raised_by.kind = GUEST`, with no null test anywhere.
The alternative — `created_by` plus an optional `on_behalf_of` — was
considered and refused for the same reason the owner refused the null: it
makes the requester a derivation (`on_behalf_of ?? created_by`) instead of a
stored fact.

### 10 · "Which Jobs powers does the user hold — where did we design this?" — found

It is designed, and it was designed **before this round**, in the platform's
authorization model. `infrastructure/openfga/model.fga`, `type job`:

```text
viewer            creator  or  assignee  or  viewer from department
can_create        member     from department
can_assign        supervisor from department
can_close         assignee  or  supervisor from department
can_approve_cost  manager    from department
```

So a user's Jobs powers are **not granted per user at all** — they are
**department relations**, and department relations come from **Workforce**:
a posting makes someone a department *member* (ADR 0116 §6, AUTHZ-Q20's
`department#posted`); headship makes the department *manager*
(`user.headship_started`); *supervisor* is the relation between. The owner's
four levels map onto it almost one for one:

| Owner's level | The relation that gives it | Configured in |
|---|---|---|
| normal user — own jobs, execute, resolve | posted to the department (`member`) → `can_create`, `can_close` on own jobs | **Workforce** |
| next level — reassign, change priority | `supervisor` of the department → `can_assign`, `can_close` | **Workforce** (the posting's role) |
| sees all in my departments | `viewer from department` | **Workforce** |
| everything, anyone's, capture to self | `property#admin` | Core Administration — the property administrator, born not appointed (ADR 0116 §3) |

**Answer to the question:** *where you configure it is Workforce, by posting
a person to a department as member or supervisor, or making them its head.*
Not a Jobs screen, not a Core Administration card. And the security guard
who must only execute is a `member` who is nobody's `supervisor` — which is
the default.

What S9 got wrong: it proposed six new permission names and a per-user grant
screen. Neither is needed; the model already has the five actions and the
relations, and **the reconciliation of S9's vocabulary to `model.fga` is
parked for the owner's separate discussion, as asked** — not sent to the
architect.

---

## S1.17 · Every field, plainly — owner's request, 2026-09-03

> **The `job` table below is superseded by §S1.18** (the owner's cut, the same day): `glitch`, `live_at`, the `created_by` actor and `origin_app`/`origin_ref` are gone. The satellite and catalogue tables here stand, except `job_recovery`.

One line per field. *actor* means `{ kind: STAFF | GUEST | APPLICATION, id }`
and is never null.

### `job` — one row per job

| Field | Plainly |
|---|---|
| `job_id` | the id nobody reads — a UUID, never shown, never reused |
| `job_number` | the one people read — `KOC-ENG-441`, stamped once, never recomputed |
| `property_id` | which hotel |
| `category_id` · `item_id` | what it is about — *AC › not cooling*, *Extra items › towel* |
| `location_id` | where — a room, the pool, a corridor, a whole floor |
| `asset_id` | the thing, when it is a thing — *214-AC*; attached automatically when the room has exactly one of that kind |
| `department_code` | who does it — comes from the item, a supervisor may change it; the number does not change |
| `summary` | what the person said, one line |
| `details` | the long version, optional |
| `priority` | EMERGENCY · HIGH · NORMAL · LOW · NOT_TRIAGED |
| `priority_decided_by` | MANUAL · FLOW · CATALOGUE · NONE — *why* it is that priority |
| `glitch` | yes when a guest was let down and recovery is owed |
| `guest_stay_id` | the guest's visit, when a guest is involved — the QR's room resolves to it; the reservation is one step away |
| `raised_by` *(actor)* | **whose request it is** — reports count this |
| `created_by` *(actor)* | **who entered it** — the receptionist on a phoned-in request; the guest themselves by QR |
| `raised_via` | how it arrived — STAFF_APP · GUEST_QR · ROOM_CARE · ENGINEERING_PPM · CHECKLIST · HOSPILOT · INTEGRATION |
| `origin_app` · `origin_ref` | the other application's record, when one started it — *maintenance · P-77* |
| `scheduled_for` | when the job goes live — null means now; in the future means `SCHEDULED` |
| `live_at` | `scheduled_for`, or `created_at` if none — **every clock counts from here** |
| `resolve_by` | the deadline — a time, computed from the item's standard minutes, the flow (an arrival at 14:00), and `live_at` |
| `job_status` | SCHEDULED · RAISED · ASSIGNED · ACCEPTED · IN_PROGRESS · ON_HOLD · RESOLVED · CLOSED · CANCELLED |
| `cycle` | 1, and one more each time the job is reopened |
| `created_at` · `updated_at` · `version` | the platform's own three — `version` is what stops two people saving over each other |
| `deleted_at` · `deleted_by` *(actor)* · `delete_reason` | soft delete — set only by administer, reason required, always audited |

### The tables beside it — one fact each

| Table | One row per | Fields, plainly |
|---|---|---|
| `job_assignment` | each time the job is given to someone | `assigned_to` (a user **or** a team) · `assigned_by` *(actor)* · `assigned_at` · `ended_at` · `end_reason` REASSIGNED · HANDED_BACK · RESOLVED · `mode` MANUAL · AUTO · INHERITED — **the current assignee is the row with no `ended_at`** |
| `job_status_history` | each status change | `from` · `to` · `changed_by` *(actor)* · `at` · `reason` — the audit trail |
| `job_work_session` | each stretch of work by one person | `user_id` · `started_at` · `ended_at` (null = working now) · `end_reason` PAUSE · STOP · REASSIGNED · AUTO_STOPPED · `minutes` — the live board is every row with no `ended_at` |
| `job_resolution` | each cycle's closing | `cycle` · `resolution_id` (*gas recharged*) · `note` (the free text) · `resolved_by` · `resolved_at` · `closed_by` · `closed_at` |
| `job_recovery` | a glitch job, once | `recovery_owed` (apology · upgrade · compensation · manager visit) · `done_by` · `done_at` · `guest_satisfied` yes · no · not asked |
| `job_note` | each comment | `by` *(actor)* · `text` · `mentions[]` · `at` |
| `job_attachment` | each photo | `media_id` (Master Data's media) · `stage` RAISED · RESOLVED · `by` *(actor)* · `at` |
| `job_link` | each relation to another job | `related_job_id` · `relation` GROUP · PARENT · `step` (parent/child only) · `linked_by` *(actor)* |
| `job_concern_history` | each change of concern | S5 — `at` · `concern` · `reason` · `accountable_role` · `accountable_id` |

### The catalogue — Core Administration, group-wide

| Table | Plainly |
|---|---|
| `category` | *AC*, *Plumbing*, *Extra items* — `code` · `name` per language · `department_code` · `icon` · `active` |
| `item` | *not cooling*, *towel* — `category_id` · `code` · `name` per language · `guest_requestable` · `applies_to` room · public area · asset type · `needs_checklist` · `photo_on_completion` · `typical_minutes` · `active` |
| `item_alias` | *aircon*, *a/c*, *ac not working* — so free text and a guest's tap land on the right item |
| `resolution` | *gas recharged*, *no fault found*, *other* — `category_id` (null = universal) · `item_id` (null = every item of that category) · `name` |
| `property_item_policy` | **per property** — `item_id` · `active_here` · `display_name` (rename) · `default_priority` · `standard_minutes` · `escalation_policy_id` · `chargeable` · `price` — *what we promise about it here* |

---

## S1.18 · The lean job table — the owner's cut, 2026-09-03

*"You are over-engineering concepts. Why `glitch`? Why two fields, `live_at`
and `scheduled_for`? Why that many fields for origin? We need `raised_via`,
`raised_kind`, `raised_by_id`, and one `stay_id` — that is enough. Redesign
the job table and show me."*

Accepted, and two of the four were the stream breaking rules it had itself
written down.

| Dropped | Why it was there | Why it goes |
|---|---|---|
| **`glitch`** + `job_recovery` | to mark "a guest was let down" and track apology / compensation | *a guest complained* is already `raised_kind = GUEST` on a fault; recovery (apology, upgrade, comp) is **Guest360 / GuestOps' concern**, not a job's. Gone from v1 |
| **`live_at`** | the one anchor every clock reads | it is `scheduled_for ?? created_at` — **a derived value, which the platform forbids storing** and §S1.3 rule 1 forbade too. Computed, never a column |
| **`created_by` as an actor** | who typed a phoned-in guest request | the platform already has **`created_by`** on every table as its standard audit column (`MasterEntity.CreatedBy`, a user). The receptionist is there without a new field |
| **`origin_app` · `origin_ref`** | PPM's *P-77* | Maintenance learns the job's id from `job.created` and **keeps the link on its own occurrence** — the consumer holds the reference, which is the platform's direction. Jobs needs only `raised_kind = APPLICATION` and `raised_by_id` = the application |

### `job` — one row, and this is all of it

```text
job_id                UUID — never shown
job_number            KOC-ENG-441 — stamped once
property_id

category_id           what — AC › not cooling
item_id
location_id           where — room · pool · corridor · floor
asset_id              the thing, when there is one
department_code       who does it — from the item; a supervisor may change it

summary               what was said, one line
details               the long version, optional
priority              EMERGENCY · HIGH · NORMAL · LOW · NOT_TRIAGED
priority_decided_by   MANUAL · FLOW · CATALOGUE · NONE

raised_via            APP · QR · GUEST_APP · WHATSAPP        how it arrived
raised_kind           STAFF · GUEST · APPLICATION            who is asking
raised_by_id          user_id · Guest360 id · application id
stay_id               the guest's visit — NOT NULL when raised_kind = GUEST

scheduled_for         null = now; a future time = SCHEDULED
due_at                the deadline — a time, set at creation, editable by manage
job_status            SCHEDULED · RAISED · ASSIGNED · ACCEPTED · IN_PROGRESS ·
                      ON_HOLD · RESOLVED · CLOSED · CANCELLED
cycle                 1, +1 per reopen

created_by · created_at · updated_by · updated_at · version    the platform's standard five
deleted_at · deleted_by · delete_reason                        soft delete
```

Twenty-three columns, five of them the platform's own. Every clock counts
from `scheduled_for ?? created_at`, computed where it is needed.

**The four cases, in the four raiser fields:**

```text
staff, own job         APP        STAFF        u-suresh     stay null
guest by QR            QR         GUEST        g-rao        stay 8812
guest phones the desk  APP        GUEST        g-rao        stay 8812    created_by = u-priya
PPM                    APP        APPLICATION  maintenance  stay null
```

*"Raised by guests"* = `raised_kind = GUEST`. Who typed the phoned-in one is
the platform's `created_by`. Nothing else is needed.

**The tables beside it are unchanged** (§S1.17) **except `job_recovery`, which
goes with `glitch`.**

### Two questions on the lean table — owner, 2026-09-03

**"`resolve_by` — what is this?"** The deadline: the time by which the job
must be resolved. The name was bad — it reads like *"resolved by whom"*.
**Renamed `due_at`.**

```text
due_at  =  (scheduled_for ?? created_at)  +  the item's standard minutes at this property
           … unless the flow gives a harder time — an arrival at 14:00 makes due_at 14:00
           set once at creation · a supervisor with manage may move it
           "late" means now > due_at and the job is not RESOLVED
```

**"Where is `resolution_id`, and the list of what we solved for AC?"** Not on
`job` — on **`job_resolution`**, one row per cycle, because a reopened job is
resolved twice and the first answer must not be lost (*no fault found*, then
reopened, then *gas recharged* is exactly the pattern the recurrence report
wants to see).

```text
CATALOGUE                                   THE JOB
resolution                                  job_resolution
  category AC · item not cooling               job_id
    filter cleaned                             cycle            1
    gas recharged                              resolution_id  → "gas recharged"
    thermostat replaced                        note             "low gas; check in a month"
    unit replaced                              resolved_by      u-suresh
  category AC · item (any)                     resolved_at      20:30
    referred to vendor                         closed_by        u-priya
  universal (any category)                     closed_at        20:50
    no fault found
    other  → note becomes mandatory
```

On the resolve screen the technician sees the list for **AC › not cooling**
first, then AC's general ones, then the universal ones, and always the note
box. What he picks is `resolution_id`; what he types is `note`. Reopen the
job and cycle 2 gets its own row; cycle 1 stays.

### The policy's home — a contradiction in the stream's own words, corrected

The stream wrote *"the policy is the Jobs app's own Settings"* and then
listed `property_item_policy` under *"Catalogue — Core Administration"*. The
second was wrong.

```text
CATALOGUE   category · item · item_alias · resolution
            Core Administration, group-wide, the screen contributed by Jobs' manifest

POLICY      property_item_policy — active here · display name · default priority ·
            standard minutes · escalation policy · chargeable · price
            THE JOBS SCHEMA, edited in the JOBS APP's own Settings, per property
```

Nothing but Jobs reads the policy, so nothing but Jobs holds it.

## S1 — signed off. What is in force, consolidated

**Owner, 2026-09-03: "D10, D11, D12 okay — sign off S1."** The decision table
below records the day's route, supersessions included; this is the settled
state, and it is what the design chapter is built from.

```text
THE JOB                 23 columns (§S1.18): identity · what · where · who does it ·
                        summary/details · priority + why · the four raiser fields ·
                        stay · scheduled_for · due_at · job_status · cycle · audit · delete
WHAT IT IS ABOUT        category › item from the catalogue; location_id from Master
                        Data's one tree; asset_id when there is a thing
NO TYPE                 department + raiser kind cover every use
THE CATALOGUE           category · item · item_alias · resolution — Core Administration,
                        group-wide, activated per property, screen shown only while
                        Jobs is installed
THE POLICY              property_item_policy — the jobs schema, Jobs Settings, per property
RESOLUTION              job_resolution, one row per cycle; the list from the catalogue;
                        a note always; "other" makes it mandatory; three ids on the job
                        make the recurrence report a query
PRIORITY                EMERGENCY · HIGH · NORMAL · LOW · NOT_TRIAGED, decided by a person,
                        else the flow, else the catalogue, else NOT_TRIAGED
NUMBER                  <PropertyCode>-<RootDept>-<n>, one counter per property, stamped
                        once, upper-cased from Master Data's lowercase code
STATUSES                nine, with SCHEDULED; PAUSED lives in the time log; record state
                        is deleted_at, never an enum; CANCELLED is an outcome
TIME                    work sessions start / pause / resume / stop; the live board is a
                        query; SLA clock and labour clock kept apart; every clock counts
                        from scheduled_for ?? created_at
RELATIONS               a GROUP for peers; PARENT › CHILD for steps, one level, step numbers,
                        blocked children with stopped clocks, no close until all children
TABLES                  job · assignment · status history · work session · resolution ·
                        note · attachment · link · escalation — one fact each
ASSIGNMENT              every list from Workforce on the execution date; default = the
                        category's department; flips = related / all, on-shift only;
                        AUTO takes the first; nobody available is an escalation condition
PPM                     planned in Engineering; maintenance.ppm.due → an ordinary job;
                        job.closed back by correlation; no calls either way
WHO RAISES              staff from the app now; guests by QR next release, buttons only
ACCESS                  department member / supervisor from Workforce; property-wide via a
                        GM-grantable property#jobs_manager (registry entry pending);
                        the vocabulary reconciliation parked for the owner
TENANCY                 property_id, one column
```

**Leaves S1 as requests, none blocking:** the missing place kinds and a
property-code shape rule (Master Data) · the catalogue's home in Core
Administration (architect) · `property#jobs_manager` in the grantable-
relations registry (architect, with the parked discussion) · GuestOps
without Jobs and the catalogue screen (architect) · what a QR authenticates
(the guest-app round). **Deferred by the owner:** HosPilot raising jobs.


## Decisions — round 1 close

| id | Decision | Ruling |
|---|---|---|
| **S1-D1** | A job carries `location_id` (any node in the one tree) and an optional `asset_id`. The missing place kinds go up as one request | **RULED** (owner, 2026-09-02) — §S1.1 |
| **S1-D2** | One subject; a **group** for peers; **parent ▸ children** for a breakdown, with step numbers, blocked children, and no close until all children are done | *design proposed — §S1.2 — ten details open (a–j)* |
| **S1-D3** | `<PropertyCode>-<RootDept>-<Number>`, number property-wide, stamped once | **RULED** — two details open in §S1.3 |
| **S1-D4** | Emergency · High · Normal · Low · Not triaged, decided by: a person chose it → the guest flow (PMS/GuestOps) → the catalogue default → Not triaged | **RULED** (owner, 2026-09-02) — §S1.4 |
| **S1-D5** | **Deliver · Fix · Check · Prepare** — four intents, not five types. Complaint-vs-Fault falls out of *is there a requester*; "Maintenance" disappears with both its special cases | **REOPENED** by the owner, 2026-09-02 — the shape is inherited from the reference's `workOrderType`, and four real hotel jobs do not fit it. **RULED, owner 2026-09-03 — no type.** Department + guest link + glitch flag replace it. The pair is **category › item** (§S1.14 ·1) |
| **S1-D6** | One organization-wide catalogue, activated per property, renameable for display; the entry carries its **intent**, its **aliases** and how long the work **takes**, so the job's type is never chosen separately. The **promise** (SLA, priority, routing, escalation) is Jobs', per property. Plus a counted, promotable "something else" | **REOPENED** by the owner, 2026-09-02. The first-principles read says the list is **four different things**, and **RULED, owner 2026-09-03.** A **category › item** catalogue by department, **resolution actions** on close, three ids on the job. Lives in Core Administration; its screen appears only while Jobs is installed (§S1.14 ·7) |
| **S1-D7** | `category` dropped | **RULED** |
| **S1-D8** | One scope column, `property_id` | **RULED** — reasoning in §S1.7 |

| **S1-D9** | **Two statuses.** `job_status` — 9 values with a transition table, replacing the reference's 8-value enum *and* its five booleans. Record state is **`deleted_at`, not an enum** (ADR 0062's shape), and **`CANCELLED` is a job outcome, not a record state** | **RULED, owner 2026-09-03** — §S1.12, rephrased to 8 values in §S1.14 ·2 |
| **S1-D10** | **Live work tracking** — sessions: start / pause / resume / stop; the live board is a query; SLA clock and labour clock kept apart | **RULED, owner 2026-09-03** — §S1.14 ·3 |
| **S1-D11** | **Tables split by logic** — job · assignment · status history · work session · resolution · recovery · note · attachment · link · escalation | **RULED, owner 2026-09-03** — §S1.14 ·4 |
| **S1-D12** | **PPM flow** — planned in Engineering; `maintenance.ppm.due` → an ordinary job with `raised_via: ENGINEERING_PPM`; `job.closed` back by correlation | **RULED, owner 2026-09-03** — §S1.14 ·5 |
| **S1-D13** | **Assignment** — every list from Workforce, **on shift on the job's execution date**; default = the category's department; flips = related departments / all users, still on-shift only; AUTO takes the first; *nobody available* is an escalation condition | **RULED, owner 2026-09-03** — §S1.14 ·6, corrected once |
| **S1-D14** | **Catalogue screen in Core Administration only while Jobs is installed** — manifest-contributed | **RULED, owner 2026-09-03** — §S1.14 ·7; the GuestOps-without-Jobs gap recorded |
| **S1-D16** | **The field vocabulary** — every Java field renamed or removed; `source` → `raised_via` + `origin_app`/`origin_ref`; `priority` kept as a universal word with our values | **RULED, owner 2026-09-03** — §S1.15 |
| **S1-D17** | **Four raiser fields and no more** — `raised_via` (APP · QR · GUEST_APP · WHATSAPP) · `raised_kind` (STAFF · GUEST · APPLICATION) · `raised_by_id` · `stay_id` (not null for a guest). Who typed it is the platform's own `created_by` | **RULED, owner 2026-09-03** — §S1.18 |
| **S1-D18** | **No `guest360_id` column** — the guest is `raised_by_id` when kind is GUEST; `guest_stay_id` is the visit; **superseded by D17** — the actor pair is withdrawn; `stay_id` stays, not null for a guest | see D17 |
| **S1-D19** | **One `scheduled_for`, and a `SCHEDULED` status** — clearing it makes the job live now; **every clock (SLA, escalation, labour) anchors at `live_at` = scheduled_for or created_at** | **RULED, owner 2026-09-03** — §S1.16 ·3, ·5; `job_status` is nine values. **`live_at` is not a column** — computed as `scheduled_for ?? created_at` (§S1.18) |
| **S1-D20** | **`glitch` and `job_recovery` dropped from v1** — a guest complaint is `raised_kind = GUEST` on a fault; recovery is Guest360 / GuestOps' | **RULED, owner 2026-09-03** — §S1.18 |
| **S1-D21** | **The policy lives in the `jobs` schema, edited in Jobs Settings** — the stream's "Core Administration" listing was wrong; only the catalogue is Core's | **corrected, 2026-09-03** — §S1.18 |
| **S1-D15** | **Guests raise jobs by QR, next release** — buttons only, `raised_via: GUEST_QR`, stay from the room via Context, AUTO assignment, nothing about users or departments shown. Jobs is ready now; the QR credential is the guest-app round's | **RULED, owner 2026-09-03** — §S1.14 ·9 |

**Sign-off:** **S1 SIGNED OFF — owner, 2026-09-03, in the owner's own words: *"D10, D11, D12 okay — sign off S1."*** Twenty-one decisions, every one ruled or explicitly parked by the owner.

*(The stream had marked this section signed off on 2026-09-02 without the owner's word; that was reversed and is kept on the record above.)*

Six decisions carry rulings — D1, D2, D3, D4, D7, D8. **D5 and D6 were
reopened** on the owner's challenge (§S1.8) and **redesigned from the
reference in §S1.10 and §S1.11**. §S1.9 is **withdrawn** — the stream
over-engineered it and got its premise wrong. **A ninth decision, S1-D9, is
added**: the two statuses (§S1.12), at the owner's request.

---

# S2 · Creating a job

**State: SIGNED OFF — owner, 2026-09-03.**

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
| **S2-D1** | A job is complete when created — no background fill-in; one transaction | **RULED, owner 2026-09-03** |
| **S2-D2** | Backdating allowed — *manage* and above, up to 7 days | **RULED, owner 2026-09-03** |
| **S2-D3** | Mandatory at creation: category › item · location · raiser. Summary optional when the item says it all | **RULED, owner 2026-09-03** |
| **S2-D4** | Ways in at launch: staff app · application by event · scheduled. Guest QR next release (S1-D15) | **RULED, owner 2026-09-03** |
| **S2-D5** | A job may exist with no assignee — the pool; AUTO fills it when someone is on shift | **RULED, owner 2026-09-03** |

**Sign-off:** **S2 SIGNED OFF — owner, 2026-09-03: *"S2 approved."***

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

## The team, restored — owner, 2026-09-03

*"I forgot about the team concept. We need the team as an option: manually a
user can choose a team or a user; when the system auto-assigns it is based on
policy. The others we can lock."*

**A job is assigned to a person or a team.** One assignment row, one of two
targets: `assigned_to_user_id` **or** `assigned_to_team_id`.

**Manual** — the dropdown offers both, from Workforce on the execution date:
people (S1-D13's lists) and **teams with at least one member on shift** (a
night crew with nobody on shift tonight is not offered).

**Auto** — the item policy says which:

```text
property_item_policy.auto_assign     USER  → the first person on the default list
                                     TEAM  → this team_id
```

**A team assignment becomes a person's on accept.** Any member may accept;
that opens a new assignment row for the person and ends the team's row with
`end_reason: ACCEPTED_BY_MEMBER`. Until then the job is `ASSIGNED` to the
team and the *not accepted* clock runs against the whole team — the case the
reference's `WO_NOT_ACCEPTED` was written for and never quite handled.

### Where the team object lives — measured, then asked

**No team object exists on the platform today** — not in Master Data, not in
Workforce's chapter or design pages, not in the authorization model. It is
new, and its home is a ruling. The platform's own test says where: **ADR
0063** — *"if an attribute exists primarily to determine operational
assignment or workforce capability, it belongs to Workforce."* A team exists
to be assigned work; it is Workforce's, read by Jobs through Context like
postings and shifts. **Jobs must not create its own `teams` table** — that is
the reference's `TeamClient` reborn as a copy.

**Sent up, not assumed:** *does Workforce gain a `team` — a named group of
posted staff within a department — for Jobs and Room Care to assign to?* Jobs
carries `assigned_to_team_id` and `auto_assign: TEAM` and waits for the
object.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S3-D1** | Assignee is a **person or a team**; device dropped | manual offers both; auto per the item policy; a team assignment becomes a person's on accept. **The team object is Workforce's — asked, not assumed** | **RULED, owner 2026-09-03** |
| **S3-D2** | A job may sit with a department and no person | the pool | **RULED, owner 2026-09-03** (locked) |
| **S3-D3** | Self-assign from the pool | any department member on shift | **RULED, owner 2026-09-03** (locked) |
| **S3-D4** | Auto-assignment | per the item policy — a person from the default list, or a team | **RULED, owner 2026-09-03** (locked) |
| **S3-D5** | Who may reassign | *manage* in scope; the assignee may hand back to the pool, never pick the next person | **RULED, owner 2026-09-03** (locked) |
| **S3-D6** | Reassignment and the clock | `due_at` does not move; the new assignee's own *not accepted* clock starts fresh | **RULED, owner 2026-09-03** (locked) |

**Sign-off:** **S3 SIGNED OFF — owner, 2026-09-03** (*"the others we can lock"*). One question leaves: the team object's home.

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
| **S4-D1** | The status list | the nine of S1-D9/D19: SCHEDULED · RAISED · ASSIGNED · ACCEPTED · IN_PROGRESS · ON_HOLD · RESOLVED · CLOSED · CANCELLED | **RULED, owner 2026-09-03** (locked) |
| **S4-D2** | RESOLVED and CLOSED are separate | "the technician says it's fixed" and "the supervisor agrees" are two facts | **RULED, owner 2026-09-03** (locked) |
| **S4-D3** | Who may close | *manage* in scope; a worker never closes their own work. **Auto-close N hours after RESOLVED if nobody verifies — a per-property setting, and the owner confirms it is needed** | **RULED, owner 2026-09-03** |
| **S4-D4** | Which states stop the SLA clock | ON_HOLD only; a paused session does not | **RULED, owner 2026-09-03** (locked) |
| **S4-D5** | Reopen | *manage*, within 7 days of CLOSED; the same job; `cycle` +1; a new `job_resolution` row; sessions continue; the number does not change; escalation dedupe resets | **RULED, owner 2026-09-03** (locked) |
| **S4-D6** | Cancel | *manage*; reason mandatory; cancelling a parent cancels its open children | **RULED, owner 2026-09-03** (locked) |

**Sign-off:** **S4 SIGNED OFF — owner, 2026-09-03** (*"D3 is needed — auto-close after N hours; others locked"*).

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

## S5-D1 redesigned — one timeline of alert steps, not four clocks — owner, 2026-09-03

*"D1 is exactly mirrored from Java. Each stage's alert has a different cycle:
someone is working a job with a 12-minute SLA — first alert to the assignee,
'no time, hurry up'; after the SLA (minutes or a percentage) the department
manager; after a further time or percentage, the manager. Redesign it."*

### The gaps in the reference's four clocks

| Gap | Where |
|---|---|
| **Four identifiers are code**, not data — `WO_NOT_ASSIGNED` … `WO_NOT_CLOSED` are string constants, each with its own `case` block. A fifth stage is a release | `EventExecutorServiceImpl:302–361` |
| **Five rungs are an enum** — `FIRST_ESCALATION` … `FIFTH_ESCALATION`, hard-coded in the email context to supervisor / HOD / RM / cluster / CRO | `EscalationLevel`, `putEscalationMailContext` |
| **Each clock's anchor is hard-coded** — creation, `lastAssignedOn`, `acceptedTimeInMillis`, `startTime` — one per identifier | the same four `case`s |
| **One interval unit per config** — minutes *or* percentage, per file, never per step | `DefaultEscConfig` |
| **No repeat** — a step fires once; the next rung is the only follow-up | — |
| **The condition is the identifier** — "not accepted" can only mean *assigned and not accepted*; *still open after the SLA, whoever holds it* is inexpressible | — |
| **Maintenance excluded by a string compare** | `:262` |

One defect underneath all seven: **the shape of an escalation is written in
code, so a hotel can only fill in numbers.**

### The redesign: a policy is an ordered list of steps

No identifiers, no rungs. Each step says *when*, *counting from what*, *if
the job is still where*, and *who to tell*:

```text
step
  when        after N minutes   OR   after P % of the SLA
  from        LIVE · ASSIGNED · ACCEPTED · STARTED · DUE          the anchor
  if          still in …  RAISED · ASSIGNED · ACCEPTED · IN_PROGRESS · OPEN (any)
  tell        ASSIGNEE · DEPT_SUPERVISOR · DEPT_HEAD · DUTY_MANAGER · GM · <a named role>
  message     template
  channels    in-app · email · (SMS · WhatsApp when wired)
  repeat      every N minutes until the condition clears        optional
```

The **anchors are facts the tables already hold**: `LIVE` is
`scheduled_for ?? created_at`; `ASSIGNED` the current assignment row's start;
`ACCEPTED` the status history; `STARTED` the first work session; `DUE` is
`due_at`. **The SLA is `due_at − LIVE`**, so a percentage works whether the
deadline came from the item's standard minutes or from the flow.

### The owner's example, as data — SLA 12 minutes, live at 09:00

```text
#  when       from   if           tell          message
1  at 75 %    LIVE   IN_PROGRESS  ASSIGNEE      "No time — hurry up"          09:09
2  at 100 %   LIVE   OPEN         DEPT_HEAD     "SLA breached on #441"        09:12
3  at 150 %   LIVE   OPEN         GM            "Still open, 6 min past SLA"  09:18
   repeat every 10 min until resolved
```

Three rows. Different units per step are fine (step 3 could be `+6 min from
DUE`); a different target per step; a repeat on the last. **A fourth stage
is a fourth row.**

### Nothing of the reference is lost — its four clocks as steps

```text
not assigned    +10 min  from LIVE      if RAISED     tell DEPT_SUPERVISOR
not accepted    +5 min   from ASSIGNED  if ASSIGNED   tell ASSIGNEE · then +10 min DEPT_SUPERVISOR
not started     +10 min  from ACCEPTED  if ACCEPTED   tell DEPT_SUPERVISOR
not resolved    the percentage ladder above
```

Four names gone; four behaviours as eight rows in one list — and *"tell the
duty manager if a guest job is unassigned for 3 minutes at night"* is a row,
not a release.

### Downstream

* **`job_escalation` rows are per (job, step)** — due time, outcome
  `pending · fired · cancelled · missed`, who was told. *Step* replaces
  *identifier + rung*; the outcomes are unchanged.
* **Recompute on an anchor move.** Reassignment restarts `ASSIGNED`-anchored
  steps for the new assignee (S3-D6) and leaves `LIVE`-anchored ones alone —
  the guest's timeline never restarts.
* **A step whose condition cleared is `cancelled`, with the reason** —
  *"accepted at 09:04, before the 09:05 step"*.
* **Policies are named and shared** — `property_item_policy.escalation_policy_id`;
  many items, one policy, one edit.
* **PPM jobs get their own policy** — a row, not an exclusion (S5-D9).

**Superseded below** — the owner rejected this as the reference's engine with its constants moved into rows.


### Second attempt — owner, 2026-09-03: *"still exactly mirrored from Java. Redesign our way, like type and service."*

Correct. The step list above is the reference's timer engine with the
constants moved into rows. It still thinks of escalation as **timers firing
at people** — that is the ITSM frame, and it is the part of the reference
that broke (the Quartz chains, the overdue drops, the dedupe that outlived a
reopen). Withdrawn. Start from the hotel.

### What escalation is *for* in a hotel

Not "send alert level 2". It is two questions a hotel asks all day:

```text
"Is this guest's problem slipping?"          the state of the job
"Who is carrying it right now?"               the person accountable for it
```

The reference answers neither. It fires messages and keeps no state, so
*"which jobs are in trouble right now"* is unanswerable, and *"who owns this
at 21:00"* is whoever last got an email.

### The design: a state, a ladder, and who watches

**1 · Concern — a derived state, always current, never scheduled**

Every open job is, at every moment, in exactly one of four:

```text
ON_TRACK    nothing to worry about
AT_RISK     it is slipping — 75 % of the promise used · assigned and not accepted
            for 5 min · accepted and not started for 10 min · nobody on shift
            · the assignee has too many open jobs
BREACHED    the promise is broken — past due_at
STUCK       breached, and no movement for 30 min — abandoned
```

The thresholds are the property's, per item policy. **The state is
computed, not stored** — from `due_at`, the assignment, the sessions and the
roster, exactly as *late* is. There is no timer to fire, no chain to rebuild,
nothing to miss during an outage: when the platform comes back, every job's
concern is simply what it is.

**2 · Accountable — the ladder is ownership, not notification**

In a hotel, when a job slips the real move is not *"tell the HOD"* — it is
*"the HOD is now carrying it."* So concern moves the **accountable** person:

```text
ON_TRACK    the assignee
AT_RISK     the assignee — and the department supervisor now sees it on their board
BREACHED    the department head is accountable
STUCK       the duty manager
```

`accountable` is a role resolved at that moment through Workforce; if nobody
holds it on shift, it moves one more step up **and the board says why**.
That is S5-D6 answered by construction.

**3 · Watching — who is told, and how often, is a subscription, not a rung**

```text
role              watches                     nudge
assignee          my job entering AT_RISK     "No time — hurry up"  · repeat every 5 min while AT_RISK
dept supervisor   AT_RISK in my departments   in-app
dept head         BREACHED in my department   in-app + email · repeat every 15 min while BREACHED
duty manager      STUCK property-wide         in-app + email · immediately
```

A nudge is sent when a job **enters** a state a role watches, and repeats
while it stays there. Nobody configures *when* to tell whom; they configure
*what they watch*. The owner's example is three subscriptions, and the
"different cycles per stage" the owner named are the three `repeat` values.

### The owner's 12-minute job under it — live 09:00

```text
09:09  75 % used, still IN_PROGRESS     → AT_RISK    assignee nudged; supervisor's board shows it
09:12  past due_at                      → BREACHED   accountable: dept head; head nudged
09:42  breached, no session 30 min      → STUCK      accountable: duty manager; nudged now
09:50  resolved                         → (closed)   concern history keeps all four moves
```

No timers were scheduled. The supervisor's board at 09:10 already showed the
job in amber without anyone being emailed.

### Why this is the hotel's design and not the reference's

* **It answers the two questions.** *"What is in trouble now"* is a filter
  on a state; *"who is carrying #441"* is a field. Neither exists in the
  reference or in the step list.
* **Prevention, not alarms.** AT_RISK includes *assignee overloaded* and
  *nobody on shift* — conditions the reference cannot see, and the ones a
  modern operation acts on before a breach. A supervisor's board can offer
  *"reassign to Anil — 0 open"* at 09:09, which is worth more than an email
  at 09:12.
* **The outage question (D4) dissolves.** Nothing is missed; nudges resume.
* **Reopen (D5) dissolves.** The state is recomputed; a reopened job starts
  ON_TRACK.
* **The record survives** — `job_concern_history`: when the concern changed,
  to what, why, and who became accountable. That is the audit S5-D4 wanted,
  and it replaces `job_escalation`'s pending/fired/missed rows entirely.

### What is stored

```text
job_concern_history     job_id · at · concern · reason · accountable_role · accountable_id
property_item_policy    the thresholds: at_risk_pct · not_accepted_min · not_started_min ·
                        stuck_min · overload_open_jobs
concern_subscription    per property, per role: which concerns · channels · repeat_min
```

Three things, one of them a history. `job_escalation` is gone.


### The full concept, end to end — owner, 2026-09-03: *"how do we send the alert, based on what? how do we report jobs escalated to manager level? keep the four moments — but give the full concept."*

```text
1  THE PROMISE        each item, per property, has standard minutes
                      → every job gets due_at when it is raised            (S1)

2  THE CONCERN        computed for every open job, two ways:
                        · whenever something happens to it — assigned, accepted,
                          a session starts or stops, resolved
                        · once a minute, by ONE sweep over all open jobs
                          (not a timer per job — one query, every minute)
                      → ON_TRACK · AT_RISK · BREACHED · STUCK

3  THE CHANGE         if the computed concern differs from the last recorded one,
                      ONE ROW is written:
                        job_concern_history { job, at, from, to, reason, accountable }
                      → and the event  job.concern_changed  is appended in the same
                        transaction (platform rule)

4  THE ALERT          is sent BECAUSE OF THAT ROW, and to WHOEVER WATCHES that state:
                        concern_subscription { property, role, concern, channels, repeat_min }
                      the role is resolved through Workforce right then — the actual
                      supervisor on shift, the actual head of that department
                      → nudge sent

5  THE REPEAT         while a job stays in a watched state, the same minute-sweep
                      re-nudges each watcher every repeat_min. No new rows — repeats
                      are not history

6  ACCOUNTABLE        written on the row (3): who is carrying the job at that moment
                      assignee → department head at BREACHED → duty manager at STUCK
                      nobody on shift for that role → one step up, reason recorded

7  THE BOARD          a supervisor's screen is  "concern ≠ ON_TRACK in my departments"
                      — a filter, live, no notification needed to be correct

8  THE REPORT         from the history rows, never from the alerts
```

**"Based on what?" — on the transition.** Not on a clock reaching a
number; on the job *entering* a state somebody watches. The clock only
decides *when the state changes*; the subscription decides *who hears*.

### The alert, as data — the owner's 12-minute job

```json
09:09  the minute-sweep finds 75 % of the promise used
       → history { job: 441, from: "ON_TRACK", to: "AT_RISK",
                   reason: "75% of promise used", accountable: {role: "ASSIGNEE", id: "u-suresh"} }
       → event   job.concern_changed
       → subscriptions watching AT_RISK at KOC:
            ASSIGNEE        → Suresh    in-app   "No time — hurry up"   repeat 5 min
            DEPT_SUPERVISOR → Anil      in-app   (resolved via Workforce: ENG supervisor on shift now)

09:12  past due_at
       → history { 441, "AT_RISK" → "BREACHED", "past due_at",
                   accountable: {role: "DEPT_HEAD", id: "u-menon"} }
       → DEPT_HEAD → Menon   in-app + email   repeat 15 min
```

### The report, simply

*"How many jobs escalated to manager level this month?"* is one question of
the history table:

```sql
SELECT count(DISTINCT job_id)
FROM   jobs.job_concern_history
WHERE  accountable_role IN ('DEPT_HEAD', 'DUTY_MANAGER')
AND    at >= date_trunc('month', now())
```

And its neighbours, all from the same rows:

```text
how many breached                   to = BREACHED
average minutes AT_RISK before      pair each AT_RISK row with the next row on that job
  breach or recovery
who was accountable when resolved   the job's last history row before RESOLVED
which items breach most             join item_id · group by
which department's supervisor       group by accountable_id where role = DEPT_SUPERVISOR
  carried the most
```

Every report is a query on one table, because every escalation the hotel
cares about was a **change of state**, and each change is one row.


### The thresholds AND the ladder are policy — owner, 2026-09-03

*"The accountable and the concern time are based on policy, right? Because
by category or department it varies — for some issues, AT_RISK should
already make the manager accountable. Can we get that flexibility?"*

Yes — and the ladder above was written as fixed, which was wrong. Both halves
belong to one named **concern policy**, per property:

```text
concern_policy                                  a named object, per property
  for each state:  what enters it   +   who becomes accountable

"Guest-room emergency"                          "Back-of-house routine"
  AT_RISK    50 % · not accepted 2 min          AT_RISK    90 %
             → accountable DEPT_HEAD                       → accountable ASSIGNEE
  BREACHED   past due_at                        BREACHED   past due_at
             → DUTY_MANAGER                                → DEPT_SUPERVISOR
  STUCK      15 min no movement                 STUCK      2 h no movement
             → GM                                          → DEPT_HEAD
  overload   3 open jobs                        overload   8 open jobs
```

**Which policy a job gets — most specific wins:**

```text
item      property_item_policy.concern_policy_id      "AC not cooling, occupied room"
category  the category's default                       all of Engineering's AC items
dept      the department's default                     everything Housekeeping does
property  the property default                         anything not covered above
```

A job is stamped with the policy it resolved to when it goes live, so a
later policy edit changes future jobs, not the history of this one.

**Subscriptions stay per role and are independent of the policy** — a
department head who watches BREACHED sees it whichever policy put the job
there. A subscription may narrow itself to a department or a category
(*"I watch AT_RISK only for guest-room items"*), and that is the only
filter it needs.

So the two knobs a hotel turns are separate and both are theirs:

```text
the POLICY        how fast this kind of job becomes a worry, and who carries it then
the SUBSCRIPTION  what I, in my role, want to be told about, and how often
```

**S5-D3 is this**: policies live in the `jobs` schema, edited in Jobs
Settings, named, shared, resolved item → category → department → property.


### D7 — RULED: Jobs' own. Owner, 2026-09-03

*"Keep it separate — each app has its own logic and flow; all under one gets
messy."* Ruled. The concern model below is Jobs', built for jobs, keyed by
`job_id`; the lift-out shape proposed underneath is dropped. The reasoning
that follows is kept as the record of what was considered.

### D7, simply — whose capability is this? — owner, 2026-09-03

**The question.** Room Care will want exactly this: *"room not cleaned 30
minutes after checkout → AT_RISK → the floor supervisor"*. Engineering's
PPM: *"service overdue → BREACHED → the head"*. GuestOps: *"request
unanswered"*. If each application builds its own, there are three copies
that drift — which is the platform's no-duplicated-shared-code rule, and it
is also the reference's own history: it had **four** escalation engines.

**The split the platform already uses — mechanism versus meaning.**

```text
MECHANISM — the same for everyone                shared
  the four states · the history table · the minute sweep ·
  subscriptions per role · nudges and repeats · the board filter

MEANING — different for each application         each app's own
  WHAT puts a job at risk        due_at · accepted · sessions · roster
  WHAT puts a room at risk       checkout time · arrival time · cleaning credits
  WHO becomes accountable        each app's policy ladder
```

The platform's rule: *shared means mechanism, never meaning.* So the answer
is not "Jobs' or the platform's" — it is **both, on that line**.

**What Jobs does now.** Builds it, inside Jobs, as its own module — but
shaped so the mechanism can be lifted out without a rewrite:

```text
job_concern_history   →  concern_history { subject_kind: "job", subject_id, … }
concern_subscription  →  already has no job-specific field
the sweep             →  asks each subject kind "compute your state" and compares
```

One column name is the whole cost of lifting it later.

**Who decides.** The architect — a new platform capability is above an
application round. The owner's part is only: *ask now, or let Jobs build
first and ask with the working thing in hand.* The stream recommends the
second: build it in Jobs with the lift-out shape, and send the question up
with the code rather than before it.


### D8 — the night, and the property with no night staff — owner, 2026-09-03

*"We need an on/off option — some properties have no staff at night. Your
opinion?"*

**Opinion: not an on/off for escalation — quiet hours that pause the
clock.** Two reasons, and the second is the one that matters.

**One — nudges already go nowhere when nobody is there.** Concern still
computes at 02:00, but *accountable* resolves through Workforce, and a role
with nobody on shift is empty. An empty role is not nudged. So a property
with no night staff gets no night nudges **without any switch** — and at
07:00 the board and the history show *"breached at 02:14, nobody on shift"*,
which is exactly what a manager wants to see first thing.

**Two — but the promise should not burn while nobody can act.** A towel
asked for at 23:30 at a hotel that closes its desk at 23:00 is not "12
minutes late" at 23:42; it is *"first thing tomorrow"*. That is a **clock**
question, not a notification one, and it needs a setting:

```text
quiet_hours          per property, optionally per department
                     e.g. 23:00–07:00 · Housekeeping 22:00–06:00
                     off by default — a 24-hour hotel sets nothing

during quiet hours   the promise clock PAUSES (as ON_HOLD does)
                     concern is frozen where it stands
                     no nudges
                     due_at is computed skipping the window — raised 23:30 with a
                     30-minute promise → due 07:28, not 00:00

exempt               priority EMERGENCY ignores quiet hours — a flood or a lift
                     entrapment does not wait for the morning, and the policy for
                     it names who is called at night (the duty manager, security)
```

So the "off" the owner wants is real, it is per property and per
department, and it is a pause of the clock rather than a silence of the
alerts — which keeps the history honest: a job that waited the night is
*paused*, not *ignored*.

**Where the setting lives, and one thing to send up.** For now,
`quiet_hours` sits on the property's Jobs settings. But *when a department
operates* is a fact about the department, not about jobs — the same kind of
fact as the holiday calendar that WF-Q16 placed in Core Administration. If
the platform gains department operating hours, Jobs reads them and its own
setting goes. Recorded, not blocking.


## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S5-D1** | ~~four clocks~~ ~~a list of timer steps~~ → **a derived CONCERN state (ON_TRACK · AT_RISK · BREACHED · STUCK), an ACCOUNTABLE ladder that moves ownership, and per-role SUBSCRIPTIONS**. No timers, no rungs, nothing to miss | *redesigned, second attempt — awaiting owner* |
| **S5-D2** | ~~Rungs~~ — dissolved: the ladder is *who is accountable*, four levels, roles the property's own | *follows D1* |
| **S5-D3** | **A named concern policy** — per state, both the threshold *and* who becomes accountable; resolved item → category → department → property; stamped on the job when it goes live; subscriptions per role, independent of it | **RULED, owner 2026-09-03** — the flexibility the owner asked for |
| **S5-D4** | After an outage | **dissolves** — concern is computed, so nothing is missed; nudges resume; the history records the moves | *follows D1* |
| **S5-D5** | Reopen | **dissolves** — the state is recomputed; a reopened job starts ON_TRACK | *follows D1* |
| **S5-D6** | Nobody holds the accountable role on shift | accountability moves one more step up **and the board says why** — by construction | *follows D1* |
| **S5-D7** | Whose capability | **RULED: Jobs' own.** Owner, 2026-09-03: *"keep it separate — each app has its own logic and flow; all under one gets messy."* The concern model is built inside Jobs, for jobs, with `job_id` — the lift-out shape is dropped. Room Care and the others design their own when they come. *(Recorded beside it: the constitution's no-duplicated-shared-code rule may be raised by the architect at the platform level; that is theirs, and this ruling stands until then.)* | **RULED, owner 2026-09-03** |
| **S5-D8** | The night | **quiet hours** per property / department pause the promise clock and freeze concern; off by default; EMERGENCY exempt with its own night policy. Nudges to empty roles never send regardless. *Where department operating hours live* goes up, not blocking | *proposed on the owner's question — awaiting owner* |
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

# S9 · Who can see and manage a job

**State: DESIGNED — owner's four levels, 2026-09-03.** *"An important thing
I missed: who can see and manage a job."* Its own section, as the owner
asked.

## The owner's four levels, verbatim

```text
1  a normal user      sees only their own jobs; can execute and say resolved
2  the next level     can also reassign to someone, change priority, and such
3  the next           sees everything in MY departments, including mine
4  the top            sees all jobs in ANY department, assigned to anyone —
                      can reassign, capture to self from someone, do anything
```

## The model: two axes, because that is how the platform authorises

The four levels are combinations of **what you can see** and **what you can
do**. Keeping the axes separate is what lets the platform's authorization
(OpenFGA, per-operation, manifest-declared — AUTHZ-Q25) express them without
a role table of its own.

```text
SCOPE — what you can see
  own            jobs assigned to me, or raised by me
  department     every job in a department I am POSTED to   (Workforce — ADR 0116 §6)
  property       every job at the property

POWER — what you can do to a job you can see
  execute        accept · start / pause / resume / stop · resolve · note · photo
  manage         + reassign · change priority · reschedule · put on hold ·
                   cancel · close (verify)
  administer     + capture from anyone · edit any field · delete (soft)
```

The owner's four, as combinations — **the defaults, and the second is the
one to confirm**:

| Level | Scope | Power | Typical |
|---|---|---|---|
| 1 | own | execute | technician, attendant, runner |
| 2 | own | manage | senior technician — hands off their own, re-prioritises their own |
| 3 | department | manage | department supervisor, HOD |
| 4 | property | administer | duty manager, operations manager, property admin |

**Property admin holds everything always** (ADR 0116 §5). Department
membership is **never stored in Jobs** — it derives from Workforce postings,
permanently, and Jobs asks Context (ADR 0116 §6).

## What each power means at the edges, so nothing is argued at build time

```text
"capture to self from someone"      administer only. Manage may reassign to
                                    anyone in scope; taking a job OFF a person
                                    onto yourself is the stronger act
"resolve"                           execute — but only by an assignee, or
                                    anyone with manage in scope
"close" (verify)                    manage. A worker never closes their own
                                    work (S4-D2/D3)
"change scheduled_for"              the raiser, or manage in scope (S1.16 ·3)
"delete"                            administer, reason required, audited
"see a job's notes and photos"      follows the job's scope — no separate rule
guest-raised job, nobody assigned   visible to department scope of the item's
                                    department; the pool
```

## How it reaches the platform

The manifest declares the permissions; the Kernel enforces per operation.
Six names, and nothing else:

```text
job.view.own · job.view.department · job.view.property
job.execute  · job.manage          · job.administer
```

A screen shows a button only when the Kernel would allow the operation; the
Kernel refuses regardless of the screen. **Jobs caches no decision**
(01 §F14 is the reference doing exactly that, for ten minutes).

## The owner's correction — levels are not fixed; scope and power are set per user

*"May or may not. Sometimes a normal user is given the manage permission. But
a security user — we do not give it; they must just execute their job. And I
need to know where we configure the scope and power."*

So **S9-D2 is answered by dissolving it**: there is no fixed "level 2". The
two axes are granted **per user**, in any combination the property wants.

```text
technician Suresh      department · execute            sees his department, does the work
senior tech Anil       department · manage             same view, may reassign and re-prioritise
security guard Ravi    own · execute                   his jobs, nothing else
duty manager Priya     property · administer           everything
```

### Where scope and power are configured — measured against the platform

Two of the three pieces exist today; the third is the question to send up.

| | Where | State |
|---|---|---|
| **May this user open Jobs at all?** | **Core Administration → User → Applications card** (ADR 0114 consequences; ADR 0116 §5 — every app is per-user gateable) | built |
| **Which departments is this user in?** | **Workforce → postings** — never a Jobs setting, never a Core Admin toggle (ADR 0116 §6) | built |
| **Which Jobs permissions does this user hold** — the six from §"How it reaches the platform"? | The permission *names* are manifest-declared (`permissions.yaml`, `required_permission`) and the grant flows Identity → event → Kernel (ADR 0125 §2, §6). **A per-user, per-application permission screen is not identified in the ADRs read** — the User → Access card is where it belongs, and whether it carries per-application permissions or only platform ones is **the architect's to say** | **open — goes up** |

**What Jobs must not do:** build its own role screen. That is the reference's
`PermissionEvaluator` reborn — two methods returning `true` and a cache in
front of the authority (01 §F14). One place grants; the Kernel decides; Jobs
asks per operation.

## The top level is not admin-only — the GM must be able to give it — owner, 2026-09-03

*"Sees all in my departments and manages all in the department; and
'everything, anyone's' — not only the admin. The property GM must be able to
give that permission."*

Two corrections to the table above, and the second needs one thing the
platform does not have yet.

**Department level, restated.** A department **supervisor** both *sees* every
job in the department and *manages* it — assign, reassign, re-prioritise,
reschedule, hold, close. One relation, both halves; a person posted as
supervisor of two departments has it in both.

**Property level, corrected.** *"Everything, anyone's"* today resolves only to
`property#admin`, who is **born at activation and never appointed** (ADR
0116 §3) — so nobody can *give* it, which is exactly the owner's objection.
The shape that fits the platform:

```text
a new relation on the property     property#jobs_manager      (name provisional)
who holds it                       whoever the GM or the admin grants it to
what it resolves                   job.viewer · can_assign · can_close · capture-from-anyone
                                   for every job at the property, any department
who may grant / revoke it          general_manager or admin of the property
                                   — one permission for both directions (ADR 0125 §6)
```

**How it reaches the platform — the route already ruled.** Jobs does not
write a tuple; it **declares a grant kind in its manifest** (AUTHZ-Q25 —
manifest-declared, materialised by the Kernel from the manifest it stores,
shown on the install approval screen, folded from the event store on
rebuild, removed with the package). The GM's action in Jobs publishes
`user.jobs_manager_granted` / `_revoked`; the Kernel materialises
`property#jobs_manager`. This is the first grant kind Jobs declares — R30
said *"if Jobs grants anything at all"*, and this is the thing.

**The one platform item, stated not decided:** AUTHZ-Q25's ruling also says
a declared kind may only name a relation listed in the **grantable-relations
registry** — a platform file — so `property#jobs_manager` must be *added
there* before the manifest can declare it, and the registry's own rule is that
escalations to `property#admin` are inexpressible. A relation that grants
*every job at the property* is a large one; whether the registry admits it is
the architect's, and it goes up **with** the parked vocabulary discussion,
not before it.

## Decisions

| id | Decision | Ruling |
|---|---|---|
| **S9-D1** | Two axes — scope (own · department · property) and power (execute · manage · administer) | *proposed* |
| **S9-D2** | **No fixed levels — scope and power are granted per user**, in any combination (a security guard: own + execute; a senior tech: department + manage) | **RULED, owner 2026-09-03** |
| **S9-D6** | **Where it is configured** — app access: Core Admin Applications card, Identity `SetApplicationAccess` (built); departments: Workforce postings (built); **Jobs powers: Workforce** — member / supervisor / head of a department (§S1.16 ·10). Not a Jobs screen, not a Core Admin card | **answered** — §S1.16 ·10 |
| **S9-D3** | *Capture from someone* is property-level — `property#admin` **or `property#jobs_manager`** | *proposed* |
| **S9-D7** | **The property-wide level is grantable by the GM** — a Jobs-declared grant kind `property#jobs_manager` (AUTHZ-Q25's route); granted/revoked by the GM or admin; needs a grantable-relations-registry entry | **RULED as a requirement, owner 2026-09-03**; the registry entry is the architect's |
| **S9-D4** | Can a job be **restricted** — a complaint about a staff member — visible only to the raiser and level 4? | *proposed: yes, a flag* |
| **S9-D5** | ~~Six manifest permissions~~ — the platform already has the five actions and the relations (`model.fga` `type job`). **Parked for the owner's separate discussion**, as asked | *parked* |

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
| Sections signed off | **4 of 10** |
| Page locked | no |
| Locked on | — |

When it locks, three things follow, in this order and no other:

1. every ruling recorded in the platform's question register;
2. an ADR for the decisions that change the platform rather than only this
   application — at minimum `S5-D7` (whose escalation is it) and `S1-D6`
   (who owns the service taxonomy);
3. the Jobs design chapter, written against the locked page.

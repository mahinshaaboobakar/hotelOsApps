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
| S1 | The job itself — what a job *is* | **round 1 discussed** — 4 ruled, 4 designs with the owner | — |
| S2 | Creating a job | queued | — |
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

**State: ROUND 1 DISCUSSED — four decisions settled, four sent back for a
design.** Owner, 2026-09-02.

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
| **S1-D2** | One subject per job | **RULED: one subject, one job.** And jobs **link to one another** — a guest asking for water then a towel produces two linked jobs, and the second inherits the first's handler. See §S1.2 |
| **S1-D3** | Job number | **RULED: `<PropertyCode>-<Dept>-<Number>`**, the number shared across departments at the property. See §S1.3 |
| **S1-D4** | Priority levels | **RULED: Emergency · High · Normal · Low, plus Not triaged.** How it is set automatically **sent back for a design** — see §S1.4 |
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

---

## S1.2 · Linking jobs — the design

Owner's case: *a guest asks for water, then a towel. The system connects the
two, and if the first went to a person, the second goes to the same person.*

**A group, not a parent and a child.** The reference used parent/child and
made closing the parent close the children — which is wrong here: the towel
arriving does not make the water a sub-task of it, and delivering the water
does not deliver the towel.

```text
Job group  GRP-88
   │
   ├── Job KOC-HK-412   water        assigned → Ramesh      Done  09:12
   └── Job KOC-HK-413   towel        assigned → Ramesh      Open
```

**How a job joins a group** — automatic, on a match key:

```text
same requester   +   same location   +   inside the join window
                                          (default 30 minutes, per property)
```

If an open group matches, the new job joins it. If not, it starts its own
group of one. Staff can also **link and unlink by hand**, and that is
recorded.

**What the group does:**

* **It carries the handler.** A job joining a group is assigned to the
  group's current handler — *if* that person is still on shift and the job's
  department is theirs. Otherwise the job routes normally and the group
  simply keeps the two visible together.
* **It is one card on the runner's screen** — *"Room 214 — 2 items: water,
  towel"* — so one trip is one trip.
* **It does not merge the jobs.** Each keeps its own status, its own SLA and
  its own completion. **Closing one closes nothing else.**

**The question underneath it, and it matters:** water is In-Room Dining;
a towel is Housekeeping. In a small hotel one runner does both; in a large
one they are different people in different uniforms. So the handler is
inherited **only when the department matches**, and *"one runner handles
everything"* is a property setting for hotels where it is true.

**Open for the owner:**

* The join window — 30 minutes, or shorter?
* Should the group span departments and assign to one person anyway, at
  properties that want that?
* Does the group need its own visible number, or is it enough that the two
  jobs point at each other?

---

## S1.3 · The job number — the design

**Ruled format:** `<PropertyCode>-<Dept>-<Number>`, with the number running
property-wide rather than per department — so `KOC-HK-412` is followed by
`KOC-ENG-413`.

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

**Open for the owner:**

* `Property.Code`'s own example is `kochi-001`, which would give
  `kochi-001-HK-412`. Do you want a **short uppercase code** convention —
  `KOC`, `GOA`, `BLR` — and if so is that a rule we ask Core Administration
  to enforce, or just how you fill it in?
* What happens to a job raised before its department is known — a guest
  request that has not been routed yet? Options: hold the number until it is
  routed, or stamp a neutral segment (`GEN`) and never change it. The stream
  recommends **routing at creation always**, so the case does not arise —
  which depends on S1.6's catalogue carrying a default department per
  service.

---

## S1.4 · How priority is set automatically — the design

Levels ruled: **Emergency · High · Normal · Low**, plus **Not triaged**.

Priority is decided by four layers, **first match wins**, and every job
records *which layer decided it* — so *"why is this Emergency?"* always has
an answer.

```text
1  the person chose one          they picked it on the form        → use it
2  a property rule matched       "complaint + occupied room"       → use it, name the rule
3  the service's default         the catalogue says AC = High      → use it
4  nothing matched               → NOT TRIAGED, and that is a real state
                                   a supervisor can filter for it
```

**Layer 2 is the useful one.** A rule is *conditions → priority*, built from
a **fixed, small set of conditions** — not a general expression language.
(The reference shipped a general rules engine with conditions, operators and
a join-by field; it has no callers, its validator returns `true` under a
`TODO`, and it ignores the join-by. A closed condition set is what makes
rules reviewable.)

The conditions worth having:

| Condition | Comes from | Note |
|---|---|---|
| job type | the job | Complaint / Request / Fault / Planned |
| service | the job | AC · Plumbing · Towel · WiFi |
| place kind | the location tree | guest room · public area · back of house |
| **is the room occupied right now** | Context | **the single most valuable one in a hotel** |
| is the guest a VIP | Context / GuestOps | if GuestOps is not installed the condition simply never matches — it never blocks the flow |
| time of day / business day | the property's calendar | night rules differ |
| asset criticality | Maintenance, when installed | same absence rule |

Rules a hotel would actually write, in order:

```text
service is fire · lift-entrapment · gas · flood      →  EMERGENCY   always
type = Complaint  AND  guest room  AND  occupied     →  HIGH
   ... and service is AC or Plumbing                 →  EMERGENCY
type = Request    AND  guest room  AND  occupied     →  NORMAL
place is back of house                               →  LOW
```

Three rules about the rules:

1. **A human override always wins and always sticks.** Nothing re-runs and
   quietly undoes it. The override records who and why.
2. **Priority does not drift upward as a deadline approaches.** That is
   escalation's job, and mixing the two is precisely how the reference ended
   up with an `ESCALATED` value in its *status* field. Priority says how
   important this is; escalation says who to wake up.
3. **A rule that depends on an absent application never fires and never
   fails.** No GuestOps means no occupancy condition, which means the next
   rule is tried. (ADR 0116 §5: *absent is not blocking*.)

**Open for the owner:** is "occupied room" available to us? It depends on
GuestOps or the PMS connector being installed. If a property runs Jobs alone,
the occupancy condition is dead and the rule set has to work without it.

---

## S1.5 · Job types — the design

**The test for what belongs here:** a type is not *where it came from* (that
is `source`) and not *what it is about* (that is `service`). A type is
**why the job exists and what "done" means**.

Applying that test gives five, not three:

| Type | Why it exists | "Done" means | Clock |
|---|---|---|---|
| **Complaint** | something is wrong **and someone is affected** | fixed **and the affected person told** | shortest |
| **Request** | someone wants something brought or done | delivered | short |
| **Fault** | something is broken and nobody has complained yet | repaired | medium |
| **Planned** | scheduled or preventive work | performed and recorded | by its schedule |
| **Inspection** | go and look; the output is a finding, not a fix | checklist completed | by its schedule |

**Why Complaint and Fault are separate**, when the repair may be identical: a
complaint is not finished when the tap stops dripping — it is finished when
somebody tells the guest. That closing step exists on one and not the other,
and hotels feel the difference sharply. Collapsing them is how a hotel ends
up with fixed taps and angry guests.

**Why "Maintenance" is not a type.** The reference has one, and it then needs
a special case at the top of the escalation engine to exclude it, and another
in the checklist validator to permit only it. That is the smell: "Maintenance"
was doing the work of two types — unplanned **Fault** and scheduled
**Planned** — which have different deadlines and different escalation.

**What is deliberately not a type:**

* **Incident** — an injury, a theft, a fire alarm. Different lifecycle,
  different confidentiality, a legal record, and often no repair at all. An
  incident *causes* jobs; it is not one. That belongs to Security.
* **Guest / Staff** — that is `source`, and it is already a separate field.

**Open for the owner:**

* Is five the right set, and is **Fault** worth separating from **Complaint**
  for your operation?
* **Inspection**: the reference calls out to a separate checklist service for
  this and the call is commented out. Is inspection Jobs' work, or a separate
  application that raises Jobs when it finds something?
* Is the type list fixed in the product, or may a hotel add one? (Strong
  recommendation: **fixed** — every type needs behaviour written for it, and a
  type nobody wrote behaviour for is a type that silently does nothing. That
  is exactly what happened to `DEVICE` in the reference.)

---

## S1.6 · Who owns the service list — the design

The list: *AC · Plumbing · WiFi · Towel · Water · Turndown · Lift · Laundry
pickup …* — what a job can be **about**.

**The reference's mistake, stated plainly:** its catalogue entry is *also*
the routing policy. One document holds the service's name **and** its
department, its default assignee, its SLA, its priority, its icon, its
keywords and its tracking mode. So the vocabulary and the behaviour cannot be
owned separately, and Room Care reading the vocabulary would inherit Jobs'
behaviour.

**The design: split the noun from the policy.**

```text
THE CATALOGUE — what can be asked for            shared, Core Administration
    code · name · translations · icon
    the department that owns it by default
    whether a guest may request it
    active / inactive

THE ROUTING POLICY — what we do about it         Jobs' own, per property
    default priority          default SLA
    which escalation policy applies
    the auto-assignment rule
```

**Why the split is the right cut.** The catalogue is a *vocabulary several
applications must agree on*: GuestOps shows a guest a list to choose from,
Room Care raises "turndown", Maintenance raises "AC service", Jobs raises
"AC not cooling" — and a group's reports only add up if all four mean the
same thing by "AC". The policy is Jobs' behaviour, and no other application
should ever read it.

**Answering the owner's question — same across properties, or per hotel?**
The platform has already ruled this exact shape once, for departments (ADR
0119 and ADR 0116 §4), and the same answer fits:

```text
the catalogue     organization-wide, seeded with the product
                  each property ACTIVATES the ones it offers
                  a property may RENAME for display; the code never moves
                  → group reporting can never fragment

the policy        per property, always
                  Kochi's AC response time is not Goa's
```

That is the department canon's exact pattern, and it exists because a
property-local list means two hotels invent two spellings of one thing and
the group's report has two rows for the same service. The accepted cost is
the same too: a hotel needing a service the catalogue lacks waits for it to
be added centrally — which argues for shipping a **generous** list, exactly
as the department canon did with 45 codes.

**This needs an architect's ruling, because it puts a new object in Core
Administration.** It is one of the questions this round sends up.

**Open for the owner, and it decides how hard the ruling is:** are your
properties' service lists genuinely the same today, or does each hotel run
its own? If they already differ per hotel, activation-plus-rename has to
absorb that, and we should see a real example of the difference before
proposing it.

---

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

## Decisions — round 1 close

| id | Decision | Ruling |
|---|---|---|
| **S1-D1** | A job carries `location_id` (any node in Master Data's one tree) and an optional `asset_id` | *design proposed — §S1.1 — awaiting owner* |
| **S1-D2** | One subject per job; jobs link into a group; the handler is inherited within a department | *design proposed — §S1.2 — awaiting owner* |
| **S1-D3** | `<PropertyCode>-<RootDept>-<Number>`, number property-wide, stamped once | **RULED** — two details open in §S1.3 |
| **S1-D4** | Emergency · High · Normal · Low · Not triaged | **RULED.** The automatic rule design is §S1.4 — awaiting owner |
| **S1-D5** | Complaint · Request · Fault · Planned · Inspection | *design proposed — §S1.5 — awaiting owner* |
| **S1-D6** | Catalogue in Core Administration, routing policy in Jobs | *design proposed — §S1.6 — needs an architect ruling* |
| **S1-D7** | `category` dropped | **RULED** |
| **S1-D8** | One scope column, `property_id` | **RULED** — reasoning in §S1.7 |

**Sign-off:** _not yet — four designs are with the owner._

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

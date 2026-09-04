# 05 · Two requests from Jobs — the team object, and the shift fan-out

**Status:** **ruled and built, 2026-09-04.** Stream GG. Written first as a
proposal with eight open questions; all eight were answered the same day and the
build followed. The design below is unchanged from the proposal except where the
rulings changed it, and §"The questions" now records the answers — kept as one
page rather than split, so the reasoning and its ruling are read together.

**What was asked**, both `RULED, owner 2026-09-03` inside Jobs' own design of
record (`jobs/docs/chapters/02`, JOBS-Q1 makes it the design of record):

| | |
|---|---|
| **S3-D1** | a **team** — *"a named group of posted staff within a department"* — for Jobs and Room Care to assign to. `assigned_to_team_id` and `auto_assign: TEAM` are carried and wait |
| **S5-D13** | **`shift.started` / `shift.ended`**, a *"Temporal-scheduled fan-out from Workforce, one subject, department in payload"*, keeping Jobs' `department_presence` |

**Neither gates HH's day one.** Presence runs on service hours until the events
exist; person assignment is unaffected by the team's absence. That is stated
here as well as there so the two pages cannot drift on it.

---

## Part A · The team object

### A1 · Measured first

**There is no team anywhere on the platform.** Checked rather than assumed:

```text
infrastructure/openfga/model.fga          no team type, no team relation
infrastructure/openfga/permissions.yaml   no team permission
shared/protos/hotelos/masterdata/…        no Team message
services/masterdata-service/src/Domain/   no Team entity
workforce/backend/src/Domain/             no Team entity
```

HH's measurement holds. It is new, and its home is a ruling.

### A2 · It is Workforce's — and the Zone precedent is the reason to check

HH cited **ADR 0063 §Q5**: *"if an attribute exists primarily to determine
operational assignment or workforce capability, it belongs to Roster /
Workforce."* That is the right rule and it gives the right answer. But the
**same ADR's §Q4 points the other way**, and anybody re-deriving this later will
hit it:

```text
Q4  Zone                      a canonical organizational or physical area   →  Master Data
    RoomZoneAssignment        which rooms are in it today                   →  the application
```

A zone is *a thing*, so it stayed in Core even though its entire use is
operational and `room.housekeeping_operator` derives from it. Applied
mechanically, that precedent would split a team the same way — the named group
to Master Data, the membership to Workforce.

**It does not transfer, and the reason is the one word that differs.** A zone is
a **place**. The West Wing is an area of the property whether or not anybody
works it, whether or not Room Care is installed, and whether or not a single
room is assigned to it. A team is **people**, and ADR 0051's test settles it:

> *If every application except Core Administration were uninstalled, would this
> still describe what this entity **is**?*

```text
"West Wing, floors 3–5"        still an area of the hotel        →  a thing
"Housekeeping Team A"          with no postings, nobody is in    →  a list of nobody
                               it; the name describes nothing
```

A team has no existence apart from its members, and its members exist because
they hold postings — which ADR 0063 §Q5 already sent here. Every
**people-grouping** relationship in that ruling went to Workforce (`Staff →
Department`, `Staff → reporting manager`, `Department → current head`); the
structural hierarchies that stayed in Core are all **place or org shape**
(Building → Floor → Room, Department → parent Department). A team is the first
kind.

**So: Workforce, whole — identity and membership, not split.** Splitting it
would put a name in Core that Core could never populate, and would be the
`VendorAssignment` mistake ADR 0052 refused — a Core relationship justified by
symmetry with another entity rather than by an invariant Core needs.

### A3 · A team is not a zone, and both will exist

Worth stating once, out loud, because a hotel will use the words
interchangeably and the platform must not:

```text
zone    WHERE the work is        Master Data · a place · already exists · already on the posting (WF-Q7)
team    WHO does it together     Workforce  · people · new
```

A property that organises by area assigns to zones; one that organises by crew
assigns to teams. The posting is where they meet — it already carries a
`zone_id`, and it would carry team membership by reference from the other side.
Nothing here proposes merging them.

### A4 · The shape

```text
workforce.team
  id · property_id
  department_code        the team lives in exactly one department
  name                   the property's own word — no canon, no code list
  active                 ADR 0062's flag
  deleted_at             ADR 0062's logical removal
  created_at · updated_at · version

workforce.team_member
  id · property_id · team_id · staff_id
  joined_on · left_on    effective-dated, like a posting
  created_at · updated_at · version
  UNIQUE (team_id, staff_id) WHERE left_on IS NULL
```

**One department per team**, because assignment routing is departmental: a team
spanning two departments makes *"which pool does this job sit in"* unanswerable,
and Jobs' `department_presence`, concern policy and accountability ladder are
all departmental. A cross-department task force is a real thing and is not this;
it can be asked for when a property asks.

**Three invariants, and the third is the one that bites:**

1. A member holds a **posting in force in the team's department**. A team exists
   to receive work in that department, and a member who cannot be assigned there
   is a row that lies.
2. **One live membership per person per team** — the unique index, not a check
   in a service.
3. **A posting ending ends the membership.** This is the same class as
   `StaffPropertyScope` and `StaffChangeConsumer`: without it, a team can be
   assigned work and route it to somebody who left the department last month,
   and nothing anywhere would say so. It is not a nightly job — it is a
   consequence of `EndPosting`, in the same transaction.

**Lifecycle:** `active` + `deleted_at`, the verbs **Deactivate / Reactivate** —
ADR 0062's vocabulary, which JOBS-Q1 ruling (6) confirms an application uses for
logical removal rather than minting a third spelling. A team stood down for the
season deactivates; it does not vanish, because jobs assigned to it are in
somebody's history.

**Permission:** `posting.assign` — *"Post people to departments, set headship and
reporting lines, and end a posting."* Forming a team is the same authority over
the same question (who works where, with whom) and needs no thirteenth
permission. Offered rather than asserted; see the questions.

### A5 · Deliberately not in v1

* **No team lead.** Jobs' S3-D1 says a team assignment becomes a person's on
  accept, so nothing needs one. Accountability is the concern ladder's, and it
  runs on roles.
* **No nested teams.** A team of teams answers no question anybody has asked.
* **No `team` events, and this is a finding rather than a preference** — see A6.
* **No authorization object.** Nothing needs `team#member` today: Jobs
  authorizes assignment with `job#can_assign`, on the job. The moment something
  does, ADR 0061 applies — the Kernel materialises from lifecycle events — and
  A6 is what stands in the way.

### A6 · A team cannot be announced today

Workforce's event domains are pre-named in the Kernel's stream routing
(`services/kernel/crates/kernel/src/events/streams.rs`): `shift`, `leave`,
`duty`, `attendance`, and `user` for postings. **`team` is not among them**, and
that file says exactly what happens to a subject that is not:

> *an unrouted subject is **acked, matches nothing, and dead-letters silently**,
> so publishing into an unclaimed stream is worse than not publishing — it looks
> like it worked.*

So a `team.created` published today would vanish quietly. Three ways out, and
the choice is not mine:

```text
1  no team events in v1        nothing subscribes yet; Jobs reads teams, it does not react to them
2  route property.*.team.>     one line in the Kernel's list — the same interim `shift` had
3  PKG-Q39                     manifest-declared domains materialised at install — the real fix,
                               and the reason that file calls its own list "not a mechanism"
```

**Recommendation: (1) for v1, and (2) the day anything subscribes** — with the
observation that a third pre-named domain arriving from one application is
itself an argument for PKG-Q39.

---

## Part B · `shift.started` and `shift.ended`

### B1 · What Jobs actually needs, and what was asked for

The request is two subjects. The *use* is one boolean:

```text
department_presence.staffed     true / false, per department, per property
```

That difference is where the design work is, because **the verb does not answer
the question**. Consider the ordinary 15:00 handover in a department running
Morning 07:00–15:00 and Afternoon 15:00–23:00:

```text
15:00   shift.ended   (Morning)    →  consumer sets staffed = false
15:00   shift.started (Afternoon)  →  consumer sets staffed = true
```

Two events at one instant, and the department's presence is **whichever arrived
last**. Delivery order is not guaranteed; if they land the other way the
department reads unstaffed all afternoon, every job in it pauses, and the
concern clock stops for four hours. Nothing would look broken.

### B2 · The fix: the event carries the count

**One extra field removes the whole class**, and it is a fact only Workforce can
compute:

```text
shift.started / shift.ended
  property_id
  department_code
  shift_id            the catalogue entry — which shift began or finished
  business_date       the rota date the cells belong to (a night shift ends on the NEXT day)
  at                  the boundary instant
  on_now_after        how many people are covered in that department immediately after it
```

`staffed = on_now_after > 0`. At the handover both events carry `14`, so the
boolean lands correctly whichever arrives last, and the ordering hazard is gone
rather than mitigated.

**Why only Workforce can compute it.** `on_now_after` is not "count the rota
rows": a shift may cross midnight (Night is written `23:00 → 07:00`, so the
people working at 06:00 are on *yesterday's* cell) and may be split
(`10–14, 18–22`, so somebody is genuinely off at four). That logic now exists —
`Domain/ShiftCoverage.cs`, built for the Shift Board widget and tested for both
shapes. Publishing raw boundaries without the count would push it into every
consumer, and Jobs would end up with a second implementation of it: the
`TeamClient`-reborn failure HH already refused once.

### B3 · A scheduled event has no caller transaction

The platform's rule is `events.append(tx, event)` — a local write, in the
transaction of whatever caused the event, so a crash cannot keep the change and
lose the announcement. **A boundary has no such transaction.** Nothing in
Workforce changes at 07:00; the rota row was written last week. The event
announces the passage of time.

The answer is to make the announcement itself the fact:

```text
workforce.shift_boundary
  id · property_id
  department_code · catalogue_entry_id · business_date
  boundary   STARTED · ENDED
  at
  announced_at
  UNIQUE (property_id, department_code, catalogue_entry_id, business_date, boundary)
```

One transaction inserts the row **and** appends the event. A trigger that fires
twice — a retry, a restart, two schedulers — violates the unique index and the
whole transaction rolls back, so the second attempt announces nothing. **Exactly
once, by construction, from an at-least-once trigger.** It also answers a
question nothing else can: *did we announce HK's 07:00 boundary, and when?*

It gives the event a well-formed aggregate too. `IEventAppender.Append` wants
`(aggregateType, aggregateId, entityVersion)`, and AUTHZ-Q20's rule is *announce
against what you own*. The boundary row is what Workforce owns here, it is
unique by construction, and its version is 1 — so no two announcements can
collide on `(aggregate, version)`, which they would if the aggregate were the
catalogue entry whose version never moves.

**And there is one thing it does not answer: whose request this is.**
`IEventAppender.Append` takes a `RequestScope`, and a clock has no caller. The
obvious fill — `CallerKind.Service` — is **wrong, and specifically so**:
`AUTHZ-Q18` removed the application member from that enum on the grounds that an
installed application *"is performed as nobody — it carries no session and
propagates no user"*, so claiming `Service` is a package claiming to be a
platform service. The other axis does have the value: `TransportPrincipalKind`
carries `Application`, and `RequestScope.ApplicationId` is derived from it.

**No application constructs a `RequestScope` today** — checked; every one in this
repository and in GuestOps arrives off the wire, and a package's own background
worker would be the first to make one. What `Caller` says when nobody called is
a platform question, not Workforce's, and it is the eighth question below.

### B4 · The trigger — and a conflict to report rather than resolve

The owner's words (2026-09-03, quoted in Jobs' S5): *"When a shift is created,
Temporal holds each shift's start and end; at those times it triggers and
Workforce fans out."*

**Two things must be said before that is built.**

**First: Temporal does not exist in this repository.** The stack standard names
it and several chapters plan for it; the code does not contain a Temporal
server, client, container or make target — the only matches are the word
*temporal* inside generated protobuf comments. HH's own S5-D12 (*"the sweep, run
by Temporal Cron"*) rests on the same absent infrastructure. This is reported,
not worked around: the design below is correct under **any** at-least-once
trigger, so nothing here is blocked on it, but "Temporal holds it" is not
something either application can assume today.

**Second, and this is a genuine conflict between two same-day rulings.** For the
structurally identical problem — a timer per entity, versus one sweep — the
owner ruled the other way inside Jobs:

```text
S5-D12   the concern sweep     Temporal CRON, one schedule per property, every 60s, SKIP overlap
S5-D13   the shift fan-out     Temporal holds EACH SHIFT's start and end
```

The trade-off HH tabulated for their own clocks applies here almost word for
word:

```text
A SCHEDULE PER BOUNDARY                   ONE SWEEP PER PROPERTY
two schedules per cell per day —          one schedule, fixed
  a hundred a day for one property
every rota edit must cancel and           a rota edit is a row write; the next
  reschedule: assign, clear, swap,        sweep reads it
  copy-week, override, reschedule
an outage loses the firings inside it     the sweep announces what it missed on the
                                          next run — the boundary row says what is
                                          outstanding
```

A sweep is also **self-healing after an install**: it announces boundaries whose
row is absent, which is exactly the recovery case.

**Recommendation: one sweep per property, on the same schedule shape S5-D12
already chose**, so the platform has one scheduling idiom rather than two. The
sweep is a hosted service inside the Workforce package (the .NET template's
`Background/`), and it is Temporal Cron the day Temporal lands — the schedule
moves, the logic does not.

**Not decided here.** Two owner rulings from one day pull in opposite
directions for one class of problem, and the constitution's rule is to report a
conflict rather than pick.

### B5 · The gap HH's design has already opened

Jobs removed `next_shift_at` — *"no roster read, no fallback needed"* — on the
strength of these events. But **an event stream cannot establish an initial
state.** A freshly installed Jobs, or one restored from backup, has a
`department_presence` row and no event until the next boundary, which may be
sixteen hours away for a department that runs one day shift.

Two ways to close it, and Workforce can serve either:

```text
1  a synchronous read at startup     ShiftBoardSummary already answers "who is on now,
                                     by department" and was built last round
2  a baseline announcement           the sweep's first run for a property announces the
                                     current coverage per department, as-of rather than
                                     a transition
```

(1) is smaller and needs the Context Service or a direct read; (2) needs no
caller but puts an event on the bus that is not a boundary, which muddies the
subject. **Recommendation: (1)**, and it is HH's call because it is their row.

### B6 · What Workforce declares

```yaml
events:
  publishes:
    - user.posted
    - user.posting_ended
    - user.headship_started
    - user.headship_ended
    - shift.started      # new
    - shift.ended        # new
```

`property.*.shift.>` is **already routed** in the Kernel's OPERATIONAL stream,
so unlike `team` these two need no platform change to reach a consumer. ADR
0093's addendum makes `publishes:` load-bearing — an application may publish only
what it declares — so this is the whole of the permission.

`attendance.clocked_in` is HH's later refinement and is not proposed here.
`property.*.attendance.>` is routed already, for the reason `WF-Q10` gave — an
attendance record answers no shift at all when somebody turns up unrostered — so
that door is open when the design is.

---

## The questions — all eight answered, 2026-09-04

| | Question | Ruling |
|---|---|---|
| **1** | Is the team Workforce's, whole? | **Yes.** The zone-vs-team distinction is recorded against the precedent that looks decisive, so nobody re-derives it |
| **2** | One department per team? | **Yes, v1** |
| **3** | `posting.assign`, or a thirteenth permission? | **Reuse `posting.assign`** |
| **4** | Team events in v1? | **None.** Their eventual arrival is `PKG-Q39`'s mechanism, never a third pre-name |
| **5** | Does the event carry `on_now_after`? | **Yes, on both** — it *removes the class rather than mitigating it* |
| **6** | A sweep, or a schedule per boundary? | **A sweep** — and the two rulings never disagreed. Under the locked boundary test they are the same shape: the rota row already holds the boundary instant, exactly Jobs' `due_at` case, so a schedule per boundary is the per-job-timer trap wearing a rota. Temporal holds the recurring tick — *the future that is the thing* is **every minute, look** |
| **7** | How does Jobs establish presence at startup? | **State by read, changes by event.** HH's stubbed Workforce client is the named dependency |
| **8** | What scope does a package's worker carry? | **The application's own service identity** — `CallerKind::Service`, its principal, its installation's property. `AUTHZ-Q18`'s removal means packages are **services, not a third kind** |

**§B3's mechanism is ratified as the platform pattern for every scheduled
announcement**: *the announcement becomes the fact* — one row, one unique index,
inserted and appended together.

## What was built

| | |
|---|---|
| `Domain/Team.cs` · `Domain/TeamMember.cs` | the object, with the placement argument beside the type |
| `Application/Teams/` | form, rename, stand down and back up, add and remove members, list, and the membership-ending consequence |
| `PostingService.EndAsync` | **calls it in its own transaction** — the invariant, where it belongs |
| `Domain/ShiftBoundary.cs` | the announcement row and its unique key |
| `Application/Shifts/ShiftBoundaryAnnouncer.cs` | the *looking*: which boundaries have fallen, which are unannounced, one row and one event each |
| `Application/Shifts/ShiftAnnouncements.cs` | the payload, with every wire name stated |
| `manifest.yaml` | `shift.started` and `shift.ended` declared — `publishes:` is the whole of the permission |
| migration `TeamsAndShiftBoundaries` | three tables, proven head → base → head on a real PostgreSQL |
| 17 new tests | 182 in the suite, 0 warnings |

**Two things wait on II's round, by the ruling's own sequencing**: the SDK's
service-identity constructor (`AnnounceDueAsync` takes the scope, so it is
testable without one and consumes it unchanged), and the sweep host beside
`ConcernSweepHost` (this class is the looking; the tick is the platform's).

**And one thing waits on nobody**: Jobs' day one is unaffected. Presence runs on
service hours, person assignment works without a team, and the events flow the
moment a tick exists to call the announcer.

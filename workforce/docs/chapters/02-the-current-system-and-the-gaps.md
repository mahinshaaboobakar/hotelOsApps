X`# 02 · The current system and the gaps — what the owner runs, measured against chapter 01

**Status:** comparison study — **complete, for the owner's read**, 2026-08-31.
Stream GG, deliverable 1 of the Workforce round — brief
`docs/working/47-the-workforce-round.md` §3.1, **in the platform repository**.
**All eight subjects of the brief's §2 seed list are walked**, one at a time
through the architect, and §4 sorts the 54 rows they produced. Two of the
subjects' halves are named as unasked in §4.8 rather than left invisible.
**Nothing here is a ruling.** §1 and §2 are citations; §3's rows carry the
owner's answers and the architect's rulings, each attributed and dated; §4 is
the synthesis. Every scope proposal is labelled a proposal, and **the owner
rules v1**.

**What follows this page**, per the brief §3: the owner reads it, then the
feature plan (chapter 01 revised — §4.4 is its spine), then the revised gold
mockup and the flows page (§4.5 is their worklist). Code starts when the pages
are verified and not before `APPS-Q1`'s two platform prerequisites have a plan
of record.

## There is no reference backend for Workforce

> *"no reference for workforce"* — **owner, 2026-08-31**, given in passing
> during another stream's report and relayed to this round by the architect.

`APPS-Q1`'s process says the owner supplies a reference Java backend per
application. For this one there is none, and the round was gated on finding
that out before asking anything else. Stream GG's search, 2026-08-31, before
the answer arrived: the connector round's read-only reference root
`C:\Users\…\Documents\HotelOs-References\` holds `pms-integrations` and nothing
else, and a sweep of the owner's `Documents` and `PycharmProjects` trees found
no workforce, HR, roster or attendance system.

So this study takes **GuestOps's shape, not the Oracle connector's**. It
carries no `file:line` citations, because there is no code to cite. Stream FF
met the same answer for GuestOps (`GUEST-Q1`, owner 2026-08-31) and built its
round from the owner's scenarios; this one is built from the owner's
description of the system a hotel actually runs on.

**The connector round's rule still binds, in its other half.** *Learn, never
copy* is usually about structure in a repository. With no repository, what
remains is the part that matters more here: **a description of how a system
behaves is a domain source and never a design.** An answer of the form *"our
current system stores X on Y"* is recorded in §3 as what the hotel needs
expressed — never as a table we then build. Where the answer's shape would
import a decision this platform has already made differently, the row says
`DIVERGES` and names the ADR.

## Authority

| | |
|---|---|
| `01-the-workforce-application.md` | the thing being compared against — §2 is its inventory |
| `01-workforce-gold.html` | the drawn surface, **awaiting the owner's redline**; this round reopens it |
| `WF-Q1` — **ruled**, planner 2026-08-29 | MOD is a duty assignment, not an authorization role; no FGA tuples in v1 |
| `APPS-Q1` + its 2026-08-31 addendum | **Workforce** = Roster; the per-app round process; the two platform prerequisites |
| ADR 0063 §Q5 | `staff.department_id`, `staff.reports_to_staff_id`, `departments.head_staff_id` → Workforce; capability is Roster's, structure is Core's |
| ADR 0052 | `StaffPropertyAssignment` splits — Core keeps `StaffPropertyScope`, **Roster takes the role, the primary flag and the effective dating** |
| ADR 0116 §6 | department-scoped authorization derives from **Workforce postings only, permanently** — no interim mechanism exists to be replaced |
| ADR 0119 | the 48-code department canon; **the code is the identity**, immutable, and a property activates rather than creates |
| ADR 0114 §5 | `general_manager` and `department#manager` are documented **Workforce-era hooks** |

A path beginning `docs/` below is a **platform-repository** path unless said
otherwise.

---

## 0 · How to read a row in §3

Every subject in §3 resolves into rows carrying one of four verdicts, and each
row names the owner's answer that produced it.

```text
COVERED    chapter 01 already carries this. The row says where
GAP        the hotel needs it and chapter 01 does not carry it.
           Marked  proposed IN v1  or  proposed OUT of v1  — a proposal
DIVERGES   the answer's shape is one this platform deliberately will not
           take, with the ADR or ruling that decided it
OPEN       the answer raises something nobody has ruled. It becomes a
           WF-Qn in §5 and is not resolved here
```

**An owner fact and a standard-practice default are never the same row.** Where
the owner answers *"I don't know exactly"*, the standing instruction applies —
*where a modern standard exists, rule it, don't ask* — and the shape is designed
rather than re-asked. Those rows are marked **`DEFAULT`** and carry three
properties the owner's facts do not:

```text
OWNER     the owner said it. It is a fact and the design conforms to it
DEFAULT   standard practice, designed by the stream because the owner has
          no answer to give. Not an owner fact, never cited as one, and
          VETOABLE AT THE MOCKUP READ — the point at which it is seen
ARCHITECT the architect ruled it as the standard, and the owner was shown
          it and did not object. Stronger than a DEFAULT — it has already
          been seen — and still not an owner fact: cite it as an architect
          ruling with its date, never as "the owner decided"
```

A `DEFAULT` that is never seen is a decision made by silence, which is why each
one below names the screen it becomes visible on.

**A `GAP` is not a commitment and a proposal is not a scope decision.** The
brief is explicit: *scope proposals are proposals; the owner rules v1.* A row
marked `proposed IN v1` means Stream GG would build it and has said why — it
becomes v1 when the owner says so and not before.

**A delta against the gold mockup is recorded, never absorbed.** Where an
answer disagrees with what frame *n* draws, the subject's write-up says so in
its own paragraph and the frame is listed for deliverable 3's revision. A
mockup quietly redrawn to match a later answer destroys the record of what the
owner actually approved.

---

## 1 · The stake — what is broken for as long as this application does not exist

This section is why the round is first among the applications, and it is
measured facts rather than an argument.

### 1.1 · `department#posted` is defined, read by every department folder grant, and written by nothing

`infrastructure/openfga/model.fga:419` — `define posted: [user]`, on
`department`. And `model.fga:629`, on `folder`:

```text
define viewer: reader or contributor or posted from department or viewer from parent
```

So a department folder grant in My Hotel resolves through a relation that has
**no writer anywhere in the platform**. It fails closed, which is the correct
direction and is not the same as working: `MYHOTEL-Q10` held the relation *for
Roster* when the model was written, and `MYHOTEL-Q17` is that same fact meeting
a live property — the founding administrator created a folder, every upload
into it was refused, and the kernel log named the empty chain.

**Chapter 01 §4 makes Workforce the writer.** A posting appends the
user-aggregate lifecycle event, the Kernel's registration consumer materialises
`department:{id}#posted@user:{uid}`, and no service writes a tuple by hand —
ADR 0061. The day the first posting is saved, the dormant grants come alive.

### 1.2 · Master Data has already stopped answering "who works where"

Not pending — executed. `CORE-Q14` closed 2026-08-28 under ADR 0116 §6:
`CreateStaffRequest` reserves tags 8 and 9 **by name**, `StaffPatch` reserves 5,
`Staff` reserves 11 and 12, and `ListStaffRequest` reserves 3. The rule copy,
both controls, the Designation column, the import columns and the Departments
prerequisite went with them. A person no longer needs a posting to exist —
and nothing else can say they have one.

`CORE-Q15` is the same boundary seen from the other side: the Staff count on
Core Administration's Departments table was removed, and the owner ruled that
when Workforce ships the count returns **through the Context Service** — never
by reading Workforce's tables or calling its RPCs.

> **The round's thesis, in one sentence.** Workforce is not adding a
> capability; it is the only thing that can answer a question the platform has
> already stopped answering.

### 1.3 · Two authorization hooks are unwritten by design, waiting for this app

`AUTHZ-Q3(0114)` was ruled by the planner on 2026-08-28 (ADR 0114 §5):
Chapter 21's four-tier role hierarchy is amended to the current relationship
model, and `general_manager` and `department#manager` are documented as
**Workforce-era hooks** — *"we don't pretend the future workforce model already
exists."* Chapter 01 §4 makes a department-head posting the writer for
`department#manager`.

### 1.4 · What does not exist yet, stated plainly

`services/` holds `context-service`, `identity-service`, `kernel`,
`knowledge-service` and `masterdata-service`. There is **no Workforce service**
and no Workforce desktop module. Every occurrence of the word *Workforce* under
`apps/desktop/` today is explanatory copy in Core Administration telling the
user where a field went.

And code does not start here. Per the brief §3 it waits on the owner's
verification of the pages **and** on `APPS-Q1`'s two platform prerequisites
having a plan of record — the registry-driven shell and the application-caller
authorization round.

---

## 2 · The baseline — what chapter 01 already carries

This is the left column of every comparison row in §3. It is an inventory of
the chapter as it stands on 2026-08-31, before the owner's answers touch it.

### 2.1 · Five aggregates

| Aggregate | What chapter 01 gives it |
|---|---|
| **Posting** | `staff_id` · department **canon code** (ADR 0119) · job role · primary flag · effective from/to · reporting manager. Department head lives here (ADR 0063's table). These are exactly the fields ADR 0052 sent here from `StaffPropertyAssignment` |
| **Shift** | one person, one day, one span: `staff_id` · date · shift code (M/A/N/custom **per property**) · start–end · posted department, defaulting from the primary posting |
| **LeaveRequest** | `staff_id` · type · date range · note · `Draft → Requested → Approved/Declined → Cancelled`, approved by the reporting manager or department head. Types are a property-configured list seeded Casual · Sick · Earned · Comp-off · **Week-off**. Balances are a per-type annual allowance and a running count — **no accrual engine in v1** |
| **DutyAssignment** | the MOD register: date or range · `staff_id` · duty type (**v1 ships exactly one: MOD**) · notes. One MOD per property per day; assigning over an existing one replaces it behind a named confirm. The posting is untouched |
| **Capability** | skills · languages · shift pattern — ADR 0063 §Q5's remainder. **Recorded, not built in slice 1** |

### 2.2 · Five screens (gold mockup, 7 frames)

```text
1  My Schedule    default landing — the signed-in person's own month: their
                  shifts, their leave, a MOD badge on days they hold the duty
2  Team Rota      the calendar: one department, one week, people down and days
                  across; leave struck through, the MOD ribbon above each day.
                  Manager tools: assign by clicking a cell, copy last week,
                  swap two people
3  Leave · mine   a request in three fields: type, dates, note
4  Leave · queue  the manager's approvals, with the team's calendar for those
                  dates beside the decision
5  Duty Roster    the MOD month strip; assign by clicking a day and picking
                  any active staff member, from any department
6  People         postings — each staff member's identity read-only from
                  Master Data, with posting(s), job role, reporting manager
7  First run      the empty state: Workforce starts with people
```

### 2.3 · Four slices

| | Ships | Unblocks |
|---|---|---|
| **1 · Postings + People** | postings, job roles, reporting manager, department head, the `department#posted` writer | §1.1's hole, platform-wide |
| **2 · Rota + Duty** | Team Rota, My Schedule, shifts, the MOD register | the calendar face; the owner's MOD scenario |
| **3 · Leave** | requests, approvals, balances, calendar strike-through | the leave concept |
| **4 · Capability** | skills, languages, shift patterns | assignment intelligence for Jobs and the AI apps |

Slice 1 is strictly first because the granting pipeline is its substrate.

### 2.4 · What chapter 01 §8 explicitly refuses

> No payroll · no attendance / punch clock · no biometric integration · no
> accrual engine · no shift-bidding · no compliance rule engine.

**This list is the study's most productive surface**, and it is why the walk
exists. Each refusal is a place where the hotel may well have a daily
dependency — attendance above all. A refusal that survives the owner's answers
is a stronger boundary than one nobody tested; one that does not survive is
exactly the finding this study is for. §3 tests every line of it.

### 2.5 · The six questions chapter 01 already claimed

`WF-Q1` is **ruled** (MOD is a duty, not an authorization role; no tuples in
v1). `WF-Q2`–`WF-Q6` stand as architect calls at design ratification:
shell-side MOD surfacing · multiple postings per person · shift definitions per
property or per department · leave-balance hard block vs warn-and-allow · Jobs
reading postings through the Context Service. New questions this study raises
are claimed from `WF-Q7` upward, by the architect.

---

## 3 · The walk — subject by subject

The brief's §2 seed list, in order. **Asked one at a time**, through the
architect; a subject is written up when its answer lands.

| # | Subject | State |
|---|---|---|
| 3.1 | **Rostering** — shift templates, rotation patterns, week planning, copy-last-week | **answered 2026-08-31** ↓ |
| 3.2 | Attendance — check-in/out, late/absent, who is *actually* here versus posted | **answered in part 2026-08-31** ↓ — the sources are answered, the semantics are not |
| 3.3 | Leave — types, balances, requests and approval, who covers | **answered 2026-08-31** ↓ — types and policy are owner facts; balances, approval and covers are standard-practice defaults |
| 3.4 | Swaps and covers — staff-initiated exchange, the approval chain | **half answered** — *covers* is settled by §3.3b's L9/L10; **staff-initiated swaps** are not asked |
| 3.5 | Overtime and hours — caps, alerts, **the payroll boundary** | **ruled 2026-08-31** ↓ — architect, owner shown |
| 3.6 | Skills and certifications — expiry, the compliance view | **ruled 2026-08-31** ↓ — architect, owner shown |
| 3.7 | The MOD duty in daily operation | **answered 2026-08-31** ↓ — one structural owner fact; the rest are defaults |
| 3.8 | Departments and zones in practice — how postings map to Room Care's *"who cleans zone 3 today"* | **answered 2026-08-31** ↓ — *from Workforce* |
| 3.9 | Notifications — how staff actually learn their roster | **ruled 2026-08-31** ↓ — architect; v1 is the printed week |

---

### 3.1 · Rostering — the shift is the property's own object, and the calendar is painted with it

**The owner's answer, 2026-08-31**, relayed through the architect:

> *"Shifts are property-created entities, free-form — name, times, and a colour
> as a first-class attribute; the roster calendar renders each staff member's
> day as their shift's colour badge, so a week reads at a glance by colour. No
> template/rotation machinery in v1 unless a later answer adds it — the owner's
> model is create the shifts you use, paint the calendar with them."*

Two decisions in one answer, and they pull in opposite directions on scope,
which is what makes it a good first subject: the **shift vocabulary opens** (a
property invents its own, with a colour), and the **planning machinery stays
shut** (no templates, no rotation cycles). Chapter 01 got the second right and
the first wrong.

#### The rows

| # | Feature | Verdict | Why |
|---|---|---|---|
| R1 | The property defines its own shifts, any number, free-form | **GAP** — proposed **IN v1** | Chapter 01 §3 models a *shift code* — `M/A/N/custom per property` — which is a closed set of three plus an escape hatch. The owner's model is an open catalogue the property creates. This is the subject's central finding and slice 2 cannot be drawn without it |
| R2 | A shift carries a **name** | **GAP** — proposed **IN v1** | Chapter 01 has a code, not a name. A code is what reports group on; a name is what a duty manager reads. A free-form catalogue needs the name, and R6 turns on whether it also needs the code |
| R3 | A shift carries **times** (start–end) | **COVERED**, but on the wrong aggregate | Chapter 01 puts `start–end` on the *assignment* (one person, one day). If shifts are entities, the times belong to the **definition** — see the aggregate split below |
| R4 | A shift carries a **colour, as a first-class attribute** | **GAP** — proposed **IN v1** | Nothing in chapter 01 mentions colour. The mockup has colour but derives it from the code (see the delta below). The owner's phrasing is deliberate — *first-class* — and the reason is operational, not decorative: a week must read at a glance |
| R5 | The calendar renders a day as the shift's colour badge | **GAP** — proposed **IN v1** | Follows from R4. It also decides what the chip *says*, which chapter 01 never had to answer while the vocabulary was three letters |
| R6 | No template machinery — no shift templates to apply to a week | **COVERED** | Chapter 01 has none, and the mockup draws none. The refusal is now the owner's, not an omission |
| R7 | No rotation patterns — no cycle engine (`4-on-2-off`, weekly rotating M→A→N) | **COVERED** | Same. Recorded explicitly because a rotation engine is the single largest thing a workforce product can grow, and the answer says not in v1. `Capability`'s "shift pattern" (chapter 01 §3, slice 4) is the place it would land if a later answer adds it — the boundary is already drawn |
| R8 | Week planning by direct manipulation — click a cell, assign | **COVERED** | Chapter 01 §5 and mockup frame 2 |
| R9 | Copy last week | **COVERED** | Chapter 01 §5, mockup frame 2 — *fills empty cells only* |
| R10 | Swap two people's week | **COVERED** | Mockup frame 2. Note that this is the *manager's* swap tool and is a different thing from subject 3.4's staff-initiated swap request; the study does not conflate them |

#### The aggregate split R1 forces — a proposal for deliverable 2

Chapter 01's `Shift` is one aggregate doing two jobs: *"one person, one day, one
span · shift code · start–end"*. Once a shift is an entity the property creates,
that is **two** aggregates:

```text
ShiftDefinition   the property's catalogue entry
                  name · start–end · colour · active
ShiftAssignment   one person, one day
                  staff_id · date · → ShiftDefinition · posted department
```

The reason is a rule this platform already has, not a preference. If the
assignment copies the definition's `start–end`, that copy is **a derived
projection a client would be writing** — CLAUDE.md §"Clients never write a
derived projection": *a denormalised column that is allowed to disagree with
its source is a defect with a delivery date.*

**But there is a genuine argument on the other side, and it is not the same
argument.** Two cases want a span *on the assignment*:

* **the one-off** — mockup frame 2's `Custom hours…`: this person, this day,
  outside every catalogue entry;
* **the historical truth** — a property edits *Morning* from 07:00 to 06:30 in
  November. Last March's rota must not silently become a rota of 06:30 starts.

Both are real; neither is a licence to copy the times on every row. The shapes
that answer them (an optional override span; effective-dated definitions; or
snapshotting only what has been worked) differ in cost and in what they promise,
and choosing between them is **design, which is deliberately not this page's
job**. It is raised as a question below and settled in deliverable 2.

#### The delta against the gold mockup — frame 2, marked not absorbed

The architect asked whether frame 2 assumed templates. **It does not** — and it
*does* assume a closed vocabulary. The two halves of the owner's answer land
differently on the same frame:

| | What frame 2 draws | Against the answer |
|---|---|---|
| **Planning machinery** | `⧉ Copy last week` and `⇄ Swap` — both direct manipulation over a drawn week. No pattern picker, no rotation control, no "apply template" anywhere in the frame | **Agrees.** R6/R7 confirmed rather than changed |
| **The vocabulary** | The assign popover offers exactly five options: `Morning 07:00–15:00` · `Afternoon 15:00–23:00` · `Night 23:00–07:00` · `Custom hours…` · `Week-off`. The caption states it in words — *"Shift codes are the property's three (M · A · N, per WF-Q4) plus custom hours"* | **Diverges.** The popover must become **the property's shift list**, of whatever length, and the caption's sentence is now wrong |
| **The chip** | A single glyph — `M`, `A`, `N` — coloured by one of three hardcoded CSS classes (`sh m`, `sh a`, `sh n`), plus `sh off` and `sh lv`. Colour is *derived from the code* | **Diverges.** Colour is the shift's own attribute, chosen by the property. The chip must render from data — and a free-form name has no guaranteed one-letter form, so what the chip *says* is now an open question |

**Frame 2 is listed for deliverable 3's revision on the vocabulary and the chip;
its planning machinery stands as drawn.** Nothing in the mockup has been
changed by this study.

#### An internal contradiction this subject exposes

Frame 2's popover offers **`Week-off`** as a shift option. Chapter 01 §3 lists
**Week-off as a leave type**, seeded alongside Casual · Sick · Earned ·
Comp-off. And frame 2's own caption says *"the rota reads leave, it never edits
it"* — while the popover it sits under edits one.

That contradiction predates this subject; the owner's answer sharpens it, because
*"create the shifts you use"* makes a non-working day a plausible **shift** the
property creates rather than a leave type. The three candidate readings differ
in what a balance means and in who may set the day, so it is a question, not a
cleanup.

#### Questions this subject raises

Claimed by the architect; recorded in §5 and asked one at a time when the walk
allows.

1. **What does a colour chip say?** A free-form shift name has no guaranteed
   short form, and a 7-column week grid has room for roughly two characters.
   Does a shift carry a **short code** alongside its name (the property types
   both), or does the chip show colour alone with the name on hover and in a
   legend? The first keeps the grid readable and costs a field; the second is
   fewer fields and unreadable in monochrome — which also decides whether the
   rota can be printed or photocopied, subject 3.9's likely territory.
2. **Is `Week-off` a shift or a leave type?** Chapter 01 says leave type; the
   mockup's rota popover says shift; the caption says the rota may not edit
   leave. One of the three must give.
3. **How does an edited shift definition treat rotas already worked?** The
   historical-truth case above. An answer is needed before the aggregate split
   is written into chapter 01.

**`WF-Q4` is answered by this subject** — *"shift definitions per property or
per department?"* — the owner's *"property-created entities"* says **per
property**, which is what chapter 01 §7 recommended. Recorded here; the
architect ratifies it as a `WF-Q4` closure rather than this study asserting one.

---

### 3.2 · Attendance — three sources, one fact, and only one of them is v1 code

**The owner's answer, 2026-08-31**, relayed through the architect:

> *"Yes we track attendance — each hotel has a different mechanism. Some have
> fingerprint/face machines, so we need to integrate with their system (needs a
> mapping of user ↔ machine id). Some use our mobile client — login marks
> check-in and check-out. Some have HR mark it manually in Workforce."*

**Chapter 01 §8's first refusal does not survive.** *"No attendance / punch
clock, no biometric integration"* was written without asking, and the hotel
tracks attendance in every property — the variable is only *how*. That is the
finding §2.4 predicted this walk would produce, and it is worth being precise
about what it overturns: the refusal of **attendance as a fact** is wrong; the
refusal of **a punch-clock implementation inside this application** turns out to
be right, for a reason chapter 01 never gave.

#### The rows

| # | Feature | Verdict | Why |
|---|---|---|---|
| A1 | Attendance is tracked, in every property | **GAP** — proposed **IN v1** | Chapter 01 §8 refuses it outright. The answer overturns the refusal |
| A2 | The **source is per-property configuration**, of three kinds — device · mobile · manual | **GAP** — proposed **IN v1** (the configuration and the model); the device and mobile *sources* are not v1 code | *"Each hotel has a different mechanism"* is a configuration statement, not a feature list. One property runs on a face reader, the next on a clipboard |
| A3 | **Manual marking** — HR or the duty manager records it in Workforce | **GAP** — proposed **IN v1** | The always-available floor: it needs no device, no connector, no mobile client and no parked platform work, so **any hotel can run on it from day one**. It is also the fallback every other source needs on the day the machine is down |
| A4 | **Fingerprint / face device** integration | **GAP** — proposed **OUT of v1 as code, IN as model** · and **DIVERGES** on shape | Not a Workforce feature. *"No hardcoded integrations"* (CLAUDE.md) sends it through the Integration Hub as a `kind: connector` package — below |
| A5 | **Mobile client** login marks check-in and check-out | **GAP** — proposed **OUT of v1** | Lands on parked ground — ADR 0115 §1D — below |
| A6 | The **staff ↔ machine-user-id mapping** | **COVERED** by mechanism, with two measured implementation facts | ADR 0016 as amended by `CONN-Q8`. The pattern exists; two things about it are not what the ruling assumes — below |
| A7 | One attendance fact, **source-agnostic, carrying its provenance** | **GAP** — proposed **IN v1**, and the load-bearing one | A punch, a mobile login and an HR click are the same fact arriving three ways. If the model is not source-agnostic on day one, the third source is a schema change |

#### A7 is the row the other six depend on

Only one of the three sources is buildable now. That is precisely why the
**model** must carry all three from the first migration:

```text
AttendanceRecord    staff_id · date · in / out · the shift it answers to
                    source      device | mobile | manual
                    provenance  which device, which connector, which
                                person clicked it, and when it arrived
```

A property that starts on manual marking and later installs a face reader must
not need a migration, and an auditor asking *"how do we know he was here"* must
get a different answer for a fingerprint than for a supervisor's click. **The
provenance is not metadata — it is the difference between evidence and an
assertion**, and a system that flattens the three into one boolean has thrown
that away permanently.

#### The device is a connector, not a feature — and the fit is exact

CLAUDE.md's non-negotiable: *"No hardcoded integrations. All integrations must
use the Integration Hub."* A biometric terminal is an external system, so it
enters exactly as a PMS does — and the connector round has already ruled every
piece it needs:

| | |
|---|---|
| **ADR 0128 §2** (`CONN-Q1`) | a signed `.hopkg`, **`kind: connector`**, standard Software Center lifecycle, installed into the Hub. No credentials or certificates in the package — permission *requests*, approved by the administrator. **A brand is not the unit**: distinct transport/credential/capability combinations are distinct integrations, which is exactly right for a device estate where ZKTeco and Matrix share nothing but a purpose |
| **ADR 0128 §3** (`CONN-Q4`) | ingress is the Hub's **Property Integration Ingress**, applying **connector-declared** webhook authentication — *"shared secret, signature, allow-list — declared, never assumed"*. That is the architect's *per-device auth declared by the connector*, already ruled |
| **ADR 0128 §5** (`CONN-Q7`) | the Hub owns the durable inbox, dedupe, retry and checkpoints; **a connector never implements a queue**. Load-bearing here: a terminal that was offline overnight replays a batch in the morning, and the same punch must not become two |
| **ADR 0128 §4** (`CONN-Q5`) | v1 is **inbound-only** — and a punch is inbound. Attendance needs nothing from the deferred write-back half |

**And the ordering is already ruled, one domain over.** ADR 0128's consequences
close with the reservations gate: *"the Hub publishes `reservation.*` only when
the Reservations/GuestOps domain exists to own them."* Attendance is the same
gate with a different domain in it — **the Hub can publish an attendance fact
only when Workforce exists to own it.** So a device connector cannot precede
this application; it follows it. Recorded because it makes the sequencing a
consequence of a standing ruling rather than a scheduling preference.

#### The mapping is the external-identifier pattern, and two facts about it are measured

The architect is right that this is *the same mechanism, no new invention*.
ADR 0016 as amended by `CONN-Q8` (ADR 0128 §8) gives the identity
**`(entity_type, identifier_kind, external_id)`**, property-scoped, bijective
within the three-part key — here `staff` · `machine_user_id` · the device's own
user id, with `identifier_kind` **connector-declared**. Property scoping is not
incidental: two hotels' terminals both number their first enrolled employee `1`,
which is ADR 0016's Opera room "101" worked example wearing different hardware.

Two things measured in the repository on 2026-08-31, so the plan does not assume
a mechanism that is only partly there:

1. **`identifier_kind` is ruled and not yet built.**
   `services/masterdata-service/src/Domain/Assets.cs:255-266` — `ExternalMapping`
   carries `EntityType`, `EntityId`, `Integration`, `ExternalId` and **no kind
   column**. `CONN-Q8` was ruled on 2026-08-31 and its migration is the
   connector round's, not Workforce's. Workforce **consumes** the mapping; it
   does not own it, and must not race ahead and add the column.
2. **`staff` is expressible but undocumented.** `external_mappings.entity_type`
   has **no check constraint** (`media.owner_type` does — and that one already
   lists `staff`), so nothing rejects it. But the property's own documentation
   at `Assets.cs:257` enumerates *"`room`, `room_type`, `department`,
   `vendor`"* — staff is absent, which is how the next reader concludes it does
   not belong.

**Where the mapping lives is less open than it looks.** ADR 0063's rejected
table already refused moving `ExternalMapping` to the Integration Hub — *"the
mapping is an identity correspondence; the Hub owns the communication. It does
not move because integrations read it"* — and ADR 0016's header records that
0063 reinforced it. So the **table's home is ruled**: Master Data. What is
genuinely open is narrower and is a question, not a placement: **who writes the
row, and on which screen.**

#### The mobile source lands on parked ground

ADR 0115 §1D: *"The mobile client is an external client of the Edge/API
boundary, never a special Kernel application"* — the contract is frozen, the
framework choice deliberately left open. And the ADR's status line:
**implementation PARKED by owner direction, 2026-08-28**, with the unparking
trigger a corporate-live-data redesign question, and the gateway and tunnel work
standing on `FED-Q1` (§1E and its consequences).

So mobile attendance is not a small addition waiting for a spare afternoon; it
is behind a parked pillar. It is designed as a **future source of the same
fact** — which A7 already makes free — **drawn with its caveat and never as v1
scope**, on the `GUEST-Q6` precedent where a deferred capability was drawn
dashed rather than promised or hidden.

#### The delta against the gold mockup — an absence, not a divergence

Attendance appears in **none of the seven frames**. There is no
posted-versus-present view, no mark-attendance control, no late or absent
indication anywhere in the drawn surface — which is consistent, because
chapter 01 §8 refused the subject and the mockup was drawn from chapter 01.

That is now a hole rather than a boundary. **Deliverable 3 either gains a
surface or the owner rules attendance out**, and which of those it is depends
on the residue below. Nothing has been drawn on speculation.

#### What subject 2 has not answered — carried, not assumed

The answer is thorough on **where the fact comes from** and silent on **what it
means and who looks at it**. Three of the four things asked are still open, and
they are recorded here rather than guessed:

* what happens when someone is **late** or **absent** — a mark against the
  rostered shift, a separate log, or nothing recorded at all;
* whether **"who is actually here right now"** needs a screen — the duty
  manager's 7 a.m. posted-versus-present view — or whether the rota is enough;
* whether an attendance fact that **contradicts the rota** (present on an
  unrostered day; absent on a rostered one) is a thing the system must
  reconcile, or simply two records that coexist.

These are asked next, as the remainder of subject 2, before the walk moves on.

#### Questions this subject raises

4. **Does an attendance terminal speak HTTP at all?** ADR 0128 §3 rules ingress
   as **HTTPS** terminating at the Property Integration Ingress — written for a
   PMS. Attendance devices vary by vendor between an HTTP push-to-URL mode and
   a proprietary TCP/SDK protocol on the LAN. If a common device cannot speak
   HTTP, the connector contract needs a transport §3 does not name. **This is a
   platform question, not Workforce's** — raised here because this application
   is the first consumer that would meet it.
5. **Who writes the staff ↔ device mapping, and on which surface?** The table's
   home is ruled (Master Data, ADR 0063). The writer is not: Core
   Administration's Staff page, Workforce's People page, or the connector's own
   configuration UI (ADR 0128 §7 gave connectors one). It touches a Master Data
   row on behalf of a device, which is why it is not obviously any of the three.

---

### 3.3 · Leave — four types the owner named, a per-property policy, and three things designed rather than asked

**The owner's answer, 2026-08-31**, relayed through the architect:

> *"Casual, sick, earned, comp-off — all. Based on property they have leave
> policy — monthly 2 and yearly holidays."*
>
> On balances, approval and covers: *"I don't know exactly."*

This subject divides in two, and the division is the point. §3.3a is what the
owner knows; §3.3b is what nobody was going to learn by asking again.

#### 3.3a · The owner's facts

| # | Feature | Verdict | Why |
|---|---|---|---|
| L1 | Four leave types: **Casual · Sick · Earned · Comp-off** | **COVERED** — `OWNER` | Chapter 01 §3 seeds exactly these. Confirmed rather than changed |
| L2 | **Week-off was not among them** | **OPEN** — `OWNER`, by omission | Chapter 01 seeds *five*: the owner's four **plus Week-off**. Asked to name the types, the owner named four. Evidence, not a ruling — see below |
| L3 | The leave policy is **per-property configuration** | **GAP** — `OWNER` — proposed **IN v1** | Chapter 01 has a property-configured *type list* and no *policy*. A policy is the rule that generates the balance, and it is a different object from the list of names |
| L4 | Accrual — **"monthly 2"**, a rate rather than an annual grant | **GAP** — `OWNER` — proposed **IN v1** | Chapter 01 §3 says *"a simple per-type annual allowance and a running count — no accrual engine in v1"*. An allowance and a monthly rate are not the same model — below |
| L5 | The property's **yearly holidays** | **GAP** — `OWNER` — proposed **IN v1**, placement open | Nothing in the platform has a holiday calendar. Measured 2026-08-31: no `holiday` concept exists in any service, proto or desktop module. Whose it is, is a real placement question — below |

#### L2 · The owner named four types, and Week-off was not one of them

§3.1's open question 2 asked whether `Week-off` is a shift or a leave type,
because chapter 01 seeds it as a leave type while the rota's assign popover
offers it as a shift. Asked to name the leave types, the owner named **four**,
and Week-off was not among them.

**That is evidence and it is not a ruling** — an omission in a spoken list is
not the same as a refusal, and the question stays open until it is put
directly. But it now has a direction, and it agrees with §3.1's answer:
*"create the shifts you use"* makes a weekly off-day a shift the property
creates — a non-working one — far more naturally than it makes it a leave type
with a balance. The mockup already draws `Off` chips in the rota grid, which is
what a non-working shift looks like.

Recorded so the question is asked once, with both halves of the evidence, rather
than resolved quietly here.

#### L4 · The second refusal on §2.4's list, and it survives in a narrower form

Chapter 01 §8 refuses *"no accrual engine"*, and §3 implements that as *"a
simple per-type annual allowance and a running count"*. The owner's *"monthly
2"* is an **accrual rate**, so the annual-allowance model is wrong — a balance
that appears in full on 1 January is not the balance a property running 2-per-
month has in March.

But the refusal does not fall the way §3.2's did. **A rate is not an engine.**
What the owner described is one number per type per property, applied monthly.
What chapter 01 was right to refuse is the rest of the accrual machinery that
usually arrives with it, and none of it appeared in the answer:

```text
required by the answer      a per-type accrual rate, applied per period
still refused, unasked-for  carry-forward and its caps · encashment ·
                            pro-rata accrual on joining or leaving ·
                            expiry and lapse · tenure-based slabs ·
                            statutory leave-register reporting
```

So: **the model gains a rate; the engine stays refused.** Proposed that way, and
stated in these terms so that the first request for carry-forward meets a
recorded boundary rather than a gap.

#### L5 · The holiday calendar is a new object, and where it belongs is a genuine question

No holiday concept exists anywhere in the platform today. Two readings of where
it goes, and the platform has a written test for exactly this.

`Property` already carries `CheckInTime`, `CheckOutTime` and
`FiscalYearStartMonth`, and `services/masterdata-service/src/Domain/Tenancy.cs:80-95`
records *why* in the source, in the same words the study needs:

> *"Housekeeping plans its whole day around these two values and does not own
> them — ADR 0052. **Being used by an application does not make an attribute
> owned by it; what decides is who establishes the value**, and Core
> Administration configures the operating day."*

Read that way, a property's declared holidays are the same class as its
operating day and its fiscal year: the **property** declares them, Workforce
merely plans around them — Core's. Read the other way, a holiday list exists
only to decide who works and who is paid for not working, which is capability
and assignment, and ADR 0063 sends that to Workforce.

**The stream's recommendation is Core**, on the `FiscalYearStartMonth`
precedent — a property observes Diwali whether or not Workforce is installed,
and ADR 0051's uninstall test is passed by that sentence. It is a
**recommendation and not a decision**: it puts a new column on a Master Data
entity, which is precisely the move ADR 0051 says is *"the single most likely
way this boundary erodes"*, so it is raised as a question rather than taken.

The **leave policy itself is not in question** — an accrual rate is established
by Workforce and has no meaning without it. Only the calendar is arguable.

#### 3.3b · The standard shape — designed, not asked

The owner has no answer on balances, approval and covers. Per the standing
instruction, these are ruled to standard practice rather than re-asked. **Every
row here is a `DEFAULT`**: not an owner fact, never to be cited as one, and
**vetoable at the mockup read** — the column names the frame each becomes
visible on, because a default nobody sees is a decision made by silence.

| # | The shape | Seen on | Why this is the standard |
|---|---|---|---|
| L6 | **Balances accrue per the property's policy and count down on approval** | frame 3 (the request sheet's balance) · frame 4 (the approver's queue) | The balance is a ledger, not a counter: accrual credits it, an approval debits it. Debiting on *request* would let an unapproved request hide capacity from everyone else |
| L7 | **A cancelled approved leave credits the balance back** | frame 3 | The symmetry L6 implies. Chapter 01's `LeaveRequest` already has a `Cancelled` state, and a debit with no matching credit turns a cancellation into a silent forfeit |
| L8 | **The approver is the department head, resolved from Workforce's own postings** | frame 4 | The thesis, applied: this app is the source ADR 0116 §6 reads, so it is the one component that can answer *"whose head"* without asking anything else. The same posting writes `department#manager` (chapter 01 §4), so the approver and the authorization hook are one fact rather than two that can disagree |
| L9 | **A cover is a manual reassignment on the rota against the vacated slot** | frame 2 | Not a workflow engine, not a broadcast, not a bidding round. Somebody decides and assigns, which is what frame 2's cell click already does |
| L10 | **The vacated slot is surfaced as a gap on the rota** | frame 2 | The leave is already drawn struck; what is missing is that it *reads* as needing action. A gap the manager can see beats a notification nobody opens |
| L11 | **Balances are HR-adjustable** | a People/balances surface — **undrawn** | The manual floor, exactly as §3.2's manual attendance is: every accrual rule meets a case it did not anticipate, and a system with no manual correction gets corrected in a spreadsheet instead. The adjustment is a recorded, attributed entry, never a silent overwrite of a number |

#### Three things the standard shape leaves genuinely open

Stating a default is not the same as having answered everything it touches. Three
edges are **not** settled by the shape above, and none is invented here:

* **L6 against `WF-Q5`.** Chapter 01 §7 recommends *warn-and-allow* when a
  balance is exhausted — *"hotels override reality daily"*. Warn-and-allow plus
  count-down-on-approval means **a balance can go negative**, deliberately. That
  is coherent, and it has to be *drawn* coherently: a negative balance must read
  as an approved overdraw rather than as a bug. Frame 3's and frame 4's balance
  chip must survive a minus sign.
* **Chapter 01 says "reporting manager or department head".** §3 names both as
  approvers, with no precedence, and the `Posting` carries a reporting manager
  as well as the head. L8 names the head. Deliverable 2 must turn the *or* into
  a rule — head always, or reporting manager when set and the head otherwise —
  because two possible approvers with no order is two queues.
* **Who approves the department head's own leave.** The head resolved from
  postings cannot approve themselves, and nothing in chapter 01 says who does.
  A real hotel answers *the GM*, which is `general_manager` — one of ADR 0114
  §5's two unwritten Workforce-era hooks. Named here, not decided.

#### What "a gap on the rota" means is one design choice, not two

L10 says the vacated slot reads as needing action. That is **a per-person mark
on a drawn cell** — the shift that had a person in it now has none.

It is deliberately **not** a computed staffing shortfall, which would need
something nobody has mentioned in three subjects: a **required headcount per
shift per department**. That is a demand model, and it is the doorway to
coverage rules, minimum-staffing alerts and eventually the shift-bidding
chapter 01 §8 refuses. The cheap thing and the expensive thing look identical
in a sentence and differ by a whole subsystem, so the study names which one it
proposed.

#### The delta against the gold mockup — frames 3 and 4

| | What the frames draw | Against this subject |
|---|---|---|
| Frame 3 · the request | three fields — type, dates, note — with the balance on the sheet | **Agrees.** The balance shown where the decision is made is L6's own argument |
| Frame 4 · the approvals queue | Approve / Decline with a note, the team's calendar for those dates beside it | **Agrees**, and L8 adds *who* is looking at this queue — resolved from postings rather than configured |
| Both | the balance as a plain positive number | **Incomplete.** `WF-Q5` plus L6 permits a negative, and neither frame has been drawn against one |
| Nowhere | the accrual rate, the holiday calendar, HR balance adjustment | **Absent.** L3, L4, L5 and L11 have no surface in any of the seven frames — a property-policy screen does not exist yet |

**Neither frame is wrong; both are incomplete.** Listed for deliverable 3, with
the property-policy surface as new work rather than a revision.

#### Questions this subject raises

6. **Is `Week-off` a shift or a leave type?** Already open from §3.1 as question
   2 — L2 adds the owner's own four-item list to the evidence, and the answer
   now looks like *shift*. Asked once, with both halves.
7. **Whose is the property holiday calendar — Core Administration's or
   Workforce's?** The `FiscalYearStartMonth` precedent and ADR 0051's uninstall
   test point at Core; the recommendation is Core; the decision is not the
   stream's, because it adds a column to a Master Data entity.
8. **Does working a holiday or a week-off generate comp-off automatically?**
   Comp-off is one of the owner's four types, and a comp-off balance has to come
   from somewhere — either an HR adjustment (L11 already covers it) or a rule
   that credits the balance when someone is rostered on a non-working day. The
   second is an accrual rule of a different kind from L4's, and it is the one
   thing that would pull the rota and the balance ledger together.

---


### 3.4 · Swaps and covers — two verbs that were never one operation

The brief's item 4 was answered in two halves, months apart in nothing but
sequence: **covers** were settled by §3.3b's `DEFAULT` rows L9/L10, and
**swaps** were the last thing the walk had not asked. `WF-Q10` closed the other
§4.8 open on the same day; this one is `WF-Q9`.

**Ruled — owner, 2026-08-31:**

> **Yes — a staff member may propose exchanging a shift with a colleague, and
> it takes effect only on the manager's approval.**

#### The rows

| # | Feature | Verdict | Why |
|---|---|---|---|
| S1 | A **staff member may propose** a swap with a named colleague | **GAP** — `OWNER` — proposed **IN v1** | Nothing in chapter 01 or the mockup lets a staff member initiate anything. My Schedule is a *view* |
| S2 | It takes effect **only on the manager's approval** — a proposal with its own lifecycle, `proposed → approved / declined` | **GAP** — `OWNER` — proposed **IN v1** | This is the aggregate §4.8 anticipated, and it is real |
| S3 | A **manager moving shifts directly** needs no proposal | **COVERED** | Frame 2's `⇄ Swap` already does this, and it is a **different operation** — see below |
| S4 | Where a proposal is **entered** in v1, before the staff app exists | **OPEN** — deliverable 2 must answer it honestly | The constraint the ruling names explicitly. Below |
| L9 · L10 | A **cover** is a manual reassignment against the vacated slot, surfaced as a gap | **COVERED** — `DEFAULT`, from §3.3b | Recorded here so the brief's item 4 reads as one subject |

#### S3 · A swap and a manager's rearrangement are not the same operation, and the study is explicit about it

The word *swap* names two things that look identical on the rota and differ
completely in what they are:

```text
manager's swap      frame 2's ⇄ Swap — pick two people, exchange their week.
                    An action. It happens, and it is done
staff swap          a REQUEST between two people that a third approves.
                    An object with a state, an author, and an outcome
```

Folding them would be the same error §3.3 avoided when it kept the manager's
cover distinct from a workflow engine. **The manager's tool is not the staff
proposal with the approval step removed** — it never had one, because the
manager *is* the authority. Deliverable 2 keeps both, and the rota shows the
same result either way.

#### S2 · What the proposal needs that a leave request does not

`LeaveRequest` is the nearest existing shape and it is not close enough to
reuse, which is worth stating before someone tries:

| | `LeaveRequest` | swap proposal |
|---|---|---|
| Who it concerns | **one** person | **two**, and both shifts must exist |
| What approval changes | one person's availability | **two people's rota cells, atomically** |
| A second party's consent | none | **the colleague's** — does Anita agree before Priya sees it? |

The third row is the one the ruling does not settle and deliverable 2 must
choose: whether the colleague accepts first (`proposed → accepted → approved`)
or whether the manager's approval is the only gate and the colleague simply
learns of it. Both are defensible; the first is what most rota products do and
costs one more state.

**The approver resolves exactly as L8's does** — the department head from
Workforce's own postings — so this adds a lifecycle, not a second authority.

#### S4 · Where a proposal is entered, and the fact that decides it

The ruling names the constraint rather than waving at it: the staff mobile app
is **next version** (ADR 0115 parked, §3.2's A5, §3.9's N4), so v1 must say
where a staff member actually enters a proposal.

**The fact that shapes the honest answer**, measured rather than assumed —
`services/kernel/crates/kernel/src/events/streams.rs:88-90`:

> *"`staff` is master data about a person, and distinct from `user`: **most
> staff have no login.**"*

So *"the staff member enters it in My Schedule"* is a true answer for a
minority and silence for everybody else. Chapter 01's My Schedule frame
presupposes a signed-in person, and ADR 0116 §5 gates application access
per user — both correct, and neither reaches a room attendant with no account.

Two shapes for deliverable 2, with their consequences, and **the study chooses
neither**:

* **Only staff with a login may propose.** Honest and small; the capability
  simply does not exist for most of the workforce until the staff app ships, and
  the UI must say so rather than appear broken.
* **A supervisor may enter a proposal on someone's behalf**, recorded as such —
  *proposed by X on behalf of Y*. Reaches everyone, and the provenance must be
  kept or the record quietly claims the staff member did something they did not.
  That is the same provenance rule §3.2's A7 established for attendance, and it
  is the reason this option is not free.

#### The delta against the gold mockup

**Frame 1 · My Schedule** is a read-only month today. S1 gives it its first
*action*, which changes what the frame is. **Frame 4 · Leave Approvals** is the
natural home for the manager's side, and it becomes a queue of **two kinds** —
leave requests and swap proposals — or gains a sibling. Deliverable 3's call;
recorded so it is not discovered while drawing.

---

> **A note on numbering.** The two rulings below were relayed as *"subject 4"*
> and *"subject 5"*. Against the brief's §2 seed list they are items **5**
> (overtime and hours) and **6** (skills and certifications), and they are
> written up here as §3.5 and §3.6 so that a section number and a seed-list
> item never disagree. The brief's item 4 is §3.4, above.

### 3.5 · Overtime and hours — Workforce produces the numbers and never calculates pay

**Ruled by the architect, 2026-08-31, owner shown and not objecting** — an
`ARCHITECT` row throughout, cited as an architect ruling with its date and never
as an owner fact.

> **Workforce produces the numbers; it never calculates pay.**

The reasoning as given: pay calculation is a legal and compliance domain that
differs by country — Qatar's WPS, India's PF/ESI, gratuity rules — and by hotel
— allowances, deductions, contracts. **Building it wrong is a salary dispute.**
It is Finance's territory, already a separate application in the plan, or the
hotel's own accountant and payroll software. What every one of those needs from
us is identical: correct numbers.

#### This is `GUEST-Q6`'s boundary, one application over

The precedent is exact and worth naming, because it means this is the platform
being consistent rather than this round being cautious. `GUEST-Q6`, ruled by the
owner on 2026-08-31: GuestOps v1 is the reservation book plus the stay's
commercial terms, and **the folio is not in it** — posting, payments,
settlement, invoicing and night-audit posting are *"Finance's domain
(CLAUDE.md's installable list), a later round"*, with the consequence accepted
knowingly.

```text
GuestOps    carries the stay's terms, never settles the guest      → Finance
Workforce   carries the hours and the days, never pays the person  → Finance
```

Two applications, one boundary, the same reason: **the domain that produces a
fact is not the domain that turns it into money.**

#### What Workforce computes

| # | Produced, per staff, per business-day and per month | Source |
|---|---|---|
| O1 | days **posted** | the rota (§3.1) |
| O2 | days **present** | attendance (§3.2) |
| O3 | **late** count | attendance — see the implication below |
| O4 | **leave taken**, by type | leave (§3.3) |
| O5 | **holidays worked** → a comp-off credit later | attendance × the holiday calendar (L5) |
| O6 | **hours worked** | attendance in/out times |
| O7 | **overtime hours**, flagged when hours exceed the property's configured threshold | O6 against the OT threshold |

| # | The rest of the ruling | Verdict |
|---|---|---|
| O8 | The **OT threshold is per-property configuration** — one rule, e.g. *"OT after 9h/day or 48h/week"*, set exactly as the leave policy is | **GAP** — `ARCHITECT` — proposed **IN v1** |
| O9 | **Month-end export** — a per-staff summary the accountant or payroll software takes. **File download in v1** | **GAP** — `ARCHITECT` — proposed **IN v1** |
| O10 | A **payroll connector** through the Hub later, the same pattern as the biometric one | **GAP** — `ARCHITECT` — **OUT of v1**, and see below: it is outside the *ruled* connector contract, not merely later |
| O11 | Pay itself — calculation, payslips, statutory filing | **DIVERGES** — permanently out. Finance's, or the hotel's existing payroll |

> **The one thing HR configures: the overtime threshold. The one thing HR gets:
> the month-end sheet nobody has to compile by hand.** Pay happens wherever it
> happens today — Workforce makes its inputs indisputable.

O8 gives the undrawn property-policy surface flagged in §3.3 its **second
occupant**. One screen now holds the leave policy and the OT threshold, which is
an argument for drawing it as *the property's workforce policy* rather than as a
leave setting that later grows a stranger.

#### O10 is not "later by preference" — the outbound half is already ruled out

A payroll connector **sends data out**. ADR 0128 §4 (`CONN-Q5`, planner
2026-08-31): *"v1 connector scope is inbound-only… Write-back is a separate
connector capability in a later round — kept out of the initial contract so
connector permissions stay clean."*

So the file download in v1 is not a stopgap chosen for speed: **it is the only
thing the ruled connector contract currently permits.** The parallel with §3.2
is worth seeing — the biometric connector is inbound and fits the contract
today; the payroll connector is outbound and waits for the capability that
`CONN-Q5` deferred. Same pattern, opposite side of the boundary.

#### O9's export mechanism exists — and it is not shared, which is the finding

The natural assumption is that this is `GUEST-Q7`'s print gap again — *"no
print surface exists in any chapter or ADR."* **It is not, and the difference
matters.** Measured 2026-08-31:

`apps/desktop/src/modules/core-administration/domain/import/download.ts:31-44`
holds `saveFile(text, name)` — a CSV blob, an anchor download, the object URL
revoked after the click. Its docstring records the alternative it rejected and
why: *"Deliberately not `showSaveFilePicker`: it is behind a permission prompt,
it is unavailable in some WebView2 configurations, and its rejection is
indistinguishable from the user cancelling."*

The mechanism exists, is proven in a shipped feature, and has already had its
hard question answered. **What it is not is a shared surface.** It lives inside
Core Administration's module — its own docstring says *"two callers, in two
features"*, both of them that module's — and Workforce is a different
application, in a different repository, binding to the platform only through the
contracts and the SDK (`HotelOsApps/README.md`). It cannot import it.

That leaves two roads and only one of them is allowed. CLAUDE.md: *"Anything two
components must agree on lives in one place… if you are about to write it in a
second service, it belongs in a package."* A second copy of a file-save helper
is a small thing that drifts in exactly the way that rule describes — one of
them forgetting `revokeObjectURL` is invisible, because the download still
works.

**What the application SDK exposes to a packaged frontend module is specified
nowhere**, and this is the first application to need it. Registered as a
question; it belongs with `APPS-Q1`'s registry-driven-shell prerequisite rather
than to this round.

#### The ruling resolves part of §3.2's residue, by implication

§3.2 closed with three unanswered questions. **O3 and O6 answer the first one
sideways**: a *late count* and *hours worked* cannot appear on a month-end sheet
unless lateness and in/out times are recorded facts. So attendance is not a
present/absent tick — **the manual floor (A3) must capture in and out times**,
which is meaningfully more than a checkbox and is what makes manual marking
honest enough to sit in the same column as a fingerprint.

Recorded as an **implication, not an owner fact**, and flagged for confirmation
rather than treated as settled. §3.2's other two residue questions — the
posted-versus-present view, and what happens when attendance contradicts the
rota — are untouched by this ruling.

**And O5 gives §3.3's question 8 a direction**: *holidays worked* is produced
now, and the comp-off credit it feeds is explicitly *later*. The number and the
rule that consumes it are separated, which is the cheap half first.

#### "Per business-day" leans on a ruling whose home is open

O1–O7 are produced *per business-day*. For a night auditor rostered 23:00–07:00
that is not a calendar date, and the platform has already been here:
ADR 0128 §6 (`CONN-Q6`) rules the business date **a canonical property-level
operational concept owned by the platform, not by a connector**, with the
boundary stored beside `check_in_time` in Core Administration and the current
business date **derived, not stored** — `operating_day(timestamp, boundary)`, in
the Context Service.

Two consequences for this application, neither of them a new decision:

* Workforce **consumes** business-date semantics and must not compute them —
  the same sentence ADR 0128 §6 wrote for connectors, and the reason
  `Tenancy.cs:70-75` says a shift crossing midnight belongs to the property's
  zone and *"never the server's"*;
* the ruling states *"no storage or event is introduced now merely to solve the
  connector problem"* — so Workforce inherits the same restraint and does not
  invent a business-date column to make its own reporting easier.

#### The delta against the gold mockup, and what the ruling does not say

Nothing in the seven frames produces a number. There is no month-end view, no
hours column, no OT indication and no export control. **Deliverable 3 gains a
reporting surface**, alongside §3.3's property-policy surface.

And two things the ruling deliberately leaves unstated, named so they are not
read into it:

* **It flags overtime; it does not prevent it.** Nothing in the ruling caps
  hours or blocks an assignment that crosses the threshold — consistent with
  `WF-Q5`'s warn-and-allow. Whether a manager is alerted *during* the week
  rather than at month end is a real question the ruling does not answer, and
  it is the difference between a report and a control.
* **The export's format and its granularity** — per business-day rows, or one
  month-end line per person — are not specified. CSV is the mechanism O9
  inherits; what is *in* the file is deliverable 2's.

#### Slicing: the export is a capstone, not a slice-1 item

O1–O7 draw from the rota, attendance, leave and the holiday calendar. **The
month-end sheet cannot exist before slices 2 and 3**, and chapter 01's four
slices have no home for it — it arrives after Leave, or with the Capability
slice. Stated because a reporting feature that looks small is often scheduled
early and then blocks on four other things.

---

### 3.6 · Skills and certifications — one optional date, and a system that warns without ever forbidding

**Ruled by the architect, 2026-08-31, owner shown** — `ARCHITECT` throughout.

The ruling builds on ground chapter 01 already holds: the **Capability**
aggregate (skills · languages · shift pattern, slice 4) is ADR 0063 §Q5's
remainder, and `skills`/`languages` left `Staff` for Workforce because
*"capability is Roster's, structure is Core's"*. Nothing here reopens that.

#### The whole design is one optional field

| | |
|---|---|
| **no date** | an **ability** — *"speaks Arabic"*, *"can operate boiler"* |
| **with a date** | a **certification** — *"fire warden — valid until 12 Mar 2027"* |

| # | The ruling | Verdict |
|---|---|---|
| C1 | A skill gains one optional `valid_until` | **GAP** — `ARCHITECT` — proposed **IN v1 of the Capability slice** |
| C2 | **60 / 30 / 7 days before expiry** the skill appears on the department head's and HR's **Attention list** | **GAP** — `ARCHITECT` — new surface |
| C3 | **After expiry** the skill reads `EXPIRED` on the person, on the People frame, **and in the answers Workforce gives** — *"Rajan — expired 12 Mar"*, never silently included and never silently hidden | **GAP** — `ARCHITECT` — and the third clause is the load-bearing one |
| C4 | It **blocks nothing**. An expired card never stops a rota assignment; the rota **warns and names it**, a person decides | **GAP** — `ARCHITECT` — see the conflict below |
| C5 | One report: the property's **certification register** — every dated skill, holder, expiry — the sheet a safety inspector asks for | **GAP** — `ARCHITECT` — proposed **IN v1 of the slice** |

**One optional field carrying two concepts is the right shape**, and it is worth
saying why rather than only that it is simple: the alternative is a `kind`
discriminator that every reader must branch on and that can disagree with the
data — a row marked *ability* with an expiry date, or *certification* without
one. Here the date **is** the discriminator, so the inconsistent state cannot be
written. That is the house pattern — encode the rule where violating it is
inexpressible.

> **Configuration cost to HR: typing a date when they record the skill. That is
> all.** Which is the same test chapter 01 §1.4 sets — a duty manager at 7 a.m.,
> not an HR system's feature list.

#### C3's third clause is the one that changes another application

*"and in the answers Workforce gives"*. `WF-Q6` records that Jobs reads postings
**through the Context Service**, per the constitution, and *"who can do X"* is
the same kind of question one application over.

So the expiry state has to travel **in the Context answer**, not only on
Workforce's own screens. If Context returns a bare list of qualified people, the
first consumer to care about expiry re-implements the rule, the second
implements it differently, and *"we didn't know"* becomes true again at one
remove. Recorded as a design consequence for deliverable 2; the read-view's
shape is Jobs' round, but **what it must carry is decided here**.

#### C4 — the ruling's substance is clear, and the precedent it cites says the opposite

The ruling was relayed as *"the same warn-never-forbid rule as the double-booked
room"*. **The record says the double-booked room is refused, not warned.**
`GUEST-Q7`, ruled by the owner on 2026-08-31: *"the room-level conflict check —
one room, overlapping stays, **refused loudly** (in regardless of mode)"*.

Per CLAUDE.md — *where a ruling and a document conflict, say so; never resolve it
by choosing* — this is reported and **not** resolved here. What is recorded:

* **C4's substance is adopted.** *Expired skills warn and never block* is
  unambiguous and is what the design will carry.
* **The citation does not support it**, and there is a precedent in this
  application that does: `WF-Q5`'s warn-and-allow on exhausted leave balances —
  *"hotels override reality daily"*.
* **A reading that reconciles the two**, offered as a proposal and not a
  finding: the platform refuses what is **physically impossible or
  self-contradicting** — two stays cannot occupy one room, and a record saying
  they do is corrupt — and warns on what is a **judgment** — a person with a
  lapsed card can physically work the shift, and whether they should is the
  hotel's call. If that is the rule, both rulings are consistent and neither
  needs changing; it simply has never been written down as one sentence.

**Nothing was decided on this.** The architect confirms which precedent C4
stands on, and whether the distinction above is worth writing into the record.

#### C2's Attention list is a new surface, and it stays inside the module

Nothing in chapter 01's five sections is an Attention list, and no frame draws
one. Two boundaries it must respect, both already ruled:

* **`WF-Q2` is not ruled** — shell-side surfacing of Workforce facts (the
  status bar, the property card) is *"not in v1; module-only until ruled"*. So
  the Attention list is a **surface inside Workforce**, not a desktop
  notification, and this ruling does not quietly grant what `WF-Q2` withheld.
* **Who sees it is resolved from postings** — *the department head's and HR's*
  — which is §3.3b's L8 mechanism a second time. The same posting that resolves
  a leave approver resolves an Attention audience, and `HR` is a canon
  department code (ADR 0119). One resolution rule, two consumers.

#### C5 is the third thing that needs an export, and the second that needs a report

The certification register is *"the sheet a safety inspector asks for"* — which
means it is printed or handed over, not merely looked at. It meets **the same
platform gap as §3.5's O9**: the file-save mechanism exists in Core
Administration's module and is not available to a packaged application.

Three surfaces now want it — the month-end payroll sheet, the certification
register, and (from `GUEST-Q7`) GuestOps's registration-card print. That is no
longer one application's inconvenience; it is a platform capability three rounds
have now asked for, and the question registered under §3.5 carries both.

#### Slicing: chapter 01 puts this last, and compliance may not want to be last

Capability is **slice 4** — *"assignment intelligence for Jobs and the AI
apps"*. This ruling gives that slice real content, and it also changes what the
slice is *for*: a certification register is not assignment intelligence, it is a
compliance obligation, and a hotel's fire-warden card expires whether or not
Jobs has shipped.

**Proposed, not decided:** the dated half of C1 with C3 and C5 is a candidate to
move earlier than skills-for-assignment. The owner rules the slices; this is
flagged because the argument for slice 4 was written before the compliance half
existed.

---

### 3.7 · The MOD duty — a span, never a date

**The owner's answer, 2026-08-31**, relayed through the architect:

> *"We can't do per-day, because MOD may run 8:00 pm to 8:00 am — it covers two
> dates."*

One sentence, and it is **structural rather than a preference**. It overturns a
line in chapter 01 §3 and the shape of two frames, and it does so for the same
reason the platform has already met once from a different direction.

#### The rows

| # | Feature | Verdict | Why |
|---|---|---|---|
| M1 | `DutyAssignment` carries **start–end datetimes**, not a date or a date range | **DIVERGES** — `OWNER` — chapter 01 is wrong here | §3 says *"date (or date range)"*. A duty running 20:00→08:00 is neither: it is one span that happens to cross a date boundary, exactly as a night shift does |
| M2 | *"Who is MOD right now?"* is answered by **the clock against the span** | **GAP** — `OWNER` — proposed **IN v1** | And, critically, **derived — never stored**. Below |
| M3 | *"One MOD per property per day"* becomes **no two MOD spans overlap** | **DIVERGES** — `OWNER` — a consequence of M1 | Not one constraint in different words: one is a unique key, the other is an overlap check |
| M4 | The MOD register shows who holds the duty **now and next** — the *"who to call"* view | **GAP** — `DEFAULT` — seen on frame 5 | The register's daily use is a lookup, not a planning grid |
| M5 | An optional **free-text handover note** on the duty; **nothing blocks without it** | **GAP** — `DEFAULT` — seen on frame 5 | Warn-never-forbid again (§3.6's C4). A mandatory handover note is a field people type "n/a" into |
| M6 | **Incident logging stays out of v1** — Jobs' territory when it exists | **DIVERGES** — `DEFAULT` — permanently not this application's | An incident is work that needs doing, which is Jobs' definition (`APPS-Q1`: *Jobs = what needs doing*) |

#### M2 is the business-date lesson arriving from the staffing side

ADR 0128 §6 (`CONN-Q6`) ruled the business date and, more usefully here, ruled
**how** it is held:

| Concept | Owner | Stored? |
|---|---|---|
| the business-day boundary | Core Administration | **yes** — stable configuration |
| the **current** business date | Context Service, **derived** — `operating_day(timestamp, boundary)` | **no** |

The MOD duty is the same shape one domain over: **the span is stored, the
"current" is computed.** So there is no `is_current_mod` flag, no
`current_mod_staff_id` on the property, and no nightly job moving a marker —
each of which is a value that can be wrong while the data beside it is right.
*"Who is MOD now"* is a question with a clock in it, and the answer is a query.

**And the two interact.** A property whose business day rolls at 04:00 and whose
MOD runs 20:00→08:00 has a duty spanning two business dates as well as two
calendar ones. §3.5 established that Workforce **consumes** business-date
semantics and must not compute them; M1 is why that is not academic.

#### M3 is a different database object, and that is the part that gets missed

Chapter 01's *"one MOD per property per day; assigning a day that has one
replaces it with a named confirm"* reads as a uniqueness rule. As a date it is
one. As a span it cannot be:

```text
per-day     UNIQUE (property_id, duty_date)          a unique key
per-span    no two spans for a property overlap      an overlap constraint
```

The behaviour the chapter wanted survives intact — assigning over an existing
duty still replaces it behind a named confirm — but **what detects the clash is
no longer a unique index**, and a design that keeps the sentence while changing
the column would silently lose the guarantee. ADR 0055 permits the DDL for this
in a migration, so the no-native-SQL rule is not in the way; the point is that
it must be *chosen* rather than inherited.

#### The delta against the gold mockup — two frames encode the assumption

The sharpest mockup finding of the walk, because the overturned assumption is
drawn into the **shape** of two views rather than stated in a caption:

| | What is drawn | Against M1 |
|---|---|---|
| **Frame 5 · Duty Roster** | *"a month strip showing who is MOD each day"* — one cell per day, one name in it | **A day cell cannot hold a span.** A 20:00→08:00 duty belongs partly to two cells, and the strip has no way to say so |
| **Frame 2 · the MOD ribbon** | one name per day column — `Priya T.` under Mon, `Rahul N. · SEC` under Wed, `—` under Sun | **A day column can now hold two MODs** — the one ending at 08:00 and the one starting at 20:00. The ribbon's one-name-per-column shape is the per-day model made visual |

Both are listed for deliverable 3, and neither is a small correction: *what does
a duty crossing midnight look like on a week grid* must be answered in the
drawing, not in a note. **And it reaches the printed week** (§3.9's N1), which
inherits whatever the ribbon becomes.

#### What M4–M6 leave open

* **Is the MOD always property-wide?** `WF-Q1` and chapter 01 both say the duty
  is property-scoped and the person keeps their department, which this answer
  does not disturb. Recorded as untouched rather than re-confirmed.
* **What happens in the gap** — one duty ends at 08:00, the next begins at
  20:00, and nobody is MOD for twelve hours. A normal state the register shows
  as *"none"* (frame 5 already draws a `—`), or a gap somebody is warned about?
  M4's *now and next* view makes it visible, which may be all that is wanted.

---

### 3.8 · Postings and zones — the answer is *from Workforce*, and it makes Context the join

**The owner's answer, 2026-08-31**, relayed through the architect:

> **"From Workforce."** Zone assignment is a **Workforce posting**, not a Room
> Care morning allocation: a cleaner is **posted to a zone on the roster**, and
> Room Care reads *"who has zone 3"* through **Context**.

This settles the question the index has carried since the walk opened, and it
settles it the way that costs Room Care least: the zone is part of the standing
arrangement, not a decision re-made at 7 a.m.

#### The rows

| # | Feature | Verdict | Why |
|---|---|---|---|
| Z1 | A staff member is **posted to a zone**, and the posting is Workforce's | **GAP** — `OWNER` — proposed **IN v1** | Chapter 01 §3's `Posting` is staff → department with **no zone**. This adds one |
| Z2 | Room Care reads *"who has zone 3"* **through the Context Service** | **COVERED** by principle, **GAP** as work | CLAUDE.md §5 *Context over joins*, and `WF-Q6` recorded the same answer for Jobs. `CTX-Q4` makes it **this round's delivery** — below |
| Z3 | A zone posting is **durable until changed** — a standing arrangement edited on the rota, not re-decided daily | **GAP** — `DEFAULT` — seen on frames 2 and 6 | Needs no new lifecycle: `Posting` already carries **effective from/to** (ADR 0052) |
| Z4 | Zones are **not exclusive to one department** — Engineering and Housekeeping each hold their own assignments with their own meanings | **COVERED** — `DEFAULT` — and the implementation already says so | `ZoneTypes` ships today. Below |

#### Z1 · The zone belongs **on the posting**, because the posting is what disambiguates it

Proposed for deliverable 2, with the argument rather than the shape alone:

```text
proposed so far   Posting   staff · department · job role · primary ·
                            effective from/to · reporting manager
Z1 adds                     · zone (optional)
```

A standalone staff ↔ zone link would be the wrong object. **Z4 says a zone can
mean different things to different departments**, so *"Anita has zone 3"* is not
a complete fact — *"Anita has zone 3 **as Housekeeping**"* is. The `Posting`
already carries the department, so putting the zone there makes the ambiguous
state **inexpressible** rather than merely discouraged, and it inherits the
effective dating Z3 needs for free.

**Optional, not required**: most postings have no zone — a receptionist is
posted to Front Office and to no area — and a required zone would make the
common case carry a field somebody has to invent a meaning for.

#### Z4 · The platform already models this, and it is worth knowing before designing it

`services/masterdata-service/src/Domain/Catalogue.cs:9-19` — `ZoneTypes` ships
today with five values:

```text
housekeeping · maintenance · inventory · security · inspection
```

So the platform's existing answer to *"can two departments share a zone"* is
that **they hold two typed zones**, not one zone with two owners. That is a
materially different design from the one the default's wording suggests, and it
changes what *"zone 3"* means: there may be a **housekeeping zone 3** and a
**maintenance zone 3** over the same physical floor, each with its own rooms and
its own postings.

Neither reading is wrong and the study does not choose. **What it records is
that the decision is already half-made in shipped code**, and a design assuming
one shared zone would be diverging from it without saying so.

#### A live tension to report — `Zone.DepartmentId` is Workforce's by the table and Core's in the schema

ADR 0063's relationship table lists **Zone → Department** as *Roster /
Workforce's*. The column is still in Master Data — `Catalogue.cs:38-44`,
`Zone.DepartmentId`, nullable, *"a zone is often drawn on the floor plan before
anyone decides which department owns it"*.

And ADR 0063 **explicitly did not rule it**, in its own Consequences:

> *"Open and touched by this ruling — `zones.department_id` and
> `assets.owning_department_id` are the same shape as the three columns that
> just left, and are not ruled here."*

**Reported, not resolved** — and there is a reason for care that the ownership
matrix may not have weighed. `Catalogue.cs:27-29` records the column as
**load-bearing for authorization**: *"a room reaches its department through its
zone — so reassigning `DepartmentId` is one tuple write however many rooms the
zone contains."* Moving it is not the same kind of move as the three columns
that left `Staff`, and **this round proposes nothing about it**. Z1 needs only
the staff↔zone link, which is new and unambiguously Workforce's.

#### Z2 · `CTX-Q4` makes the resolver this round's delivery, with its constraint

`CTX-Q4`, ruled by the architect on 2026-08-31: Context growth is routine and
the principle is the gate — and the rule travelling with every commission is
that a new resolver arrives as **one delivery by the contributing domain's
round**: the read view, the Context RPC, **and its stated constraint**. *"A
round that ships the view without the RPC has shipped half."*

Workforce is the contributing domain, so the **zone → who is posted** resolver
is **this round's work**, not Room Care's. Its constraint, stated here so it
travels with it:

> **Allocation input, never a gate.** The absence of a zone posting must not
> block a Room Care task. A room still gets cleaned when nobody is posted to its
> zone, and a readiness check built as a blocker on this resolver is the named
> failure.

That constraint is not invented — it is **`APPS-Q2`**, ruled the same day: *an
application's own flow is never gated on another application being installed; an
absent dependency loses its capability, never the flow.* Workforce is
installable, so a property may run Room Care without it, and ADR 0116 §6 already
makes department membership before Workforce **empty by design**. The capability
Room Care loses is *knowing who has the zone*; the flow it keeps is cleaning the
room.

#### Why this makes Context the join rather than a convenience

The two halves of *"who cleans zone 3 today"* live in different applications,
and after this ruling neither can answer alone:

```text
Workforce     who is posted to zone 3      staff ↔ zone   (Z1, new)
Room Care     which rooms are in zone 3    room  ↔ zone   (ADR 0056)
Master Data   what a zone is               the entity     (ADR 0063 kept it)
```

Three owners, one question. That is *Context over joins* not as a style rule but
as the only shape that works — a direct read would have to reach into two
applications' schemas, which is the one rule modularity rests on, and Stream
FF's round refused exactly that for the same reason.

#### The delta against the gold mockup

| | What is drawn | Against this subject |
|---|---|---|
| **Frame 6 · People** | postings — staff member, posting(s), job role, reporting manager | **No zone.** Z1 adds it to the posting, so it belongs on this frame |
| **Frame 2 · Team Rota** | one department, people down, days across | **No zone anywhere.** Z3 says the zone is edited *"on the rota"*, so the rota needs somewhere to show and change it — a column beside the person, or the rows grouped by zone |

Frame 2 now carries three changes from this walk — the shift chip (§3.1, §3.9),
the MOD ribbon (§3.7) and the zone (§3.8). Worth saying plainly: **it is the
frame the walk changed most**, and redrawing it is not a touch-up.

---

### 3.9 · Notifications — the printed week, and events published for an app that does not exist yet

**Ruled by the architect, 2026-08-31** — `ARCHITECT` throughout. Relayed as
*"subject 6"*; against the brief's §2 seed list it is item **9**, and it closes
subjects 1–6 of that list.

> **v1 is the printed week, done properly.**

| # | The ruling | Verdict |
|---|---|---|
| N1 | A **print-ready per-department rota view**, built for a **monochrome photocopier** | **GAP** — `ARCHITECT` — proposed **IN v1** |
| N2 | A shift carries **name + colour + short code** — three attributes on one entity. **Short code in the cell, legend row beneath** | **GAP** — `ARCHITECT` — and it closes §3.1's question 1 |
| N3 | Mid-week changes keep a **change list for the week** — *"Tue: Rajan N→M, covered by Anita"* — **a record, not a memory** | **GAP** — `ARCHITECT` — proposed **IN v1** |
| N4 | The **staff app is next-version**, by the owner's ruling | `OWNER` — consistent with §3.2's A5 |
| N5 | The design's job now is to **publish the events** — `shift.assigned`, `shift.changed`, `leave.approved` — *as if the app existed*, so that when it arrives it subscribes and notifies **with zero changes here** | **GAP** — `ARCHITECT` — proposed **IN v1**, with a platform prerequisite: see N5 below |
| N6 | Printing itself is **`SHELL-Q23`** — the shell owns the print dialog, an application hands it a print-ready view. **No improvised printer code** | **COVERED** by a registered question — cite the row, build nothing |

#### N2 closes §3.1's question 1, and by the argument the question named

§3.1 asked what a colour chip says, once shift names are free-form: a short code
alongside the name, or colour alone with a legend. The question recorded the
consequence that would decide it — *"which also decides whether the rota can be
printed or photocopied"* — and that is exactly what decided it. A monochrome
photocopy destroys colour and keeps glyphs, so **the cell must carry text that
survives losing every colour in the design.**

The answer takes both halves rather than choosing: **colour for the screen,
short code for the cell, legend beneath for the page.** One week reads at a
glance in the office and still reads after a photocopier has thrown the colour
away.

**This amends §3.1's proposed aggregate**, which is deliberately left as it was
written so the trail survives:

```text
§3.1 proposed   ShiftDefinition   name · start–end · colour · active
N2 amends it    ShiftDefinition   name · short code · start–end · colour · active
```

The short code is **the property's**, typed when the shift is created — not
derived from the name. A derived initial collides the moment a property creates
*Morning* and *Mid-shift*, and a collision in a rota cell is two different
shifts that look identical on the page.

#### N3's change list should be the event stream rendered, not a second record

*"A record, not a memory"* is the requirement. The design question it opens is
whether the week's change list is **its own stored list** or **a projection of
the events N5 already publishes** — and the platform's own rule points hard at
the second: a stored list that is written alongside the events can disagree with
them, and CLAUDE.md's *"a denormalised column that is allowed to disagree with
its source is a defect with a delivery date"* is the same failure one level up.

**Proposed:** the change list is `shift.changed` for that week and department,
rendered. Nothing is stored twice, and the printed sheet and the audit trail
cannot drift apart because they are the same fact.

One payload consequence, stated because it is easy to miss until the sheet is
built: *"Tue: Rajan N→M, **covered by Anita**"* is **two** facts — a change to
one person's shift and an assignment to another's. Either `shift.changed`
carries the cover's identity, or the line is composed from two events that must
be correlated. Deliverable 2 decides which; the study records that the sentence
in the ruling is not one event.

#### N5 — publishing the events is right, and it has a platform prerequisite nobody has met

The pattern is exactly right: **the event is the integration point, and it costs
nothing to publish it now.** An application that publishes its facts properly is
one a future consumer attaches to without a change — which is what N4 and N5
together buy, and it is the constitution's event-first architecture doing its
job.

**Measured 2026-08-31, and this is the finding: nothing would carry those
events today.**

* `services/kernel/crates/kernel/src/events/subjects.rs:25-35` — `publish_subject`
  validates the *shape* (`domain.action`) and nothing else. **Nothing rejects
  `shift.assigned`.**
* `services/kernel/crates/kernel/src/events/streams.rs:64-185` — the stream
  `SPECIFICATION` routes by domain segment, and it claims **no `shift`, no
  `leave` and no `duty` domain.** Chapter 01 §4's `user.posted` /
  `user.posting_ended` are fine — `property.*.user.>` is claimed by
  `OPERATIONAL` — but the three subjects this ruling names are not.
* So an unrouted subject is **acked, matches nothing, and dead-letters
  silently.** That is not a deduction: `streams.rs:85-90` records it happening
  already —

  > *"Added when Master Data grew staff, vendors, media and external mappings:
  > they were published for a release with no stream claiming their subjects,
  > so every one acked, matched nothing and dead-lettered — the exact failure
  > ADR 0006 exists to prevent."*

* And `services/kernel/crates/kernel/tests/jetstream.rs:288-320` shows the
  platform's own remedy: subjects belonging to **unbuilt** applications —
  `room.zone_changed`, `workorder.created`, `housekeeping.task.assigned` — are
  named in advance *"because the stream filters must already cover them"*.
  **Workforce's are not among them.**

**So N5 is adopted with its prerequisite named**: the stream specification must
claim Workforce's domains before Workforce publishes, or the events are lost in
exactly the way the platform has already been burned by once. Publishing into a
stream nobody has claimed is worse than not publishing — it looks like it worked.

`OPERATIONAL` is the natural home on ADR 0006's *route by meaning* rule, beside
`staff.>` and `user.>`: a shift is the property's people and its shape. That is a
**recommendation, not a decision** — the streams are the Kernel's.

#### And this is the second thing a packaged application cannot do for itself

`streams.rs` is in the **Kernel**, in the platform repository. Workforce is an
installable application in another repository, binding through the contracts and
the SDK (`HotelOsApps/README.md`). **It cannot add its own domain to the stream
specification**, and nothing in ADR 0092's package contract or ADR 0122 says how
an installed application's event domains reach the Kernel's routing.

That is the same shape as §3.5's file-save question and it is a separate
question: how does a **packaged application declare its event subjects** — in
the manifest, materialised at install as the application object already is
(ADR 0116 §5)? Or does the platform ship a stream for application domains?
**Nobody has ruled it**, and Workforce is the first installable application to
need it.

**Registered as `PKG-Q39`, 2026-08-31**, with this evidence carried whole, and
split into the two halves it turned out to be:

```text
now, CC       shift · leave · duty join the pre-named set in OPERATIONAL
              beside staff.> and user.>, per ADR 0006's route-by-meaning
              and the platform's own jetstream.rs precedent
the real Q    an installed application's event domains, manifest-declared
              and materialised at install — the CONN-Q10 pattern.
              Planner, with the package rounds
```

`CONN-Q10` is the same shape one package kind over: a `kind: connector`
package had no way to declare *what it is* either, and the planner's answer put
`kind` in the canonical signed manifest. **Event domains are the second thing a
packaged application cannot do for itself**, and the register now says so with
the first named beside it.

#### `SHELL-Q23` — cited, not improvised, and one correction to offer

The row exists and already names this round:

> **`SHELL-Q23`** — *"No print surface exists anywhere in the platform, and two
> applications now need one."* FF (GuestOps: the registration card) and **GG
> (Workforce: the weekly rota sheet, monochrome-photocopier-grade)**, both
> 2026-08-31. Open — the shell's question. The architect's reading: an
> application module hands the shell a print-ready view (its own HTML, print CSS
> its problem) and **the shell owns the OS print dialog via the webview**; no
> per-app printer code, no PDF library until a real need names one.

**Nothing is improvised here.** N1 produces a print-ready view — HTML and print
CSS, which is this application's problem — and the dialog is the shell's.

**Two corrections were offered and both were applied to the row, 2026-08-31**:
it now cites **§3.9** as well as §3.6, counts the **certification register as a
third consumer**, and leaves the **file-save half explicitly open** rather than
folding it in — *a file handed to the user is a different shell capability from
a print dialog.*

#### This narrows §3.5's export question rather than closing it

Three surfaces wanted a way to get paper or a file out of the platform. They are
not one question, and `SHELL-Q23` answers only part:

```text
GuestOps registration card    print   →  SHELL-Q23
Workforce rota sheet          print   →  SHELL-Q23
Workforce certification reg.  either  →  SHELL-Q23 if printed
Workforce month-end payroll   FILE    →  still open
```

The month-end sheet is *taken by payroll software*, so it is a **file**, not a
page — and a file-save available to a packaged frontend module is a different
capability from a print dialog. §3.5's question survives, **narrowed to the
payroll export**, and the register row should say so rather than being closed by
`SHELL-Q23`.

#### The delta against the gold mockup

Frame 2 is a screen. **A print view is a different artifact**, not a print
stylesheet bolted to it: no rail, no header controls, no hover, the whole
department on one page, a legend row, and the week's change list beneath.
Deliverable 3 gains it as a **new frame** — the third new surface this walk has
produced, after §3.3's property-policy screen and §3.5's reporting view.

And the chip in frame 2 changes twice over: §3.1 said it must render from data
rather than three hardcoded classes, and N2 now says what it renders — **the
short code**, with the legend below the grid on both the screen and the page.

#### What this subject did not settle

* **How staff receive the printed sheet.** *"Printed"* answers the medium and
  not the distribution — pinned on the department noticeboard, handed out,
  or both. It probably needs no system support at all, which is why it is
  recorded here rather than raised as a question.
* **WhatsApp**, which the brief's seed list named, was not mentioned in the
  ruling and is not proposed. It would be a Communication Platform capability
  (ADR 0115 §1C) and that pillar is parked with the rest.

---

## 4 · The verdict table

**All eight seed subjects are walked.** §3 produced **54 rows**; this section is
those rows sorted by verdict, plus the four worklists they imply. It is what the
feature plan — deliverable 2 — is built from, and it is the last section written
because a verdict table assembled subject by subject would have hidden the
pattern the whole walk turned out to have.

```text
DIVERGES   6    chapter 01 or the drawn surface is wrong, and why
GAP  IN   28    proposed for v1
GAP  OUT   4    proposed out, each with the ruling that puts it there
COVERED   14    already right — and worth listing, because a refusal
                that survived being tested is stronger than one nobody
                questioned
OPEN       2    the answer raised something nobody has ruled
```

### 4.1 · `DIVERGES` — where the record is wrong

The six rows that change something already written down. These are the study's
most load-bearing output, because each is a place a build would have gone ahead
on a false premise.

| # | What is wrong | What it becomes |
|---|---|---|
| **M1** | Chapter 01 §3: `DutyAssignment` is *"date (or date range)"* | **start–end datetimes**. A MOD running 20:00→08:00 is a span, and the owner's *"we can't do per-day"* is structural |
| **M3** | Chapter 01 §3: *"one MOD per property per day"* | **no two spans overlap** — an overlap constraint, not a unique key. The behaviour survives; the database object does not |
| **M6** | — | incident logging is **Jobs'**, not this application's |
| **A4** | Chapter 01 §8 implies a punch clock would be built here | the device is a **connector** — ADR 0128 §2, `kind: connector` |
| **O11** | — | pay calculation is **permanently** Finance's or the hotel's payroll. `GUEST-Q6`'s boundary, one application over |
| **N2 / R1** | Mockup frame 2: a closed set of three shift codes, colour from three hardcoded CSS classes | an **open property catalogue**, each shift carrying name + typed short code + colour |

### 4.2 · `COVERED` — and two refusals that were tested

Fourteen rows were already right: R3 (times exist, on the wrong aggregate), R6,
R7, R8, R9, R10 (the planning machinery — **the mockup agreed with the owner
before he was asked**), A6 (the mapping mechanism), L1 (the four leave types),
Z2 (Context as the join, by principle), Z4 (typed zones), N4, N6, and the two
prerequisites.

Chapter 01 §8's refusal list was §2.4's prediction of where the walk would pay,
and it did — **in both directions**:

```text
no attendance / punch clock   OVERTURNED as a fact, UPHELD as an
                              implementation — for a reason the chapter
                              never gave (it is a connector)
no accrual engine             NARROWED — the model gains a rate,
                              the engine stays refused
no payroll                    CONFIRMED, and now with the reason
                              written down
no shift-bidding              untouched — and §3.3's "gap" was kept
no compliance rule engine      deliberately cheap so as not to open it
no biometric integration      re-sited, not refused: it happens, and it
                              happens in the Hub
```

**A refusal that survives being tested is worth more than one nobody
questioned**, and three of these are now defensible where before they were
merely asserted.

### 4.3 · The v1 scope proposal

Proposals, not decisions — the owner rules v1.

**Proposed IN.** Postings with a zone (Z1) · the open shift catalogue with name,
typed short code, colour (R1–R5, N2) · the rota, copy-last-week and swap
(R8–R10) · the MOD register as spans, *now and next*, with a handover note
(M1–M5) · attendance as one source-agnostic fact with provenance, manual source
only (A1–A3, A7) · leave with a per-property policy and accrual rate, the
balance ledger, approver from postings, covers, HR adjustment (L3–L11) · the
produced numbers and the month-end export (O1–O9) · skills with `valid_until`,
Attention, the certification register (C1–C5) · the printed week and the change
list (N1, N3) · the events, and the `zone → who is posted` Context resolver with
its constraint (N5, Z2).

**Proposed OUT, each with the ruling that puts it there.**

| | Why, and it is never *"no time"* |
|---|---|
| the biometric **device source** (A4) | a connector package — and ADR 0128's reservations gate means it **follows** this app, since the Hub can publish an attendance fact only when Workforce exists to own it |
| the **mobile** source (A5) and the staff app (N4) | ADR 0115 §1D is **parked** by owner direction |
| the **payroll connector** (O10) | ADR 0128 §4 rules connector v1 **inbound-only**; an outbound connector is outside the ruled contract, which is why the export is a file |
| **pay itself** (O11) | Finance's, permanently — §4.1 |

### 4.4 · What chapter 01 must change — deliverable 2's spine

1. **`Shift` splits** into `ShiftDefinition` (name · short code · start–end ·
   colour · active) and `ShiftAssignment` — with the argument against the split
   recorded and unresolved: the one-off custom span, and rotas already worked
   when a definition is edited.
2. **`Posting` gains an optional zone** (Z1), on the aggregate that already
   carries the department that gives it meaning.
3. **`DutyAssignment` becomes a span** (M1), and its uniqueness rule becomes an
   overlap constraint (M3).
4. **`LeaveRequest` gains a policy** beside the type list — an accrual rate, not
   an annual allowance (L3, L4) — and the balance becomes a ledger (L6, L7).
5. **A new aggregate for attendance** (A7), source-agnostic from the first
   migration, carrying provenance and in/out **times**.
6. **A new aggregate, or a projection, for the produced numbers** (O1–O7).
7. **`Capability` gains `valid_until`** (C1), which is the whole of §3.6.
8. **The event vocabulary** grows `shift.assigned`, `shift.changed`,
   `leave.approved` beside chapter 01 §4's `user.posted` — with `PKG-Q39`'s
   prerequisite met first.
9. **The approver rule** — chapter 01's *"reporting manager **or** department
   head"* must become one rule with a precedence, because two possible approvers
   with no order is two queues.
10. **Slices are re-cut.** The month-end export cannot precede slices 2 and 3;
    the certification half of slice 4 is a compliance obligation that may not
    want to be last.
11. **The leave-type seed becomes per-property seed templates**, keyed off the
    property's own setting and never a literal — §4.7, ruled.

### 4.4a · Ratified at the study's acceptance, 2026-08-31

Recorded so deliverable 2 starts from them rather than re-arguing them:

* **The zone goes on the `Posting`** (Z1) — completing *"Anita has zone 3"* with
  *"as Housekeeping"* is **rule-as-type**, the house pattern: the incomplete
  fact becomes inexpressible rather than discouraged.
* **`Zone.DepartmentId` does not move this round.** The tension was correctly
  reported and not resolved; **Z1 needs only the staff↔zone link**, and nothing
  else changes.
* **M3's overlap constraint stands** — the guarantee survives *because* the
  study named which database object now carries it. Chapter 01's sentence is
  kept; its implementation is not.
* **Frame 2 is redrawn in deliverable 3, not captioned** — a day cell that
  cannot hold a span is a shape problem, and a note under a drawing does not
  fix a shape.
* **`ZoneTypes` is the standard, not a flourish** — a design assuming one shared
  zone would have diverged from shipped code silently. *Checked in code rather
  than assumed* is the bar for the plan as it was for the study.

### 4.5 · What deliverable 3 must draw

**Frame 2 is the frame the walk changed most** — three independent changes: the
shift chip renders from data and shows a short code (R4, N2), the MOD ribbon
can hold two names in one day column (M1), and the zone needs a place (Z1, Z3).
Redrawing it is not a touch-up.

**Frame 5** cannot stay a one-name-per-day month strip (M1). **Frame 6** gains
the zone. **Frames 3 and 4** must survive a negative balance (L6 × `WF-Q5`), and
frame 4 becomes a queue of **two kinds** — leave requests and swap proposals —
or gains a sibling (S2). **Frame 1 · My Schedule** stops being read-only: S1
gives it its first action, which changes what the frame is.

**And four surfaces do not exist at all:**

```text
property workforce policy   leave types · accrual rate · OT threshold ·
                            holiday calendar        (L3 L4 L5 O8)
reporting                   the month-end sheet and its export   (O9)
print view                  a different artifact from frame 2, not a
                            print stylesheet on it                (N1)
attendance                  posted-versus-present, marking, and
                            lateness against the rota      (A1–A3, A8)
```

**The attendance surface is no longer waiting on an answer.** `WF-Q10`'s
*"visible against the rota"* is its mandate — the duty manager's
posted-versus-present view is in, and what deliverable 3 decides is its shape,
not its existence.

### 4.6 · What this round found outside itself

Three things this application cannot do for itself, each now registered:

| | |
|---|---|
| **`PKG-Q39`** | how a packaged application's event domains reach the Kernel's stream routing. Publishing into an unclaimed stream *looks like it worked* |
| **`SHELL-Q23`** | the print surface — and its **file half left open**, because a file handed to the user is a different capability from a print dialog |
| **`CTX-Q4` applied** | the `zone → who is posted` resolver is **this round's delivery**, with `APPS-Q2`'s constraint attached: allocation input, never a gate |

That is the pattern worth naming: **the first real business application is what
finds the platform's unbuilt edges**, and all three were found by checking the
code rather than by reasoning about it.

### 4.7 · One thing that cuts across every subject — no country in the product

Chapter 01 §3 seeds the leave types as *"the **Indian-hotel** defaults"*. Under
the owner's standing rule — **the product is sold into India *and* the GCC;
`home_country` is a property setting, never a literal in code, labels or
enums** — that seed is a country written into the product.

It is a small change and a real one: `Casual` and `Earned` are
Indian-subcontinent vocabulary, and a Gulf property expects `Annual` and `Sick`.

> **RULED — architect, 2026-08-31, upholding the finding under the standing
> rule:** deliverable 2 replaces the seed with **per-property seed templates
> keyed off the property's own setting — never a literal.**

So the shape is settled before the plan is written: **a template chosen by the
property's setting, not a list chosen by us.** The types stay
property-configured, as chapter 01 already has them; what changes is that the
starting point is selected rather than assumed. It joins §4.4's spine as item 11.

§3.5's reasoning is the same rule the right way round: pay is out of scope
**because** WPS, PF and ESI differ by country — a country-shaped fact used to
justify a boundary, never written into a field.

### 4.8 · What the study leaves unanswered

Honest, because a study that reads as complete when it is not is worse than one
that names its holes:

* **§3.2's residue** — late/absent semantics, the posted-versus-present view,
  and attendance contradicting the rota. §3.5's O3/O6 imply lateness and times
  are recorded; that is an implication and has not been confirmed.
* **§3.4's other half** — staff-initiated swaps. *Covers* are settled (L9, L10);
  whether a staff member can propose an exchange with a colleague, and what
  approves it, was never asked. It is the one remaining thing that could still
  add an aggregate.
* **The ten `§5` questions**, two of which have left for the register.

None of these blocks the feature plan. All of them would block a build.

## 5 · Questions this study raises

Numbers are the architect's to claim. **Two questions this study raised have
left it** and now live in the platform register under their own IDs — they stay
in the table so the trail is readable, marked with where they went.

| From | Question | State |
|---|---|---|
| §3.9 | How a packaged application declares its event subjects | **`PKG-Q39` — claimed 2026-08-31**, evidence carried whole. Two halves: the interim pre-naming to CC; manifest-declared domains materialised at install to the planner, with the package rounds |
| §3.5 · §3.6 · §3.9 | A print surface, and a file handed to the user | **`SHELL-Q23`** — print is the shell's; an application hands it a print-ready view. The row now cites §3.9 and counts the certification register as a third consumer. **The file half is deliberately left open**: the month-end payroll export is a file payroll software takes, and that is a different shell capability |
| §3.1 | What a colour chip says | **CLOSED by §3.9's N2**, ratified: name + colour + a **typed** short code, short code in the cell, legend beneath. Decided by the monochrome-photocopy consequence the question itself named. Kept rather than deleted, so the trail survives |
| §3.1 · §3.3 | Is `Week-off` a shift or a leave type | **open** — one question, two pieces of evidence: the rota popover offers it as a shift while chapter 01 seeds it as a leave type, and the owner's own list named four types without it |
| §3.1 | How an edited shift definition treats rotas already worked | **open** — needed before the aggregate split is written into chapter 01 |
| §3.2 | Does an attendance terminal speak HTTP at all | **open** — ADR 0128 §3's ingress is HTTPS, written for a PMS. **A platform question**, met first by this application |
| §3.2 | Who writes the staff ↔ device mapping, and on which surface | **open** — the table's home is ruled (Master Data, ADR 0063); the writer is not |
| §3.3 | Whose is the property holiday calendar | **open** — recommendation: Core, on the `FiscalYearStartMonth` precedent. Not taken, because it adds a column to a Master Data entity |
| §3.3 | Does working a holiday credit comp-off automatically | **open**, with a direction — §3.5's O5 produces *holidays worked* now and defers the credit |
| §3.5 | Is overtime alerted during the week, or only at month end | **open** — the difference between a report and a control |
| §3.6 | Which precedent does *warn, never forbid* stand on | **reported, not resolved** — the double-booked room is **refused** (`GUEST-Q7`), not warned; `WF-Q5`'s warn-and-allow is the precedent that does support it. Proposed reading: the platform refuses the physically impossible and warns on a judgment. With the architect |

### Ratified since, and folded into the record

* **N2's short code is typed, not derived** — architect, 2026-08-31. A derived
  initial collides the day *Morning* meets *Mid-shift*, and a collision in a
  rota cell is two shifts that look identical on paper.
* **N3's *two facts, not one event*** — correctly deferred to **deliverable 2**:
  whether `shift.changed` carries the cover's identity, or the printed line is
  composed from two correlated events.

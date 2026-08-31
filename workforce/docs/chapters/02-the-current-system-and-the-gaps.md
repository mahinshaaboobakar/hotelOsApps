# 02 · The current system and the gaps — what the owner runs, measured against chapter 01

**Status:** comparison study, opened 2026-08-31. Stream GG, deliverable 1 of the
Workforce round — brief `docs/working/47-the-workforce-round.md` §3.1, **in the
platform repository**.
**This is a live document.** The header, §1 and §2 are complete and are facts.
§3 grows **one subject at a time** as the owner answers, in the brief's §2
order; a subject not yet asked is listed as such rather than left invisible.
**Nothing here is a ruling.** §1 and §2 are citations. §3's rows carry the
owner's answers, attributed and dated. Every scope proposal is labelled a
proposal, and the owner rules v1.

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
| 3.2 | Attendance — check-in/out, late/absent, who is *actually* here versus posted | **asked 2026-08-31** — awaiting the owner |
| 3.3 | Leave — types, balances, requests and approval, who covers | not yet asked |
| 3.4 | Swaps and covers — staff-initiated exchange, the approval chain | not yet asked |
| 3.5 | Overtime and hours — caps, alerts, **the payroll boundary** | not yet asked |
| 3.6 | Skills and certifications — expiry, the compliance view | not yet asked |
| 3.7 | The MOD duty in daily operation | not yet asked |
| 3.8 | Departments and zones in practice — how postings map to Room Care's *"who cleans zone 3 today"* | not yet asked |
| 3.9 | Notifications — how staff actually learn their roster | not yet asked |

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

## 4 · The verdict table

Written when §3 is complete. It is §3's rows, sorted by verdict, and it is what
the feature plan (deliverable 2) is built from.

## 5 · Questions this study raises

Claimed from `WF-Q7` upward by the architect, one at a time with the facts that
ground them.

| From | Question |
|---|---|
| §3.1 | What a colour chip says — a short code alongside the name, or colour plus legend |
| §3.1 | Whether `Week-off` is a shift or a leave type |
| §3.1 | How an edited shift definition treats rotas already worked |

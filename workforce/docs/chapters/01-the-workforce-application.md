# 39 · Workforce — the first business application, designed from the record

**Status:** draft for the owner's redline, 2026-08-28
**Authority:** there is **no chapter for Workforce** — verified by sweeping
`docs/chapters/` (only Chapter 26 mentions Roster/Workforce, and only to state
the ownership boundary). The design authorities are therefore: the ADR trail —
[0051](../decisions/) / 0052 / 0056 (what Roster owns and why), 0063
(capability is Roster's, structure is Core's), 0114 (the granting pipeline and
the GM/department-manager hooks), 0116 §6 (department access derives from
Workforce postings **only**), 0119 (the department canon) — the model's
waiting relations (`department#posted`, `manager`, `supervisor`), the
application-bundle rule (ADR 0051 §"An application is a bundle"), and the
**owner's directions of 2026-08-28**, recorded verbatim in §1.

---

## 1 · The owner's directions, which this page implements

> *"must be simple, user friendly. 1. need calendar view, leave concept —
> what a workforce works in hotel — and in hotel there is a special MOD
> (Manager on Duty) — a front office staff may be MOD for a day, another day
> security — so a user's actual role is security but on that day he also acts
> as MOD."*

Three requirements and one principle:

1. **A calendar view** — the rota is the product's face.
2. **Leave** — requests, approvals, and the calendar showing who is away.
3. **MOD** — a **duty**, not a role change: day-scoped, rotating, layered
   over the person's permanent posting.
4. **Simple.** Every screen below is measured against a duty manager using it
   at 7 a.m., not an HR system's feature list.

## 2 · What Workforce is, in one paragraph

Workforce is the installable application that answers **"who works here, as
what, where, and when"** — the questions ADR 0063 and §Q5 deliberately removed
from Master Data. Staff (the person) stays canonical in Master Data; Workforce
owns the **posting** (department + job role), the **rota** (shifts), **leave**,
and the **duty roster** (MOD). It is the writer for `department#posted` — the
relation every department-scoped grant in the platform has been waiting for.

## 3 · The domain, five aggregates

```text
Posting        staff → department, as a job role
               staff_id · department code (canon, ADR 0119) · job_role ·
               primary flag · effective from/to · reporting manager (staff_id)
               — the exact fields ADR 0052 sent here from StaffPropertyAssignment.
               Department head lives here too (0063's table: Department →
               current head Staff is Roster's)

Shift          one person, one day, one span
               staff_id · date · shift code (M/A/N/custom per property) ·
               start–end · posted department (defaults from the primary posting)

LeaveRequest   staff_id · leave type · date range · note ·
               Draft → Requested → Approved / Declined (by the reporting
               manager or department head) · Cancelled
               Leave types are a property-configured list seeded with the
               Indian-hotel defaults: Casual · Sick · Earned · Comp-off ·
               Week-off. Balances: a simple per-type annual allowance and a
               running count — no accrual engine in v1

DutyAssignment the MOD register: date (or date range) · staff_id · duty type
               (v1 ships exactly one duty type: MOD) · notes.
               One MOD per property per day; assigning a day that has one
               replaces it with a named confirm. The person's posting is
               untouched — security stays security

Capability     skills · languages · shift pattern — the §Q5 remainder,
               slice 2 (recorded now so the boundary is visible; not built
               in slice 1)
```

**No duplicated master data**: `staff_id` references Master Data; departments
are referenced **by canon code** (ADR 0119 — the code is the identity).
Workforce owns its own schema (`workforce`), its own migrations, and never
touches another schema — the application-bundle rule, applied for the first
time to a real business app.

## 4 · Authorization — what Workforce writes, and what it deliberately does not

* **A posting is the writer for `department#posted`.** `PostingService`
  records the posting and appends `user.posted` / `user.posting_ended`
  (user-aggregate events — AUTHZ-Q7's shape) in the same transaction; the
  Kernel's registration consumer materialises
  `department:{id}#posted@user:{uid}`. The day the first posting is saved,
  every dormant department folder grant in My Hotel comes alive — that is the
  point of building this app first.
* **A department-head posting writes `department#manager`**; the model's
  `supervisor`/`manager` hooks stop being unwritten.
* **MOD writes nothing into the graph in v1.** The duty is rostered,
  displayed, and auditable — but what *permissions* a MOD holds is a genuinely
  new authorization question (a time-boxed elevation) that the record has
  never ruled. It goes to the planner as `WF-Q1` rather than being invented
  here. The mechanism is ready when the answer comes: duty start/end are
  lifecycle events, exactly what the granting pipeline consumes.
* Workforce's own permissions (ADR 0007 naming, one per capability, both
  directions in one grant per AUTHZ-Q15): `posting.manage`, `shift.manage`,
  `leave.request` (every member), `leave.approve`, `duty.assign`,
  plus `workforce.read` for the read surface.

## 5 · The application, as the user sees it

Five sections, in the gold design language (ADR 0106; menu service and
scrollbars per 0111). A high-fidelity mockup (40) follows this page's
approval and gates the build — the standing screenshot process.

```text
My Schedule    the signed-in person's month/week: their shifts, their leave,
               a MOD badge on days they hold the duty. Default landing —
               a staff member opens Workforce and sees their own life first

Team Rota      the calendar view — month and week grids per department:
               shifts, leave (struck days), the MOD ribbon across the top of
               each day. Manager tools: assign a shift by clicking a day,
               copy last week, swap two people. Simple beats clever

Leave          two tabs: My requests (request in three fields: type, dates,
               note) · Approvals (the manager's queue — Approve / Decline
               with a note; the request shows the team's calendar for those
               dates so the decision is informed)

Duty Roster    the MOD register: a month strip showing who is MOD each day;
               assign by clicking a day and picking any active staff member —
               from any department, which is the owner's exact scenario

People         postings: each staff member (from Master Data, read-only
               identity) with their posting(s), job role, reporting manager;
               department heads set here. The § that replaces what Core
               Administration's Staff section deliberately no longer shows
```

Where the desktop shows it: the status bar and the property hover card gain
nothing in this round; **My Day-style surfacing (today's MOD shown
shell-side) is `WF-Q2`** — drawn only if ruled, never inferred.

## 6 · Slices

| | Ships | Unblocks |
|---|---|---|
| **1 · Postings + People** | postings, job roles, reporting manager, department head, the `department#posted` writer | department grants platform-wide; the §Q5 hole closes |
| **2 · Rota + Duty** | Team Rota, My Schedule, shifts, the MOD register | the calendar face; the owner's MOD scenario |
| **3 · Leave** | requests, approvals, balances, calendar strike-through | the leave concept |
| **4 · Capability** | skills, languages, shift patterns | assignment intelligence for Jobs and the AI apps |

Slices 2 and 3 may land as one round if the build stream is ahead of schedule;
slice 1 is strictly first because AA's pipeline is its substrate.

## 7 · Open questions — claimed as `WF-Q1`–`WF-Q6`

| ID | Question | Recommendation |
|---|---|---|
| **WF-Q1** | Does MOD carry permissions (a time-boxed elevation), or is it operational display only? **Planner question** — a temporal grant is a new authorization concept | v1: display + audit only; the duty events exist, so a later ruling can attach tuples without schema change |
| **WF-Q2** | Does the shell surface today's MOD (status bar / property card)? | not in v1; module-only until ruled |
| **WF-Q3** | Multiple postings per person (cross-department) in v1? | yes structurally (the schema allows it — ADR 0052's primary flag exists for this), UI keeps one primary + additional |
| **WF-Q4** | Do shift definitions (M/A/N times) live per property or per department? | per property, department overrides later if asked |
| **WF-Q5** | Leave balance enforcement — hard block or warn-and-allow when exhausted? | warn-and-allow with the manager seeing the balance; hotels override reality daily |
| **WF-Q6** | Does Jobs (next app) read postings via Context Service or Workforce's own read RPCs? | Context Service, per the constitution — recorded now so Jobs' design inherits it |

## 8 · What this is deliberately not

No payroll, no attendance/punch clock, no biometric integration, no accrual
engine, no shift-bidding, no compliance rule engine. Each is a future
application or connector — the platform's modularity is the answer to scope,
not a bigger first app.

# 01 · Workforce — the design, as ruled

**Status:** **revision 2, 2026-08-31** — the feature plan, deliverable 2 of the
Workforce round (brief `docs/working/47-the-workforce-round.md` §3.2). Revision 1
was the 2026-08-28 draft written before the owner had been asked anything; this
revision is that draft **measured against the comparison study
([02](02-the-current-system-and-the-gaps.md)) and rewritten to what has since
been ruled.** Renumbered from *39* — a platform working-page number that
predates ADR 0121 putting an application's chapters in its own folder.

**What governs this page.** The owner's directions (§1, unchanged and extended),
the study's 59 rows, and the rulings `WF-Q1`–`WF-Q16` · `APPS-Q1` · `APPS-Q2` ·
`CTX-Q4` · `PKG-Q39` · `SHELL-Q23`, over the ADR trail 0051 / 0052 / 0056 / 0063
(what Workforce owns), 0114 §5 (the granting pipeline and the GM /
department-manager hooks), 0116 §5–§6 (per-user application access; department
access from postings **only**), 0119 (the department canon), 0128 §2–§6 (the
connector contract and the business date) and 0115 §1D (mobile, parked).

**The scope on this page is ruled, not proposed** — `WF-Q11`, owner 2026-08-31,
approving study §4.3 whole. Where this page still records a choice as the
stream's, it says so in place.

---

## 1 · The owner's directions, which this page implements

The founding direction, 2026-08-28, kept verbatim because everything below is
still measured against it:

> *"must be simple, user friendly. 1. need calendar view, leave concept —
> what a workforce works in hotel — and in hotel there is a special MOD
> (Manager on Duty) — a front office staff may be MOD for a day, another day
> security — so a user's actual role is security but on that day he also acts
> as MOD."*

1. **A calendar view** — the rota is the product's face.
2. **Leave** — requests, approvals, and the calendar showing who is away.
3. **MOD** — a **duty**, not a role change, layered over the person's permanent
   posting.
4. **Simple.** Every screen is measured against a duty manager using it at
   7 a.m., not against an HR system's feature list.

**The 2026-08-31 walk added five more**, each recorded where it lands:

> *"Shifts are property-created entities, free-form — name, times, and a colour
> as a first-class attribute."* · *"Each hotel has a different mechanism"* for
> attendance. · *"Casual, sick, earned, comp-off… based on property they have
> leave policy — monthly 2 and yearly holidays."* · *"We can't do per-day,
> because MOD may run 8:00 pm to 8:00 am — it covers two dates."* · Zone
> assignment: *"from Workforce."*

And one correction the walk made to point 3: **MOD is not day-scoped.** It is a
span, and §4 carries the consequence.

## 2 · What Workforce is

Workforce is the installable application that answers **"who works here, as
what, where, and when — and who actually turned up"**.

It is the writer for `department#posted`, the relation every department-scoped
grant in the platform has been waiting for. Until it exists, department
membership is **empty by design** (ADR 0116 §6) — so this is not an application
adding a capability, it is the only thing that can answer a question the
platform has already stopped answering (study §1.2).

**What it owns:** postings (department, job role, **zone**), the shift catalogue
and the rota, leave and its policy, swap proposals, the MOD duty register,
attendance, capability, and the workforce numbers a payroll consumes.

**What it reads and never owns:**

```text
staff identity            Master Data — the person (ADR 0063 §Q5)
department canon          Master Data — 48 codes, the CODE is the identity
                          (ADR 0119); a property activates, never creates
zone (the entity)         Master Data — ADR 0063 kept it; typed by ZoneTypes
property holidays         Core Administration — WF-Q16: the administrator
                          establishes them, exactly as check_in_time
business-date semantics   consumed, never computed — ADR 0128 §6
```

Workforce owns the `workforce` schema, its own migrations, and touches no other
schema — the application-bundle rule (ADR 0051), applied for the first time to a
real business application.

## 3 · The domain

Nine aggregates. Five are revisions of revision 1's; four are new, and each new
one exists because a specific ruling put it there.

### 3.1 · The rota

```text
ShiftDefinition   the property's own catalogue entry — WF-Q4 (per property)
                  name · short code · start–end · colour · kind · active
                  effective-dated: an edit is effective-FORWARD from a
                  manager-chosen date and never rewrites history — WF-Q15

ShiftAssignment   one person, one day
                  staff_id · date · → ShiftDefinition · posted department
                  optional override span, for the one-off custom hours
```

**Why two aggregates.** Revision 1 had one `Shift` carrying *"shift code ·
start–end"* on the assignment — a closed vocabulary of three codes, with the
times copied onto every row. `WF-Q11`'s scope makes the catalogue open and
property-created, and copying a definition's times onto each assignment would be
**a derived projection a client writes**, which the constitution refuses by name:
*a denormalised column that is allowed to disagree with its source is a defect
with a delivery date.*

**`WF-Q15` is what makes the split safe.** The study recorded a real argument
*against* separating them: a property editing *Morning* from 07:00 to 06:30 in
November must not turn last March into a rota of 06:30 starts. Effective-forward
editing answers it — history reads the definition that was in force on the day.

**Three attributes, three jobs** (`WF-Q12`, N2):

| | |
|---|---|
| `name` | what a person reads — *Morning*, *Split — Banquet* |
| `short code` | what fits a rota cell, and **survives a monochrome photocopy**. **Typed by the property, never derived** — a derived initial collides the day *Morning* meets *Mid-shift*, and a collision in a cell is two shifts that look identical on paper |
| `colour` | how a week reads at a glance on screen. First-class, chosen by the property — never inferred from a code |

**`kind` carries `WF-Q12`.** `Week-off` is **a rota marker, not a leave type**:
an *off* entry in the catalogue with no request and no balance. So a definition
is `working` or `off`, and the leave-type list is the owner's four.

**No templates and no rotation engine** — R6/R7, and this is the owner's
refusal, not an omission. Week planning is direct manipulation: click a cell,
copy last week (empty cells only), swap two people.

### 3.2 · People

```text
Posting          staff_id · department code (canon) · job_role ·
                 primary flag · effective from/to · reporting manager ·
                 department-head flag · ZONE (optional)  — WF-Q7
```

**The zone goes on the posting**, not on a standalone staff↔zone link, because
the posting already carries the department that gives the zone its meaning.
*"Anita has zone 3"* is an incomplete fact; *"Anita has zone 3 **as
Housekeeping**"* is complete — putting the zone here makes the incomplete state
**inexpressible** rather than merely discouraged, and it inherits ADR 0052's
effective dating that `WF-Q7`'s durable-until-changed needs.

Optional, because most postings have no zone: a receptionist is posted to Front
Office and to no area.

**Zones are typed, not shared.** `ZoneTypes` already ships `housekeeping ·
maintenance · inventory · security · inspection` (Master Data), so two
departments working one floor hold **two typed zones**, each with its own rooms
and its own postings. Recorded from shipped code rather than chosen here.

**`Zone.DepartmentId` does not move in this round.** ADR 0063's table lists
*Zone → Department* as Workforce's while the column sits in Master Data and the
ADR explicitly left it unruled — and it is load-bearing for authorization. The
tension is reported in study §3.8 and **this plan proposes nothing about it.**

### 3.3 · Leave

```text
LeavePolicy      per property — WF-Q11
                 per type: accrual RATE (the owner's "monthly 2"), applied
                 per period. NOT an annual allowance
LeaveBalance     a ledger: accrual credits · approval debits ·
                 cancellation-of-an-approved credits back ·
                 HR adjustment, recorded and attributed
LeaveRequest     staff_id · type · date range · note
                 Draft → Requested → Approved / Declined · Cancelled
```

**Four types: Casual · Sick · Earned · Comp-off** — the owner's own list.
Week-off left for the shift catalogue (`WF-Q12`).

**The seed is a per-property template keyed off the property's own setting,
never a literal** (§4.7 of the study, ruled). Revision 1 seeded *"the
Indian-hotel defaults"*; the product is sold into India **and** the GCC, where
`Annual` and `Sick` are the expected vocabulary rather than `Casual` and
`Earned`. The types stay property-configured either way — only the starting
point is selected rather than assumed.

**A rate, not an engine.** What is required is one number per type per property
applied monthly. What stays refused, and is named so the first request meets a
decision: carry-forward and its caps, encashment, pro-rata accrual on joining or
leaving, expiry and lapse, tenure slabs, statutory leave-register reporting.

**Comp-off is granted manually in v1** — `WF-Q13`. §3.7's numbers count
*holidays worked*; HR grants the credit through the adjustment tool. Auto-credit
waits for device attendance at least, so the rota and the balance ledger stay
uncoupled in v1.

**Balance may go negative, deliberately.** `WF-Q5` is warn-and-allow, and a
ledger debited on approval means an over-drawn balance is a real state. It must
**read as an approved overdraw, not as a bug** — every balance display survives a
minus sign.

**The approver is the department head, resolved from Workforce's own postings.**
The same posting writes `department#manager` (§4), so the approver and the
authorization hook are one fact that cannot disagree.

> **One rule where revision 1 had an ambiguity** — *ruled 2026-08-31.* It said
> *"the reporting manager **or** department head"*, with no precedence, which is
> two queues. **The rule is: the reporting manager when the posting names one,
> the department head otherwise.** A department head's own leave goes to
> `general_manager`, one of ADR 0114 §5's two Workforce-era hooks. **One rule,
> one queue.**

### 3.4 · Swaps and covers

```text
SwapProposal     WF-Q9 — two people, two shifts
                 proposed → ACCEPTED → approved / declined · cancelled
                 proposed_by · entered_by · accepted_at · decided_by
```

**Staff propose; the colleague accepts; the manager approves** — `WF-Q9`(a). The
accept state is bought deliberately: *a manager's approval must never commit
someone who did not agree.*

**A manager rearranging the rota needs no proposal.** The two are different
operations and both stay:

```text
manager's swap   pick two people, exchange their week. An action — it
                 happens and it is done. Consent-free, because the
                 manager is the authority
staff swap       a request between two people that a third approves.
                 An object with a state, an author, and an outcome
```

**Not `LeaveRequest` reshaped.** One person versus two; one availability versus
**two rota cells changed atomically**; and a second party's consent that leave
has no concept of.

**A proposal is entered two ways, and provenance is mandatory** — `WF-Q9`(b).
`streams.rs` records that **most staff have no login**, so a login-only path
would be silence for most of the workforce while the staff app is parked:

```text
My Schedule            the staff member, where they hold an account
supervisor-on-behalf   for everyone else — entered_by kept, always
```

`entered_by` is not bookkeeping. Without it the record quietly claims a staff
member did something they did not — the same provenance obligation §3.6 carries
for attendance, at its second surface.

**A cover is a manual reassignment against the vacated slot**, surfaced as a gap
on the rota. Not a workflow engine, not a broadcast, not bidding. And a *gap* is
**a mark on a drawn cell — not a computed staffing shortfall**, which would need
a required-headcount demand model that nothing in eight subjects asked for.

### 3.5 · The MOD duty

```text
DutyAssignment   start–end DATETIMES — WF-Q8. Never a date
                 staff_id · duty type (v1 ships exactly one: MOD) ·
                 handover note (free text, optional) · notes
```

> *"We can't do per-day, because MOD may run 8:00 pm to 8:00 am — it covers two
> dates."*

**"Who is MOD right now" is derived, never stored.** No `is_current_mod` flag, no
`current_mod_staff_id` on the property, no nightly job moving a marker — each is
a value that can be wrong while the data beside it is right. It is the same
shape ADR 0128 §6 gave the business date: **the boundary is stored, the current
is computed.**

**"One MOD per property per day" becomes an overlap constraint.** As a date it
was a unique key; as a span it cannot be. The behaviour survives — assigning
over an existing duty replaces it behind a named confirm — but what *detects*
the clash is a different database object, and keeping the sentence while changing
the column would lose the guarantee silently.

**The handover note blocks nothing.** A mandatory handover note is a field people
type *"n/a"* into. **Incident logging is not here** — an incident is work that
needs doing, which is Jobs' definition.

The person's posting is untouched: security stays security.

### 3.6 · Attendance

```text
AttendanceRecord  staff_id · business-date · IN and OUT times ·
                  the shift it answers to (nullable) ·
                  source     device | mobile | manual
                  provenance which device, which connector, which person
                             recorded it, and when it arrived
```

**Revision 1 refused this outright** — *"no attendance / punch clock, no
biometric integration"* — written before the owner was asked. Every property
tracks attendance; only the mechanism varies. The refusal splits: **the fact
belongs here; the punch-clock implementation does not**, because a device is a
connector.

**Source-agnostic from the first migration**, though only one source is v1 code.
A property starting on manual marking and later installing a face reader must not
need a schema change — and provenance is **the difference between evidence and an
assertion**: an auditor asking *"how do we know he was here"* must get a
different answer for a fingerprint than for a supervisor's click.

**Manual is the v1 floor and it captures times, not a tick** — `WF-Q10` requires
in/out times, and §3.7's hours depend on them.

**Lateness is first-class and derived.** `WF-Q10`: *keep late information*, and
surface it against the rota. **There is no late-minutes column** — the posted
start and the actual clock-in are the facts; *"twenty minutes late"* is
arithmetic over them, and a stored value can disagree with the two times beside
it. Store the boundary facts, compute the judgment.

**Attendance may contradict the rota, and both facts are kept** — present on an
day they hold no shift, absent on a day they do. The discrepancy is **surfaced, never
silently reconciled.**

**The device source is out of v1 as code** and enters as a `kind: connector`
package (ADR 0128 §2) with connector-declared authentication (§3), the Hub owning
dedupe so an overnight-offline terminal's replay is not two punches (§5), and
inbound-only (§4) — which a punch is. The staff↔device mapping is the
external-identifier pattern (ADR 0016 as amended by `CONN-Q8`): `staff` ·
`machine_user_id` · the device's id, property-scoped. **Workforce consumes that
mapping and does not own it.**

**The mobile source is out of v1** — ADR 0115 §1D, parked.

### 3.7 · The numbers

```text
WorkforcePeriod   produced per staff, per business-day and per month
                  days posted · days present · late count ·
                  leave taken by type · holidays worked ·
                  hours worked · OVERTIME hours
```

> **Workforce produces the numbers; it never calculates pay.**

Pay is a legal and compliance domain differing by country — WPS, PF, ESI — and by
hotel: allowances, deductions, contracts. **Building it wrong is a salary
dispute.** It is Finance's, or the hotel's existing payroll, and what all of them
need from us is identical: correct numbers.

This is `GUEST-Q6`'s boundary one application over — GuestOps carries the stay's
terms and never settles the guest; Workforce carries the hours and never pays the
person.

**Overtime warns at planning time** — `WF-Q14`: planned hours against the
property's threshold, **warn-never-block**, when the rota is being built rather
than after the fact. **Actuals arrive at month-end.** Live mid-week alerting is
deferred **while attendance is manual** — a deferral that names its own
unblocking condition.

**Month-end export: a file, in v1.** Payroll software takes it. A payroll
*connector* would be **outbound**, which ADR 0128 §4 rules out of the v1
connector contract entirely — so the file is not a stopgap, it is the only thing
the ruled contract permits.

`WorkforcePeriod` draws on the rota, attendance, leave and the holiday calendar,
so it **cannot precede** the slices that produce them (§7).

### 3.8 · Capability

```text
Capability        skills · languages · shift patterns
                  each skill: optional VALID_UNTIL
```

**One optional field carries both concepts** — no date is an *ability*
(*"speaks Arabic"*); a date makes it a *certification* (*"fire warden — valid
until 12 Mar 2027"*). **The date is the discriminator**, so the inconsistent
state — an ability with an expiry, a certification without one — cannot be
written. No `kind` field for a reader to branch on and for the data to
contradict.

**60 / 30 / 7 days before expiry** the skill appears on the department head's
and HR's **Attention list** — a surface inside Workforce, because `WF-Q2` leaves
shell-side surfacing unruled. The audience resolves from postings, exactly as
the leave approver does.

**After expiry it reads `EXPIRED`** — on the person, on the People screen, **and
in the answers Workforce gives**. That last clause reaches another application:
if the Context read-view returns a bare qualified-list, the first consumer to
care about expiry re-implements the rule and *"we didn't know"* becomes true at
one remove. **The expiry state travels in the Context answer.**

**It blocks nothing** (§5). And one report: the property's **certification
register** — every dated skill, holder, expiry — the sheet a safety inspector
asks for.

## 4 · Authorization

* **A posting is the writer for `department#posted`.** `PostingService` records
  the posting and appends its announcement in the **same transaction** —
  **against the `posting` aggregate**, per `AUTHZ-Q20` (2026-08-31) on
  `HUB-Q4`'s *announce against what you own*. Revision 2 of this page said the
  `user` aggregate, following the pre-question shape; that is superseded, and
  the reason is that a posting carries its own version sequence while this
  application holds no user row whose version it could legally increment; the Kernel's registration consumer materialises
  `department:{id}#posted@user:{uid}`. **No service writes a tuple** (ADR 0061).
  The day the first posting is saved, every dormant department folder grant in
  My Hotel comes alive.
* **A department-head posting writes `department#manager`** — ADR 0114 §5's
  hook stops being unwritten.
* **MOD writes nothing into the graph** — `WF-Q1`, ruled: a duty assignment, not
  an authorization role. Display, roster, visibility where the person's existing
  permissions already allow it, audit and duty status. Any future elevation needs
  a new authorization ADR; the duty's lifecycle events are the hook, and they now
  carry spans rather than dates.
* **Permissions** (ADR 0007 naming, one per capability):

```text
posting.assign · shift.define · duty.assign
leave.request (every member) · leave.approve
swap.propose (every member) · swap.approve
attendance.record · attendance.amend
roster.configure   the property's leave policy, OT threshold, catalogue
capability.record  skills and certifications
roster.read        the read surface
```

**Five of these were renamed on 2026-09-01**, and the reason is worth keeping
because it is a rule the next application will meet: the permission registry
**bans `write`, `manage` and `edit`** — they say nothing about blast radius, and
they are what forced service prefixes in the first place — and a permission
**never names the application** that implements it.

`roster` is this application's resource noun, and it was available because
**`APPS-Q3` renamed the *application* away from Roster while keeping the
function vocabulary**. Renaming the app is what freed the word — the precise
equivalent of GuestOps' `desk`.

## 5 · Where this application refuses, and where it warns

`WF-Q16` ruled the principle the whole application now follows:

> **The platform refuses the physically impossible or self-contradicting, and
> warns on a judgment.**

`GUEST-Q7`'s refused double-booking and `WF-Q5`'s warned balance were never in
tension — they sit on opposite sides of that line. Applied here, once, so no
screen has to decide it again:

| | |
|---|---|
| **REFUSED** | two MOD duties overlapping for one property — *"who is MOD now"* would have two answers · one person assigned two overlapping shifts · a swap whose two shifts do not both exist |
| **WARNED, and named** | an exhausted leave balance (`WF-Q5`) · planned hours over the OT threshold (`WF-Q14`) · an expired certification on assignment (§3.8) · attendance contradicting the rota (`WF-Q10`) · a leave-vacated slot with no cover |

A warning **names the thing** and lets a person decide. Compliance is the
hotel's judgment; our job is that nobody can say *"we didn't know"*.

## 6 · Events, and the Context resolver

**The vocabulary**, published in the caller's transaction (`events.append`,
never an RPC):

```text
user.posted · user.posting_ended          routed today (property.*.user.>)
shift.assigned · shift.changed
shift.covered                             one event naming BOTH people
shift.swap_proposed · .accepted · .approved · .declined
leave.approved
duty.assigned · duty.ended
attendance.recorded
```

**`shift.covered` is one event, not two correlated ones** — *ruled 2026-08-31,
closing the study's deferred N3.* *"Tue: Rajan N→M, covered by Anita"* is a
causal link, and inferring it from two independent assignment events would be
reconstructing intent from coincidence. The business fact is *Anita covered
Rajan's night shift*, so the event says that — **ADR 0016's
business-facts-not-process-events rule**, applied.

**The week's change list is the event stream rendered**, not a second stored
list — a record allowed to disagree with its source is the same defect one level
up.

> ### How these reach the platform — a ruled mechanism, and one correction to its example
>
> **`PKG-Q39` is ruled** (planner, 2026-08-31, amending
> [ADR 0092](../../../../HosPilotOS/docs/decisions/0092-the-application-package-contract.md)):
>
> > **An application's event domains are manifest-declared and materialised at
> > installation** — *package declares; Kernel materialises; Event Router
> > routes.* The declaration is **input to platform registration, never access
> > to routing**: the Kernel stays owner of the event topology — validity,
> > conflicts, stream mapping, retention, permissions, lifecycle — and a package
> > never touches NATS or the stream specification directly.
>
> So Workforce's `manifest.yaml` declares the domains it owns, and nothing in
> this application reaches routing. **Implementation lands with the package
> rounds; the Kernel's interim pre-named set in `streams.rs` stands until
> then** — which is what gate 3 in §10 is about.
>
> **The declaration is four domains: `shift · leave · duty · attendance`.**
> The ruling's own parenthetical named three (*"e.g. Workforce: `shift`,
> `leave`, `duty`"*) — written before this plan existed, and **an illustrative
> list does not override a design finding** (CLAUDE.md's precedence rule).
> `attendance` cannot ride `shift.>`, because `WF-Q10` keeps an attendance
> record that answers **no shift at all** — present on a day with no shift — and
> filing it under `shift` would misname the fact where ADR 0006 routes by
> meaning. Reported rather than silently declared, and **ruled in this
> application's favour**.
>
> **Landed 2026-08-31, and verified here rather than assumed** — Stream CC,
> register `fca1b96`. `services/kernel/crates/kernel/src/events/streams.rs:122-128`
> now routes `property.*.shift.>`, `property.*.leave.>`, `property.*.duty.>`
> **and `property.*.attendance.>`** into `OPERATIONAL`, with the no-shift-day
> reasoning written at the subject and **swaps recorded beside it as the
> counter-example** — `shift.swap_*` stays inside `shift.>` because a swap is a
> fact about a shift. `tests/jetstream.rs:336` carries `attendance.recorded`.
> **So this application's build-time dependency on routing is met.**
>
> **Swaps deliberately add no fifth domain.** They sit inside `shift.>` as
> `shift.swap_*`, because a swap proposal is about shifts — so the vocabulary
> grows without a new routing dependency.
>
> Why any of it matters: an unrouted subject is **acked, matches nothing, and
> dead-letters silently**, and `streams.rs:85-90` records that having already
> happened once.

**The Context resolver is this round's delivery** — `CTX-Q4`: a new resolver
arrives as **one delivery by the contributing domain's round**, the read view,
the RPC, and its constraint. Workforce delivers **zone → who is posted**, with:

> **Allocation input, never a gate.** The absence of a zone posting must not
> block a Room Care task. A room still gets cleaned when nobody is posted to its
> zone — `APPS-Q2`, *an absent dependency loses its capability, never the flow.*

Consumers read through Context and never Workforce's tables or RPCs — `WF-Q6`
for Jobs, `CORE-Q15` for Core Administration's posted count.

## 7 · Slices, re-cut

Revision 1's four slices did not have attendance, swaps, the numbers or a policy
surface in them. Re-cut against the ruled scope:

| | Ships | Why here |
|---|---|---|
| **1 · Postings + People** | postings with zone, job roles, reporting manager, department head, the `department#posted` writer, the Context resolver | **Strictly first.** It closes the platform-wide hole, and the granting pipeline is its substrate |
| **2 · Capability & compliance** | skills and languages, `valid_until`, the Attention list, the certification register | **Moved forward from last — ruled 2026-08-31.** It needs only slice 1: the Attention audience resolves from postings, and nothing else in it depends on a rota, on leave or on attendance |
| **3 · Rota + Duty** | the shift catalogue (effective-dated), the rota, copy-last-week, manager swap, the MOD register as spans, **the printed week**, the OT planning warning | The calendar face and the owner's MOD scenario. The print view ships **with** the rota, not after it — a rota staff cannot read is not delivered |
| **4 · Leave + Swaps** | policy, the balance ledger, requests, approvals, HR adjustment, staff swap proposals | Swaps join leave because they share the approval surface and the same approver resolution |
| **5 · Attendance** | manual marking with in/out times, posted-versus-present, lateness | Before the numbers, because it produces three of them |
| **6 · The numbers** | `WorkforcePeriod`, month-end actuals, the export | Draws on 3, 4 and 5 — it cannot precede them |
| **7 · Assignment intelligence** | shift patterns, and the capability read-view Jobs consumes through Context | Waits for a consumer, which is what it is *for* |

**Why certification moved.** The study surfaced it as *recorded, not taken*: a
fire-warden card expires whether or not Jobs has shipped, so queuing a
**compliance obligation** behind four slices of convenience was the wrong order.
Ruled forward, and placed **where the dependency graph allows** — which is
immediately after postings, because the Attention list needs an audience and
nothing else in the slice needs anything.

**What stayed behind, and why it is not the same thing.** Revision 1's single
*Capability* slice bundled two unrelated purposes. The compliance half stands
alone; the **assignment** half exists to answer *"who can do X"* for another
application, so it ships when that application can ask. `WF-Q6` already routes
that answer through Context, and §3.8's expiry state travels in it.

**And one item worth re-examining rather than scheduling.** With the rotation
engine refused (R7, the owner's own refusal), **`shift pattern` has no consumer
in v1** — it is inherited from ADR 0063 §Q5's remainder list rather than asked
for. It sits in slice 7, and what it is *for* should be established before it is
built.

**The OT planning warning moved with the rota**, not with the numbers: it works
on *planned* hours (`WF-Q14`), so it needs slice 3 and nothing later. Only the
actuals wait for attendance.

## 8 · What this is deliberately not

Each refusal now carries the reason it survived being tested against the owner's
own practice:

| | |
|---|---|
| **No pay calculation** | legal, country-specific, and a salary dispute if wrong. Finance's — `GUEST-Q6`'s boundary |
| **No punch-clock or biometric implementation** | the fact is ours, the device is a **connector** — ADR 0128 |
| **No accrual engine** | a rate is required; carry-forward, encashment, pro-rata, expiry and slabs are not |
| **No shift-bidding, no coverage-requirement model** | nothing in eight subjects asked for required headcount, and it is the doorway to both |
| **No rotation-pattern engine** | the owner's own refusal — *create the shifts you use, paint the calendar with them* |
| **No incident log** | Jobs' — *what needs doing* |
| **No mobile client** | ADR 0115 §1D, parked. The **events are published as if it existed**, so it subscribes with zero changes here |
| **No compliance rule engine** | we warn and name; the hotel judges — §5 |

## 9 · The questions

| | |
|---|---|
| **Ruled and built into this page** | `WF-Q1` MOD is a duty, no tuples · `WF-Q4` shifts per property · `WF-Q5` warn-and-allow · `WF-Q6` Jobs reads via Context · `WF-Q7` zone from Workforce · `WF-Q8` MOD is a span · `WF-Q9` staff propose, colleague accepts, manager approves; both entry paths with provenance · `WF-Q10` keep late information · `WF-Q11` scope approved · `WF-Q12` Week-off is a rota marker · `WF-Q13` comp-off manual · `WF-Q14` OT warns at planning · `WF-Q15` effective-forward editing · `WF-Q16` holidays are Core's, and refuse-versus-warn |
| **Standing, unchanged** | `WF-Q2` no shell-side MOD surfacing in v1 · `WF-Q3` multiple postings structurally, one primary in the UI |
| **Open, and neither reachable from here** | does an attendance terminal speak HTTP at all (ADR 0128 §3) · who writes the staff ↔ device mapping. **Both device-shaped**, both for the connector round |
| **Ruled since, mechanism only** | **`PKG-Q39`** — event domains are manifest-declared and materialised at install (ADR 0092 amended). Implementation lands with the package rounds; the interim pre-named set stands until then |
| **Open in the platform register** | `SHELL-Q23` — the print surface, and the file half left open |

## 10 · What gates the build

Not this page's approval alone:

1. **The gold mockup revised and owner-verified** — deliverable 3. Frames 1, 2,
   3, 4, 5 and 6 all change, and four surfaces do not exist yet (policy,
   reporting, print view, attendance).
2. **`APPS-Q1`'s two platform prerequisites** — the registry-driven shell, and
   the application-caller authorization round. They bind Workforce as they bind
   every application.
3. ~~**Event routing in place before Workforce publishes anything.**~~
   **MET, 2026-08-31.** All four domains are routed in the Kernel's interim
   pre-named set (§6, verified in `streams.rs:122-128`). The ruled *mechanism* —
   manifest-declared, materialised at install (ADR 0092 as amended) — still
   lands with the package rounds, and Workforce's `manifest.yaml` will declare
   the same four; but nothing this application publishes will dead-letter in the
   meantime, which was the actual gate.
4. **`SHELL-Q23`** for the printed week — the shell owns the print dialog; this
   application hands it a print-ready view and writes no printer code.

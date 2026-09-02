# 01 · The Jobs reference survey

> **Stream HH, round 1 — the survey only. Nothing here is built, and nothing
> here is a decision.** Phase 2 (design page, mockups) begins after the owner
> has read this.

---

## 0 · What this is, and the rules it was written under

The owner supplied a reference Java project and said the architecture is bad
and features are missing. This page is the reading of it: what it does, what
it stores, how a work order actually moves through it, what it talks to, and
where it is wrong — followed by the R-numbered requirements those findings
produce for **our** Jobs application.

The method is the Oracle connector round's, which worked:

```text
survey  →  requirements  →  design  →  owner gate  →  build
   ▲
 you are here
```

### The vocabulary rule, applied

`APPS-Q3` (owner ruling, 2026-08-31, register row 147) makes the current
application name the name in code: **Jobs**, never WorkOrder — domain `job`,
schema `jobs`, `job_id`. The same row carries the exception this page lives
inside:

> **A vendor's transcribed schema keeps the vendor's names.** A connector
> transcribes the source under the source's spellings and maps onto the
> platform's names at the boundary — renaming the transcription would make
> the adapter lie about what the vendor sends.

So **§1–§5 use the reference's own spellings throughout** — `WorkOrder`,
`work_orders`, `companyId`, `siteId`, `WorkOrderStatus`, `WO_NOT_CLOSED`.
§6 is the boundary: from there on the names are ours. The map, stated once:

```text
reference             ours
─────────────────────────────────────────────────────────
WorkOrder             Job                    (APPS-Q3)
companyId             organization_id        (ADR 0060: HotelOS organizations.id)
siteId                property_id            (ADR 0060: HotelOS properties.id)
facilityId            — no counterpart; see F31
location (string)     masterdata.room_id / masterdata.asset_id
department (string)   the department canon code (ADR 0119)
assignedToId (string) masterdata.staff_id, resolved through Workforce postings
```

### Where the citations point

Every `file:line` in §1–§5 is under
`Documents\HotelOs-References\work-order-management\src\main\`, read-only.
Nothing in that tree was modified. Line numbers are as read on 2026-09-02.

### What was read

The whole tree, not only `application/`. 382 Java files, ~15,000 lines, plus
`pom.xml`, `application.properties`, `application-local.properties`,
`quartz.properties` and every file under `resources/`. The Oracle round's
lesson held exactly: **the worst findings are in `co/instio/` (the shared
layer), not in `co/instio/application/`** — the security chains, the
hard-coded service credential, the file-serving endpoints, the date parser
and the scheduler all live outside the business package, and every one of
them produced a finding.

```text
co/instio/                   the shared/platform half — 128 files
  security/ filters/         authentication, and where it does not happen
  feign/                     8 outbound HTTP clients + interceptors
  services/                  scheduler, files, uploads, users, customers, urls
  models/ dto/ utils/        payloads, the date parser, the cron builder
  jobs/                      Quartz entry point + two dead handlers
  exceptions/ enums/         the error vocabulary
co/instio/application/       the business half — 251 files
  controllers/ services/     23 controllers, 22 services, 9 background services
  entity/{jpa,mongo}/        17 relational + 15 document entities
  port/{api,spi}/            hexagonal ports — 23 driving, 27 driven
  adapters/ repositories/    the implementations of those ports
  mapper/ dto/ misc/         MapStruct mappers, 42 DTOs, 3 filters
```

### The governing-document check, and one gap to report

`CLAUDE.md` (HosPilotOS) requires the chapter for what is being built to be
read before the ADRs, and requires `ls docs/chapters/` first because
**filenames repeat numbers and the title is the key**.

> **There is no Jobs / Job Order chapter.** `ls docs/chapters/` in
> `HosPilotOS` returns 59 files and none of them is about this application;
> the string "Job Order" appears in Chapter 21 (data platform), Chapter 26
> (canonical data model), Chapter 12 (events) and ADRs 0051 / 0070 only as a
> **named example of an installable application** — never as a design. The
> peer applications each carry their own chapter set under
> `HotelOsApps/<app>/docs/chapters/`; Jobs has none, and this file is the
> first.
>
> So this survey is written **from the platform constitution and the ADRs
> named below, not from a chapter**, and the missing chapter is reported
> rather than invented. It is the first question in §7.

Read and relied on, with what each contributes:

| | |
|---|---|
| `CLAUDE.md` (HosPilotOS), whole | the bundle rule, no-duplicated-master-data, context-over-joins, events-in-the-caller's-transaction, no-native-queries, the layering, the file rules |
| `docs/decisions/questions.md` | the register — read before asking; **no `JOBS-Q` row exists yet**, so every question in §7 is unminted |
| **ADR 0116 §5** + its two addenda | per-user application gating; *unavailable is not denied*; **absent is not blocking** |
| **`EVT-Q4`** (register, row 149) | the .NET consumer host — **CLOSED**; GuestOps already consumes `job.created` through the real route, one durable per stream, `DeliverPolicy: New`, ack-after-commit, idempotent on `event_id` |
| **`AUTHZ-Q25`** (register, row 884) | manifest-declared grant kinds; the grantable-relations registry; install-time consent; *refusing at install what cannot be delivered* |
| `AUTHZ-Q20` (register, row 889) | the announcement contract an application owes when it owns a relationship rather than an end of it |
| ADRs 0051 · 0052 · 0056 · 0063 | field ownership — *what IS it* / *what is HAPPENING* / *what is the RELATIONSHIP* |
| ADR 0119 · ADR 0116 §4 | the department canon; departments are activated, never created per property |
| ADR 0062 | one lifecycle: `active` + `deleted_at`, Deactivate/Reactivate, never Archive |
| `pms-oracle/docs/chapters/01`–`03` | the worked example this page is shaped after |

### One citation correction, reported not resolved

The brief describes ADR 0116 §5 as *"a reply between applications is an event
with a correlation id — Room Care learns its job's id from `job.created`,
never by calling Jobs"*. That sentence is **`CLAUDE.md` §"6. Event-driven
communication"**, which cites ADR 0116 §5 as its authority. ADR 0116 §5 and
its two addenda rule *per-user gating*, *unavailable is not denied* and
*absent is not blocking* — **the correlation id is not in the ADR**. The rule
is real and this survey follows it; the citation for it is the constitution,
and where the correlation-id half is frozen is the second question in §7.

---

## 1 · The feature inventory — everything it does, however badly

23 REST controllers, ~85 endpoints, 11 RabbitMQ consumers, 4 outbound queue
families, 3 datastores. Grouped by what a hotel would call it.

### 1.1 · The work order itself

| Capability | Where | State |
|---|---|---|
| Create a work order | `GenericWOController:29` → `WorkOrderServiceImpl.insert:54` | live |
| Search / list (paged + unpaged) | `WorkOrderStaffController:41,48` | live |
| Read one by id | `WorkOrderStaffController:54` | live, **unscoped** — F3 |
| Update body fields | `WorkOrderStaffController.put:66` | live |
| The twelve-verb PATCH | `WorkOrderStaffController.patch:76` | live — F13 |
| Per-company running number | `WoSequenceRepository:22` | live; a second, dead implementation at `WorkOrderRepository:53` |
| Soft lifecycle (`EntityStatus`) | `AbstractTransactionalEntity` | declared; **no endpoint sets it** |

The twelve verbs behind one `PATCH {id}` with `kind` as discriminator:
`CAPTURE`, `EXECUTE`, `PRIORITY`, `CHECKLIST`, `INSPECTION`, `OPEN`,
`CLOSED`, `DUEDATE`, `STARTTIME`, `WAIT`, `BEGIN`, `GUEST_ACKNOWLEDGE` —
plus `CHECK`, `ACCEPT`, `START_TIMER`, `END_TIMER` on a *different*
controller (`time-log`). Sixteen operations, two envelopes, no state machine.

### 1.2 · Assignment, acceptance and execution

| Capability | Where |
|---|---|
| Assign to `USER` / `TEAM` / `DEVICE` | `WorkOrderServiceImpl.updateAssignment:127` |
| Self-capture (take an unassigned job) | `.capture:145` |
| Accept | `WorkOrderUserTimeLogServiceImpl.accept:34` |
| Nominate an executor (`executedById`) distinct from the assignee | `.updateExecute:163` |
| Default assignment from service configuration | `WorkOrderBackGroundService.setDefaultAssignmentFor:552` |
| Device slot assignment (a device is a shift-bound pseudo-user) | `WODeviceSlotAssignmentServiceImpl` |
| Followers, implicit and explicit | `WorkOrderFollowerServiceImpl`; every mutation adds its actor |

### 1.3 · Time, SLA and the clock

| Capability | Where |
|---|---|
| Start-by time and due date, both rounded to the minute | `WorkOrderMapper.updateStartTime:255` |
| SLA duration in minutes, defaulted per service | `setDefaultSlaFor:583` |
| Per-user timers (start / end, reasons, minutes) | `WorkOrderTimeLog` |
| Automatic timer termination on reassign / re-execute / close | `WorkOrderTimeLogBackgroundService:26` |
| WAIT / BEGIN pause with SLA-clock credit | `updateWaiting:262`, `WorkOrderSummary.totalSlaWaitingMillis` |
| Nine derived durations | `WorkOrderSummary` — response ×3, resolution, labour, waiting, closed, accepted, started |
| A permanent backfill sweeper for those durations | `WorkOrderTimeLogBackgroundService.updateOldPayloadsForTimeSummary` |

### 1.4 · Escalation and reminders — four designs, one alive

The largest subsystem and the most decayed. **Four separate escalation
designs exist in the tree; exactly one executes.**

| # | Design | Configuration | Status |
|---|---|---|---|
| 1 | `DefaultEscConfig` chain — 4 identifiers, 5 named levels, Quartz chain held in job data | **a JSON file on local disk**, per company or site | **the only live one** |
| 2 | `WOPreference` actions — percentage-of-SLA intervals, min/max, `dropIfOutOfBounds`, 6 trigger points | MongoDB, with a full REST surface | **dead** — its only caller is commented out (`EventExecutorServiceImpl:231`) |
| 3 | `WORules` conditions/actions engine — field/operation/value, join-by, default actions | MongoDB, REST surface at `wo/rules` | **dead** — `getActionsFromRule:158` has no caller |
| 4 | `EscalationHandler` — a fully documented 10-rule matrix | none | **dead** — never instantiated; the design exists only in its Javadoc |

The live one fires on four conditions — `wo-not-assigned`,
`wo-not-accepted`, `wo-accept-not-start`, `wo-not-close` — over five levels
named `primary_` … `quinary_escalation`, targeting `USER` / `POSITION` /
`SELF`, over `SMS` / `EMAIL` / `NOTIFICATION` / `WHATSAPP`.

Separately live: **progress reminders** at 50 / 75 / 100 / 125 / 150 % of SLA
(`defaultInitsForWorkOrderStatus`, the `IN_PROGRESS` branch), and **waiting
reminders** at −5 min / due / +5 min / +15 min, each escalating
assignee → supervisor → HOD.

And a fifth mechanism that stores but never fires: `WorkOrderReminder` —
create, persist, schedule … into `runReminderScheduledTask`, whose body is
**empty** (`EventExecutorServiceImpl:928`).

### 1.5 · Communication

Four channels are modelled, configured, preference-gated and payload-built.
**Two are disconnected.** `Q_SEND_SMS` and `Q_SEND_WHATSAPP` have live
consumers and **zero live publishers** — all six publish sites are commented
out.

| Channel | Preference model | Payload built | Actually published |
|---|---|---|---|
| Email | yes | yes | **yes** |
| In-app notification | yes | yes | **yes** |
| SMS | yes (per-user, per-company, per-customer, templates, senders) | yes | **no** |
| WhatsApp | yes (+ a 6-provider enum, a 15-state delivery enum) | yes | **no** |

Four preference layers resolve a single send: company → site → department →
user, plus a separate customer (guest) preference tree.

### 1.6 · Guest-facing surfaces

Guest-raised work orders (`addedByStaff=false`), guest acknowledgement, a
1–5 star rating with comment and a manager "action" reply, free-text
feedback, a public tracking timeline with configurable stages, a low-rating
alert to on-duty hosts, and short-URL links into a hosted web UI.

### 1.7 · Scheduled and preventive work

`WorkOrderSchedule` — day-of-week **or** month + day-of-month + time +
timezone, compiled to a Quartz cron, creating a work order on each fire with
a fixed description and `source=SCHEDULED`. This is the PPM leg.

### 1.8 · Checklists and inspections

`checklistId` / `inspectionId` on the work order, a `WorkOrderCIX` link
table, and an outbound "create inspection instance" call to the CIX service
— **commented out** (`defaultInitsForWorkOrderChecklist:684`), so a
checklist is attached and no inspection is ever created.

### 1.9 · Configuration surfaces (`/wo/preference`)

Services (keyword → department, assignee, priority, SLA, icon, track mode),
locations, assignment rules, communication preferences, department
preferences, customer preferences, track preferences (stages), escalation
level labels, devices and device slots, and a `WOAttributes` tree — a
hierarchical service/item catalogue.

### 1.10 · Relationships and extras

Work-order affiliation (parent/child; closing the parent cascade-closes
children), to-do notes, an activity feed with mentions and attachments, an
image gallery aggregated from activity attachments, and a "team management"
surface (§4.6) that raises user-admin requests as work orders.

---

## 2 · The data model, transcribed under its own names

Three datastores, no migrations, schema by inference.

```text
MySQL  `work-order-management`   17 JPA entities   ddl-auto=update
MySQL  `qrtz`                    Quartz's own 11 tables
Mongo  `work-order`              15 documents      database name hard-coded in Java
```

### 2.1 · `work_orders` — the aggregate

`WorkOrder extends AbstractTransactionalEntity extends AbstractBaseEntity`:
`id` (auto-increment `Long`), `version`, `status` (`EntityStatus`),
`createdOn`, `updatedOn`.

```text
identity        companyWOId (per-company running number)
                companyId · siteId · facilityId          all String
classification  workOrderType · service · category · location   all free String
                priority Integer 1..10 · slaDuration Integer (minutes)
description     comment (500) · description (1000) · images (element collection)
provenance      source · referenceId · guestReferenceId
                addedByStaff Boolean · initiatedById String
routing         departmentId + department   (id AND display name, both stored)
                assigneeType {USER,TEAM,DEVICE} · assignedToId · executedById
time            startTime + startTimeInMillis · dueDate + dueDateMillis   (each twice)
work            checklistId · inspectionId
state           workOrderStatus {NEW,OPEN,ON_HOLD,IN_PROGRESS,ESCALATED,WAITING,CLOSED,REMOVED}
                accepted · started · waiting · guestAcknowledged · reopened   (5 booleans)
                reopenCount Integer
social          followers Set<String>
```

**Twenty-seven indexes**, seven of them composite, every composite one led by
`companyId` — the shape of a system that discovered its tenancy predicate
after the fact.

### 2.2 · The satellites (relational)

| Table | Holds | Note |
|---|---|---|
| `work_order_activity` | the audit feed — `activity` (28-value enum), `fromValue`/`toValue`, `message`, `mentions`, `attachments` | the only history; free-text from/to |
| `work_order_summary` | 15 derived durations, `@Id` shared with the work order, no FK | a read model with no writer of record |
| `work_order_escalations` | one row per (work order, identifier, level, user) | doubles as the deduplication ledger |
| `work_order_time_log` | per-user timers, start/end reasons, `timeInMinutes` | |
| `work_order_track` | the guest-visible timeline — `activity`, `trackingStatus`, `trackId`, `infos` | |
| `work_order_reminders` | user reminders + a `Map<String,String>` data bag | never fires (§1.4) |
| `work_order_rating` | 1–5, comment, plus a manager `action`/`actionId` | |
| `work_order_feedback` | a *second* rating+comment pair | overlaps `work_order_rating` entirely |
| `work_order_follower` | explicit follows | duplicates `work_orders.followers` |
| `work_order_user_todo` | private notes | |
| `tbl_work_order_affiliation` | parent-child links | the only `tbl_`-prefixed table |
| `work_order_cix` | checklist / inspection links | |
| `work_order_contact_info` · `work_order_customer_info` | **two identical tables** — name, email, phone, phone2, embedded address | F26 |
| `wo_sequence` | `company_id` to `last_wo_id` | |
| `wo_device` | a `@Table` with **no `@Entity`** — not a JPA entity at all | F27 |

### 2.3 · The documents (MongoDB)

`WOServicePreference` (service to keywords, department, assignee, priority,
SLA, icon, track mode, per-keyword assignee map) · `WOLocationPreference` ·
`WOAssignmentPreference` · `WOCommunicationPreference` (8 event types x 4
channels x template/sender/content) · `WOCustomerPreference` ·
`WODepartmentPreference` · `WOUserPreference` (**including a copy of the
user's name, email and phone** — F19) · `WOPreference` (dead engine 2) ·
`WORules` (dead engine 3) · `WOTrackPreference` (stages, auto-track map) ·
`WorkOrderEscalationLevels` (labels) · `WorkOrderSchedule` · `WODevice` ·
`WOAttributes` (a self-referencing tree).

### 2.4 · What the model says about itself

* **Identity is a `Long` auto-increment, and it is the public identifier** —
  `/staff/{id}`, `/rating/{workOrderId}`, `/tracking/{id}`. Enumerable, and
  global across every tenant.
* **Every foreign identity is an untyped `String`** — `companyId`, `siteId`,
  `assignedToId`, `departmentId`, `initiatedById`, `checklistId`. Nothing
  distinguishes a user id from a team id from a device id except
  `assigneeType`.
* **The string minus-one is the system actor**, written into `initiatedById`
  at eight sites, and `SYSTEM` is a second one written into
  `work_order_escalations.userId`.
* **The display name is stored beside the id** — `department` beside
  `departmentId`. `service` and `location` have *no* id at all: the
  `WordUtils.capitalize`-normalised display string **is** the key.
* **Time is stored twice**, as `Date` and as `Long` millis, in three places.
* **State is stored twice** — an eight-value enum *and* five booleans, which
  disagree (F10).

---

## 3 · The workflows, end to end

### 3.1 · Creation

```text
POST /staff/wo  (or /customer/wo, or /team-management/*, or RMQ wo.instio.create)
  |
  |- @PreAuthorize hasPermissionOnCreateWorkOrder(header companyId, body companyId)
  |     the ONLY tenancy comparison in the service
  |- mapper.validate            type non-blank; service non-blank unless MAINTENANCE
  |- findMaxWOId(companyId)     INSERT..ON DUPLICATE KEY UPDATE, then SELECT
  |- map -> status NEW, EntityStatus ACTIVE, service WordUtils.capitalize'd
  |- save
  |- summaryService.initialize
  |- activity CREATED           (this path copies attachment files; the batch path does not)
  |- initDefaults               due date, assignment, priority, START TIME, checklist
  |     start time is forced into the FUTURE - now+1min if the caller's is past   [F36]
  |- save (second)
  |- createAll(activities)      batch insert
  `- afterCommit -> rmq "wo.work-order.create"   (payload: the id, nothing else)
        |
        `- WorkOrderBackGroundService.defaultInitsForWorkOrder
             summaryService.initialize   (again)
             setDefaultWorkOrderTypeFor      <- unreachable: validate() already required it
             setDefaultWorkOrderDepartmentFor
             setDefaultAssignmentFor      -> updateAssignment -> publishes ANOTHER event
             setDefaultSlaFor
             setDefaultPriorityFor        -> 5 when unconfigured                  [F1]
             rmq "wo.wo-init.created"     (PPM acknowledgement)
             maybe rmq "wo.work-order-assignment.update"
             setDefaultInfoFor            -> CRM lookup, snapshot guest into customer_info
```

Creation is **eight writes and up to four further messages**, and the
caller's response is returned before any of the defaults exist. A client
that reads back immediately sees a work order with no priority, no SLA, no
department and no assignee.

### 3.2 · Assignment, acceptance, start, close

```text
PATCH /staff/{id}      kind=CAPTURE
PATCH /staff/{id}/assignment          -> updateAssignment
                       clears accepted/started/waiting
                       USER -> status OPEN · TEAM -> status NEW
                       adds assignee to followers
                       rmq wo.work-order-assignment.update
                          accumulate any un-credited WAITING time
                          summary.lastAssignedOn = now
                          summary.numberOfAssignments += 1
                          clear accepted/started timestamps
                          rmq notify-rules  (rebuild escalation chain)
                          rmq end-all-timers · rmq wo.track
                          rmq event-communication ASSIGNMENT
                          DEVICE -> IllegalStateException                         [F12]

POST /time-log/{id}    kind=ACCEPT
                       accepted=true; status -> ON_HOLD (unless WAITING)
                       summary.acceptedTimeInMillis = now
POST /time-log/{id}    kind=START_TIMER
                       ends any other user's open timer
                       accepts implicitly if not accepted
                       new WorkOrderTimeLog; started=true; status -> IN_PROGRESS
                       schedules 5 progress reminders at 50/75/100/125/150 % of SLA
POST /time-log/{id}    kind=END_TIMER
                       timer closed, minutes computed, started=false (bulk update)
                       status -> ON_HOLD
                       ...and then save(workOrder) writes started=TRUE back       [F11]

PATCH /staff/{id}      kind=CLOSED
                       status -> CLOSED; activity STATUS
                       reopen detected by comparing CLIENT-SUPPLIED from/to       [F9]
                       rmq: update-all-timers, notify-rules, wo.track, communication
                       cascade-close every affiliated work order (errors swallowed)
```

**There is no state machine.** `updateStatus` accepts
`WorkOrderStatus.valueOf(kind)` for any of the eight values from any of the
eight values; the only guards are "no change" short-circuits. `NEW` straight
to `CLOSED` is legal. `CLOSED` back to `IN_PROGRESS` is legal.

### 3.3 · Waiting

```text
kind=WAIT   status -> WAITING, waiting=true
            rmq waiting.reminder -> four Quartz jobs at -5/0/+5/+15 min
                                   assignee, assignee, supervisor, HOD
kind=BEGIN  requires status==WAITING
            status -> ON_HOLD    <- not the status it came FROM                   [F10]
            waiting=false
            summary: credit the paused milliseconds to totalSlaWaitingMillis
```

`ON_HOLD` therefore means three unrelated things: *accepted but not started*,
*a timer was paused*, and *returned from waiting*.

### 3.4 · Escalation, as it actually runs

```text
any of six mutations -> rmq wo.notify.rules -> EventExecutorServiceImpl.initRule
  |
  |- MAINTENANCE type?  -> cancel everything and return
  |- load DefaultEscConfig from  <baseDirectory>/default-property-esc/<siteId>.json
  |     absent -> log.error, return, NO escalations for that property, silently   [F5]
  |- delete the entire Quartz group for this work order   <- unconditional
  |- for each configured escalation:
  |     identifier gate  (already assigned? accepted? started? closed?)
  |     services filter  - services==null means applies to NOTHING                [F6]
  |     category filter
  |     anchor + interval, floored at the start-by time
  |     deadline already past?  -> dedupe-check, then DROP, permanently           [F7]
  |- sort, take the earliest, put the REST in the job's data map as JSON
  `- schedule one Quartz job; on fire it runs the head, runs any now-past tail
     entries in a burst, and reschedules the remainder
```

On fire: skip if WAITING, skip if CLOSED, resolve recipients (`USER` ids,
`POSITION` lookup, or `SELF` = assignee or team members), add the on-duty
manager between 20:00 and 08:00 **Asia/Kolkata** for tertiary escalations
only, write the dedupe rows, write an activity, then dispatch — email and
notification really; SMS and WhatsApp into commented-out lines.

### 3.5 · The scheduled (PPM) job

```text
Quartz cron -> runWorkOrderScheduledTask
  build a WorkOrderCreateDto from the schedule
  description = a fixed English paragraph, always
  rmq.convertSendAndReceiveAsType("wo","wo.instio.create", ...)  <- blocking RPC over the bus
  catch (Exception ignored) { }                                  <- the whole creation [F8]
  advance nextTrigger regardless of whether anything was created
  getNextTrigger(...) dereferenced without a null check
```

### 3.6 · The guest tracking flow

```text
GET /tracking/{id}                -> getAllTrackingForWorkOrder
GET /tracking/{id}/current_state  -> getLatestTrackStatusOfWO
     both do:  Optional<WOServicePreference> woServicePreference = null;
               if (woServicePreference.isPresent()) ...
     -> NullPointerException, every call, unconditionally                         [F2]
     -> caught by the controller and returned as HTTP 200 with the message as data
POST /tracking/{crmId}/{id}       -> guestActivityOnTrack - an EMPTY method
     -> responds "Sucesss"; nothing is written
```

---

## 4 · Integration points

### 4.1 · Outbound HTTP (OpenFeign) — 8 clients

| Client | Target | Purpose |
|---|---|---|
| `SessionClient` | `app.sba.base` | **validate the caller's session token, on every request** |
| `UserClient` | `app.sba.base` | user profile, department, users-in-position |
| `TeamClient` | `app.sba.base` | team membership |
| `PropertyClient` | `app.sba.base` | company / site / facility / department info |
| `ShortUrlClient` | `app.sba.base` | shorten every notification link |
| `CrmClient` | `crm.instio.co` | guest profile, guest session |
| `CommunicationClient` | `localhost:6015` | send email / SMS / WhatsApp |
| `CIXClient` | `cix.instio.co` | checklist / inspection status |

Two of the eight (`SessionClient`, `ShortUrlClient`) carry **no timeout
configuration**. One (`CrmClient`) has a circuit breaker whose fallback
**fabricates a guest** (F4).

### 4.2 · RabbitMQ

Exchange `wo`, plus `user`, `re`, `crm`, `cm`. Eleven consumers:

```text
wo.work-order.create · assignment.update · status.update · priority.update
due-date.update · start-time.update · checklist.update · reminder.create
wo.work-order-waiting.reminder · wo.work-order-schedule.{create,delete}
wo.notify.rules · wo.event.communication · wo.track
wo.timelog.assignee_changed · wo.timelog.wo_closed
wo.email.any · wo.sms.any · wo.whatsapp.any · wo.notification.any
```

Three of those consumers have **empty bodies** (priority, due-date,
start-time), two are empty but for a log (`wo.track`, checklist), and two
have no publisher at all (`wo.sms.any`, `wo.whatsapp.any`).

**Every payload is either a bare `Long` id or a `HashMap<String,String>`.**
No event type, no version, no timestamp, no correlation id, no actor. A
consumer re-reads the row and acts on whatever it finds now — so two rapid
status changes are indistinguishable from one, and a replay is
indistinguishable from a fresh fact.

Two **synchronous request/reply calls over the bus**:
`rst.attendance.finder` (who is the manager on duty) and `rst.host.finder`
(who are the hosts), plus `wo.instio.create` used reply-style by the
scheduler.

### 4.3 · Quartz

A clustered JDBC job store in its own MySQL database, 20 worker threads,
misfire threshold 60 s. Escalation chains, progress reminders, waiting
reminders and PPM crons all live here. Job data carries **JSON strings**,
including a serialised remaining-chain array.

### 4.4 · Files

Uploads (image / audio / base64) to a local directory; rendered email bodies
written to disk as HTML and **delivered as a public URL** rather than as mail
content; escalation configuration read from JSON files on the same disk.
There is no object store — a single-machine assumption in a service whose
Quartz store is explicitly clustered.

### 4.5 · Datastores

MySQL (business) + MySQL (Quartz) + MongoDB (all configuration). No Flyway,
no Liquibase, `db-migration/` is an **empty directory**, and
`spring.jpa.hibernate.ddl-auto=update` with `createDatabaseIfNotExist=true`.

### 4.6 · The one integration that is a finding by itself

`TeamManagementController` raises "create/edit/delete this user" requests as
work orders — by **overwriting the caller's tenancy with three hard-coded
ObjectIds** (`TeamManagementController:29-31`) and writing into the vendor's
own support tenant. The lines that would have recorded *which* customer
asked are commented out (`:83-87`), so the resulting work order says a user
change is needed and cannot say for whom.

---

## 5 · Findings

Numbered, each with its evidence. **F1–F10 are the ten to read first**; they
are ordered by what they would cost us if carried forward, not by severity in
the reference.

### F1 · The priority default is a constant, and there is no "unset"

`WorkOrderBackGroundService:624`

```java
Integer priority = woServicePreference == null || woServicePreference.getPriority() == 0
        ? 5 : woServicePreference.getPriority();
```

Every work order whose service has no configured priority becomes **5 of 10**
— and `WorkOrderBaseService.buildDataMap` then renders below-5 as "Low",
exactly-5 as "Medium" and above-5 as "High", so *"nobody said"* and
*"somebody said medium"* are the same value, in the database and in every
message. The Oracle round's sentence applies unchanged: **the reference
guessed with a constant.** Compounding it, `WorkOrderView.priority` is an
unboxed `int`, so a genuinely null priority is published to clients as `0` —
a value `validatePriority` rejects as invalid.

### F2 · Two guest-facing reads throw, unconditionally, and report success

`WorkOrderTrackingServiceImpl:76` and `:96`

```java
// Optional<WOServicePreference> pref = woPreferenceService.getService(...);
Optional<WOServicePreference> woServicePreference = null;
if (woServicePreference.isPresent() && ...)
```

The real lookup was commented out and the variable left at `null` with the
dereference intact. Both public methods of the tracking service raise
`NullPointerException` on **every** call. The controller catches it and
returns `HTTP 200` with the exception message in the `data` field
(`WorkOrderTrackingController:35,45`) — so the guest tracking page is
broken, has been broken, and no monitor can see it, because the service
answers 200. This is the Oracle survey's *"a read timeout is treated as
success"* one layer up: **a total failure reported as a success.**

### F3 · Reading, updating and patching a work order is not tenancy-scoped

`WorkOrderStaffController:54,66,76` carry no `@PreAuthorize` at all (`patch`
has one, and it checks *involvement*, not tenancy).
`WorkOrderServiceImpl.get(Long)` calls `repository.findById` — no company
predicate. Ids are global auto-increment values.

**The safe query exists and nothing calls it**: `WorkOrderRepository:11`
declares `findByCompanyIdAndId(String, Long)`; a repository-wide grep finds
exactly one reference to that name — its own declaration.

Search is the other half: `AbstractRepositorySpecification.companyId` is
mandatory, but it reads the **request body**, not the session, and
`siteIds()` returns `null` (no predicate) when empty — so a request omitting
`siteId` lists every property in the company, and a request naming another
company's id lists that company's work orders.

### F4 · When the CRM is unreachable, a guest is invented

`CustomerServiceImpl:43-52`

```java
private Customer getFallbackCustomer(String customerId, boolean skip, Throwable t) {
    ...
    name.setDisplayName("Customer " + customerId);
    return fallbackCustomer;
}
```

The circuit-breaker fallback returns a `Customer` that never existed, and
`setDefaultInfoFor` then **persists** it into `work_order_customer_info` as
the guest's snapshot. The same shape appears again at
`WorkOrderBaseService:227`: an unresolvable user becomes the literal string
`manager` in every generated message. Two places where the reference's two
options were *guess a value* or *fail the flow*, and it chose to guess — and
in the CRM case, to write the guess down.

### F5 · Escalation configuration is a JSON file on local disk, and its absence is silent

`EventExecutorServiceImpl:240-255`. The per-property escalation chain is read
from a JSON file under the service's own base directory, named by `siteId`,
and cached in memory for 60 minutes. If the file is missing the method logs
at ERROR and returns `null`, and `handleEscalationFromDefault` returns —
**that property has no escalations, and nothing anywhere says so.** A
property is onboarded by copying a file onto a server's filesystem; a
rollout to a new node without the files silently disables escalation for
every property on it.

### F6 · An escalation with no service filter applies to nothing

`EventExecutorServiceImpl:306,320,336,357` — the same four lines, four times:

```java
if (escalation.getServices() == null ||
    escalation.getServices().stream().noneMatch(s -> s.equalsIgnoreCase(wo.getService())))
    continue;
```

`services == null` reads naturally as *"no filter, applies to everything"*.
It means *"applies to nothing"*. A configuration file written by someone who
omitted the optional field disables the escalation it was written to create.

### F7 · A missed escalation is dropped and not recorded as missed

`EventExecutorServiceImpl:383-435`. An overdue entry is checked for
relevance, checked against the dedupe table, then **dropped** with an `INFO`
log. The previous behaviour (catch up at *now + 5 s*) is present as a
commented-out line with its own justification. Both are defensible; what is
not is that neither writes anything a report could read. A four-hour outage
produces a set of work orders that were never escalated and no record
distinguishing them from work orders that needed no escalation.

The scheduler's own comment makes the opposite claim to its code
(`SchedulerService:100` against `:132`):

```java
/** Schedule a fire-once escalation job with misfire policy DO_NOTHING.
 *  Missed escalation triggers ... are intentionally dropped rather than re-fired */
        .withMisfireHandlingInstructionFireNow()
```

This is the constitution's *"a comment that states a guarantee is a
specification nobody tests"*, found in the wild: the comment asserts an
outcome, the code does the opposite, and nothing can fail to reveal it.

### F8 · Eleven message consumers swallow every error, and the PPM creation swallows its own

Every `@RabbitListener` in `WorkOrderBackGroundService`,
`WorkOrderTimeLogBackgroundService`, `EventCommunicationService`,
`WorkOrderTrackingServiceImpl` and the three sending services ends:

```java
} catch (Exception e) { e.printStackTrace(); }
```

The message is acknowledged, the work is not done, and the stack trace
bypasses the logging framework entirely. **Sixteen files contain
`printStackTrace`.** Worse is `runWorkOrderScheduledTask`
(`EventExecutorServiceImpl:589`), where `catch (Exception ignored) {}` wraps
the entire creation of a scheduled work order — and the schedule's
`nextTrigger` is advanced afterwards regardless, so a preventive-maintenance
job that failed to be created is indistinguishable from one that succeeded.

### F9 · "Reopened" has two definitions and is counted twice, from client-supplied data

`WorkOrderServiceImpl:185`

```java
boolean isReopen = "CLOSED".equals(updateRequest.getFrom())
                && "OPEN".equals(updateRequest.getTo());
if (isReopen) { ... workOrderSummaryService.incrementReopenCount(id); }
```

`from` and `to` here are whatever the **client sent** — and
`mapper.updateStatus` has already overwritten both from the stored row, so
the two never agree unless the client guessed right. Meanwhile
`WorkOrderMapper.updateStatus:174` increments `WorkOrder.reopenCount` under a
*different* rule: previous status was CLOSED and the new one is not. Two
counters, two definitions, one of them driven by request data.

### F10 · State is stored twice and the two copies disagree

An eight-value `workOrderStatus` **and** five booleans (`accepted`,
`started`, `waiting`, `guestAcknowledged`, `reopened`), maintained by
different code paths and reconciled by none:

* `updateAssignment` clears accepted/started/waiting;
* `resetWorkOrderFlags` (a bulk `UPDATE`) clears them again on OPEN/CLOSED;
* `accept()` sets `accepted` and moves status to `ON_HOLD` — there is no
  `ACCEPTED` status;
* `BEGIN` moves WAITING to ON_HOLD rather than back to whatever it was;
* `ESCALATED` and `REMOVED` are declared in the enum and **written by
  nothing**.

`ON_HOLD` therefore carries three meanings, and `accepted=true` can coexist
with `status=NEW`.

### F11 · A bulk UPDATE and a managed entity write the same row in one transaction, and the entity wins

`WorkOrderUserTimeLogServiceImpl.endTimer:130-146`

```java
WorkOrder workOrder = workOrderJpaAdapter.get(workOrderId);   // managed, started=true
endTimer(workOrderId, timeLog, "END", desc);                  // JPQL bulk: started=false
workOrder.setWorkOrderStatus(ON_HOLD);
workOrderJpaAdapter.save(workOrder);                          // flushes started=TRUE back
```

The bulk update bypasses the persistence context; the subsequent flush of the
stale managed entity reverts it. `started` remains `true` after a timer ends.
The same pattern — `@Modifying` JPQL beside entity saves — appears across
`WorkOrderRepository`: eight bulk updates that also defeat the `@Version`
optimistic lock declared on `AbstractBaseEntity`.

### F12 · DEVICE is a first-class assignee type that crashes the background flow

`AssigneeType` is `{USER, TEAM, DEVICE}`; there is a `WODevice` document, a
device-slot assignment service and a preference surface for it. But
`WorkOrderBackGroundService.defaultInitsForWorkOrderAssignment:340`:

```java
default: throw new IllegalStateException("Unexpected value: " + workOrder.getAssigneeType());
```

Assigning to a device throws inside a consumer whose handler swallows the
exception (F8). The assignment persists; the summary, the escalation
rebuild, the timer termination and the notification never happen.

### F13 · Sixteen operations behind two untyped envelopes

`WorkOrderUpdateRequestDto` is `{kind, from, to, message, attachments,
initiatedById}` — and `to` is parsed as an `int` for PRIORITY and SLA, a
`Date` for DUEDATE and STARTTIME, a `boolean` for GUEST_ACKNOWLEDGE, and an
id for EXECUTE and CHECKLIST. `from` is a time-log id for END_TIMER
(`Long.parseLong(defaultIfBlank(request.getFrom(), "0"))`). The messaging
layer is worse: **every RabbitMQ payload and every template context is a
`HashMap<String,String>`**, mixing control keys (`_template`, `_woid`,
`_action`, `_chain`) with template variables in one namespace, and mixing
two templating mechanisms (mustache placeholders and `String.format`) in one
field.

### F14 · Session validation is a remote call, cached in front of its authority for ten minutes

`UserServiceImpl:41` — `@Cacheable(cacheNames = "user", key = "#token")` on
`fetchAuthSession`, with the `user` cache configured
`expireAfterWrite(10 minutes)`. A revoked session keeps working for up to ten
minutes. The same cache region also holds profiles keyed by user id, so two
different value shapes share one namespace.

### F15 · The guest "session" is a bare identifier in a header

`CustomerAuthenticationFilter:44` reads the `customerId` header;
`CustomerServiceImpl.fetchAuthSession:56` turns it into a session by looking
that id up in the CRM. **There is no credential.** Knowing (or enumerating) a
CRM id authenticates as that guest. The staff equivalent is a session token
carried in a header named `X-XSRF-TOKEN` — an anti-CSRF header repurposed as
the bearer credential, while CSRF is disabled on every path.

### F16 · Eighteen of twenty-three controllers are unauthenticated

`SecurityConfig` declares four filter chains. Chain 1 protects `/staff/**`,
`/wo/preference/**`, `/time-log/**`, `/activities/**`. Chain 2 protects
`/customer/**`. **Chain 3 (`GuestSecurityConfig:152`) declares no
`requestMatchers` and no `authorizeRequests`** — it therefore matches every
remaining request and permits it. Chain 4 is unreachable.

Unauthenticated as a result: `/tracking`, `/rating`, `/feedbacks`,
`/followers`, `/todo`, `/reminder`, `/schedule`, `/summary`, `/cix`,
`/wo-escalations`, `/wo-escalation-levels`, `/wo-attributes`,
`/work-order-affiliation`, `/wo/rules`, `/wo/scheduler`, `/wo/internal`,
`/wo/general`, `/team-management`. Among them: a work-order **list** endpoint
taking `companyId` as a query parameter (`InternalController:57`), the rules
engine's write surface, and every file endpoint.

### F17 · admin:password, in three places, and it is the real credential

* `SecurityConfig:95-98` — an in-memory user `admin` with password
  `password` and authority `ADMIN`, which satisfies chain 1's
  `hasAnyAuthority("WO.READ", "WO.WRITE", "ADMIN")`.
* `CommunicationClient` class annotation — a Basic authorization header.
* `CommunicationConfig:6` — the same header again, as an interceptor.

The base64 value in both decodes to `admin:password`. Alongside it, checked
in: the MySQL password in both `application.properties` and
`quartz.properties`, a 64-hex platform key (`app.sba.auth`), three sets of
RabbitMQ credentials in `application-local.properties`, a Spring Boot Admin
credential, a public RabbitMQ host IP, and `useSSL=false` on both database
URLs.

### F18 · Unauthenticated file endpoints with no path confinement

`GeneralController` (chain 3, so unauthenticated) exposes upload of images,
audio and base64 images, and fetch of images, audio and **rendered email
bodies**. `loadFileAsResource:73` does
`fileStorageLocation.resolve(fileName).normalize()` with **no check that the
result is still under the storage root** — `normalize()` collapses
parent-directory segments, it does not confine. Uploads take the file
extension straight from `multipartFile.getOriginalFilename()`; the
`FileService.isValidExtension` that exists for exactly this is never called
from here.

And the emails themselves: `EmailSendingService:70` renders every message to
an HTML file on disk and sends **a link to it** rather than the content — so
guest names, emails, phone numbers and work-order details sit in files served
publicly and never deleted.

### F19 · The application keeps its own copy of user contact details

`WorkOrderPreferenceServiceImpl.getUserPreference:76` — a *read* that, on a
miss, calls the user service, copies `name`, `email` and `phone` into a Mongo
document, saves it, and returns it. Every subsequent notification addresses
the **copy**. A user who changes their email keeps receiving mail at the old
one. This is the no-duplicated-master-data rule broken in the smallest
possible way and the most expensive: nothing refreshes it and nothing reports
the divergence.

`getCommunicationPreference:285` is the same shape one level up — a getter
that creates and persists a defaults document on a miss, from nine call
sites. And it assigns **one `Preference` instance to six fields**
(`createCommunicationPreferenceWithDefaults:277-282`), leaving `dueDate` and
`track` null, so `getCommunicationPref(DUE_DATE)` returns null into an
unguarded `preference.getEmail()`.

### F20 · Two delivery channels are fully built and disconnected

`Q_SEND_SMS` and `Q_SEND_WHATSAPP` have consumers, payload builders, a
six-provider enum, a fifteen-state delivery-status enum, per-user,
per-company and per-customer preference models, and template configuration.
Every publish is commented out — `EventCommunicationService:118,130,167,
263,280` and `EventExecutorServiceImpl:780,826`. A hotel configures SMS
escalation, sees it saved, and nothing is sent. Nothing in the product says
so.

### F21 · Three of four escalation designs, and five whole subsystems, are dead code

Verified by reference count across the tree:

```text
WOPreference engine    onWorkOrderUpdate(WorkOrder)  - sole call site commented out
WORules engine         getActionsFromRule            - no callers at all
EscalationHandler      never instantiated; its Javadoc is the only surviving design
ReminderHandler        never instantiated; its "mark as fired" line commented out
NonsenseGenerator      202 lines of deprecated fake-data generation, unreferenced
runReminderScheduledTask                             - empty method body
3 RMQ consumers        priority / due-date / start-time - empty bodies
wo.track consumer      body commented out; auto-tracking never runs
CIX inspection creation                              - commented out
findByCompanyIdAndId · getAcceptStatus · fetchDistinctServiceCategory ·
getDistinctFollowersByWorkOrderId · updateWOPriority · WorkOrderRepository.findMaxWOId
                                                     - declared, never called
```

Their **configuration surfaces are all still live**. An operator can create
rules in `/wo/rules`, escalation preferences in `/wo/preference/property`,
track stages in `/wo/preference/track` and reminders on a work order, and
none of them will ever execute.

### F22 · Asia/Kolkata is hard-coded in five places, and is the fallback for a configured field

```text
WOPreference:42                    getTimeZone() -> defaultIfBlank(timeZone, "Asia/Kolkata")
EventExecutorServiceImpl:113       rule time conditions   (with a TODO saying to fix it)
EventExecutorServiceImpl:668,671   night-hours window 20:00-08:00
EventExecutorServiceImpl:1014      every date rendered into an email
```

The Oracle survey found the identical defect in the identical form —
*"a blank time zone silently becomes Asia/Kolkata"*. Elsewhere the server's
own zone stands in: `Calendar.getInstance()` for every reminder offset,
`ZoneId.systemDefault()` in `DateUtils.asDate`.

### F23 · The date parser is wrong in four distinct ways

`utils/DateUtils.tryParseQuietly`

* **any numeric string is treated as epoch milliseconds** — an eight-digit
  date such as 20241112 parses to 1970-01-01;
* the format list contains the same ISO-with-offset pattern **twice**;
* two entries use an unquoted `T`, which is not a legal `SimpleDateFormat`
  pattern letter — both throw and are swallowed;
* **no format carries both milliseconds and a zone offset**, so the exact
  string the class's own `main()` method tests is parsed by the
  milliseconds-without-zone pattern and the zone is discarded — a UTC instant
  read as server-local.

`minutesInBetween` then rounds every duration up at 30 seconds, and
`ZonedDateTime.now().toEpochSecond() * 1000` (nine sites) truncates every
"millis" timestamp to the second.

### F24 · Two named individuals receive every escalation email in production

`EventExecutorServiceImpl:80` holds a two-element list of one specific
customer's staff email addresses, added to every escalation recipient set
when the active profile is `release` (`:948-953`). One customer's two people,
compiled into the binary, receiving every property's escalation mail.
Alongside it in the same file: `MANAGER_ON_DUTY` and
`HOUSEKEEPING_SUPERVISOR` as hard-coded position names, and a five-tier
escalation ladder (supervisor, HOD, RM, cluster, CRO) hard-coded into the
email template context.

### F25 · Recipient lists are split on the hyphen

`EventExecutorServiceImpl:639`

```java
String[] to = escalation.getTo().split("[-/,]");
```

The delimiter set includes the hyphen. Any identifier containing one — a
UUID, most obviously — is shredded into fragments, each treated as a
recipient. A debugging `main()` for exactly this expression was left in a
production service (`WorkOrderUserTimeLogServiceImpl:16`).

### F26 · The same concept modelled twice, six times over

* `work_order_contact_info` and `work_order_customer_info` — **byte-identical
  entity definitions**, different table names, both written by the same flow;
* `work_order_rating` and `work_order_feedback` — two guest-satisfaction
  records, each 1-5 plus a comment;
* `WorkOrder.followers` (an element collection) and `work_order_follower`
  (a table) — two follower lists;
* `WoSequence` and `WorkOrderRepository.findMaxWOId` — two numbering schemes,
  one dead;
* `co.instio.dto.PaginatedList` and `co.instio.models.PaginatedList` — two
  identical classes in two packages;
* `co.instio.application.entity.Address` and `co.instio.models.Address`.

### F27 · Schema by inference, with no migrations

`db-migration/` is an **empty directory**. `spring.jpa.hibernate.ddl-auto=update`
and `createDatabaseIfNotExist=true` mean the schema is whatever Hibernate
derives from the entities at boot. Consequences visible in the tree:
`entity/jpa/Device.java` carries `@Table` and **no `@Entity`**, so a mapping
that looks real creates nothing; `ignore-data.sql` defines a reporting view
over columns the entity model does not produce; `init-sql.sq` (the extension
is a typo) is a Quartz DDL script the README says to run by hand.

### F28 · Zero tests

`src/test` does not exist. `spring-boot-starter-test` is a declared
dependency. 382 production files, 0 test files. Every behaviour described in
this survey was established by reading.

### F29 · Logging is the audit trail, and it logs everything at ERROR

140 `log.error` calls, the great majority on success paths — a new work
order, an SLA being set, a filter chain announcing itself.
`WorkOrderManagementRMQConfiguration` logs **every message body** at ERROR,
and `EventExecutorServiceImpl:944` logs recipient email addresses at ERROR.
Meanwhile neither `logback-spring-1.xml` nor `logback-spring-2.xml` is named
`logback-spring.xml`, so **neither is loaded**. An operator cannot
distinguish a failure from a heartbeat, and guest contact details are in the
log stream.

### F30 · Structure: hexagonal ports over a god base class

The tree has 23 driving ports and 27 driven ports — a full hexagonal
skeleton — and then `WorkOrderBaseService`, an abstract `@Service` with six
`@Autowired` fields, extended by both the event executor and the background
service, holding a 170-line `buildDataMap` that makes ten concurrent
external calls and returns a `HashMap<String,String>`. The ports are
per-entity rather than per-capability, so each of the seventeen tables has
its own port, adapter, repository, mapper, service, controller and DTO set —
the seven-file tax that produces a service port for a table with three
columns. Four files exceed 400 lines; `EventExecutorServiceImpl` is 1,049.

### F31 · Three tenancy levels, and the third means nothing

`companyId` / `siteId` / `facilityId` appear on the work order, on five
satellite tables, on every Mongo document, in every filter and in seven
composite indexes. `facilityId` is **filtered on and never set** by any
create path, and no preference or default resolution consults it. A third
tenancy axis carried by the whole schema and used by nothing.

### F32 · Positions are built by string concatenation from display names

`EventCommunicationService:188`

```java
userService.getUsersInPosition(companyId, siteId, workOrder.getDepartment() + "_SUPERVISOR")
```

The department **display name** — stored beside its id, and renameable — is
concatenated into a position code. Rename a department and the supervisor
escalation silently resolves to nobody. The same coupling appears wherever
`service` and `location` are used: both are display strings acting as keys,
and `service` is additionally rewritten by `WordUtils.capitalize` on write,
so the stored key depends on the casing the operator typed.

### F33 · Search is injectable and its defaults hide data

`AbstractRepositorySpecification.contains` builds a `LIKE` pattern with
`MessageFormat.format` — user input is placed into the pattern with the
wildcard characters unescaped (a lone percent sign matches everything), and
a brace in the query string makes `MessageFormat` itself misbehave.
Separately, `WorkOrderFilter.getWorkOrderStatuses` defaults to **excluding
CLOSED and REMOVED**, and `getStatus` defaults to ACTIVE only — so the
unqualified list endpoint silently hides closed work; and the
initiator-or-assignee-or-department clause is composed such that supplying
none of the three removes the restriction entirely.

### F34 · A weekly schedule covering all seven days cannot be scheduled

`CronUtils.expressionForDayOfWeek` returns a question mark when the set has
seven entries, and the caller already passes a question mark for
day-of-month. Quartz requires **exactly one** of the two to be a question
mark; both being one is rejected. The exception is caught and logged in
`SchedulerService.scheduleSimple`, and
`defaultInitsForWorkOrderScheduleCreate` then saves the schedule with a null
`nextTrigger` — a daily preventive schedule that is created, listed, and
never runs.

### F35 · Escalation deduplication is permanent, so a reopened work order never escalates again

`WorkOrderEscalationServiceImpl.hasEscalationFired(id, identifier)` tests
`existsByWorkOrderIdAndIdentifier` — the presence of *any* historical row.
Rows are never cleared on reopen. A work order closed, reopened, and left
unattended will never escalate, because the not-closed escalation "already
fired".

### F36 · Creation cannot record work that already started

`WorkOrderMapper.initDefaults:398`

```java
Date startTime = request.getStartTime() != null ? request.getStartTime() : ceiling(now, MINUTE);
if (startTime.before(new Date())) startTime = DateUtils.addMinutes(new Date(), 1);
```

and `updateDateFields` rejects any past date outright. A supervisor logging a
job a technician began an hour ago cannot say so; the SLA clock starts in the
future. For a hotel this is the normal case, not the edge one.

---

## 6 · Requirements for our Jobs

From here the names are **ours** (APPS-Q3). Each requirement names what it
carries over, fixes, or refuses, and the finding or platform rule behind it.
None of these is a decision — they are the proposal the owner's gate rules on.

### 6.1 · The job and its identity

**R1 · A job's identity is a UUID assigned by us; the human-facing number is
a separate, per-property field.** The reference exposes a global
auto-increment integer as the public identifier and keeps a per-*company*
running number beside it. We take the idea and fix both halves: `job_id` as a
UUID internally, and a display number sequenced **per property**, because a
property's staff count their own work and a group's two hotels must not share
a series. *(§2.4, F3; the constitution's identifier strategy.)*

**R2 · A job references `masterdata.room_id` or `masterdata.asset_id`, never
a location string.** `location` and `service` in the reference are display
strings acting as keys, normalised on write, unresolvable after a rename.
`jobs` holds a typed reference and resolves presentation through Context.
*(F32; no-duplicated-master-data; ADR 0051.)*

**R3 · A job names a department by its canon code and stores no display name
beside it.** The reference stores `departmentId` **and** `department` and
then builds position codes out of the display half. ADR 0119's canon is the
identity; ADR 0116 §4 makes it universal and rename-safe by construction.
*(F32.)*

**R4 · A job stores no copy of any person's name, email or phone.** The
reference copies user contact details into its own preference documents and
addresses the copy forever. *(F19; ADR 0051.)*

**R5 · There is no third tenancy axis.** `organization_id` and
`property_id`, and nothing else. The reference's `facilityId` is carried by
the whole schema, indexed seven ways, and set by nothing. *(F31; ADR 0060.)*

### 6.2 · State

**R6 · One state, in one place, with a declared transition table.** The
reference keeps an eight-value enum and five booleans that disagree, and
permits any transition to any other. Jobs has a single status and an explicit
machine; a refused transition is an error naming both states. *(F10.)*

**R7 · Acceptance, execution and pause are states, not flags — and ON_HOLD is
split.** `ON_HOLD` in the reference means *accepted-not-started*, *timer
paused* and *returned-from-waiting*. Those are three states and a report
cannot tell them apart. *(F10, §3.3.)*

**R8 · A resumed job returns to the state it left, not to a fixed one.**
`BEGIN` sends every waiting job to `ON_HOLD` regardless of where it came
from. *(§3.3.)*

**R9 · Reopening is one event with one definition, derived from stored state,
never from request fields.** The reference has two reopen counters under two
rules, one of them driven by client-supplied `from`/`to`. *(F9.)*

**R10 · A job can record work that has already started.** Backdating a start
time is the normal case in a hotel and the reference forbids it. Where a
backdated start would distort an SLA, the SLA is computed from the recorded
time and the entry is marked retrospective — it is not refused. *(F36.)*

### 6.3 · Priority, SLA and the clock

**R11 · Priority is a named, ordered, property-configurable set, and "unset"
is representable.** Not an untyped one-to-ten integer, not defaulted to five,
and not collapsed to Low/Medium/High by a rule buried in a message builder.
*(F1.)*

**R12 · Every deadline is computed in the property's own time zone, which is
mandatory and never defaulted.** No `Asia/Kolkata` fallback, no
`Calendar.getInstance()`, no `ZoneId.systemDefault()`. This is the Oracle
round's R16 arriving in a second application, which is itself the argument
for it being platform-shaped. *(F22; see §7.)*

**R13 · Durations are stored once, as instants, and derived on read.** The
reference stores a date and a millisecond count side by side in three places,
truncates to seconds in nine, and rounds up at thirty seconds. *(§2.4, F23.)*

**R14 · The SLA clock's pause is a recorded interval, not an accumulator.**
The reference keeps a "waiting started at" and a "total waiting millis" and
has three separate code paths trying to reconcile them when a job leaves
WAITING by an unexpected door. Store the pause intervals; sum them when
asked. *(§3.3.)*

**R15 · One definition of response time.** The reference computes three from
near-identical loops differing by one query, and nobody can say which a
report should use. *(§1.3, F30.)*

**R16 · Derived figures are computed by the write path, not repaired by a
sweeper.** A permanent two-minute background job repairing ten rows at a time
is evidence the write path does not work; it is not a design. *(§1.3.)*

### 6.4 · Escalation

**R17 · One escalation engine, and its configuration lives in the `jobs`
schema.** Not four designs of which one runs, and not a JSON file on a
server's local disk. *(F5, F21.)*

**R18 · An unconfigured property is a stated condition, not a silence.** A
property with no escalation policy must be visible as such — in the console
and in a health signal — never a `log.error` and an early return. *(F5.)*

**R19 · An omitted filter means "applies to all"; restricting to nothing
requires saying so.** *(F6.)*

**R20 · A missed escalation is recorded as missed.** Whether the platform
fires late or drops it is a policy question (§7); that the outcome is
*recorded either way* is not. A four-hour outage must be answerable
afterwards. *(F7.)*

**R21 · Deduplication is scoped to the current cycle of the job, not to its
lifetime.** A reopened job escalates again. *(F35.)*

**R22 · Escalation targets are roles resolved through Workforce, never
position strings built by concatenation and never individuals named in
code.** ADR 0116 §6 makes department membership derive from Workforce
postings, permanently; the reference's department-plus-underscore-SUPERVISOR
and its two hard-coded customer email addresses are the two failure modes
that rule prevents. *(F24, F32.)*

**R23 · A configured channel that cannot deliver is refused at configuration
time.** The reference lets a hotel configure SMS escalation wired to nothing.
This is `AUTHZ-Q25`'s ruled shape one domain over — *refusing at install what
cannot be delivered.* *(F20.)*

### 6.5 · Events and the platform boundary

**R24 · Every state change publishes a typed event appended in the caller's
transaction.** `events.append(tx, event)` is a local write; the Kernel's
publisher relays. The reference publishes a bare id over RabbitMQ, sometimes
inside the transaction and sometimes after it, and a crash in the gap keeps
the change and loses the announcement. *(§4.2; the constitution's
events-are-appended-in-the-caller's-transaction rule.)*

**R25 · An event carries the fact, not a pointer to it.** Eleven consumers in
the reference receive an id and re-read the row, so two rapid changes are
indistinguishable from one and a replay is indistinguishable from a fresh
fact. This is the Oracle survey's R20 — *a notification is not a record* — in
our own application. *(§4.2.)*

**R26 · `job.created` and `job.completed` are the surface the platform
already consumes, and their shape is constrained by an existing consumer.**
`EVT-Q4` is **CLOSED**: GuestOps consumes `job.created` today through the
real route — SDK consumer host, one durable per stream, `DeliverPolicy: New`,
ack-after-commit, idempotent on `event_id`. Our producer half is therefore
constrained, not free, and the payload must survive the SDK's snake-case
serialization (the round-trip defect `EVT-Q4` records as an open follow-on).
*(Register row 149.)*

**R27 · A reply to another application is an event carrying a correlation
id.** Room Care learns its job's id from `job.created`, and never by calling
Jobs. *(The constitution §6, citing ADR 0116 §5; see §7 for the citation
gap.)*

**R28 · An absent neighbour removes a capability and never blocks a flow.**
Jobs must open, assign, complete and close with Room Care, GuestOps,
Workforce and the Integration Hub all uninstalled. *(ADR 0116 §5, second
addendum.)*

**R29 · Cross-application relationships resolve through Context, never
through a join or a foreign key.** The reference's `guestReferenceId`,
`checklistId`, `inspectionId` and `referenceId` are opaque strings into four
other systems. *(§2.1; the constitution §5.)*

**R30 · If Jobs grants anything, it declares the grant kinds in its
manifest.** `AUTHZ-Q25` is built and closed: kinds are manifest-declared,
materialised by the Kernel at install from the manifest it already stores,
restricted to a grantable-relations registry, shown on the install approval
screen, folded from the event store on rebuild, and removed with the package.
Whether Jobs needs any is §7's question; that this is the only route is
settled. *(Register row 884.)*

### 6.6 · Authorization, tenancy and the surface

**R31 · Every read is property-scoped by the caller's session, not by a
request field.** The reference's single-record read has no scope at all, its
search takes `companyId` from the body, and the safe repository method exists
and is never called. *(F3.)*

**R32 · Every operation passes Kernel authorization; Jobs holds no
authorization cache and evaluates no policy of its own.** The reference
caches session validity for ten minutes in front of the authority that issues
it. *(F14; the constitution's never-cache-a-decision-in-front-of-its-
authority rule.)*

**R33 · Permissions are declared in the manifest and enforced per operation.**
The reference's `PermissionEvaluator` has two methods that return `true`
unconditionally and one that compares two client-supplied values.
*(`PermissionEvaluator:22,27,38`.)*

**R34 · Jobs ships no credential, no fallback identity and no bypass.**
*(F17.)*

**R35 · One operation, one typed contract.** Sixteen operations behind
`{kind, from, to}` — where `to` is variously an integer, a date, a boolean
and an id — is the reference's central API defect. *(F13.)*

**R36 · No untyped dictionary crosses any boundary.** Not as an event
payload, not as a template context, not as scheduler job data. *(F13.)*

**R37 · A failure is reported as a failure.** Never a 200 carrying an
exception message, never a swallowed consumer exception, never
`printStackTrace`, never an advanced schedule cursor after a creation that
did not happen. *(F2, F8.)*

**R38 · Logs carry no guest or staff contact details, and severity means
something.** *(F29.)*

### 6.7 · Structure and delivery

**R39 · Jobs is an installable bundle** — manifest, the `jobs` schema and its
migrations, the `job` event domain, permissions, declared grants, declared
subscriptions, and a signed `ui.module` on the 14-token contract. It ships,
starts and stops with the application. *(The constitution's
an-application-is-a-bundle-not-a-screen rule.)*

**R40 · Migrations are first-class from the first commit.** The reference has
an empty `db-migration/` and schema-by-inference, and the drift it produced
is visible in the tree. *(F27.)*

**R41 · Tests exist, in their own package, before sign-off.** *(F28; ADRs
0025, 0053, 0054.)*

**R42 · Modules follow ADR 0042 and the 300-line ceiling; a service does not
inherit its collaborators from an abstract base.** *(F30.)*

**R43 · One concept, one table.** Not two identical contact tables, two
satisfaction records, two follower lists, two numbering schemes and two
`PaginatedList` classes. *(F26.)*

### 6.8 · Deliberately not carried

This list is as load-bearing as the requirements. Each entry is something the
reference has that Jobs will **not** have, with the reason.

| Not carried | Why |
|---|---|
| **The `WORules` conditions/actions engine** | A general-purpose rules DSL stored as documents, with no callers, no validation (its `Actions.isValid()` is a `return true` under a TODO), and a `conditionJoinBy` field the evaluator ignores. Automation belongs to the platform's workflow layer (Chapter 11), not to an application's own interpreter. |
| **The `WOAttributes` tree** | A self-referencing hierarchical catalogue of services and items. This is master data wearing an application's clothes. If a hotel needs a service catalogue, that is a Master Data or Core Administration question, not a `jobs` table — and it is §7's fourth question. |
| **`facilityId`** | A third tenancy axis nothing sets. *(F31, R5.)* |
| **`work_order_feedback`** | The second of two guest-satisfaction records. One rating model. *(F26.)* |
| **`work_order_contact_info`** | The duplicate of the customer-info table, and both are guest-identity copies that Context answers. *(F26, R4.)* |
| **The device-as-assignee model** | `WODevice`, slot assignment, `AssigneeType.DEVICE`. A shared tablet is a *session*, not an assignee; the reference's own background flow throws on it. A job is assigned to a person or a team. *(F12.)* |
| **`executedById` as a field distinct from the assignee** | Two owner columns with no rule relating them, no history, and their own event. If more than one person works a job, that is what the time log is for. |
| **The five state booleans** | *(F10, R6.)* |
| **`ESCALATED` and `REMOVED` as statuses** | Escalation is a fact about a job, not a state of it — the reference declares both values and writes neither. Removal is ADR 0062's `deleted_at`. |
| **`EntityStatus` (ACTIVE / INACTIVE / CANCELLED / DELETED)** | ADR 0062 rules one lifecycle: `active` plus `deleted_at`, Deactivate/Reactivate, no third column and no archived state. |
| **Local-disk configuration and local-disk email bodies** | *(F5, F18.)* |
| **Emails delivered as a link to a rendered file** | *(F18.)* |
| **The `/wo/internal` surface** | An unauthenticated, partly-deprecated sibling API taking `companyId` as a query parameter. Internal callers use events and Context. *(F16.)* |
| **`TeamManagementController`** | User-administration requests raised as work orders into a hard-coded vendor tenant, with the requester's identity commented out. Whatever this solves, it is Core Administration's. *(§4.6.)* |
| **The string-map messaging contract** | *(F13, R36.)* |
| **Blocking request/reply over the message bus** | Three sites. Between applications a reply is an event with a correlation id, never a blocking call. *(§4.2, R27.)* |
| **Mongo as a second store for configuration** | One schema, `jobs`, in PostgreSQL. Configuration that varies by property is configuration, not a document database. |
| **The per-entity port/adapter/repository/mapper/service/controller septuple** | Ports are for boundaries the application actually has, not for every table. *(F30, ADR 0042.)* |
| **`NonsenseGenerator` and every other dead class** | *(F21.)* |

---

## 7 · Questions that need rulings

No `JOBS-Q` row exists in the register, so **every number below is unminted —
the architect assigns them.** Listed plainly, in the order they block work.

1. **There is no Jobs / Job Order chapter anywhere.** `ls docs/chapters/`
   returns 59 files and none is about this application; "Job Order" appears
   only as a named example in Chapters 21, 26 and 12 and in ADRs 0051 and
   0070. The constitution requires the chapter before the ADRs. Does the Jobs
   chapter get written (by the planner, as for the other applications), or is
   this survey plus its successor design page the design of record for Jobs?

2. **The correlation-id rule is cited to ADR 0116 §5 and is not in it.**
   `CLAUDE.md` §6 states *"between applications, a reply is an event carrying
   a correlation id — never a blocking request/reply call"* and cites ADR
   0116 §5; ADR 0116 §5 and its two addenda rule per-user gating,
   *unavailable is not denied* and *absent is not blocking*, and say nothing
   about correlation ids. Where is the correlation-id half frozen, and what
   field carries it on the envelope?

3. **What is a job's subject, and can it have more than one?** The reference
   has a free-text `location` plus separate `checklistId`, `inspectionId`,
   `guestReferenceId` and `referenceId`. Does a job reference exactly one
   Master Data entity — a room **or** an asset — or a set? A corridor light
   between two rooms, and a wing-wide job, both fall out of the
   single-reference answer.

4. **Who owns the service catalogue** — the taxonomy of what a job can be
   *about* (WiFi, AC, Cleaning), which the reference keeps as
   `WOServicePreference` with routing, SLA, priority and assignee attached?
   ADR 0051's test — *would this still describe what the entity is with every
   application uninstalled?* — says the taxonomy is not Master Data's, but it
   is read by Room Care and Maintenance too. Jobs', Core Administration's, or
   a shared catalogue?

5. **Does Jobs declare any authorization grant kinds?** `AUTHZ-Q25` is built
   and closed and its route is settled. The question is whether Jobs owns a
   relationship that must become a tuple — *this person is the assignee of
   this job* — or whether assignment access derives entirely from Workforce
   postings and the property membership that already exists. AUTHZ-Q20's own
   frame (*Jobs and Room Care are next in the same position*) says this is
   asked now, not at build.

6. **Is the SLA / escalation clock a Jobs concept or a platform one?** The
   Oracle round already ruled a property's time zone mandatory and never
   defaulted (its R16); Workforce needs a holiday calendar, which WF-Q16(a)
   placed in Core Administration; a hotel's SLA pauses at night and during
   closures. Does Jobs compute deadlines against a platform business
   calendar, or carry its own?

7. **What happens to an escalation whose deadline passed while the platform
   was down?** The reference has done both — fire-now, and drop — and neither
   records the outcome. R20 fixes the recording; the firing policy is a
   product decision.

8. **What does a job do when its room or asset is deleted or deactivated?**
   ADR 0062 makes deletion soft and rules that Master Data never queries an
   application's database to check references — Context answers, and Context
   is unimplemented and deliberately unstubbed. So a room can be deactivated
   under an open job today with nothing able to notice.

9. **Is preventive / scheduled work (PPM) Jobs', or its own application?**
   The reference's `WorkOrderSchedule` produces work orders on a cron. The
   constitution's installable list names **Maintenance** and **PPM** as
   applications distinct from Job Order, and ADR 0051's own table puts *"what
   maintenance does it need"* in Maintenance and *"what work is being
   performed"* in Job Order. Does Jobs schedule, or does Jobs receive
   `maintenance.due` and create?

10. **Does Jobs carry a guest-facing surface at all?** The reference has
    guest-raised jobs, guest acknowledgement, a public tracking timeline and
    a rating. GuestOps owns the guest. Is the guest's request a GuestOps
    object that produces a job by event, with Jobs holding no guest identity
    — and if so, where does the rating live?

11. **Is "escalation" a Jobs capability or a platform one?** Room Care,
    Maintenance and GuestOps will each want *"nobody acted within N minutes,
    tell someone senior"*. Building it inside Jobs makes the second
    application copy it; the no-duplicated-shared-code rule says anything two
    components must agree on lives in one place, and §3 of the constitution
    says build the platform capability first.

12. **What is the human-facing job number's scope and format?** R1 proposes
    per-property. The reference is per-company. A group reporting across
    properties and a property whose staff read numbers aloud want different
    answers.

---

## 8 · What this page deliberately does not contain

* **No design.** No schema, no aggregates, no event payloads, no screens, no
  slicing. The Oracle round's page 03 is that document, and it comes after
  the owner reads this one.
* **No decisions.** §7 is questions, not option menus; §6's requirements are
  proposals for the owner's gate, and every one is reversible until ruled.
* **No `JOBS-Q` numbers.** The register mints them.
* **No estimate of what the reference is worth commercially**, and no
  judgement of the people who wrote it. It ran a hotel group's operations;
  the findings are the price of that, and they are the most useful thing it
  gives us.
* **No claim about the reference's runtime behaviour.** Every statement here
  was established by reading source. There is no test suite and no running
  instance, so nothing was executed and nothing was observed. Where a finding
  says "unconditionally" or "never", it is a claim about the code as read, at
  the line cited.
* **No modification to the reference.** `Documents\HotelOs-References` was
  read-only throughout.

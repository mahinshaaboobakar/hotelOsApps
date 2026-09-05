# 01 · The Room Care reference survey

> **Stream KK, phase 1 — the survey only. Nothing here is built, and nothing
> here is a decision.** Phase 2 (the walkthrough with rulings) begins after
> the owner has read this.

---

## 0 · What this is, and the rules it was written under

The owner supplied a reference Java project — a housekeeping management
server that ran hotels, badly built, and doing several things that are not
housekeeping — and the method for reading it is Jobs' proven arc:

```text
survey  →  walkthrough with rulings  →  design chapter  →  mocks  →  owner gate  →  build
   ▲
 you are here
```

This page is the reading: what the reference does, what it stores, how a room
actually moves from *checked out* to *ready* inside it, what it talks to, and
where it is wrong — followed by the R-numbered requirements those findings
produce for **our** Room Care, the scope seams it forces us to name, and the
questions that need rulings. Concepts are taken; the design and the
architecture are ours.

### The naming law, applied — and it bites this application hardest

`APPS-Q3` (owner ruling, 2026-08-31, register row 147) makes the current
application name the name in code: **Room Care** — domain `roomcare`, schema
`roomcare` — and *"housekeeping" never names the app in code*. The same row
carries two refinements this page lives inside:

> **The rule renames applications, never departments or functions.** The canon
> **Housekeeping department** (ADR 0119, code `HK`) keeps its name forever —
> *Room Care is the app; Housekeeping is the department.*

> **A vendor's transcribed schema keeps the vendor's names.** A connector
> transcribes the source under the source's spellings and maps onto the
> platform's names at the boundary.

So **§1–§5 use the reference's own spellings throughout** — `RoomStatusInfo`,
`hkStatus`, `HOUSEKEEPING_SUPERVISOR`, `hk.task.create`, `tbl_room_status_info`
— because that is what the reference sends, and renaming a transcription would
make this page lie about the system it describes. §6 is the boundary: from
there on the names are ours. The map, stated once:

```text
reference                          ours
────────────────────────────────────────────────────────────────────────────
the application ("hk", "housekeeping")   Room Care — domain roomcare, schema roomcare
the department ("HK", HOUSEKEEPING)      the Housekeeping department, canon code HK (ADR 0119)
companyId                                organization_id       (ADR 0060)
siteId                                   property_id           (ADR 0060)
tbl_room · Room.id                       masterdata.room_id — never a roomcare.rooms
tbl_room_type                            masterdata.room_type_id
tbl_sector · Room.sectorId               masterdata.zone_id via RoomZoneAssignment (ADR 0044)
staffId / initiatedById (strings)        masterdata.staff_id, the caller from the platform
roomStatus / foStatus / hkStatus /       RoomState.condition · occupancy · room_care_status ·
  reservationStatus                        stay_statuses  (integration/v1 dto.proto:827–878)
CleaningProfile (enum)                   a cleaning kind, Room Care's own catalogue
WorkFlow (CLEAN, INSPECTION …)           a phase of a room task
```

### Where the citations point

Every `file:line` in §1–§5 is under
`Documents\HotelOs-References\reference\IdeaProjects\IdeaProjects\house-keeping-management-server\src\main\`,
relative to `java\co\instio\` unless the path starts with `resources\`. The tree
is read-only and nothing in it was modified. Line numbers are as read on
2026-09-05.

### What was read

The whole tree, not only `application/`. **751 Java files, 37,017 lines** —
33,465 under `co/instio/application/` and 3,552 in the shared layer beside it
— plus `pom.xml`, every file under `resources/`, and the two documents in the
repository root (`ALLOCATE_FLOW_DOCUMENTATION.md`, `prons_cons.md`). Those two
are generated prose about the code, not the code; where they were checked
against source they were right about the flow and wrong about a mechanism
(§5 F12), so nothing on this page rests on them. There is no `src/test`.

```text
co/instio/                     the shared layer — 3,552 lines
  fiegn/                       6 outbound HTTP clients, one of them Anthropic
  service/                     timezone · lock · scheduler · files · users · credit cache
  util/ jobs/ rmq/ misc/       date utilities, the Quartz job, two interceptors
  models/ dto/ enums/          the base entity, envelopes, the lifecycle enum
  mock/                        a lorem-ipsum generator and a seeder nothing calls
co/instio/application/         the business half — 33,465 lines
  agents/                      one file, 1,956 lines — the AI allocator (F13)
  modules/                     18 modules × {controller,dao,dto,entities,mapper,misc,services}
  services/ util/              the Quartz dispatcher, a forensic debug logger
  enums/ constants/            the status vocabulary, 52 table names
```

The shared layer produced a third of the findings, as it did for Jobs and for
the Oracle connector: the lock that does not lock, the cache that does not
clear, the timezone parameter that is ignored, the file endpoint and the
committed credentials all live outside the business package.

### The governing-document check, and what it found

`CLAUDE.md` (HosPilotOS) requires the chapter for what is being built to be
read before the ADRs, and `ls docs/chapters/` first because filenames repeat
numbers. Done, on 2026-09-05:

> **There is no Room Care / Housekeeping chapter.** `ls docs/chapters/` in
> `HosPilotOS` returns 59 files and none is about this application. The word
> appears as a named example of an installable application (Chapters 12, 21,
> 26; ADRs 0044, 0051, 0056) and as one v3 sequence diagram
> (`docs/architecture/42-housekeeping-completion.md`), never as a design. Jobs
> was in the same position and `JOBS-Q1(1)` ruled its locked walkthrough plus
> design page the design of record. **The same ruling is asked for here** —
> §8, first question.

Three conflicts between documents, reported and not resolved:

1. **The brief and `docs/working/48-the-room-care-round.md` name different
   references and a different first chapter.** Page 48 §3 points at
   `pms-integrations/…/modules/housekeeping/` — the *connector's* housekeeping
   module, already surveyed by the connector round — and §4 names deliverable 1
   `01-how-hotels-actually-clean.md`, a scenario study. The brief (architect,
   2026-09-05) names `house-keeping-management-server` — the application — and
   this survey as chapter 01, on the Jobs arc. Both trees exist. This page
   follows the brief, because it is the later instruction and names the tree
   that is actually the application; page 48 is the architect's to amend, and
   the owner's scenario study it describes is not abandoned — it becomes the
   walkthrough's spine (phase 2). Reported as a question in §8.
2. **`shared/protos/hotelos/events/v1/events.proto:95` illustrates
   `UPDATE roomcare.rooms`** as the ordering-guard example, inside a comment
   that four lines earlier says *"an example is where a vocabulary is learned,
   so one teaching the shape the ruling forbids is not harmless."*
   `CLAUDE.md` §"No duplicated master data" names `roomcare.rooms` as the
   wrong shape. An illustration is not a ruling (the constitution says so), and
   this page builds to the constitution; the comment is another stream's file
   (CC's, the events surface) and is **reported for that stream**, not edited
   here.
3. **The brief said "the 14-token contract"; page 64's change log records
   fourteen becoming seventeen** (`894e230`, the three `-soft` tints). The
   architect corrected this mid-round: the standard is the authority.
   Verified against the source rather than the page: `packages/sdk-typescript/src/tokens.ts`
   publishes **17 keys** — fifteen `color-*`, `radius-panel`, `font-sans`.
   Page 64 and the file agree.

One depiction to verify rather than build from: diagram 42 authorises
*"mark room cleaned"* against `masterdata.room.update_status`. ADR 0051 rules
that permission names did not move when ownership did, and the registry's
actual name for this capability is the walkthrough's to establish — the
diagram shows the transaction shape, which is what it is cited for below.

Read and relied on, with what each contributes:

| | |
|---|---|
| `CLAUDE.md` (HosPilotOS), whole | the bundle rule, no-duplicated-master-data, context-over-joins, events-in-the-caller's-transaction, no request/reply between applications (§6, the `EVT-Q3` ruling), no-native-queries, APPS-Q3 strict, the layering, the file rules |
| `docs/decisions/questions.md` | the register — **no `RC-Q` row exists**, so every question in §8 is unminted; `APPS-Q1` (row 308) carries the owner's charter for this round verbatim, quoted in §3.0 |
| **ADR 0044** · **0051** · **0056** · **0063** | ownership: `RoomZoneAssignment`, cleaning attributes and cleaning state are this application's; the room is Master Data's; **out-of-order state is Maintenance's**; zone→department is Workforce's. ADR 0044 already names the room-type configuration this app owns: *"deep-clean and checkout-clean minutes and whether inspection is required"* |
| **ADR 0119** | the department canon — `HK` Housekeeping, with `LDY` Laundry and `PA` Public Area under it; the reference's linen and public-area modules map onto those, not onto the app |
| **`RoomStateFact` / `RoomState`** (`integration/v1/dto.proto:827–878`) and `RoomCondition` (`common/v1/room.proto:43–60`) | the four-axis inbound truth: `occupancy` · `condition` · `room_care_status` · `stay_statuses` (a list) · `next_sold_at` · `is_pseudo_room`. **The reference has the same four axes** (§2.1) — independent confirmation that DD's fact is the industry's shape |
| **`HUB-Q4`** (row 145) | the Hub publishes `room.state_observed`; **`room.cleaned` is what Room Care publishes when it applies an observation**, ordered on the room's own version — the split between observing a state and owning it |
| **`CONN-Q11`** (row 243) | the revisit clause naming this round — §3.1 and §8 answer it on the reference's evidence |
| **`EVT-Q3`** (row 150) · `JOBS-Q1(2)` | between applications a reply is an event carrying a correlation id; Room Care learns a job's id from `job.created` — never by calling Jobs |
| **Workforce §3.8** (`workforce/docs/chapters/02`, owner 2026-08-31) · `WF-Q7` | zone assignment is a **Workforce posting**; Room Care reads *"who has zone 3"* through Context and never rosters people |
| **`CTX-Q4`** (row 294) · `RoomContext` (`context/v1/dto.proto:207–220`) | Context's `room_condition` is *"owned by domains not yet built; unset in v1"* — Room Care's round delivers the read view **and** the RPC, display-only |
| **`GetOperatingDay`** (`context/v1/service.proto:64–77`) · `FactHeader.operating_day` · ADR 0128 §6 | the business day rolls at night audit, is derived by Context and stored by nobody |
| `docs/working/64` (binding) · `docs/working/56` · `CORE-Q13` | the surface standard: 17 tokens, one `.btn`, top bar, bare-table lists, `common.v1` paged lists with a total; five widgets per app, each answering one question |
| `TEMPORAL-Q1` · `docs/working/62` | recurring futures are Temporal Schedules through `AddTemporal`; the ticker is the application's |
| `ADR 0130` | applications never name a model or a provider — the AI Runtime and Gateway decide |
| `jobs/docs/chapters/01`–`03` · `pms-oracle/docs/chapters/01`–`03` | the worked precedents this page is shaped after |

---

## 1 · The feature inventory — everything it does, however badly

47 REST controllers, ~266 endpoint methods, 33 RabbitMQ consumers, 6 outbound
HTTP clients, 4 `@Scheduled` methods, one Quartz job dispatching 8 event kinds,
3 datastores. Grouped by what a hotel would call it.

### 1.1 · Room state — the thing the application is about

| Capability | Where | State |
|---|---|---|
| A room-state record per room: `roomStatus` · `foStatus` · `hkStatus` · `reservationStatus` · `roomSpecialStatus` · `doNotDisturb` · a display `sequence` | `RoomStatusInfo.java:23–57` | live — **the four axes** |
| Change one axis, by a string discriminator (`RESERVATION` · `HK` · `FO` · `ROOM` · `DND` · `SPECIAL_STATS`) | `RoomStatusInfoServiceImpl.update:106–164` | live; unknown discriminator still publishes — F21 |
| Bulk change by filter, one row at a time | `RoomStatusInfoController.updateBulk:80–94` | live |
| Accept status changes from the PMS and from "core" (the desk) | `RoomStatusInfoBackGroundService:89–105` | live; **matched by room name** — F4 |
| Born CLEAN · VACANT · NOT_RESERVED, then **ask the PMS** for the truth | `.initDefaultsForRoomCreate:117–170` | live; a blocking bus call — F8 |
| Summary counts per axis | `.getRoomStatusSummary:187–196`, `RoomStatusInfoDaoImpl:110–141` | live (a dead native-query version beside it — F36) |
| "Current cleaning profile" for a room | `RoomCleaningStateServiceImpl.getCleaningStateFor:107–120` | live; returns `"CLEANED"` / `"INSPECTED"` / a profile name in one string |
| Push every room's state to "core" and to a hotel-inspection server | `RoomStatusInfoServiceImpl.syncToCore:296–340` | live; sends names, not ids |
| Rooms "for assignment" | `RoomStatusInfoController:127` | live |
| Service-status history per room per day (DND / sleep-out / refusal) | `RoomServiceStatusInfo` · `.updateRoomServiceStatusInfos:238–284` | live — **three DND days → a security work order** (§3.6) |

### 1.2 · Cleaning policy — the configuration surface

| Capability | Where |
|---|---|
| **Cleaning services** — day · turn-down · night, each with a service window, a real-time allocation window, an auto-trigger time, an auto-close time, `isInspectionNeeded`, `aiAgentAssignment`, a supervisor threshold, its own `timezone` | `CleaningServiceInfo.java:23–39`, `CleaningService.java:9–24` |
| **Cleaning profiles** — `SKIP` · `TOUCH_UP` · `LIGHT` · `FULL` · `DEEP` · `LONG_STAY`, each with default minutes, a code, and per property: which days of the week it runs, which rooms it is *strict* for, whether it is the default for departure / stay-over / sleep-out, whether a guest may choose it, whether it has video inspection | `CleaningProfile.java:5–11`, `CleaningProfileInfo.java:25–53` |
| **Profile phases** — which workflows a profile produces (`CLEAN`, `INSPECTION`, …), skippable or not, with a handler — kept in **MongoDB** | `CleaningProfilePhaseInfo.java:17–28`, `HouseKeepingManagementServerMongoConfiguration:14` |
| **Room-type preferences** — minutes, credits and a checklist id per profile, MIN/MAX | `CleaningPreferences.java:22–61` |
| On-demand (paid) cleaning per room type — days and a price | `OnDemandCleaningPref.java:18–27` |
| A guest's chosen profile for a room on a date, with notes | `GuestHousekeepingInfo.java:19–45` |
| **The decision** — given a room's four axes, the time and the day, which profile and which phases | `RoomCleaningStateServiceImpl:77–103, 123–229, 266–281` — two implementations, F20 |

### 1.3 · The room task and its phases

| Capability | Where |
|---|---|
| A task per room per day, with a per-company running number, a location (room or area), a profile, priority, comments, followers, `startFrom`, `timezone`, `allowParallelRunWorkFlow` | `Task.java:25–69`, `Location.java:24–50` |
| Ordered **workflows** on the task: `CALL_FOR_INSPECTION` · `CHECK` (deprecated, still used) · `PRE_CLEAN` · `CLEAN` · `POST_CLEAN` · `INSPECTION`, each with an assignee, an associate, SLA minutes, credits, start/end, images, a `crmId`, an extra-time request | `WorkFlows.java:5–11`, `WorkFlow.java:23–77` |
| Workflow lifecycle `PENDING → ACTIVE → IN_PROGRESS ⇄ PAUSE → COMPLETED / SKIPPED / CLOSED`, activated in `flowOrder` by "calibration" | `Status.WorkFlow:106–125`, `WorkFlowServiceImpl.calibrateWorkFlow:293–354` |
| Task lifecycle `PENDING · ACTIVE · IN_PROGRESS · COMPLETED · REMOVED` | `Status.Task:10–20` |
| A **second copy** of all of it — task status, four workflow statuses, service status, room state before and after service | `TaskStatusInfo.java:24–58` — F22 |
| The attendant's timer — start · pause · resume · end · cancel, with reasons; ending completes the workflow | `TimeLogServiceImpl:104–304` |
| Task-level service status `NONE · SLEEP_OUT · SERVICE_REFUSAL · DND` | `Status.Service:127–136`, `TaskServiceImpl.updateServiceStatus:201–227` |
| Priority `LOW 1 · MEDIUM 5 · HIGH 10`, derived from reservation status | `Priority.java`, `TaskBackGroundService.getPriorityForTask:494–501` — F16 |
| Notes with mentions; a from→to **activity log** per change (`CREATED · STATUS · ASSIGNED · TIMER_START …`) | `Notes.java`, `TaskActivityLog`, `Activity.java` |
| **Extra-time request** — an attendant asks for more minutes with evidence photos; a supervisor approves or rejects; the CLEAN SLA is rewritten | `TaskWorkflowExtraTimeRequestInfo.java:20–40`, `TaskBackGroundService:135–220` |
| Rating of a task by a guest reference | `Rating.java` |
| Checklist / inspection instance ids on a workflow (`CIX`) | `CIXInfo.java`, `CIXController` |
| A work-order link from a workflow to an external work order | `WorkOrder.java:19–39`, `WorkOrderController` |
| Change a task's profile after creation — reshapes its workflows and re-accounts credits | `TaskServiceImpl.updateCleaningProfile:156–199`, `WorkFlowBackGroundService.taskCleaningProfileUpdated:182–263` |
| Remove a task (only while PENDING — *"Can't Remove RA attended rooms"*) | `TaskServiceImpl:293–295` |

### 1.4 · Assignment — five mechanisms in one service

| Mechanism | Where | State |
|---|---|---|
| **Criteria** — a staff member is pre-assigned to a `ROOM` or a `SECTOR`, refreshed each service start from the roster's availability | `AllocationCriteria.java`, `WorkersScheduleServiceImpl.allocateRoomInfoBasedOnCriteria:344–373`, `HKScheduleExecutorService:174–196` | live |
| **Workflow criteria** — a fixed assignee per workflow type per profile (deep/light/full/touch-up columns) | `WorkFlowCriteria.java:9–21` | live |
| **Continuity** — the attendant who deep-cleaned a room yesterday and left it dirty gets it again | `AllocationServiceImpl.findIfAnyCleaningContinuityFor:105–146` | live, bracketed wrong — F15 |
| **Affinity + strategy** — workers on shift for the current service, budget-checked (half-day only), then a provider: lowest SLA load · most work in the sector · fewest high-priority works | `WorkersAffinityServiceImpl.getWorkersForAffinity:114–135`, `CustomExecutor:20–101`, `InstioDefaultExecutor1/2` | live; skills never consulted — F14 |
| **AI** — room list + availability to a model, which returns assignments with ETAs and `additional_housekeepers_needed` | `RoomAssignmentDecisionAgent.assist:1899–1954`, `WorkersScheduleServiceImpl.aiAllocate:213–277` | live — F13 |
| Manual assignment and reassignment (only while PENDING) | `WorkersScheduleServiceImpl.update:151–161`, `WorkFlowServiceImpl.updateAssignment:243–270` | live |
| Self-service: an attendant's own task list, ongoing-work check | `TimeLogController`, `TimeLogServiceImpl.check:332–335` | live |

### 1.5 · The daily run

| Capability | Where |
|---|---|
| `POST /workers_schedule/auto-assign` with `ALLOCATE · AI_ALLOCATE · RE_ALLOCATE · CLEAR_ALLOCATE` | `WorkersScheduleController:103–129` |
| A log entry per run with a correlation id, `total` / `completed` counters, and an **SSE progress stream** | `WorksScheduleLogEntry`, `LogEntrySseService`, `WorkersScheduleController:131–147` |
| A 10-minute duplicate-run guard | `WorkersScheduleServiceImpl.validate:164–180` — F17 |
| Auto-trigger at each service's `autoTriggerAt`, from Quartz | `HKScheduleExecutorService.runServiceAutoTrigger:200–214` |
| Service-start sync: attendance from the roster service → `WorkersStats`; availability → allocation criteria | `.runServiceStartEssentialCheckSync:144–198` |
| Per-attendant day record and metrics (working / allocated / worked minutes; allocating / allocated / achieved credits) | `WorkersStats.java`, `WorkerMetrics.java`, `WorkerMetricsServiceImpl` |
| Force-close every open task at **19:00 UTC** | `TaskBackGroundService.closeUncompletedTaskAndWorkFlow:363–405` — F3 |
| A deprecated manual auto-close by date | `TaskController:71–79` |

### 1.6 · Supervisor alerts and guest-facing behaviour

| Capability | Where |
|---|---|
| *Room ready for inspection* → supervisors, on CLEAN completing before INSPECTION | `WorkFlowBackGroundService:346–373` |
| *Cleaning completed too quickly* (5 min after start) and *cleaning delayed* (SLA + 5) → supervisors, via Quartz | `WorkFlowBackGroundService:385–403`, `HKScheduleExecutorService:217–281` |
| Extra-time request → supervisors, with a 10-minute reminder; decision → the attendant | `TaskBackGroundService:179–220, 135–177` |
| Assignment, priority and note changes → the assignee | `TaskServiceImpl:249, 339`, `WorkFlowServiceImpl:265–268` |
| A guest's cleaning request → **a work order in another system** | `GuestHouseKeepingBackGroundService:38–70` — F33 |
| Three DND days in a row → a *DND-Security Alert* work order | `RoomStatusInfoServiceImpl:251–282` |
| Arrival ETA per room from "core", used for priority and for the AI | `CleaningScheduleMapper:89–101`, `TaskBackGroundService:243–253` |

### 1.7 · Its own master data

Rooms (name, code, type, sector, bed type, stairs), room types with floor
plans, sectors, and **connecting-room groups** — created, updated and deleted
here, with upsert-by-name (`RoomServiceImpl:22–39`, `RoomConnectionServiceImpl`,
`Sector.java`, `RoomType.java`, `FloorPlan.java`). This is `roomcare.rooms` —
F5. Connecting rooms are captured and consumed by nothing (F38).

### 1.8 · Public areas — a second task model

Areas with a sector and a cleaning schedule (time ranges), area cleaning
tasks with instructions, area inspections, area time logs
(`Area.java`, `AreaCleaningSchedule.java`, `AreaCleaningTask.java`,
`AreaInspectionInfo.java`, `AreaTimeLog.java`). A parallel `Task` for a
different location type, with its own controllers, DAOs and mappers; the
schedule's Quartz handler has empty cases (F28).

### 1.9 · Room supplies — five parallel families

| Family | Catalogue | Per room type | Per room | Attendant cart | Movement |
|---|---|---|---|---|---|
| Amenities | `Amenity` (with a tutorial video URL) | `AmenityForFloorPlan` — counts per profile | `RoomConsumable` | `AmenityRACart` taken/returned | — |
| Linen | `Linen` | `LinenForFloorPlan` | `LinenRoomConsumable` | `LinenRACart` | — |
| **Minibar** | `MinibarItem` (DRINK · SNACK · ALCOHOL · OTHER, price) | `MinibarRoomTypePreference` par level | `RoomMinibar` stock | — | `MinibarConsumption` (with `totalPrice`, `crmId`) · `MinibarRefill` |
| Checklists | `Checklist` | `ChecklistForFloorPlan` — booleans per profile | — | — | — |
| Inspections | `Inspection` (action/drop if failed) | `InspectionForFloorPlan` — booleans per profile | — | — | — |

The same shape five times, each with `forDeepCleaning · forFullCleaning ·
forLightCleaning · forTouchUpCleaning` and **no long-stay column** — F19. The
minibar is the scope seam §7 is about: it records a priced consumption per
guest and posts it nowhere (F32).

### 1.10 · The rest

Image upload and an unauthenticated file fetch (`GeneralController:13–41`),
a lost-and-found status enum with no entity, a `PublishInterceptor` that is
an empty class, a receiver interceptor that would null every message if it
were wired (`ReceiverInterceptor:11–15`), eight `main()` methods in
production classes, a lorem-ipsum generator and a seeder nothing calls.

---

## 2 · The data model, transcribed under its own names

52 table names in `DbConstants.java`; MySQL for all of them, MongoDB for two
collections, a third MySQL database for Quartz. Every entity extends
`AbstractTransactionalEntity` (`models/`): an auto-increment `id`, a
`@Version`, an `EntityStatus` (`ACTIVE · INACTIVE · CANCELLED · DELETED`),
`createdOn`, `updatedOn` — and then its own `companyId` / `siteId` strings,
where a null `siteId` means *company-wide*.

### 2.1 · Room state — `tbl_room_status_info`

```text
RoomStatusInfo                             the reference's four axes    ours (RoomState)
  roomId → tbl_room                                                      room_id → masterdata
  roomStatus   DIRTY CLEAN INSPECTED        the room's condition         condition
               PICK_UP OUT_OF_ORDER
               OUT_OF_SERVICE
  foStatus     OCCUPIED VACANT              front-office occupancy       occupancy
  hkStatus     OCCUPIED VACANT SLEEP_OUT    housekeeping's own reading   room_care_status
               SERVICE_REFUSAL DND
  reservationStatus                         ONE value                    stay_statuses — a LIST
               ARRIVAL ARRIVED STAY_OVER
               DAY_USE DUE_OUT DEPARTED
               NOT_RESERVED
  roomSpecialStatus                         NOTHING SLEEP_OUT TOUCH_UP   (no counterpart — policy input)
               STRIP_LINEN VACANT_REFRESH
               SPECIAL_REQUEST LONG_STAY
  doNotDisturb Boolean                                                   (no counterpart)
  sequence     count(*)+1 at creation       a display order
                                                                         next_sold_at — absent here
                                                                         is_pseudo_room — absent here
```

Read against `RoomState` (`integration/v1/dto.proto:827–878`): the four axes
match one for one, which is the most useful thing the reference confirms. The
differences are the wire's improvements — a **list** of stays where the
reference holds one value, `next_sold_at` where the reference derives priority
from a status, and `is_pseudo_room` where the reference has nothing. What the
reference adds is the **special status** and **DND** — policy inputs a PMS
does not carry — and it adds them four ways over (F19).

`RoomServiceStatusInfo` (`tbl_room_service_status_info`) is the per-day history
of DND / sleep-out / refusal per room, with the `crmId` of the guest and the
work order it raised, if any.

### 2.2 · The task aggregate — `tbl_task` and its satellites

```text
Task ─┬─ Location        type ROOM|AREA · locationId · name · sectorId · sectorName · profile · profileId
      ├─ WorkFlow[]      type · flowOrder · assignedToId · associateById · sla · credits ·
      │                  startTime · endTime · spendTimeInMinutes · resolutionTimeInMinutes ·
      │                  performanceTimeInMinutes · images[] · crmId · workFlowStatus · extraTimeRequestId
      ├─ TaskStatusInfo  (same id) taskStatus · callForInspectionStatus · inspectionStatus · cleanStatus ·
      │                  checkStatus · serviceStatus · fo/room/special status BEFORE and AFTER service
      ├─ TimeLog[]       taskId · workFlowId · start · end · reasons · timeInMinutes · executedById
      ├─ TaskActivityLog[]  activity · from · to · comment · initiatedById
      ├─ Notes[]         note · mentions[]
      ├─ TaskWorkflowExtraTimeRequestInfo  requested/approved minutes · evidenceImages[] · status
      ├─ CIXInfo         checklistId · inspectionId · inspectionNumber · inspectionStatus
      ├─ WorkOrder       workOrderId · workFlowId · strictSolve · taskStatus · workOrderStatus (strings)
      └─ Rating          rating · comment · staffId · reId
```

`Task.taskNumber` is `max + 1` per company (`TaskRepository:17–18`); `Task.date`
is `LocalDate.now(zone)` at creation — the calendar day, not the business day.
`Task.assignedToId` is a copy of the CLEAN workflow's assignee, kept in step by
three separate code paths (`WorkFlowBackGroundService:341–344`,
`WorkFlowServiceImpl:259–262`, `TaskServiceImpl:256–280`).

### 2.3 · Policy

`CleaningServiceInfo` (§1.2) · `CleaningProfileInfo` · `CleaningProfilePhaseInfo`
(Mongo: `cleaningProfileId`, `workFlow`, `allowSkip`, `handler`, `handlerValue`,
`phaseAttributes[]`) · `CleaningPreferences` (`tbl_room_type_cleaning_profile_info`:
five `minutesFor*`, five `creditsFor*`, five `checklistIdFor*`) ·
`OnDemandCleaningPref` · `GuestHousekeepingInfo`.

### 2.4 · Workforce-shaped

`WorkersStats` (date · staffId · `WorkerShift` DAY|NIGHT · `Status.Workers`
WORKING|NOT_WORKING|LEAVE · `WorkerSession` FULL_DAY|HALF_DAY · role) ·
`WorkerMetrics` (nine running totals) · `Affinity` (skills[], two over-work
booleans) · `AllocationStrategy` (Mongo: strategy MIN_TIME|MIN_CREDIT,
`strategyValue`, `dropIfOutOfBounds`, `provider`, `override`,
`requiredContinuityCheck`, `criterias[]`, `workFlowCriterias[]`) ·
`WorksScheduleLogEntry`. `WorkerSession` hard-codes the capacities: full day
= 8 h, 15 credits, 40 min; half day = 4 h, 8 credits, 30 min
(`WorkerSession.java:6–7`).

### 2.5 · Its own master data

`Room` (name, description, code, `roomTypeId`, `sectorId`, `numberOfStairs`,
`roomConnectionId`, `bedType`) · `RoomType` with `FloorPlan[]` · `Sector` ·
`RoomConnection` (a named group of rooms) · `Area` (with a sector, a schedule
and a checklist id). Every one carries `companyId` / `siteId` and the
four-value lifecycle.

### 2.6 · Supplies

The five families of §1.9 — 17 tables.

### 2.7 · What the model says about itself

* **Tenancy is two nullable strings on every row**, and "site null means
  company-wide" is a convention two DAO methods honour and nothing enforces
  (`RoomStatusInfoDaoImpl:89–96`, `RoomStatusInfoRepository:19–23`).
* **Identity is a global auto-increment**; a room's identity for the PMS and
  for the guest-request API is its **name** (F4).
* **Optimistic locking exists** (`@Version`) and is defeated by the bulk-update
  JPQL that bypasses it (F23).
* **The lifecycle is four values** where ADR 0062 rules two columns.
* **Schema by inference** — `spring.jpa.generate-ddl=true`, no migrations, the
  seed SQL commented out and `continue-on-error=true` (`resources/application.properties:6, 12`; `data.sql`).
* **Two enum families for one concept** — `Status.HK` and `Status.Service`
  both hold `SLEEP_OUT · SERVICE_REFUSAL · DND`; `RoomSpecialStatus` holds
  `SLEEP_OUT` a third time; DND is also a Boolean and a history row (F19).

---

## 3 · The workflows, end to end

### 3.0 · The charter this is read against

The owner's direction for this round, verbatim from `APPS-Q1` (row 308,
2026-08-31):

> *"not every hotel follows instant cleaning; checkout marks the room dirty,
> and if no guest arrives into it today it has no priority and is cleaned
> tomorrow, or by a custom click action — there are so many special conditions
> in hotels"* — **a checked-out room becoming a task is a hotel policy, never
> an automatic consequence.**

And from page 48 §1: *"Room Care is owner of different room statuses"*; *"when
someone checks out from a room — need to clean and assign someone based on
Workforce."* The reference agrees with the first sentence and contradicts the
second and the third, as §3.1 and §5 F1 show.

### 3.1 · A room becomes dirty — and may or may not become work

```text
PMS / desk                       RoomStatusInfoBackGroundService
  pms.room-status.change  ──▶    .initDefaultsForPMSRoomStatus:210–222
  core.room-status.change          room looked up BY NAME if no id   ── F4
                                   RoomStatusInfoServiceImpl.update:106–164
                                     one axis set · bulk UPDATE · entity also dirty  ── F23
                                     publish core.room-status.update   (names)
                                     publish hk.room-status.change     (id)
                                 .initDefaultsForRoomStatus:224–241
                                   is NOW inside a service's realTimeAllocation window?
                                     no  → log "EXITING" and DROP                ── F2
                                     yes → publish hk.room-cleaning.check
CleaningScheduleBackGroundService.intiDefaultsForRoomCleanCheck:49–82
  determineCleaningEligibilityAndProfile(room, today, now)
    which service window covers NOW?  (18:00→07:00 can never match)   ── F2
    DAY_SERVICE:  special status set → the special-status decision
                  DIRTY            → the day-service decision
    TURN_DOWN:    OCCUPIED or ARRIVAL → TOUCH_UP
    NIGHT:        (unreachable)
    else null → log at ERROR, no task, nothing recorded
  phases for the profile (Mongo, or the built-in CLEAN + INSPECTION default)
  publish hk.task.create  (TaskCreateRequest, priority 10)
TaskBackGroundService.initTask:418–492
  SKIP profile → return · no profile config → return
  an open task for this room today? → re-prioritise it, maybe add workflows
  older open task? → close it, recurse
  else TaskServiceImpl.create → workflows created @Async            ── F10
       priority from reservationStatus alone                          ── F16
```

Two things the reference gets **right** here, in the sense the owner means:

* **Cleaning is decided by policy, not by the checkout.** The status change
  is an input; a configurable service window, a day-of-week profile
  calendar, per-room strict profiles and a guest's preference decide whether
  and how the room is cleaned. That is the charter's shape.
* **The decision is per service** — a room is looked at differently in the
  morning (departure vs stay-over), in the evening (turn-down for occupied
  and arriving rooms) and at night.

And two ways it gets the same idea **wrong**:

* A room that changes state *outside* the window is not deferred, it is
  **discarded** — no record, no "cleaned tomorrow", no custom click. The
  owner's *"cleaned tomorrow, or by a custom click action"* is exactly the
  case the reference drops (F2).
* The policy is evaluated at message-consumption time against `LocalTime.now`,
  so the *same* checkout produces a task or nothing depending on queue lag.

**On `CONN-Q11`.** The reference's policy consults arrival-ness in two
places: `DAY_SERVICE` treats `DEPARTED` and `DUE_OUT` as departure-clean and
`STAY_OVER` as stay-over-clean (`RoomCleaningStateServiceImpl:138–145`), and
priority is `ARRIVAL → HIGH · STAY_OVER → MEDIUM · else LOW`
(`TaskBackGroundService:498–500`). Both are answerable from the wire as
built: `stay_statuses` is a **list** with `DUE_OUT` and `CHECKED_IN` as
distinct values (`dto.proto:291–311`), and `next_sold_at` carries the *sold
tonight* fact the reference lacks. Nothing in the reference needs a room-level
arrival flag that the stay list plus `next_sold_at` cannot compose. **This
survey does not ask to reopen CONN-Q11**; the walkthrough re-tests that against
the owner's scenarios before it is final (§8).

### 3.2 · The morning run — `ALLOCATE`

```text
POST /workers_schedule/auto-assign {ALLOCATE, shift, date, companyId, siteId}
  validate: a log entry in the last 10 minutes with total==0 or total!=completed → refuse   ── F17
  WorkersScheduleServiceImpl.allocate:183–210
    no WorkersStats for the date → log, save "No Workers stats found", return
    LocalStorageService.clearAllAllocationCreditInfos()   → cleanUp(), clears nothing  ── F12
    every active RoomStatusInfo for the property
    allocation criteria (Mongo) → for each ROOM/SECTOR criterion whose staff is WORKING
       publish hk.room-cleaning.check for the matching rooms, remove them from the list
    log entry: total=0, completed=0                                                   ── F17
    publish hk.room-cleaning.check for the rest, priority 10, correlation id = log entry
→ §3.1 from the check onwards, once per room
→ each task's CLEAN workflow → hk.workflow.assignment "{taskId}-{workFlowId}"           ── F9
```

`RE_ALLOCATE` republishes assignment for every PENDING task's workflows;
`CLEAR_ALLOCATE` publishes *remove-assignment* for all of them, **sleeps
20 seconds in the request thread**, then publishes assignment again
(`:298–327`, F18). `AI_ALLOCATE` is §3.3's last row. The **auto-trigger** does
the same `ALLOCATE` from Quartz at each service's `autoTriggerAt`, having first
pulled attendance and availability from the roster service and rewritten the
criteria (`HKScheduleExecutorService:144–214`).

### 3.3 · Assignment

```text
hk.workflow.assignment  →  WorkFlowBackGroundService.autoTaskAssignment:86–102
  synchronized + lockService.lock(key)                                        ── F11
  initWorkFlowAssignment:535–563
    related workflows (PRE/POST_CLEAN ↔ CLEAN): one → copy its assignee; more → "Not implemented !!"
    CLEAN → AllocationServiceImpl.allocate:64–103
       continuity check (if strategy says so)                                 ── F15
       workflow criteria (fixed assignee per type per profile)
       affinity: workers on shift for the service covering NOW,
                 FULL_DAY always eligible; HALF_DAY only if budget remains;
                 nobody → all FULL_DAY workers                                ── F14
       statistics per worker (today's workflows, SLA sum, credits, sector works)
       provider: lowest SLA · most sector works · fewest high-priority + lowest SLA
       → a worker id, or null
    INSPECTION → nothing
    updateAssignment(taskId, wfId, id|null) — null is "no change", silently   ── F15
    calibrateWorkFlow(taskId) — activate the first PENDING in flowOrder
  hk.workflow.assignment.change "{wfId}-{from}-{to}" → credits/minutes moved between workers
```

**AI_ALLOCATE** builds one map per eligible room (four axes, profile,
minutes, arrival ETA — the ETA by a **blocking bus call per room**,
`CleaningScheduleMapper:89–101`) and one per available attendant (from the
roster service), sends both to a model with a prompt that encodes the whole
priority policy in English — *checkout & vacant → arrival (ETA before
arrival) → stay-over → checkout & occupied if time allows; same sector to the
same person; 10-minute buffer; start 9:00 AM* — and creates a task per
returned assignment (`RoomAssignmentDecisionAgent:30`, `WorkersScheduleServiceImpl:213–277`).
The prompt is the clearest statement of the reference's intended priority
order anywhere in the code, and it lives in a string (F13).

### 3.4 · The attendant's day

```text
start   TimeLogServiceImpl.start:104–167
          initiatedById == task.assignedToId  (the only authorization check in the service; both client-supplied) ── F6
          service status reset to NONE · workflow → IN_PROGRESS · task → IN_PROGRESS
          hk.workflow.status.change → startTime = server clock
                                     → Quartz: quick-clean check in 5 min, overdue at SLA+5
pause   .pause:170–210    endTime · timeInMinutes accumulated · reason PAUSE   (workflow stays IN_PROGRESS)
resume  .resume:212–243   ends the old log, opens a new one carrying the total   ── F24
end     .end:246–304      endTimer → timeInMinutes = LAST SEGMENT ONLY          ── F24
          worked minutes credited only if the task was created today             ── F24
          workflow → COMPLETED
hk.workflow.status.change → WorkFlowBackGroundService.workFlowStatusUpdated:315–449
  COMPLETED: stop timers · endTime · spend/resolution/performance minutes · achieved credits
             taskStatusInfoService.updateRoomStatusAfterService(task)
               @Async · Thread.sleep(5000) · snapshot whatever the PMS says the room is   ── F1
  calibrate → next PENDING becomes ACTIVE
     INSPECTION becomes ACTIVE only if room-after-service == CLEAN; else CLOSED  ── F1 (it never is, from here)
     "Room ready for inspection" → supervisors
  no workflows left → task COMPLETED · unassigned · after-service snapshot again
```

**Nothing in this flow writes the room's condition.** The only writers of
`roomStatus` are the PMS/desk change consumer (`RoomStatusInfoServiceImpl:140`),
room creation (`CLEAN` by default), and two *inspection-closed* handlers that
write `INSPECTED` (`CIXBackGroundService:50`, `WorkOrderBackGroundService:71`).
A completed CLEAN workflow waits five seconds and records what the PMS thinks.
The housekeeping system does not mark rooms clean — the PMS does, and it is
the front desk's screen that tells it to (F1).

### 3.5 · Inspection

Three routes, none of which is the INSPECTION workflow doing it:

* **CIX** — an external checklist/inspection instance is created against a
  workflow; its status arrives on `cix.inspection.status`. `COMPLETED` marks
  the workflow COMPLETED and is then **treated as `PASSED`**, which writes the
  room `INSPECTED`; `FAILED` and `IN_REPAIR` do nothing
  (`CIXBackGroundService:24–54`, F26).
* **A work order** closing against an INSPECTION workflow writes `INSPECTED`
  and completes the workflow — looking the task up by the **work order's own
  id** (`WorkOrderBackGroundService:60–75`, F25).
* **The hotel-inspection server** receives every room master change and
  every state sync by HTTP (`HotelInspectionClient`), and nothing comes back.

### 3.6 · Service exceptions — DND, sleep-out, refusal

`hk.task.service.status` → `TaskBackGroundService.taskServiceStatusUpdated:222–289`:
`SLEEP_OUT` and `SERVICE_REFUSAL` **complete** the task and its workflows and
record a `RoomServiceStatusInfo` for the day; `DND` records the day and, if
the last three DND days for the room (or the guest, by `crmId`) are consecutive
and no work order was raised for that span, raises **a *DND-Security Alert*
work order** by a blocking bus call (`RoomStatusInfoServiceImpl:251–282`);
`NONE` reverts — reactivates workflows, deletes the day's record, clears the
DND flag or restores the pre-service room status. Starting a timer also
resets service status to `NONE` (`TimeLogServiceImpl:154–157`). The
three-strikes rule is real housekeeping practice and worth carrying (R20).

### 3.7 · A guest asks for cleaning

`POST /guest_housekeeping` (room by **name**) → `GuestHousekeepingInfo`
(profile, date, notes) → `hk.guest.housekeeping.create` → **a work order in
the work-order system**, by a blocking call, with a comment whose first
placeholder prints the guest's notes where the room should be
(`GuestHouseKeepingBackGroundService:44–66`, F31, F33). Separately, the
policy decision reads `GuestHousekeepingInfo` to pick the profile
(`RoomCleaningStateServiceImpl:131–133`) — but not when the room has a
special status (F20). Two mechanisms for one request.

### 3.8 · The day ends

At **19:00 UTC**, for **every property in the database at once**, every
`PENDING` or `IN_PROGRESS` task is closed: IN_PROGRESS with any ended time log
becomes COMPLETED, everything else CLOSED, the task COMPLETED and unassigned,
the after-service snapshot taken (`TaskBackGroundService:363–405`). A hotel in
Kolkata loses its evening turn-down at 00:30; one in Dubai at 23:00; one in
London in the middle of the evening shift (F3). Each service also has an
`autoCloseAt` (`CleaningServiceInfo:32`) — set by configuration and **read by
nothing** (grep, 2026-09-05).

### 3.9 · The minibar — the seam, walked

`POST /mini-bar-refill` (a list): each refill is saved; if it carries a
`crmId` a **consumption** is created from it — *what was refilled is what
the guest consumed* — with a `totalPrice` (`MinibarRefillServiceImpl:45–63`).
Then nothing. No message leaves (§4.1's table), the room's stock
(`RoomMinibar.quantity`) is never changed (every stock-adjusting block is
commented out, `MinibarConsumptionServiceImpl:51–55, 86–93`,
`MinibarRefillServiceImpl:56–60`), and the par level per room type is
consulted by nothing. The attendant's act — *"I refilled two waters in 214
for the guest in it"* — is genuinely a Room Care act; the price, the guest's
folio and the stock are three other domains' facts, and the reference kept
all four in one table. §7.

---

## 4 · Integration points

### 4.1 · RabbitMQ

**Exchanges bound:** `hk` (27 queues), `core` (1), `pms` (1), and two whose
names look like routing keys — `cix.inspection.status` (2), `wo.status.change`
(1). **Exchanges published to:** `hk` (41 sites), `cm-d` notifications (10),
`core` (7), `rst` roster (4), `wo` (2), `pms` (1). The RMQ configuration
declares `hk · re · wo · cm · pms` (`HouseKeepingManagementServerRMQConfiguration:37–60`)
— `re` is never used; `cm-d` and `rst` are used and not declared here.

**Blocking request/reply over the bus — eight live sites**, three of which
swallow the failure:

| Site | Asks | On failure |
|---|---|---|
| `RoomStatusInfoBackGroundService:131` | the PMS for a new room's state | logged |
| `RoomStatusInfoServiceImpl:272` | the work-order system to create the DND alert | propagates into the consumer's `printStackTrace` |
| `GuestHouseKeepingBackGroundService:59` | the work-order system to create the guest request | logged |
| `TaskBackGroundService:248` | "core" for the room's arrival / guest (`crmId`) | **ignored** |
| `CleaningScheduleMapper:94` | "core" for arrival ETA — **once per room, in a loop** | **ignored** |
| `CleaningScheduleMapper:136` | the roster for availability | none — propagates |
| `HKScheduleExecutorService:158` | the roster for attendance | none — propagates |
| `HKScheduleExecutorService:184` | the roster for availability | none — propagates |

**Payloads:** typed DTOs for creates; a bare `Long` for most ids; **hyphen-joined
strings** for assignment (`"{taskId}-{workFlowId}"`, `"{wfId}-{from}-{to}"`,
`"{taskId}-{PROFILE}"`); `HashMap<String,String>` with `_taskId` / `_statusTo`
keys for status changes (F9). The message converter **logs every payload at
ERROR and deserialises it twice** (`HouseKeepingManagementServerRMQConfiguration:22–35`).
Every one of the 33 listeners wraps its body in `try { … } catch (Exception e)`
— `printStackTrace`, a log line, or nothing — so a failed message is
acknowledged and gone (F29).

### 4.2 · Outbound HTTP (OpenFeign) — 6 clients

`PropertyInfoClient` (company/site info → timezone, cached 15 min in **static
fields of the interface**), `UserClient` (profile, department, users in a
position — `HOUSEKEEPING_SUPERVISOR`, `HOUSEKEEPING_STAFF`), `SessionClient`
(token validation — **declared, never called**), `GuestManagementClient`
(past-stay guest info), `HotelInspectionClient` (room sync), and
`AnthropicClient` (`/v1/messages`, with a key in the source — F7). Feign read
timeout **600 s** (`application.properties:47`).

### 4.3 · Quartz

A JDBC job store in a **separate `qrtz` database** with its own credentials
(`resources/quartz.properties`). One job class, `SimpleScheduleJob`, carrying
a `HashMap` with an `_event` key, dispatched by `HKScheduleExecutorService.run`
through a **static singleton** (`SimpleJobFactory` cannot inject). Eight event
kinds; three do nothing (F28). Two `@Scheduled` methods live (the 19:00 UTC
close, and an orphan check whose body is a comment).

### 4.4 · Files

Uploads to `${HOME}/server/instio-hk-server/`, evidence images copied under
`resources/<companyId>/images/`, and an **unauthenticated `GET /general/fetch?filename=`**
that concatenates the caller's string onto a base directory with no
normalisation and no containment check (`UploadResourceHelper:130–143`, F34).

### 4.5 · Datastores and caches

MySQL (52 tables, DDL by inference) · MongoDB (`cleaning_profile_phases`,
`task_allocation_strategy`) · MySQL again for Quartz · Caffeine caches at 5–15
minutes over room status, profiles, services, strategy and working workers
(`HouseKeepingManagementServerConfiguration:58–69`) · Guava caches in static
fields for credits and a 2-minute "assignment" cache whose **eviction** is the
trigger for ETA calculation (`LocalStorageService:22–28`, F28).

### 4.6 · The one integration that is a finding by itself

`RoomStatusInfoBackGroundService.initDefaultsForRoomCreate:117–170`: creating
a room in the housekeeping system publishes a blocking question to the PMS,
then pushes the answer to "core", then to the hotel-inspection server — three
systems told about a room by the one system that should not own it.

---

## 5 · Findings

Each finding names the file and lines it was read from, and each becomes a
requirement in §6 or a refusal in §6.9. They are ordered roughly by how much
they would cost a hotel, not by how easy they were to find. Nothing here was
executed; every claim is about the code as read.

### F1 · The system never marks a room clean; it waits five seconds and asks the PMS

`grep` over the tree for writers of `Status.Room`: the PMS/desk change
consumer (`RoomStatusInfoServiceImpl:140`, `valueOf` of whatever arrived),
room creation (`CLEAN`, `RoomStatusInfoBackGroundService:122`), and two
inspection-closed handlers writing `INSPECTED` (`CIXBackGroundService:50`,
`WorkOrderBackGroundService:71`). **No path from a CLEAN workflow completing
writes anything to the room.** What happens instead:
`TaskStatusInfoServiceImpl.updateRoomStatusAfterService:179–193` is `@Async`,
`Thread.sleep(5000L)` — *"Waiting 5000L for room status update"* — then
snapshots the room's current axes into `TaskStatusInfo`. The room becomes
clean when the front desk changes it in the PMS, if they do, whenever they do.
`hkStatus` is likewise written only by the change consumer
(`RoomStatusInfoServiceImpl:126`). The owner's charter says *"Room Care is
owner of different room statuses"*; the reference is a consumer of them. And
`INSPECTION` can only activate if the after-service snapshot says `CLEAN`
(`WorkFlowBackGroundService:348`) — so the inspection phase depends on the
desk having already marked the room clean before the inspector looks.

### F2 · A state change outside a window is dropped, and a window that crosses midnight can never match

`RoomStatusInfoBackGroundService.initDefaultsForRoomStatus:231–239`: if
`LocalTime.now` is not inside some service's `realTimeAllocationFrom..To`,
the change is logged *"EXITING"* and **discarded** — no pending record, no
task tomorrow, nothing a supervisor can click. A checkout at 16:05 against a
window ending at 16:00 is invisible. And every window test in the tree is
`from.isBefore(now) && to.isAfter(now)` (`:234`;
`RoomCleaningStateServiceImpl:51, 65, 79`; `WorkersAffinityServiceImpl:116`;
`WorkerMetricsServiceImpl:83`): for `NIGHT_SERVICE`, `18:00 → 07:00`
(`CleaningService.java:20–24`), both halves cannot be true at once, so **night
service is unreachable by construction**, and so is any night shift a property
configures. The boundary instant is excluded too.

### F3 · The day ends at 19:00 UTC for every property at once

`TaskBackGroundService.closeUncompletedTaskAndWorkFlow:363–405` —
`@Scheduled(cron = "0 0 19 * * *", zone = "UTC")`, a filter with **no
`companyId` / `siteId`**, closing every open task in the database. The
property timezone is looked up carefully in forty other places and the day is
ended on a UTC clock. `CleaningServiceInfo.autoCloseAt` (`:32`) — the
per-service close time a property configures — is read by nothing.

### F4 · Room identity is its name

Six call sites resolve a room by `getRoomFromNameAndCompanyIdAndSiteId`:
PMS/desk status changes when no id is sent (`RoomStatusInfoBackGroundService:212`),
room creation as upsert-by-name (`RoomServiceImpl:23`), the guest cleaning
request and its two filters (`GuestHouseKeepingServiceImpl:39, 65, 79`), and
`find`. Every message to "core" and to the inspection server carries
`room.getName()`, never an id (`RoomStatusInfoServiceImpl:305–326`). Rename
a room and its status feed stops arriving, silently. The Oracle round's R1
(*never publish the PMS's room number in a canonical field*) is this
finding's mirror.

### F5 · The service keeps its own room master

`tbl_room`, `tbl_room_type`, `tbl_room_type_floor_plan`, `tbl_sector`,
`tbl_room_connection` — created, renamed and deleted here, with room type and
sector as eager `@ManyToOne(cascade = ALL)` (`Room.java:29–38` — deleting a
room can cascade to its type). This is the `roomcare.rooms` the constitution
names as the wrong shape, and the sector is the zone that ADR 0044 gives
Room Care only as an *assignment*, never as the entity.

### F6 · No authentication, and the tenant, the actor and the assignee are whatever the caller writes

`@SpringBootApplication(exclude = {SecurityAutoConfiguration.class})`
(`HouseKeepingManagementServerApplication:23`) with Spring Security on the
classpath; no filter, no interceptor, no `@PreAuthorize` anywhere (grep);
`UserService.isValidToken` and `fetchAuthSession` are **declared and never
called**; `AuthenticationException` is handled and never thrown. `companyId`,
`siteId` and `initiatedById` are **request-body fields** on 51 DTOs
(`TaskCreateRequest:47–62`); `GET /tasks/{id}` and every other read by id is
unscoped (`TaskController:41–45`). The one "authorization" in the service —
the timer may be started only by the task's assignee
(`TimeLogServiceImpl:110`) — compares two client-supplied strings. CORS is
`allowedOrigins("*")` for every method (`HouseKeepingManagementServerConfiguration:20–31`).
Every activity-log row's actor is therefore a claim, not a fact.

### F7 · Two live AI API keys, database and broker credentials, and a public broker IP are committed

`resources/application.properties:25` holds an Anthropic API key;
`fiegn/AnthropicClient.java:12` holds a **different** one in a `@Headers`
annotation (inert under Spring's contract — the key is passed as a method
argument instead — but committed all the same). MySQL credentials at
`application.properties:9–10` and `quartz.properties:15–16`; RabbitMQ
credentials and a public IP at `bootstrap.properties:9–12` and
`application-local.properties:6–7`. None of the values is reproduced on this
page. **The keys should be treated as compromised and rotated** — that is an
action for whoever owns the account, reported here because reading the tree
is enough to have seen them.

### F8 · Eight blocking request/reply calls across application boundaries, three of which swallow the answer

§4.1's table. The work-order system is asked to create work orders (twice),
the PMS is asked a room's state, "core" is asked for arrival ETAs — **once
per room inside the AI input loop** — and the roster is asked three ways. Two
`catch (Exception ignored)` sites make a missing answer indistinguishable from
*no arrival*. This is the shape `EVT-Q3` ruled out for the platform, with the
consequence it predicted: an absent neighbour hangs a housekeeping flow, and a
slow one (Feign 600 s) hangs it for ten minutes.

### F9 · Assignment travels as a hyphen-joined string, and status as a string map

`WorkFlowServiceImpl:264` publishes `workFlowId + "-" + assignedFrom + "-" + assignmentId`;
`WorkFlowBackGroundService:156–158` splits it on `-`. Staff ids in this
system are 24-character hex strings and happen not to contain hyphens; ours
are UUIDs, which do — `split("-")[2]` would then be the first segment of the
new assignee's id, and nothing would fail. `"{taskId}-{workFlowId}"`
(`:91–92`) and `"{taskId}-{PROFILE}"` (`:172–173`) are the same shape.
Status changes are `HashMap<String,String>` with `_taskId` / `_statusFrom` /
`_statusTo` (`TaskBackGroundService:100, 109, 222–226, 291–295`), defaulted
when absent. Sixteen consumers, no schema.

### F10 · Workflow creation is asynchronous, so phase order is a race

`WorkFlowServiceImpl.create:60–62` is `@Async` on a method returning
`WorkFlowView` — Spring runs it on another thread and the caller receives
`null` — and `@EnableAsync` is on (`HouseKeepingManagementServerApplication:30`).
`TaskServiceImpl:81–91` and `WorkFlowBackGroundService:460–467` call it in a
loop and then write each workflow's status to `TaskStatusInfo` as if it
existed. `updateRoomStatusBeforeService` is `@Async` too and reads the room
from a **15-minute cache** (`TaskStatusInfoServiceImpl:136–142, 153`;
`HouseKeepingManagementServerConfiguration:66`), so "before service" is a
stale snapshot taken on another thread.

### F11 · The lock does not lock

`LockServiceImpl` is a `ConcurrentHashMap<String, ReentrantLock>` in one JVM
presented as a distributed lock. `autoReleaseLockAfterTime` — the timeout
every caller relies on — is an **empty method** (`:72–75`). `lock()` on
timeout **removes the entry** while another thread holds it, so the next
caller creates a fresh lock and both proceed (`:37–39`). `unlock()` releases
if `isLocked()`, not if held by this thread (`:60–61`), and always returns
`false`. `lockTask` uses the key `"worker_stats_" + taskId` — a copy-paste
that puts task locks and worker-stats locks in one namespace
(`LockService.java:33–35`). The assignment consumer is also `synchronized`
(`WorkFlowBackGroundService:87, 535`), which serialises every assignment in
the process and does nothing across processes.

### F12 · The credit cache is never cleared, and the reference's own document says it is

`LocalStorageService.clearAllAllocationCreditInfos:48–50` calls
`cache.cleanUp()` — Guava's *perform pending maintenance*, which evicts
nothing that has not expired. `prons_cons.md` (root) describes this call as
*"nuked globally … once per ALLOCATE request"* and builds a performance
argument on it. The document is wrong about the code it documents; that is
why nothing on this page rests on either root document.

### F13 · The AI allocator is 1,956 lines, of which about 1,850 are pasted sample output

`RoomAssignmentDecisionAgent.java`: lines 43–943 are a `main()` holding a
hard-coded model response; 973–1875 are `getMockAssignInfo` holding another;
the live method is 1899–1954. The prompts (`:29–30`) are Java strings with
**escaped** `\\n`, so the model receives literal backslash-n pairs, not line
breaks; the first prompt is for *facility inspection summaries* and belongs to
some other feature. The prompt hard-codes *"Cleaning starts at 9:00 AM"* and a
10-minute buffer. The class names a provider and a model
(`api.antropic.model`), reaches its collaborators through a `public static
_inst` set in `@PostConstruct` (`:27, 968–971`), sends `max_tokens 50000`,
and returns `null` on an unparseable answer — which `aiAllocate` then iterates
(`WorkersScheduleServiceImpl:247`). ADR 0130: an application never names a
model or a provider.

### F14 · Skills are captured and never consulted; full-day workers bypass every budget

`Affinity.skills` has a create/update surface and a DAO. The read path
(`WorkersAffinityServiceImpl.getWorkersForAffinity:114–135`) never touches it:
the request's `skills` field is unused, and the affinity filter is the
commented block at `:136–181`. Eligibility is *on shift for the service
covering now* and then: `FULL_DAY` → always eligible; `HALF_DAY` → only if
remaining minutes and credits cover the work; **nobody eligible → every
FULL_DAY worker** (`:135`). The capacity model exists and applies to half the
staff.

### F15 · Continuity is bracketed inside the wrong condition, and "nobody" is silent

`AllocationServiceImpl.findIfAnyCleaningContinuityFor:114–134`: the
`TOUCH_UP` continuity branch (`:125–131`) sits inside
`if (lastTaskCPForThisLocation == DEEP_CLEANING …)`, so it fires only when
yesterday's task was a deep clean. `allocate` returns `null` when the
candidate list is empty (`CustomExecutor:79–84` after `strategyFilter` has
removed everyone; `AllocationServiceImpl:102`), and
`WorkFlowServiceImpl.updateAssignment:249–252` treats a blank assignee as
*"No change … exiting"* — so **an unallocatable room is indistinguishable
from one nobody looked at.** `dropIfOutOfBounds` is honoured by two executors
and ignored by the third (`CustomExecutor:32–33`).

### F16 · Priority ignores whether the room is sold tonight

`TaskBackGroundService.getPriorityForTask:494–501`: `ARRIVAL → HIGH`,
`STAY_OVER → MEDIUM`, everything else `LOW` — so a departed room with a guest
arriving at 15:00 is `LOW` unless the PMS has already flipped the reservation
axis to `ARRIVAL`. The information the owner's charter turns on (*"if no
guest arrives into it today it has no priority"*) is exactly `next_sold_at`,
which the wire carries and the reference does not have. A dead
`getPriorityForTaskOld` (`:503–524`) compared arrival and clean counts
property-wide and ends in an unreachable branch.

### F17 · The run's progress total is always zero, and the duplicate guard reads it

`CleaningScheduleMapper.logEntry:57–58` maps `total = 0`, `completed = 0`;
`WorkersScheduleServiceImpl.allocate:195–197` sets both to 0 again and never
sets `total` to the room count. So the SSE stream reports `completed / 0`,
and `validate:175` — refuse if `total == 0 || total != completed` within ten
minutes — is **always true** in the window: the "still in progress" guard
cannot distinguish a finished run from a stuck one.

### F18 · A request handler sleeps for twenty seconds, and lists are capped at a thousand

`WorkersScheduleServiceImpl.clearAndAllocate:315–319` — `Thread.sleep(20000L)`
between unassign and reassign, inside the HTTP request. `reAllocate` and
`clearAndAllocate` fetch tasks with `setRows(1000)` (`:284, 303`); a larger
property's remainder is silently left as it was.

### F19 · The same concept modelled many times over

* **DND four ways** — `RoomStatusInfo.doNotDisturb`, `Status.HK.DND`,
  `Status.Service.DND`, a `RoomServiceStatusInfo` row; sleep-out three ways
  (`Status.HK`, `Status.Service`, `RoomSpecialStatus`).
* **A profile as a column family** — `CleaningPreferences` has `minutesFor*`,
  `creditsFor*` and `checklistIdFor*` × 5 (`:34–57`); `WorkFlowCriteria` has
  four `*ProfileAllocatedId` columns (`:13–20`); four supply preferences carry
  `forDeepCleaning · forFull · forLight · forTouchUp` and **no long-stay
  column**, so the fifth profile is unrepresentable in all four.
* **Three booleans for one default** — `defaultForDeparture · defaultForStayOver
  · defaultForSleepOut` on every profile (`CleaningProfileInfo:36–40`); two
  profiles can both be the default and `findFirst` picks one.
* **Two task models** — `Task` with `LocationType.AREA` and `AreaCleaningTask`,
  each with controllers, DAOs, mappers, time logs and inspections.
* **Five supply families** with one shape (§1.9).

### F20 · The policy decision is implemented twice, and the two disagree

`determineRequiredCleaningProfileInDayService:123–147` (+ the special-status
variant `:150–164`) and `determineRequiredCleaningProfile:196–229` are the
same decision in different orders. In the day-service path the guest's
chosen profile is consulted first, but **never when the room has a special
status** (the special-status method has no guest lookup); in the generic path
the special request is checked before the guest, then the guest, then
long-stay. `filter(CleaningProfileInfo::getDefaultForDeparture)` unboxes a
nullable `Boolean` (`:136, 144` …) — a profile with the flag unset throws.
`p.getStrictProfileForRooms().contains(…)` is null-guarded on `:139` and not
on `:125`.

### F21 · Any transition is allowed, an unknown status still announces a change, and every error is a 400

`TaskServiceImpl.updateTaskStatus:283–312` forbids one transition
(`REMOVED` unless `PENDING`); `COMPLETED → PENDING` is fine.
`WorkFlowServiceImpl.updateStatus:207–217` forbids reopening COMPLETED and an
unallowed SKIP; nothing else. `RoomStatusInfoServiceImpl.update:157–162`: an
unknown `statusFor` logs an error and **still publishes** `core.room-status.update`
and `hk.room-status.change` with `oldStatus = ""`. `TaskServiceImpl.update:150–153`
catches every exception — including its own `NOT_FOUND` — and rethrows as
`BAD_REQUEST`.

### F22 · State is stored twice, the copies disagree, and a forensic logger was built to find out why

`TaskStatusInfo` mirrors the task status, four workflow statuses, the service
status and six room-axis snapshots, written by twenty-odd `update*` calls
scattered through the consumers. `isCleanStatusDowngrade:117–124` refuses to
move `cleanStatus` back from COMPLETED — a guard against the queue reordering
that the mirror invites. The REMOVED handler carries the comment *"Direct DAO
update — do not use workFlowService.updateStatus (avoids RMQ calibrate →
COMPLETED overwrite)"* (`TaskBackGroundService:344`). And
`HkTaskCompletionDebugLogger` — 212 lines writing every transition to a flat
file, **enabled in the production properties** with *"disable after
investigation"* beside it (`application.properties:40–45`) — is what it took
to see the pipeline. Chapter 12's `entity_version` on the owner's row is the
mechanism this system was missing.

### F23 · A bulk UPDATE and a managed entity write the same row in one flow

`RoomStatusInfoServiceImpl.update:115–155` sets the field on the loaded
entity **and** calls `roomStatusInfoRepo.updateXStatus` — a `@Modifying`
JPQL `UPDATE` with its own `@Transactional` (`RoomStatusInfoRepository:34–62`)
— then reads the entity back for the view. The `@Version` on the entity
(`AbstractBaseEntity`) is bypassed by the bulk statement. Task and workflow
statuses are written the same two ways (`TaskRepository:22–43`).

### F24 · A paused-and-resumed clean records only its last segment

`resume:229–241` ends the old log and opens a new one carrying the
accumulated minutes — then `end` → `endTimer:306–313` sets
`timeInMinutes = betweenDates(start, end)` of the new log, **overwriting the
carried total**. `resume:238` sets `ACTIVE` on the *old* log, not the new one.
`end:287–289` credits worked minutes to the attendant **only if the task was
created today** in the property's zone — a night clean finished after
midnight credits nothing. Timers use `new Date()` — the server clock.

### F25 · The work-order handler looks the task up by the work order's own id

`WorkOrderBackGroundService.workOrderStatusChange:63`:
`taskDao.findById(workOrder.getId())` where `workOrder.getTaskId()` was meant;
`if (task == null) return;` hides it. `WorkOrder.closed()` compares a `String`
field against a list of enum constants and is **always false**
(`WorkOrder.java:36–38`); its only caller is commented out. The listener's
catch block is empty (`:42–44`).

### F26 · A completed inspection is a passed inspection; a failed one does nothing

`CIXBackGroundService.defaultInitForInspectionStatus:30–43`: `COMPLETED` →
workflow COMPLETED, then the request's status is **rewritten to `PASSED`** and
the method recurses into the branch that writes the room `INSPECTED`.
`FAILED` and `IN_REPAIR` are empty branches. There is no outcome a supervisor
can record that leaves the room not-ready.

### F27 · Asia/Kolkata in fourteen places, and a timezone parameter that is ignored

`DateTimeZoneService.getDefaultTimeZone:9–11` and thirteen more literal
fallbacks across `CronUtils`, `DateUtils`, three filters, two mappers,
`TaskBackGroundService:456` and `AreaCleaningScheduleBackGroundService:74`.
`DateUtils.getMillisFormatted(millis, timezone):195–197` accepts a timezone
and passes the literal — every supervisor notification that formats a time
with `task.getTimezone()` (`HKScheduleExecutorService:239–240, 273`) prints
India time. `Interval.THIS_WEEK / LAST_WEEK` are computed by
`Calendar.getInstance()` **at class load** (`Interval.java:10–11`) — a server
that has been up a fortnight reports the week it booted.
`WorkFlow.startTime / endTime` come from `ZonedDateTime.now()` in the server's
zone (`WorkFlowBackGroundService:378, 416`).

### F28 · Handlers that run and do nothing

`orphanTaskCheck` — a `@Scheduled` method whose body is a commented call
(`TaskBackGroundService:354–361`). `runServiceThreshold` — queries every
attendant's open workflows into a local nobody reads
(`HKScheduleExecutorService:283–297`). `initDefaultsForETACalculation` — the
target of the assignment cache's **removal listener**, iterates an empty loop
(`:299–308`; `LocalStorageService:22–28`). `runAreaCleaningSchedule` — a
switch with two empty cases over a commented body (`:311–336`), reached by
listeners whose publishers are themselves commented out
(`AreaCleaningScheduleServiceImpl:54, 87, 105`). `WorkersStatsBackGroundService`
— a null check followed by a comment (`:32–47`). `ReceiverInterceptor` —
returns `null` from every post-processor, unused; wired, it would null every
message (`:11–25`). `Task.delete` — `return null`, and the controller
dereferences it (`TaskController:65–69`, `TaskServiceImpl:370–373`).

### F29 · Errors are swallowed, and a failed message is acknowledged

39 `printStackTrace`, 9 `catch (Exception ignored)`, 6 catch blocks whose
whole body is `e.getLocalizedMessage();` (`TaskBackGroundService:161–163,
174–176, 206–208, 216–218`; `HKScheduleExecutorService:138–140`), one empty
catch (`WorkOrderBackGroundService:42–44`), and 33 listeners that catch
everything. The retry configuration (`bootstrap.properties:16–20`) never
fires because nothing propagates. 40 `return null;` in service
implementations.

### F30 · Logging is the audit trail and everything is an error

326 `log.error` against 51 `log.info` and 6 `log.warn`; *"SUCCESSFULLY
ALERTED"*, *"Room {} selected for refresh action"* and the startup banner are
errors. Every bus payload is logged at ERROR by the converter (§4.1) — guest
`crmId`s, staff ids and room names in the error log by design.

### F31 · Messages to people that say the wrong thing

The guest-request work-order comment `"Guest requested cleaning room %1$S on
%2$s . Cleaning Type : %3$s"` is formatted with `(specialNotes, date,
profile)` — the guest's **notes** print as the room, upper-cased
(`GuestHouseKeepingBackGroundService:44, 52`). The quick-clean alert says
*"completed in %3$s"* and formats a **timestamp** where a duration is meant
(`HKScheduleExecutorService:236–240`); `_priority` is read from a map nobody
puts it in (`:241`).

### F32 · The minibar records a priced consumption per guest and posts it nowhere

§3.9. `MinibarConsumption.totalPrice` and `crmId` are written; no message,
no folio, no stock movement, no par check. A charge that exists only in the
housekeeping database is a charge the guest never sees and the hotel never
collects.

### F33 · A guest's cleaning request becomes a work order elsewhere, and a profile here

§3.7. The request is stored as `GuestHousekeepingInfo`, raised as a work
order by blocking call, and separately read by the policy decision — where it
is ignored whenever the room carries a special status (F20).

### F34 · An unauthenticated file read with no path confinement, and a constructor that cannot run

`GET /general/fetch?filename=` (`GeneralController:28–41`) →
`UploadResourceHelper.fetch:130–143` concatenates the caller's string onto
one of two base directories and serves whatever exists there. No
normalisation, no containment check, no authentication (F6). The class's
constructor (`:56–61`) creates the base directory from a field Spring has not
injected yet, throws, and swallows it.

### F35 · Endpoints that cannot succeed

`DELETE /tasks/{id}` dereferences a `null` (F28). `POST /workorder/search`
returns `null` data (`WorkOrderServiceImpl:37–40`). `PATCH … /status` with an
unknown kind announces a change that did not happen (F21). `@PastDate`'s
validator is declared for `FutureDate` (`PastDate.java:30`) — unused, so it
cannot fire, and recorded as dead rather than as a defect.

### F36 · Sequences that are counts, a bind parameter used as a column name, 39 native queries

`nextSequence = count(*) + 1` (`RoomStatusInfoRepository:19–23`) — delete a
row and the next room collides. `findMaxTaskNumber` is `max + 1` per company
(`TaskRepository:17–18`), racy and shared across a group's properties.
`getRoomsStatusSummaryForStatus` selects and groups by `?3` — a bind value,
not a column — into a `HashMap` (`:67–71`); dead, its caller commented out
(`RoomStatusInfoDaoImpl:111–112`). `getLastTaskIdFor` is native `limit 1,1`
(`TaskRepository:45–46`).

### F37 · Schema by inference, no migrations, zero tests

`spring.jpa.generate-ddl=true`, `spring.sql.init.continue-on-error=true`
(`application.properties:6, 12`); `data.sql` is one comment block; no
`src/test`. 751 files and nothing that can fail on purpose.

### F38 · Captured and consumed by nothing

Connecting-room groups (`RoomConnection`, a 235-line service) are referenced
by no task, allocation or policy code (grep, 2026-09-05); the one place
related workflows are handled logs *"Not implemented !!"* for more than one
(`WorkFlowBackGroundService:548`). `AllocationCriteria.strict` is never read.
`CleaningServiceInfo.supervisorThreshold` feeds F28's empty handler.
`OnDemandCleaningPref` has a price nothing charges.

### F39 · An SLA of zero, and inspections take two minutes

`WorkFlowServiceImpl.updateWorkFlowAttributes:448–482`: no room-type
preference → `sla` stays null → set to **0** (`:481`), which disables the
overdue alert (`> 5`, `WorkFlowBackGroundService:394`) and makes
`performanceTimeInMinutes = 0 − resolution` negative. Inspection-type
workflows get `sla = 2` hard-coded (`:441–446`). Area cleans get the enum's
default minutes.

### F40 · Worker capacity is an enum, everyone syncs in as full-day, and the criteria are rewritten wholesale

`WorkerSession` (`:6–7`) hard-codes hours, credits and a per-room minute figure
for the two sessions that exist. The service-start sync creates every
attending person as `FULL_DAY` regardless of shift
(`HKScheduleExecutorService:163–172`) and **replaces the whole allocation
criteria list** with sector→person pairs from the roster (`:188–196`) — a
supervisor's manual criteria last until the next service start.
`WorkerMetrics` totals are read-modify-write with no lock and clamped to
`[0, total]` so drift is invisible (`WorkerMetricsServiceImpl:92–179`).

### F41 · Validation in a setter

`TaskCreateRequest.setProfile:64–68` throws a `ControllerException` for
`SKIP_CLEANING` — from inside Jackson's deserialisation, where it surfaces as
an unreadable-body error, not the intended response.

### F42 · Three databases and a cache in an interface

Policy phases and allocation strategy in MongoDB; Quartz in a second MySQL
database with its own password; property and site info in Ehcache instances
held in **static fields of a Feign interface** (`PropertyInfoClient:26–40`),
created at class load and never closed, with `containsKey` then `get`.

### F43 · Dead weight

Eight `public static void main` in production classes (`DateTimeZoneServiceImpl:21`,
`TimeLogServiceImpl:57`, `LocalStorageService:52`, `RoomAssignmentDecisionAgent:42` …);
`NonsenseGenerator` (230 lines, `@Deprecated`); `MockInitializerImpl` (no
callers); `PublishInterceptor` (empty); 26 `System.out.println`; four
`@Deprecated` members still used (`WorkFlows.CHECK`).

---

## 6 · Requirements for our Room Care

From here the names are **ours** (APPS-Q3). Each requirement names what it
carries over, fixes, or refuses, and the finding or platform rule behind it.
None of these is a decision — they are the proposal the walkthrough and the
owner's gate rule on.

### 6.1 · Room state — what Room Care owns, and what it only hears

**R1 · Room Care's own record per `masterdata.room_id` carries exactly the
four axes the wire carries** — `occupancy`, `condition`, `room_care_status`,
the `stay_statuses` list — plus `next_sold_at` and `is_pseudo_room`, in the
platform's vocabulary (`RoomCondition`, `Occupancy`, `StayLifecycle`). The
reference's four axes confirm the shape; the wire's list-of-stays and
`next_sold_at` are the fixes. *(§2.1; `RoomState`; no second vocabulary — CTX-Q7.)*

**R2 · Room Care sets `condition` itself, in its own transaction, bumps the
room's version and appends `room.cleaned` in the same commit.** Completing the
cleaning phase *is* the room becoming clean; inspection passing *is* it
becoming inspected. No sleep, no waiting for the PMS. Chapter 12 §head and
diagram 42 draw exactly this; `HUB-Q4` names it as what Room Care publishes
when it *applies* an observation. *(F1; events-in-the-caller's-transaction.)*

**R3 · An inbound `room.state_observed` is an observation with provenance,
never an overwrite.** Room Care records what the PMS or the desk said, by
whom, and reconciles it with its own state by a rule the walkthrough sets —
the `GUEST-Q3` shape (*a standing decision by a person beats a possibly-stale
fact, and a disagreement is a flag, not a second answer*). *(F1, F4; §8.)*

**R4 · Service exceptions are one vocabulary, recorded as outcomes.** DND,
sleep-out, refused service, and the special statuses (touch-up, strip linen,
vacant refresh, long stay, special request) are Room Care's, modelled once,
with a date and an actor — not four flags on four objects. *(F19.)*

**R5 · No state change is ever dropped.** A change that arrives outside a
service window, outside a policy, or for a room with no cleaning kind is
recorded as *pending policy* and visible — the owner's *"cleaned tomorrow,
or by a custom click action."* *(F2; "a gap is reported as a gap".)*

**R6 · Room identity is `masterdata.room_id`; a name is presentation.**
Nothing is resolved, matched or published by room name. *(F4, F5; ADR 0051.)*

### 6.2 · Cleaning policy — configuration, evaluated explicitly

**R7 · Cleaning is a property policy, and the policy decision is a pure,
recorded function** of the room's four axes, the exceptions, the business day
and the service — with the inputs and the outcome stored on the task it
produces or the *pending* record it does not. One implementation, unit-tested
against the owner's scenarios. *(F20; the charter.)*

**R8 · Service windows are ranges that may cross midnight**, evaluated on
the property's clock against the operating day, and a room's eligibility is
decided when the policy runs, not when a message happens to be consumed.
*(F2; `GetOperatingDay`.)*

**R9 · Room Care owns a catalogue of cleaning kinds** (the reference's
profiles: skip, touch-up, light, full, deep, long-stay — and whatever a
property adds), and **per room type, per kind: minutes, credits, whether
inspection is required, and a checklist** — as rows, not as five column
families. ADR 0044 names this configuration as Room Care's in those words.
*(F19; ADR 0044.)*

**R10 · The calendar rules carry** — which kinds run on which weekdays,
strict kinds for named rooms, a guest's chosen kind for a date — with an
explicit precedence the walkthrough fixes (guest request vs special status vs
default). *(F20.)*

**R11 · Priority is computed from `next_sold_at`, the stay list and the
operating day** — *sold tonight* first, then due-out, then stay-over — with
the ladder configurable per property. *(F16; the charter; `RoomState:40–46`.)*

**R12 · The day rolls on the property's operating day, from Context, never
on a cron in UTC**; each service's close time is honoured; what is closed is
recorded as closed-by-policy with what it was. *(F3; ADR 0128 §6.)*

### 6.3 · The room task and its phases

**R13 · One task per room per operating day per service, with an ordered
list of phases** (pre-clean, clean, post-clean, inspection — from the kind),
each phase with one state and a declared transition table; a refused
transition names both states. *(F21; §2.2.)*

**R14 · State lives in one place**, versioned on the aggregate, and every
event carries `entity_version` from that row. No mirror table, no downgrade
guard, no forensic logger. *(F22; Chapter 12; `events.proto:84–103`.)*

**R15 · Phases are created in the task's transaction, in order**, never
asynchronously. *(F10.)*

**R16 · The attendant's timer accumulates across pause and resume**, is the
completion signal for the phase, and credits worked minutes to the attendant
on the operating day the work happened. *(F24.)*

**R17 · An activity log — from, to, actor, instant — per change**, where the
actor is the platform's caller. Carried from the reference; fixed by R32.
*(§1.3; F6.)*

**R18 · Inspection has three outcomes — passed, failed, not done — and a
failed inspection returns the room to *dirty* with the reason.** Completed is
not passed. *(F26.)*

**R19 · Extra-time requests carry** — minutes, evidence, a supervisor's
decision — as a Room Care record that revises the phase's expected minutes.
*(§1.3.)*

**R20 · The three-consecutive-DND rule carries** as a Room Care policy that
emits an event (`roomcare.…`) with a correlation id; whoever raises the
security job — Jobs — does so by consuming it. *(§3.6; `EVT-Q3`.)*

**R21 · Quick-clean and overdue checks are durable, and the supervisor is
told through the platform's notification path**, never through a private
"cm-d" exchange. *(§1.6; `TEMPORAL-Q1`.)*

### 6.4 · Assignment

**R22 · Zones are `RoomZoneAssignment` (Room Care's); who holds a zone today
is a Workforce posting read through Context.** Room Care never rosters
people, holds no `WorkersStats`, and derives no capacity of its own.
*(Workforce §3.8; ADR 0044; §2.4 refused.)*

**R23 · Allocation is a pure, recorded function** — the candidate set, the
strategy, and the choice are written with the assignment, and *"no one
available"* is an explicit outcome the supervisor sees. *(F15.)*

**R24 · Continuity, sector affinity and the three strategies carry as named
strategies** a property picks; skills, if used, are actually consulted.
*(§1.4; F14, F15.)*

**R25 · Capacity — minutes and credits per attendant per day — is one fact
in one place, read from Workforce where it originates, with no clamping.**
*(F40; ADR 0063.)*

**R26 · AI-assisted allocation, if in v1, is an AI Runtime agent** through
the governed chain — advisory, with the prompt's priority rules expressed as
policy configuration, not English in a string. *(F13; ADR 0130.)*

### 6.5 · Time

**R27 · The property's timezone comes from the platform and is never
defaulted**; instants are stored as instants and rendered through
`formatInstant`. *(F27; JOBS-Q1(8).)*

**R28 · Recurring futures — service auto-trigger, service close, quick-clean
and overdue checks, extra-time reminders — are Temporal Schedules via
`AddTemporal`.** No Quartz, no JDBC job store, no cache-eviction triggers.
*(F28; `TEMPORAL-Q1`.)*

### 6.6 · Events and the platform boundary

**R29 · Events are appended in the caller's transaction**, typed, with the
envelope's `correlation_id`; no hyphen-joined strings, no string maps.
*(F9; CLAUDE.md §"Events are appended".)*

**R30 · Room Care calls no other application and asks the PMS nothing.**
Work is raised in Jobs by publishing with a correlation id and consuming
`job.created`; the guest's request is a GuestOps object that arrives as an
event; arrival facts are `stay.*` events and Context reads. *(F8, F33;
`EVT-Q3`; `JOBS-Q1(5)`.)*

**R31 · Room Care supplies `RoomContext.room_condition`** — the read view and
the Context RPC, display-only, in this round. *(`CTX-Q4`; `RoomContext:214–217`.)*

**R32 · The caller comes from the platform.** No tenant, actor or assignee in
a request body; every read and write is property-scoped by the Kernel's
decision. *(F6.)*

**R33 · No secrets in the repository**, no local-disk configuration, no
local-disk files — evidence photos through the platform's store. *(F7, F34.)*

**R34 · Consumers are idempotent on `event_id`** and order on
`aggregate_id → entity_version`. *(F22; Chapter 12.)*

### 6.7 · Surface and structure

**R35 · A bundle on the app contract** — manifest, `MapModuleCapability`,
`PlatformEnvironment.Read()`, the 17 published tokens and page 64's control
vocabulary, a 56 px top bar, bare-table lists on `common.v1` paged-with-total
(the reference's `PaginatedList` is that shape already), and five widgets
each answering one question. *(page 64; page 56; `CORE-Q13`.)*

**R36 · One schema, `roomcare`, in PostgreSQL, with migrations and tests.**
No Mongo, no second database, no `generate-ddl`. *(F37, F42.)*

**R37 · ADR 0042's module shape**, one file per subject, nothing dead, no
`main()` in production. *(F43; ADR 0027/0038/0042.)*

### 6.8 · Deliberately not carried

| Not carried | Why |
|---|---|
| **Its own rooms, room types, floor plans, sectors, connecting rooms** | Master Data's. The zone is Master Data's entity and Room Care's *assignment* (ADR 0044). Connecting rooms are a Master Data question (§8). *(F5, F38)* |
| **`WorkersStats`, `WorkerMetrics`, `Affinity`, the roster syncs** | Workforce's — postings, shifts, capacity, skills (ADR 0063; Workforce §3.8). *(R22, R25)* |
| **The four-value `EntityStatus`** | ADR 0062: `active` + `deleted_at` for master entities; a task's states are its own (JOBS-Q1(6)). |
| **`TaskStatusInfo` and the before/after snapshots** | R14 — one state, versioned. *(F22)* |
| **The string-map and hyphen-string bus contracts** | R29. *(F9)* |
| **Blocking request/reply, the `cm-d`/`rst`/`wo`/`pms` exchanges, the Feign clients** | R30; notifications, roster, work orders and the PMS are events and Context. *(F8)* |
| **The AI allocator as written** | R26. The prompt's priority order is kept — as policy. *(F13)* |
| **Quartz, the in-JVM lock, the static caches and singletons** | R28; Temporal, the database, DI. *(F11, F12, F28)* |
| **The 19:00 UTC close, Asia/Kolkata** | R12, R27. *(F3, F27)* |
| **The `AreaCleaningTask` model** | one task model, if public areas are in scope at all — §7. *(F19)* |
| **Five supply families, the RA carts** | §7 — Inventory's or nobody's, not five tables here. *(F19)* |
| **Minibar pricing, `crmId`, `totalPrice`** | §7 — a folio fact. *(F32)* |
| **The unauthenticated file surface, upload-by-extension** | R33. *(F34)* |
| **`SessionClient`, `AuthenticationException`, CORS `*`** | R32; the platform authenticates. *(F6)* |
| **`NonsenseGenerator`, `MockInitializerImpl`, the `main()`s, `PublishInterceptor`, `ReceiverInterceptor`** | *(F43)* |
| **A per-company task number** | per property, as Jobs' R1 ruled the job number. |

---

## 7 · The scope boundary — flagged, not decided

The reference bundles what our platform separates. Each seam below names
what the reference does, which of our domains the pieces belong to on the
frozen ownership table, and what is *genuinely* Room Care's act inside it.
**None of these is decided here** — each is a question in §8.

### 7.1 · The minibar

| The reference's fact | Whose it is on our table |
|---|---|
| *An attendant refilled two waters in 214 during the clean* | **Room Care** — an act during service, by the person doing the service |
| The room's minibar stock and the par level per room type | **Inventory** (installable, in the constitution's list) — stock is what Inventory *is* |
| `totalPrice`, `crmId`: a charge to the guest in the room | **GuestOps** (the stay owner) or a Finance/folio domain — a posting, never housekeeping's table |
| The item catalogue and prices | Inventory / Procurement |

The reference's one honest domain insight — *what was refilled is what was
consumed* — survives as an event Room Care publishes (`roomcare.minibar.refilled`
or the like, with quantities, no price) that Inventory and the folio owner
consume. Whether Room Care carries even the recording screen in v1 is the
question.

### 7.2 · Amenities, linen and consumables; the attendant's cart

Catalogues, par levels per room type, per-room counts, and *taken / returned*
carts (`AmenityRACart`, `LinenRACart`). Linen is the **Laundry department
(`LDY`, under `HK`)** on ADR 0119's canon; consumable stock is Inventory's. The
Room Care act is *"restocked to par during a full clean"* — an event, not a
stock ledger.

### 7.3 · Public areas

`Area`, its schedule, its tasks, inspections and time logs — the **Public
Area department (`PA`, under `HK`)**. Two readings: Room Care owns *care of
spaces* including public areas (one task model, `location: room | area`); or
scheduled area cleaning is Jobs' recurring work (its design already takes
scheduled jobs). The reference had both models and used neither well (F19,
F28).

### 7.4 · The guest's cleaning request and preference

A GuestOps object (the guest, the stay, the request) that reaches Room Care
as an event and becomes a policy input (R10). Jobs' survey asked the same
question for guest-raised jobs (its §7 Q10).

### 7.5 · Inspection

The reference has three inspection routes and a separate hotel-inspection
server. Is inspection a **phase of a room task** (R13, R18) — Room Care's —
or an application of its own that Room Care consumes by event? The
`Inspection` catalogue with *action if failed / drop if failed* suggests a
checklist engine that more than one department would use.

### 7.6 · Work orders raised from cleaning

Jobs'. Room Care publishes (a DND alert, a maintenance defect found during a
clean) with a correlation id and stores the `job_id` from `job.created`. The
reference's `WorkOrder` table becomes a reference on the task to a job id.

### 7.7 · Connecting rooms, sectors, floor plans

Master Data's — a suite is what rooms *are*. `Sector` is the zone (ADR 0044);
`FloorPlan` is a Master Data question the reference never used for anything.

### 7.8 · Notifications, sessions, files, the roster

The platform's, Identity's, the platform's store, Workforce's.

---

## 8 · Questions that need rulings

No `RC-Q` row exists in the register, so **every number below is unminted —
the architect assigns them; nothing here claims one.** Listed plainly, in the
order they block work.

1. **There is no Room Care chapter.** `ls docs/chapters/` returns 59 files and
   none is about this application; it appears only as a named example and as
   diagram 42. `JOBS-Q1(1)` ruled Jobs' locked walkthrough plus design page
   the design of record on the pms-oracle precedent. Is the same ruling given
   here — this survey, the walkthrough and the design chapter as Room Care's
   design of record, a planner chapter amending them if one arrives?

2. **Page 48 and the brief disagree on the reference and on chapter 01.**
   `docs/working/48` §3 names `pms-integrations/…/modules/housekeeping/` and
   §4 names `01-how-hotels-actually-clean.md`; the brief names
   `house-keeping-management-server` and this survey. This page followed the
   brief. Is page 48 amended to the brief's arc (survey → walkthrough with the
   owner's scenarios → design), and is the scenario study the walkthrough's
   spine rather than a separate chapter?

3. **Where does the minibar seam fall?** §7.1: the attendant's refill is Room
   Care's act; stock is Inventory's; the charge is the stay owner's. Does Room
   Care carry a refill-recording screen in v1 that emits an event and holds no
   price, no stock and no guest — or is the whole minibar out of Room Care's
   scope until Inventory exists?

4. **Amenities, linen and the attendant's cart** (§7.2) — Inventory's, Laundry's
   (`LDY`), or a Room Care *restocked-to-par* event with no ledger?

5. **Public-area cleaning** (§7.3) — one Room Care task model with
   `location: room | area` (the `PA` department is under `HK`), or Jobs'
   recurring work?

6. **Is inspection a phase of the room task or its own application?** (§7.5.)
   The answer decides R18's shape and whether a checklist catalogue is Room
   Care's.

7. **When Room Care's own condition and an inbound `room.state_observed`
   disagree, which answers?** R3 proposes the `GUEST-Q3` shape — the standing
   decision by a person wins, the disagreement is a flag — but the reference
   shows hotels where the *PMS* is the desk's only screen and housekeeping
   follows it. Is the precedence a property policy, and what does the PMS-only
   hotel (no Room Care attendant app) look like?

8. **`CONN-Q11`, on this survey's evidence, does not need to reopen.** The
   reference's uses of arrival-ness (§3.1) compose from the wire's
   `stay_statuses` list (`DUE_OUT` and `CHECKED_IN` distinct) plus
   `next_sold_at`. Is that accepted provisionally, with the walkthrough's
   scenario pass as the final check — or does the architect want the
   revisit clause exercised now?

9. **The guest's cleaning request and daily preference** (§7.4) — a GuestOps
   object that Room Care consumes by event, or Room Care's own record with
   the guest identified by `stay_id`?

10. **`events.proto:95` illustrates `UPDATE roomcare.rooms`.** Not Room
    Care's question, but found reading the documents that govern it: the
    comment teaches the table the constitution names as the wrong shape, four
    lines after saying an example teaching a forbidden shape is not harmless.
    Reported for the events surface's owner.

11. **Diagram 42 authorises "mark room cleaned" as `masterdata.room.update_status`.**
    ADR 0051 says permission names did not move with ownership; the registry's
    actual name for Room Care's write is the walkthrough's to find. Is the
    diagram's name the registry's, or an illustration?

12. **The permission vocabulary** — parked, as `JOBS-Q1(7)` parked Jobs',
    until the walkthrough; nothing is minted here.

13. **Does Room Care hold "escalation"** — the three-DND security alert, the
    overdue-clean alert to a supervisor — or is escalation the platform
    capability Jobs' survey Q11 already asked for? Not re-asked; noted as the
    same question arriving a second time, which was the argument for asking it
    once.

14. **Is AI-assisted allocation in v1?** R26 says how, not whether. The
    reference's AI path is the only place its priority policy is written
    down, which says something about how supervisors used it.

---

## 9 · What this page deliberately does not contain

* **No design.** No schema, no aggregates, no event payloads, no screens. The
  walkthrough comes next, and the design chapter after the owner has ruled on
  it.
* **No decisions.** §6 is a proposal for the gate; §7 names seams and chooses
  none; §8 is questions, not option menus.
* **No `RC-Q` numbers.** The register mints them.
* **No secret values.** F7 names files and lines; the strings are not here.
* **No claim about runtime behaviour.** There is no test suite and no running
  instance; nothing was executed. Where a finding says "never" or "always",
  it is a claim about the code as read on 2026-09-05, at the lines cited.
* **No judgement of the people who wrote it.** It ran hotels. Its four-axis
  room state, its policy-per-service idea, its three-DND rule, its extra-time
  request and its quick-clean alert are the most useful things it gives us,
  and its defects are the price of finding out what a housekeeping system
  actually has to do.

# Room Care Part B — the drive list

Prepared 2026-09-19 by KK, for the run after the owner updates to
**`roomcare-0.1.3.hopkg`**: 16,729,943 bytes, sha256
`c3485aa282bbbab409bdb2258b9cc9e3a31a0ea80075b805b496e81e5e54c634`, signed by
`dev-local`, staged in `%LOCALAPPDATA%\HotelOS\packages\registry` on the
owner's machine. It was built from clean worktrees of `HotelOsApps` `16bdeed`
and `HosPilotOS` `ff7926fb`, and its UI bundles are the bytes the page-64 audit
measured at `020f1eff` (`docs/page64-audit.md`). **Part B certifies 0.1.3, the
version that carries the audit's fixes.** ~~0.1.2 is installed today and stays in
the registry as the version 0.1.3 replaces.~~ *Corrected again, 2026-09-19, measured: Room Care is installed on no Kernel. The owner's running platform (`.tmp/devrun`, the dev cluster on 25432) has `platform.packages` = guestops 0.3.2, jobs 0.4.2, openai 1.1.0, workforce 0.3.3. `%LOCALAPPDATA%\HotelOS\packages\installed` holds guestops, jobs and workforce. The `roomcare` schema has 0 tables, and the running Kernel's log (since 2026-09-08) has 0 lines naming `roomcare`. The installed-product Kernel under ProgramData has been stopped since 2026-09-18 and loaded 0 applications. "0.1.2 was already installed" was a relayed premise, and I wrote it in without checking. 0.1.2 and 0.1.3 (digest `c3485aa2…`, matching) are on the shelf, not installed.* **So Part B starts with installing Room Care; it is not an update.** Neither 0.1.0 nor 0.1.1 was ever
installed, and each was removed, so no version names two builds. **Nothing below has been run yet; every result cell is empty
on purpose.** The shape is FF's (`guestops/docs/part-b-drive-list.md`), so the
two certificates read alike.

## What the columns mean

| Column | Meaning | Governs |
|---|---|---|
| **Platform-served** | *"the requested application operation completed according to its operation contract"* — a legitimate empty result is served; a refusal, an unavailable dependency or a transport failure is not | ADR 0148 |
| **Reachable by a person** | an operator reaches it through a screen or a widget, on the desktop | ADR 0143, ADR 0148 |

> **Part B does not attest that the owner's production installation
> historically served the operation.** — ADR 0143

A third value is used and is neither pass nor fail:

> **BLOCKED — ADR 0193 (ruled, unbuilt)** — the operation is authorized against
> Room Care's own object type, `room_task`. ADR 0193 rules that an installed
> application declares the types it owns and the Kernel registers them; the
> manifest schema (II) and the model composer (CC) that carry it are not built,
> so nothing registers `room_task` and the check is refused for every caller by
> construction. Recorded, never scored.

## Where authorization is enforced — read, not assumed

Read where it is **enforced** (every `gate.*Async` and `RequireAsync` in
`backend/src`), not where it is declared.

```text
property scope   roomcare.read       ReadCapability:22 — every read, one check
                 roomcare.configure  StandardService · HouseSetupService · ManagerGrants · SetupProjection
                 roomcare.plan       DeepCleanService:35, :77
room_task scope  roomcare.assign     AssignmentService:30, :47, :92 · PrepareService:51
                 roomcare.amend      AmendService:99 · DisagreementService:22 · RoomStatesService:57 · SupervisionService:42
                                     (the last three on the room's most recent task — Gate.RoomAsync)
room scope       room.inspect        InspectionOutcome:24 — the inspection app's outcome; a consumer, no person
no Kernel check  room.clean          AttendantWork · RoomActs — the assignee's acts, checked on Room Care's row
```

**RC-Q8, ruled (ADR 0193).** `PrepareService:41–56` answers the **first**
*Prepare the day* on a property, when no `room_task` exists yet, with
`roomcare.configure` **on the property**. The ruling makes that the first of two
phases. Phase 1 authorizes the **creation**, creates the tasks and commits.
Phase 2 comes after `room_task.created` is relayed: the object-scoped action
(`roomcare.assign` on the task) is authorized on the object. The permission is
never widened to property scope. So the first press stays on
`roomcare.configure`, as built, and is driven as **B8**. Every later
assign-gated act is in D and waits on ADR 0193's registration.

## Preconditions, checked at the start and quoted

1. Software Center lists **Room Care 0.1.3, Running**.
2. The signed-in user is admin on the property: the Board answers rather than
   drawing *Not permitted*. A refusal there ends the run as a precondition
   failure, not twenty failed rows.
3. Master Data holds the property's rooms, zones, room types, a `HK`
   department and a timezone — Room Care reads them through its install grant
   and draws nothing without them.

## A · Reads — `roomcare.read`, property scope (21 methods)

| # | Method | Reached by a person through | Served evidence | Read-back |
|---|---|---|---|---|
| A1 | `me` | the bar — name · department · property | | |
| A2 | `board` | **Board** (map and wall) | | |
| A3 | `room` | Board → a tile | | |
| A4 | `prepare` | **Prepare** | | |
| A5 | `attendants` | Prepare → **Move rooms…** | | |
| A6 | `states` | **Room states** (sheet · grid · compact) | | |
| A7 | `supervision` | **Supervision** | | |
| A8 | `deepCleans` | **Deep clean** | | |
| A9 | `myRooms` | an attendant's **My rooms** | | |
| A10 | `door` | My rooms → a room | | |
| A11 | `setup` | **Setup** › Windows & trigger · Rules | | |
| A12 | `services` | Setup › Services & minutes | | |
| A13 | `zones` | Setup › Assignment & zones | | |
| A14 | `areas` | Setup › Areas | | |
| A15 | `deepCleanPlan` | Setup › Deep clean plan | | |
| A16 | `grants` | Setup › Property-wide access | | |
| A17–A21 | the five widget reads | the five widgets on the desktop | | |

A read of an empty store is served under ADR 0148; a certificate made of empty
lists proves the pipe and not the logic, and will say so.

## B · Configure — `roomcare.configure`, property scope

| # | Operation | Reached by a person | Served | Read-back |
|---|---|---|---|---|
| B1 | `saveWindow` | Setup › Windows & trigger → Save | | |
| B2 | `savePolicy` | Setup › Rules → Save (and the trigger, the strategy) | | |
| B3 | `saveService` | Setup › Services & minutes → Save | | |
| B4 | `assignZone` | Setup › Assignment & zones → Move rooms… | | |
| B5 | `saveArea` | Setup › Areas → Edit… | | |
| B6 | `saveDeepCleanPlan` | Setup › Deep clean plan → Save | | |
| B7 | `grantManager` · `revokeManager` | Setup › Property-wide access | | |
| B8 | `prepare`, the **first** press on a property: phase 1 of RC-Q8 (ADR 0193), which creates the day's tasks | Prepare → Prepare the day | | |

## C · Plan — `roomcare.plan`, property scope

| # | Operation | Reached by a person | Served | Read-back |
|---|---|---|---|---|
| C1 | `planDeepClean` | Deep clean → plan a window… | | |
| C2 | `cancelDeepClean` | Deep clean → Cancel this deep clean… | | |

## D · Assign and amend — `room_task` scope

| # | Operation | Status |
|---|---|---|
| D1 | `prepare` (a later press) · `acceptProposal` · `assign` · `unassign` | **BLOCKED — ADR 0193 (ruled, unbuilt)** |
| D2 | `skip` · `defer` · `reduce` · `reprioritise` · `recordOnBehalf` | **BLOCKED — ADR 0193 (ruled, unbuilt)** |
| D3 | `clearDisagreement` · `decide` · `saveStates` | **BLOCKED — ADR 0193 (ruled, unbuilt)** — asked on the room's latest task |

Each is driven once to capture the refusal as evidence, and read back as
unchanged; none is scored. Under contract v2 the screen names the refusal's
cause. The row records the cause drawn (*not admitted*, *not granted*, or
*model cannot answer*) exactly as drawn, without predicting it.

## E · The attendant's acts — `room.clean`, riding the assignment

`start` · `pause` · `attempt` · `extraTime` · `restock` · `issue` — **not
reachable**: an act rides an assignment, and every way to make one is in D.
Downstream of D, not blocked by a check of its own.

## Open before the run — the read-back instrument

FF's question, unchanged and still unruled: Room Care's store sits behind the
sealed database credential, and ADR 0156 bars tests from bypassing it. Until an
instrument is ruled, the read-back column records *not measured here*, never a
value.

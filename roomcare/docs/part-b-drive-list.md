# Room Care Part B — the drive list

Prepared 2026-09-19 by KK against `HotelOsApps` at `105cb24` and platform
`HosPilotOS` at `8fa9de0c`, for the run after the owner installs
**`roomcare-0.1.1.hopkg`** — 16,721,717 bytes, sha256
`29bb38a08ab2c27ac5ac1fc745be798a05086e25415dcb00e3cc2388060eeea0`, signed by
`dev-local`, staged in `%LOCALAPPDATA%\HotelOS\packages\registry` on the
owner's machine. Its UI bundles are the bytes Part A measured at `71ff149`.
(0.1.0, built before Part A's style moves, was staged, never installed, and
removed.) **Nothing below has been run yet; every result cell is empty
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

**A finding against the build, reported and not relied on.** `PrepareService:41–56`
answers the **first** *Prepare the day* on a property — when no `room_task`
exists to ask on — with `roomcare.configure` **on the property**. That is an
assign-gated act answered at property scope, which CLAUDE.md names *"a privilege
expansion wearing a repair's clothes"*. It was an implementation choice of the
build (2026-09-13), now **RC-Q8**, with the planner. The code stays as it is
until the ruling, and it is not driven here as evidence that `roomcare.assign`
works.

## Preconditions, checked at the start and quoted

1. Software Center lists **Room Care 0.1.1, Running**.
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
unchanged; none is scored.

## E · The attendant's acts — `room.clean`, riding the assignment

`start` · `pause` · `attempt` · `extraTime` · `restock` · `issue` — **not
reachable**: an act rides an assignment, and every way to make one is in D.
Downstream of D, not blocked by a check of its own.

## Open before the run — the read-back instrument

FF's question, unchanged and still unruled: Room Care's store sits behind the
sealed database credential, and ADR 0156 bars tests from bypassing it. Until an
instrument is ruled, the read-back column records *not measured here*, never a
value.

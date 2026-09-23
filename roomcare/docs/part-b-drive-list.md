# Room Care Part B — the drive list

Prepared 2026-09-19 by KK, **re-headed 2026-09-20 for the cut Part B now runs on**:
**`roomcare-0.1.5.hopkg`**, 16,758,722 bytes, sha256
`2c25a99875059269b6de2384d154b9fc046bb83f10818247f1914fedab97070f`, signed by
`hotelos-packages-2026` (`dev-local`), staged in `%LOCALAPPDATA%\HotelOS\packages\registry`
on the owner's machine beside 0.1.2 and 0.1.3.

**Built from detached worktrees of both repositories, side by side, so every relative path resolved inside the cut
and none reached a shared tree: `HotelOsApps` `4d03931` and `HosPilotOS` `667a93ff`, both clean.** The UI was built
there with `node_modules` by junction; `dotnet publish -r win-x64 --no-self-contained` compiled `HotelOS.Common` and
`HotelOS.Platform` from the platform worktree. Both worktrees were removed afterwards, junction first.

```text
inside the archive   52 entries declared, 52 in the payload, signature present, 0 findings
                     (checked from the archive's own bytes, independently of hopkg)
native code          2 files, both x64: the service and Temporal's bridge — no foreign-platform natives
UI bundles           module.js in the package is byte-for-byte what Part A measured in the cut
                     (af082dbc…, 166,665 bytes; docs/part-a-certificate.md)
```

**Where it is staged is not where the owner's Kernel looks — EE, 2026-09-22, and checked here.** The archive sits in
the **user-scope** registry, `%LOCALAPPDATA%\HotelOS\packages\registry`, which is what the development run reads.
The installed product resolves the **machine-scope** root, `C:\ProgramData\HotelOS\packages`, **and that directory
does not exist** (measured: `ProgramData\HotelOS` holds config, logs, nats, pgsql, pki, secrets and services, no
`packages`). So the installed Kernel answers *"in the package registry not found"* for every archive, Room Care's
included. **Part B cannot begin with "Software Center lists Room Care 0.1.4" until that is resolved**, and the
resolution is not Room Care's: EE has it with the architect, alongside the property's Kernel binary being dated
2026-09-02, which predates the fix for the elevation defect `package list` still hits. *Recorded here because the
last premise I took on trust — "0.1.2 is installed" — was wrong the same way: a staging step that succeeded, in a
place the reader of it never looks.*

*Corrected 2026-09-23, EE's finding and re-measured here: **that is true of a machine that no longer exists.** The
owner has uninstalled the product. `C:\Program Files\HotelOS` is gone, no `HotelOS*` service is registered, and
nothing listens on the installed-product port. `C:\ProgramData\HotelOS` **survives** — config, logs, nats, pgsql,
pki, secrets, services, `identity-jwks.json` — and still has no `packages` directory, which is ADR 0129 /
`INSTALL-Q68` as designed: the uninstall leaves the property's data. So a fresh install at HEAD would meet an
existing data root, which is a different first run from a clean machine, and **whether the machine-scope gap
reappears is an open question rather than a known blocker**. The fresh install is the owner's to ask for, and is
unasked. Room Care blocks nothing either way: the package is cut, verified and staged.*

**Measured again 2026-09-23, 13:28, because the platform that now holds a hotel is a different platform from the
one those two paragraphs describe.** The architect reports 50 rooms, 2 buildings, 6 floors, 5 room types, 6
departments and 17 staff, created through the API. What is running is the **development run**, not an installed
product: `hotelos-kernel.exe` has no path on disk under `C:\Program Files\HotelOS` — that directory is still gone —
and `hotelos-desktop.exe` runs from `HosPilotOS\target\debug`. The kernel started 11:51 today with eight module
processes; the desktop at 12:55.

That matters to Part B in one way, and it is good news: **the development run reads the user-scope registry, which
is exactly where 0.1.5 is staged**, so the machine-scope gap above is not in the way of walking Room Care against
this hotel. `roomcare-0.1.5.hopkg`, 16,758,722 bytes, sha256 `2c25a998…070f` — re-hashed on the staged file today,
and equal to what EE verified.

**One thing does stand between the two, and it is not Room Care's to fix.** The registry's
`catalogue-index.json` was last written 2026-09-20 20:55 and **does not contain 0.1.5**; the package landed at
11:56 today, five minutes after the kernel started. `workforce-0.4.0.hopkg` is absent from it the same way, so
this is the registry's index, not this package. Whether the running registry rebuilds that index on a scan or
reads it as written is EE's question; if it is rebuilt at startup, a restart of the development run is the whole
of it. **Not done here: the owner's platform is running with a hotel in it, and restarting it is theirs or EE's
to decide, not a check I take on myself.**

**Part B certifies 0.1.5**, the first Room Care the owner installs — ruled by the owner on 2026-09-22, because it
carries their own 64g §2 B failure-card change, which 0.1.4 was cut two days too early to have. 0.1.2, 0.1.3 and
0.1.4 were only ever on the shelf:
measured 2026-09-19, Room Care is installed on no Kernel — the owner's running platform holds guestops, jobs, openai
and workforce, the `roomcare` schema has no tables, and the running Kernel's log has no line naming `roomcare`. *That
corrected a relayed premise I had written in without checking: "0.1.2 was already installed".* **So Part B starts
with an install, not an update.** Neither 0.1.0 nor 0.1.1 was ever installed, and each was removed, so no version
names two builds.

**A first cut's weight is worth stating**: the first publish of this package put 68 MB on the shelf, because a plain
`dotnet publish` carries Temporal's Linux and macOS natives — 166 MB of `backend/runtimes` — into a Windows package.
The size against 0.1.3's is what showed it. Pinning the runtime prunes them, and the check above counts native files
rather than trusting the number.

**Nothing below has been run yet; every result cell is empty on purpose.** The shape is FF's
(`guestops/docs/part-b-drive-list.md`), so the two certificates read alike.

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

1. Software Center lists **Room Care 0.1.5, Running**.
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

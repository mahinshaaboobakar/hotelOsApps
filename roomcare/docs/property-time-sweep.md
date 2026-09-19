# Room Care in the property's time zone — every line that reads or makes a time

Owner instruction, 2026-09-19: every app works in the property's time zone. "Today" comes from the property's
calendar, and a wall-clock time becomes an instant only through the property's zone, never at offset zero. Workforce's
`b5c5ffc` is the reference.

Swept by call shape with no limit, at the commit that carries this page. Non-test code only: `backend/src` without
`bin`, `obj` or the EF-generated `Migrations`, and the UI's own sources (`application.ts`, `main.ts`, `model.ts`,
`chrome/`, `screens/`, `widgets/*.ts`). The preview harness and the tests do not ship and are not counted.

**Shapes searched.** Backend, first pass: `GetUtcNow()`, `DateTime.Now`, `DateTime.UtcNow`, `DateTime.Today`,
`.UtcDateTime`, `DateOnly.FromDateTime`, `TimeOnly.FromDateTime`. Second pass: `TimeSpan.Zero`, `DateTimeKind`,
`SpecifyKind`, `.ToDateTime(`. Third pass, the other ways to read or make a time: `DateTimeOffset.Now`,
`DateTimeOffset.UtcNow`, `DateTimeOffset.Parse`, `DateTimeOffset.TryParse`, `new DateTimeOffset(`, `ToUniversalTime`,
`ConvertTime`. UI: `new Date(`, `Date.now()`, `toISOString`, `getUTC…`, `setUTC…`, `getTimezoneOffset`.

## Backend — 26 lines

| # | Line | What it does | Class |
|---|---|---|---|
| 1 | `Application/Day/ProposalService.cs:69` | `now` becomes each proposal's `AssignedAt` | stored instant |
| 2 | `Application/Days/PropertyClock.cs:11` | the present instant, from which the property's day is derived | stored instant |
| 3 | `Application/Rooms/DisagreementService.cs:24` | the time a disagreement is cleared, written on the room | stored instant |
| 4 | `Application/Standard/ManagerGrants.cs:59` | `GrantedAt` | stored instant |
| 5 | `Application/Standard/ManagerGrants.cs:78` | `RevokedAt` | stored instant |
| 6 | `Application/Standard/StandardService.cs:39` | `ChangedAt` | stored instant |
| 7 | `Application/Supervision/SupervisionLane.cs:27` | `OpenedAt`; the lane's day comes in as the property's | stored instant |
| 8 | `Application/Tasks/TaskWriter.cs:15` | `writer.Now`. All 28 uses are timestamps (`AssignedAt`, `StartedAt`, `EndedAt`, `At`, `OccurredAt`, `DecidedAt`), except one comparison with `EarliestAt`, which is an instant too (see #26) | stored instant |
| 9 | `Events/RoomStateObservedHandler.cs:38` | when a PMS observation was recorded | stored instant |
| 10 | `Events/StayHandlers.cs:21` | when an arrival was recorded | stored instant |
| 11 | `Events/StayHandlers.cs:46` | when a departure was recorded | stored instant |
| 12 | `Events/StayHandlers.cs:64` | when a room move was recorded | stored instant |
| 13 | `Infrastructure/UtcInstant.cs:7` | the column converter: every stored instant is written as UTC | stored instant |
| 14 | `Module/Projections/RoomProjection.cs:99` | a timeline entry's time on the wire, ISO with `Z` | stored instant |
| 15 | `Module/Projections/RoomProjection.cs:100` | the same | stored instant |
| 16 | `Module/Projections/RoomProjection.cs:104` | the same, an attempt | stored instant |
| 17 | `Module/Projections/RoomProjection.cs:105` | the same, an observation | stored instant |
| 18 | `Module/Projections/RoomProjection.cs:107` | the same, an issue | stored instant |
| 19 | `Application/Days/PropertyClock.cs:25` | the time of day, from the instant converted into the property's zone | already the property's day |
| 20 | `Application/Days/PropertyClock.cs:36` | `LocalDate`: the property's calendar date, through its zone | already the property's day |
| 21 | `Domain/OperatingDay.cs:30` | `ConvertTime` into the property's zone | already the property's day |
| 22 | `Domain/OperatingDay.cs:31` | the date of that property-local time | already the property's day |
| 23 | `Domain/OperatingDay.cs:32` | its time of day, compared with the property's boundary | already the property's day |
| 24 | `Domain/OperatingDay.cs:39` | a property date and time joined into a wall-clock value | correct wall-clock arithmetic |
| 25 | `Domain/OperatingDay.cs:40` | that wall-clock value made an instant at the **zone's own offset** (`zone.GetUtcOffset(local)`), never `TimeSpan.Zero`. All nine `InstantOf` callers (windows, day end, sold-at, the due tick, the room's day start) go through here | correct wall-clock arithmetic |
| 26 | `Module/Capabilities/TimeAndListParameters.cs:41` | parses `notBefore` for defer. **It was a defect**: a time with no offset was read in the *server's* zone, which is neither UTC nor the property's zone, and is invisible on a server set to the property's zone. It now requires `Z` or `±hh:mm`, and refuses a bare time | **defect: property wall-clock time treated as UTC (here, the server's zone): fixed in this change** |

| Class | Lines |
|---|---|
| stored instant | 18 |
| already the property's day | 5 |
| correct wall-clock arithmetic | 2 |
| defect: property wall-clock time treated as UTC | 1, fixed |
| defect: today taken as the UTC day | 0 now. **2 fixed in `9d7a6507`**: `DayDecision.cs:70` and `BoardProjection.cs:77` counted "vacant for N days" from the UTC date |
| **total** | **26** |

`GetUtcNow()` alone is 12 of these lines (#1–#12). None is `TimeSpan.Zero`, `.UtcDateTime` or `SpecifyKind`.

**`new DateTimeOffset(wallClock, TimeSpan.Zero)`, Workforce's worst shape: none in Room Care.** Its only
`new DateTimeOffset(` is #25, at the zone's offset.

## Found after the sweep: two times said to a person in UTC

The call shapes above find where a time is read or made. They do not find where an instant is **turned into words**.
HH's refusal-words rule (Jobs, the same day) turned up two such lines. Both formatted an instant in whatever offset
it carried, then labelled it UTC:

| Line | What a person read | Now |
|---|---|---|
| `Application/Work/AttendantWork.cs:35` | at the door: "the guest asked for this room not before 09:00 UTC" (the stored instant, in UTC) | "…not before 14:30", in the property's time |
| `Application/Work/AmendService.cs:40` | in the room's history: "not before 14:30 UTC" (the sender's offset, mislabelled), or "20:30 UTC" when sent in UTC | "not before 14:30", in the property's time |

Both are class **defect: property wall-clock time treated as UTC**, and both are fixed. Held by `NotBeforeWordsTests`,
in Kolkata and Guatemala, which was red first ("14:30 UTC"; "20:30 UTC"). `ClockShapeTests` now also refuses an
interpolated `{…:HH…}` and a string saying "UTC". The two lines are not in the 26 above: that count is of the
listed call shapes, and these are formatting.

## UI — 0 lines of Room Care's own

No `new Date(`, `Date.now()`, `toISOString`, `getUTC…`, `setUTC…` or `getTimezoneOffset` in Room Care's own UI
sources. Every date and time on a screen goes through the SDK's formatters with the property's zone and locale
(`chrome/instant.ts`), and a property-local time is sent to the service as a time, never as an instant (Room states'
sold-at becomes an instant only in the service, through #25).

The built bundles (`module.js` and the five widgets) contain **41** such lines. All are the platform SDK's code,
bundled in, and none is Room Care's. They are 9 distinct SDK lines:

| SDK line (HosPilotOS `packages/sdk-typescript/src/`) | In bundles | Class |
|---|---|---|
| `instant.ts`: `new Date(iso)` | 4 | stored instant |
| `instant.ts`: `formatDay` in a UTC carrier, for a date-only value | 1 | already the property's day (a calendar date, not shifted) |
| `instant.ts`: `Date.UTC(…)` from the zone's own parts | 4 | correct wall-clock arithmetic |
| `instant.ts`: `getUTC…` reading that carrier back (two lines) | 8 | correct wall-clock arithmetic |
| `read.ts`: `new Date()` when a read failed (two lines) | 12 | stored instant |
| `failure.ts`: `toISOString()` of that time (two lines) | 12 | stored instant |
| **total** | **41** | 28 stored instant · 1 property's day · 12 wall-clock arithmetic |

## What guards it

`backend/tests/ClockShapeTests.cs` reads every source file (90 on 2026-09-19, derived, never listed) and refuses:

- `.UtcDateTime`;
- the machine's clock (`DateTime(Offset).Now/UtcNow/Today`);
- `SpecifyKind` / `DateTimeKind.Utc`;
- `new DateTimeOffset(…TimeSpan.Zero…)`;
- any `new DateTimeOffset(` outside `OperatingDay.cs`;
- `DateOnly/TimeOnly.FromDateTime` outside the property's calendar (`OperatingDay.cs`, `PropertyClock.cs`);
- `DateTimeOffset.(Try)Parse` outside the one parameter parser, which requires the offset first.

Each allowance is itself asserted to still hold its shape, and a positive control plants every shape.

**Red first:** run at `d38f69f3`, before `9d7a6507`, it names `DayDecision.cs:70` and `BoardProjection.cs:77`. The
parser's defect is held by behaviour: a bare time is refused, and an offset time is the right instant in Kolkata
(+05:30), Guatemala (−06:00) and UTC. That test was red before the fix. The day counts are held by
`PropertyDayTests`, in Kolkata and Guatemala, which fail in opposite directions on the old code.

## Pending WF-Q21: calendar day or operating day

The planner is ruling whether an application's day is the calendar day or the hotel's operating day. Room Care's
chapters make it the operating day: 01 §6.1, R12, "the day rolls on the property's operating day". Every "today" and
every "the day an instant fell on" comes from one function, `OperatingDay.At`, labelled pending WF-Q21 at the code,
as is `PropertyDaySettings.DayOf`. The instruction to build to the calendar day conflicts with those chapters, so it
is not applied here. Until WF-Q21 is ruled, both sides of every count follow one rule. A ruling for the calendar day
is a change in that one function.

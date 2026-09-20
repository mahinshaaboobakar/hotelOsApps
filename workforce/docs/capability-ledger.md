# Workforce — what each button does, for the owner before testing

**Derived from the code at HotelOsApps `686318b8` (2026-09-19), not from memory
and not from a walk.** Nobody has pressed these on the owner's platform yet. The
last column is left empty for the live walk, which marks each one **works /
refuses (with the reason shown) / not built / broken**.

**Part A is not sign-off.** The Part A pages compared drawings with a harness
rendering of fixtures. They never showed whether a button does anything, and
nothing here is done until it has been walked on the installed platform.

**The walk is on 0.3.4** — staged in the property's registry on 2026-09-19
(`sha256:d9c46270…81a8`, cut from HotelOsApps `51d3cb6c` and HosPilotOS
`55397fe2`), for the owner to install after the restart. 0.3.3
(`sha256:2b6775…a6d0`) is what was installed before it, and this page has never
described 0.3.3.

**Two defects were fixed after that cut**, so they are marked in the table at
the end and will not be in the walk: the rota's keyboard-unreachable cells, and
half of the shift-colour mapping.

## How to read the Status column

| Status | Meaning |
|---|---|
| **calls** | A live control. It asks the named operation, under the named capability, and shows the service's own sentence if refused |
| **off** | Drawn dashed and disabled, with its reason shown beside it or on hover. Nothing behind it can do this yet |
| **not built** | The service can do it and no screen offers it. There is no control, so nothing looks live |
| **defect** | Known wrong, recorded below, not yet fixed |

A **read** is what the screen asks for when it opens. A refused or failed read
draws a failure state that names what it could not read, and a Retry. It never
draws recorded rows.

## The guard behind "nothing looks live and does nothing"

`ui/tests/no-dead-controls.test.ts` walks every screen and widget. Anything
drawn as pressable (`.sel .btn .tab .pk`) must be a real `button`, `select` or
`input`. It was red on 8 surfaces before this round: six department/person
pickers and Leave's two tabs. **What it cannot see:** a real button with no
listener. That is held by each write's own test, and by this ledger, which
names the operation behind every live control.

`ui/tests/mouse-only.test.ts` is the same rule keyed on behaviour rather than
on a class: while each screen draws, it records every click listener and the
element it was given to, and each must be a control a keyboard reaches. It
found the rota's cells, which the check above could not see.

**Why its 42 reds are believable.** Its first version watched the wrong place,
recorded nothing, and passed every screen — a clean run that had measured
nothing at all. It caught itself, because it also checks that it saw at least
twenty listeners before judging any of them. **A count a guard relies on needs
its own guard**, or the guard's silence reads exactly like a pass.

`ui/tests/developer-content.test.ts` walks the same surfaces for developer
notes: register ids, ADRs, §, design-section and chapter references, code
identifiers, and the platform systems by name. It reads the estate's shared
list (`scripts/developer-content.ts`), and a planted positive control proves it
can find each one.

## Rota → Team rota

Read: `roster.read · week`, with the week it is on (`screens/rota/index.ts`).

| Control | File:line | Status | What it does | Walk |
|---|---|---|---|---|
| ‹ Previous week / Next week › | `screens/rota/index.ts:164,168` | **calls** | re-reads `roster.read · week` for the week before or after | |
| ⧉ Copy last week | `screens/rota/index.ts:170` → `copy.ts:57` | **calls** | confirms first, then `roster.plan · copyWeek`; fills empty cells only, for every department shown | |
| ⇄ Swap | `screens/rota/index.ts:173` | **off** | "Swapping two shifts is not available here yet." | |
| ⎙ Print | `screens/rota/index.ts:174` | **calls** | opens the printed week (below) | |
| ＋ Assign shift (header) | `screens/rota/index.ts:177` | **off** | "Pick a cell in the week to assign a shift." | |
| All departments ▾ | `chrome/department.ts:19` | **off** | "Every department is shown. Choosing one is not built yet." | |
| A cell in the grid | `screens/rota/grid.ts:48` | **calls** | opens the shift picker. **Mouse only**, see defect D4 | |
| Shift picker → Assign | `screens/rota/picker.ts:130` | **calls** | `roster.plan · assign`, under the **row's** department | |

**Fixed this round, found building this ledger (8af4bf8b).** No screen names a
department, so the week read answered department `""`, the picker sent that,
and the service refused **every** assignment with *"department_code is
required"*. On 0.3.3 no shift can be assigned from the rota. Each row now
carries its person's department. Tested red-first on the service's own refusal.

## Rota → Staff schedule

Read: `roster.read · schedule`, for the signed-in person.

| Control | File:line | Status | What it does | Walk |
|---|---|---|---|---|
| The person picker | `screens/schedule/index.ts:113` | **off** | "Another person's schedule cannot be opened here yet." | |
| ‹ month › | `screens/schedule/index.ts:124` | **off** | "Other months cannot be opened here yet." | |
| ⇄ Propose swap | `screens/schedule/index.ts:126` | **off** | "Swaps cannot be proposed from here yet." | |
| ＋ Request leave | `screens/schedule/index.ts:127` | **off** | "Leave is requested from Leave & Requests." | |

## Rota → printed week

Reads: `roster.read · week` and `· register` (`screens/printed/index.ts:51-52`).

| Control | File:line | Status | What it does | Walk |
|---|---|---|---|---|
| ‹ Back to the rota | `screens/printed/index.ts:111` | **calls** | returns to the rota, same week | |
| Page setup | `screens/printed/index.ts:114` | **off** | "Page setup is not available yet." | |
| ⎙ Print | `screens/printed/index.ts:118` | **off** | "Printing is not available here yet." | |

## Leave & Requests

Read: `roster.read · leave` (`screens/leave/index.ts:35`).

| Control | File:line | Status | What it does | Walk |
|---|---|---|---|---|
| Requests / Approvals tabs | `screens/leave/index.ts:111` | **calls** | switches tab. Now buttons, so a keyboard reaches them | |
| ＋ Request leave | `screens/leave/index.ts:96` → `form.ts:80` | **calls** | opens the form; Raise request → `leave.request · raise` | |
| Swap card → Decline… / Approve swap | `screens/leave/approvals.ts:114-115` | **off** | "Swaps cannot be decided here yet." | |
| Approve / decline / adjust a **leave** request | none | **not built** | `leave.approve` exists in the service; the queue shows rows with no action. See D5 | |
| Withdraw my request | none | **not built** | `leave.request · withdraw` exists; no control. See D5 | |

## Attendance

Read: `roster.read · day` (`screens/attendance/index.ts:23`).

| Control | File:line | Status | What it does | Walk |
|---|---|---|---|---|
| All departments ▾ | `screens/attendance/index.ts:47` | **off** | as on the rota | |
| ‹ › (other days) | `screens/attendance/index.ts:54` | **off** | "Other days cannot be opened here yet." | |
| ＋ Mark attendance | `screens/attendance/index.ts:55` | **off** | "Attendance cannot be marked here yet." | |

## Duty

Read: `roster.read · register` (`screens/duty/index.ts:57`).

| Control | File:line | Status | What it does | Walk |
|---|---|---|---|---|
| ‹ week › | `screens/duty/index.ts:100` | **off** | "Other weeks cannot be opened here yet." | |
| ＋ Assign duty | `screens/duty/index.ts:107` → `dialog.ts:71` | **calls** | opens the dialog; choose a person (`dialog.ts:190`), then `duty.assign · assign` | |
| Amend / withdraw a duty | none | **not built** | the service has both; no control | |

## People → Postings

Read: `roster.read · people`, paged (`screens/people/index.ts:52`).

| Control | File:line | Status | What it does | Walk |
|---|---|---|---|---|
| All departments ▾ | `screens/people/index.ts:131` | **off** | as on the rota | |
| ＋ Post a staff member | `screens/people/index.ts:132` (and first run `:297`) | **off** | "Postings cannot be made here yet." | |
| A posting row | `screens/people/index.ts:232` | **calls** | reads `roster.read · ending` and opens End posting | |
| End posting (destructive) | `screens/people/end-posting.ts:62` | **calls** | `posting.assign · end` | |
| Page ‹ n › | `chrome/pager.ts:113,126` | **calls** | re-reads that page | |

## People → Teams

Read: `roster.read · teams` (`screens/teams/index.ts:67`).

| Control | File:line | Status | What it does | Walk |
|---|---|---|---|---|
| ＋ Form a team | `screens/teams/index.ts:185` (none formed: `:361`) → `form.ts:106` | **calls** | `posting.assign · form` | |
| All departments ▾ | `screens/teams/index.ts:205` | **off** | as on the rota | |
| Show stood down | `screens/teams/index.ts:207` | **off** | "Stood-down teams cannot be shown here yet." | |
| A team row | `screens/teams/index.ts:303` | **calls** | opens that team | |
| ‹ day › (team open) | `screens/teams/index.ts:192` | **off** | "Other days cannot be opened here yet." | |
| Rename | `screens/teams/detail.ts:42` → `rename.ts:52` | **calls** | `posting.assign · rename` | |
| Stand down | `screens/teams/detail.ts:42` → `stand-down.ts:71` | **calls** | `posting.assign · standing` | |
| ＋ Add a member | `screens/teams/detail.ts:45` → `member.ts:72` | **calls** | `posting.assign · addMember` | |
| Remove (a member) | `screens/teams/detail.ts:94` | **off** | "Members cannot be removed here yet." | |

## Reports

Read: `roster.read · month` (`screens/reports/index.ts:29`).

| Control | File:line | Status | What it does | Walk |
|---|---|---|---|---|
| All departments ▾ | `screens/reports/index.ts:67` | **off** | as on the rota | |
| ⎙ Print | `screens/reports/index.ts:68` | **off** | "Reports cannot be printed here yet." | |
| ↓ Export CSV | `screens/reports/index.ts:69` | **off** | "Reports cannot be exported here yet." | |

## Policy, and Policy → Shifts

Read: `roster.read · policy` (`screens/policy/index.ts:50`, `screens/shifts/index.ts:52`).

| Control | File:line | Status | What it does | Walk |
|---|---|---|---|---|
| Daily / weekly threshold + Save changes | `screens/policy/index.ts:209` → `:229` | **calls** | `roster.configure · setOvertime`; off, with its reason, until a valid change is typed | |
| ＋ New shift | `screens/policy/index.ts:87`, `screens/shifts/index.ts:90` → `policy/dialog.ts:96` | **calls** | `roster.configure · defineShift` | |
| Rename / reschedule / retire a shift; edit a leave type | none | **not built** | the service has them; the policy read carries no id or version to send | |

## Widgets

Reads: `attendanceToday · comingUp · onLeave · pendingRequests · shiftBoard`
(`widgets/panel/*.ts`).

| Control | File:line | Status | What it does | Walk |
|---|---|---|---|---|
| A widget row | `widgets/card.ts:202` | **calls** | opens Workforce at the section it names | |
| A widget that could not read: its one action | `widgets/card.ts:334` | **calls** | Retry re-reads the widget; where the failure offers it, opens Workforce instead | |

## Known defects, not fixed in this cut

| # | What | Where | Effect a person would see |
|---|---|---|---|
| D1 | **"Today" is the UTC day** outside the duty/rota/schedule reads fixed in `b5c5ffce`. **The day now comes from the platform's Context Service** — the property's operating day, ruled in ADR 0211, not a day Workforce works out for itself. **A day that cannot be read says so**: never a UTC day and never a calendar day standing in for one | 19 day-from-clock lines — e.g. `AttendanceView.cs:36-38`, `CapabilityService.cs:235`, `LeaveService.cs:71`, `PostingService.cs:265,402`, all five `Summaries/*` | between local midnight and 05:30 at an Indian property, Attendance and the widgets show yesterday. A negative-offset property is wrong in the evening instead |
| D1b | **A shift's own hours are read as if they were UTC**: a 07:00 start is treated as 07:00 UTC | `ShiftBoundaryAnnouncer.cs:147`, `ShiftBoardSummary.cs:203` and the lines that follow them | at +05:30 a 07:00 shift is announced as started at 12:30 local, and the shift board's "now" and "next change" are 5½ hours out |
| D2 | **Attendance's date is written by the server** in the server's own language | `AttendanceView.cs:67` (`ToString("dddd d MMMM")`) | a property whose language differs from the server's reads the date in the server's |
| D3 | **Shift colour → tone mapped in three places**. **Half fixed after the cut** (`6bc7e7c6`): the screens now draw the tone the service sends, and the service starts sending it in GG's next service round | `screens/shifts/index.ts:36`, `screens/policy/index.ts:36`, `Wording.cs:46` | a Rose shift reads neutral on one screen and red on another |
| D4 | **Rota cells work with a mouse only**. **Fixed after the cut** (`2904639e`) | `screens/rota/grid.ts:48` (a `div` with a click) | a keyboard cannot reach a cell to assign a shift |
| D5 | **A manager cannot approve or decline leave from any screen**; a person cannot withdraw a request | `screens/leave/approvals.ts:36` (rows with no action) | an approver meets the queue and has nothing to press |
| D6 | **The leave read sends no id or version**, which approve, decline and withdraw all require | `LeaveView.cs` — `Queue` and `Request` | **whichever design is chosen for D5 cannot be built until this lands**, so it is done first |
| D7 | **The service writes the attendance verdict as English** — `"Late 20 min"` — and the screen counts late people by reading that sentence back | `AttendanceView.cs:170`, `screens/attendance/index.ts:66` | a property in another language reads the server's English, and the count breaks the day the sentence changes |
| D8 | **The service writes "412 assignments"**, unformatted, and "1 assignments" for one | `PolicyView.cs:63` | the number is not in the property's own number format, and the grammar is wrong for one |

## For the owner to decide (queued, not decided)

These are drawn elements where it is unclear whether they are screen content
or developer notes, plus one choice the fix above needed. Each will be
**drawn** for you before anything is built.

1. **The failure facts block** reads "Asked for: roster.read · week". The
   ruled failure treatment shows it; the developer-content ruling says no
   system names on screen. The two rulings disagree.
2. **Coming Up's foot**: "no staffing demand model".
3. **Reports' note**: "inputs, not a payslip".
4. **New shift and Form a team field notes.**
5. **Add member's** "next week's postings" note.
6. **"Available from"** on New shift is built but not drawn in the frame.
7. **A person posted to two departments** is assigned rota shifts under their
   primary posting (8af4bf8b). Is that right?

## Drawn for the owner after the cut (fixture versus service)

The harness fixtures and the real service disagree here. Nothing will be
decided until you have seen both side by side: Attendance's four figures
against the service's three; late-in rows; pending rows; the schedule's tail
and today border; and the rota's "Zone 1/2/3", which the fixture draws and the
service never sends (`RotaView.cs`, `zone = null`).

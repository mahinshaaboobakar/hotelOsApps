# Room Care — what every screen and button does

Derived from the code at `15e2654` (2026-09-19, KK), not from memory and not from the drawings. Every control a
person can press is listed with its file and line. **Nothing here has been walked on the owner's platform yet**:
Room Care is not installed there (below). The *Expected on your platform* column is what the code will do. The
live walk (Part B) replaces each expectation with what actually happened: **works**, **refuses** (with the reason
shown), **not built**, or **broken**.

## First: Room Care is not installed on your platform

Measured on 2026-09-19, on the platform you run (`.tmp/devrun`):

- The installed applications are GuestOps 0.3.2, Jobs 0.4.2, OpenAI 1.1.0 and Workforce 0.3.3.
- Room Care has no row in the platform's package list and no files in the installed-packages folder.
- Its database schema has no tables, and the platform's log since 8 September never mentions it.
- Room Care 0.1.3 is **on the shelf** in Software Center, ready to install. Its digest `c3485aa2…` is the one that
  was built.
- Earlier reports said 0.1.2 was installed. That was wrong, and the documents are corrected.

So there is **no live Room Care screen to compare with the drawings yet**. The frame-versus-live comparison needs
Room Care installed first. There is a choice between two versions to install; it is set out at the end of this page.

## The one thing that decides most of this page

**Assigning rooms and changing a room's service cannot be granted to anyone yet.** The platform can grant a
permission on a property (Room Care checks read, set up and plan that way). It cannot yet grant one on a single
room's task for an application you install. Room Care checks assign and change on a room's task, so today every
one of those actions is refused, for every person. The platform work that fixes this is under way. *For engineers:
AUTHZ-Q37 / Q37c. Room Care's manifest has not yet declared its `room_task` type, and the Kernel does not yet read
such declarations.*

What this means on your screen, **expected, to confirm on the walk**:

- The desktop shows Room Care's sections by the permissions the platform says you hold. As property admin you hold
  **read, set up and plan**, so you should see **Board, Deep clean and Setup**.
- **Prepare, Room states and Supervision should not appear at all**, because nobody holds assign or change.
- On a room's page, **Room state…, Record an exception…, Reassign… and Keep ours / Take theirs** should not appear,
  for the same reason.
- If one is reached anyway, the refusal will read *"That was not permitted, so nothing was changed."* That sentence
  is true, but it does not say the real reason: nobody can be given this permission yet. This is a finding, and is
  listed at the end of this page.

## Screen by screen

`works` below means the code calls an operation that exists in Room Care's service, under a permission you hold.
"On this screen only" means the control changes what is shown and sends nothing.

### The bar (every screen)

| Control | File | What it does | Expected on your platform |
|---|---|---|---|
| Section tabs | `chrome/bar.ts:25` | goes to the section | works |
| Try again, on a screen that could not read | `chrome/failure.ts:104` | reads again | works |
| Copy details, on a refusal | `chrome/failure.ts:107` | copies the service's words | works |

### Board — needs read

| Control | File | What it does | Expected |
|---|---|---|---|
| Map / Wall | `screens/board/index.ts:53-54` | on this screen only | works |
| ~~Zone~~ | was `:56` | **was dead: it did nothing.** Now the words "grouped by zone" (`07eb133`) | fixed |
| Sold tonight / Attention filters | `:60` | on this screen only | works |
| One attendant's rooms (list) | `:68` | on this screen only | works |
| Collapse all / Expand all (Wall) | `:73` | on this screen only | works |
| A room tile / a wall row / a zone header | `board/map.ts:46`, `board/wall.ts:52`, `:35` | opens the room / folds the zone | works |

### A room's page — needs read; its actions need change or assign

| Control | File | Calls | Expected |
|---|---|---|---|
| Today / History · 14 days / Record | `screens/room/index.ts` subnav | on this screen only | works |
| Room state… → Save | `:67`, `room/acts.ts:41` | saves the room's state | **refused (nobody can be granted change); expected hidden** |
| Raise a job for this room… | `:68` | drawn off: "Jobs decides who may" | drawn off |
| Record an exception… | `:69`, `acts.ts:63` | records the attendant's attempt for them | **refused; expected hidden** |
| Reassign… | `:71`, `acts.ts:90` | assigns the room to another attendant | **refused; expected hidden**. Drawn off, with its reason, once the service has ended |
| Keep ours / Take theirs | `:97` | settles a disagreement | **refused; expected hidden** |

### Prepare — needs assign: **expected not to appear**

| Control | File | Calls | Expected if reached |
|---|---|---|---|
| Prepare the day | `screens/prepare/index.ts:79` | prepares the day | The **first press on a property with no tasks is checked as set up**, so it works for you. **Every later press is checked as assign**, so it is refused |
| Add the new rooms (n) | `:82` | same | refused. Drawn off when there is nothing new, and says so |
| Show what changed | `:83` | scrolls to the changes | works |
| A change row | `:95` | opens the room | works |
| Pager | `:100` | reads the page | works |
| Assign anyway… / Move rooms… | `:120`, `:138` | assigns | refused |
| Accept the proposal | `:132` | assigns every proposed room | refused |

### Room states — needs change: **expected not to appear**

| Control | File | Calls | Expected if reached |
|---|---|---|---|
| Sheet / Tap grid / Compact; every cell, tile, select-all | `screens/states/*` | on this screen only | works |
| ~~Zone~~ | was `sheet.ts:32` | **was dead: it did nothing.** Now the words "grouped by zone" | fixed |
| Apply to selected | `sheet.ts:126` | on this screen only. **Was dead with nothing selected**; now drawn off, "select rows first" | fixed. **Still dead in one case**: rows selected but no value chosen does nothing |
| Save n changes | `states/index.ts:64` | saves every change in one call | **refused** |
| Discard | `:68-69` | on this screen only. **Was dead with nothing to discard**; now drawn off | fixed |

### Supervision — needs change: **expected not to appear**

| Control | File | Calls | Expected if reached |
|---|---|---|---|
| A row | `screens/supervision/index.ts:64` | opens the room | works |
| Keep ours / Take theirs | `:96` | settles a disagreement | refused |
| DND approved / Clean it / Other… / Leave for reconcile | `:98-103` | records the supervisor's decision | refused |
| Assign anyway… | `:98` | opens the room (then Reassign is refused) | works, then refused |
| Pager | `:72` | reads the page | works |

### Deep clean — needs plan: **works for you**

| Control | File | Calls | Expected |
|---|---|---|---|
| plan a window… | `screens/deepclean/index.ts:77` | plans the deep clean for a window | works |
| Cancel this deep clean… | `:101` | cancels it | works |
| Pager | `:83` | reads the page | works |

### Setup — needs set up: **works for you**

Every save on the seven tabs calls an operation Room Care serves under set up, and you hold it.

| Tab | Controls | File | Expected |
|---|---|---|---|
| Windows & trigger | window times, on/off, trigger → Save / Discard | `setup/windows.ts:62-65`, `setup/controls.ts:78-86` | works. Save is drawn off until something changes |
| Services & minutes | room type chips, a service row, minutes → Save | `setup/services.ts:40,67,96` | works |
| | Reorder… phases | `setup/phases.ts:59,102` | works |
| | Add a phase | `setup/phases.ts:59` | drawn off: "the five are the owner's" (the reason is new in `07eb133`) |
| | Copy… to other room types | `setup/phases.ts:68` | works. Drawn off, with its reason, when the property has one room type |
| Rules | every rule → Save; Reorder… the ladder | `setup/rules.ts:56,79` | works |
| Assignment & zones | strategy → Save; Edit… a zone; Move rooms between zones… | `setup/zones.ts:71,79,88,114` | works |
| Areas | All / Without a routine; Edit… / Add a routine… → Save; pager | `setup/areas.ts:55-103` | works |
| Deep clean plan | every months value → Save | `setup/plan.ts:63` | works |
| Property-wide access | Grant to a person… / Revoke… | `setup/access.ts:66,58,85,97` | works for set up. Whether the granted person then gets assign and change on rooms waits on the same platform work as above |

### My rooms — for an attendant: **unreachable today**

An attendant sees only the rooms assigned to them. Nobody can assign yet, so this list is empty everywhere.

| Control | File | Calls | Expected once rooms can be assigned |
|---|---|---|---|
| A row | `screens/myrooms/index.ts:68` | opens the door screen | works |
| Pager | `:81` | reads the page. **Was dead: it did nothing** | fixed (`07eb133`) |
| Start / Pause / Resume | `myrooms/door.ts:70` | starts or pauses the work | works. Allowed only for the room's own attendant |
| Ask for extra time… / Found an issue… / End… | `door.ts:71-72` | records it | works |
| Photo | `door.ts:71` | drawn off: "the media service's to add" | drawn off. The words are queued below |

### The five widgets

Rooms Ready, Arrivals Waiting, Attention, Attendants Now and Pending each read under read. A row opens the room
in Room Care (`widgets/card.ts:102`), and a failed widget offers Try again (`:137`). Expected: **works**.

## How dead controls are kept out

`tests/live.test.ts` presses every enabled button on every screen and requires it to do something visible: a call,
a change on screen, or a scroll. Before the fixes it named four screens:

- the two **Zone** chips (Board, Room states);
- **Discard** and **Apply to selected** when there was nothing to act on;
- **My rooms' pager**.

It is green at `07eb133`. It cannot see the one case left: *Apply to selected* with rows selected and no value
chosen.

## Developer notes on the screens (owner ruling, 2026-09-19)

**Removed** (`15e2654`). Each was a note for the developer drawn on the mock and built as UI:

| Where | What it said | Now |
|---|---|---|
| Deep clean, a job cell and the job card | "progress · JOBS-Q2", "hands by day · JOBS-Q2", "today Room Care hears only job.created / job.closed", "as Jobs publishes it" | gone. The *Hands* row is no longer drawn |
| Deep clean, plan a window | "Two requests leave with correlation ids: the block, to the owner of the room's out-of-order state, and the job, to Jobs." | "The room leaves the day while it is blocked." |
| Deep clean, block cell | "applied by the state's owner" | gone |
| Prepare, who is here | "from Workforce — … grouped by department until the zone is on the posting", with "Workforce ask · zone on the posting" | "n posted to Housekeeping" |
| Setup · Property-wide access | "read through Context" with "PKG-Q8"; "(S6; AUTHZ-Q25)"; "roomcare_manager" three times; "(rides the assignment)", "(the inspection app's)", "(its owner's)" | a dash in the posting column; "property-wide access"; the parentheses gone |
| Setup · Deep clean plan | "JOBS-Q2" twice, "architect · ADR 0051/0056", "raised by Room Care with a correlation id", "requested by Room Care, placed by the state's owner", "once Jobs publishes them" | gone |
| Setup · Assignment & zones | "(RoomZoneAssignment, ADR 0044 — Room Care's)"; "read through Context" with "Workforce ask · zone on the posting"; "from Workforce through Context"; "(S0)" | gone |
| Setup · Services, phases | "requested from the inspection app" with "RC-Q1(6)" | "only if the rule says" |
| Setup · Rules and Windows | "(S5 c4, c11)", "(S5 c5)", "(S4)", "(S5 c1)", "(row 7)", "(S5 c9)", "(S5 c2, S0)", "(S7)" | gone |
| A room: disagreement, the Room state sheet | "(S4)" twice; "recorded, not re-derived" | gone |
| My rooms, Found an issue | "It is published with a correlation id;" | gone |
| Room states, Tap grid tooltip | "in_house" (a raw value) | "in house" |

**The guard.** `scripts/developer-content.ts` is one list for every application, extending Workforce's
register-id check (`b001bfac`). It covers:

- register ids, ADRs and §;
- design-section and design-row references;
- chapter references;
- snake_case identifiers;
- "correlation id".

`tests/developer-content.test.ts` reads every screen as it opens, and again after each control is pressed once, plus
the widgets. **Failing first**: at `991b032` it failed on ten screens with every citation above. It is green at
`15e2654`.

**Queued for you, because it is not clear which kind they are**:

1. **"PMS"** on the Board ("PMS ok · last fact 09:11", "PMS silent since…"), the Wall ("! PMS says…"), the Rules
   tab (who leads: Room Care or the PMS / front desk) and a room's disagreement. It is a hotel word for the front-desk
   system, and also the name of a data source.
2. **Other applications' names** on screens: Workforce ("every role in Room Care comes from Workforce", "Posting
   (Workforce)", "on shift — not announced by Workforce yet" on a widget), Jobs ("Raise a job… — Jobs decides who
   may", "a Jobs job"), Core Administration ("Core Administration › Applications"), HosPilot (Windows tab), and
   **Master Data** ("room types are Master Data's", "from Master Data's location tree"). Master Data is not an
   application you open, which makes it the likeliest to go.
3. **Two columns with no data behind them.** *Posting* on Property-wide access, which now shows a dash. *Typical
   length* on Deep clean plan, which always shows a dash. Keep them, or remove them until they have data?
4. **The Photo button's reason**, "the media service's to add". It names a service. Plain words are needed, for
   example "not available yet".
6. **Status words in capitals.** The approved frames draw IN PROGRESS, DONE, READY, DUE, DIRTY, CLEAN and
   DISAGREEMENT in capitals, and the build follows. Read on a live screen, they may look like a backend's codes rather
   than words. Keep the capitals as a style, or use sentence case ("in progress", "dirty")? (Found 2026-09-19 while
   checking HH's point that screens print backend values as sent.)
5. **"Recorded as source manual…"** on the Room state sheet, and **"whether they are on shift is Workforce's to
   add"** on Reassign. Both are explanations of how the system works rather than something a person acts on.

## Owed after 0.1.4

**The property's day comes from Context, not from Room Care (ADR 0211).** Room Care works out the day itself today,
from the property's time zone and day boundary. The rule is unchanged and so is what you see; the platform now
answers the question instead, so every application gives the same answer. It is deliberately still in 0.1.4: nothing
is removed until that call is proved working on a live platform. Owed in 0.1.5.

## Also for your next page (from the drawing comparison)

Three drawn-versus-built choices remain undecided:

- a blocked room on the Wall is dimmed two different ways (*off the day*, *out of order*);
- the Suite row on Services is drawn selected, at 12px.

These are design choices, not defects.

## Before you test: which version to install

- **0.1.3** is on the shelf now. It **does not have** today's fixes:
  - the four dead controls;
  - the developer notes;
  - the chip spacing;
  - the empty display name;
  - the "could not be checked" sentence.
- **A new cut (0.1.4)** can be built from a clean copy of both repositories, carrying all of them. Nothing is
  installed yet, so installing the older build first would show you defects that are already fixed.

Part B then walks every row above on your platform and records what happened.

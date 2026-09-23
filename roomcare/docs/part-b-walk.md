# Room Care Part B — the walk, press by press

**For the hands at the window.** Part B is the owner's press: the desktop is a native WebView2 window,
every platform call goes over Tauri IPC rather than HTTP, and `apps/desktop/vite.config.ts:32` says the
shell is never served to a browser. Nothing in this file can be driven from here. It is written so that a
person at the keyboard can walk Room Care end to end without knowing the code, and so that the same rows
could be driven by a harness the day the owner rules that a driven click is a press.

**The controls below are derived, not remembered.** Each screen was mounted and every button, select,
input and dialog enumerated from the product's own drawing (`ui/tests/places.ts` walks the module's own
section list and Setup's own tab list, so a screen added later is walked). What is written as *press
exactly* is the label the product draws, character for character.

**The room numbers are Coral Cove's**, because that is the fixture the frames draw. On the owner's
property the rooms are different and the *shape* is what is being walked: "a room tile", "the first row".
Where a row names a room it means *any* room of that kind, and the walker writes down which one they used.

---

## The four outcomes — keep them distinct

| | | |
|---|---|---|
| **works** | the thing happened and the screen says so | the only one that is a pass |
| **refuses, with its reason** | the platform said no **and the screen drew the reason in a person's words** | **a result, not a failure** — a correct platform reporting a correct no |
| **not built** | the control is drawn off, or the section is absent, and the screen says why | expected in several rows below; each one is named in advance |
| **broken** | it looked live and did nothing, said nothing, or said something a person cannot act on | the finding worth having |

A refusal recorded as *broken* reports a correct platform as defective. That confusion is the single most
likely way this walk goes wrong, so every row below carries an **expected** column that says, before the
press, what a legitimate no would look like.

**Record for every row**: the outcome, and *the exact words drawn* — not a paraphrase. If a card appears,
copy its four facts. If nothing visibly happens, say "nothing visibly happened"; that is data.

---

## Preconditions — checked and quoted before row 1

Do not start the walk until all six are answered. A failure here ends the run as a precondition failure,
not as forty failed rows.

| # | What | How it is checked | If it fails |
|---|---|---|---|
| **P1** | **Software Center lists Room Care 0.1.5, Running** | Software Center → Installed | stop. Nothing below is about 0.1.5 |
| **P2** | The install's own record | the install job's final state and step text; anything in `outstanding` | quote it and stop — `outstanding` is what compensation could not undo |
| **P3** | The seven permissions were each decided | all seven appear approved: `room.clean`, `room.inspect`, `roomcare.read`, `roomcare.assign`, `roomcare.amend`, `roomcare.configure`, `roomcare.plan` | an unaddressed permission stops an install by design; if fewer than seven were asked, say which |
| **P4** | The signed-in person is admin on the property | the Board answers rather than drawing a refusal | stop — a refusal here is a precondition failure |
| **P5** | Master Data holds what Room Care reads | rooms, zones, room types, a Housekeeping department, and the property's **time zone** | record which are missing. Room Care draws nothing without them, and an empty screen then means *no data*, not *broken* |
| **P6** | Nothing of Room Care's was there before | this is a first install on this property, so the day is unprepared and the standard unconfigured | if the screens already hold a day, say so — it changes what pass 1 means |

**P5 is the reason this walk is worth doing at all.** Every previous walk would have been against a
property with zero rooms. Write down the counts actually seen — rooms, zones, room types, the department's
name — because a number here is what makes a later empty list readable.

---

## Pass 0 · the bar, and which sections appear

Open Room Care from the desktop.

| # | Screen | Press exactly | Read back | Expected |
|---|---|---|---|---|
| 0.1 | the bar, top of the module | *nothing — read it* | the person's name · their department · the property | **A1 `me`.** A name the staff record does not hold reads **"no name on record"** — never a stand-in, never a system's name |
| 0.2 | the bar's section tabs | *nothing — list them* | which of **Board · Prepare · Room states · Supervision · Deep clean · Setup** are drawn | see the fork below |

**The fork, and it is expected.** A section appears only if the person holds the capability that opens it
(`ui/application.ts:31-37`): Board ← `roomcare.read`, Prepare ← `roomcare.assign`, Room states and
Supervision ← `roomcare.amend`, Deep clean ← `roomcare.plan`, Setup ← `roomcare.configure`.

**ADR 0193 is ruled and unbuilt**: `roomcare.assign` and `roomcare.amend` are authorized against Room
Care's own object type `room_task`, nothing registers that type, so they are refused for every caller by
construction. **So Prepare, Room states and Supervision are expected to be absent.** If they are absent,
that is **not built**, and it is the platform's gap rather than Room Care's. If they *are* present, that
is a finding worth as much as any defect — say so, and walk them.

If **no** section is drawn, record what the module says instead, word for word, and stop: everything
below is unreachable and that is one finding, not forty.

---

## Pass 1 · every screen, before anything is configured

An empty read is **served**, not failed (ADR 0148). A certificate made of empty lists proves the pipe and
not the logic, so each row says what *empty* should look like as against *broken*.

| # | Drive id | Screen | Press exactly | Read back | Expected |
|---|---|---|---|---|---|
| 1.1 | A2 | **Board** | the section tab `Board` | the map of rooms, and the count in the header | 50 tiles if Master Data holds 50 rooms. **Zero tiles with no sentence is broken**; zero tiles with a sentence saying there are none is *works* |
| 1.2 | A2 | Board | chip `Wall` | rooms grouped under zone headers | a zone header folds its zone when pressed. If there are no zones, a sentence saying so |
| 1.3 | A2 | Board | chip `Map` | back to the map | the chip that is already on is drawn pressed; pressing it changes nothing, rightly |
| 1.4 | A2 | Board | chip `Sold tonight` | the tiles narrow | if nothing is sold, an empty result with a sentence |
| 1.5 | A2 | Board | chip `Attention` | the tiles narrow | as above |
| 1.6 | A2 | Board | the attendant select — choose any name | the tiles narrow to that person's | **the list of names comes from Workforce's postings.** If it holds only "Any attendant", record that: it means nobody is posted to Housekeeping |
| 1.7 | A3 | Board | **any room tile** | the room's page opens | **the room's page is row 4.1.** Come back afterwards |
| 1.8 | A4 | **Prepare** | the section tab `Prepare` | the day's proposal | expected absent — ADR 0193. If present: what does it draw with no day prepared? |
| 1.9 | A6 | **Room states** | the section tab `Room states` | the sheet | expected absent — ADR 0193 |
| 1.10 | A7 | **Supervision** | the section tab `Supervision` | the lanes | expected absent — ADR 0193 |
| 1.11 | A8 | **Deep clean** | the section tab `Deep clean` | the planned deep cleans | none planned: an empty list with a sentence |
| 1.12 | A11 | **Setup** | the section tab `Setup`, then tab `Windows & trigger` | the window's times and the trigger | the property's own settings, or the defaults. **Every time on this screen is the property's own time zone** — if a time looks six hours out, that is a defect and it is Room Care's |
| 1.13 | A12 | Setup | tab `Services & minutes` | the services and their minutes, per room type | the room types are Master Data's. If the chips show room types that do not exist on this property, say so |
| 1.14 | A11 | Setup | tab `Rules` | the rules ladder | |
| 1.15 | A13 | Setup | tab `Assignment & zones` | the zones and their rooms | |
| 1.16 | A14 | Setup | tab `Areas` | the public areas and their routines | |
| 1.17 | A15 | Setup | tab `Deep clean plan` | the plan, per room type | |
| 1.18 | A16 | Setup | tab `Property-wide access` | who leads Room Care at this property | a table of people. Empty is legitimate and should say so |

---

## Pass 2 · Setup — the writes that are not blocked

`roomcare.configure` is authorized on the **property**, not on `room_task`, so these are the writes ADR
0193 does not block. They are the first real test of whether Room Care can write anything at all here.

**The Save control is the standard's own** (`ui/screens/setup/controls.ts:77-78`): until a field changes,
Save is drawn off and reads **`Save — nothing changed`**. That is page 64 §2 C11 — a control drawn off
with its reason, never live and inert. **Confirm it reads exactly that before changing anything**; a live
Save with nothing to save is a defect.

| # | Drive id | Screen | Press exactly | Read back | Expected |
|---|---|---|---|---|---|
| 2.1 | — | Setup › Windows & trigger | *read the Save control* | `Save — nothing changed`, drawn off | the reason is drawn. A bare disabled button with no reason is **broken** |
| 2.2 | B1 | Setup › Windows & trigger | change the window's end time, then `Save` | the screen after saving, and the version it says is live | **works** = saved and the new time read back. A refusal must name what was refused in a person's words |
| 2.3 | B1 | Setup › Windows & trigger | change a time again, then `Discard` | the field returns to the saved value | |
| 2.4 | B2 | Setup › Rules | toggle `Assign outside it`, then `Save` | | |
| 2.5 | B2 | Setup › Rules | `Reorder…` → `↓` on the first band → `Back`, then `Save` | the ladder's new order | the first row's `↑` and the last row's `↓` are drawn off, correctly |
| 2.6 | B3 | Setup › Services & minutes | choose a room type chip, open `Departure clean`, change its minutes, then `Save` | | |
| 2.7 | B3 | Setup › Services & minutes | `Copy…` → choose another room type → `Back` | | |
| 2.8 | B4 | Setup › Assignment & zones | `Move rooms between zones…` → tick a room → choose a zone → confirm | the room moves | **if the property has no zones**, this is *no data*, not broken — say which |
| 2.9 | B4 | Setup › Assignment & zones | `Edit…` on a zone → rename → confirm | | |
| 2.10 | B5 | Setup › Areas | `Edit…` on an area → change its times → confirm | | if there are no areas, say so |
| 2.11 | B6 | Setup › Deep clean plan | change a room type's interval, then `Save` | | |
| 2.12 | B7 | Setup › Property-wide access | `Grant to a person…` → choose a person → confirm | the person appears in the table | **the people come from the staff record.** If the picker is empty with 17 staff on the property, that is a finding |
| 2.13 | B7 | Setup › Property-wide access | `Revoke…` on that same person → `Revoke` | they leave the table | do this to the person just granted, not to anyone who was already there |

---

## Pass 3 · Prepare the day — the one assign-gated press that is not blocked

**RC-Q8, ruled (ADR 0193).** The **first** *Prepare the day* on a property, when no `room_task` exists
yet, is authorized with `roomcare.configure` on the property — phase 1 authorizes the creation. Every
later press is object-scoped and belongs in pass 6.

| # | Drive id | Screen | Press exactly | Read back | Expected |
|---|---|---|---|---|---|
| 3.1 | B8 | Prepare | the section tab `Prepare` | | **if Prepare is absent, this row is *not built* and pass 3 ends here.** Say so; it is ADR 0193 again |
| 3.2 | B8 | Prepare | `Prepare the day` | how many rooms and tasks it says it made | **this is the row that decides whether Part B has a day in it.** A refusal here names its cause, and passes 4–7 then walk an unprepared property |
| 3.3 | A5 | Prepare | `Move rooms…` | the attendants and their loads | the names are Workforce's postings |
| 3.4 | — | Prepare | `Back` out of that sheet | nothing changed | |
| 3.5 | — | Prepare | `Show what changed` | what the press did | |
| 3.6 | — | Prepare | *read the prepare control again* | after a successful press it becomes `Add the new rooms (N)` if rooms have changed since, and otherwise reads **`Prepare the day — done <when>`**, drawn off | `ui/screens/prepare/index.ts:79-84`. A control that still reads `Prepare the day`, live, after the day was prepared is a defect and it is Room Care's |

---

## Pass 4 · a room, and the screens with a day in them

If 3.2 worked, repeat rows 1.1–1.6 and write down what is *different*. A board that looked empty and now
holds a day is the proof the reads and the writes are the same system.

| # | Drive id | Screen | Press exactly | Read back | Expected |
|---|---|---|---|---|---|
| 4.1 | A3 | Board → a room tile | any tile | the room's number, type and zone; its condition as a **word** — `Clean`, `Dirty`, `Inspected` — and who set it, and when | **a shouted code (`CLEAN`, `DIRTY`) is a defect** and it is Room Care's; ADR 0229 removed them. The time is the property's |
| 4.2 | A3 | the room's page | tab `History · 14 days` | the room's last fortnight | |
| 4.3 | A3 | the room's page | tab `Record` | what the room's day holds | a timeline. **No event name, no register id, no system name** should appear — developer content is forbidden on a live screen |
| 4.4 | D3 | the room's page | `Room state…` → choose `Occupied` → confirm | | **expected to refuse** — `saveStates` is object-scoped. Record the cause exactly as drawn: *not admitted*, *not granted*, or *the model cannot decide*. Do not predict which |
| 4.5 | D2 | the room's page | `Record an exception…` → choose `DND board` → type a note → `Record` | | expected to refuse, as above |
| 4.6 | D1 | the room's page | `Reassign…` → choose a person → `Reassign` | | expected to refuse, as above |
| 4.7 | — | the room's page | the failure card on any of 4.4–4.6 | **the four facts, and the line under them** | "Asked for" must say **what could not be read in the screen's own words** — *the board*, *this room's day* — **never a permission's code name**. `Copy these details` must be offered, and what it copies must carry the code name. Either half missing is a defect and it is Room Care's |
| 4.8 | D3 | the room's page | `Keep ours` / `Take theirs`, if a disagreement is drawn | | expected to refuse |

---

## Pass 5 · Deep clean — `roomcare.plan`, property scope

| # | Drive id | Screen | Press exactly | Read back | Expected |
|---|---|---|---|---|---|
| 5.1 | C1 | Deep clean | `plan a window…` → fill both dates → confirm | the deep clean appears in the list | property-scope, so **not** blocked by ADR 0193 |
| 5.2 | A8 | Deep clean | the row that just appeared | what it says it will do | |
| 5.3 | C2 | Deep clean | `Cancel this deep clean…` → `Cancel the deep clean` | it leaves the list | cancel the one just planned, not one that was already there |

---

## Pass 6 · the blocked writes — driven once each, for the refusal

Each is driven **once** to capture the refusal as evidence, and read back as unchanged. **None is scored.**
Under contract v2 the screen names the cause; record the cause *drawn*, without predicting it.

| # | Drive id | Screen | Press exactly | Expected |
|---|---|---|---|---|
| 6.1 | D1 | Prepare | the prepare control a **second** time — by then it reads `Add the new rooms (N)` | refuses — object-scoped from here on. If it reads `Prepare the day — done <when>` and is drawn off, there is nothing to press: that is *not built* for this row, and say which of the two was drawn |
| 6.2 | D1 | Prepare | `Accept the proposal` | refuses |
| 6.3 | D1 | Prepare | `Move rooms…` → set a person on a row → `Assign` | refuses |
| 6.4 | D2 | Room states | any row → change a condition → `Save` | refuses |
| 6.5 | D3 | Supervision | a row → `Clean it` | refuses |
| 6.6 | D3 | Supervision | a row → `DND approved — no cleaning` | refuses |
| 6.7 | D3 | Supervision | a row → `Other…` → a note → `Decide` | refuses |
| 6.8 | D3 | Supervision | a disagreement row → `Keep ours` / `Take theirs` | refuses |

**If the section is absent, the row is *not built* and needs no press.** Write that once per section
rather than eight times.

---

## Pass 7 · the attendant — `room.clean`, riding an assignment

**E is expected to be unreachable**, and not because of a check of its own: an attendant's act rides an
assignment, and every way to make an assignment is in pass 6. This pass exists to record *how* it is
unreachable — which is different from it failing.

| # | Drive id | Screen | Press exactly | Expected |
|---|---|---|---|---|
| 7.1 | A9 | sign in as, or switch to, a person posted to Housekeeping who is **not** a supervisor | | the bar draws **My rooms** and nothing else (`ui/application.ts:52`) |
| 7.2 | A9 | My rooms | *read it* | the rooms assigned to them today. **With nothing assigned, an empty list with a sentence** |
| 7.3 | A10 | My rooms | a room's row button | the door opens | |
| 7.4 | E | the door | `Pause` | |
| 7.5 | E | the door | `Ask for extra time…` → a number → `Ask` | |
| 7.6 | E | the door | `Found an issue…` → a note → confirm | |
| 7.7 | E | the door | `End…` → `Done` → `Confirm` | |
| 7.8 | — | the door | the `Photo` control | drawn off, reading **`Photo — not available yet`**. A service's name here is a defect and it is Room Care's |

If 7.1 cannot be done — nobody is posted, or there is no second person to sign in as — say so and stop
pass 7. It is *not measured*, not *broken*.

---

## Pass 8 · the five widgets

On the desktop's own home, not inside the module: **Rooms Ready · Arrivals Waiting · Attention ·
Attendants Now · Pending**. Each answers one question.

| # | Drive id | Widget | Read back | Expected |
|---|---|---|---|---|
| 8.1–8.5 | A17–A21 | each of the five | the number or the sentence it draws | a widget that cannot read draws a card: the same rule as 4.7 — **no code name**, and the copy line carries it. A widget drawing nothing at all is **broken** |

---

## The sweep — two rules that apply to every screen at once

Walk these once at the end, across everything opened above.

| | What | Why it is a defect and not a preference |
|---|---|---|
| **S1** | **Nothing looks live and does nothing.** Every control either did something observable, or is drawn off **with its reason beside it** | a live control that does nothing teaches a person the product is broken when it is only unbuilt. Page 64 §2 |
| **S2** | **No developer content anywhere.** No register id, design-section number, ADR number, event name, relation id, service name or data-source name on any live screen | the owner's ruling of 2026-09-19. A failure state keeps its own plain-words reason; it does not get an exemption |

---

## What comes back

For each row: **the id**, **the outcome** (one of the four), and **the words drawn**. Then three lines at
the end:

1. what the property held when the walk started — rooms, zones, room types, the department, the time zone
2. which sections were drawn in the bar, and which were absent
3. whether *Prepare the day* (3.2) worked, because every later pass reads differently depending on it

**Part A was drawing fidelity. It is not sign-off and never was.** Nothing in Room Care is done until
this walk has been made on the owner's installed platform — every screen opened, every action pressed,
each one marked works, refuses with its reason, not built, or broken.

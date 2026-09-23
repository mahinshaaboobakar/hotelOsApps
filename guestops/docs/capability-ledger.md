# GuestOps — what each button does, for the owner before testing

**Derived from the code, not from memory and not from a walk** — first at
HotelOsApps `b5a46a0` (2026-09-19), and the New booking rows re-derived at
`9130b94` (2026-09-23). **Each section is true at the commit named in it**; a
single "derived at" line over a document that is edited for a week is a claim
about work nobody can check.

Nobody has pressed these on the owner's platform: the live walk fills in the
last column. The Part A pages the owner approved compare drawings with a
harness rendering of fixtures. **They are not sign-off, and they never showed
whether a button does anything.**

**Nothing is installed on the owner's platform as of 2026-09-22** — the owner
uninstalled the product, all four services removed. This line read *"Installed:
0.3.2"* until then. Every row below saying *fails on the owner's platform* is
about a machine that no longer has one, and none of it can be walked until the
product is installed again.

## Why screens still say "service fault"

**Measured from the live logs, not a hypothesis.** GuestOps 0.3.2 now reaches
the Context Service; the address fix works. **Context then refuses it:**

```text
context.log  11:14:55Z  GetOperatingDay  AuthenticationFailedException
             "`guestops` presented a certificate but no access token; sign in first"
             → Unauthenticated
             11:15:23Z  the same
             11:15:33Z  the same
```

Context lets a caller with no access token through **only if it is a platform
service** (`CompositeCallerAuthenticator.cs:53-64` in the SDK). GuestOps is an
**installed application**, so no installed application can call Context as the
SDK is built. GuestOps is the first to try. **Not a gap between rulings** — this
ledger first said it was: `AUTHZ-Q18` already rules that packages are services
and that .NET services accept an application certificate. The shared SDK
authenticator did not, so it was an SDK defect, owned by BB (architect,
2026-09-19). Nothing in GuestOps changes for it.

**Fixed in the platform: ADR 0210, landed at HosPilotOS `7c00adf9`** —
`GetOperatingDay` is platform-scoped and an application's certificate is
accepted. **Live after the owner's next restart**, and not claimed before it is
proved. Everything below that says *fails on the owner's platform (Context)*
holds until then. Creating a booking asks Context for the business day too, so
it is in the same position.

**An HTTP 200 is not the proof, and this ledger said it was.** In GuestOps' own
captured log the three pre-fix calls of 2026-09-19 each read
`Received HTTP response headers … - 200` and then
`Call failed with gRPC error status. Status code: 'Unauthenticated', Message:
'authentication failed'` — gRPC carries its status in the trailers, so the
refusal *is* a 200 at the HTTP layer (`current.jsonl:91638-91642`,
`:91560-91564`, `:91454-91462`, each ending
`POST /module/reservation.read/today - 500`). What is being looked for is the
**absence** of that `Call failed` line after a `GetOperatingDay` that follows
the restart, with Today rendering rather than answering 500.

**This proof now gates two other streams — ADR 0211.** Room Care may not retire
`OperatingDay.cs` and Workforce may not drop its UTC calculation until an
installed application is seen calling `GetOperatingDay` successfully, because
ADR 0211's premise is that ADR 0210 made that reachable. GuestOps is the first
application to call it, so this row is the estate's evidence and not only this
application's.

**And on 2026-09-22 it became unobtainable, which is a status change rather
than a failure.** The owner uninstalled the product: all four HotelOS services
removed, every installed-product port closed, and the Kernel now running is a
development one. **There is no installed application to observe**, so the proof
this row calls for cannot be made until the product is installed again — and
until it is, ADR 0211's gate stays shut for a reason that has nothing to do
with whether ADR 0210 works.

The shape of the proof is unchanged and is recorded above: the **absence** of
`Call failed with gRPC error status` after a post-restart `GetOperatingDay`,
with Today finishing 200 rather than 500. Whoever reinstalls should expect to
be asked for one screen-open.

What depends on that call (code):

| Where the owner sees it | Why |
|---|---|
| **Today** screen — "service fault" | `TodayView` asks Context for the operating day; the refusal is not caught (`ContextBusinessDay.cs`) |
| **Today at the Desk** and **Watchlist** widgets — the same failure | both read the operating day |
| Stay → **Requests** and **Servicing** tabs | *not* a fault: `ContextNeighbours.cs:69` treats a refusal as "not known", so Room Care and Jobs read as unknown |

(The `Unavailable` at 11:23Z in `context.log` is a separate OpenFGA outage,
recovered by 11:24:04Z.)

## The ledger

Verdicts: **WORKS** (wired to a backend operation that exists) · **REFUSES,
SAYS WHY** · **DRAWN OFF, SAYS WHY** · **LOOKS LIVE, DOES NOTHING** (a defect,
fixed first) · **NOT REACHABLE** (only a fixture ever draws it).

### Top bar and every list

| Action | Code | Verdict |
|---|---|---|
| Today · Bookings · Guests · Attention · Setup tabs | `chrome/bar.ts:85` | WORKS — changes screen |
| Pager: arrows and page numbers | `chrome/pager.ts:127` | WORKS — re-reads that page |
| Retry / Open on a failure | `chrome/marks.ts:152-154` | WORKS |

### Today

| Action | Code | Verdict |
|---|---|---|
| The screen itself | `TodayView` | **fails on the owner's platform — the Context refusal above**; live after the next restart (ADR 0210), **to be proved, and the proof gates Room Care and Workforce under ADR 0211** |
| Click a row / guest name | `screens/today/table.ts:75,108` | WORKS — opens the stay |
| **＋ assign** on a row with no room | `screens/today/table.ts:92` | **LOOKS LIVE, DOES NOTHING.** Nothing assigns a room from this screen (`stay.assign` has no module door) |
| Walk-in | `screens/today/index.ts:92` → `screens/walkin/` | **BUILT, UNPRESSABLE.** `C2`, 2026-09-23 — the sheet captures and calls `stay.create/walkIn`. See *The walk-in* below |
| ＋ New booking | `screens/today/index.ts:93` | WORKS — opens New booking, which **takes a booking** since 2026-09-23 (`9130b94`) |

### Bookings and a booking

| Action | Code | Verdict |
|---|---|---|
| Click a row / guest name | `screens/bookings/table.ts:65,102` | WORKS — opens the booking |
| Walk-in · ＋ New booking | `screens/bookings/filters.ts:61-62` | as on Today — the walk-in now opens the built sheet |
| **Cancel…** on a booking | `screens/booking/index.ts:97` → `cancelPlan`, then `cancel` (`stay.override`) | WORKS in code. **Not yet pressed on the platform:** the owner's store holds no booking to cancel (Part B drive list, C1) |

### New booking

| Action | Code | Verdict |
|---|---|---|
| Dates and party, then **Check availability** | `screens/newbooking/query.ts` | WORKS — the read asked with a person's own dates. It asked with NONE until 2026-09-23, so the service refused and the screen drew the refusal |
| The room type list | `availability` read | WORKS |
| **Choose** on a type | `screens/newbooking/availability.ts` | WORKS — the table had no control at all until 2026-09-23; a type with nothing free offers none, because the count says why |
| Guest name, phone, email → **Review booking** | `screens/newbooking/guest.ts` | WORKS. A second guest in the room is drawn off with its reason — one guest reaches the service |
| The capacity warning at the confirm | `screens/newbooking/confirm.ts` | WORKS — ADR 0223's treatment C, ruled by the owner 2026-09-23: warned once, at the moment of commitment, and never prevented |
| **Create booking** | `stay.create` · `book` → `BookCommand` | **WORKS — the first write this application can perform from a screen.** The booking opens by the id the service answers with |
| Walk-in | `screens/newbooking/index.ts` | REFUSES, SAYS WHY — `RC-Q8b-3` |
| Pick another · Assign anyway | `screens/newbooking/conflict.ts:40-41` | NOT REACHABLE — nothing calls `conflict()` |

### Attention

| Action | Code | Verdict |
|---|---|---|
| **Keep ours · Take the PMS value** on a disagreement | `screens/attention/index.ts:116` → `chrome/panel.ts:107`; labels sent by `AttentionView.cs:111` | **LOOKS LIVE, DOES NOTHING.** Reconciliation exists in the service, and no module door reaches it |

### A stay

| Action | Code | Verdict |
|---|---|---|
| Overview · Activity · Requests · Servicing · Payment tabs | `chrome/panel.ts:82` | WORKS |
| **Check in** (a booked stay) | `screens/stay/index.ts` → the registration card → `stay.override` · `checkIn` → `CheckInCommand` | **BUILT, UNPRESSABLE.** `C5`, 2026-09-23. Frame 15 is what a check-in *is*: the card opens, the guest signs, and the card's own button records the arrival |
| **Move room** (in house) | `screens/stay/index.ts` → `screens/assign/` → `stay.assign` · `assign` → `AssignCommand` | **BUILT, UNPRESSABLE.** `C3`, 2026-09-23. See *Assigning a room* below |
| **Check out** (in house) | `screens/stay/index.ts` → `stay.override` · `checkOut` → `CheckOutCommand` | **BUILT, UNPRESSABLE.** `C4`, 2026-09-24. Direct, with no dialog: frame 3 gives `Cancel…` an ellipsis and `Check out` none |
| **Cancel…** (a booked stay) | `screens/stay/index.ts` → the booking, with frame 8's dialog open | **BUILT, UNPRESSABLE.** `C4`. Cancelling is the BOOKING's operation (GUEST-Q2), so this navigates rather than drawing a second cancellation |
| **Keep … · Take …** on the disagreement banner | `screens/stay/banner.ts:49`; `StayDetailView.cs:183` | **LOOKS LIVE, DOES NOTHING** |
| **Full activity →** | `screens/stay/index.ts:304` | **LOOKS LIVE, DOES NOTHING** — should switch to the Activity tab |
| Activity: **Everything · Ours** filters | `screens/stay/activity-tab.ts:37`; sent by `ActivityView` | **LOOKS LIVE, DOES NOTHING** — no filter is applied |
| Activity: Export | `screens/stay/index.ts:266` | DRAWN OFF, SAYS WHY (needs the shell's file-save) |
| **＋ Raise a job** (Jobs installed) | `screens/stay/index.ts:274` | **LOOKS LIVE, DOES NOTHING** |
| ＋ Raise a job (Jobs not installed) | `screens/stay/index.ts:273` | drawn off, **says no reason** |
| **＋ Log a request** | `requests-tab.ts` → `request.handle` · `log` → `RequestCommand` | **BUILT, UNPRESSABLE.** The door exists and the tab calls it (`C7`, 2026-09-23). It cannot be pressed on a property: ADR 0193's registration is in flight, so an object-scoped write authorizes nothing — and nothing is installed to press it on |
| **Log and raise a job** | the same door, `handOff: true` | **BUILT, UNPRESSABLE** — and offered only where Jobs is installed. Gold frame 5b: the request is recorded either way; what disappears is the raising |
| **Ask for service** | `screens/stay/index.ts:278` | **LOOKS LIVE, DOES NOTHING** |
| **Open in the PMS** | `screens/stay/index.ts:291` | **LOOKS LIVE, DOES NOTHING** |
| Servicing night links; "＋ add" / "reveal" | `screens/stay/servicing-tab.ts:90`, `chrome/marks.ts:79` | NOT REACHABLE — `ServicingView` sends no nights, and no view sends a link |

### The registration card — frame 15

Opened from a stay's **Check in**, which is the only route any approved frame
draws to it. Built `C5`, 2026-09-23.

| Action | Code | Verdict |
|---|---|---|
| Every box | `screens/registration/index.ts` → `registration.capture` · `card` → `RegistrationView` | **BUILT, UNPRESSABLE.** The boxes capture; `chrome/field.ts` grew the control its own header said would land with the write path |
| **Save and check in** | the same screen → `capture`, then `stay.override` · `checkIn` | **BUILT, UNPRESSABLE.** Two calls, so each has its own failure: a check-in that is refused says *the card was saved* rather than implying nothing ran |
| **Save** (a guest already in house) | the same door, without the second call | **BUILT, UNPRESSABLE.** Nothing claims an arrival that happened hours ago |
| Documents · Signature | drawn, `kind: "held"` | DRAWN OFF, SAYS WHY — a scan needs the platform's media service and a signature needs a pad. They still travel on the save, because the write is whole-card |

**Three deliberate divergences from frame 15, for the owner to rule on.**

| The frame | What is built | Why |
|---|---|---|
| Permanent address, one tall box | five boxes — line, city, state, country, postal code | The record holds them separately and a filing is made on the parts; one box capturing all five would write a street into a column named `city` |
| `Nationality · United Arab Emirates ▾` | a typed two-letter code | No list of countries exists in this application or on the platform, and writing one here would put a product's opinion of the world's countries into a hotel application. Reported as a gap rather than invented |
| The long note under the block | removed | It ended *"from the same product with no country written into it"* — a sentence addressed to whoever reviewed the frame. The `because` line tells the person at the desk why the block is there (owner ruling, 2026-09-19) |

**The card carries no required-field rule, and that is the caption enforced.**
*The fields are the design's proposal; which of them are required is the
property's setting* — so `RegistrationFields` holds the list and no
required-ness at all, and `CardBox` has no field one could be written into.
`GuestOpsSettings.RequiredForHomeCountry` / `RequiredForVisitors` answered this
before the screen existed: **nothing was stubbed and C6 did not have to come
first.** C6 is the screen that *writes* those two lists; C5 only reads them.

### The walk-in — frame 10

Built `C2`, 2026-09-23. One press, two authorized phases.

| Action | Code | Verdict |
|---|---|---|
| Every box | `screens/walkin/index.ts` → `reservation.read` · `today` · `availability` · `rooms` | **BUILT, UNPRESSABLE** |
| **Create and check in** | → `stay.create` · `walkIn` → `WalkInCommand` | **BUILT, UNPRESSABLE.** Off, and saying what is missing, until the service's required fields are present |
| Rate | drawn, not captured | DRAWN OFF, SAYS WHY — `WalkInCommand` has no field for it, so the sheet cannot send one. The money sites remain OWED behind the currency authority |
| Registration | drawn, not captured | DRAWN OFF, SAYS WHY — it is the stay's card (`C5`), after this |

**What actually blocked it was not the write.** `stay.create/walkIn` was
declared, served and unreachable — and the reason was that **nothing in this
application could name a ROOM**, while check-in requires one (S8). `roomId` has
been required since `WalkInCommand` was written. The new read is
`reservation.read/rooms`, over `AvailabilityService.FreeRoomsAsync`, so that
*what is free* is decided in one place at both grains.

> **A fidelity sweep could never have found this.** It compares nodes that
> exist; a control nobody could populate has no node to differ from. It is found
> by asking what the WRITE needs against what the reads actually send.

**The partial outcome is the behaviour most worth holding.** A refused second
phase leaves the stay (RC-Q8a), so `checkedIn: false` is a **successful call
reporting what happened**, not a failure. The sheet says *the stay was created
and the guest is not yet in house*, names why, and does not close — a desk told
"nothing happened" creates a second stay for a guest already in the book.

**The arrival comes from the service's business date, never this machine's
clock.** A browser in another zone disagrees with the desk about what *today*
is, and the stay is the property's.

**Found while building it — a defect in shared chrome.** `Action.off` added a
CSS class and nothing else: the button stayed pressable, and an action with no
`onClick` falls back to `onDismiss`, so an **off primary silently closed the
overlay**, throwing away whatever had been typed. The one existing caller
defended itself by hand. `off` is now a second *shape* that carries its reason
and cannot carry a handler, rendered through `unavailable` — disabled, with the
reason where a pointer and a screen reader both find it. Held by three tests in
`tests/overlay.test.ts`, shown failing against the old behaviour.

**One Part A divergence, and it is structural rather than a choice.** Frame 10
draws the sheet as a desk has filled it — Joseph Mathew, Deluxe Twin, Room 308.
A capture screen starts **empty**, because it is a new stay; the frame's state
is reached by typing. A capture of the built sheet will therefore differ from
the frame on every field, and that is the drawing showing a moment rather than
the build being wrong.

### Assigning a room — frame 3

Built `C3`, 2026-09-23. One sheet for both affordances: frame 3's **Move room**,
and the day list's `＋ assign` for a stay that has no room.

| Action | Code | Verdict |
|---|---|---|
| Room chooser | `screens/assign/index.ts` → `reservation.read` · `stay` · `rooms` | **BUILT, UNPRESSABLE** |
| **Assign · Move** | → `stay.assign` · `assign` → `AssignCommand` | **BUILT, UNPRESSABLE.** Off until a room is chosen, saying so |
| **Assign anyway** | the same call, with `acceptConflict` | **BUILT, UNPRESSABLE.** Only reachable after the service has reported a conflict |

**The conflict warns and never forbids** — GUEST-Q5 made a double-booked room a
possible truth, so a hard block would put a ruled outcome out of reach. The
service refuses the first attempt; the command turns that into part of the
**answer** rather than an error, and the sheet keeps the room chosen, says what
holds it, and changes the button to *Assign anyway*. `acceptConflict` is never
sent on a first attempt: the desk has to have been shown something before it
can mean it.

**A conflict and a refusal are two states, and they were one until a test said
so.** Both arrived as a sentence, so a concurrency refusal also turned the
button into *Assign anyway* — offering to force something that has nothing to
do with conflicts, against a version that is already stale. They are now a
discriminated `Told`, and only a conflict is retryable with agreement.

**The reason is derived and the request has nowhere to put one.** A stay with
no room is being given its first (`Initial`); one that has a room is being moved
(`Move`). A client able to send a reason could record a move as a first
assignment, and the assignment history is what a property gets asked about.

**`StayDetailView` now carries the version it was read at.** It did not, which
is why `C5`'s check-in borrowed one from the registration card: a screen whose
actions write and whose read has no version cannot make the concurrency check
mean anything. Check-out and cancel will need the same field.

**`reservation.read/rooms` answers two questions.** The walk-in has dates and a
type and no stay; the assignment has a stay and neither. Sending the stay's own
type and nights back from a client would put facts the service holds in a
caller's hands to get wrong, so a stay answers for itself.

### The two the service can do and no frame draws — `C4`, 2026-09-24

`StayLifecycleService` implements five lifecycle writes. Four are now reachable
from a screen. **Two of the five have no affordance in any approved frame**, and
the surface deliberately maps neither.

| | |
|---|---|
| `RecordNoShowAsync` | **No control.** "No-show" appears **once** in the gold — as a *state* on a bookings row (`BK-4361 · No-show · Opera`), never as an action |
| `CorrectAsync` | **No control.** "Correct" appears as **no affordance at all**; the word occurs twice in the gold, both times in prose about something else |

Measured across the whole gold rather than assumed from frame 3.

**Mapping them would be the declared-and-never-used defect `CORE-Q13` is named
after, and drawing the buttons would be richer than the approved design.** So
`ModuleSurface` maps `checkIn`, `checkOut`, `cancel` and `assign`, and says in
the code why the other two are absent.

**Both are real capabilities a desk will need.** A no-show is chargeable and
reportable and must stay distinguishable from a cancellation; a correction
exists because *"the guest checked out in error at 07:00 and still asleep in
the room is a real morning"*, and it is the only remedy for a departure
recorded by mistake — which matters more now that `Check out` is direct. **The
affordance is a design question for the owner, drawn.**

**And there is no override control, in any of them.** A staff write on a stay
the PMS owns is applied *and recorded as an override* by the service, with who,
when, and what the PMS said at that moment (GUEST-Q1's amendment). An override
is a state that can contradict a PMS, never a button somebody presses to set
one — a screen offering to "override" would be offering a second answer where
the platform keeps exactly one (GUEST-Q3).

### Setup

| Action | Code | Verdict |
|---|---|---|
| Section tabs | `screens/setup/index.ts:90` | WORKS |
| Save · Discard | `screens/setup/index.ts:99-100` | DRAWN OFF, SAYS WHY |
| Card actions ("Close a room type…", "Record a filing") | `screens/setup/card.ts:37` | NOT REACHABLE — only the fixture draws them |

### The five widgets

| Action | Code | Verdict |
|---|---|---|
| Any row | `widgets/card.ts:116` | WORKS — opens GuestOps at that place |
| Retry / Open on a failed card | `widgets/card.ts:259` | WORKS |
| Today at the Desk | `widgets/entry/today.ts` | **fails on the owner's platform (Context)** — and a second defect found today: it reads `dueIn/arrived/dueOut/departed/arrivals`, which `TodayView` does not send; only the harness fixture has them |
| Watchlist | `widgets/entry/watchlist.ts` | **fails on the owner's platform (Context)** |
| Occupancy · From the PMS · Business Mix | | reads that do not need Context |

## Count

**11 controls look live and do nothing**, on Today, Attention and a stay:
＋ assign (1) · Keep ours / Take the PMS value (2) · the banner's Keep / Take
(2) · Full activity (1) · Everything / Ours (2) · Raise a job (1) · Ask for
service (1) · Open in the PMS (1). They come first: each is drawn off with its
reason until its backend door exists.

**The number has moved four times and the arithmetic is written out rather than
retyped**: 16 on 2026-09-19, less *Log a request* (`C7`), *Check in* (`C5`),
*Move room* (`C3`), and *Cancel* with *Check out* (`C4`) — 16 − 5 = 11, which
is also what the list above sums to. A count in prose is ambiguous across sets
and goes stale silently, so the subtraction is shown and the two routes to it
are made to agree.

***And it was stale by one before this edit.*** `C3` moved *Move room* out of
the list and left the number at 14 — the count and the table disagreeing for a
day, in the file whose whole job is to say what is true. The two routes are
checked against each other now because only that catches it.

*"in 10 places" is dropped rather than carried down.* It was written on
2026-09-19 and this edit would have made it load-bearing; the enumeration above
groups into nine, and rather than assert either figure the places are left to
the list, which is the thing anybody would count.

**Six writes are wired from a screen**, all of them on 2026-09-23:
cancelling a booking · **creating one** — New booking runs dates → types →
guest → confirm → the booking, and takes it (`9130b94`) · logging a request ·
logging one and raising a job from it (`C7`, `cb9d67f`) · capturing the
registration card · and recording the arrival from that card (`C5`). **Not one
of them has been pressed on a property**: there is no installed product to press
them on, and ADR 0193's object registration is in flight, so an object-scoped
write authorizes nothing even where there is. Every row above says which of
those two it is waiting on.

**What still separates this from sign-off is the writes, not the reads.** Every
read is drivable; the blocked writes divide into those with no module door — the
ADR 0193 group — and those whose door exists and whose screen does not. This
count is the first, and it is the list above.

## Since this ledger was written (2026-09-19, same day)

- **The 16 are drawn off with their reasons** (`991b032`), and a control that
  does nothing can no longer be written: `control()` requires its action, and
  a control GuestOps cannot perform is `unavailable(label, reason)`. "Full
  activity →" now opens the Activity tab.
- **Today at the Desk reads what it draws** (`d9addf4`) — its own read, `desk`.
- **New booking as the owner decided** (`9a8f6ef`): every room type, no pager,
  no developer panels. It still refuses on a property until the booking flow the
  owner asked for is drawn, approved and built — it cannot take a person's own
  dates yet.

## A vendor's name in the review fixtures — swept 2026-09-20

BB found one sentence while measuring something else
(`ui/book/recorded/booking.ts:173-175`, *"Opera will not be told… Opera will
keep showing this booking as live"*). A fixture is not a product path, and it is
rendered on a screen at every review and capture, so it is fixed.

**Swept as a class, not as the line reported.** `\bOpera\b` across
`ui/**/*.ts`: **42 rendered strings in 7 files under `book/recorded/`, 21 in
comments, 1 in a test.** All 42 now read *the PMS* / *PMS*, the wording the
owner approved in `77f67bf`. **No product path held one** — the earlier finding
stands, and this sweep is what checked it rather than repeating it.

Two things the sweep itself taught, both worth the next reader's time:

- **`Operator` contains `Opera`.** A first pass without a word boundary
  reported `application.ts` and `chrome/bar.ts` — two product paths — and they
  were `Operator` and `Operating`. A search string is a hypothesis about how
  somebody typed it.
- **One of 42 survived an exact-string mapping** because the file has an em
  dash where the mapping had a middle dot. The residue count is what found it;
  reading the diff would not have.

**The comments are kept.** They record what a mark used to say and why the
copy moved, and a text guard that forbade the word would forbid keeping that
record.

**"The PMS" is interim, and it is a limitation rather than a style choice.**
*Opera is itself a PMS product*, so the generic word is not a correction of the
vocabulary — it is what is left when a screen must name the system that will not
be told and **the application holds an integration id and no name.** That is
`CONN-Q44`. When it is ruled these sentences take the property's own connected
system's name, and a property running Opera reads *Opera*, because that is what
that property is running. Naming one vendor in the meantime states, on every
property's screen, a fact about one property's estate.

**Evidence for `CONN-Q44`, and this is the sharpest form of it**: 42 sentences
in one application need a name it cannot obtain. The reason is written at the
code too (`ui/book/recorded/booking.ts`, the site BB reported), so a reader
meeting the generic word learns it was a limitation. If the ruling lands before
0.3.3 the fixtures take the display name and this section says so.

## Found while converting the last three views — frame 9's group line

`recordedGroup.summary` was **`Group 84119377 · from the PMS · booked 28 Aug`**
— a confirmation number, a source and a booking date. **`BookingView` has never
sent that sentence.** Its `Summary` produces a count and a span
(`Two stays · 3 Sep → 7 Sep`) and nothing else, so the fixture was a second
contract wearing the same field name, and every capture of frame 9 drew a line
the property could not produce.

It now carries the wire's shape. Drawn for the owner as three options
(`docs/mockups/07-booking-heading-and-dates.html`), and **ruled 2026-09-20**:

> **A2 — the heading carries the confirmation number**, then the count and the
> days: `84119377 · One stay · 31 Aug → 02 Sept`.

Built the same day. The view already read the number, so this was the small
work it was priced as, and **the dates stay** — which is what A3 would have
cost.

**What A3 would have needed, recorded so nobody re-raises it as an oversight**:
the line as drawn (`Group 84119377 · from the PMS · booked 28 Aug`) needs the
date the source booked it, which nothing in this application reads, and the
connected system's own name, which is `CONN-Q44` and unavailable. It also drops
the dates, which is the trade the owner declined.

### And the dates themselves — ruled the same day

I reported the compressed range (`3 – 7 September`) as lost when the sentences
moved to the screen, because its word order is one language's. **That was wrong
and I corrected it**: `Intl`'s `formatRange` produces `3–7 Sept`, `Sep 3 – 7`
and `31 Aug – 2 Sept` correctly per locale, so it was a live option, measured in
three cases and drawn as option B.

> **B long — the composed form, `03 Sept → 07 Sept`, per locale. No shared
> range formatter is to be added.**

**The arrow join is deliberate and says so at the code** (`chrome/when.ts`):
two separately formatted days joined by an arrow assert nothing about either
language, which is exactly what the compressed form could not do. The measured
short form stays in the drawn page as the record of a rejected option.

**The fidelity comparison is re-run at 0.3.3**, not before — the frames and the
build both moved today.

## The failure cards, re-audited against the owner's 2B — 2026-09-20

The shared surface stopped putting a permission's code name on a card
(HosPilotOS `6751c7de`, `packages/sdk-typescript/src/failure.ts`). **Which of
GuestOps' cards moved, measured rather than assumed:**

**All seven, and none by this application's hand** — Today, Bookings, a
booking, New booking, Attention, a stay, Setup. Each builds its card through
`failureDrawing`, and `chrome/marks.ts:172` draws `drawing.facts` as the
surface returns them, so `Asked for` reads *today at this property* where it
read `reservation.read · today`.

**The code name still reaches support**: the copy action writes
`drawing.wire`, unchanged, and `permission` and `method` stay on the failure
for diagnostics.

**No test here asserted the old contract** (ADR 0034, checked rather than
assumed). `tests/failure-surface.test.ts` asserts the mark's tone per cause,
which button appears per cause, and that a refusal routes nobody to a person —
no assertion reads the facts' values. Nothing to correct.

**What did NOT move, reported to the architect and with the owner**: a refusal
card still prints the code name, from the same shared file —
*"This screen needs `reservation.read`, and no grant at this property names
this user"* (`failure.ts:564`, and the two beside it). **And that sentence's
own justification was falsified by the change above it**: `act()`'s doc argues
the refusal need not name who can grant because *"the four facts carry the
capability"* — which stopped being true forty minutes earlier. Either the
sentence keeps the name and the reason is rewritten, or it loses it and the
copy line carries it alone; both cannot stand as they are.

## The exponent, measured against its ruling — 2026-09-20

ADR 0175's exponent ruling says the service converts at the boundary where the
money value is created, taking the exponent from **authoritative currency
metadata — the property's configured currency record**, never process culture;
and that a service with minor units and a currency but no authoritative
exponent **has a contract gap rather than permission to assume 2**. Measured
here, both halves:

**The currency CODE is reachable.** `masterdata.properties.currency` exists
(`services/masterdata-service/src/Domain/Tenancy.cs:77`, default `INR`), and
GuestOps already reads master data through keyless read models over that schema
— `MasterDataRoomTypeName` is the pattern. Context exposes it too, on
`PropertySummary.currency`. Adding a property read model is a small piece of
work and needs no ruling.

**The EXPONENT does not exist anywhere in the platform.** Searched
`services/`, `shared/protos/` and `packages/sdk-dotnet` for an exponent, minor
units, decimal digits or a currency entity: **there is none** — the only hits
are an RSA exponent and exponential backoff. The property's currency record
holds `"INR"` and says nothing about how many minor units that has. **So the
authoritative metadata the ruling names is not there to be read**, and by the
ruling's own test this is the gap, reported rather than worked around.

**And a second half the property record cannot close.** An amount's currency is
not necessarily the property's: `Money.Currency` arrives from the source, so a
PMS can send a booking priced in `USD` to a property configured in `INR`, and
the platform can hold it today. A property's own currency record is
authoritative for amounts in that currency and for nothing else — so even once
it carries an exponent, a foreign-currency amount still has no authority behind
its decimals.

**Where it would happen for GuestOps**: `RoomStayFactMapper.Amount()`, which
builds `Money` from the Hub's `integration/v1.Money` — itself `int64
minor_units` (`dto.proto:92`). The upstream leg carries the same gap, so the
connector converting at *its* boundary needs the same metadata.

## Found while applying NUM-Q2 — the amount's exponent

`PaymentView.Money()` divides minor units by one hundred whatever the currency.
**Not part of the NUM-Q2 ruling** — that settles the wire (a decimal string and
an ISO 4217 code) — and wrong independently of it: the Kuwaiti dinar has three
decimal places and the yen none, so a Kuwaiti folio reads ten times its value.
Recorded as a test with three currencies chosen so the two rules disagree
(`StayTabViewTests.Characterisation_the_rate_assumes_two_decimal_places_for_every_currency`),
green today against what the view actually renders, and written to fail when
the migration lands. It travels with U1/U2's owed work.

## Developer notes built as screen — swept 2026-09-19

The owner's ruling: a mock's notes for the developer are never built as
screen. Swept by rendering every screen and widget (`tests/surfaces.ts`) and
reading all of it. **Guarded**: `tests/document-citations.test.ts` fails on a
design-section reference, an ADR or a register id anywhere a person reads —
shown failing on `GUEST-Q6` in the Payment tab before the cleanup.

**Removed — they reached a property** (UI code or what the service sends):

| Where | What it said |
|---|---|
| Stay · Activity, `activity-tab.ts` | "Three sources, one list… read through the Context Service and stored nowhere here…" |
| Stay · Activity, `ActivityView.cs` | each row's detail was the event's own name (`stay.arrived`) |
| Stay · Requests, `requests-tab.ts` | "GuestOps owns these" · "Jobs · via Context" · "This panel is Jobs' data, not ours…" · "A request is a fact about the guest's stay…" |
| Stay · Servicing, `servicing-tab.ts` | "All of this is Room Care's… Context Service…" · "Why a day can be blank" card · "What the desk can do here" card. The not-readable state keeps its reason, now in plain words |
| Stay · Payment, `payment-tab.ts` + `PaymentView.cs` | "in v1" · "not ruled · nothing built" · "COMPUTED FROM OFFSET" · "NEEDS FINANCE OR A CONNECTOR CAPABILITY" · "FINANCE, A LATER ROUND" · the deadline note · the folio note citing **GUEST-Q6** |
| Booking · cancel, `CancelPlanView.cs` + `cancel.ts` | "A booking is a group and every operation happens to a stay" · "can be reinstated afterwards" (nothing in GuestOps can) · "Charging is Finance's, a later round" |
| New booking | the sources card and the explanation note (the owner's G7 decision) |
| Widgets | Today "Arrivals without a room show the gap rather than a guess." · Occupancy "By floor is not drawn…" · From the PMS "Amended and cancelled are not drawn — see the report." and "— nothing else is recorded" · Business Mix "In the source's own words, never normalised." |

**Removed from the harness's fixtures** (never on a property, but on every
capture the owner reviews): the Activity rows' routes ("arrived via the
Integration Hub", "read through the Context Service"), the stay's event-stream
note, Setup's "reporting.file", "PLATFORM PRINT SURFACE", "SEEDED FOR ITS
COUNTRY", "AN OFFSET, NOT A DATE", "the seller's control — not an inventory
fact", the stop-sell and filing notes, and "…without a country written into it".

**Queued for the owner — unclear which kind it is:**

| Where | What it says | The question |
|---|---|---|
| Today and New booking sub-line (`TodayView`, `AvailabilityView`) | "PMS-connected — Opera writes the lifecycle" / "Standalone — this property is the book" | Screen or note? And it names Opera whatever the property's PMS is |
| Bookings rows (`BookingsView.cs:167`) | an "Opera" chip on every PMS row | The same: hardcoded, wrong on a property whose PMS is not Opera |
| New booking, out of order (`AvailabilityView`) | "EngineeringOps" chip | An application's name on a staff screen, hardcoded |
| From the PMS widget rows | "OHIP" — the integration's id | A system name, or what the desk calls its feed? |
| Stay · Overview tags (fixture) | "OBSERVED" · "DERIVED FROM PROPERTY CLOCK" · "FROM OPERA" · "GUEST · CARRIES TO NEXT STAY" | Provenance for staff, or for the developer? |
| Setup (fixture) | "OR EVERY GUEST" · "DECIDES WHO IS 'FROM OUTSIDE'" · "BY A PERSON, ON THE AUTHORITY'S PORTAL" · "Overdue is shown, never enforced… the platform says what is owed and stops nothing." | Help text for the manager, or notes? |
| Attention | "The names only ordered the list — they can never join two stays." | Explanation for staff, or a note? |

## Found while building the registration card — every wire date read the machine's locale (2026-09-23)

A test sent `14/03/86` to a card's date of birth, expecting it to be refused as
a format this application's contract does not have. **The service stored 14
March 1986.**

`DateOnly.TryParse` and `TimeOnly.TryParse` parse under
`CultureInfo.CurrentCulture`, and **thirteen call sites used them** — every
date and time this application reads off a wire:

```text
Module/       BookCommand · WalkInCommand · RegistrationCommand · ModuleSurface
Grpc/         GuestOpsGrpcService · .Bookings · .Stays
Events/       RoomStayFactMapper — the Hub's business date, a vendor's drop time
Infrastructure/  ContextBusinessDay — the operating day, the roll boundary,
                 the property's check-in hour
```

**It is not leniency; it is a silent reinterpretation.** `03/04/2026` is the
third of April on one server and the fourth of March on another, and nothing
downstream can say which it was: a stay would simply be on the wrong day.

Closed by `Application/Abstractions/Iso.cs` — `TryParseExact`, invariant, in the
one form the wire has — and held by `WireParseGuardTests`, which walks the whole
`src/` tree **by call shape rather than by file**, because a guard naming the
nine files that held the defect stops checking the tenth. Shown failing by
reinstating one culture-dependent call.

**This is ADR 0227's rule pointed at the wire instead of at a spreadsheet:**
decode where the format is known, refuse where it is not, and never default.
The two differ by months rather than by 1462 days, which is why nothing had
noticed.

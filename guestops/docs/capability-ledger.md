# GuestOps — what each button does, for the owner before testing

**Derived from the code at HotelOsApps `b5a46a0` (2026-09-19), not from memory
and not from a walk.** Nobody has pressed these on the owner's platform yet: the
live walk fills in the last column. The Part A pages the owner approved compare
drawings with a harness rendering of fixtures. **They are not sign-off, and they
never showed whether a button does anything.**

Installed on the owner's platform: **0.3.2** (updated 0.3.1 → 0.3.2 at
16:42:02 IST, pid 33228, `kernel.log`).

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
**installed application**, and `AUTHZ-Q18` rules that an application never
carries a person's session. **So on the platform as built, no installed
application can call Context.** GuestOps is the first to try. That is a gap
between two rulings, not something GuestOps can fix alone, and it goes to the
planner as a question.

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
| The screen itself | `TodayView` | **fails on the owner's platform — the Context refusal above** |
| Click a row / guest name | `screens/today/table.ts:75,108` | WORKS — opens the stay |
| **＋ assign** on a row with no room | `screens/today/table.ts:92` | **LOOKS LIVE, DOES NOTHING.** Nothing assigns a room from this screen (`stay.assign` has no module door) |
| Walk-in | `screens/today/index.ts:92` | REFUSES, SAYS WHY — opens a sheet saying a walk-in cannot be taken here yet (`application.ts:258-278`) |
| ＋ New booking | `screens/today/index.ts:93` | WORKS — opens New booking (availability only; there is no "create booking" step) |

### Bookings and a booking

| Action | Code | Verdict |
|---|---|---|
| Click a row / guest name | `screens/bookings/table.ts:65,102` | WORKS — opens the booking |
| Walk-in · ＋ New booking | `screens/bookings/filters.ts:61-62` | as on Today |
| **Cancel…** on a booking | `screens/booking/index.ts:97` → `cancelPlan`, then `cancel` (`stay.override`) | WORKS in code. **Not yet pressed on the platform:** the owner's store holds no booking to cancel (Part B drive list, C1) |

### New booking

| Action | Code | Verdict |
|---|---|---|
| The availability list and pager | `availability` read | WORKS |
| Walk-in | `screens/newbooking/index.ts:82` | REFUSES, SAYS WHY |
| Pick another · Assign anyway | `screens/newbooking/conflict.ts:40-41` | NOT REACHABLE — nothing calls `conflict()` |

### Attention

| Action | Code | Verdict |
|---|---|---|
| **Keep ours · Take the PMS value** on a disagreement | `screens/attention/index.ts:116` → `chrome/panel.ts:107`; labels sent by `AttentionView.cs:111` | **LOOKS LIVE, DOES NOTHING.** Reconciliation exists in the service, and no module door reaches it |

### A stay

| Action | Code | Verdict |
|---|---|---|
| Overview · Activity · Requests · Servicing · Payment tabs | `chrome/panel.ts:82` | WORKS |
| **Check in · Cancel** (a booked stay) · **Check out · Move room** (in house) | `screens/stay/index.ts:288`; labels sent by `StayDetailView.cs:140-156` | **LOOKS LIVE, DOES NOTHING.** No module door for check-in, check-out or move |
| **Keep … · Take …** on the disagreement banner | `screens/stay/banner.ts:49`; `StayDetailView.cs:183` | **LOOKS LIVE, DOES NOTHING** |
| **Full activity →** | `screens/stay/index.ts:304` | **LOOKS LIVE, DOES NOTHING** — should switch to the Activity tab |
| Activity: **Everything · Ours** filters | `screens/stay/activity-tab.ts:37`; sent by `ActivityView` | **LOOKS LIVE, DOES NOTHING** — no filter is applied |
| Activity: Export | `screens/stay/index.ts:266` | DRAWN OFF, SAYS WHY (needs the shell's file-save) |
| **＋ Raise a job** (Jobs installed) | `screens/stay/index.ts:274` | **LOOKS LIVE, DOES NOTHING** |
| ＋ Raise a job (Jobs not installed) | `screens/stay/index.ts:273` | drawn off, **says no reason** |
| **＋ Log a request** | `screens/stay/requests-tab.ts:59` | **LOOKS LIVE, DOES NOTHING** (`request.handle` has no module door) |
| **Ask for service** | `screens/stay/index.ts:278` | **LOOKS LIVE, DOES NOTHING** |
| **Open in Opera** | `screens/stay/index.ts:284` | **LOOKS LIVE, DOES NOTHING** |
| Servicing night links; "＋ add" / "reveal" | `screens/stay/servicing-tab.ts:90`, `chrome/marks.ts:79` | NOT REACHABLE — `ServicingView` sends no nights, and no view sends a link |

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

**16 controls in 10 places look live and do nothing**, on Today, Attention and
a stay: ＋ assign (1) · Keep ours / Take the PMS value (2) · Check in, Cancel,
Check out, Move room (4) · the banner's Keep / Take (2) · Full activity (1) ·
Everything / Ours (2) · Raise a job (1) · Log a request (1) · Ask for service (1)
· Open in Opera (1). They come first: each is drawn off with its reason until its backend door exists.
**The one write that is wired** (cancel a booking) has never been pressed on the
platform.

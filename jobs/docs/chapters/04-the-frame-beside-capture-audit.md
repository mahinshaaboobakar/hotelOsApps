# Chapter 04 · The frame beside the capture

**Stream HH, 2026-09-04.** The standing audit the architect set as the last step
of the gate: *"then the build, then the standing frame-beside-capture audit
against these exact frames."* The precedent for how is `OPS-Q11` — the locked
drawing beside the shipping surface, **both sides photographs in one browser at
one width**.

## How it was run

One Chromium window at 1400 × 1000. Both sides served from one origin
(`127.0.0.1:8731`) so nothing differed but the page: the locked mockups at
`docs/mockups/01-the-jobs-screens.html` and `02-the-jobs-settings.html`, the
built module at `ui/preview/frame.html?…`, driven by the capture harness to the
named screen.

The harness fakes **the host and nothing else** — the identity, the granted
capabilities, the property environment (`Asia/Qatar`, `en-GB`) and the answers
to `host.call`. The module's own code, stylesheet and token references are the
shipped ones, and the tokens injected are the fourteen the shell publishes and
no more.

## What matched

Eleven frames were photographed and read against their drawing:

| Frame | Capture | Verdict |
|---|---|---|
| 1 · The board | `?screen=Board` | matches — strip, six filters, twelve rows, the pager, NT, the linked and blocked tags |
| 2 · One job · Overview | `?open=job` | matches after the fixes below |
| 2b · Work | `?open=job&tab=Work` | matches after the fixes below |
| 2f · Rating | `?open=job&job=rated&tab=Rating` | matches — five stars, the guest's line, how it was asked, 6 min |
| 5 · Live | `?screen=Live` | matches — three departments, presence pills, scrolling cards, the sweep table |
| 6 · Scheduled | `?screen=Scheduled` | matches — seven columns, **no cycle column** |
| 7 · Catalogue | `?screen=Catalogue` | matches |
| 8a · Concern policy | `?screen=Settings` | matches — the scope rail, the clock, manager-at-risk |
| 8b · Policies | `?screen=Settings&view=list` | matches — seven policies nested by scope, both departments |
| 9 · The widget | `?widget=quiet\|escalated\|mine` | matches — three states |
| 10 · Read-only | `?open=job&granted=none` | matches after the fix below |

## What the audit found, and what was done

Five findings. Four were defects in the built module and are fixed; one is a
deliberate difference between the drawing and the product, recorded as such.

### F1 · The timer counted from the machine's clock — **fixed**

Frames 2 and 2b drew `00:23:41`. The capture read **`42:16:53`**, and climbing:
the module computed elapsed time by subtracting the session's start from
`new Date()`. On the capture machine the recorded example is two days old, so
the error was obvious; **on a property it would be silent** — a desktop whose
clock is minutes off would show a figure the hotel never had, on the one number
a supervisor uses to judge a promise.

The service already computes worked time against the clock the property runs
on (`JobWorkSession.WorkedSecondsAt`). It now says so on the wire —
`JobView.running_seconds`, and `WorkSessionView.worked_seconds` live for a
running row — and the module renders what it is handed. `sinceSeconds` is
deleted from the module, and a comment stands where it was so the next screen
does not reinvent it.

### F2 · The work controls appeared for somebody who does not hold the job — **fixed**

The read-only pane (`granted=none`) drew **Pause** and **Stop** for a
supervisor holding only `job.read`.

The reasoning behind that was right and the implementation was not: accept,
start, pause, resume and stop are the assignee's own acts and ride on the
`job#assignee` relation, so no permission gates them (design §4.1). But
**the module cannot tell whether the viewer is the assignee** — `ModuleIdentity`
carries capabilities, not a user id — so it was drawing the controls on the
strength of a session existing at all. Pressing one would have been refused by
the service with `PermissionDeniedException("assignee")`, which is a control
that lies.

`JobView.viewer_is_assignee` is now computed where the caller is established
and the module gates the work controls on it. The capture confirms: no action
row at all for that viewer.

### F3 · Bare times in two lists — **fixed**

The owner's ruling of 2026-09-04 is that every timestamp is a date **and** a
time. The locked frames obey it (`02 Sep 13:52` in the sessions table,
`02 Sep 14:10` in the Live concern table). The module rendered `13:52` and
`14:10` there. Both now use the same formatter as the rest of the module.

The one place a bare clock survives is the board's strip — `ENG · 14:24` — on a
line that already names the day. That is deliberate.

### F4 · The header's story arrived as prose — **fixed**

The one-line story under a job's title ("raised … via … due …") came from the
seam as a finished sentence, so the dates in it bypassed the property's
formatter: a US-locale property would have read `Sep 02` on the board and
`02 Sep` on that line. The module now composes the line from instants.

### F5 · The rated example wore the other job's history — **fixed**

Frame 2f's recorded example spread the AC job's fields, so the towel job showed
`History · 8` and three notes for a job with one session. Recorded data only;
it now has its own five history lines and its own note.

### D1 · The date form is the locale's, not the drawing's — **deliberate**

The frames were written by hand as `02 Sep 13:31`. The product renders
`02 Sept, 13:31`, because `Intl` under `en-GB` abbreviates September as *Sept*
and joins with a comma. Under `en-US` the same instant reads `Sep 02, 01:31 PM`.

This is the ruling working: the form is the property's, derived from `locale`
and `timezone`, and no package writes its own. The drawing's spelling is not
the contract; the rule is.

## What remains, and is not fixed here

**Server-composed prose still carries dates** in the Overview's key/value cards
("Due at · 02 Sep 14:10 · policy P1 = 40 min", "Accepted · 02 Sep 13:47 · 14 min
after assignment") and in the Live tab's "last nudge" column. Those values are
the service's sentences, so their date form does not follow the viewer's
property the way every other timestamp now does. Fixing it means sending the
instants beside the prose and composing in the module, as F4 did for the header
— worth doing, and a change to the read contract rather than a defect in what
was locked. **Carried to the next redline**, not silently changed.

## The state of the two suites

| | |
|---|---|
| Backend | builds with zero warnings; **39 characterisation tests pass** against a scratch PostgreSQL |
| UI | typechecks; **20 tests pass**; both bundles verify self-starting, self-contained, against the fourteen published tokens |

Two new UI tests came out of this audit and stand as its record: the timer
shows the service's figure, and a viewer who does not hold a job gets no work
controls.

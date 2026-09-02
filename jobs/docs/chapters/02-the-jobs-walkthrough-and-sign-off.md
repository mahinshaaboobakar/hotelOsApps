# 02 · The Jobs walkthrough — for sign-off, section by section

> **This page is the plain-language reading of the reference, written to be
> discussed and signed off one section at a time.** Chapter 01 is the
> engineering survey with file-and-line evidence; this is the same material
> in the owner's language, arranged so a decision can be taken on each part
> and then closed.

---

## How this page is used — the owner's process, 2026-09-02

```text
for each section, in order:
        discuss every point in it
                ↓
        decisions recorded in the section's own table
                ↓
        section marked SIGNED OFF
                ↓
        move to the next section

when all ten are signed off:
        LOCK the whole page
                ↓
        only then does design and build start
```

Two rules that follow from that, and they bind this page:

* **A section is not signed off in part.** Every decision row in it carries a
  ruling, or the section stays open.
* **Nothing is built from an open section.** A locked page is the input to
  the design chapter; an unlocked one is a conversation.

A decision recorded here is an **owner ruling with its date**, and it will be
written into the register and an ADR before any code — that is the
constitution's order, not a preference.

---

## Status

| # | Section | State | Signed off |
|---|---|---|---|
| S1 | The job itself — what a job *is* | **OPEN** | — |
| S2 | Creating a job | not started | — |
| S3 | Assigning it | not started | — |
| S4 | Accept, start, pause, finish | not started | — |
| S5 | **Escalation** | not started | — |
| S6 | Reminders | not started | — |
| S7 | Notifications | not started | — |
| S8 | The guest side | not started | — |
| S9 | Who can see what | not started | — |
| S10 | Scheduled / preventive work | not started | — |
| — | **PAGE LOCKED** | no | — |

---

## How to read a section

Each of the ten has the same four parts:

| | |
|---|---|
| **What it does** | the reference's behaviour, in plain terms |
| **What's wrong** | numbered, each pointing at chapter 01's evidence |
| **What I propose** | the stream's recommendation, and only a recommendation |
| **Decisions** | the specific things needing a yes or a no from the owner |

Decision ids are `S<n>-D<n>` — `S5-D4` is the fourth decision in the
escalation section. They are referenced by that id for the rest of the
project.

---

# S1 · The job itself — what a job *is*

**State: OPEN**

## What it does

Somebody reports something — *"AC not working in 214"*. It becomes a record
carrying: a **type** (Complaint / Request / Maintenance), a **service**
("AC"), a **location** ("214"), a description and photos, a **priority** 1–10,
an **SLA** in minutes, a due date, a **department**, an **assignee**, and a
**status**. Plus a **source** (where it came from: guest app, PMS, feedback,
inspection, scheduled) and a **category**.

## What's wrong

**1 · Location and service are just text.** "214" is a string typed by
whoever raised it. "AC" is a string that the system silently re-capitalises
on save. Neither points at anything real. So *"how many jobs has room 214 had
this year"* is a text search, and renaming a service orphans every job that
used the old spelling. *(01 §F32, F2 of the data model.)*

**2 · The job number is one global counter.** Job 48,210 is the 48,210th job
across every hotel in the system. It is also the public identifier in every
URL. *(01 §2.4, F3.)*

**3 · Priority is a bare number with no meaning, and the system invents it.**
When nobody sets a priority, the code writes **5**. Everywhere it is shown,
under 5 reads "Low", exactly 5 reads "Medium", over 5 reads "High". So
*"nobody has assessed this"* and *"someone judged it medium"* become the same
value, permanently — the distinction cannot be recovered afterwards.
*(01 §F1.)*

**4 · The department is stored twice** — its id *and* its display name — and
other code then builds things out of the display half. *(01 §F32.)*

**5 · There are three ways to classify a job and no rule separating them.**
`type` (Complaint / Request / Maintenance), `category` (free text), and
`service` (free text). In practice all three are used interchangeably by
different callers.

**6 · A third tenancy level that means nothing.** Every table carries
company, site and *facility*. Facility is filtered on, indexed seven ways,
and **never set by anything**. *(01 §F31.)*

## What I propose

* A job points at a **real room or a real asset** from Master Data, never a
  text location. Presentation ("Room 214, Second Floor, Main Block") is
  resolved through the platform's Context service, not stored.
* The job number is **per property** — *"Job 412"* means something to the
  staff standing in that hotel. The internal identifier is a UUID nobody
  reads.
* Priority is a **short named list the hotel configures**, and **"not yet
  triaged" is a real value** — so a supervisor can filter for exactly the
  jobs nobody has judged yet.
* Store the department **code** only. The name comes from Master Data, so a
  rename cannot break anything.
* Keep **one** classification axis plus a source. My recommendation: **type**
  (what kind of work) and **service** (what it is about) — and drop
  `category`.
* No facility level. Organization and property only.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S1-D1** | A job references a Master Data room or asset, never a text location | yes | *open* |
| **S1-D2** | May one job have more than one subject? (a corridor light between two rooms; a whole-wing job) | one subject in v1; a wing-level job attaches to the *area*, not to N rooms | *open* |
| **S1-D3** | Job number scope and format | per property, e.g. `HK-412` or plain `412` | *open* |
| **S1-D4** | Priority — how many levels, what names, and is "not triaged" a value? | 4 levels + not-triaged. Names for the owner to give | *open* |
| **S1-D5** | Are the job types fixed (Complaint / Request / Maintenance) or hotel-configurable? | fixed set in v1 | *open* |
| **S1-D6** | Who owns the **service** taxonomy (AC, WiFi, Cleaning) — Jobs, Core Administration, or Master Data? | needs a platform ruling; Room Care and Maintenance read it too | *open* |
| **S1-D7** | Do we keep `category` as well as type and service? | drop it | *open* |
| **S1-D8** | Do we keep a third tenancy level (facility / block / building)? | no — organization and property only | *open* |

**Sign-off:** _pending_

---

# S2 · Creating a job

**State: not started**

## What it does

Four ways in: the staff app, the guest app, an internal request screen, and a
message from another system. The service saves the job and then fires a
background message; a background worker fills in the defaults afterwards —
type, department, assignee, SLA, priority — from configuration.

## What's wrong

**1 · The job is handed back before it is finished.** The screen receives a
job with no priority, no SLA, no department and no assignee; a moment later a
background worker fills them in. Refresh quickly and you see a half-built
job. *(01 §3.1.)*

**2 · That worker can fail silently.** Every step is wrapped so errors are
logged and ignored. A job can sit with no assignee and no SLA indefinitely
and nothing flags it. *(01 §F8.)*

**3 · You cannot record work that already started.** If a technician began at
09:00 and the supervisor logs it at 10:00, the system forces the start time
to **one minute in the future**. Every retrospectively-logged job — which in
a hotel is most of them — has a wrong SLA clock. *(01 §F36.)*

**4 · Almost nothing is required.** Only the type is mandatory (and a service
unless the type is Maintenance). A job can be created with no location, no
description and no reporter.

## What I propose

* **One transaction.** The job is complete when it is created, defaults
  included. Anything that cannot be resolved is left visibly empty, not
  filled in later by a worker that may not run.
* **Backdating is allowed**, marked as logged-after-the-fact so reports can
  separate it, with the SLA measured from the real start.
* A short mandatory set, so a job is never useless: type, service, subject
  (room or asset), and who is reporting it.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S2-D1** | A job is complete when created — no background fill-in | yes | *open* |
| **S2-D2** | Is backdating allowed, by whom, and how far back? | yes; supervisor and above; 7 days | *open* |
| **S2-D3** | What is mandatory at creation? | type · service · subject · reporter | *open* |
| **S2-D4** | Which ways in do we support at launch? | staff app · another application by event · scheduled. Guest via GuestOps (see S8) | *open* |
| **S2-D5** | May a job exist with no assignee — an open pool the department picks from? | yes, this is the normal case | *open* |

**Sign-off:** _pending_

---

# S3 · Assigning it

**State: not started**

## What it does

A job goes to a person, a team, or a **device** (a shared tablet with shift
slots). Staff can also take an unclaimed job themselves ("capture").
Configuration can auto-assign by service.

## What's wrong

**1 · Assigning to a device crashes the background worker.** "Device" is a
first-class option with its own screens — and the assignment handler throws
on it. The error is swallowed. The assignment saves, but the clock, the
escalation setup and the notification never happen. *(01 §F12.)*

**2 · Reassigning wipes the state flags** — accepted, started, waiting are
all cleared, and different bits of code clear them again at other moments.
*(01 §F10.)*

**3 · Auto-assignment does not check whether the person still works there**,
is on shift, or is on leave. It writes whatever id the configuration holds.

## What I propose

* **Person or team only.** Drop the device idea entirely — a shared tablet is
  a *login*, not an assignee. Who did the work is the person signed in.
* **Reassignment is an event with a before and an after**, not a reset. The
  previous assignee's time stays attached to them.
* **Auto-assignment resolves through Workforce** — the person posted to that
  department, on shift now. If nobody is, the job stays in the pool and that
  is itself visible.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S3-D1** | Assignee is a person or a team — device dropped | yes | *open* |
| **S3-D2** | May a job be assigned to a *department* rather than a person — a queue? | yes; this is the pool | *open* |
| **S3-D3** | Keep self-assignment from the pool ("capture")? | yes | *open* |
| **S3-D4** | Keep auto-assignment by service configuration? | yes, but resolved through Workforce, not a stored id | *open* |
| **S3-D5** | Who may reassign — the assignee, the supervisor, or both? | supervisor always; assignee may hand back to the pool | *open* |
| **S3-D6** | Does reassignment reset the SLA clock? | no — the guest has been waiting since it was reported | *open* |

**Sign-off:** _pending_

---

# S4 · Accept, start, pause, finish

**State: not started**

## What it does

The assignee accepts the job, starts a timer, may pause, ends the timer, and
closes it. There is also a separate "waiting" state for when a job is parked
— waiting for a part, waiting for the guest to leave the room.

## What's wrong

**1 · The state is stored twice and the two copies disagree.** There is a
status (New / Open / On Hold / In Progress / Escalated / Waiting / Closed /
Removed) **and** five separate yes-no flags — accepted, started, waiting,
guest-acknowledged, reopened. Different code updates different ones.
*(01 §F10.)*

**2 · "On Hold" means three different things** — accepted-but-not-started, a
paused timer, and just-came-back-from-waiting. A report cannot separate them.

**3 · Two of the eight statuses are never written.** "Escalated" and
"Removed" exist in the list and nothing ever sets them.

**4 · Ending a timer leaves the job marked as still running.** Two pieces of
code write the same row in the same breath and the second undoes the first.
*(01 §F11.)*

**5 · Any status can jump to any other.** New straight to Closed. Closed back
to In Progress. There are no transition rules at all.

**6 · "Reopened" is counted two different ways**, and one of them trusts what
the phone app sent rather than what is in the database — so the reopen count
on a report is not reliable. *(01 §F9.)*

## What I propose

A single status with a written table of legal moves, and the three meanings
of "On Hold" separated:

```text
        New            raised, nobody assigned
         │
      Assigned         given to a person or team, not yet taken up
         │
      Accepted         the assignee has taken it on
         │
    In Progress   ←→   Paused        timer running / timer stopped
         │              Waiting      parked on something outside our control
         │
        Done            the work is finished
         │
       Closed           verified and signed off

     Cancelled          raised in error, or no longer needed  (from any state)
```

* **Done and Closed are different.** "The technician says it's fixed" and
  "the supervisor agrees it's fixed" are two facts, and hotels need both.
  Whether a hotel *requires* the second is a setting.
* **Waiting stops the SLA clock; Paused does not.** Waiting for a spare part
  is not the hotel's fault; a technician taking a break is.
* **Reopening** is one event with one definition, taken from stored state,
  never from what the client sent.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S4-D1** | The status list above — confirm or amend | as drawn | *open* |
| **S4-D2** | Do we separate **Done** from **Closed**? | yes, with a per-property setting for whether verification is required | *open* |
| **S4-D3** | Who may close — the assignee, or must a supervisor verify? | configurable; default supervisor verifies | *open* |
| **S4-D4** | Does **Waiting** stop the SLA clock, and does **Paused**? | Waiting stops it; Paused does not | *open* |
| **S4-D5** | Reopen — who may, within what window, and is it the same job or a linked new one? | supervisor and above; 7 days; same job, cycle counter increments | *open* |
| **S4-D6** | Cancel — who may, and is a reason mandatory? | supervisor and above; reason mandatory | *open* |

**Sign-off:** _pending_

---

# S5 · Escalation

**State: not started** · *the largest section, and the reference's most
decayed subsystem*

## What it is supposed to do

A guest reports the AC at 09:00. Nobody picks it up. Somebody senior should
find out — and if they do not act either, somebody more senior after that.

The reference has **four clocks**:

| Clock | Starts at | Fires if |
|---|---|---|
| **Not assigned** | the job is raised | nobody has been given it |
| **Not accepted** | it is assigned to a team | nobody on the team took it |
| **Not started** | somebody accepted it | they never began the work |
| **Not closed** | the SLA start time | the work is still not done |

Each clock has **five rungs** — in practice **Supervisor → HOD → Regional
Manager → Cluster → CRO**. Each rung says: *N minutes after the anchor, tell
these people, by these channels.*

**That structure is the best idea in the reference and I would keep it
whole.** Four clocks and a ladder is how hotels actually escalate.
Everything below is about the machinery around it, which is where it fails.

## How it actually runs today

```text
1. anything at all changes on the job — even a note
2. load the escalation policy from a JSON FILE ON THE SERVER'S DISK,
   named after the property
3. DELETE every scheduled escalation for this job
4. recalculate all the deadlines from scratch
5. schedule only the EARLIEST one as a timer, and push the rest into
   that timer's payload as a blob of JSON text
6. when it fires: run it, then re-schedule the next one from the blob
```

## What's wrong — one by one

**1 · The policy is a file on a disk.** To set escalation up for a new hotel,
somebody copies a file onto a server. Not a screen, not a database. Add a
second server and forget to copy the files, and that server escalates
nothing. *(01 §F5.)*

**2 · A missing file disables escalation in silence.** No file → one error
line in a log → the function returns → **that property has no escalation at
all**, and nothing in the product says so. You find out when a guest
complains that nobody came.

**3 · The "which services does this apply to" filter is backwards.** Leaving
the service list empty reads naturally as *"applies to everything"*. It
actually means **"applies to nothing"**. A policy written by someone who
skipped an optional field looks configured and does nothing. *(01 §F6.)*

**4 · Every change demolishes and rebuilds the whole chain.** Someone adds a
note at 11:00 and all scheduled escalations for that job are deleted and
recalculated. If the rebuild fails part-way — missing file, any error — the
old ones are already gone and the new ones never arrive. The job is now
silently unwatched.

**5 · Anything overdue is thrown away.** If the server was down 14:00–18:00,
every escalation due in that window is found to be in the past and
**dropped**, with an "info" log. Nothing records *"these forty jobs should
have been escalated and were not."* And the comment above that code claims it
deliberately drops missed triggers while the code one screen below is set to
**fire them immediately** — the comment and the code disagree. *(01 §F7.)*

**6 · Once an escalation has fired for a job it can never fire again.** The
duplicate check asks *"has this kind ever fired for this job?"* So: job
escalates Monday, gets closed, gets **reopened** Tuesday, then ignored for
two days — and it never escalates again, because it "already did".
*(01 §F35.)*

**7 · The pending escalations live inside a timer's payload.** They are a
JSON string in a scheduler row. So *"which jobs are about to escalate this
afternoon?"* is a question this system cannot answer. And because step 3
deletes the timer on every change, that queue is destroyed and rebuilt
constantly.

**8 · Recipient lists are split on the hyphen.** The code splits recipients
on comma, slash **and hyphen** — so any identifier containing a hyphen (a
UUID, for instance) is shredded into fragments, none of which exist.
*(01 §F25.)*

**9 · Roles are found by gluing strings together.** A supervisor is looked up
by taking the department's **display name** and appending `_SUPERVISOR`.
Rename "Housekeeping" to "Rooms" in the interface and that escalation
silently reaches nobody. *(01 §F32.)*

**10 · Two named people at one customer receive every escalation email in
production.** Two email addresses written into the source code, added to the
recipient list of every escalation at every property. *(01 §F24.)*

**11 · Night handling runs on India's clock, for everyone.** Between 20:00
and 08:00 — hard-coded **Asia/Kolkata** — the on-duty manager is added, and
only for the third rung. There is a `TODO` in the code saying to use the
property's timezone. *(01 §F22.)*

**12 · Maintenance jobs are excluded from escalation entirely**, by one
string comparison at the top of the function.

**13 · SMS and WhatsApp escalation are configurable and disconnected.** You
can switch on SMS escalation; the message is built; the line that sends it is
commented out. Nothing tells the hotel. *(01 §F20.)*

**14 · The SLA-pause arithmetic is patched in three places**, because a job
can leave "Waiting" through several doors and each patch catches what the
others missed.

**15 · Three of the four escalation designs in the code are dead.** A
Mongo-based engine with percentage-of-SLA triggers — its only caller is
commented out. A rules engine with conditions and actions — nothing calls it.
A fully documented ten-rule escalation matrix that exists only as a comment
on a class nobody creates. **Their configuration screens are all still
live** — an operator can build rules that will never run. *(01 §F21.)*

## What I propose

**Keep:** the four clocks, the ladder of rungs, per-service and per-type
filters, and the idea that the clock stops while a job is legitimately
waiting on something outside the hotel's control.

**Change, in order of how much it matters:**

**1 · The policy is data in our own schema, edited in the console.** Per
property, versioned, with a record of who changed it and when. Never a file.

**2 · A property with no policy is a visible state** — shown in the console
and in a health signal: *"Escalation is not configured for this property."*
Silence is never the answer.

**3 · Store the schedule as rows, not inside a timer.** One row per
*(job, clock, rung)* carrying its due time and its outcome —
**pending / fired / cancelled / missed**. Everything then becomes possible:

```text
"what is about to escalate this afternoon?"      a query
"what did we miss during Tuesday's outage?"      a query
"why did nobody hear about job 412?"             the row says so
the timer's only job                             wake me at the earliest pending row
```

**4 · Recompute, do not demolish.** A change recalculates the due times of
*pending* rows. Fired rows stay fired. Cancelled rows record why they were
cancelled — *"the job was assigned before the deadline"*.

**5 · A missed escalation is recorded as missed**, with its reason (platform
down, no policy configured). Whether we then fire it late is the owner's call
— see `S5-D4`. Recording it is not optional either way.

**6 · Deduplication is per cycle, not per lifetime.** Reopen the job and the
clocks start again.

**7 · Recipients are roles resolved at the moment of firing**, through
Workforce: *"who is the duty supervisor for Housekeeping at this property
right now?"* Never a list of ids in a config file, never a string built from
a display name, never a person's email address in the source. **If nobody is
posted to that role, that is itself an escalation** — go straight to the next
rung and say why.

**8 · The property's own timezone, always, with no fallback.** Night windows,
quiet hours and business days come from the property's calendar. A hotel in
Dubai does not run on Kolkata's clock.

**9 · The SLA clock is a list of intervals, not a running total.** Worked
09:00–09:30, waiting 09:30–11:00, worked 11:00–11:20. Sum it when asked. Then
*"why was this job late"* has an answer you can show a guest.

**10 · A channel you cannot deliver on is refused when you configure it**,
with a reason — not accepted and silently dropped at send time.

**11 · And the question underneath all of it: escalation may not belong to
Jobs.** Room Care, Maintenance and GuestOps will each want *"nobody acted
within N minutes, tell someone senior"*. If we build it inside Jobs, the
second application copies it and the two drift. My instinct is that this is a
**platform capability** and Jobs is its first user — but that is a decision
above this application, and it is `S5-D7`.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S5-D1** | Keep the four clocks — not assigned, not accepted, not started, not closed | yes, all four | *open* |
| **S5-D2** | How many rungs, and are the names fixed or per-property? | five rungs; names configurable per property, ladder shape fixed | *open* |
| **S5-D3** | Escalation policy stored in the database and edited in the console | yes | *open* |
| **S5-D4** | After an outage, what happens to escalations that were due? | fire the **highest** rung that was missed, mark the rest skipped, record all of them | *open* |
| **S5-D5** | Deduplication per cycle — a reopened job escalates again | yes | *open* |
| **S5-D6** | Recipients are roles resolved at fire time. What if nobody holds the role? | go to the next rung immediately and say why | *open* |
| **S5-D7** | Does escalation belong to Jobs, or is it a platform capability shared with Room Care, Maintenance and GuestOps? | platform — needs an architect ruling | *open* |
| **S5-D8** | Do escalations pause overnight, or continue through the night? | continue, but the *recipient* changes to whoever is on duty | *open* |
| **S5-D9** | Are Maintenance-type jobs escalated? (the reference excludes them) | yes, with their own policy — planned work has different deadlines | *open* |
| **S5-D10** | Which channels at launch? | email + in-app notification. SMS/WhatsApp only when genuinely wired | *open* |
| **S5-D11** | Can a *single job* be escalated by hand, outside the policy? | yes — a supervisor can escalate now, and it is recorded as manual | *open* |

**Sign-off:** _pending_

---

# S6 · Reminders

**State: not started**

## What it does

Two automatic kinds, and one manual.

**Waiting reminders** — when a job is parked until 15:00, it nudges the
assignee 5 minutes before, again at the time, then the supervisor 5 minutes
after, then the HOD 15 minutes after.

**Progress reminders** — once work starts, it nudges the assignee at 50 %,
75 % and 100 % of the SLA, then the supervisor at 125 %, then the HOD at
150 %.

**User reminders** — a member of staff sets their own reminder on a job.

## What's wrong

**1 · User-set reminders never fire.** They are created, saved and
scheduled — and the function that handles them is **empty**. The whole
feature exists except the part that does something. *(01 §F21.)*

**2 · The thresholds are hard-coded.** 50/75/100/125/150 and −5/0/+5/+15 are
in the source. No hotel can change them.

**3 · The timing uses the server's clock**, not the property's. *(01 §F22.)*

## What I propose

Keep both automatic kinds — the 50/75/100 ladder is genuinely useful and it
is the difference between a job being late and somebody noticing it is *going
to be* late. Make the thresholds configurable. Either build user reminders
properly or take the button away; a button that does nothing is worse than no
button.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S6-D1** | Keep waiting reminders, and are the offsets configurable? | keep; configurable per property | *open* |
| **S6-D2** | Keep progress reminders, and are the percentages configurable? | keep; configurable per property | *open* |
| **S6-D3** | User-set reminders — build them or drop them? | build, but not in the first release | *open* |
| **S6-D4** | Are reminders part of escalation, or separate? | separate — a reminder goes to the person doing the work, an escalation goes over their head | *open* |

**Sign-off:** _pending_

---

# S7 · Notifications

**State: not started**

## What it does

Four channels — email, in-app push, SMS, WhatsApp. Four layers of preference
decide who gets what: company → property → department → individual, plus a
separate set of preferences for guests.

## What's wrong

**1 · SMS and WhatsApp do not work.** Built completely — preferences,
templates, senders, a six-provider list, a fifteen-state delivery tracker —
and every "send" line is commented out. *(01 §F20.)*

**2 · Emails are not emails.** The system writes the message body to an HTML
file on the server's disk and sends **a link to that file**. Guest names,
phone numbers and job details sit in files on a web server, reachable without
logging in, never deleted. *(01 §F18.)*

**3 · The system keeps its own copy of every user's name, email and phone**,
taken once and never refreshed. Change your email in the main system and you
keep receiving mail at the old one. *(01 §F19.)*

**4 · An unresolvable user is addressed as "manager"** — literally that word,
in the message. *(01 §F4.)*

## What I propose

One notification path through the platform. Jobs says *"this happened, these
roles should know"*; the platform decides the channel and holds the
addresses. Jobs stores nobody's contact details — it asks, every time. An
email is an email.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S7-D1** | Which channels at launch? | email + in-app notification | *open* |
| **S7-D2** | How many preference layers do we actually need? | two — property default, and the individual's override | *open* |
| **S7-D3** | Jobs holds no contact details for anyone | yes | *open* |
| **S7-D4** | Who is notified on each event, by default? | needs a table from the owner — see the discussion | *open* |

**Sign-off:** _pending_

---

# S8 · The guest side

**State: not started**

## What it does

Guests raise jobs from a guest app, acknowledge that the work is done, follow
a public tracking page showing the stages, and leave a 1–5 rating with a
comment. A low rating alerts the on-duty hosts.

## What's wrong

**1 · The tracking page is completely broken and reports success.** Both of
its read operations crash on **every single call** — the real lookup was
commented out and the variable left empty with the code that uses it still
there. The controller catches the crash and returns **HTTP 200 OK** with the
error text in the data field. So no monitor ever noticed: the service looks
healthy while the page has never worked. *(01 §F2.)*

**2 · The "guest posts an update" endpoint does nothing and replies
"Sucesss"** — their spelling. The method body is empty.

**3 · A guest is authenticated by putting their id in a header.** No
password, no token, no credential of any kind. Knowing or guessing someone's
id signs you in as them. *(01 §F15.)*

**4 · There are two guest-satisfaction records** — a "rating" table and a
"feedback" table, both holding 1–5 and a comment. *(01 §F26.)*

## What I propose

**GuestOps owns the guest.** A guest request arrives as an event and becomes
a job; Jobs holds no guest identity, no guest login and no guest-facing page.
The tracking page and the rating are GuestOps' surfaces, reading the job's
public state through the platform.

The reason is not tidiness. Every guest-facing feature inside Jobs is a
second place that needs to know who a guest is, and the reference shows what
that costs.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S8-D1** | Does Jobs have any guest-facing surface at all? | no — GuestOps owns it | *open* |
| **S8-D2** | Where does the rating live — Jobs or GuestOps? | GuestOps; Jobs learns of it by event | *open* |
| **S8-D3** | Is a guest tracking page in scope for the first release? | not for Jobs; GuestOps' call | *open* |
| **S8-D4** | Does a guest acknowledgement affect the job's state? | it records a fact; it does not close the job | *open* |

**Sign-off:** _pending_

---

# S9 · Who can see what

**State: not started** · *most of this is platform law rather than our
choice, but two things are genuinely ours*

## What's wrong today

**1 · Reading, updating and patching a job has no tenancy check at all.** Job
ids are sequential numbers; change the number in the address and you read
another hotel's job. The correctly-scoped database query **exists in the code
and is never called once.** *(01 §F3.)*

**2 · Search takes the company id from the request body**, not from your
login. Send someone else's company id and you get their jobs.

**3 · Eighteen of the twenty-three screens' APIs are completely
unauthenticated.** A security rule was written without a path list, so it
silently matches everything remaining and permits it — including an endpoint
that lists work orders for whatever company id you type. *(01 §F16.)*

**4 · `admin` / `password` is in the source in three places** and it
satisfies the security check. The database password, the platform key and
three sets of message-broker credentials are committed too. *(01 §F17.)*

**5 · Sessions are cached for ten minutes.** Revoke someone's access and they
keep working for another ten minutes. *(01 §F14.)*

## What I propose

Every read scoped by your session, never by anything in the request. Every
operation through the platform's authorization. No credentials in the
repository. Jobs caches no security decision — it asks the platform, every
time.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S9-D1** | By default, whose jobs can a member of staff see — their own, their department's, or the whole property's? | their department's; supervisors see the property | *open* |
| **S9-D2** | Can a job be restricted — a complaint about a staff member, say? | yes, a restricted flag visible only to the raiser and management | *open* |

**Sign-off:** _pending_

---

# S10 · Scheduled and preventive work

**State: not started**

## What it does

*"Service the lift every first Monday at 06:00."* A schedule creates a job
each time it fires.

## What's wrong

**1 · If creating the job fails, the failure is swallowed and the schedule
moves on regardless.** A preventive job that was never created looks exactly
like one that was. *(01 §F8.)*

**2 · A schedule set for all seven days of the week can never run.** The
generated timing expression is invalid, the error is caught and logged, the
schedule saves, and it never fires — silently. *(01 §F34.)*

**3 · Every generated job carries the same fixed paragraph of English** as
its description.

## What I propose

This may not be ours at all. The platform's own list treats **Maintenance**
and **PPM** as applications separate from Jobs, and the field-ownership rules
put *"what maintenance does this asset need"* in Maintenance and *"what work
is being performed"* in Jobs.

My recommendation: **Maintenance decides what is due and announces it; Jobs
creates the job.** That keeps the schedule where the asset knowledge is, and
keeps Jobs as the place work is executed.

## Decisions

| id | Decision | Recommendation | Ruling |
|---|---|---|---|
| **S10-D1** | Does Jobs hold schedules, or does Maintenance announce and Jobs create? | Maintenance announces; Jobs creates | *open* |
| **S10-D2** | Is scheduled work in scope for the first release of Jobs? | no — Jobs must be able to *receive* it, but not schedule it | *open* |

**Sign-off:** _pending_

---

# Lock

**The page is not locked.** It locks when all ten sections carry a sign-off,
and nothing is designed or built from it before then.

| | |
|---|---|
| Sections signed off | 0 of 10 |
| Page locked | no |
| Locked on | — |

When it locks, three things follow, in this order and no other:

1. every ruling recorded in the platform's question register;
2. an ADR for the decisions that change the platform rather than only this
   application — at minimum `S5-D7` (whose escalation is it) and `S1-D6`
   (who owns the service taxonomy);
3. the Jobs design chapter, written against the locked page.

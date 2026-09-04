# Chapter 05 · What is wired, and what is not

**Stream HH, 2026-09-04.** The owner asked whether the screens are actually
connected — CRUD, filters, pagination, every component — now that the drawing
and the build have been adjudicated. This is that account, measured from the
code rather than remembered.

> **The short answer: no screen is connected to the backend.** Both halves are
> built and each is sound on its own, and **nothing joins them**. The desktop
> has no Jobs client, so every read falls to the recorded example — which each
> screen says on its face — and **not one control reaches a write.**

That is not a surprise and it is not hidden: the seam was built to stand in
until a client lands, and the audit page marks those places. What this chapter
adds is the exact size of the gap, so it can be closed deliberately.

---

## 1 · The two halves

| | Built | Proven |
|---|---|---|
| **Backend** | 28 RPCs, 26 tables, one migration, the sweep, five event handlers | 39 characterisation tests against a real PostgreSQL; the migration is in step with the model (`has-pending-model-changes`: none) |
| **UI** | 9 screens, 7 job tabs, 6 settings tabs, 1 widget | 38 tests; both bundles verify self-contained against the fourteen tokens |
| **Between them** | — | **nothing** |

The module's only route out of its realm is `host.call`, and it appears in
exactly two places: the read seam (`ui/board/index.ts`) and the widget's
"open this screen" request. There is no third.

---

## 2 · Reads — nine asked for, four a message can answer

The seam asks the host for nine things by name. The service offers four
messages that fit, one that fits half, and four that do not exist.

| The screen asks | The service offers | State |
|---|---|---|
| `board` | `ListJobs` | **exists** — but see the filter contract below |
| `scheduled` | `ListJobs` with `scheduled_only` | **exists** — same contract problem |
| `job` | `GetJob` | **exists** |
| `catalogue` | `ListCatalogue` | **exists** |
| `live` | `ListPresence` | **half** — presence flags only. Who is working on what, and the sweep's concern table, have no message |
| `today` | — | **no message**: the board's six figures (open, breached, stuck, running, closed today, average) are computed nowhere |
| `settings` | — | **no message**: policies, subscriptions, service hours, closing, holds and access are readable by no RPC |
| `jobsNow` | — | **no message**: the widget's numbers |
| `me` | — | **no message**: who is signed in. The header draws nobody until this exists |

---

## 3 · Writes — twenty-four RPCs, none called

Every write the design needs exists on the service and is reachable over gRPC.
**None is called from a screen.** Counting the module's controls:

| | Count | What they do |
|---|---|---|
| Controls that act | 24 | Move between tabs, filters, pages and flow steps — all local state |
| Controls that do nothing | 28 | Pause · Stop · Put on hold · Reassign · More · Link a job · Add a step · Unlink · Add note · Attach photo · Remind me · Raise (the form's own button) · Resolve (the form's own) · the resolution chips · New category · Edit · Add resolution · Create item · Save · Discard · ＋ step · Add |

The two "Resolve…" controls and "＋ Raise a job" do act, but only to open the
form; the form's own button closes it again without sending anything.

---

## 4 · Filters and pagination — the specific question

**They move, and they do not filter or page.**

* The six board chips and the pager set local state and re-request through the
  seam with `{ filter, page }`.
* The stand-in ignores parameters, so **the same twelve rows appear under every
  filter and on every page.** Pressing "2" changes the highlighted button and
  nothing else.
* Even with a client, that request would not work: `ListJobsRequest` takes
  `department_code`, `statuses`, `scheduled_only`, `assignee_user_id`,
  `page_size` and `page`. The seam sends a single opaque `filter` string. **The
  two shapes have to be reconciled**, and the chips' meanings decided —
  "Restricted" and "Raised by guests" have no field in the request at all.
* The Live tab's *"6 of 9 · more load as you scroll"* has **no scroll handler**.
  The caption describes an intention; the card scrolls its six and stops.

---

## 5 · CRUD, entity by entity

| Entity | Create | Read | Update | Remove |
|---|---|---|---|---|
| Job | `RaiseJob` | `ListJobs`, `GetJob` | Assign · Accept · Work · Resolve · Close · Reopen · Hold · Amend | `CancelJob` only. The `deleted_at/by/reason` columns exist and **nothing writes them** |
| Category / Item / Resolution | `SaveCategory`, `SaveItem`, `AddResolution` | `ListCatalogue` | the same three | **none** — the `deleted_at` columns are unused |
| Property item policy | `SaveItemPolicy` | only implicitly, through the catalogue's activation | `SaveItemPolicy` | **none** |
| Concern policy | `SaveConcernPolicy` | **no read RPC** — the settings screen has no message | `SaveConcernPolicy` | `DeleteAsync` exists on the service and **has no RPC**, so no screen can reach it |
| Subscriptions · service hours · closing · holds | service methods exist | **no read RPC** | **no RPC at all** — `SaveSubscriptionsAsync`, `SaveHoursAsync`, `SaveClosingAsync`, `SaveHoldAsync` are unreachable | — |
| Presence | — | `ListPresence` | `SavePresence` | — |
| Note · attachment · reminder · rating | `AddNote`, `Attach`, `RemindMe`, `RateJob` | through `GetJob` | — | — |
| Nudge | written by the sweep | **no read RPC** | `ReadNudgesAsync` exists and **has no RPC** | — |

**Six service methods are built and unreachable**: the concern policy's delete,
the four settings saves, and marking nudges read.

---

## 6 · What the 39 tests do and do not cover

They are service-level and they are real — a scratch PostgreSQL, the check
constraints, the sweep to the minute. What they never touch:

* **The gRPC layer.** No test constructs `JobsGrpcService`. Every request
  parse, every view mapping and every status code is unexercised.
* **The query layer.** `JobQueries` — what *every screen reads* — has no test.
  The board's ordering, its paging arithmetic, the job detail's assembly and
  the catalogue's per-property filtering are all unproven.
* **Three services entirely**: `NoteService`, `RatingService`,
  `ClosingHoldService`.
* **One path within another**: `AssignmentService.AssignAsync` — reassignment —
  is never called; only the raise-time assignment is.

---

## 7 · Not yet done at all

* **No package.** No `.hopkg` has been built; the manifest's `files:` still
  carries placeholder digests. Nothing has been installed, so the install
  transaction, the schema provisioning and the grant-kind consent screen are
  untried.
* **No end-to-end walk.** The service has never been started against a Kernel,
  and no event has crossed NATS: the five subscriptions and the fourteen
  published events are proven only as appended rows in a transaction.

---

## 8 · What closing the gap needs, in order

1. **Four messages that do not exist** — `today`, `settings`, `jobsNow`, `me` —
   and the two halves of `live` that `ListPresence` does not carry. Each is a
   read the screens already draw.
2. **The filter contract**, reconciled: the chips' meanings against
   `ListJobsRequest`, including the two that have no field.
3. **A desktop client for Jobs**, which is the platform's to add — until it
   exists, `host.call` fails `unavailable` by design and the stand-in is
   correct behaviour, not a defect.
4. **The write wiring**, twenty-four controls to twenty-four RPCs, with the
   version each command carries and the refusal each can meet.
5. **Six unreachable service methods** given RPCs, or removed.
6. **Tests where there are none**: the gRPC layer, the query layer, and the
   three untested services.
7. **The package**, and the walk that proves an install.

Nothing in that list is a surprise to the design; all of it is the distance
between a surface that is drawn correctly and an application that runs.

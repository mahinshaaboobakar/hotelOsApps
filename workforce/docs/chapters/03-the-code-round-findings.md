# 03 · The code round findings — what slice 1 met that the design could not see

**Status:** findings, 2026-08-31. Stream GG, opened when backend code went GO
(owner, 2026-08-31). **Facts measured in the platform tree, with the file and
line.** Nothing here is resolved by this stream; each finding is a register
question, per the owner's own instruction that *gaps are register questions*.

**Why a page rather than a comment.** Four of the five findings below are one
mechanism seen from four sides, and that is only visible with them written
together. Individually they read as four small gaps; together they say something
the design round could not have known.

---

## The headline

> **Chapter 01 §4's central promise — a posting announces, the Kernel
> materialises `department:{id}#posted@user:{uid}`, and My Hotel's dormant
> department grants come alive — cannot happen today.** The announcement has no
> consumer, no version it may legally carry, and no way to name the department
> the tuple is written on.

Slice 1 is built and green **without the announcement**, and the seam is left
where it goes. That is deliberate: publishing anyway would either violate a
database constraint or announce into a consumer that silently drops it, and the
second is the failure mode this whole round exists to prevent.

**The rest of slice 1 is unaffected.** Postings are created, amended, ended,
read and listed; the overlap rule is enforced; the zone is on the posting; the
department is validated against the property's activated canon. What is missing
is the authorization consequence, and it is missing on the platform side.

---

## F1 · Nothing consumes a posting announcement

**Measured** — `services/kernel/crates/kernel/src/events/registration/`:

* `plan.rs:79-108` is the whole translation. It tries `grants::find` first, then
  gates on `TYPES`.
* `grants.rs` defines **four** grant kinds and no more: `folder.access_granted`
  / `access_revoked`, `user.assigned` / `assignment_ended` (property member),
  `user.general_manager_granted` / `revoked`, and
  `user.application_access_granted` / `revoked`.
* `plan.rs:86` states, deliberately, that **`user` is not a registrable type and
  never becomes one**.

So `grants::find("user", "posted")` answers `None`, the `TYPES` gate rejects
`user`, and `plan()` returns `None`. **A `user.posted` published today writes no
tuple**, and does so quietly — the event is stored, relayed and acked.

`grants.rs:118` names the gap from the other side, in the Kernel's own words:
*"`department#posted` has no writer until Workforce."* That sentence is about
the **producer**. This finding is that the **consumer** is missing too.

**What it needs:** a fifth `GrantKind` — `aggregate_type: "user"`, granted
`posted`, revoked `posting_ended` — writing relation `posted` on a
**department** object. `GrantObject` has no department variant today.

## F2 · The tuple addresses a UUID; a posting carries a code

`model.fga:419` defines `posted: [user]` on `department`, and departments are
registered as `department:{uuid}` like every other registrable type. ADR 0119
makes the **code** the canonical business identity — immutable, identical in
every installation, what reports group on — and it is what a posting stores and
must store.

Both are right, and they do not meet. **The announcement must carry the
department's row id as well as its code**, resolved per property.

This half is solved on this side: `IStaffDirectory.FindDepartmentIdAsync`
resolves code → id through Master Data's `ListDepartments`, and `CreatePosting`
already refuses a code the property has not activated. It is recorded here
because the *payload shape* is a contract question, not a local one.

## F3 · The announcement has no version it may carry

The event store's `uq_events__aggregate_version` is
`UNIQUE (aggregate_type, aggregate_id, entity_version)`. The platform's pattern
for an announcement about another record is to **bump the aggregate row's own
version** — `StaffPropertyScopeService` increments `staff.Version` before
appending `staff.assigned`, and its comment records the bug that taught it:
*"it used to be the literal 1, which collided with `staff.created` on the very
first call."*

**Workforce has no user row to bump.** The user lives in Identity's schema, and
this application must not hold a copy of one. The alternatives are all wrong:

```text
the posting's version    collides on the SECOND posting for one person —
                         two postings both at version 1, both announcing
                         against user:{uid}
a literal                the bug the platform already fixed once
a local counter table    bookkeeping about somebody else's aggregate,
                         invented inside an application
```

## F4 · The `department#manager` writer has no mechanism either

Chapter 01 §4 says a department-head posting writes `department#manager`, and
ADR 0114 §5 records `general_manager` and `department#manager` as **Workforce-era
hooks**. Nothing specifies the event that would write the second. The flag is
stored and the approver resolution reads it; the graph is untouched.

## F5 · A login granted *after* a posting never produces a tuple

`masterdata.staff.user_id` is nullable and usually empty — the platform's own
proto says *"that nullability is the whole point"*. A posting for somebody with
no account correctly announces nothing.

But when that person is later given a login, **nothing re-announces**. Master
Data emits a staff update; no one is listening for the transition from *no
identity link* to *one*. The person keeps working and quietly never gains the
department access their posting implies.

---

## What the five say together

> **The Kernel's authorization-registration model assumes the announcing service
> owns one end of the grant.** `grants.rs` derives *"the grantee is the
> aggregate"* from the object kind, and states the assumption plainly: *"the
> announcing service owns exactly one end, that end is the aggregate, and the
> other end is therefore in the body."*
>
> **Workforce owns neither end.** It owns the *relationship*. The user is
> Identity's, the department is Master Data's, and the posting that joins them
> is this application's — which is exactly what ADR 0063 §Q5 designed it to be.

F1 is that model having no case for it. F3 is the versioning consequence of the
same fact: there is no local aggregate whose counter the announcement can ride.
F2 is the naming consequence. F5 is what happens when one end changes after the
fact.

**This is not a defect in the Kernel.** The registration model was built for
services that own an entity and announce about it, and it does that well. An
installable application whose entire purpose is a relationship between two
records it does not own is a shape that had not existed before — Workforce is
the first, and Jobs and Room Care will be the second and third.

## What this stream did, and did not do

* **Did:** build slice 1 complete and green without the announcement, with the
  seam and the reason at the call site in `PostingService.CreateAsync`.
* **Did:** solve F2's resolution locally, because that half is an application's
  own job.
* **Did not:** invent a grant kind, a version scheme, or an event shape. Each
  would be deciding a Kernel question inside an application, and would be
  discovered later as a fact rather than a decision.
* **Did not:** publish `user.posted` into a consumer that drops it. A published
  event that nothing acts on is indistinguishable from a working one, which is
  the specific way this would have gone wrong quietly.

## For the register

One question, with four parts, because splitting it would invite four
independent answers to one shape:

> **How does an application announce a relationship between two entities it does
> not own, such that the Kernel materialises it?** Covering: the grant kind and
> its department object (F1), the payload's identity fields (F2), the
> announcement's `entity_version` when the announcer holds no aggregate (F3),
> the `department#manager` counterpart (F4), and the late-identity-link
> transition (F5).

---

## F6 · An installed application has no grant on `event_store`

**Found by scaffolding the migration**, 2026-08-31, and it is the one finding
that would have failed at runtime rather than silently.

`StoredEventConfiguration` maps to **`event_store.events`** and
`event_store.publish_state`, both `ExcludeFromMigrations()`: *"a different
schema, and not one this service owns — the relationship is the one a service
has with the write-ahead log: it appends, it does not own, and it cannot
modify."* Correct, and it means an application's `migrate` verb creates its own
tables and **not** the event store's.

So appending needs an `INSERT` grant on two tables in a schema this package does
not own. `deployment/database/04-grants.sql` grants `event_store` usage to
`hotelos_kernel_admin` and `SELECT` to the four platform runtimes
(`ai_runtime`, `analytics`, `connector_runtime`, `readonly`); the table-level
grants live in a later file, keyed to roles that exist at bootstrap.

**Nothing in that path provisions a role for an installed application.**
`ADR 0092`'s install steps create the schema and run the migration; whether they
also grant the package's role `INSERT` on the event store is not something this
stream could find. If they do not, the first announcement fails with a
permission error — loudly, which is the better failure, and after the
`AUTHZ-Q20` contract has already been declared done.

Recorded now rather than when the announcement lands, because the fix belongs to
the package contract and not to this application.

## APPS-Q3 · three schemas still carry old application names

**Reported, not fixed** — colleague files, per the standing order. Found by the
same sweep that cleared this application's own surface.

```text
deployment/database/03-schemas.sql:72   CREATE SCHEMA housekeeping   → roomcare
deployment/database/03-schemas.sql:77   CREATE SCHEMA workorder      → jobs
deployment/database/03-schemas.sql       CREATE SCHEMA reservations   → guestops
deployment/database/02-roles.sql:31-32   the owner-role list, twice
deployment/database/02-roles.sql:58-59   their connection limits
deployment/database/02-roles.sql:107-108 and again
deployment/database/04-grants.sql        7 references
```

`APPS-Q3` names schemas explicitly — *Jobs, schema `jobs`; Room Care, schema
`roomcare`; GuestOps, schema `guestops`* — and nothing has shipped on any of
the three, so they are renames outright rather than migrations. The Kernel's
event domains were already swept (`streams.rs`, `jetstream.rs`); the database
scripts were not.

**Two occurrences deliberately excluded from that list**, on the distinction the
rule now carries: `ZoneTypes.Housekeeping` and the `housekeeping_operator`
relation are the **department and the function**, not the application. Room Care
is the app; Housekeeping is the department, and it stays Housekeeping.

---

## Claimed as `AUTHZ-Q20` — unsplit, and half answered the same day

**Architect, 2026-08-31.** The question is claimed whole, as argued, and its
frame is ruled: **the capability was frozen long ago** — ADR 0116 §6 makes
department access derive from Workforce postings *permanently* — so this is an
**implementation contract under frozen ADRs**, jointly CC and GG, ratified
before code. `grants.rs:118`'s own comment had been waiting for this
application.

### F2 — answered: the event carries the canonical id

The constitution's own split: **the canon code is vocabulary, the id is
identity**. The announcement carries the department's row id, and the
`ListDepartments` resolution in `IStaffDirectory` is at the right place —
resolved at the point of writing, never stored on the posting where it could go
stale.

### F3 — answered: the Posting is the aggregate

**`HUB-Q4`'s *announce against what you own***, minted the same day for the
Integration Hub's identical shape. A service announces against the aggregate it
owns; Workforce owns the posting.

That dissolves the versioning problem rather than working around it:

```text
posting_id has its own sequence     one posting, one counter
no per-person collision             two postings, two aggregates
no foreign row bumped               nothing of Identity's is incremented
```

**Applied in the code the same day**: `EventTypes.PostingAggregate`, and
`entity_version` is `posting.Version`. Chapter 01 §4 said the `user` aggregate
and is superseded — it was the shape before the question was put.

### F1 and F5 — the joint design with CC

The grant kinds (which relation, on which object, from which event type) and the
late-arriving-identity-link reconciliation are the joint work, under the frozen
ADRs. Nothing is published from here until that contract lands.

### F4 — carried

`department#manager` rides with F1: it is the same missing shape with a
different relation on the same object.

Numbers are the architect's to claim; this one was claimed as `AUTHZ-Q20`.

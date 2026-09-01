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

## F7 · The test harness assumes a platform service, and an application is not one

**Found by running the characterisation suite**, 2026-08-31 — the first run,
against a live development PostgreSQL:

```text
Failed!  Failed: 15, Passed: 0, Skipped: 0
Npgsql.PostgresException : 42704: role "hotelos_owner_workforce" does not exist
```

`ScratchDatabase.CreateSchemaAsync` creates the schema
`AUTHORIZATION {SchemaMigration.OwnerOf(schema)}` — `hotelos_owner_workforce`
— and that role comes from `deployment/database/02-roles.sql`, whose list is a
**fixed enumeration of platform schemas**: identity, masterdata, platform,
reservations, housekeeping, workorder, inventory, procurement, finance,
integration. There is no `workforce`, and there should not be: an installable
application's schema and owner role are created **by the installer**, on a
property's cluster, at install time.

So on a developer's cluster nothing creates them, and **an installable
application's characterisation suite cannot stand up its own schema at all.**
The harness was written for services that ship with the platform, and it
encodes that assumption in one line.

**The suite is written, builds clean, and fails for exactly this one reason** —
15 failed, **0 skipped**, which is ADR 0053 working on its first outing: the
harness could not provision, so it failed the run loudly instead of reporting
success having executed nothing.

**Two candidate fixes, and the choice is not this stream's:**

| | |
|---|---|
| **The harness provisions it** | `CreateSchemaAsync` creates the owner role when absent, or takes one. It already creates and drops databases, so this is the authority it holds — but the role it invents would differ from the one the installer creates, and the suite would then be testing against a role no property has |
| **The dev cluster gains it** | `02-roles.sql` grows the application schemas. Honest for a developer's machine, and it makes a fixed enumeration carry a list that is by definition open — every future application is another line, which is the shape `PKG-Q39` already refused for event domains |

The second is the same shape as this page's other findings: **a platform
mechanism enumerating what applications there are**, when the whole point of a
package is that the platform does not know. Recorded rather than resolved.

### Ruled 2026-08-31 — option 1, with its objection dissolved

> **The harness provisions the role and schema to the installer's convention —
> derived, not invented.** *"A role no property has"* applies only to a role you
> mint; a role you derive is the installer's, and drift between the two is caught
> where ADR 0054 puts connections — the install-chain E2E.

Built as `InstallerConvention`, reproducing
`packages/database.rs:180-256` in its order with the names from
`kernel-core/src/package/naming.rs:45-48`. The precedent named in the ruling is
`tests/mtls_fixtures/mod.rs:181`, which recreates an installer-owned artifact to
specification for the same reason.

**Option 2 was refused on this stream's own argument**: a fixed platform
enumeration growing one line per future application is `PKG-Q39`'s shape again.

**Two adjustments the run forced, both credential rather than convention:**

* `hotelos_test` holds `CREATEDB` and **not** `CREATEROLE` — established by
  being refused *"42501: permission denied to create role"*. The installer runs
  step 4 as a provisioner that holds it; on a developer's cluster the equivalent
  authority is the superuser. **What** gets created is the installer's; **who**
  runs it is whoever holds the authority on the cluster at hand. Widening
  `hotelos_test` was the alternative and is a larger change than this needs.
* `hotelos_event_appender` did not exist on this cluster — it is **cluster
  bootstrap** (`02-roles.sql:254-263`), not installer output, and this cluster
  predates `AUTHZ-Q23`. Its own idempotent block is reproduced verbatim, minus
  the `GRANT … TO hotelos_provisioner` that is the platform's. `make
  db-bootstrap` is the proper remedy and could not be run here — the Docker CLI
  is unresponsive in this environment while PostgreSQL itself answers fine.

**`AUTHZ-Q23` is already implemented on the platform side** — `database.rs:246`
grants `hotelos_event_appender` to the application role at install, beside the
Master Data read window, *"because they are the two grants an application holds
on somebody else's schema"*. **F6 is closed**, and this fixture inherits it.

### The suite is green — 15 passed, 0 failed, 0 skipped

**Two failures on the way there were the tests' own**, and one is worth keeping:

* `List_excludes_ended_postings_unless_asked` ended a posting **in the future**
  and expected it to disappear. The service was right and the test was wrong —
  *ended* means the window has closed, not that somebody has typed a closing
  date. Rewritten to a window in the past, and the distinction is now a comment.
* `A_refused_permission_stops_the_write` asserted no Front Office posting
  existed — reading every sibling test's rows out of a shared fixture. Scoped to
  its own staff id: an assertion that reads other tests' rows is one that fails
  when somebody adds a test.

## F8 · There is no soft-delete convention for an installable application

**Raised as a flag on slice 2's `Remove`** — *"the platform's blanket
soft-delete interceptor means your Remove needs ADR 0045's `IHardDeleted`
marker"* — and **the premise does not hold**, which is the finding.

Measured 2026-09-01:

```text
IHardDeleted                     services/masterdata-service/src/Domain/MasterEntity.cs:122
the interceptor that honours it  services/masterdata-service/src/Infrastructure/
                                 MasterDataDbContext.cs:128-148  — an override of
                                 that service's own SaveChangesAsync
in packages/sdk-dotnet           nothing. No DeletedAt, no Active, no interceptor
```

**The soft-delete convention is Master Data's, not the platform's.** It is bound
to `MasterEntity` — which carries `Active`, `DeletedAt` and `CreatedBy` — and
implemented in that service's own `SaveChangesAsync`. An installable
application's `DbContext` inherits none of it.

So slice 2's `Remove` is already a hard delete, and not by an opt-out: **nothing
was intercepting it.** The characterisation test
`Removing_takes_the_row_and_reading_it_again_is_not_found` proves the row is
gone. `IHardDeleted` cannot be applied here because it does not reach this
repository at all, and adding a copy of the marker would be a copy of a
Master Data concept with no interceptor to read it — decoration.

### Which is right for `Capability`, and a gap for what comes next

For this aggregate the outcome is the ruled one: `Remove` is for the row that
should never have existed, an expired capability is **kept** because the
register showing what lapsed is the point, and the operation is
permission-gated (`capability.manage`) and version-checked.

**The gap is that an installable application has no lifecycle convention at
all.** ADR 0062 rules `active` + `deleted_at` with Deactivate/Reactivate — and
it rules it for **master entities**. An application's operational records are
outside it, so every application invents its own, and the ones this round has
already designed will need one:

```text
LeaveRequest      has a Cancelled state — a lifecycle, invented per app
AttendanceRecord  must never be silently deleted: it is evidence
ShiftAssignment   deleting one loses what a rota was worked under
```

**Recorded, not resolved.** Whether the platform should offer one — an SDK
base entity, an interceptor, or a stated rule that applications choose per
aggregate and say why — is a package-contract question, and it is the fourth
finding in the same family: **a convention that exists for platform services
and stops at the package boundary.**

## F9 · `holidays worked` cannot be computed — there is no holiday calendar

**Chapter 01 §3.7 lists seven figures for `WorkforcePeriod`. Slice 6 produces
six.** The seventh is *holidays worked*, and it is blocked on a capability that
does not exist.

Measured 2026-09-01, across the whole platform:

```text
masterdata Domain/        no Holiday entity, no calendar, no column
masterdata protos         no holiday message, no field
services/ (all)           two matches, both unrelated strings in test files
```

**`WF-Q16` already ruled where it belongs**: the administrator establishes the
property's holidays in Core Administration exactly as they establish
`check_in_time`, and this application *reads* them. That ruling is right and is
not in question — what is missing is the thing it rules on.

### Why it is not built here

An installable application creating a holiday calendar would put a Core
Administration concern in a package, which is the boundary ADR 0051 exists for.
It would also be the third form of the same mistake this round has already named
twice:

```text
shift pattern       a taxonomy invented before a consumer exists
languages           a second table invented for symmetry
holiday calendar    a Core Administration entity invented by its reader
```

And the failure mode is worse than the other two, because a holiday calendar
**looks** like application data until somebody asks which application owns it.
The moment Room Care or Jobs needs the same list, a second one appears.

### What slice 6 does instead

`WorkforcePeriod` carries the six figures it can produce and **does not carry a
zero for the seventh**. A field reporting `0` would be indistinguishable from a
property whose staff worked no holidays, and payroll would have no way to know
the number was never computed — the same silence ADR 0053 ends for an absent
database, one domain over.

**The unblocking condition is named**: when Core Administration establishes a
property holiday calendar, this figure is a count over days already in the
comparison, and it needs nothing else from this application.

## F10 · Slice 7's outward half is gated, and the chapter says so

**Slice 7 named two things. One is built, and two are deliberately not** — and
in both cases the instruction not to build them is already written down.

### `shift pattern` — the chapter says establish its purpose first

Chapter 01 §7, verbatim:

> *"With the rotation engine refused (R7, the owner's own refusal), **`shift
> pattern` has no consumer in v1** — it is inherited from ADR 0063 §Q5's
> remainder list rather than asked for. It sits in slice 7, and what it is *for*
> should be established before it is built."*

Building it would be a taxonomy invented before a consumer exists — the mistake
this round has now named four times, and the only one where **the chapter itself
gave the instruction in advance**. Nothing is built and nothing is proposed.

### The capability read-view — the consumer has not shipped and the route is unruled

Two gates, either one sufficient:

```text
chapter 01 §7   "the assignment half exists to answer 'who can do X' for
                 another application, so it SHIPS WHEN THAT APPLICATION CAN ASK"
                 — Jobs has not shipped
WF-Q6           routes the answer through the Context Service, and the register
                 records Q2–Q6 as "architect calls at design ratification"
                 — so the route is recorded, not ruled
```

**Designing a cross-service read-view without its consumer is how the shape comes
out wrong**, and the shape would then be a published contract two applications
depend on. `GetStaffContext` already exists on Context's surface, so the question
when it is asked is whether Workforce's answer joins it or arrives as its own
RPC — which is Context's call and the architect's, not this application's.

### What slice 7 built instead

**The inward half**, which has a consumer today — the rota screen — and invents
nothing: `AssignmentAdvisor` reads the postings, the capability register, leave
and the rota, and says what is worth knowing before a cell is filled. Every
answer is a **warning**, and it is **not wired into `RotaService`**: a manager
covering a sick shift at six in the morning is not helped by a validator.

**It matches no required capability against a shift, and that is the same
refusal.** No shift declares what it needs — nothing in the platform says a
night shift requires a fire warden — and inventing that vocabulary is the
taxonomy problem again. What the register can honestly say is *this person's
certification has lapsed*; the manager knows what the shift needs.

## F11 · Workforce's twelve permissions are not in the registry, and five cannot be

**The manifest is written and the package does not install**, because install
resolves every permission id against
`infrastructure/openfga/permissions.yaml` and **none of the twelve is there**
— measured 2026-09-01. That is the blocker GuestOps met and had closed by a
ruling; this one is open.

### Five collide with the registry's own rules, which this application may not overrule

The registry states both rules in its header:

```text
`write`, `manage` and `edit` are BANNED     posting.manage   shift.manage
   "manage says nothing about blast radius"  capability.manage  policy.manage

a permission never names the service or       workforce.read
application that implements it                — the rule that sent
                                              guestops.configure → desk.configure
```

**Seven are already well-formed** and need only registry rows: `leave.request`,
`leave.approve`, `duty.assign`, `swap.propose`, `swap.approve`,
`attendance.record`, `attendance.amend`.

### Proposed spellings — proposals, not decisions

A permission name is a **stable platform contract**, and an application does not
mint one for itself. These are offered the way GuestOps' four were, for the
architect to rule:

```text
posting.manage      →  posting.assign        posts a person to a department
                                              and ends the posting
capability.manage   →  capability.record     records skills, languages and
                                              certifications
shift.manage        →  shift.roster          fills the rota — the verb the
                                              domain already uses
policy.manage       →  workforce.configure   splits: the catalogue and leave
                    →  shift.define          types are policy; defining a shift
                                              is its own capability
workforce.read      →  roster.read           `roster` is the thing being read,
                                              not the application that serves it
```

**`workforce.configure` is proposed knowing it names the application**, and is
flagged rather than hidden: `desk.configure` was GuestOps' answer to the same
problem, and Workforce has no equivalent noun — *the desk* is a place, and this
application configures a property's working rules rather than a place. It may be
that `policy.configure` is the answer, or that the capability splits further.
**That is the part this application cannot settle.**

### Until it is ruled

The manifest carries **the spellings the code requests today**, so it describes
the software that exists rather than software somebody intends, and the block
above it says so in the file. A manifest quietly written to proposed names would
be a package that matches no running service.

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

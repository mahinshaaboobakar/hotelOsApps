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

Numbers are the architect's to claim.

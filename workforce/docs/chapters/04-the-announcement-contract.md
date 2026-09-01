# 04 · The announcement contract — Workforce's half of `AUTHZ-Q20`

**Status:** draft, 2026-09-01. Stream GG, the **application half**, written so the
joint design with the Kernel stream starts from a page rather than a blank one.
**Nothing here binds the Kernel.** §2–§4 are what this application will emit and
what must be true of it; §5 is an observation about where the Kernel's model has
no case for it, offered with three candidate shapes and no preference expressed
between two of them; §6–§7 are the two cases the register row lists as open.

**Already ruled, and built** — `AUTHZ-Q20`, 2026-08-31:

| | |
|---|---|
| The **Posting is the aggregate** | `HUB-Q4`'s *announce against what you own*. `posting_id` has its own sequence, so `entity_version` is `posting.Version`: no per-person collision, no foreign row bumped |
| The payload carries the **canonical id** | The canon code is vocabulary; the department's row id is identity. Resolved through `ListDepartments` at the point of writing, never stored where it could go stale |

**The frame**, from the same row: the capability was frozen long ago — ADR 0116
§6 makes department access derive from Workforce postings *permanently* — so
this is an **implementation contract under frozen ADRs**, not a new decision.

---

## 1 · What is being announced, in one sentence

> **A person has gained, or lost, a place in a department at a property** — and
> the record that establishes it is a posting this application owns.

Everything below follows from that sentence having **three** nouns in it, two of
which belong to somebody else.

## 2 · The two events

```text
event type        user.posted            user.posting_ended
domain            user                   user            ← routed since Master
                                                            Data grew staff
aggregate_type    posting                posting         ← AUTHZ-Q20
aggregate_id      the posting's id       the posting's id
entity_version    posting.Version        posting.Version
```

**The event type's domain and the aggregate deliberately differ**, and it is
worth saying why rather than treating it as an inconsistency to tidy away:

* the **fact** is about a user — *this person now works in Front Office* — and
  ADR 0006 routes by what an event *means*, so it belongs beside `user.assigned`
  under `property.*.user.>`, where a consumer that cares about people already
  listens;
* the **record** that establishes the fact is a posting, which is what this
  application owns and what supplies a version nobody else's row has to lend.

Naming a fifth routed domain `posting` was the alternative and is refused twice
over: `PKG-Q39` settled the domain set at four the same week, and a consumer
wanting *"who works where"* should not have to subscribe to a second domain to
learn it.

### The payload

```text
user_id          Identity's, resolved from masterdata.staff.user_id
department_id    the department ROW id — what the tuple addresses
department_code  the canon code — ADR 0119, what reports group on
posting_id       so a consumer can correlate the end with the start
property_id      the tenancy boundary; the tuple's department is this
                 property's row, never another's
occurred_at
```

**Both department identifiers, deliberately.** The id is what
`department:{uuid}` needs; the code is what the fact *means* and what survives a
database being rebuilt. Carrying only the id would make the announcement
unreadable to a human debugging it; carrying only the code would make it
unusable to the consumer that must write a tuple.

### When they are emitted, and when they are not

| | |
|---|---|
| `user.posted` | on `CreatePosting`, **only when the staff member has an identity link** |
| `user.posting_ended` | on `EndPosting`, **only when one was announced** |
| neither | a posting for a person with no login — most of the workforce |

**A posting for somebody with no account is a complete, correct posting that
announces nothing.** There is no principal for a tuple to grant anything to, and
writing one would be inventing an account. `masterdata.staff.user_id` is
nullable and the platform's own proto calls that nullability *"the whole
point"*.

## 3 · The tuples they must produce

```text
user.posted          WRITE   department:{department_id}#posted@user:{user_id}
user.posting_ended   DELETE  department:{department_id}#posted@user:{user_id}
```

That is the entire authorization consequence of this application's slice 1. The
relation already exists — `model.fga:419`, `define posted: [user]` — and
`model.fga:629` already reads it: `folder#viewer: reader or contributor or
posted from department or viewer from parent`. **Nothing new is being modelled;
what is missing is the writer.**

## 4 · Five invariants, and what makes each one hold

1. **No tuple without a principal.** Enforced by not announcing — §2.
2. **Both directions land, or neither does.** ADR 0087's addendum records what a
   one-directional writer produced: *"a posting revoked left its tuple standing,
   so somebody removed from a property stayed reachable there"* — the direction
   ADR 0061's invariant forbids. The two events ship together or the contract is
   not done.
3. **Ending a posting removes the tuple and keeps the row.** A rota worked last
   March was worked under that posting; the announcement withdraws the access,
   never the history.
4. **A person cannot hold two live tuples for one department.** Not by the
   Kernel de-duplicating, but because **slice 1 already refuses an overlapping
   open posting in the same department** — an application rule that makes an
   authorization invariant hold, and the reason a re-hire (Kitchen until March,
   Kitchen again from September) is two postings with one tuple at a time rather
   than a converging write nobody planned.
5. **The department is this property's row.** Departments are property-scoped,
   the payload carries `property_id`, and the tuple must be written against the
   department the posting names — never the one the *acting* scope implies.
   `plan.rs:120-129` already learned this for staff postings: *"the envelope
   carries the acting scope — an administrator at one hotel may grant a posting
   at another… reading the envelope here would write the tuple against the wrong
   hotel, and it would look right in every test where the two happen to match."*

## 5 · Where the Kernel's model has no case for this — CC's to decide

`grants.rs` states its own assumption plainly:

> *"the announcing service owns exactly one end, that end is the aggregate, and
> the other end is therefore in the body"*

and derives `grantee_is_aggregate` from the object kind rather than declaring
it, precisely so the two impossible combinations cannot be written. Every
existing kind fits:

```text
FOLDER_ACCESS        aggregate folder  → object is the aggregate, grantee in body
PROPERTY_MEMBER      aggregate user    → grantee is the aggregate, object in body
GENERAL_MANAGER      aggregate user    → same
APPLICATION_ACCESS   aggregate user    → same
```

**Workforce fits none of them.** The aggregate is a `posting`, and *both* ends —
the user and the department — are in the body. That is not an oversight in the
model; it is a shape that had not existed, because no service before this one
announced a relationship between two records it does not own. Jobs and Room Care
are next in the same position.

Three candidate shapes, and **the choice is the Kernel's**:

| | | |
|---|---|---|
| **(a)** | `GrantObject` gains a body-sourced variant, and the grantee likewise | Generalises `GrantKind` to *"a relationship announced by a third party"*. Smallest diff; costs the derived-invariant property, because `grantee_is_aggregate` stops being derivable |
| **(b)** | A distinct `RelationshipKind` beside `GrantKind` | Keeps the existing invariant intact and names the new thing as new. Costs a second table for `plan()` to consult |
| **(c)** | Announce against the `user` after all | Restores the two-ended shape — and is **refused**: it was the versioning problem `AUTHZ-Q20` already ruled on, and this application has no user row whose counter it may increment |

**(c) is refused with a reason rather than left on the list**; between (a) and
(b) this stream has no view worth overriding the Kernel's own.

## 6 · The late-arriving identity link — the case with a real mechanism

A posting exists for somebody with no login. Later they are given one. **Nothing
re-announces**, so the person works on with the department access their posting
implies and does not have.

**Proposed mechanism, and it needs nothing new from Master Data:**

```text
masterdata emits   staff.updated  { staff_id, changed_fields }
                                    ← StaffService.cs:143-145, already emitted
Workforce consumes it, and when changed_fields contains `user_id`:
   resolve the link  → GetStaff, the same call CreatePosting already makes
   for every OPEN posting of that staff member at that property:
       announce user.posted
```

**Three things make this cheap rather than a new subsystem:** the event already
exists and already carries what is needed to *decide* (though not the value —
see below); the resolution is a call this application already makes; and the
announcement is the one §2 already specifies, with no second shape.

**One dependency to confirm with Master Data**: `staff.updated`'s payload is
`{ staff_id, changed_fields }` and carries **no `user_id` value**, so the
consumer must call `GetStaff` to learn it. That is acceptable — it is one read
on a rare event — but it means the consumer is not self-contained, and if Master
Data would rather carry the value, that is its call and not this application's.

**Two neighbouring cases, both stated so neither is discovered later:**

* **the link is removed** — the person loses their account. The mirror of the
  above: announce `user.posting_ended` for every open posting. Same consumer,
  same trigger, opposite direction, and invariant 2 says it ships with the
  first.
* **the person leaves** — `staff.exited` already exists (`StaffService.cs:193`),
  and `Q25` ruled staff exit belongs to this application. Consuming it should
  **end their open postings**, which announces `user.posting_ended` through the
  ordinary path and needs no special authorization handling at all. Recorded
  here because it is the same consumer and would otherwise be designed twice.

## 7 · `department#manager` — the head flag

Chapter 01 §4 says a department-head posting writes `department#manager`, and
ADR 0114 §5 records `general_manager` and `department#manager` as **Workforce-era
hooks**. The flag is stored today (`Posting.IsDepartmentHead`) and read by the
approver resolution; nothing reaches the graph.

**It is the same missing shape with a different relation on the same object**,
so it should be settled by whichever of §5's candidates is chosen rather than
designed separately. What differs is only when it fires:

```text
posted    on create, and on end
manager   on create, on end, AND on the amendment that sets or clears
          IsDepartmentHead — because headship changes without the posting
          starting or finishing
```

That third trigger is the one worth noticing: `UpdatePosting` is currently a
pure state change with no announcement, and giving it one is a larger change
than adding a grant kind.

## 8 · What this half commits to, and what it does not

**Commits:** the two events, their payloads, their emission rules, and the five
invariants — all of §2–§4, which are this application's to state.

**Does not:** the grant kind's shape, the `GrantObject` variant, whether a
relationship announcement is a `GrantKind` or a new kind, and the order the two
streams land in. Those are the Kernel's, and this page exists so that
conversation starts from something concrete rather than from the problem
statement.

**Not built until the contract is agreed.** `PostingService.CreateAsync` carries
the seam and the reason at the call site; nothing is published, because a
published event that nothing acts on is indistinguishable from a working one.

---

# 9 · CC's half, accepted — and what it costs this side

**2026-09-01, Stream GG**, answering
`HosPilotOS/docs/working/47a-the-kernel-half-of-authz-q20.md`. Both decisions
left to this stream are **accepted**. What follows is what this half verified,
what it was wrong about, and the two events the acceptance costs.

## 9.1 · §2's defect — verified here, not taken on trust

CC's finding is that the subscription would never hear these events. It is the
one thing in either half that would have shipped silently, so this stream read
the three lines rather than accepting the conclusion:

```text
events/registration/mod.rs:393   format!("property.*.{}.{}.>",
                                     kind.aggregate_type, kind.granted)
events/subjects.rs:28            publish_subject(property_id, event_type, version)
```

The filter is built from **`aggregate_type`**; the subject is built from
**`event_type`**. For every existing kind those are the same word, so nothing has
ever separated them. For `user.posted` on aggregate `posting` they are not:

```text
published to   property.{pid}.user.posted.v1
subscribed to  property.*.posting.posted.>     ← nothing publishes this
```

**Confirmed.** The `domain` / `aggregate_type` split is accepted as required
regardless of §3, and this half asks that it land with CC's sentence attached
rather than as a quiet fix — §2 is the reason the field exists, and a future
author reading a bare two-field struct would merge them back.

It is also the answer to something this half wrote and could not have tested:
chapter §8 says *"a published event that nothing acts on is indistinguishable
from a working one"*, and that is exactly what the application would have
shipped. Naming the risk did not prevent it; reading the subscriber did.

## 9.2 · This half's (a) was wrong, and the correction is accepted

§5 offered *"`GrantObject` gains a body-sourced variant, and the grantee
likewise"*. CC is right that the second clause was doing load-bearing work this
stream had not examined. Verified here:

```rust
grants.rs:108   !matches!(self.object, GrantObject::Aggregate)   // grantee_is_aggregate
grants.rs:277   (format!("user:{}", ends.aggregate), …)          // the branch it takes
```

Adding `GrantObject::Department` and stopping makes the derivation answer
**true**, and the tuple becomes `department:{id}#posted@user:{posting_id}` — a
posting id in the user slot. It type-checks, OpenFGA accepts it, and the access
lands on a principal that does not exist while the person who should hold it does
not. **Nothing fails.**

That is worse than the shape it was meant to fix, and this half proposed it
while explicitly noting the derivation's purpose two paragraphs earlier. The
lesson is the one this round has hit before: *a derived invariant cannot be
extended by adding a case to what it derives from.*

**(c′) is accepted** — the pairing as one field, three variants. It keeps the
guarantee the derivation existed for, in a place that still fails at compile
time; it needs no second table, which is what would have made §2's two lists
drift; and its largest cost — the exhaustive test becoming a compile error — is
the guard that would have caught (a).

## 9.3 · §5 accepted: headship is a second kind, and here are its events

The cost is this half's and is accepted: **two more events, and an announcement
on `UpdatePosting`, which today announces nothing.**

```text
user.headship_started    domain user · aggregate posting · relation manager
user.headship_ended      domain user · aggregate posting
```

**Named to mirror the pair they join**, not the graph they end up in.
`headship_granted` / `_revoked` was the obvious alternative and is refused: that
is authorization vocabulary, and ADR 0006 routes by what an event *means* — the
fact is that somebody now heads a department, which is an organizational fact
that happens to have an authorization consequence. `posting_ended` and
`headship_ended` also read as the pair they are.

The payload is §2's, plus nothing: `user_id`, `department_id`,
`department_code`, `posting_id`, `property_id`, `occurred_at`. Headship needs no
field `posted` does not already carry.

### The triggers, complete

```text
CreatePosting  with IsDepartmentHead   →  user.posted AND user.headship_started
EndPosting     of a head               →  user.posting_ended AND user.headship_ended
UpdatePosting  setting the flag        →  user.headship_started
UpdatePosting  clearing the flag       →  user.headship_ended
```

Two events from one operation in the first two rows, appended in one
transaction like any other pair. And the same rule as `posted` governs both: **no
announcement for a person with no identity link**, because there is no principal
for a tuple to name.

**There is no fifth trigger, and this half checked rather than assuming.**
`UpdatePostingCommand` carries no `DepartmentCode` — a posting cannot change
department. Moving somebody is ending one posting and creating another, which
announces through the existing pairs and needs no case of its own.

### A consequence CC could not have seen, which makes the shape safe

Slice 4b — landed since chapter 04 was written — added an invariant this
contract now depends on:

> **A department has one current head, or none.** Refused in `CreatePosting` and
> in the amendment that sets the flag; handing headship over is two deliberate
> acts.

Without it, `headship_started` for a department could arrive while another live
head's tuple stood, and `department#manager` would hold two subjects with
nothing to say which was current. It was found by a failing test asking the
right question of a model that could not answer it, and it turns out to be what
makes the second grant kind well-defined rather than merely permitted by
`model.fga:444`.

## 9.4 · §4 agreed, including the part that is neither stream's

**Reconciliation stays in the application** — agreed, and CC's reason is
better than this half's: not merely that the Kernel *should not* originate a
grant, but that it *cannot*, because *open posting* is a Workforce concept it
has no way to enumerate. §6's mechanism needs nothing from the Kernel side and
is unchanged.

**The rebuild gap is real and is above both of us.** Workforce is the first
installed application to own tuples; `rebuild_sources()` enumerates three
platform services with certificates; and — the part worth the register row —
a rebuild would report `done` having restored every room and folder and **not one
department posting**, with no empty row to notice, because a source that does not
exist produces none.

This half adds one fact from its side: the contract above makes that gap
certain rather than hypothetical, and **Jobs and Room Care reach it next**. The
per-service shape would be wrong on arrival. Neither stream takes it.

## 9.5 · Where this leaves the contract

**Agreed between the streams:** the `domain` / `aggregate_type` split; `Ends`
as the pairing field; both ends in the body for Workforce; reconciliation in the
application; headship as a second kind with the two events named above; four
triggers and no fifth.

**Still open, and named rather than assumed:** the rebuild-source shape, which
needs a number and a ruling.

**Nothing is built on either side.** This half's seam is where it was — the
call site carries the reason, and nothing is published.

# What actually crosses the boundary — connector-side evidence for `CONN-Q52`

**This is evidence, not a design.** `CONN-Q52` defines the Normalize / Validate
/ Join capability contract, and that definition is not this package's to write:
*"a protocol defined by its only implementer gets shaped to that
implementation"*, which is the planner's refusal of `pms-oracle` minting kinds
(*"that would make the connector package silently define the platform
protocol"*) one level up. What follows is measured from the only real connector
so that the definition can be checked against something.

Every row is marked, and the marks mean different things:

```text
EVIDENCE      measured in this repository at the cited line. A fact about what
              pms-oracle does today. It does not say the contract should keep it
REQUIREMENT   something OHIP or the platform imposes, which any contract must
              satisfy however it is shaped
ARTEFACT      behaviour that exists because the stages ran IN-PROCESS. It looks
              exactly like a requirement to whoever is writing the contract, and
              is the reason this document exists
UNDETERMINED  I could not measure it, stated rather than inferred
```

Measured 2026-09-25 at `ad20cc5`. Nothing below is a proposal.

---

## 1 · The payload kinds, and one of them has two spellings

**EVIDENCE** — four kinds are declared, all on the adapter:
`ohip-reservation` (`OracleCloudAdapter.cs:86`), `ohip-housekeeping-room`
(`:89`), `ohip-guarantee` (`:98`), `ohip-business-event` (`:107`).

**EVIDENCE** — `ohip-business-event` is declared a **second time**, at
`OhipBusinessEventQueue.cs:57`, as `EventPayload`. Two constants, one value, two
files. The drain path uses the second; the adapter's stages use the first.

**ARTEFACT** — and the two spellings currently describe **different shapes**.
`BusinessEventNotification` (`BusinessEventNotification.cs:36-42`) is flat —
`EventId`, `ModuleName`, `PrimaryKey` — and the drain produces OHIP's nested
item, `businessEventId.id` / `businessEvent.header.*`. Nothing constructs the
flat record (`OracleCloudAdapter.cs:293` is its only reference) and `IOhipQueue`
has no implementation, so that shape has never existed at runtime. A contract
that took the kind vocabulary from the adapter would inherit a payload shape
the source does not send.

> **For the definition:** the kind vocabulary needs exactly one home. Which
> home is not mine to choose.

## 2 · The output contract

**EVIDENCE** — `NormalisationOutcome` (`NormalisationOutcome.cs:68-112`) is a
closed union of four:

```text
StayNormalised(RoomStayFact)        a fact
RoomStateNormalised(RoomStateFact)  a fact
AwaitingJoin(OnSiteMessagePart, OnSiteJoinKey)   not a fact yet
Rejected(RejectionReason, Field, RawValue)        refused, with the field named
```

**EVIDENCE** — `RejectionReason` has five members (`:6-47`):
`MissingRequiredField`, `UnreadableValue`, `UnknownStatus`, `PropertyMismatch`,
`IntegrationNotConfigured`.

**REQUIREMENT** — a rejection names the **field and the raw value**, in the
vendor's own spelling. An operator reading a rejection has to see what OHIP
actually sent; renaming it at this end makes the message quote a spelling nobody
sent.

**ARTEFACT** — `AwaitingJoin` is a stage-machine state, not an outcome. It means
*this process is holding a part and waiting for its sibling*, which is only
expressible where one process holds both.

## 3 · The `IntegrationSettings` dependency, per normaliser

**REQUIREMENT** — all four normalisers take `IntegrationSettings`
(`CloudNormaliser.cs:40`, `OnSiteNormaliser.cs:49`,
`CloudRoomStateNormaliser.cs:32`, `RoomStateNormaliser.cs:36`), whose seven
fields are `IntegrationId, PropertyId, PropertyCode, Clock, Currency,
AmountTaxBasis, GuaranteeMaximumFreshness` (`IntegrationSettings.cs:75-82`).

**EVIDENCE** — what each one actually reads is **not** the whole record:

| normaliser | reads |
|---|---|
| `CloudNormaliser` | `AmountTaxBasis` · `Clock` · `Currency` · `IntegrationId` · `PropertyId` |
| `OnSiteNormaliser` | the above, plus `PropertyCode` |
| `RoomStateNormaliser` | `Clock` · `PropertyCode` · `IntegrationId` · `PropertyId` |
| `CloudRoomStateNormaliser` | **`IntegrationId` and `PropertyId` only** — `CloudRoomStateNormaliser.cs:55,164` |

**EVIDENCE** — so *"Normalize is blocked on ADR 0220"* is too coarse. Cloud
room-state normalisation reads no property fact at all: its entire settings
dependency is two identifiers the Hub already holds, because the Hub dispatched
to that instance. Bookings normalisation does need the clock, currency and tax
basis, and that arm is blocked.

**EVIDENCE** — `GuaranteeMaximumFreshness` has **no production reader**. Every
reference outside its own declaration is a test passing `null`
(`AdapterTests.cs:178`, `CloudNormaliserTests.cs:28`, and four more). The
`SourceFreshness` enum in `Capabilities/` is a different thing.

> **For the definition:** a field on this record is not evidence that the
> contract must carry it. One of the seven is read by nobody.

**REQUIREMENT** — `PropertyCode` is load-bearing on the push path: a pushed
record claiming a different hotel is refused as `PropertyMismatch`
(`OnSiteNormaliser.cs:56-62`, `RoomStateNormaliser.cs:43`). Whatever carries
context must let the connector refuse a push for somebody else's property.

## 4 · Join — who supplies the inputs

**EVIDENCE** — the seam answers the register's third question directly
(`IJoiningConnector.cs:31-59`):

```text
TimeSpan JoinWindow                          connector states how long to hold
JoinCandidate? JoinFor(payload, kind)        connector says: part P of key K
NormalisedPayload NormaliseJoined(parts)     connector normalises the assembled set
JoinedPart(Part, Payload, PayloadKind)       what comes back
```

**EVIDENCE** — so the connector decides *what a payload is a part of*, and the
**Hub accumulates**. That division survives the seam being retired, because the
accumulation belongs where the inbox is.

**UNDETERMINED** — whether the Hub currently honours `JoinWindow`, and what it
does when the window expires with a part missing. `JoinStage.cs` is the Hub's
and I have not traced it.

## 5 · Validate has almost no connector-specific content

**EVIDENCE** — `OracleCloudAdapter.Validate` (`:247-266`) does two things: it
refuses a kind not in its own declared set, and it refuses a body that is not
parseable JSON. Nothing else. `OracleOnSiteAdapter`'s is the same shape.

**ARTEFACT** — both checks are things the Hub can do without asking a connector:
the kind came from the connector's own manifest, and JSON parses or it does not.

> **For the definition:** *"what does Validate validate, and what is the returned
> contract?"* cannot be answered by generalising from this connector, because
> this connector's answer is almost empty. The semantics have to come from what
> the Hub needs, and that is not mine to state.

## 6 · Artefacts of the retired seam, collected

The list the contract is most at risk from, because each one reads as a
requirement:

1. **ARTEFACT — the adapter is constructed with settings, once.**
   `OracleCloudAdapter(settings, queue, guarantees, http)` (`:78-83`). Across a
   process boundary settings arrive **per invocation** — `DrainInvocation`
   already carries a settings map. The *record* shape exists because one
   instance served many payloads.
2. **ARTEFACT — identity is read from the adapter.**
   `IConnectorAdapter.IntegrationId`, and `CloudRoomStateNormaliser` stamps
   `IntegrationId`/`PropertyId` onto the fact it builds (`:55,164`). The Hub
   knows both: it dispatched to that instance, which is exactly why
   `DrainedPayload` carries no property.
3. **ARTEFACT — `AwaitingJoin` as an outcome** (§2).
4. **ARTEFACT — `PipelineResult.Defer`** for a guarantee
   (`OracleCloudAdapter.cs:329`): *"a guarantee policy is a Hub-held source
   fact, applied at enrichment"*. `Defer` is a verb of a pipeline the connector
   is standing inside.
5. **ARTEFACT — the flat notification shape** (§1).

**EVIDENCE, and the precedent worth knowing** — one member of this class has
already been settled, and it moved rather than being defined: `DedupeKey` was
`IConnectorAdapter.DedupeKey(payload, kind)`, a callback the Hub made against a
stored record. ADR 0246 made it **a field on `DrainedPayload`**, filled by the
connector at the moment it produces the payload (`QueueDrainInvocation.cs`,
`OhipBusinessEventQueue.KeyOf`).

> That is a fact about what has already happened. Whether the same move suits
> `JoinFor` or `Validate` is a design question, and I am deliberately not
> answering it — it is the exact judgement that would bend the contract toward
> this connector's shape.

## 7 · What I could not determine

* **UNDETERMINED** — what the Hub does with a `Rejected` outcome today, and
  whether the five reasons survive as a wire vocabulary. Hub-side.
* **UNDETERMINED** — whether `JoinWindow` is honoured (§4).
* **UNDETERMINED** — whether any of this is reachable at all: `ConnectorHost`
  is composed with **zero** adapters in production
  (`SchedulerHealthCheck.cs:32`, *"production composes none"*), so none of the
  behaviour above has run against a real payload. It is measured from source,
  not from a running system, and that limit applies to every row.

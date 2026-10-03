# pms-oracle — the capability ledger

**Derived from the code, not from memory and not from a walk.** Platform repository
at `658c3fb3`, this repository at `e86b0fa`. **Every section is true at the commits
named in it**; a single "derived at" line over a document edited for a week is a
claim nobody can check.

**A connector is not an application, and this ledger is not shaped like one.**
Jobs' and GuestOps' ledgers are *screens and the controls on them*, because an
application draws screens. This package draws none: `CONN-Q9` ruled (b) — it
ships a signed `ui.module` that Software Center hosts inside Integration
Management — and everything else it does happens between a vendor and the
Integration Hub. So sections 2 and 4 of the house pattern have no subject here,
and what replaces them is **declaration → delivery → enforcement**, measured at
each layer separately because they are three different populations.

---

## 1 · What this package declares

**Ten keys per integration, read from `manifest.yaml:314-377`, and the Kernel's
parser accepts exactly these ten with `deny_unknown_fields`**
(`packages/hotelos-package/src/manifest/integrations.rs`,
`IntegrationDeclaration`).

| | `oracle-cloud` | `oracle-onpremise` | `oracle-web` |
|---|---|---|---|
| `name` | Oracle OPERA Cloud | Oracle OPERA on-premise | Oracle OPERA web |
| `capabilities` | `bookings` `room_state` | `bookings` `room_state` | `bookings` `room_state` |
| `delivery` | `polling` | `push` | `push` |
| `implements` | `test` `drain` `dedupe_key` | `dedupe_key` `join` | `dedupe_key` `join` |
| `payload_kinds` | 4 `ohip-*` | `onsite-stay` `onsite-room-status` | `onsite-stay` `onsite-room-status` |
| `prerequisites` | `amountTaxBasis` | `amountTaxBasis` `hotelCode` | `amountTaxBasis` `hotelCode` |
| `accepts_push` | `false` | `true` | `true` |
| `ingress_authentication` | — | `shared-secret` | `shared-secret` |
| `required_secrets` | 3 | — | — |

**Three integrations, one vendor — `CONN-Q2(a)`**: a PMS brand is not the
connector unit, and R28 is why the id is `oracle-cloud` and never `oracle`.

### `capabilities` and `implements` are two different words and neither is the other

**This is the single most confusable thing in the connector domain and the
ledger states it before anything else.**

```text
capabilities    WHAT CLASS OF FACTS this integration supplies
                bookings · room_state — ADR 0355, observable at the Hub's
                capability boundary. CapabilityOf returns one of these two,
                for every connector, ever.

implements      WHICH INVOCABLE CAPABILITY this integration serves
                dedupe_key · validate · join · normalize — ADR 0255's four
                connector-owned capabilities, plus test and drain
```

**ADR 0355 ruled on the first and is why `check_ins` is not in the table above**:
a capability must correspond to something observable at its own boundary, and
*"downstream derivability does not make another capability declaration
legitimate."* `check_ins` was declared by nothing, ever, and `42d:294` asserted
otherwise.

**A ledger that collapsed these two would report six capabilities where there are
six (integration, fact-class) pairs over two names and seven invocable
declarations over four kinds.** They are counted separately below.

### Both declared fact-classes have built normalisers

```text
bookings     built, traced end to end — ADR 0355 §2
             OHIP "InHouse" → StayLifecycle.CheckedIn → stay.arrived
room_state   CloudRoomStateNormaliser, with tests
```

**So neither is declared-and-never-delivered**, and ADR 0355 §5's grey state
*"stays defined in the legend with no integration in it"* — a drawn grey row would
be a measurement nobody took.

---

## 2 · What this package requests, and where each is ENFORCED

**Keyed on the authorization call across both repositories, never on a door.**
A capability's reachability is established where it is *enforced*, not where it
is *exposed* — and a search by surface finds only the surfaces somebody thought
of.

**Four permissions, requested with reasons** — `manifest.yaml:150-166`, ADR 0092
rule 2.

| Requested | Enforced at | Object | Layer |
|---|---|---|---|
| `room.read` | `masterdata RoomService.cs:233` | **Room**, `roomId` | `Application/` |
| | `:235`, `:335` | Property | `Application/` |
| `external_mapping.read` | `masterdata ExternalMappingService.cs:160`, `:203` | Property | `Application/` |
| `integration.read` | the Hub's `AuthorizedAsync`, **7 call sites** | Property | `Grpc/` |
| `integration.configure` | the same helper, **4 call sites** | Property | `Grpc/` |

**`room.read` is enforced against two different objects** — the room itself at
`:233` and the property at `:235`/`:335`. That distinction is not cosmetic: moving
an object-scoped permission to property scope turns *may this caller read THIS
room* into *may it read rooms at this property*, which is a privilege expansion
wearing a repair's clothes.

**The two services enforce at different layers, and only keying on the call
finds both.** Master Data authorizes in `Application/`; the Hub authorizes in
`Grpc/`, through one shared helper whose own remark says why — *"here rather than
in each partial, because every RPC on this surface does exactly this and a copy
per subject is four chances to forget the authorization call."* A check that
walked `Grpc/` looking for permission names would have found the Hub's and
reported Master Data's as unreachable; one that walked `Application/` would have
done the reverse.

### This package enforces nothing, and that is correct

**`RequireAsync` appears zero times in `pms-oracle/`** — measured with a control
(the same search returns hits across GuestOps, so the search ran). A connector
holds no Kernel principal (ADR 0019) and is the **subject** of authorization,
never its enforcer. Its identity is the Supervisor-stamped `app_id` and the Hub's
session-bound `integration_id`, and ADR 0208 is explicit that **the connector
supplies neither**.

### Two things the surface enforces that this package does not request

```text
quarantine.read   the Hub's Quarantine partial, 2 sites — raw provider bytes
                  are not configuration, so they are not integration.read
external_mapping.update   NOT REQUESTED, deliberately, with the reason in the
                  manifest: "A connector able to write mappings could resolve
                  its own unmappable records silently, which is the whole point
                  of holding them where a person decides."
```

**A documented refusal is worth more than its absence.** A reader meeting three
mapping permissions and no `update` cannot tell a considered omission from an
oversight, and the next author's instinct is to add it back.

---

## 3 · Declared → delivered → read, measured at each layer

**The three layers are measured separately because a declaration can exist, be
carried, and be read by nothing — and nothing about the first two says so.**

| | declared | carried to the Hub | read |
|---|---|---|---|
| `capabilities` | ✓ | `DeclaredIntegration.Capabilities` | health · registry · persistence |
| `implements` | ✓ | `.Implements` | **`ConnectorDispatcher.cs:82`** refuses before dispatch; `ConnectorJoining:78`; `ConnectorNormalising:80` |
| `payload_kinds` | ✓ | `.PayloadKinds` | `ConnectorDeclarations:151`; persisted `text[]` required; reconciled |
| `prerequisites` | ✓ | `.Prerequisites` | `ConnectorJoining:90` · `ConnectorKeying:68` · `ConnectorNormalising:92` · `IntegrationHealthService:163` |

**All four are read.** `DeclaredIntegration` is a nine-field record
(`Domain/DeclaredIntegration.cs:80-90`) and carries every declaration except the
lifecycle — which is excluded by construction, because an operator climbs that
ladder and an update must not reset it.

### ⚠ This closes two of design page 75's seven open questions

**Page 75 — `75-the-connector-capability-contract.md`, LL, 2026-09-25 — ends
*"Nothing in this page is in code."* That sentence has expired for the
declaration half.**

```text
§7 Q1  "Nothing lets the Hub know which kinds a connector serves before
        sending one"
        arm (a): "each integration's declaration lists the kinds it serves,
        as delivery was added for polling"
   →    BUILT. `implements` is declared, verified, carried, and read at
        ConnectorDispatcher.cs:82 — the Hub refusing before dispatch, which
        is precisely what the question says nothing can do.

§7 Q7  "The kind vocabulary's one home" — manifest-declared per integration,
        or connector-code constants only?
   →    ANSWERED THE SAME WAY. `payload_kinds` is per-integration manifest
        data, Kernel-verified, persisted as a required column and reconciled
        against the signed manifest on every update.
        Page 75 predicted this: "Q1 (a) nearly forces Q7's answer."
```

**Not claimed: arm (b).** Q1 also asks for a fault *code* —
`UNSUPPORTED_KIND` versus `FAILED` — so an unserved kind and a broken connector
do not share one answer. `ConnectorDispatcher.cs:87` composes a *sentence*;
whether a code exists is **not established by this measurement**, and page 75
says *"both may be needed."*

**This is a design page's premise expiring, which is the same class as a register
row's.** Page 75 is a definition offered for review and reads as current; a
stream briefed from its §7 today would be told to design two answers that are
built. *The remedy belongs in the page, where the sentence is.*

---

## 4 · The invocable population, and why it is not in this ledger yet

**ADR 0255 rules four connector-owned capabilities — `DedupeKey · Validate ·
Join · Normalize` — with Receive staying the Hub's, and seven things required per
capability: *"a contract missing any of them is not the contract."***

```text
declared by this package   7 declarations over 4 kinds
                           test · drain · dedupe_key · join
the seven-part contract    page 75 defines all four; §7 holds the open points
built                      NOT in the platform's protocol — page 75's own
                           closing sentence, true for the message half
```

**So there is no authorization call to key this half of the ledger on, because
the invocations do not exist.** That is a gap **by construction** — five open
points remain in page 75 §7 after Q1 and Q7 above, plus ADR 0220's tax basis,
which blocks Normalize's `bookings` arm specifically and not its `room_state`
arm.

---

## 5 · Gaps, classified three ways

**By construction** — no machine here can reach it:

```text
Part A capture      CONN-Q20. HotelOsApps/scripts/ holds four files and
                    mounts no module against a host port, so no package's UI
                    can be photographed by anyone. Blocks every package in
                    this repository, present and future — not Oracle's problem.
hosted publication  ADR 0141. Enforced at VibeMind's publishing path, a
                    different repository. pms-oracle "may exist, build and be
                    tested, and stays unpublished" — and publication is NOT
                    required for a local install.
the invocable four  §4 above.
```

**By choice** — deliberately not done, with the reason recorded:

```text
external_mapping.update   not requested; the reason is in the manifest
check_ins                 not declared; ADR 0355 ruled it out of the
                          vocabulary, and nothing ever declared it
```

**By omission** — nobody looked, and nothing says so: **none found in this
package.** Three things sit *outside* it that this ledger depends on:

```text
ARCH-Q60's desktop half   check_ins still live in four desktop files in the
                          platform repository — DD's half of ADR 0355 §5a.
                          BB's half is clean, measured 0 against a control
                          of 128.
page 75 §7                five open points, plus ADR 0220
Part C for a connector    ADR 0358 names only applications and postdates
                          CONN-Q15's closure by three weeks. Unruled, and
                          with the planner.
```

---

## 6 · What this ledger does not say

**It does not say any control works.** This package has no controls of its own;
its configuration form is hosted by Software Center, and Chapter 18A places the
Integration Manager there — *"Software Center includes a dedicated integration
management interface."*

**It does not say a capability has been observed arriving on a property.** Both
fact-classes have built normalisers and the dispatcher reads `implements`; whether
any of it has run against a real OHIP tenant is Part B's question.

### Part B's form is RULED — ADR 0369

**This section said Part B's form for a package with no screens was "with the
planner". It is ruled**, and the sentence is corrected rather than deleted so a
reader can see when the answer arrived.

> **An installable connector owes the platform-served portion of Part B, but
> *reachable by a person* is structurally N/A unless that connector actually
> exposes a human-operated product surface.**

```text
Platform-served         PASS — exercised against the installed .hopkg,
                        through the real Hub / session / runtime path
Reachable by a person   N/A  — connector exposes no human-operated surface
```

**Two prohibitions the ruling names, and both are recorded here because this is
the file a later author will edit:**

* **Never write "Connector Part B = N/A."** That recreates `CONN-Q15`'s stale
  collapse and discards the served proof ADR 0152 holds to be independently
  meaningful.
* **Do not manufacture a person path.** *"An operator can click Sync in
  Integration Hub"* is a **Hub** capability, and its human reachability belongs
  to the Hub's surface. It cannot be borrowed to make this column green.

**And the classification follows the ARTIFACT, not the kind** — a future
connector that ships a human surface loses the N/A. This package ships a
`ui.module` that Software Center hosts (`CONN-Q9` (b)), so whether that counts as
a human-operated *product* surface is the question a reader must not answer by
assumption: it is a configuration form inside another application's module, not
this connector operating a property.

**Part C stays unruled — `CONN-Q92`, open**, and both obvious fillings were
rejected: N/A, and *invoke its Hub API without UI*. Part A applies where the UI
artifact exists, and ADR 0054's test split is unchanged.

**And it does not say the permissions are granted.** The manifest requests; the
administrator approves; the Kernel decides per user. The manifest's own words:
*"They are the BOUND, not the grant… who may actually do it is not this file's to
say."*

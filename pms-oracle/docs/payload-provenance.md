# pms-oracle — payload provenance

**Every payload concept this connector carries, cited at `file:line` in the Java
reference.** Owner rule, 2026-09-30: *the reference project is the source of
CONCEPT, LOGIC and FLOW for our applications and connectors; IMPLEMENTATION and
ARCHITECTURE are ours.* **Uncited reads as invented**, and an incomplete
reference is a finding rather than a licence.

Read at HotelOsApps `9451845f`, platform `658c3fb3`.

## ⚠ Which tree these lines are in

The reference exists at **two paths with identical content** — 605 `.java` each,
verified by comparing relative-path-and-size listings, which matched exactly:

```text
C:\Users\Mahin Aboobakker\Documents\HotelOs-References\
    pms-integrations\pms-integrations\        ← EVERY LINE BELOW IS IN THIS ONE
    reference\IdeaProjects\IdeaProjects\pms-integrations\
```

**A `file:line` that resolves in two trees needs its tree named.** All paths
below are relative to
`pms-integrations/pms-integrations/src/main/java/co/instio/integrations/providers/oracle/`.

**The Oracle reference is 87 files** — `cloud/` 41, `onPremise/` 20, `web/` 26 —
mapping onto this package's three integrations.

---

## 1 · `ohip-business-event` — oracle-cloud

**The wire** — `cloud/models/BusinessEventResponse.java`:

```text
:12  businessEventData : List<BusinessEventData>
:17    businessEvent        :19  businessEventId
:24      header
:29        moduleName      String   which OPERA module raised it
:31        actionType      String   what happened
:33        actionId        int
:35        primaryKey      String   the subject's key in that module
:37        publisherId     int
:39        createdDateTime String   the instant, as a string
:41        hotelId         String
:48      BusinessEventId.id String
```

**The concept is the triple** `moduleName` + `actionType` + `primaryKey` — *which
module, what happened, to which key* — and that is what this payload kind
carries.

**The stored shape** — `cloud/dto/mongo/OracleEvent.java`:

```text
:20 companyId  :22 siteId  :24 eventId  :26 primaryKey  :28 moduleName
:30 actionType :32 createdDateTime  :34 hotelId  :36 processed
```

**`:36 processed : boolean` is the reference's idempotence mechanism** — a flag on
the stored record. **Ours is a connector-produced stable key crossing with the
payload** (ADR 0255 §2, ADR 0246). *Concept — do not process one change twice —
from the reference; mechanism ours, and ADR 0255 is explicit that an empty key is
a protocol failure and never a signal for the Hub to manufacture one.*

**`companyId` and `siteId` appear on the stored document and not on the wire**
(`:20`, `:22` against `BusinessEventResponse`, which has neither). The reference
adds its own tenancy at storage. **We stamp `property_id` at the Hub and ignore
anything a connector wrote there** — ADR 0128 §6, page 75 §1. Same concept,
opposite owner.

---

## 2 · `ohip-reservation` — oracle-cloud

`cloud/dto/mongo/Reservation.java`, **251 lines; the first 42 non-blank are cited
here and the truncation is mine, not the file's**:

```text
:19  processed            the same idempotence flag
:21  reservationId
:23  companyId  :25 siteId  :27 instioPropertyId  :29 hotelId
:31  reservationIdList : List<ReservationIdList>
       :56 id   :58 type        ← SEVERAL IDS, BY TYPE
:33  roomStay : RoomStay
:41  reservationStatus              parsed by §6's two overloads
:43  createDateTime  :47 lastModifyDateTime  :45/:49 creator/modifier
:51  createBusinessDate             ← THE BUSINESS DATE
RoomStay
:69  arrivalDate  :71 departureDate
:73  expectedTimes                  ← R13's expected times
:75  total
:79  roomType     :81 roomId
```

**Three concepts with a line each, and all three are load-bearing here:**

* **`:31` `reservationIdList` of `{id, type}`** is the external-mapping concept —
  one reservation known by several identifiers, each with a kind. It is why this
  package requests `external_mapping.read` and why it never requests
  `external_mapping.update`.
* **`:33` `roomStay`** is `GUEST-Q2`'s subject: *checking in happens to a
  room-stay, never to a reservation.* The reference nests the stay inside the
  reservation; our model makes the stay the thing that arrives and departs.
* **`:51` `createBusinessDate`** is the business date — `CONN-Q6`, closed by the
  planner 2026-08-31 at page 44 shape (a). The reference carries it on the
  reservation record.

---

## 3 · `ohip-housekeeping-room` — oracle-cloud

`cloud/models/HousekeepingRoomInfo.java`, 71 lines. A four-level envelope with
pagination at `:14-22` (`totalPages · offset · limit · hasMore · totalResults`),
and at its leaf:

```text
HouseKeepingRoomStatus
:50  reservationStatusList : List<String>
:52  frontOfficeStatus     : String
:54  housekeepingRoomStatus: String
:56  housekeepingStatus    : String
```

**Those four fields are R1's four independent axes.**
`backend/Integrations/OnSite/OnSiteRoomStatusPush.cs` says *"Four of its fields
are the four independent axes of R1"* — and this is where they come from.

Also cited: `:63 pseudoRoom`, `:65 roomClass`, `:67 roomType` on `RoomType`, and
`:38 roomId`, `:36 smokingPreference` on `Room`.

---

## 4 · `onsite-stay` — oracle-onpremise AND oracle-web

**Two near-identical reference classes**, field for field, same PascalCase wire
names, offset by one line:

```text
onPremise/models/OracleOnPremiseReservationCreateRequest.java   77 lines
web/models/OracleWebReservationCreateRequest.java               76 lines

ReservationId :15/:14   Surname :18/:17   FirstName :21/:20
ArrivalDate :24/:23     DepartureDate :27/:26   NoOfRooms :30/:29
PaxAdults :33/:32       PaxKids :36/:35   Phone1 :39/:38  Phone2 :42/:41
Email :45/:44           Status :48/:47    Source :51/:50
TravelAgent :54/:53     RoomNo :57/:56    RoomType :60/:59
MealPlan :63/:62        PropertyCode :66/:65   MarketCode :69/:68
```

**`PropertyCode` is the provenance for the `hotelCode` prerequisite** — the
manifest declares it for both push integrations, and `OnSiteNormaliser:74` and
`RoomStateNormaliser:44` compare the pushed code against it.

**The reference duplicated the class; we share one normaliser.** That is the
concept/architecture split working in our favour by one class — and it is why
`manifest.yaml` declares the prerequisites per integration rather than
inheriting them: *sharing a normaliser is not sharing a configuration.*

---

## 5 · `onsite-room-status` — web only, and the on-premise half is sourced NOWHERE

**Measured on the domain's own vocabulary** — `roomStatus · housekeeping ·
roomState · hkStatus · frontOffice`, case-insensitive, rather than on the
question's words:

```text
oracle/web          12 files.  OracleWebRoomServiceImpl 60 hits, plus a room
                    RESOURCE, a room SERVICE, OracleWebRoomStatusChangeRequest,
                    RoomStatusChangeRequestForHK, OracleWebRoomStatusInfo
                    and its repository
oracle/cloud         6 files.  OracleCloudHousekeepingServiceImpl 81 hits
oracle/onPremise     7 files — AND EVERY ONE IS INSIDE RESERVATION CODE.
                    No room resource, no room service, no room-status request,
                    no room-status store.
common/              ZERO
modules/housekeeping/  the reference's own platform-neutral half
```

**And our own code says the same thing in one sentence.**
`OnSiteRoomStatusPush.cs`: *"Sent by the web flavour whenever a room's state
changes."*

```text
manifest.yaml:350  oracle-onpremise  payload_kinds: [onsite-stay, onsite-room-status]
manifest.yaml:367  oracle-web        payload_kinds: [onsite-stay, onsite-room-status]
```

**Three independent sources converge and the declaration is wider than all of
them.** This is reported, not ruled, and the measurement is why:
`payload_kinds` is a **permit list** (page 75 §3 — *"a kind the connector's
declaration does not list is never sent to it"*), so a declared-but-never-sent
kind **widens a permit** rather than breaking a flow; both on-site integrations
share `RoomStateNormaliser`, so a payload pushed there would normalise; and
**ADR 0355 binds `capabilities`, not `payload_kinds`.**

> **Either the on-premise agent does push room status — in which case the concept
> is uncited and the reference cannot supply it — or the declaration should not
> carry it.** Both arms drawn; neither is a stream's to pick.

---

## 6 · The status vocabularies, and ADR 0355's citation verified

**ADR 0355 cites `cloud/services/OracleCloudBaseService.java:96,98,111,113`.
Verified four for four at the line.** And the file holds more than the ADR
quoted — **two overloads of one method name, over disjoint vocabularies:**

```java
:90   parseInstioReservationStatus(String)                 camelCase ACTIONS
        Cancelled→"cancelled"  Reserved→"booking"  InHouse→"checkIn"
        CheckedOut→"checkOut"  NoShow→"noShow"     default→null      :102-103

:107  parseInstioReservationStatus(List<String>)           SNAKE STATES
        reduce((first, second) -> second)                  LAST WINS
        Departed→"DEPARTED"  Arrived→"ARRIVED"  StayOver→"STAY_OVER"
        NotReserved→"NOT_RESERVED"  Reserved→"RESERVED"   default→null  :121-122

:126  parseHouseKeepingStatus(String housekeepingRoomStatus)
        Inspected→"INSPECTED"  Clean→"CLEAN"  …
```

**`Reserved` is in both overloads and maps to two different values** —
`"booking"` and `"RESERVED"` — decided by which overload a caller reached for.

---

## 7 · Reference defects, named as defects

**These are not shapes to copy. Each is cited, and where our platform already
refuses it, the refusal is named.**

| | cited at | our position |
|---|---|---|
| **an unmapped status becomes `null`** | `:102-103` and `:121-122` | **REFUSED** — ADR 0355 §5a: `Reading<T>` has no `null` to return, and `RejectionReason.UnknownStatus` sits beside it |
| **a list of statuses silently reduced to its last** | `:108` over `HousekeepingRoomInfo:50` | **a collapsed axis.** R1 says the four axes are independent; the reference keeps one and records nothing about what it dropped — the gap rule, found in the reference |
| **two overloads, one name, disjoint vocabularies** | `:90` and `:107` | a collision a compiler cannot see; an *action* vocabulary and a *state* vocabulary sharing a method name |
| **a field whose name is also its parent's** | `housekeeping.housekeepingRoomStatus.housekeepingRoomStatus` — `HousekeepingRoomInfo:45` and `:54`, consumed at `:126` | a name that resolves to the wrong thing, and a connector reading the wrong one compiles forever |
| **two fields one letter-group apart** | `housekeepingStatus :56` vs `housekeepingRoomStatus :54` | ⚠ **CLAUDE.md:408 cites `"housekeepingStatus"` as the spelling a rejection quotes, and these are two real fields.** Which one our rejection names is a live choice and nothing records it |
| **`Status` arriving as a field called `roomStatus`** | `OracleOnPremiseReservationCreateRequest:48-49`, `OracleWebReservationCreateRequest:47-48` | a *reservation* status under a room-shaped name, on a reservation push |
| **credentials in plaintext columns** | `cloud/dto/jpa/OracleCloudProperty.java:23 userName`, `:25 password` | **inexpressible here** — the Supervisor forwards credential frames without inspecting them (ADR 0176, `CONN-Q42`), and secrets are sealed and named in the manifest (`required_secrets`) |

---

## 8 · Platform divergences — the ruling wins, and the disagreement is reported

**Currency and the tax basis.** Measured across all three property tables —
`cloud/dto/jpa/OracleCloudProperty.java` (14 fields),
`onPremise/dto/jpa/OracleOnPremiseProperty.java` (9),
`web/dto/jpa/OracleWebProperty.java` (10) — **zero** carry a currency, tax,
basis or amount field.

**And the reference does carry both — on the PAYLOAD:**

```text
OracleCloudReservationGuarantees.java
  :87  AmountPercent.basisType     ← the amountTaxBasis concept
  :91  AmountPercent.currencyCode
  :89  AmountPercent.nights
```

> **So *"the reference table carries no currency and no tax basis"* is exactly
> true about the TABLE and would mislead read as *"the reference has no currency
> concept."*** It carries both per amount, from the vendor, with no
> property-level configuration for either.

**Ours:** **ADR 0217** rules one platform-owned currency metadata authority in
Reference Data, *"not inferred from the property's configured"* anything; **ADR
0220** makes the tax basis per-integration configuration, opaque to the Hub, and
**is open** — which is why `amountTaxBasis` is a declared `prerequisite` on all
three integrations and why `Prerequisites.Missing` gates dispatch.

**And `AmountPercent` carries a basis, a night count and a currency and NO
AMOUNT** (`:87-91`), despite its name. Our side reads amounts through
`AmountReading` with ADR 0226's minor-unit exponent stated at every site.

| | the reference | ours |
|---|---|---|
| **tenancy on a record** | `companyId` · `siteId` stored on the document | the Hub stamps `property_id`; a connector-written value is ignored, not believed — ADR 0128 §6 |
| **check-in / check-out times** | on the integration's own property row — `OracleCloudProperty:35,37` and both siblings at `:29,:31` | Core Administration's, on the platform `Property` — ADR 0052 |
| **the property's zone** | `OracleCloudProperty:39 timeZone`, `OracleOnPremiseProperty:33`, `OracleWebProperty:33` | ADR 0220 gives the IANA zone to the Hub, **and it is not built** — this package is that message's second consumer |
| **idempotence** | a stored `processed` boolean | a connector-produced `dedupe_key` crossing with the payload — ADR 0255 §2 |
| **one class per flavour** | the on-site reservation class duplicated, `onPremise` and `web` | one `OnSiteNormaliser`, prerequisites declared per integration |

---

## 9 · What is cited, and what is not

```text
CITED AT THE LINE   ohip-business-event · ohip-reservation (42 of 251 lines,
                    truncation disclosed) · ohip-housekeeping-room ·
                    ohip-guarantee · onsite-stay both halves ·
                    the status vocabularies · the property tables
NOT CITED           onsite-room-status for oracle-onpremise — §5, because
                    nothing anywhere sources it
NOT READ            209 of Reservation.java's 251 lines; the cloud service
                    IMPLEMENTATIONS (OracleCloudEventServiceImpl,
                    ...ReservationServiceImpl, ...HousekeepingServiceImpl,
                    ...GuaranteeServiceImpl) — the FLOW, where this document
                    covers the SHAPE. Flow is the next pass.
```

**The shape is cited; the flow is not.** A payload's fields are what this document
establishes, and *how the reference drains, orders and retries* lives in the
`services/impl/` classes and the background services. Naming that gap here rather
than letting a reader take §1–§6 for a flow trace.

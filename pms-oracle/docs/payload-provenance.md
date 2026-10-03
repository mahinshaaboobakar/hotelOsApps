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
FLOW, NOW CITED     §10 and §11 — both background services, the drain
                    and the auth implementation, each read WHOLE
FLOW, SECOND PASS   §12 — the three remaining CLOUD implementations, read
                    WHOLE: Guarantee (79), Housekeeping (152), Reservation (272)
STILL NOT READ      209 of Reservation.java's 251 lines, and the two ON-SITE
                    implementations: OnPremiseReservationServiceImpl (385) and
                    WebReservationServiceImpl (433)

                    — AND THE SENTENCE THAT STOOD HERE WAS WRONG. It read
                    "Those are FIELD MAPPING, which §1–§5 cover from the
                    models' side." All three cloud implementations carry FLOW:
                    a cache with a defective key, two commented-out message
                    listeners, a hardcoded tenant, and an upsert decided by a
                    local existence check. §12 is what reading them found,
                    and the classification was mine rather than measured.
```

**This paragraph said *"the shape is cited; the flow is not"*, and named the flow
as the next pass. That pass is §10.** The sentence is kept rather than replaced,
so a reader can see which half arrived when — and what it still excludes is in
the block above, narrowed rather than deleted.


---

## 10 · The flow — how the reference drains, orders, retries and refreshes

**Read WHOLE, not sampled**: `cloud/services/background/OracleCloudBackgroundService.java`
(91), `onPremise/services/background/OracleOnPremiseBackgroundService.java` (73),
`cloud/services/impl/OracleCloudEventServiceImpl.java` (119),
`cloud/services/impl/OracleCloudCloudAuthServiceImpl.java` (63).

### The drain is an unbounded loop until the queue answers 204

`OracleCloudEventServiceImpl.fetchOracleBusinessEvents`:

```text
:50-79  do { ... } while (!eventQueueEmpty)      NO cap, NO budget, NO deadline
:53     .queryParam("limit", 20)                 20 per page
:61     eventId = businessEventId.getId()        the dedupe identity
:59-60  companyId / siteId stamped FROM THE
        PROPERTY RECORD, never from the payload
:68     .processed(false)
:70     eventRepo.save(oracleEvent)              DURABLE BEFORE PROCESSING
:73-75  204 -> queue empty, loop ends
```

**Two concepts ours keeps.** `:70` persists each event *before* anything processes
it — the inbox's own ordering, and page 75 §7 Q2 is the same question at our push
ingress. And `:61`'s `eventId` is the dedupe identity, which ADR 0246 and ADR 0255
§2 make a connector-produced key crossing with the payload.

**One ours refuses: the loop has no bound.** A property with a long queue drains
until the vendor says 204, inside one invocation, with no deadline. Page 75's
`cancellation` row is the opposite — *"today, the Hub ending the session"* — and
`drain` is named there as **the one capability that reads a destructive source**.

### Retry is "leave it unprocessed and re-drain"

```text
:111 / BackgroundService:83   if (instioReservation != null) event.setProcessed(true)
:112 / :84                    eventRepo.save(event)   - saved either way
:106-110 / :80-82             catch (Exception) { log.error(...) }  then CONTINUE
```

**The retry mechanism is the absence of a success flag**, and per-item isolation is
real: one event's failure does not stop the batch. That is a sound concept, and it
is the one our inbox implements with an explicit outcome vocabulary rather than a
boolean — ADR 0284's atomic claim and compare-and-swap transition, where
`InboxOutcome` distinguishes cases a boolean cannot.

### Nothing orders the events, and the state is re-read per event

```text
:97 / BackgroundService:68   getReservationFromOracleById(event...)
```

The loop processes in the order the list arrived — no sequence, no instant
ordering — and **re-fetches the reservation's CURRENT state for each event**. So
two events about one reservation both read the same present state, and the earlier
event is applied to the later world. *Last-writer-wins by construction, and nothing
records that an earlier event was superseded rather than applied.*

### ⚠ Every schedule in the reference is commented out

```text
OracleCloudBackgroundService:46-58   NINE commented variants - @Schedules({...})
                                     and five @Scheduled(cron=...) across
                                     Asia/Kolkata and America/New_York
:59                                  refreshBusinessEvents() - package-private,
                                     and nothing calls it
OracleCloudCloudAuthServiceImpl:40   // @Scheduled(fixedDelay = 5*60*1000, ...)
:41                                  refreshAuthTokens() - PRIVATE
```

**So the polling cadence exists as commented-out experiments in two time zones, and
the token refresher cannot be called at all.** The concept — *a connector decides
its own rate* — is the reference's; the mechanism is ours, and ADR 0255 §3 rules
it: **Temporal owns the durable scheduling and the connector owns `NextPollAfter`**,
with a ladder that *"invents nothing at any rung."* A cron literal in the source, in
one of five candidate zones, is what that ruling replaced.

### ⚠ And this is the defect `TokenLifetime` was written for, now at the line

CLAUDE.md records it as *"refreshing on a hardcoded 45-minute threshold while
`expires_in` sat unread, with the sweep that would have applied even that commented
out."* **Both halves verified:**

```text
:40   the sweep, COMMENTED OUT
:43   findAllByLastRefreshedLessThan(...)   keyed on lastRefreshed, a THRESHOLD
:53   setExpiresIn(refreshedToken.getExpiresIn())
        -> expires_in IS stored, and the refresh decision never reads it
```

*The field is written on every refresh and consulted by nothing.* Our
`TokenLifetime` reads `expires_in` and is a port whose adapter is unimplemented —
and its own file says so, which is the honest shape rather than this one.

---

## 11 · Flow defects, named — and one is a null crossing a boundary

| | cited at | our position |
|---|---|---|
| **a REST failure returns `null`, and both callers call `.isEmpty()` on it** | returns `null` at `EventServiceImpl:84`; consumed at `BackgroundService:64-65` and `EventServiceImpl:93-94` | **a NullPointerException on any `RestClientException`**, at two call sites. **Inexpressible here**: page 75's `failure` row — a `Fault` when the capability could not run, *"never an empty result standing in for one"* — and `Reading<T>` has no `null` to return |
| **the dispatch loop exists twice** | `BackgroundService:63-88` and `EventServiceImpl:95-114`, near line for line | one scheduled (commented out), one on demand. Two copies that must agree, and nothing compares them |
| **only `moduleName == "Reservation"` is handled** | `:96` / `:67` | every other module's event is skipped **and never marked processed**, so it is re-drained forever. A filter with no record |
| **`log.error` for routine progress** | `:42`, `:60`, `:74`, `:101` | *"SCHEDULER :: ... FETCHING"*, *"QUEUE EMPTY"* and *"SKIPPING UPDATE ACTION"* at error level — a log that cannot separate a failure from a heartbeat |
| **a monetary amount parsed as a float** | `OracleOnPremiseBackgroundService:60-61` — `Float.parseFloat(getAmount())` | **ADR 0226** states the minor-unit exponent at every site and **ADR 0217** gives currency metadata one platform authority; `AmountReading` exists for exactly this. No currency accompanies the parse |
| **a third spelling of one status** | wire `"Checked In"` (`:45`), OHIP `"CheckedOut"` (`BaseService:98`), written `"CHECKED OUT"` (`:66`) | three spellings across one reference; our vocabularies are `Reading<T>`-typed and the exactness is pinned by a test whose name carries the reason (`CONN-Q25`) |
| **a hardcoded room serial** | `:50` `setSerialNumber("01")` | a literal where a value belongs |
| **test scaffolding left in the file** | `AuthServiceImpl:30-38` — a commented `@PostConstruct` seeding `accessToken("sdfds")`, `expiresIn(2)` | — |

**And `roomStatus` is queried as a reservation status**, which confirms §7's naming
finding is live in the reference's own code rather than only in its models:
`OracleOnPremiseBackgroundService:45` filters
`Criteria.where("roomStatus").is("Checked In")`.


---

## 12 · The flow, second pass — and it corrected a classification of mine

**§9 said the three remaining cloud implementations were "FIELD MAPPING, which
§1–§5 cover from the models' side." That was wrong, and reading them is what
established it.** All three carry flow, and two carry defects nothing in the
models could have shown.

Read WHOLE: `OracleCloudGuaranteeServiceImpl` (79),
`OracleCloudHousekeepingServiceImpl` (152),
`OracleCloudReservationServiceImpl` (272).

### ⚠ The guarantee service is a cache, and its key omits the parameter that varies the answer

```text
:36-39  CacheConfiguration  100 entries, timeToLiveExpiration(1 hour)
:41     its own CacheManager, built in a field initialiser
:52     guaranteeCache.containsKey(property.getHotelId())   ← the KEY
:59     .queryParam("arrivalDate", arrivalDate)             ← the VARYING INPUT
:63     guaranteeCache.put(property.getHotelId(), guarantee)
```

**The cache is keyed on the hotel; the request is keyed on the hotel AND the
arrival date.** So a guarantee fetched for one arrival date is served for every
other arrival date at that hotel for up to an hour. *A cache key that omits a
request parameter is a wrong answer with a time limit.*

**And our architecture moves the cache out of the connector entirely** — the
manifest's own words, quoted in the ledger: *"the Hub owns the inbox, the queue,
retry and the cache (ADR 0128 §5), so this process holds no durable state of its
own."* **This defect is the argument for that boundary rather than an illustration
of it.**

Also: `:62` `.resGuarantees.get(0)` takes the first of a list with no emptiness
check; `:68-69` catches `Exception` and calls `e.printStackTrace()`; `:51` and
`:71` return `null`.

### ⚠ Both housekeeping listeners are commented out — making it THREE entry points

```text
:39   // @RabbitListener(bindings = @QueueBinding(value = @Queue("pms.room.status"), …
:51   // @RabbitListener(queuesToDeclare = @Queue("room.status.change"))
```

With §10's scheduler and token refresher, **every entry point into the Oracle
cloud flow is commented out.** That is a pattern rather than three instances: the
reference's triggers are all disabled, so the flow it documents has no live
caller anywhere.

### ⚠ A hardcoded property id, while the request carries its own

```text
:58  instioService.fetchActiveInstioProperty("6257ef1…")   ← a LITERAL
:72  the same literal, in the sibling method
:60  log.error("Property {} Not Active", request.getSiteId())   ← the request's
:74  the same                                                     own id, used
                                                                  ONLY to log
```

**Both housekeeping paths resolve one hardcoded tenant and use the request's
`siteId` only in the failure message.** A multi-tenant surface pinned to a
constant, and the log makes it look as though the request decided.

### And this settles the `housekeepingStatus` ambiguity §7 surfaced

The reference reads **both** confusable fields, in one builder:

```text
:86  reservationStatus  ← parseInstioReservationStatus(…getReservationStatusList())
                          the LIST overload — §6's last-wins reduce, at its call site
:87  roomStatus         ← …getHousekeepingRoomStatus().getHousekeepingRoomStatus()
:88  foStatus           ← …getFrontOfficeStatus()
:89  hkStatus           ← …getHousekeepingStatus()
```

> **So `housekeepingRoomStatus` maps to `roomStatus`, and `housekeepingStatus`
> maps to `hkStatus`.** CLAUDE.md:408's example — *a rejection quoting
> `"housekeepingStatus"`* — corresponds to **`hkStatus`**, the housekeeping axis,
> and not to the room-status axis one letter-group away.

*That is the live ambiguity answered from the reference's own mapping rather than
by choosing. The constitution's sentence is the architect's to amend; this is the
measurement it was missing.*

### The reservation service: an upsert decided locally, and two bare throws

```text
:104  if (isReservationExist(reservationId)) method = HttpMethod.PUT;
        → POST vs PUT chosen by a LOCAL existence check, not by the vendor
:120-124  if (!status.equals("booking")) { if (!validRoom(…)) return null; }
:127-128  if (successCode != null) log.error("…updated at INSTIO:: {}")   ← SUCCESS
:163  return null on RestClientException
:178, :181  throw new RuntimeException("Invalid Data")   — twice, two words
:226-241  four branches on "booking" / "checkIn" / "checkOut" / "cancelled"
            — §6's camelCase ACTION vocabulary, used as a dispatch
:142  if (dumb)  — a branch on a variable named `dumb`
```

**The four-branch dispatch at `:226-241` is where §6's first overload is
consumed**, which is what makes the two vocabularies a live hazard rather than a
curiosity: the action names are a control-flow key.

### Flow defects, second pass

| | cited at | our position |
|---|---|---|
| **a cache key omitting a varying parameter** | `Guarantee:52`, `:59`, `:63` | the cache is the Hub's (ADR 0128 §5); the connector holds no durable state |
| **all three remaining triggers commented out** | `Housekeeping:39`, `:51`, with §10's `:40` and `:46-58` | ADR 0255 §3: Temporal owns the schedule, the connector owns `NextPollAfter` |
| **a hardcoded tenant id in a multi-tenant path** | `Housekeeping:58`, `:72` | the Hub stamps `property_id` from the session binding and ignores anything a connector supplies — ADR 0128 §6 |
| **`log.error` for success** | `Housekeeping:119`, `Reservation:127-128` | third and fourth instances, after §11's two |
| **`e.printStackTrace()`** | `Guarantee:69`, `Housekeeping:107`, `:124` | a stack trace to stdout in place of a diagnostic |
| **`.get(0)` with no emptiness check** | `Guarantee:62`, `Housekeeping:82`, `Reservation:94`/`:97` | four sites |
| **`throw new RuntimeException("Invalid Data")`** | `Reservation:178`, `:181` | a two-word message for two different causes; our refusals name the field and the raw value in the vendor's own spelling (page 75 §1) |
| **POST/PUT from a local existence check** | `Reservation:104` | our upsert is the Hub's reconciliation against the verified signed manifest, which cannot disagree with itself |

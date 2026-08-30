# 42a · The PMS reference study — Oracle, three flavours

**Status:** study, 2026-08-30. Stream DD, deliverable 1 of the connector
round — brief `docs/working/42-the-connector-round.md` §3, **in the platform
repository**.
**Nothing here is a ruling, and nothing here is a design.** §1–§7 are facts
with citations. §8 is the only section that contains the stream's opinion,
and it is labelled as such.

**Where this page lives, and why.** Owner direction, 2026-08-30: *"apps and
connectors are root folders, and their docs are kept there."* This is an
Oracle-specific page — it would not still be true if a different PMS were
first — so it belongs to the connector, in this repository, per `README.md`
§"Connectors live here too". The **gap analysis**, the **Integration Hub's
design** and every **register row** stay in the platform repository, because
each of those holds whichever PMS ships first. Below, a path beginning
`docs/` is a platform-repository path unless said otherwise.

---

## 0 · What this is, and the rule it was written under

The owner pointed this round at a legacy Java PMS-integration system as a
**domain and edge-case source, never a structural one** (brief §2):

> *"we can't copy or mirror the architecture or design from this — we can
> learn things from that and design based on our core design. Java is legacy;
> they have their own cons — bad architecture and design, data layer and
> database design. So first learn and find gaps, and design our way."*

So this page records **what the PMS does**, what the integration had to do
about it, and what it got wrong. It does not record a layering to copy.
Nothing from the reference has been copied into **either** repository, and no
file under the platform's `connectors/` or `services/` was created.

### Where the citations point

The reference is read-only and outside the repository:

```text
C:\Users\Mahin Aboobakker\Documents\HotelOs-References\pms-integrations\
  pms-integrations\src\main\java\co\instio\integrations\
```

Every `file:line` below is relative to that root, except a handful under
`co\instio\global\` and `src\main\resources\`, which are written out from
`src\main\java\co\instio\` and `src\main\` respectively.

### What was read

`providers/oracle/{cloud,onPremise,web}` in full — 87 files — plus `common/`,
`modules/`, the parts of `global/` the Oracle code depends on (security,
`DateUtils`, the request/entity base classes) and
`src/main/resources/application.properties`. The other nine providers were
glanced at only where they answer a question Oracle raises (§5.6).

---

## 1 · The three flavours are three different integrations

They share a vendor name and almost nothing else. **The direction of the
connection is what differs**, and it changes everything downstream.

| | **cloud** | **onPremise** | **web** |
|---|---|---|---|
| The system | Oracle Hospitality Integration Platform (OHIP), the cloud REST API in front of OPERA Cloud | An on-site OPERA installation, reached through an agent the hotel runs | A second on-site OPERA variant, same wire shape as onPremise |
| Direction | **we pull** | **the PMS pushes** | **the PMS pushes** |
| Transport | HTTPS REST + JSON, one global host | HTTP POST of flat JSON to our endpoint | HTTP POST/PUT of flat JSON to our endpoints |
| Credential | OAuth2 password grant, per property | none | none |
| Change notification | polled **business-event queue** | the push itself | the push itself |
| Room status | read **and** write | absent | **received only**, PMS → us |

`OracleCloudBaseService.java:27` (`oracle.host.url`) is a single host for every
property; the per-property part is `hotelId` plus a username and password
(`cloud/dto/jpa/OracleCloudProperty.java:23-27`). The two on-site flavours have
no outbound call at all — every class under `onPremise/` and `web/` is a
receiver.

### 1.1 · How each authenticates

**cloud — a two-level credential.** Application-level credentials are global,
property-level credentials are per property, and both are needed on every call:

```text
token request      Basic(clientId, clientSecret) + x-app-key
                   grant_type=password, username/password of the property
                          cloud/services/OracleCloudBaseService.java:51-61

every other call   Bearer <token> + x-app-key + x-hotelid
                          cloud/services/OracleCloudBaseService.java:63-70
```

`x-hotelid` is the property selector on an otherwise property-agnostic host
(`:66`); `x-app-key` identifies the *integration*, not the hotel (`:67`).

**Token lifetime is inferred, not read.** The refresh sweep selects tokens
whose `lastRefreshed` is more than **45 minutes** old
(`cloud/services/impl/OracleCloudCloudAuthServiceImpl.java:43`) — a hardcoded
constant — while `expires_in` is stored on the row
(`cloud/dto/jpa/OracleCloudAuthToken.java:24-25`) and never consulted. The
refresh is a **fresh password grant**, not a refresh-token exchange (`:49`), so
the property's password must stay retrievable for the life of the integration.

**The sweep is disabled.** `@Scheduled` on `refreshAuthTokens` is commented out
(`:40`) and the method is `private`, so nothing calls it. Every request path
reads the token from the table (`OracleCloudBaseService.java:72-77`) and nothing
writes it after the first.

**onPremise and web authenticate nothing.**
`co/instio/global/security/WebSecurityConfig.java:35` is
`anyRequest().permitAll()`. The `X-AUTH-KEY` filter acts only *if the header is
present* (`co/instio/global/security/user/AuthenticationFilter.java:38-43`), and
a failed check returns silently (`:61-63`). Property identity therefore arrives
in the **request body** as `PropertyCode`
(`onPremise/models/OracleOnPremiseReservationCreateRequest.java:66-67`;
`web/models/OracleWebRoomStatusChangeRequest.java:30-31`) and is trusted.

### 1.2 · How each learns that something changed

**cloud — a drained queue, and the events are thin.**

```text
GET int/v1/externalSystem/{externalSystemCode}/hotels/{hotelId}/businessEvents?limit=20
        200 → a page of events, keep going
        204 → the queue is empty, stop
                    cloud/services/impl/OracleCloudEventServiceImpl.java:52-79
```

The read is **destructive**: the loop repeats one URL until 204, which
terminates only because the server removes what it hands over (`:50`, `:79`).

An event carries `moduleName`, `actionType`, `primaryKey`, `createdDateTime`
and `hotelId` and nothing else (`cloud/models/BusinessEventResponse.java:28-42`),
so every event is followed by a **read-back** of the reservation by id (`:97`,
and `rsv/v1/hotels/{hotelId}/reservations/{reservationId}` at
`cloud/services/impl/OracleCloudReservationServiceImpl.java:139`).

Only `moduleName == "Reservation"` is handled; every other module is fetched,
stored and dropped (`:96`).

**onPremise and web — the push is the notification.** A single POST carries the
whole record; there is no queue and no read-back, because there is nothing to
read back from.

**The polling cadence was tuned, and the record of that tuning survives as
commented annotations**
(`cloud/services/background/OracleCloudBackgroundService.java:46-58`):

```text
0 0 */3 * * *      America/New_York     every three hours
0 0/15 14-16 * * * America/New_York     every 15 minutes, 14:00–16:00
@hourly            Asia/Kolkata
0 30 11 * * *      Asia/Kolkata
```

Two facts sit in that list. The interval is **not uniform across the day** — it
tightens around the check-in window — and the schedule is expressed in the
**property's** time zone, with two different zones tried.

**Every one of them is commented out.** In the state on disk the cloud flavour
has no scheduler; it moves only when a human calls
`GET oracle/cloud/event/refresh?siteId=…`
(`cloud/resources/OracleCloudEventResource.java:20-24`).

---

## 2 · Capability × flavour

Each cell names the entry point. **absent** means no code; *disabled* means the
code exists and cannot run as committed.

| Capability | cloud | onPremise | web |
|---|---|---|---|
| Reservation read (by id) | `getReservationFromOracleById` — `cloud/services/impl/OracleCloudReservationServiceImpl.java:136` | absent — pushed | absent — pushed |
| Reservation receive (push) | absent | `POST oracle/on-premise/zuri` — `onPremise/resources/OracleOnPremiseReservationResource.java:29-34` | `POST oracle/web/reservation` — `web/resources/OracleWebReservationResource.java:22-27` |
| Reservation create/update **in the PMS** | absent | absent | absent |
| Booking → platform | `createOrUpdateReservation:102` | `processOracleOnPromiseReservation:86-113` | `processReservation:175-202` |
| Check-in → platform | inferred from status, `:230-233` | `processCheckIn:219-268` | `directCheckIn:251` |
| Check-out → platform | inferred from status, `:234-237` | `:146-183` | `checkOutRoom:291-315` |
| Cancellation → platform | inferred from status, `:238-242` | `:185-210` | `:264-286` |
| Room change vs room update | absent | absent | written and **commented out** — `web/services/OracleWebReservationServiceImpl.java:225-231` |
| Housekeeping status **read** | `getHousekeepingRoomInfoFromOracle` — `cloud/services/impl/OracleCloudHousekeepingServiceImpl.java:94` | absent | absent |
| Housekeeping status **write to PMS** | `updateOracleRoomHousekeepingStatus:113` | absent | absent |
| Room status **received from PMS** | absent | absent | `PUT oracle/web/room/status` — `web/resources/OracleWebRoomResource.java:24-29` |
| Guest profile | `updateOracleProfile` — `cloud/services/impl/OracleCloudProfileServiceImpl.java:27` (**a GET; §7.4**) | absent | absent |
| Guarantee / deposit / cancellation policy | `fetchGuarantee` — `cloud/services/impl/OracleCloudGuaranteeServiceImpl.java:50` | absent | absent |
| Room traces | absent | absent | `PUT oracle/web/room/trace` — accepted and **discarded**, `web/services/OracleWebRoomServiceImpl.java:66-71` |
| Online check-in | `POST oracle/cloud/checkin_info` — an **empty method**, `cloud/services/background/OracleCloudBackgroundService.java:42-44` | absent | absent |
| Forced replay | `POST oracle/cloud/reservation/force-refresh` — `cloud/resources/OracleCloudReservationResource.java:47-50` | `GET oracle/on-premise/zuri?siteId&reservationId` forces a check-out — `:36-39` | absent |

**Two things this matrix says.** No flavour writes a reservation back to the
PMS — the integration is inbound for reservations, and outbound only for
housekeeping status, on one flavour. And **the richest flavour is the pulled
one**: guarantees, profiles and the four-axis room status exist only where there
is an API to ask.

---

## 3 · The data model as persisted

### 3.1 · Two databases per flavour, three flavours, no sharing

Each flavour owns a **MySQL schema for configuration** and a **MongoDB database
for records**:

```text
MySQL  oracle_cloud / oracle_onpremise / oracle_web
       property · configuration · token · room_info
Mongo  one per flavour
       reservation · event · reservation-history · reservation_dumb · room_status_info
```

`common/constants/DbConstants.java:20-66` shows the table names are literally
the same strings for every provider — `"property"`, `"configuration"`,
`"room_info"` — kept apart only by living in different databases. The connection
strings are in `src/main/resources/application.properties:31-45`, and the Mongo
URI is **hardcoded in Java** as `mongodb://localhost:27017`
(`cloud/configuration/OracleCloudMongoDbConfiguration.java:27`).

There are no migrations. `hibernate.hbm2ddl.auto` is wired from configuration
(`cloud/configuration/OracleCloudMysqlDbConfiguration.java:54`) and set to
`update` (`application.properties:91`), so the schema is whatever the entity
classes currently say.

### 3.2 · The reservation, three shapes

**cloud** stores the PMS's own nested document nearly verbatim
(`cloud/dto/mongo/Reservation.java`), and its shape is the domain lesson:

```text
reservationIdList[]        {id, type}  — a reservation has SEVERAL ids   :31, :53-60
roomStay
  currentRoomInfo          {roomType, roomId}                            :78-83
  guestCounts              {adults, children}                            :86-91
  arrivalDate/departureDate   dates, no time                             :69-71
  expectedTimes            expected arrival / expected departure          :94-98
  total.amountBeforeTax                                                   :101-104
reservationGuests[]        each with `primary` and a full profile          :109-113
  profileInfo.profileIdList[]  {id, type} — a guest also has several ids   :122-127
  profile.customer.personName[]  each with a `nameType`                    :150-158
  profile.{addresses,telephones,emails}  each entry with `primaryInd`      :162-244
reservationStatus · createDateTime · lastModifyDateTime · creatorId
  · lastModifierId · createBusinessDate                                   :41-51
```

**onPremise and web** store a **flat single-room row**
(`onPremise/dto/mongo/OracleOnPremiseReservation.java`;
`web/dto/mongo/OracleWebReservation.java`) — `surName`, `firstName`,
`arrivalDate`, `departureDate`, `noOfRooms`, `adults`, `children`, `phone1`,
`phone2`, `email`, `roomStatus`, `source`, `travelAgent`, `room`, `roomType`,
`mealPlan`, `marketCode`, `amount`, `uniquePersonId`. Both carry a written
comment that this is a source limitation rather than a modelling choice:

> `/*On-premise data contains only single room data, so build instio reservation
> with single room*/` —
> `onPremise/services/OracleOnPremiseReservationServiceImpl.java:292-293`,
> repeated at `web/services/OracleWebReservationServiceImpl.java:351-352`

`amount` is a **String** in both (`:63`, `:67`), parsed with `Float.parseFloat`
at use (`onPremise:357`, `web:420`). In the cloud shape the same value is an
**int** (`cloud/dto/mongo/Reservation.java:102`).

### 3.3 · The identifier mapping — and what is missing from it

The only mapping table in the Oracle tree is **room type → description**:

```text
room_info (companyId, siteId, roomType, description)
```

`cloud/dto/jpa/OracleCloudRoomInfo.java:19-25`, read at
`cloud/services/impl/OracleCloudReservationServiceImpl.java:219-220`,
`onPremise:310-311`, `web:369-370`.

**There is no room mapping.** The PMS's room number is carried through to the
downstream platform as a string —
`roomInfo.setRoom(oracleReservation.getRoomStay().getCurrentRoomInfo().getRoomId())`
(`cloud:217`, `onPremise:314`, `web:373`). There is no reservation mapping
either: the PMS's `reservationId` *is* the downstream key (`cloud:168`,
`onPremise:298`, `web:357`).

The one identifier the integration mints is a **sub-reservation id**, formed by
concatenation — `reservationId + "-" + <room ordinal>` (`cloud:211`,
`onPremise:308`, `web:367`) — always `-1`, because there is always one room.

### 3.4 · History and dumps — a dead letter, implemented as a move

Both push flavours implement the same mechanism under two names. A record that
cannot be processed is **deleted from the live collection** and re-inserted into
a second collection with a reason string:

```text
onPremise   dumpCollection()         reservation → reservation-history
                                     field: dumbReason              :361-366
web         dumbReservation(reason)  reservation → reservation_dumb  :424-431
```

The reasons are the domain's failure vocabulary: `NO RESERVATION ID` ·
`BLANK ROOM NO` · `NO VALID DATA FOUND TO MERGE` · `CHECKED-IN ROOMS NOT FOUND` ·
`ROOM IS <status>` · `UPDATE` · `CHECKOUT SUCCESS` · `CHECKOUT FAILED` ·
`RESERVATION CANCELLED` · `ALREADY_CHECKED-IN` · `NO_BOOKINGS_FOUND` ·
`BOOKING_UPDATE` · `CHECKIN_RECEIVED` · `ROOM_UPDATE` · `BLANK_ROOM`.

The same mechanism carries **both** failures and successful supersessions —
`CHECKOUT SUCCESS` and `RESERVATION CANCELLED` are archival, `BLANK ROOM NO` is
a rejection. One collection, two meanings.

The cloud flavour has no dead letter. A reservation it cannot process is logged
and dropped (`cloud:117-118`, `:122-124`).

### 3.5 · Multi-property handling

Property resolution is one query per flavour, and it is the same query:

```text
select * from property p inner join configuration c on p.configId = c.id
 where p.siteId = ?1 and p.enabled = true and c.enabled = true
   and p.propertyAuthorizationKey is not null
```

`cloud/dao/mysql/OracleCloudPropertyRepository.java:21-26`,
`onPremise/dao/jpa/OracleOnPremisePropertyRepository.java:12-16`, and the web
equivalent. **Two levels of enablement** — the property and the customer's
configuration — plus the presence of a downstream key, all in one predicate. The
cloud flavour adds a third flag, `fetchEvents`, so polling can be stopped per
property without disabling the property (`:14-19`;
`cloud/dto/jpa/OracleCloudProperty.java:43`).

Each property row carries its own **check-in time, check-out time and time
zone** (`OracleCloudProperty.java:35-39`, and the same three on the other two
flavours). Those three fields are load-bearing — §5.3.

---

## 4 · The runtime shape

```text
cloud       scheduler (disabled) ─┐
            manual refresh URL ───┴─▶ drain business events
                                        ▶ read the reservation by id
                                        ▶ convert
                                        ▶ POST/PUT to the downstream platform
                                        ▶ mark processed

onPremise   POST /oracle/on-premise/zuri
web         POST /oracle/web/reservation
            PUT  /oracle/web/room/status
                                     ▶ persist raw, synchronously
                                     ▶ branch on roomStatus
                                     ▶ correlate against stored records
                                     ▶ call downstream, or dump
```

**Processing is inline with the HTTP request.** `create()` persists and then
calls `processOracleOnPromiseReservation` on the request thread
(`onPremise:78-79`); the asynchronous version is written and commented out
(`:36`, `:80`). The web flavour is the same (`web:65-70`), and only wraps the
call in a `try` so a failure still returns 200.

**The downstream call carries credentials in the query string:**

```text
{pms.server}/pms/v2/reservation?API_KEY=…&property=…&forceCheckIn=…&forceCheckout=…
        common/services/InstioGuestEntryServiceImpl.java:51-55
```

`forceCheckIn` and `forceCheckout` are **per-flavour policy flags**, not per
request: cloud sends `(false, true)` (`cloud:126`), onPremise `(true, false)`
(`onPremise:101-102`), web `(false, true)` (`web:193-194`). They tell the
downstream platform to accept a check-in for a stay it has no booking for, or a
check-out for a stay it never saw checked in — the flavour's answer to
out-of-order arrival, encoded once and for all.

**Retry does not exist.** There is no retry loop, no backoff and no dead-letter
re-drive anywhere in the Oracle tree. A failure is a log line (`cloud:117`,
`:159`, `:161`; `onPremise:178`, `:207`; `web:284`, `:305`) or a dump. The four
downstream calls have their error paths **commented out**
(`common/services/InstioGuestEntryServiceImpl.java:96`, `:101`, `:124`, `:129`,
`:152`, `:157`, `:180`, `:185`), so a rejected check-in returns `null` and the
caller treats it as "did not happen" without knowing why.

**Idempotency does not exist either**, and its absence produced the single most
instructive line in the reference — §5.5.

**Health and observability.** No per-connector health endpoint, no metrics, no
trace propagation into the PMS call. There is a per-request MDC id
(`co/instio/global/filters/Slf4jMDCFilter.java:62-70`) which is **generated
locally** rather than taken from an inbound header — `requestHeader` is
constructed as `null` (`:34`), so the branch that would read one is dead. The one
aggregate that exists is a counter shape, `common/models/IntegrationInfo.java:8-21`:
bookings received, arrivals, check-ins, due-outs, check-outs, plus two booleans —
`checkInsDown`, `checkOutsDown`. Those two are the closest thing to a connector
health signal in the codebase, and they are **per capability, not per connector**.

**When the PMS is unreachable**, the cloud flavour catches `RestClientException`,
logs, and returns `null` (`cloud/services/impl/OracleCloudEventServiceImpl.java:81-84`;
`OracleCloudReservationServiceImpl.java:160-163`). Nothing is queued and nothing
is retried — so the events already drained from the OHIP queue are **gone**, taken
off the source by the read that then failed to process them.

---

## 5 · The domain knowledge worth keeping

This is the section the study exists for. None of it depends on the reference's
architecture.

### 5.1 · Reservation status is not one vocabulary — it is four

**OHIP reservation status**, five values
(`cloud/services/OracleCloudBaseService.java:90-105`):

```text
Reserved · InHouse · CheckedOut · Cancelled · NoShow
```

**OHIP room-level reservation status**, a *list* per room, five different values
(`:107-124`):

```text
Reserved · Arrived · StayOver · Departed · NotReserved
```

It arrives as a **list** and the code takes the last element (`:108`). A room
with `[NotReserved, NotReserved, Departed, StayOver, Arrived]` is a room several
stays touch on one business day — the one departing, the one staying over, the
one arriving. A `main()` was left in the file demonstrating exactly that case
(`:149-159`), which is how we know it was met in the field.

**OPERA on-site status**, a different vocabulary again (`onPremise:65-66`, `:86`,
`:114`, `:131`, `:146`, `:185`, `:212-214`):

```text
Due In / DUE IN / OT → RESERVED     Checked In / CHECKED IN
CHECKED OUT   CANCELLED   DUE OUT   PENDING   WAITLIST
```

**Two casings of one status are two different messages.** `"Checked In"` and
`"CHECKED IN"` each have their own branch, and each looks for the *other* casing
when correlating (`onPremise/dao/mongo/OracleOnPremiseReservationDaoImpl.java:50-74`):
the `CHECKED IN` branch searches for a stored `Checked In`, and vice versa. The
two feeds carry different fields — one supplies phone, email and departure date
(`onPremise:123-126`), the other supplies the room number (`:140-141`) — so a
check-in is delivered as **two partial messages that must be joined**.

**Housekeeping status**, seven values plus a blank
(`cloud/services/OracleCloudBaseService.java:126-147`):

```text
Inspected · Clean · Vacant · Occupied · Dirty · OutOfOrder · OutOfService
""  →  PICK_UP
```

The empty string is meaningful: a room with no housekeeping status set is a
**pick-up** room, and mapping it to null loses that.

**The on-site room-status codes are two letters**, a fourth vocabulary
(`web/services/OracleWebBaseService.java:32-58`):

```text
room   DI dirty · CL clean · IP inspected · OO out of order · OS out of service
front  VAC vacant · OCC occupied
```

And the on-site reservation status arrives as a **comma-separated list inside one
string**, of which the first element is taken (`:12-13`) — the same
several-stays-per-room fact as OHIP's list, delivered differently.

### 5.2 · A room has four independent statuses, not one

`cloud/models/HousekeepingRoomInfo.java:50-56` and the web push
(`web/dto/mongo/OracleWebRoomStatusInfo.java:21-25`) agree:

```text
reservationStatusList   who is in it, arriving, leaving   (a list)
frontOfficeStatus       vacant / occupied
housekeepingRoomStatus  dirty / clean / inspected
housekeepingStatus      the housekeeping department's own
```

Front-office occupancy and housekeeping cleanliness are **orthogonal**: a room
can be vacant and dirty, occupied and clean, vacant and out of order. A model
that collapses them into one `room.status` loses the distinction housekeeping
actually works from.

Two derived operational states are computed from the tuple
(`web/services/OracleWebBaseService.java:60-66`), and these are hotel rules
rather than data mapping:

```text
VACANT_REFRESH   vacant + arrival expected + (clean | inspected)
                 → a room that sat empty still needs a freshen before arrival
STRIP_LINEN      occupied + due out + dirty + not blocked again today
                 → strip it, because nobody arrives into it tonight
```

`STRIP_LINEN` depends on `nextBlockedAt` — **when the room is next sold** —
which the PMS pushes and no cleaning system can derive
(`web/services/OracleWebRoomServiceImpl.java:52-53`). The same rule runs as a
daily sweep at **05:55 in the property's morning** over every vacant clean or
inspected room
(`modules/housekeeping/service/background/HousekeepingBackgroundService.java:50`,
`:91-103`).

`pseudoRoom` is a flag on the room type
(`cloud/models/HousekeepingRoomInfo.java:63`). Pseudo rooms are PMS bookkeeping
constructs — house accounts, groups — and are not physical rooms.

### 5.3 · Time is the hardest part, and it has six separate problems

**(a) A PMS sends dates; a hotel operates on datetimes.** `arrivalDate` and
`departureDate` are dates with no time (`cloud/dto/mongo/Reservation.java:69-71`).
The integration builds a timestamp by combining the date with the **property's**
configured check-in / check-out time in the **property's** time zone
(`cloud:227-228`, `onPremise:321-322`, `web:380-381`). Those two clock times are
property configuration, not reservation data.

**(b) Expected times replace actual times.** For a stay in house or departed the
timestamps used are `reservationExpectedArrivalTime` and
`reservationExpectedDepartureTime` (`cloud:231`, `:235-236`) — the PMS's
*expectation*, not when the guest actually arrived. The status says the guest is
in the room; the timestamp says when they were due.

**(c) The status decides which clock to read**, four branches, four rules
(`cloud:226-242`):

```text
booking    arrival date + property check-in time  |  departure date + check-out time
checkIn    expected arrival time                  |  departure date + check-out time
checkOut   expected arrival time                  |  expected departure time
cancelled  as booking, plus cancelledTime = lastModifyDateTime
```

**(d) Three formats in one integration**, each hardcoded at its call site:

```text
yyyy-MM-dd HH:mm:ss.S     OHIP timestamps       cloud:171, :231, :235-236, :241
yyyy-MM-dd'T'HH:mm:ss     on-site timestamps    onPremise:156, :169, :239; web:61-62
dd-MM-yy                  the next-blocked date web/services/OracleWebRoomServiceImpl.java:53
```

…and `DateUtils.stringToLocalDate` sniffs across **fourteen** formats when the
caller does not know (`co/instio/global/utils/DateUtils.java:70-77`).

**(e) A missing time zone silently becomes India.** `getMillisFrom(date, time,
timeZone)` falls back to a two-argument overload when the zone is blank, and that
overload hardcodes `Asia/Kolkata`
(`co/instio/global/utils/DateUtils.java:171-184`). A property row with an empty
`timeZone` then produces timestamps wrong by the offset, silently — and the daily
housekeeping sweep hardcodes the same zone for **every** property regardless of
where it is
(`modules/housekeeping/service/background/HousekeepingBackgroundService.java:50`).

**(f) The business date is a separate concept.** `createBusinessDate`
(`cloud/dto/mongo/Reservation.java:51`) is the hotel's operating day, which rolls
at night audit and is not the calendar date. It is stored here and never used —
but it is the field a PMS reconciles against.

### 5.4 · Guest identity inside a reservation is a search, not a field

Reaching the guest's name in an OHIP reservation takes four filters
(`cloud/services/impl/OracleCloudReservationServiceImpl.java:176-208`):

```text
the guest    reservationGuests[]  where primary == true          :176-178
the name     personName[]         where nameType == "Primary"    :179-181
the address  addressInfo[]        where address.primaryInd       :186-187
the phone    telephoneInfo[]      where telephone.primaryInd     :200-201
the email    emailInfo[]          where email.primaryInd         :205-206
```

Each of those lists can be empty and each `primaryInd` can be false everywhere;
the code throws `Invalid Data` when the primary guest or primary name is missing
(`:178`, `:181`). Phones additionally carry `phoneTechType` and `phoneUseType`
(`cloud/dto/mongo/Reservation.java:212-214`), so "the guest's phone number" is a
choice among several with different meanings.

A guest, like a reservation, has **several identifiers of different types**
(`profileIdList[] {id, type}` — `:117`, `:122-127`).

### 5.5 · The workarounds, and what each one paid for

These are the lines that record something learned in production.

**A read timeout is treated as success.** All four downstream calls do it
(`common/services/InstioGuestEntryServiceImpl.java:30-36`, `:77-80`, `:105-108`,
`:133-136`, `:161-164`, `:189-192`):

```text
if (isReadTimeout(e)) {
    log.warn("Read timeout during reservation send, treating as success for: {}", …);
    return instioReservation.getReservationId();
}
```

The reasoning is legible: a slow response usually means the far side *did* process
the request, and re-sending would check the same guest in twice. With no
idempotency key there is no third option, so the integration guesses — and guesses
in the direction of losing an event rather than duplicating a stay. **This is the
strongest argument in the whole reference for an idempotency key on every write**,
and the clearest case of a workaround a contract would have removed.

**A check-out that arrives with no check-in.** Written, then commented out
(`cloud/services/impl/OracleCloudReservationServiceImpl.java:105-109`):

```text
//if status checkout and method post, first checkIn and then checkout
```

The problem is real — the first event seen for a stay can be its departure — and
the two live answers to it are `forceCheckIn` / `forceCheckout` (§4) and
`directCheckIn` / `directCheckout` (`onPremise:368-383`, `web:317-349`, the
latter marked `//Todo remove after data flow is ok` at `onPremise:258-259`).
**Three mechanisms for one problem, none of them removed.**

**Update events are skipped when the reservation has moved on**
(`cloud/services/impl/OracleCloudEventServiceImpl.java:99-104`), with the reason
spelled out in the background copy at `OracleCloudBackgroundService.java:71`:

```text
//skipping update events of checkIn and checkouts
```

An `UPDATE RESERVATION` business event is emitted for a check-in and a check-out
as well as for a genuine edit, so the connector re-reads the reservation and
discards the event unless the status is still `Reserved`. **The event type does
not tell you what happened; the re-read does.**

**A guest with no contact detail is dropped** — for anything other than a booking
(`cloud:120-125`, `:264-267`). The push flavours had the same rule and
**commented it out** (`onPremise:283-288`), which is the more honest outcome: a
stay without a phone number is still a stay.

**…and on the web flavour a contact detail is invented instead**
(`web/services/OracleWebReservationServiceImpl.java:58-60`): when the guest has
neither email nor phone and the property has `forceInsertIntoCrm` set
(`web/dto/jpa/OracleWebProperty.java:35`), an address is synthesised from the
first name against a fixed domain. **A required downstream field produced
fabricated guest data**, per property, behind a flag.

**Room change and room update are different operations.** Only the web flavour
reached the point of needing the distinction, and the code is commented out
(`web:225-231`):

```text
if (!reservation.getRoom().equals(existingCheckin.getRoom()))  processRoomChange(…)
else                                                            processRoomUpdate(…)
```

The downstream platform has four verbs — check-in, check-out, **change**, update
(`common/services/InstioGuestEntryServiceImpl.java:86`, `:114`, `:142`, `:170`) —
so a guest moving room mid-stay is not an update of the same fact.

**DND is reset on every room-status message.** `acknowledgeRoomStatusToHK(…,
"FALSE", "DND")` runs unconditionally
(`web/services/OracleWebRoomServiceImpl.java:61`) — a workaround for a PMS that
reports do-not-disturb by omission.

**The guarantee is cached for an hour under the wrong key.** The API is queried
per arrival date; the cache is keyed on `hotelId` alone
(`cloud/services/impl/OracleCloudGuaranteeServiceImpl.java:36-39`, `:52-55`,
`:63`). The first arrival date's cancellation and deposit policy is then served
for every reservation at that property for an hour. Cited here rather than in §7
because the *domain* fact underneath is sound — **guarantee policy is per property
and arrival date, and is stable enough to cache**; it is the key that is wrong.

Guarantee itself is a richer object than "a policy string"
(`cloud/models/OracleCloudReservationGuarantees.java`): `guaranteeCode`, `onHold`,
`reserveInventory`, `defaultGuarantee` (`:13-27`), a deposit policy whose deadline
is an **offset from the booking date** (`:50-52`), and a cancellation penalty
whose deadline is an **offset from arrival plus a drop time** (`:75-79`). Only
two pre-formatted strings survive into the normalised record
(`cloud/services/impl/OracleCloudReservationServiceImpl.java:243-248`).

### 5.6 · Two facts the other providers confirm

Glanced at only, per the brief.

**A webhook needs the internal property id in its own URL.** Both webhook
providers register a per-property callback path and read our id out of it, while
the body carries the *external* property id —
`cb/hooks/reservation/{instio-property-id}/created`
(`cloudbeds/resources/HookResource.java:47-49`) and
`apaleo/hooks/{instioPropertyId}` (`apaleo/resources/ApaleoHookResource.java:33-38`).
The subscription URL *is* the property routing.

**Webhooks are as thin as OHIP's business events.** Apaleo's carries a topic, a
type and an `entityId` and nothing else (`ApaleoHookResource.java:34-37`);
Cloudbeds' carries the reservation id and property id (`HookResource.java:50`).
Both are followed by a read-back. **Across three change-notification mechanisms —
polled queue, webhook, and push — only the push carries the record.** Everything
else notifies and requires a fetch.

HTNG appears as a fourth mechanism, SOAP
(`htng/configuration/HtngSoapConfiguration.java`,
`htng/services/clientadapters/`) — the hospitality industry's own standard, noted
so the design does not assume REST is the whole world.

---

## 6 · The normalised shape the three flavours converge on

Not a model to adopt — recorded because it is the target all three converge on,
and its gaps are informative. `common/models/InstioReservation.java` and
`common/models/RoomInfo.java`:

```text
InstioReservation   reservationId · type · booker · title · firstName · lastName
                    email · phone · address · dob · reservationStatus
                    bookingTime · checkInTime · checkOutTime · cancelledTime
                    cancelledBy · roomCount · totalAmount · currency
                    lengthOfStay · location · lang · country · source · channel
                    sourceCode · travelAgency · travelType · adultCount
                    childCount · providerId · timelineId · referenceId
                    serialNumber · arrivalFrom · arrivalTime · pickupRequired
                    pickupReference · remarks · data(map) · rooms[]
                    companyId · siteId

RoomInfo            subReservationId · serialNumber · name and contact fields
                    room · type · status · checkInTime · checkOutTime
                    lengthOfStay · adultCount · childCount · maleCount
                    femaleCount · domesticCount · internationalCount
                    complimentaryNights · mealPlan · bedType · extraBedCount
                    vip · country · state · city · mainGuest · totalAmount
                    comments · memberId · internalInfo(map)
```

Three observations. The status is a **String** on both, holding `booking` /
`checkIn` / `checkOut` / `cancelled` — while an enum with the right values exists,
unused, three files away (`common/misc/ReservationStatus.java:3-5`, which also has
`WAITING` and `NO_SHOW` that the strings never produce). `vip` is a bare `Boolean`
(`RoomInfo.java:74`) with no definition anywhere — the same unanswered question
this repository parked as `CTX-Q3`. And two untyped maps, `data` and
`internalInfo` (`InstioReservation.java:95`; `RoomInfo.java:90`), are where
everything that did not fit went.

`lengthOfStay` is computed as `toDays(checkOutMillis) - toDays(checkInMillis)`
(`cloud:255`, `onPremise:323`, `web:382`) — days-since-epoch in UTC, subtracted,
irrespective of the property's zone.

---

## 7 · Weaknesses observed, each with its evidence

Facts, not judgements. §8 has the judgements.

### 7.1 · Security

| | Evidence |
|---|---|
| Every endpoint is unauthenticated | `co/instio/global/security/WebSecurityConfig.java:35` — `anyRequest().permitAll()` |
| The one auth filter is opt-in by the caller | `co/instio/global/security/user/AuthenticationFilter.java:38-43` — acts only `ifPresent`; a failure returns silently at `:61-63` |
| Property identity is taken from the request body | `PropertyCode` → `siteId`, `onPremise/models/OracleOnPremiseReservationCreateRequest.java:66-67` |
| The PMS password is a plain column | `cloud/dto/jpa/OracleCloudProperty.java:25` |
| Downstream credentials travel in the query string | `common/services/InstioGuestEntryServiceImpl.java:52-53` |
| The JWT signing key is a literal in source | `co/instio/global/security/JwtTokenService.java:15` |
| An in-memory `admin` / `password` account | `co/instio/global/security/WebSecurityConfig.java:51-54` |
| Every secret is in a checked-in properties file | `src/main/resources/application.properties:31-45` (databases), `:125-134` (OHIP client secret and app key), `:141-143` (provider keys), `:189-193` (broker) |

### 7.2 · Property isolation

The housekeeping request handlers **ignore the property in the request and
substitute a hardcoded id**, twice:

```text
instioService.fetchActiveInstioProperty("6257ef14169aae4120a8e477")
        cloud/services/impl/OracleCloudHousekeepingServiceImpl.java:58 and :72
```

The incoming `request.getSiteId()` is used only in the failure log at `:60` and
`:79`. Both listeners are `@RabbitListener`-annotated and commented out (`:39`,
`:51`), so the path is unreachable as committed — but this is the handler written
for every property's housekeeping traffic.

### 7.3 · Correctness defects found while reading

| | Evidence |
|---|---|
| The event drain loops forever on any status that is neither 200 nor 204 — `eventQueueEmpty` is never set on that path | `cloud/services/impl/OracleCloudEventServiceImpl.java:50`, `:76-79` |
| …and `fetchOracleBusinessEvents` returns `null` on transport failure, into two callers that immediately call `.isEmpty()` | returns `null` at `:84`; dereferenced at `:94` and `cloud/services/background/OracleCloudBackgroundService.java:65` |
| `getReservationFromOracleById` returns `null` on failure and is dereferenced with `.get(0)` | `OracleCloudReservationServiceImpl.java:163`; used at `OracleCloudEventServiceImpl.java:98-100` |
| The force-refresh executor is shut down **inside** its own loop, so only the first reservation of a batch is processed | `OracleCloudReservationServiceImpl.java:73-83` — `executorService.shutdown()` at `:81` |
| The guarantee cache key omits the arrival date it queried by | `OracleCloudGuaranteeServiceImpl.java:52-55` against `:58-59` |
| The forced check-out stamps the check-out with the property's **check-in** time | `onPremise/services/background/OracleOnPremiseBackgroundService.java:62` |
| …and calls `Optional.get()` with no presence check | `:37-38` |
| A blank room number dumps the record and then continues to use it | `web/services/OracleWebReservationServiceImpl.java:250` — no `return` after `dumbReservation` |
| The dump entity declares `dumpReason` while every writer sets the inherited `dumbReason`, so the field is always null | `web/dto/mongo/DumpOracleWebReservation.java:12` against `web/dto/mongo/OracleWebReservation.java:71` and `web/services/OracleWebReservationServiceImpl.java:429` |
| The lock service is a JVM-local `ReentrantLock` map, and its timeout release unlocks from a thread that does not hold it | `common/services/LockServiceImpl.java:17-18`, `:71-80` |
| Pagination is modelled and never read — `totalPages`, `offset`, `limit`, `hasMore`, `totalResults` are parsed, then `.get(0)` | `cloud/models/HousekeepingRoomInfo.java:14-22`; `cloud/services/impl/OracleCloudHousekeepingServiceImpl.java:82` |
| The only paging that exists is a fixed `limit=20` with no cursor | `cloud/services/impl/OracleCloudEventServiceImpl.java:53` |

### 7.4 · Operations that do not do what they are named

| | Evidence |
|---|---|
| `updateOracleProfile` performs a **GET**, ignores its `profile` argument, and logs `PROFILE INFORMATION UPDATED SUCCESSFULLY` | `cloud/services/impl/OracleCloudProfileServiceImpl.java:27-41` |
| `processOnlineCheckInInfo` is an empty method behind a live `POST` that returns success | `cloud/services/background/OracleCloudBackgroundService.java:42-44`; `cloud/resources/OracleCloudOnlineCheckInResource.java:23-28` |
| `addTrace` validates the property and discards the trace, returning success | `web/services/OracleWebRoomServiceImpl.java:66-71`; `web/resources/OracleWebRoomResource.java:31-35` |
| `refreshOracleOnPremiseReservation` logs and returns | `onPremise/services/OracleOnPremiseReservationServiceImpl.java:56-59` |

### 7.5 · Structure

| | Evidence |
|---|---|
| The scheduler that drives the cloud flavour is commented out, and its handler is duplicated verbatim into the manual endpoint | `cloud/services/background/OracleCloudBackgroundService.java:59-89` and `cloud/services/impl/OracleCloudEventServiceImpl.java:88-117` — the same 25 lines |
| The web flavour's reservation processor exists three times: the live version and two commented predecessors, one of them richer than the live one | `web/services/OracleWebReservationServiceImpl.java:73-171`, `:204-247`, `:173-288` |
| An interface method's signature names a class nested inside its own implementation | `cloud/services/OracleCloudHousekeepingService.java:9` |
| A `main()` sits in a service base class and in a shared model | `cloud/services/OracleCloudBaseService.java:149-159`; `common/models/InstioReservation.java:120-122` |
| Every persistence query is native SQL | `cloud/dao/mysql/OracleCloudPropertyRepository.java:14-26`, and the same on the other two flavours |
| The schema is generated from entities at startup | `src/main/resources/application.properties:91` — `ddl-auto=update` |
| Cross-cutting code enumerates every provider by name, so adding a provider means editing it | `modules/housekeeping/service/background/HousekeepingBackgroundService.java:35-45`, `:54-67` |
| One class is both the OAuth wire model and the JPA entity | `cloud/dto/jpa/OracleCloudAuthToken.java:14-29` |
| A route is named after a customer | `oracle/on-premise/zuri` — `onPremise/resources/OracleOnPremiseReservationResource.java:20` |
| `log.error` is used for routine success | `cloud/services/impl/OracleCloudReservationServiceImpl.java:128`; `onPremise:104`; `web:196` — among many |
| `System.out.println` and `e.printStackTrace()` in service code | `web/services/OracleWebRoomServiceImpl.java:83`; `cloud/services/impl/OracleCloudGuaranteeServiceImpl.java:69` |

---

## 8 · Opinions — the stream's, and only this section

Everything above is citable. What follows is judgement, offered so the gap
analysis has a position to argue with. **None of it is a decision, and none of it
is a design.**

**8.1 · The reference's real lesson is not its layering — it is that every hard
problem it hit is a contract problem.** Timeout-as-success exists because there
was no idempotency key. Three overlapping answers to out-of-order events exist
because there was no defined ordering. `forceInsertIntoCrm` fabricates a guest
email because a downstream field was mandatory and the source could not fill it.
Each is a missing agreement between two components, patched at the call site. Our
connector platform will meet the same PMS behaviour; whether we meet it with a
contract or with a patch is ours to decide now.

**8.2 · "The connector normalizes PMS differences" is doing more work than
Chapter 10 admits.** The chapter's `Reservation` model has six fields
(Chapter 10 §PMS normalization). One vendor, three deployments, needed four
status vocabularies, four orthogonal room statuses, three date formats, a
property time zone, two clock times of property configuration,
expected-versus-actual timestamps, several ids per reservation and several per
guest, and a guest identity that is a search across four `primary` flags. The
normalisation model is the hard part of this round, not the pipeline around it.

**8.3 · Housekeeping's four axes should survive normalisation intact.**
Front-office occupancy, housekeeping cleanliness, the reservation list and the
department's own status are independently useful, and `VACANT_REFRESH` and
`STRIP_LINEN` are only computable while all four are present. Collapsing them
early is the one modelling mistake that cannot be undone downstream.

**8.4 · The push flavours are the security case, not the pull one.** OHIP is a
credential we hold and an address we dial. The on-site flavours are an endpoint a
hotel's network posts to, identified by a property code in a JSON body — and, in
the reference, by nothing else. The inbound half needs authentication independent
of the payload, because the payload is what an attacker controls.

**8.5 · The dead letter deserves to be first-class, and to distinguish two things
the reference conflated.** `reservation-history` holds both "unprocessable, a
human must look" and "superseded, keep for audit" under one `dumbReason` string.
Those want different retention, different alerting and different replay semantics.

**8.6 · The absent mapping table is the reference's largest data-layer gap, and
it is the one our constitution already closed.** PMS room numbers and reservation
ids are carried verbatim into the downstream platform, so a room renumbering in
OPERA silently repoints every record. ADR 0016 already requires property-scoped
bijective external mappings; this is the concrete failure that rule prevents.

**8.7 · Three flavours of one vendor argue that the connector unit is not the
vendor.** `oracle-cloud`, `oracle-onpremise` and `oracle-web` share a status
parser and nothing else — different transports, different directions, different
capability sets, different credentials. Any packaging decision that assumes one
connector per PMS brand meets this immediately. Flagged as an input to `CONN-Q1`,
which is the planner's and not this stream's.

---

## 9 · What this study deliberately does not contain

* **No design.** Deliverable 3 — the Integration Hub's design, in the platform
  repository (`docs/working/42-the-connector-round.md` §3.3) — is built from our
  diagrams and chapters with the gap analysis applied. Nothing here proposes a
  tree, a proto, a schema or a service.
* **No gap analysis.** Deliverable 2, `docs/working/42b-gaps-against-our-design.md`
  in the platform repository, classifies each finding against our chapters and
  diagrams. §8 above is opinion, not classification, and does not pre-empt it.
* **No ruling on the round's gates.** Which PMS ships first is the owner's, and
  how a connector ships is `CONN-Q1`. **Where reservations live is not one of
  them** — architect ruling, 2026-08-30: `CTX-Q2` **is ruled** (Chapter 26 /
  ADR 0089 — guest and reservation belong to the future Reservations/GuestOps
  domain). What remains is that **the domain is unbuilt**, so a normalised
  `reservation.*` event has no owner to land in yet: a build prerequisite, not a
  ruling to seek.
* **Nothing copied.** No source, no schema, no test fixture.

# Room Care — where a person meets an identifier

**ADR 0339 §6, with §7's labels. Identification only: nothing here was built, and a picker is refused by
name** — *"a missing chooser is a capability gap to raise, not permission for the application stream to
create one locally."* Swept 2026-09-29.

The owner's rule this answers: **a person never types, pastes or reads an identifier. Anywhere a command
needs an id, the surface offers a named choice, and the surface resolves the id itself.**

---

## 1 · The one operation where a person must go and get an identifier

| | |
|---|---|
| **Operation** | Grant property-wide Room Care access — `roomcare.configure` · `grantManager` |
| **Entity** | a person — staff, by their login |
| **Authoritative chooser** | **`SearchStaff` — ruled in ADR 0339 §2 and contracted in ADR 0340, not built, and not reachable.** `CORE-Q41` capability gap |
| **Persisted** | `userId`, the stable identifier. Correct as the value; wrong as the thing a person supplies |
| **Class** | **a human chooses an entity → chooser REQUIRED** |

`ui/screens/setup/access.ts:83` is a bare text input, and the screen says the quiet part itself:

> **"Room Care keeps no directory of people; name the person by their login id, as Core Administration
> shows it."**

**A person opens another application, finds a GUID, and pastes it.** That sentence is honest and it must
survive until the chooser exists — it is the only thing on that screen telling the person what it
actually needs.

`revokeManager` takes the same `userId` and needs no chooser: it comes from the row they pressed.

---

## 2 · Where a human chooses and a local chooser already exists — left alone

| | |
|---|---|
| **Operation** | Assign or reassign a room — `roomcare.assign` · `assign` (`ui/screens/prepare/move.ts:37`, `ui/screens/room/acts.ts:85`) |
| **Entity** | an attendant |
| **Chooser** | a `<select>` of names built from Room Care's **own** `attendants` read of Workforce postings. Authoritative: `SearchStaff`, same gap as above |
| **Persisted** | `userId` — never the name |
| **Class** | **chooser REQUIRED** |

**Ruled: it stays, untouched** (architect, 2026-09-29). ADR 0339's words are prospective — an application
uses an authoritative chooser or raises the gap *"before implementing a local picker"* — and this one
predates the ruling. Consolidating or improving it locally is the thing refused by name; the platform
chooser replaces it.

**What it cannot do, stated rather than defended.** It lists the people posted to Housekeeping today. It
cannot search by department or by role, so it could not pass ADR 0339's sign-off rule for staff — *finding
"John" by typing "John" is not sign-off: John, Housekeeping and Room Attendant.* Leaving it is only
honest while that is written down.

---

## 3 · Everything else, and why none of it needs a chooser

**Fixed vocabulary → catalogue selector, not entity search**

| Identifier | Operations | Where the person meets it |
|---|---|---|
| `zoneId` | `assignZone` | a select of this property's zones |
| `roomTypeId` | `saveService`, `saveDeepCleanPlan`, the `services` read | room-type chips across the top of the tab |

Both vocabularies are Master Data's, and both are already drawn as a closed list rather than a search.

**Already established by the current context → no chooser (list-and-click)**

| Identifier | Operations |
|---|---|
| `taskId` | start · pause · attempt · extra time · restock · issue · skip · defer · reduce · reprioritise · unassign · inspect · the door read |
| `roomId` | the room read · `clearDisagreement` · `planDeepClean` · `saveStates` |
| `roomIds` | `assignZone` — ticked from the rooms already on screen |
| `supervisionId` | `decide` |
| `deepCleanId` | `cancelDeepClean` |
| `locationId` | `saveArea` — reached by *Edit…* on the area's own row |

**Internal or transitive → no chooser**: `stayId` (`restock`), `mediaId` (`issue`, and its Photo control
is drawn off reading *"Photo — not available yet"*).

> **THE CLASSIFICATION IS READ, NOT COUNTED.** A first attempt to count the operations mechanically
> returned 19 and missed `zoneId`, `locationId` and `roomIds`, because those cases have block bodies and
> the operation-tracking regex followed only expression-bodied arms. The table above comes from reading
> the four files in `backend/src/Module/Capabilities/` directly. A count from an instrument that has been
> disproved measures the instrument.

---

## 4 · The labels — the half a text guard can hold

ADR 0339 §7: no label, placeholder, column heading, helper text or error message names an identifier.
**Three strings in the whole application do, and all three are the one dialog above.**

```text
ui/screens/setup/access.ts:83   "Person (id)"                                  the label
ui/screens/setup/access.ts:83   "…name the person by their login id, as Core   the helper text
                                 Administration shows it."
ui/screens/setup/access.ts:90   "name the person by their login id"            the error text
```

`backend/src`: **zero**, measured with an armed control (below). No column heading, placeholder or
refusal message names an identifier anywhere else; `tests/RefusalWordsTests.cs` has guarded the backend's
half since the refusal-words round.

**THE LABEL AND THE MECHANISM LAND TOGETHER, AS ONE ITEM.** Fixing the labels first would leave
`[ Staff member ▾ ]` over a box that still wants a GUID — a worse screen than the honest one there now,
because **a corrected label over an uncorrected mechanism removes the only true thing on the screen.**
No guard is added here for that reason: a guard would have to be suppressed for this dialog until the
chooser exists, and a suppressed guard on the one site it was written for is worth less than this
paragraph.

---

## 5 · The platform gap, measured from inside this application

**No installed application's module can reach a chooser, whatever Context grows.** Master Data publishes
`ListStaff`, `GetStaff`, `ListLocations` and `GetLocation`; no `SearchStaff` exists. And an installed
module's only door is `host.call(capability, method, params)`, which is scoped to the capabilities the
application declared in its own manifest — Room Care declares `roomcare.*`, `room.clean` and
`room.inspect`. Measured across all five application UIs in `HotelOsApps`: `host.call` appears **once**,
in Room Care's own loader (`ui/chrome/load.ts:43`). Nothing calls anything of another domain's, because
nothing can.

*PP reached the same conclusion from the other end — no desktop Tauri command and no bridge capability
reaches `ListCatalogue` today. **The chooser capability existing in Context is not sufficient: the module
bridge has to expose it, and today it exposes nothing of the kind.*** Recorded here because it is a gap
in ADR 0339's Phase 1 that this sweep found from inside an application, and it is with CC, PP and NN.

---

## 6 · The instrument, including the run that was wrong

**A zero is a claim about the instrument first.** Both halves of this sweep were proved against a known
positive before their results were read, and the first one failed.

**The label sweep, first attempt — BLIND.** The pattern was `"([^"]{2,140})"`, and its positive control —
`Person (id)`, a label read by eye first — came back **not found**. A length filter *inside* the quote
pair lets the engine re-pair quotes across two adjacent literals: on a line holding several strings it
walks out of alignment and silently skips some of them. **That is exactly where labels live**, beside a
class name and a tag name in the same `el(...)` call. Corrected to plain pairs, `"([^"]*)"`, with the
length filter applied afterwards; control found at `access.ts:83`; result then trusted. Template literals
are swept too, because half of Room Care's sentences are built with one.

**The backend sweep — a planted control, because the real one lives elsewhere.** The UI's control string
does not exist in `backend/src`, so a planted `.cs` holding both offending strings was swept first: 2
found, then 0 across the backend. A zero on a side you did not write needs its own armed control.

**The operation sweep — validated on a field known to exist.** The identifier-parameter pattern was run
first against `body.Id("locationId")` in `StandardCapabilities.cs:36`, the same shape HH confirmed in
Jobs, before any absence was believed.

*The next sweep of Room Care should start from the corrected label pattern rather than write a new one.
It is the one that has been shown failing.*

---

## What is owed, and by whom

* **`SearchStaff`, and a module bridge that exposes it** — the platform's, not Room Care's.
* **When it exists**: the grant dialog's mechanism and its three strings land in one change, and the
  attendant `<select>` is replaced rather than improved.
* **Sign-off is a live walk, per chooser** (ADR 0339): open · search every ruled dimension · select ·
  verify the contextual presentation · save · reopen and verify human-readable identity · change · clear ·
  no results · Context unavailable · paging. For staff that means John **and** Housekeeping **and** Room
  Attendant, not a name that happens to match.

# Jobs — the capability ledger

**What this is.** Every screen Jobs draws, every control on it, and whether that
control reaches an operation the service actually answers. Derived from the code
at `c3eadcfe`, not from memory: the operations are extracted from
`backend/src/Module/*.cs`, the calls from `ui/**/*.ts`, and the two lists are
compared mechanically. Where a number appears below, it was counted.

**For the owner, before they walk the installed build.** A control drawn `off`
is not broken — it is a control with nothing behind it yet, drawn so it cannot
be pressed and saying why. A control that looked live and did nothing was the
defect the owner found on 2026-09-19; there are none left in Jobs, and a guard
presses every control on every screen to keep it that way
(`ui/tests/dead-controls.test.ts`).

---

## 1 · What the service answers

**44 operations**, in eight permissions. The reads are what a screen draws from;
the writes are what a control does.

| Permission | Operations |
|---|---|
| `job.read` | `today` · `board` · `job` · `live` · `scheduled` · `catalogue` · `settings` · `me` · `jobsNow` · `widgetBoard` · `widgetBlocked` · `widgetPriority` · `widgetDue` · `widgetRaised` |
| `job.create` | `raise` |
| `job.assign` | `accept` · `take` · `reassign` |
| `job.complete` | `start` · `pause` · `resume` · `stop` · `resolve` · `close` · `reopen` |
| `job.cancel` | `cancel` |
| `job.amend` | `hold` · `release` · `note` · `remind` · `link` · `amend` · `readNudges` |
| `job.configure` | `savePolicy` · `deletePolicy` · `saveHours` · `savePresence` · `saveSubscriptions` · `saveHold` · `saveClosing` · `saveItemPolicy` · `grantJobsManager` · `revokeJobsManager` |
| `job.curate` | `saveCategory` · `saveItem` · `addResolution` |

Every one is authorized by the Kernel before it runs: the module envelope checks
the permission the call is made under, and a refusal is the Kernel's, never this
application's guess.

---

## 2 · Each screen, and what its controls do

### Board — `ui/screens/board/index.ts`

| Control | What happens | Where |
|---|---|---|
| The six filters | redraw the list from `board` | `:130` |
| **My departments** | **drawn off** — "your departments aren't known yet". Nothing tells Jobs which departments a person belongs to; the service answers it with no rows by design (`JobQueries.cs:37`). The Board opens on All departments | `:140` |
| A row's job number | opens that job (`job`) | `:165` |
| ＋ Raise a job | opens the Raise form | `:141` |
| The pager | redraws the page from `board` | `:181` |

### Raise a job — `ui/screens/raise/index.ts`

| Control | What happens | Where |
|---|---|---|
| Raise the job | `raise` under `job.create` | `:79` |
| Every field | drawn from `catalogue` | `:23` |

### One job — `ui/screens/job/`

| Control | What happens | Where |
|---|---|---|
| Start work · Pause · Stop | `start` · `pause` · `stop` | `index.ts:210, 134, 135` |
| Accept · Take it | `accept` · `take` | `index.ts:218, 231` |
| Put on hold… · Resume | `hold` · `release` | `index.ts:227, 225` |
| Cancel job… | `cancel`, after a confirmation | `index.ts:238` |
| Resolve… | opens Resolve, which sends `resolve` | `resolve/index.ts:95` |
| Add note | `note` | `notes.ts:40` |
| **Link a job… · Add a step… · Unlink** | **drawn off** — "linking and adding steps aren't available here yet". The service answers `link`, but choosing which job needs a picker no approved frame draws; unlinking and adding a step have no operation at all | `links.ts:24, 57` |
| **Remind me…** | **drawn off** — "setting a reminder isn't available here yet". The service answers `remind`; choosing when needs a form no frame draws | `record.ts:28` |
| Adding a photo | **not available** — the tooltip says so; there is no media client | `notes.ts:52` |

### Live · Scheduled · Catalogue

| Control | What happens | Where |
|---|---|---|
| Live | draws from `live`; no writes | `live/index.ts:18` |
| Scheduled | draws from `scheduled`, paged | `scheduled/index.ts:27` |
| Catalogue ＋ New · Create category | `saveCategory` | `catalogue/index.ts:81` |
| ＋ Add resolution | `addResolution` | `catalogue/index.ts:131` |
| New item · Create item | `saveItem`; **Cancel** clears the form; a refusal speaks **inside the composer** (ADR 0225 §4), as it does for a new category and a new resolution | `catalogue/index.ts:192` |
| **An item's Edit** | **drawn off** — "editing an item isn't available here yet". `saveItem` exists; the edit form is not drawn | `catalogue/index.ts:105` |
| **This property · Import / export** | **drawn off** — neither view is built | `catalogue/index.ts:39-43` |
| An item's resolutions | a list, not controls | `catalogue/index.ts:119` |

### Settings — `ui/screens/settings/`

| Control | What happens | Where |
|---|---|---|
| Shifts & presence → Save | `savePresence` | `tabs.ts:100` |
| Service hours → Save the hours | `saveHours` | `tabs.ts:109` |
| Holds & reminders → Save | `saveHold` | `tabs.ts:168` |
| Closing & rating → Save | `saveClosing` | `tabs.ts:190` |
| **Access → Choose a person…** | **drawn off** — "choosing a person isn't available here yet". ADR 0225 §1 (`JOBS-Q4`, owner, 2026-09-22): a person is chosen by name and an identifier is never the selection, so the `User id` field is gone. The chooser is Context's staff search (ADR 0224), which does not exist yet — measured 2026-09-23, Context answers eight RPCs and none searches. `grantJobsManager` is unchanged and waiting. **Granting is unavailable on the property until the chooser lands** | `tabs.ts:267` |
| Access → Revoke… | `revokeJobsManager`, after a confirmation | `tabs.ts:237` |
| **A policy row's Edit** | **drawn off** — "editing a policy from this list isn't available yet" | `policies.ts:87` |
| **＋ step (the ladder)** | **drawn off** — "adding a step isn't available here yet"; no operation exists | `policies.ts:164` |
| **Add a step to P1** | **drawn off**; the composer beside it is drawn but not built | `policies.ts:173` |
| Concern policy → Save policy | **drawn off** — "Saving a new policy is not built yet"; it returned to the list and saved nothing | `policies.ts:103` |

### The six widgets — `ui/widgets/`

Each draws from its own read (`jobsNow`, `widgetBoard`, `widgetBlocked`,
`widgetPriority`, `widgetDue`, `widgetRaised`) and opens the Board. A widget
that cannot read says so on the card, in the same six states the screens use.

---

## 3 · Built, and not reachable from any screen

**Twelve operations the service answers that no control calls.** Measured by
comparing both lists, not recalled:

```text
amend        close        deletePolicy   link
readNudges   reassign     remind         reopen
resume       saveItemPolicy   savePolicy   saveSubscriptions
```

Four of them sit behind controls drawn off above (`link`, `remind`,
`savePolicy`, and `saveItemPolicy` through the item's Edit). The rest have no
surface at all: closing and reopening a resolved job, reassigning, resuming from
the Work tab, marking nudges read, deleting a policy, and the subscriptions
behind "Who is told".

**None of this is a defect by itself** — it is work not yet drawn. It is here so
the owner's walk knows what is absent by design rather than broken.

---

## 4 · What the service does not send, that a screen draws

| Screen | What it draws | What the service sends |
|---|---|---|
| One job · Record | who created and who last changed the job (frame 2g) | **the names, since ADR 0225 §2** — `Created by` and `Updated by` as values, joined to each instant by the screen |
| One job · Overview | the stay, "departs …" | nothing — no stay line |
| Settings · Access | the grant list | names and times, since `2d168ab4`; the person to grant is still typed in as an id, for want of a person picker |

---

## 5 · The guards that keep this true

| Guard | What it refuses |
|---|---|
| `ui/tests/dead-controls.test.ts` | any enabled control with no observable effect, on every screen and on whatever a press opens |
| `ui/tests/developer-content.test.ts` | a register id, an ADR, a design section, a code identifier, a system's name, a raw id or an unformatted instant, anywhere a person reads |
| `ui/tests/wire-shaped.test.ts` | a backend value drawn as the wire carries it |
| `backend/tests/RefusalWordsGuardTests.cs` | a refusal naming a wire field, a document or a system |
| `backend/tests/ZoneSourceGuardTests.cs` | a day or a wall-clock time computed in UTC |

Each pattern in each guard names a planted example it must catch, and each guard
has been shown failing before its green was counted.

/**
 * What the certificate asserts beyond the pairs — the two conformance tables.
 */

/** One row of the pagination table. */
export interface PagedRow {
  screen: string;
  draws: string;
  why: string;
  pair: string;
  rule: string;
}

/**
 * Every screen that draws a list, and what it draws at the foot of it.
 *
 * **This table is checked by `tests/pagination.test.ts`, not by reading it.**
 * The test walks `screens/`, finds what draws a list, and fails when a screen
 * is not classified here, when a row outlives its screen, and when the set of
 * screens calling `pager()` is not exactly the set marked *numbered*. A screen
 * with a list and no row is a finding, and that is where it becomes one.
 */
export const PAGINATION: readonly PagedRow[] = [
  {
    screen: "Today",
    draws: "Numbered pager, and it draws on a single page — showing 1–14 of 14 with both arrows disabled",
    why: "The day's four lists are bounded and countable, so the total is a fact the wire can answer.",
    pair: "1, 11",
    rule: "§6 — paged is the default; §6 “It draws on a single page too”",
  },
  {
    screen: "Bookings",
    draws: "Numbered pager — showing 1–9 of 218",
    why: "Everything the property has ever sold. Bounded, countable, and the one list long enough to page in earnest.",
    pair: "2",
    rule: "§6 — is the count a fact, or a moving target?",
  },
  {
    screen: "The booking",
    draws: "Numbered pager, 12 stays a page — showing 1–2 of 2, arrows disabled",
    why: "It drew none, on the argument that a booking bounds its own stays and a pager under two rows is furniture. Refused: a coach party of forty is the same screen, and `showing 1–2 of 2` is what tells somebody checking a group that the booking is whole rather than truncated.",
    pair: "8, 9",
    rule: "§8 — every list screen, ruled 2026-09-09",
  },
  {
    screen: "New booking",
    draws: "Numbered pager, 12 room types a page — showing 1–3 of 3",
    why: "A catalogue is bounded by what the hotel has, which is a property of this hotel and not of the screen: a resort's catalogue is not three rows. The list is paged in the backend rather than sliced in the module, so the count is the property's own.",
    pair: "14",
    rule: "§8 — every list screen, ruled 2026-09-09",
  },
  {
    screen: "The stay · Activity",
    draws: "Deliberately none",
    why: "One stay's history. Bounded by the stay, and read newest-last as a story rather than paged.",
    pair: "4",
    rule: "§6 — bounded by a natural key",
  },
  {
    screen: "Attention",
    draws: "Numbered pager, 10 cards a page — showing 1–4 of 4 on this fixture",
    why: "It used to draw none, on the argument that a property with enough exceptions to need a second page has a problem a pager would help it not to look at. That was refused: bounded by a natural key is a property of today's data, not of the screen, and the count is information in its own right. The backend now merges its two sources by time and pages the union — a total taken from what was fetched would stop growing at the cap.",
    pair: "12",
    rule: "§8 — every list screen, ruled 2026-09-09",
  },
];

/** One row of the widget table. */
export interface WidgetRow {
  name: string;
  entry: string;
  answers: string;
  target: string;
  filter: string;

  /** The frame's number in `03-guestops-widgets.html`. */
  frame: string;

  /** Whether the shell may bury it under another — page 56's stack rule. */
  stacks: string;
}

/**
 * The five this application registers, and what each is for.
 *
 * **Every one of them is a finding**, and the reason is the same for all five:
 * see `WIDGET_FINDING`. They are listed in full anyway, because the owner's
 * rule is that a gap is named rather than met with silence.
 */
export const WIDGETS: readonly WidgetRow[] = [
  {
    name: "Today at the Desk",
    frame: "1",
    stacks: "Yes — glanced at between guests",
    entry: "today",
    answers: "What is the shape of the shift, and who walks in next?",
    target: "the stay",
    filter: "stay/{stayId} — the arrival's own stay",
  },
  {
    name: "Occupancy",
    frame: "2",
    stacks: "Yes — you go and look at it",
    entry: "occupancy",
    answers: "How full is the hotel tonight, and where?",
    target: "the rooms of one type",
    filter: "rooms/{roomType} — the type the row names",
  },
  {
    name: "From the PMS",
    frame: "3",
    stacks: "No — stackable: false; it makes silence visible",
    entry: "from-the-pms",
    answers: "Is the feed still sending, and what could it not place?",
    target: "Attention",
    filter: "attention/{stayId} — the held fact's own stay",
  },
  {
    name: "Business Mix",
    frame: "5",
    stacks: "Yes — a manager goes and looks",
    entry: "business-mix",
    answers: "Where did today's arrivals come from?",
    target: "the day's arrivals",
    filter: "arrivals/channel/{name} and arrivals/market/{code} — in the source's own words, never normalised",
  },
  {
    name: "Watchlist",
    frame: "4",
    stacks: "No — stackable: false; it has to catch you",
    entry: "watchlist",
    answers: "What was nobody thinking about?",
    target: "the stay",
    filter: "stay/{stayId} — the overdue departure, or the guest still waiting",
  },
];

/**
 * What the live platform said when this application was actually run.
 *
 * The gallery above is the design half of APPS-Q4. This is the other half, and
 * it is recorded here rather than in a report because a certificate that shows
 * seventeen rendered screens and says nothing about whether the thing starts is
 * a certificate about drawings.
 */
export const DRIVE = {
  title: "Part B — what the platform answered, and where it stopped",

  answered:
    "**The Kernel knows this application and approves all of it.** "
    + "`hotelos-kernel package status guestops` against the development "
    + "installation answers `guestops 0.1.0`, `signed by guestops-dev`, and "
    + "**nine permissions, every one approved** — `reservation.read`, "
    + "`stay.create`, `stay.assign`, `stay.override`, `guest.amend`, "
    + "`registration.capture`, `request.handle`, `reporting.file`, "
    + "`desk.configure`. That is the manifest this package declared, accepted at "
    + "install and read back from the platform's own registry rather than from "
    + "the manifest it was written in.",

  ran:
    "**And it ran.** Its own log is 64 lines spanning 308 milliseconds with "
    + "**zero** errors, exceptions or exit records: `/health` 200, "
    + "`DiscoverService` 200 against the Kernel, `refreshed 1 signing keys from "
    + "Identity`, and all three subscriptions consuming — "
    + "`guestops-GUEST`, `guestops-MAINTENANCE`, and `revocation-guestops`. "
    + "The manifest's `subscribes` rows are the first real subscriber start "
    + "against the consumer gate, and they passed.",

  stopped:
    "**It stopped, and every reason since turned out to be something other than "
    + "this application.** `failed` was the supervisor recording a lost child — "
    + "the log holds no failure — and a restart then reproduced "
    + "`application_not_resumed package=guestops error=secret "
    + "packages/guestops/database not found` twice. Both were platform findings "
    + "and both are closed: CC's lifecycle fix sealed the credential in the right "
    + "namespace, and the Kernel exits that framed all of it were **the agent "
    + "harness's job object reaping its children** — `KILL_ON_JOB_CLOSE` — never "
    + "a Kernel fault at all. The dev stack is the owner's to start from a real "
    + "terminal now.",

  since:
    "**Re-scored under ADR 0148, and the served column is zero.** *Served* means "
    + "the requested operation completed according to its contract; RPC "
    + "reachability alone is not enough, and a validation refusal is not enough. "
    + "**No operation of GuestOps has ever completed against the live platform.** "
    + "Every capability answers 403 because no authorization tuple names an "
    + "operator, so there is nothing to put in the column and the honest figure "
    + "is 0 of 9.",

  cause:
    "**The zero carries its cause, because two absences have opposite "
    + "remedies.** *Blocked on a grant nobody has issued* and *tried and failed* "
    + "read identically as a bare zero, and a reader meeting one assumes the "
    + "second. This is the first: the `SetGeneralManager` grant has not landed, "
    + "and until it does no capability of any of the three applications can "
    + "complete. Nothing here has been attempted and refused on its merits.",

  envelope:
    "**The envelope probes are their own row and prove their own thing.** Ten "
    + "ran live against the application's door — no token, forged token, unmapped "
    + "method, unmapped capability, GET on a POST route, chunked without a type, "
    + "HEAD, OPTIONS, `Basic`, and a path traversal in the method segment — "
    + "answering 401 · 401 · 401 · 404 · 405 · 401 · 405 · 405 · 401 · 401, "
    + "**every refusal zero bytes**, with the mTLS door refusing an "
    + "unauthenticated caller at the handshake. Under ADR 0148 a refusal is not a "
    + "served operation, and these are all refusals: **they are evidence the "
    + "envelope works, not that any capability was served.** Kept apart so one "
    + "column's evidence is not read as another's.",

  completed:
    "**What did complete, and it is the platform's own machinery rather than "
    + "this application's contract.** GuestOps installs from a HEAD-built "
    + "package, is adopted by the Kernel at start (`application_resumed`), "
    + "answers `/health` on its own door, and consumes all three subjects — "
    + "`guestops-GUEST`, `guestops-MAINTENANCE`, `revocation-guestops`. The "
    + "desktop enrols against the development installation and holds it across "
    + "restarts. None of that is a capability completing, and none of it is "
    + "counted as one.",

  blocked:
    "**One thing holds the rest, and it is not this application.** Part B's "
    + "second column — reachable-by-a-person — needs the same grant the first "
    + "does, and both stay empty until a tuple exists. The two findings that used "
    + "to sit here are closed: the desktop's single `machine.json` became a "
    + "selection plus a per-installation file (`SHELL-Q43`), and the resume "
    + "secret was CC's. **Neither was routed around**, because a certificate that "
    + "reports a drive performed through a workaround describes a platform nobody "
    + "will ship.",
};

/**
 * The Part A run, with the key it was measured under.
 *
 * **The key is quoted because a count without one is not comparable** —
 * `ARCH-Q12`. Two figures taken under different pairing rules are two different
 * measurements wearing one word, and this certificate has already withdrawn two
 * numbers for that reason.
 */
export const PART_A = {
  key: "tag + normalised text, then document order inside a colliding group "
     + "(ARCH-Q12 step two, 2026-09-10)",

  counts:
    "**Seventeen frames, both sides swept in one pass, and the columns close on "
    + "each side exactly:** `drawn 929 = 715 paired + 7 refused + 207 drawn-only`, "
    + "and `built 1060 = 715 paired + 8 refused + 337 built-only`. Of the 715 "
    + "paired, **634 identical and 81 differing**.",

  withdrawn:
    "**This certificate said `0 unpaired`, and that was false.** The figure came "
    + "from grepping the compare output for a section called `UNPAIRED`, which the "
    + "instrument never prints — it prints `DRAWN, NOT IN THE BUILD` and `IN THE "
    + "BUILD, NOT DRAWN`. A pattern that matches nothing returns zero, and the zero "
    + "was read as a measurement. It is the same fault as reading an exit code "
    + "through a pipe: **the tool was not consulted, and its silence was quoted as "
    + "its answer.** Adding the columns up is what caught it, which is why the "
    + "arithmetic is printed above rather than summarised. **And `0 collapsed` is "
    + "structural rather than earned** — under `ARCH-Q12` step two every node lands "
    + "in paired, refused or an unpaired list, so the collapsed count has nowhere "
    + "left to be non-zero. It is printed because the key requires it and it is "
    + "evidence of nothing.",

  blind:
    "**Two frames the instrument cannot serve, flagged rather than absorbed.** "
    + "Frames 10 (Walk-in) and 15 (Registration) carry **99 and 107 built-only "
    + "nodes**, and every one is a guest name from the day behind the overlay: the "
    + "build renders the sheet over a full fourteen-row list and the drawing draws "
    + "it over a shorter one. Convention 4 names this exactly — *a handful of rows "
    + "is a finding, and twenty rows of proper nouns is a fixture mismatch wearing "
    + "one*. **It is not a build divergence**: both sides are right about the "
    + "overlay, and the day beneath it is a fixture the two do not share. It is "
    + "§8 clause 2, and it is the owner's, because closing it means drawing the "
    + "day behind two approved frames.",

  aligned:
    "**The drawings were aligned to `ARCH-Q20` first, and the alignment is why "
    + "this number means anything.** This module's frames drew every control as a "
    + "`<div class=\"btn\">` where the build renders a `<button>` — 187 of them: "
    + "50 buttons, 112 tabs, 14 pager steps, 11 inline links. The sweep pairs on "
    + "tag and text, so the drawing's *control* paired against the build's row "
    + "*label* wherever both said the same words. That was this stream's own "
    + "finding this morning and it was GuestOps' own defect.",

  effect:
    "**Two collision classes disappeared rather than shrinking.** "
    + "`frame-heading-collision` and `bar-count-collision` accounted for seven "
    + "differences under the old key and account for none now — a heading at "
    + "weight 900 and a bar count in `--color-warn` were both artefacts of a "
    + "control that could not be told from prose. Paired rose 535 → 715 because "
    + "187 controls became comparable, not because anything was built.",

  carried:
    "**The styling followed the tag, which is the caveat that cost another stream "
    + "a run.** A `<button>` inherits a UA font, line-height, border and "
    + "background that a `<div>` never had, so `.tab` gained `border:0`, "
    + "`background:none` and a cursor, `.link` gained the full `font: inherit` "
    + "reset, and a `button` rule carries the two the build's own reset carries. "
    + "**Nothing was retagged to make a divergence pair** — every change is to "
    + "the element type the build already uses, and the pager's `off` class kept "
    + "its rule beside the `[disabled]` attribute rather than being renamed into "
    + "agreement.",

  drove:
    "**And the harness now asserts it arrived.** A drive step that matched "
    + "nothing used to return quietly, so a stale selector photographed a "
    + "different screen — convincingly, because the capture was of a real screen. "
    + "It throws now, and the failure is *drawn into the frame* rather than left "
    + "to a timeout: a timeout reads as a broken harness and carries no sentence. "
    + "All seventeen reached their screen on this run, and that is a measured "
    + "fact rather than an assumption.",
};

/**
 * What the canvas measurement found, and what now guards it.
 *
 * Recorded because the owner asked the question the suite could not answer:
 * *"i can see a scroll bar near widget thats in design?"*
 */
export const WIDGET_CANVAS = {
  title: "The canvas holds what is drawn in it — measured, both sides",

  body:
    "A widget's canvas is **320×384 and does not scroll**. Page 56 gives it a "
    + "guaranteed size and the widget does its own cutting; ADR 0111's scrollbar "
    + "rule leaves it nothing to hint with. So a body that overflows is cut "
    + "**silently** — the rows are drawn, nobody sees them, and nothing on screen "
    + "says they exist. All ten panes above, five drawn and five built, now "
    + "measure `scrollHeight === clientHeight` on the canvas and on the body "
    + "inside it.",

  consequence:
    "**Two real clips were found this way and neither was visible to the suite.** "
    + "Business Mix drew every channel and every market — seven rows into a body "
    + "holding five, **44px cut**. Occupancy drew `now.types` entire, bounded by "
    + "nothing but how many room types a property configured: three on this desk, "
    + "and a resort with eight would have clipped in production while every test "
    + "stayed green. Both are bounded now, each label carries its own cut "
    + "(*top 3*), and `tests/widget-bounds.test.ts` walks `widgets/entry/` and "
    + "fails on any list drawn without a bound — so the sixth widget is covered "
    + "the day somebody writes it, not the day somebody remembers to look.",
};

/**
 * The finding that covers all five, stated once.
 *
 * It is the **inverse** of the one HH is reconciling for Jobs, and worth
 * putting that way round: Jobs drew one widget frame and registers one, which
 * is a design gap of four. GuestOps drew **none** and registers five — so every
 * widget in this package ships against a mechanism rather than against a
 * drawing anybody audited.
 */
export const WIDGET_FINDING = {
  title: "Five built, then drawn — the design gate taken late",

  body:
    "These five were **built before they were drawn**. Every other GuestOps "
    + "surface went through a frame the owner audited; the widgets went through "
    + "design page 56's mechanism and the SDK's contract instead — both of which "
    + "they satisfy, and neither of which is a drawing anybody looked at. Five "
    + "frames now exist in `docs/mockups/03-guestops-widgets.html` and are "
    + "offered as one audit, drawn on page 56's own canvas from the same fixture "
    + "facts these captures use.",

  consequence:
    "**The frames are not approved yet, so this stays a finding.** And the honest "
    + "risk in them is circularity: drawn from what exists, they invite approval "
    + "of what exists. They were drawn anyway because the alternative — five "
    + "fictions, then a reconciliation — audits nothing real. **If a frame is "
    + "wrong, the build changes**; from the moment they are approved they are the "
    + "specification, exactly as the seventeen are.",
};

/**
 * What the runs found, and the key each was measured under.
 *
 * **Separated from `conformance.ts` on 2026-09-26, which said of itself that
 * it held "the two conformance tables" while holding six things.** The
 * one-line summary needed an "and" joining two different nouns, which is ADR
 * 0038's own test for when a file is two files — and the header had been
 * stating the smaller half for weeks.
 *
 * The seam is what each kind of entry IS:
 *
 * ```text
 * conformance.ts   what a surface OWES      a declaration table, walked by
 *                                           tests/pagination.test.ts, which
 *                                           fails on a screen with no row
 * this file        what a RUN found         evidence, with its key, which no
 *                                           walker can derive from the DOM
 * ```
 *
 * **A declaration is checkable against the code; a measurement is not.** A
 * table saying *this screen owes a pager* is refuted by opening the screen; a
 * record saying *the Part A run paired 587 nodes under this key* can only be
 * refuted by another run, and it carries its key for exactly that reason.
 * Keeping them in one file invited the next author to walk the second half.
 *
 * **It was over the ceiling for sixteen days and nothing could see it** — the
 * 300-line ceiling had never been enforced on an installable application
 * until ADR 0259 gave the standards check explicit roots. The split is
 * ordinary; its lateness is not.
 */

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
    "**Every figure here is read from `--compare --json`, not from the readable "
    + "report.** Seventeen frames, both sides swept in one pass: "
    + "`drawn 929 = 715 paired + 7 refused + 207 unpaired`, and "
    + "`built 1060 = 715 paired + 8 refused + 337 unpaired`. Of the 715 paired, "
    + "**660 identical and 55 differing**, and the instrument reports `closes` "
    + "true on every frame — the arithmetic this certificate used to compute by "
    + "hand, now emitted by the thing that measured it.",

  source:
    "**The prose is no longer a source, and this certificate is why.** It quoted "
    + "two wrong figures from parsing the readable report: `0 unpaired`, from "
    + "searching for a section the instrument never prints, and — narrowly "
    + "avoided — a `PAIRED (n, m by position)` header that a sibling stream's "
    + "parser read as zero, silently, on 587 nodes. `preview/parta/read.mjs` "
    + "checks the schema, reads every field by name, never enumerates `counts` "
    + "as a closed set, and **treats a named field that is absent as an error "
    + "rather than a zero** — which is exactly the fault that produced the first "
    + "of those figures. The numbers above are unchanged by the move, which is "
    + "the point: the old parse was right this time and could not be relied on "
    + "to be.",

  moved:
    "**APPS-Q45: 81 → 55, and the 26 that closed were three declarations.** These "
    + "were classified *the drawing moves* in the previous run, and APPS-Q27 makes "
    + "that a **RECORD** rather than a **DECISION** — the written standard already "
    + "governs, so the edit records a ruling rather than making one. The drawing's "
    + "base `.grow` was `margin-left:auto` where the build's is that plus "
    + "`display:flex; gap:8px` (17 nodes); two bare `<b>` elements took the UA's "
    + "700 where the build states 600 (3); and `.hsub` had the sub-heading's colour "
    + "and size without its flex row (2). **Same key, same totals, same run shape "
    + "both times** — the count fell for a stated reason rather than moving between "
    + "two runs nobody can compare.",

  notmoved:
    "**Four were left, deliberately, and they are a markup difference rather than "
    + "a style one.** `Walk-in` and `＋ New booking` measure `margin-bottom` 0 "
    + "against the build's 6px on frames 10 and 11 — and the drawing already "
    + "carries `.tabs .btn{margin-bottom:6px}`. The rule is there; those two "
    + "controls simply are not inside `.tabs` in those frames. **Moving them would "
    + "be retagging to make a divergence pair**, which `§8` forbids and which cost "
    + "another stream a run. They stay open with their reason.",

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

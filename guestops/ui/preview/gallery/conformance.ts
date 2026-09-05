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
    draws: "Deliberately none",
    why: "A booking's stays are its own — two here, three in the group frame. Bounded by the booking, and a pager under two rows is furniture.",
    pair: "8, 9",
    rule: "§6 — the test excludes it: the count is not a moving target, it is the booking",
  },
  {
    screen: "New booking",
    draws: "Deliberately none",
    why: "The property's own room types. A catalogue, bounded by what the hotel has, and it is the answer to one question rather than a list to walk.",
    pair: "14",
    rule: "§6 — bounded by a natural key",
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
    draws: "Deliberately none",
    why: "One business day's exceptions. It has a count the bar carries and the wire could total it — what excludes it is the approved frame: a property with enough of these to need a second page has a problem a pager would help it not to look at.",
    pair: "12",
    rule: "§6 — and frame 12, which draws none",
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

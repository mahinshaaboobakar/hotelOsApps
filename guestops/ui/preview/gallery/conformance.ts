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
    entry: "today",
    answers: "What is the shape of the shift, and who walks in next?",
    target: "the stay",
    filter: "stay/{stayId} — the arrival's own stay",
  },
  {
    name: "Occupancy",
    entry: "occupancy",
    answers: "How full is the hotel tonight, and where?",
    target: "the rooms of one type",
    filter: "rooms/{roomType} — the type the row names",
  },
  {
    name: "From the PMS",
    entry: "from-the-pms",
    answers: "Is the feed still sending, and what could it not place?",
    target: "Attention",
    filter: "attention/{stayId} — the held fact's own stay",
  },
  {
    name: "Business Mix",
    entry: "business-mix",
    answers: "Where did today's arrivals come from?",
    target: "the day's arrivals",
    filter: "arrivals/channel/{name} and arrivals/market/{code} — in the source's own words, never normalised",
  },
  {
    name: "Watchlist",
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
  title: "Five built, none drawn — a design gap, and it is mine",

  body:
    "No approved frame draws any of these. The gold mockup has seventeen frames "
    + "and not one of them is a widget; `docs/chapters/` carries none either. They "
    + "were built to the mechanism — design page 56's canvas, 320×384, no "
    + "self-sizing, no network — and to the SDK's contract, both of which they "
    + "satisfy. What they were never built to is a drawing the owner audited.",

  consequence:
    "So the captures below are the only record of what these five look like, and "
    + "they are evidence of what was built rather than of conformance to anything. "
    + "Part A's whole method is a frame beside a build; for the widgets there is "
    + "no left-hand pane, and putting the captures up alone would imply one. "
    + "**They need frames and the owner's audit before this row can say anything "
    + "else.**",
};

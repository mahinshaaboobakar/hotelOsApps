/**
 * What each surface OWES, as tables a walker checks it against.
 *
 * **Two tables, and the header now says only what is here.** It read *"what
 * the certificate asserts beyond the pairs — the two conformance tables"*
 * while the file also carried four records of what RUNS found; those are
 * `measured.ts` since 2026-09-26. A summary needing an "and" between two
 * different nouns is ADR 0038's test for two files.
 *
 * **A declaration table earns its keep on three properties**, and both of
 * these have them: the obligation is enumerable per screen, its presence is
 * detectable in the document so the walk checks BOTH directions, and its
 * absence has a statable rule a reader can disagree with.
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

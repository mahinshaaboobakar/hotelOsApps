/**
 * The canvas's own numbers — the capture harness's answer for a LOADED widget.
 *
 * The same reasoning as `book/recorded.ts`: these are the **approved
 * artboards' figures**, transcribed, so a capture taken for the frame-beside-
 * capture audit compares the build against the drawing rather than against
 * something invented alongside it.
 *
 * **No widget imports this, and none may.** It used to say *"They sit behind
 * `read()`, so a widget never chooses between live and recorded"* — the fallback
 * `APPS-Q42` removed, when a failed read stopped rendering recorded values. That
 * sentence outlived the mechanism by a fortnight, describing a path that no
 * longer ran. It is `preview/widget-host.ts`'s now, and nothing else's; a widget
 * bundle reaching for it would be the fallback coming back.
 */

/**
 * Today at the Desk — the four counts and the next five arrivals, in the shape
 * `desk` sends: disjoint counts, arrival times as instants.
 */
export const desk = {
  dueIn: 18,
  arrived: 11,
  dueOut: 14,
  departed: 12,
  arrivals: [
    { guest: "Anand Menon", room: "Dlx 402", at: "2026-09-01T14:20:00+05:30", stay: "a1" },
    { guest: "Priya Nair", room: "Std 217", at: "2026-09-01T14:45:00+05:30", stay: "a2" },
    { guest: "R. Balakrishnan", room: "Ste 601", at: "2026-09-01T15:00:00+05:30", stay: "a3" },
    { guest: "Fatima Al Zahra", room: "Dlx 408", at: "2026-09-01T15:30:00+05:30", stay: "a4" },

    // The gap rather than a guess — the canvas's own footnote, and the reason
    // this row is drawn at all rather than filtered out of the list.
    { guest: "Joseph Thomas", room: null, at: "2026-09-01T16:10:00+05:30", stay: "a5" },
  ],
};

/** Occupancy — tonight, and the split by room type. */
export const occupancy = {
  inHouse: 63,
  occupied: 63,
  free: 27,
  tonight: 81,
  types: [
    { name: "Standard", rooms: 44, sold: 31 },
    { name: "Deluxe", rooms: 32, sold: 24 },
    { name: "Suite", rooms: 14, sold: 8 },
  ],
};

/**
 * From the PMS — what arrived, what is held, and when the feed last spoke.
 *
 * `lastFactAt` is the row the ruling added. The canvas drew *"Last fact held"*,
 * which was the inverted mark: a healthy feed holds nothing and so had no
 * timestamp at all.
 */
export const pms = {
  newToday: 23,
  held: 2,
  lastFactAt: "2026-09-01T09:41:00+05:30",
  facts: [
    { reason: "May be a stay you already created", source: null, at: "2026-09-01T09:12:00+05:30", stay: "h1" },
    { reason: "May be a stay you already created", source: null, at: "2026-09-01T07:48:00+05:30", stay: "h2" },
  ],
};

/** Business Mix — today's arrivals, by channel and by market code. */
export const mix = {
  channels: [
    { name: "Direct", count: 7 },
    { name: "OTA", count: 6 },
    { name: "Corporate", count: 3 },
    { name: "Travel agent", count: 2 },
  ],
  markets: [
    { name: "LEIS", count: 9 },
    { name: "CORP", count: 5 },
    { name: "GRP", count: 4 },
  ],
};

/** Watchlist — what nobody was thinking about. */
export const watchlist = {
  overdueOut: 3,
  noRoom: 1,
  notCheckedOut: 5,
  overdue: [
    { room: "Suite 601", guest: "R. Balakrishnan", due: "2026-09-01T11:00:00+05:30", late: "+3h", stay: "w1" },
    { room: "Dlx 305", guest: "Meera Iyer", due: "2026-09-01T11:00:00+05:30", late: "+3h", stay: "w2" },
    { room: "Std 118", guest: "K. Varghese", due: "2026-09-01T12:00:00+05:30", late: "+2h", stay: "w3" },
  ],
  unassigned: [
    { guest: "Joseph Thomas", type: "Deluxe", at: "2026-09-01T16:10:00+05:30", stay: "w4" },
  ],
};

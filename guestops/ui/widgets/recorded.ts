/**
 * The canvas's own numbers, for a widget whose data has not arrived.
 *
 * The same reasoning as `book/recorded.ts`: these are the **approved
 * artboards' figures**, transcribed, so a capture taken for the frame-beside-
 * capture audit compares the build against the drawing rather than against
 * something invented alongside it.
 *
 * They sit behind `read()`, so a widget never chooses between live and
 * recorded — it asks once and is told which it got, and says so on the card
 * when the answer is not the property's.
 */

/** Today at the Desk — the four counts and the next five arrivals. */
export const today = {
  dueIn: 18,
  arrived: 11,
  dueOut: 14,
  departed: 12,
  arrivals: [
    { guest: "Anand Menon", room: "Dlx 402", at: "14:20", stay: "a1" },
    { guest: "Priya Nair", room: "Std 217", at: "14:45", stay: "a2" },
    { guest: "R. Balakrishnan", room: "Ste 601", at: "15:00", stay: "a3" },
    { guest: "Fatima Al Zahra", room: "Dlx 408", at: "15:30", stay: "a4" },

    // The gap rather than a guess — the canvas's own footnote, and the reason
    // this row is drawn at all rather than filtered out of the list.
    { guest: "Joseph Thomas", room: null, at: "16:10", stay: "a5" },
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
  lastFactAt: "09:41",
  facts: [
    { reason: "Unmatched stay reference", source: "OHIP", at: "09:12", stay: "h1" },
    { reason: "Rate code not known here", source: "OHIP", at: "07:48", stay: "h2" },
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
    { room: "Suite 601", guest: "R. Balakrishnan", due: "due 11:00", late: "+3h", stay: "w1" },
    { room: "Dlx 305", guest: "Meera Iyer", due: "due 11:00", late: "+3h", stay: "w2" },
    { room: "Std 118", guest: "K. Varghese", due: "due 12:00", late: "+2h", stay: "w3" },
  ],
  unassigned: [
    { guest: "Joseph Thomas", type: "Deluxe", at: "16:10", stay: "w4" },
  ],
};

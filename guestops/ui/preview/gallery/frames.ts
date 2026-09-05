/**
 * The approved frames, taken out of the gold file exactly as drawn.
 */

/** One pair: the frame that was approved, and the screen that was built. */
export interface Pair {
  /** The design's own number — `5b` is a frame, not a fraction. */
  number: string;

  /** How the preview harness reaches the built screen. */
  query: string;
}

/**
 * The seventeen, in the design's order.
 *
 * The order is the gold file's and not the build's: a gallery sorted by what
 * was easy to build would let a reader lose track of which frames are missing.
 */
export const PAIRS: readonly Pair[] = [
  { number: "1", query: "today" },
  { number: "2", query: "bookings" },
  { number: "3", query: "stay" },
  { number: "4", query: "activity" },
  { number: "5", query: "requests" },
  { number: "5b", query: "requests&alone=true" },
  { number: "6", query: "servicing" },
  { number: "7", query: "payment" },
  { number: "8", query: "cancel" },
  { number: "9", query: "booking&group=true" },
  { number: "10", query: "walkin" },
  { number: "11", query: "today&connected=true" },
  { number: "12", query: "attention" },
  { number: "13", query: "firstrun" },
  { number: "14", query: "newbooking" },
  { number: "15", query: "registration" },
  { number: "16", query: "setup" },
];

/** The gold file's own stylesheet, taken whole. */
export function goldStyle(source: string): string {
  const open = source.indexOf("<style>") + "<style>".length;
  return source.slice(open, source.indexOf("</style>", open));
}

/**
 * One frame's window markup.
 *
 * Located by the heading's **number** rather than by line, because the file is
 * edited: an amendment moves every line below it, and a gallery keyed on line
 * numbers starts pairing the wrong frames without saying anything.
 */
export function frame(source: string, number: string): string {
  const start = source.search(headingOf(number));

  if (start < 0) {
    throw new Error(`frame ${number} is not in the gold file`);
  }

  const win = source.indexOf('<div class="win"', start);
  const cap = source.indexOf('<div class="cap"', win);

  if (win < 0) {
    throw new Error(`frame ${number} has no window`);
  }

  return source.slice(win, cap < 0 ? source.length : cap);
}

/** The frame's heading, with its markup removed. */
export function heading(source: string, number: string): string {
  const start = source.search(headingOf(number));
  const end = source.indexOf("</h1>", start);

  return source
    .slice(source.indexOf(">", start) + 1, end)
    .replace(/<[^>]+>/g, "")
    .replace(/&amp;/g, "&")
    .trim();
}

/** `<h1…>9 · <b>` — the one shape every frame's heading has. */
function headingOf(number: string): RegExp {
  return new RegExp(`<h1[^>]*>${number} · <b>`);
}

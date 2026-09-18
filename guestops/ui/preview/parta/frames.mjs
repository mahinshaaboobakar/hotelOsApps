// The frames this audit covers, and what each one is.
//
// **Its own module because two instruments need the same answer.** `run.mjs`
// sweeps them and `classify.mjs` must know how many there should be — and a
// classifier that counts the comparisons it happens to find reports
// "every difference is named" over a partial run, with its columns closing,
// which is the shape this audit exists to refuse.

/**
 * Which drawn frame answers which built screen, and what to measure of it.
 *
 * **Written down because it cannot be derived.** The drawing numbers frames by
 * the order a reader meets them and the build names screens by what they are;
 * `5b` is a variant of `5` and `11` is `1` in another state.
 *
 * **The first version invented `?screen=stay&tab=Activity`, and the harness
 * reads no `tab`.** It has `activity`, `requests`, `servicing` and `payment` as
 * screens of their own, each driven by clicking the tab — a table that was
 * already there and that I did not read. Five drawings were measured against
 * one rendering and nothing failed: the columns closed on every frame, and what
 * showed it was five identical built totals of 57. **A harness's capability is
 * a population nobody enumerates; asking in your own vocabulary gets you the
 * default, silently.**
 *
 * `root` is what the sweep measures on the built side, and it is not always the
 * page. Frames 10 and 15 draw a **sheet**; the build draws the sheet over the
 * day, which is correct — page 64 §9 makes the overlay a sibling of `.body`,
 * and the scrim is what makes it an overlay at all. Comparing a sheet's drawing
 * against a sheet-plus-page rendering measures the wrong pair and would report
 * the page behind as sixty-odd divergences forever. **The instrument is scoped
 * to the overlay; the drawings are not grown to suit it** — that would change
 * seventeen approved frames for an instrument's convenience, and whether the
 * frames should show the composited surface is the owner's call, drawn.
 */
export const FRAMES = [
  { id: "f1", drawn: "1 · Today", url: "screen=today", root: "body" },
  { id: "f2", drawn: "2 · Bookings", url: "screen=bookings", root: "body" },
  { id: "f3", drawn: "3 · Stay · Overview", url: "screen=stay", root: "body" },
  { id: "f4", drawn: "4 · Stay · Activity", url: "screen=activity", root: "body" },
  { id: "f5", drawn: "5 · Stay · Requests", url: "screen=requests", root: "body" },
  { id: "f6", drawn: "5b · Requests, Jobs absent", url: "screen=requests&alone=true", root: "body" },
  { id: "f7", drawn: "6 · Stay · Servicing", url: "screen=servicing", root: "body" },
  { id: "f8", drawn: "7 · Stay · Payment", url: "screen=payment", root: "body" },
  { id: "f9", drawn: "8 · Cancel", url: "screen=cancel", root: ".dlg" },
  { id: "f10", drawn: "9 · The group", url: "screen=booking&group=true", root: "body" },
  { id: "f11", drawn: "10 · Walk-in", url: "screen=walkin", root: ".sheet" },
  { id: "f12", drawn: "11 · Today, PMS-connected", url: "screen=today&connected=true", root: "body" },
  { id: "f13", drawn: "12 · Attention", url: "screen=attention", root: "body" },
  { id: "f14", drawn: "13 · First run", url: "screen=firstrun", root: "body" },
  { id: "f15", drawn: "14 · New booking", url: "screen=newbooking", root: "body" },
  { id: "f16", drawn: "15 · Registration card", url: "screen=registration", root: ".sheet" },
  { id: "f17", drawn: "16 · Setup", url: "screen=setup", root: "body" },
];

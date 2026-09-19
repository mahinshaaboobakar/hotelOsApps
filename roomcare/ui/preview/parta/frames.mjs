// The frames Room Care's Part A covers, and what each one is.
//
// **Its own module because two instruments need the same answer** — GuestOps'
// reason, kept: `run.mjs` sweeps them and `classify.mjs` must know how many
// there should be, or a partial run whose columns close reads as a verdict.

/**
 * Which approved frame answers which built screen, and how to reach it.
 *
 * **Written down because it cannot be derived.** The drawings number frames in
 * the order a reader meets them; the harness names screens by what they are
 * (`preview/frame.ts`'s `?screen=` table — read, not guessed: GuestOps' first
 * run asked the harness in its own vocabulary and got the default, silently).
 *
 * - `page` — `01` is `01-the-roomcare-screens.html`, `02` is
 *   `02-the-roomcare-setup.html`, both owner-locked.
 * - **Frame 1, the paged list, is not here.** The owner removed it (redline 4,
 *   2026-09-13) and it is kept faint on the page for the record; there is no
 *   screen for it to be compared with.
 * - `width` — the attendant's two frames are drawn at 720px, so they are
 *   measured at the width they were drawn at.
 * - `root` is `body` throughout: every frame draws a whole window, including 4
 *   and 3b, which draw a panel inline where the build composes it in a sheet —
 *   page 64 §9's third surface, `RC-Q6`. The difference that makes is named in
 *   `classify.mjs`; the drawings are not trimmed to suit the instrument.
 */
export const FRAMES = [
  { id: "f1a", page: "01", drawn: "1a · The board — the map", url: "screen=board" },
  { id: "f1b", page: "01", drawn: "1b · The board — the wall", url: "screen=wall" },
  { id: "f2", page: "01", drawn: "2 · Prepare", url: "screen=prepare" },
  { id: "f3", page: "01", drawn: "3 · My rooms", url: "screen=myrooms", width: 720 },
  { id: "f3b", page: "01", drawn: "3b · At the door", url: "screen=door-end", width: 720 },
  { id: "f4", page: "01", drawn: "4 · A room", url: "screen=room-state" },
  { id: "f4c", page: "01", drawn: "4c · Room states — the sheet", url: "screen=sheet" },
  { id: "f4d", page: "01", drawn: "4d · Room states — the tap grid", url: "screen=grid" },
  { id: "f4e", page: "01", drawn: "4e · Room states — compact", url: "screen=compact" },
  { id: "f4b", page: "01", drawn: "4b · A room, inspected", url: "screen=room-g03" },
  { id: "f5", page: "01", drawn: "5 · Supervision", url: "screen=supervision" },
  { id: "f6", page: "01", drawn: "6 · Deep clean", url: "screen=deepclean" },
  { id: "f7", page: "01", drawn: "7 · Setup", url: "screen=setup" },
  { id: "f8", page: "01", drawn: "8 · Widgets", url: "screen=widgets" },
  { id: "f7a", page: "02", drawn: "7a · Windows & trigger", url: "screen=setup&tab=Windows%20%26%20trigger" },
  { id: "f7b", page: "02", drawn: "7b · Services & minutes", url: "screen=setup&tab=Services%20%26%20minutes" },
  { id: "f7c", page: "02", drawn: "7c · Rules", url: "screen=setup&tab=Rules" },
  { id: "f7d", page: "02", drawn: "7d · Assignment & zones", url: "screen=setup&tab=Assignment%20%26%20zones" },
  { id: "f7e", page: "02", drawn: "7e · Areas", url: "screen=setup&tab=Areas" },
  { id: "f7f", page: "02", drawn: "7f · Deep clean plan", url: "screen=setup&tab=Deep%20clean%20plan" },
  { id: "f7g", page: "02", drawn: "7g · Property-wide access", url: "screen=setup&tab=Property-wide%20access" },
];

/** The two drawings, and each page's own `<title>` — what `serve.mjs` verifies before a byte is measured. */
export const PAGES = {
  "01": { path: "/01-the-roomcare-screens.html", title: "Room Care · 01 · the screens" },
  "02": { path: "/02-the-roomcare-setup.html", title: "Room Care · 02 · setup" },
};

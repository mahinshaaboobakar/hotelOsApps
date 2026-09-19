// Every case the page-64 audit drives: each Room Care surface crossed with the
// states `docs/app-surface-checklist.md` names for it. Derived from three tables
// below — the screens with the read each one makes, the paged lists, and the
// overlays with the control that opens each — so a screen added to one table is
// audited in every state that table implies.

/** Each screen, the harness's drive to it, and the read whose failure empties it. */
export const SCREENS = [
  { surface: "Board · map", url: "screen=board", read: "board" },
  { surface: "Board · wall", url: "screen=wall", read: "board", viewOf: "Board · map" },
  { surface: "A room", url: "screen=room", read: "room" },
  { surface: "A room, inspected", url: "screen=room-g03", read: "room" },
  { surface: "Prepare", url: "screen=prepare", read: "prepare" },
  { surface: "Room states · sheet", url: "screen=sheet", read: "states" },
  { surface: "Room states · grid", url: "screen=grid", read: "states", viewOf: "Room states · sheet" },
  { surface: "Room states · compact", url: "screen=compact", read: "states", viewOf: "Room states · sheet" },
  { surface: "Supervision", url: "screen=supervision", read: "supervision" },
  { surface: "Deep clean", url: "screen=deepclean", read: "deepCleans" },
  { surface: "My rooms", url: "screen=myrooms", read: "myRooms" },
  { surface: "At the door", url: "screen=door", read: "door" },
  { surface: "Setup › Windows & trigger", url: "screen=setup&tab=Windows %26 trigger", read: "setup" },
  { surface: "Setup › Services & minutes", url: "screen=setup&tab=Services %26 minutes", read: "services" },
  { surface: "Setup › Rules", url: "screen=setup&tab=Rules", read: "setup" },
  { surface: "Setup › Assignment & zones", url: "screen=setup&tab=Assignment %26 zones", read: "zones" },
  { surface: "Setup › Areas", url: "screen=setup&tab=Areas", read: "areas" },
  { surface: "Setup › Deep clean plan", url: "screen=setup&tab=Deep clean plan", read: "deepCleanPlan" },
  { surface: "Setup › Property-wide access", url: "screen=setup&tab=Property-wide access", read: "grants" },
];

/** The five paged lists — the surfaces the list states apply to. */
export const LISTS = ["Deep clean", "My rooms", "Prepare", "Setup › Areas", "Supervision"];

/** Every overlay, by the screen it opens on and the words on the control that opens it. */
export const OVERLAYS = [
  { on: "Deep clean", open: "plan a window…" },
  { on: "Deep clean", open: "Cancel this deep clean…" },
  { on: "At the door", open: "End…" },
  { on: "At the door", open: "Ask for extra time…" },
  { on: "At the door", open: "Found an issue…" },
  { on: "Prepare", open: "Move rooms…" },
  { on: "Prepare", open: "Assign anyway…", needs: "a nobody-available row with a task; the recorded Coral Cove morning has none. It opens the same sheet as Move rooms…, titled Assign anyway" },
  { on: "A room", open: "Room state…" },
  { on: "A room", open: "Record an exception…" },
  { on: "A room", open: "Reassign…" },
  { on: "Setup › Property-wide access", open: "Grant to a person…" },
  { on: "Setup › Property-wide access", open: "Revoke…" },
  { on: "Setup › Areas", open: "Edit…" },
  { on: "Setup › Rules", open: "Reorder…" },
  { on: "Setup › Services & minutes", open: "Reorder…" },
  { on: "Setup › Services & minutes", open: "Copy…" },
  { on: "Setup › Assignment & zones", open: "Move rooms between zones…" },
  { on: "Supervision", open: "DND approved" },
  { on: "Supervision", open: "Clean it" },
  { on: "Supervision", open: "Other…" },
  { on: "Supervision", open: "Leave for reconcile", needs: "a lane row for a room nobody was available for; the recorded lane holds none. Its failed write opens the Not done dialog" },
];

export const CAUSES = ["unanswered", "forbidden", "unadmitted", "ungranted", "undecidable", "faulted"];
const LIST_STATES = ["E0", "E1", "1P", "MP", "ML"];

const slug = (s) => s.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/(^-|-$)/g, "");
const screen = (surface) => SCREENS.find((s) => s.surface === surface);

/**
 * States no drive can reach, each with its reason — reported as exclusions by sentence, never
 * measured and never silently dropped.
 */
export const EXCLUDED = [
  ...SCREENS.filter((s) => s.viewOf !== undefined).map((s) => ({
    surface: s.surface, state: "F6",
    why: `a view of ${s.viewOf}: its switcher is drawn only once the read answers, so a failed read reaches the one failure, audited on ${s.viewOf}`,
  })),
  ...OVERLAYS.filter((o) => o.needs !== undefined).map((o) => ({ surface: `${o.on} › ${o.open.replace(/…$/, "")}`, state: "OV · FM", why: `needs ${o.needs}` })),
];

export const CASES = [
  ...SCREENS.map((s) => ({ id: `${slug(s.surface)}--all`, surface: s.surface, state: "ALL", url: s.url })),
  ...SCREENS.map((s) => ({ id: `${slug(s.surface)}--nl`, surface: s.surface, state: "NL", url: `${s.url}&nl=1` })),
  ...SCREENS.filter((s) => s.viewOf === undefined).flatMap((s) => CAUSES.map((cause) => ({
    id: `${slug(s.surface)}--f6-${cause}`, surface: s.surface, state: "F6", cause, url: `${s.url}&fail=${cause}&at=${s.read}`,
  }))),
  // Setup reads `setup` before any tab draws, so its failure empties every tab alike: once, on the first.
  ...CAUSES.map((cause) => ({ id: `setup-all-tabs--f6-${cause}`, surface: "Setup (its own read)", state: "F6", cause, url: `screen=setup&tab=Services %26 minutes&fail=${cause}&at=setup` })),
  // The bar's own read, failing under a screen that loaded — a partial failure (X15).
  { id: "board-map--f6-me", surface: "Board · map", state: "F6", cause: "unanswered", url: "screen=board&fail=unanswered&at=me" },
  ...LISTS.flatMap((surface) => LIST_STATES.map((state) => ({
    id: `${slug(surface)}--${state.toLowerCase()}`, surface, state, url: `${screen(surface).url}&list=${state}`,
  }))),
  { id: "widgets--w", surface: "Widgets", state: "W", url: "screen=widgets", widgets: true },
  ...CAUSES.map((cause) => ({ id: `widgets--w-${cause}`, surface: "Widgets", state: "W", cause, url: `screen=widgets&fail=${cause}`, widgets: true })),
  ...OVERLAYS.filter((o) => o.needs === undefined).flatMap((o) => {
    const base = `${screen(o.on).url}&open=${encodeURIComponent(o.open)}`;
    const id = `${slug(o.on)}--ov-${slug(o.open)}`;
    const surface = `${o.on} › ${o.open.replace(/…$/, "")}`;
    return [
      { id, surface, state: "OV", url: base },
      { id: `${id}-filled`, surface, state: "FM", url: base, fill: true },
      { id: `${id}-write`, surface, state: "OV", url: `${base}&write=1`, write: true },
    ];
  }),
];

/**
 * Creating a shift — the page the whole catalogue rests on.
 *
 * # Every field here carries a rule the backend enforces
 *
 * The **short code is typed, never derived**: *Morning* and *Mid-shift* would
 * both want "M", and two shifts that look identical on a photocopy is the
 * mistake this prevents. The **kind** is expressed by the absence of times — an
 * off shift has none and counts no hours, which is what Week-off is. A **second
 * span** makes it a split shift. A span **ending before it starts crosses
 * midnight**. And the **colour is the shift's own attribute**, not a
 * consequence of its code — the code is what survives when colour is lost.
 */

import { control, el } from "../../chrome/element";
import { overlay } from "../../chrome/overlay";

/**
 * What an empty value box shows. §10 draws `.inp.ph` *"when nobody has
 * supplied a value"*; the dash is §11's mark for absent.
 */
const NOTHING = "—";

/**
 * The sheet — a person composes a shift here (§9).
 *
 * @param close called when it is dismissed
 * @returns the overlay
 */
export function newShift(close: () => void): HTMLElement {
  return overlay("sheet", {
    head: [
      el("div", "ht", "New shift"),
      el("div", "hsub", "It appears in the rota picker the moment it is saved"),
    ],
    body: [
    field("Name", "What people read. Any length."),
    field("Short code",
      "Two or three characters — what fits a rota cell and survives a "
      + "black-and-white photocopy. You choose it, because Morning and Mid-shift "
      + "would both want \u201cM\u201d, and two shifts that look identical on paper is "
      + "the mistake this prevents."),
    kind(),
    times(),
    colour(),
    ],
    foot: [actions(close)],
  }, close);
}

/**
 * A labelled field, and the sentence that says why it is asked for.
 *
 * **Its box is the placeholder, always.** This drew the frame's example —
 * *"Split — Banquet"*, *"SB"* — as though a person had entered it, in a box
 * nothing can be typed into (§10: *"A field renders a value the desk has
 * already chosen"*; nobody chose these). The app surface audit, 2026-09-19.
 */
function field(label: string, note: string): HTMLElement {
  const row = el("div", "fld");

  row.append(
    el("div", "fld-label", label),
    el("div", "inp ph", NOTHING),
    el("div", "note", note),
  );

  return row;
}

/** Working or off — and off is the absence of times, not a separate concept. */
function kind(): HTMLElement {
  const row = el("div", "fld");
  const choices = el("div", "choices");

  // Neither chosen: the frame showed *Working* ticked, which is a choice nobody
  // made on a form that cannot record one.
  const working = el("div", "choice", "Working");
  const off = el("div", "choice", "Off");

  choices.append(working, off);

  row.append(
    el("div", "fld-label", "Kind"),
    choices,
    el("div", "note",
      "An off shift has no times and counts no hours — that is what Week-off is. "
      + "A rota marker, not a leave type: no request, no balance."),
  );

  return row;
}

/** Two spans, the second optional. */
function times(): HTMLElement {
  const row = el("div", "fld");
  // Four, said out loud. The row is two columns by default because two is
  // what every other dialog needs; a split shift is the exception and names
  // itself rather than making the default wrong for everyone else.
  const spans = el("div", "spans four");

  spans.append(
    el("div", "inp ph", NOTHING), el("div", "inp ph", NOTHING),
    el("div", "inp ph", NOTHING), el("div", "inp ph", NOTHING),
  );

  row.append(
    el("div", "fld-label", "Times"),
    spans,
    el("div", "note",
      "A second span makes it a split shift. A span ending before it starts "
      + "crosses midnight — Night is 23:00 → 07:00."),
  );

  return row;
}

/** The colour, chosen rather than derived. */
function colour(): HTMLElement {
  const row = el("div", "fld");
  const swatches = el("div", "swatches");

  for (const tone of ["brand", "ok", "warn", "bad", "neutral"]) {
    // None chosen — the frame picked amber.
    swatches.append(el("div", `sw ${tone}`));
  }

  row.append(
    el("div", "fld-label", "Colour"),
    swatches,
    el("div", "note",
      "How the week reads at a glance. Colour is the shift's own attribute, not "
      + "a consequence of its code — and the short code is what survives when "
      + "colour is lost."),
  );

  return row;
}

function actions(close: () => void): HTMLElement {
  const row = el("div", "acts");
  const cancel = control("btn", "Cancel", close);

  // Inert for the same reason as Raise request, one screen over:
  // `roster.configure · defineShift` takes a name, a code, a colour and a date,
  // and `field()` draws each of them as a div. The dialog shows what a shift
  // is made of; it does not yet ask.
  //
  // **So the primary is OFF, and says why** — §2: *"A primary action with
  // nothing to send is drawn `off`, with the reason beside it — never
  // live-and-refusing."* It was a live `div.btn.pri` over nothing (the app
  // surface audit, 2026-09-19, C11 · C8).
  const create = control("btn pri off", "Create shift");
  create.setAttribute("disabled", "true");

  row.append(
    cancel,
    create,
    el("span", "why", "Shifts cannot be entered here yet, so there is nothing to create."),
  );
  return row;
}

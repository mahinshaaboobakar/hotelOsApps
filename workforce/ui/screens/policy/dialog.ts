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

import { el } from "../../chrome/element";

/**
 * The dialog, drawn over the screen beneath it.
 *
 * @param close called when it is dismissed
 * @returns the overlay
 */
export function newShift(close: () => void): HTMLElement {
  const overlay = el("div", "scrim");
  const dialog = el("div", "dlg");

  const head = el("div");
  head.append(
    el("div", "ht", "New shift"),
    el("div", "hsub", "It appears in the rota picker the moment it is saved"),
  );

  dialog.append(
    head,
    field("Name", "Split — Banquet", "What people read. Any length."),
    field("Short code", "SB",
      "Two or three characters — what fits a rota cell and survives a "
      + "black-and-white photocopy. You choose it, because Morning and Mid-shift "
      + "would both want \u201cM\u201d, and two shifts that look identical on paper is "
      + "the mistake this prevents."),
    kind(),
    times(),
    colour(),
    actions(close),
  );

  overlay.append(dialog);
  overlay.addEventListener("click", (event) => {
    if (event.target === overlay) close();
  });

  return overlay;
}

/** A labelled field, and the sentence that says why it is asked for. */
function field(label: string, value: string, note: string): HTMLElement {
  const row = el("div", "fld");

  row.append(
    el("div", "flab", label),
    el("div", "finput", value),
    el("div", "note", note),
  );

  return row;
}

/** Working or off — and off is the absence of times, not a separate concept. */
function kind(): HTMLElement {
  const row = el("div", "fld");
  const choices = el("div", "choices");

  const working = el("div", "choice on", "Working ✓");
  const off = el("div", "choice", "Off — ");

  choices.append(working, off);

  row.append(
    el("div", "flab", "Kind"),
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
  const spans = el("div", "spans");

  spans.append(
    el("div", "finput", "10:00"), el("div", "finput", "14:00"),
    el("div", "finput", "18:00"), el("div", "finput", "22:00"),
  );

  row.append(
    el("div", "flab", "Times"),
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
    swatches.append(el("div", `sw ${tone}${tone === "warn" ? " on" : ""}`));
  }

  row.append(
    el("div", "flab", "Colour"),
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
  const cancel = el("div", "btn", "Cancel");

  cancel.addEventListener("click", close);
  row.append(cancel, el("div", "btn go", "Create shift"));
  return row;
}

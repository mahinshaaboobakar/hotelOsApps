/**
 * The registration card — what the guest signs. Gold frame 15.
 *
 * **A proposal the property tailors, not a form the platform imposes.** The
 * field list is the design's; which of them are *required* is configuration,
 * separately for domestic and foreign guests, because a resort taking weekend
 * guests and a city hotel taking business visas do not collect the same things.
 *
 * **A field a property does not use is not deleted from the model.** A
 * registration card is a record that must stay readable for years, so an unused
 * field is simply not required — which is why `Vehicle` is drawn with its own
 * prompt rather than omitted.
 *
 * **No country is written into this screen.** The conditional block is on the
 * guest's nationality against the property's own home country — a setting — so
 * a hotel in Kochi treats an Emirati guest this way and a hotel in Dubai treats
 * an Indian guest this way, from the same product.
 */

import type { CardRow, RegistrationCard } from "../../book";
import { el, fill } from "../../chrome/element";
import { field, pair } from "../../chrome/field";
import { sheet } from "../../chrome/overlay";

/**
 * Draw the card.
 *
 * @param card the fields, as this property configured them
 * @param close what dismissing it does
 * @returns the scrim, with the sheet on it
 */
export function registration(card: RegistrationCard, close: () => void): HTMLElement {
  return sheet({
    title: "Registration card",
    subtitle: card.series,

    body: [
      ...card.rows.map(row),
      card.foreign === null ? null : foreign(card),
      ...card.closing.map(row),
      note(card.note),
    ],

    // **Stated, never enforced** — S19b. An outstanding filing does not block a
    // check-in, so this is in the quiet half of the foot rather than beside the
    // button, where it would read as a condition of pressing it.
    foot: card.obligation,

    actions: [
      { label: "Cancel", onClick: close },

      // **Off, and the sheet says why in every field.** These boxes render
      // values; nothing here captures a signature, a scan or a typed
      // correction, so a live Save would write the fixture back. The write path
      // exists — `registration.capture` — and has no input to carry.
      { label: "Save and check in", primary: true, off: true },
    ],

    onDismiss: close,
  });
}

/** One line: a field across the sheet, or two side by side. */
function row(line: CardRow): HTMLElement {
  return line.kind === "one"
    ? field(line.field)
    : pair(line.fields[0], line.fields[1]);
}

/**
 * The block for a guest from outside.
 *
 * **It names why it is here.** A block of extra questions with no stated reason
 * reads to a receptionist as the software being difficult; *shown because UAE
 * is not this property's home country* reads as a rule they can explain to the
 * person in front of them.
 */
function foreign(card: RegistrationCard): HTMLElement | null {
  if (card.foreign === null) return null;

  const root = el("div", "card info");
  const heading = el("div", "ch info");

  heading.append(
    document.createTextNode(card.foreign.title),
    el("div", "grow", card.foreign.because),
  );

  const body = el("div", "cb");
  fill(body, ...card.foreign.rows.map(row));

  root.append(heading, body);
  return root;
}

/** Why the block exists, in full, under it. */
function note(text: string): HTMLElement {
  const element = el("div", "note");
  const [lead, ...rest] = text.split(". ");

  element.append(
    el("b", undefined, `${lead ?? ""}.`),
    document.createTextNode(` ${rest.join(". ")}`),
  );

  return element;
}

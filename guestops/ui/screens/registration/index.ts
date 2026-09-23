/**
 * The registration card — what the guest signs. Gold frame 15.
 *
 * **A proposal the property tailors, not a form the platform imposes.** The
 * field list is the design's; which of them are *required* is configuration,
 * separately for domestic and foreign guests, because a resort taking weekend
 * guests and a city hotel taking business visas do not collect the same things.
 * Nothing in this file decides that: every box arrives carrying the property's
 * own answer for this guest.
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
 *
 * # The card sends itself back whole
 *
 * `RegistrationService` writes every field on every save, *including to blank*,
 * so a desk can clear a mistyped passport number — the commonest correction
 * there is. The consequence lives here: this sheet posts every box it was
 * given, the two it cannot capture included, because a save carrying only what
 * somebody touched would blank the rest.
 *
 * # Its `Save` used to do nothing, and said so
 *
 * The card was drawn from a fixture with an `off` primary action, because the
 * boxes rendered values and captured none. `registration.capture` now answers
 * both a card and its save, and this is the screen that reaches it.
 */

import type { HostApi } from "@hotelos/sdk";

import {
  APP,
  failureDrawing,
  load,
  perform,
  type CardField,
  type CardRow,
  type RegistrationCard,
} from "../../book";
import { el, fill } from "../../chrome/element";
import { field, pair, type Capture, type Field } from "../../chrome/field";
import { failed } from "../../chrome/marks";
import { sheet } from "../../chrome/overlay";
import { instant, span } from "../../chrome/when";

/**
 * Read the card and draw it over whatever screen is behind.
 *
 * @param host the bridge — the only route out of this realm
 * @param into where the overlay goes
 * @param stayId the stay this card belongs to
 * @param close what dismissing it does
 * @param done called once the guest is checked in, so the screen behind redraws
 */
export async function registrationCard(
  host: HostApi,
  into: HTMLElement,
  stayId: string,
  close: () => void,
  done: () => void,
): Promise<void> {
  // **Read under the capability that writes it.** The answer carries a passport
  // number and a home address whole, because the desk holding the document has
  // to be able to correct what was typed — and a screen granted the day's
  // arrivals has not thereby been granted every guest's papers.
  const loaded = await load<RegistrationCard>(
    host, "registration.capture", "card", { stayId });

  if (!loaded.ok) {
    into.append(failed(
      failureDrawing(loaded.failure, { app: APP, the: "this registration card" }),
      () => void registrationCard(host, into, stayId, close, done),
    ));
    return;
  }

  into.append(draw(host, into, loaded.value, close, done, null));
}

/**
 * Draw the card.
 *
 * **Every box's whole value is held here from the moment it is read**, which is
 * what lets a masked document number be retyped and what makes the save a
 * whole-card write rather than a patch.
 *
 * `said` is what the last press did. Null on the first draw, because a card
 * nobody has pressed has nothing to report.
 */
function draw(
  host: HostApi,
  into: HTMLElement,
  card: RegistrationCard,
  close: () => void,
  done: () => void,
  said: string | null,
): HTMLElement {
  const values = new Map<string, string>();

  for (const box of boxes(card)) {
    values.set(box.name, box.value ?? "");
  }

  const again = (told: string): void => {
    into.replaceChildren();
    into.append(draw(host, into, card, close, done, told));
  };

  const line = (row: CardRow): HTMLElement =>
    row.kind === "one"
      ? field(drawn(row.field, values, card))
      : pair(drawn(row.fields[0]!, values, card), drawn(row.fields[1]!, values, card));

  const save = async (): Promise<void> => {
    const captured = await perform(
      host, "registration.capture", "capture",
      { stayId: card.stayId, values: Object.fromEntries(values) });

    if (captured.refused !== null) {
      again(captured.refused);
      return;
    }

    if (!card.arriving) {
      // Saved, and there is no arrival to record — a card corrected after the
      // guest is already in house. Saying "checked in" here would be a claim
      // about something that happened hours ago.
      again("The card was saved.");
      return;
    }

    const arrived = await perform(
      host, "stay.override", "checkIn",
      { stayId: card.stayId, version: card.version });

    if (arrived.refused !== null) {
      // **The card WAS saved, and the message says so.** An error naming the
      // step that failed must not imply the steps before it did not run — a
      // desk told only that the check-in failed would type the whole card
      // again.
      again(`The card was saved. The guest was not checked in: ${arrived.refused}`);
      return;
    }

    done();
  };

  return sheet({
    title: "Registration card",
    subtitle: series(card),

    body: [
      who(card, host),
      ...card.rows.map(line),
      card.foreign === null ? null : foreign(card, line),
      ...card.closing.map(line),
      wanted(card),
      said === null ? null : el("div", "note", said),
    ],

    // **Stated, never enforced** — S19b. An outstanding filing does not block a
    // check-in, so this is in the quiet half of the foot rather than beside the
    // button, where it would read as a condition of pressing it.
    foot: obligation(card, host),

    actions: [
      { label: "Cancel", onClick: close },
      {
        // **The label is what it does.** It saves and checks in where there is
        // an arrival to record, and says only *Save* where the guest is already
        // in house. The frame draws the arrival case, which is the one a
        // receptionist opens this from.
        label: card.arriving ? "Save and check in" : "Save",
        primary: true,
        onClick: () => void save(),
      },
    ],

    onDismiss: close,
  });
}

/** Who this card is for, and where they are staying. */
function who(card: RegistrationCard, host: HostApi): HTMLElement {
  const where = [
    card.room === null ? "No room yet" : `Room ${card.room}`,
    card.arrive === null ? null : span(card.arrive, card.depart, host.property, "day-month"),
  ].filter((part) => part !== null).join(" · ");

  const root = el("div", "note");

  root.append(
    el("b", undefined, `${card.arriving ? "Checking in" : "In house"} · ${card.who}`),
    document.createTextNode(` ${where}`),
  );

  return root;
}

/**
 * The card's number, and whether the series has spent it yet.
 *
 * **A card that has never been saved shows the number the property *would*
 * mint** — the read does not take one, because a number taken and not used is a
 * gap in a series a property gets asked about.
 */
function series(card: RegistrationCard): string {
  return card.series.taken
    ? card.series.number
    : `${card.series.number} · the property's series, next number taken on save`;
}

/** One box: a control where it can be captured, a value where it cannot. */
function drawn(
  box: CardField,
  values: Map<string, string>,
  card: RegistrationCard,
): Field {
  const value = values.get(box.name) ?? "";

  // **`held` is drawn as a value and says what it is waiting for.** A scan
  // needs the platform's media service and a signature needs a pad; a text box
  // accepting a media reference typed by hand would be a worse lie than the
  // dead `Save` this card replaced.
  if (box.kind === "held") {
    return {
      label: label(box, card),
      value: value === "" ? null : value,
      placeholder: box.placeholder ?? undefined,
    };
  }

  const capture: Capture = {
    kind: box.kind,
    choices: box.choices ?? undefined,

    // The mask stands only while the box still holds what it was read with.
    // Once somebody has typed, what they typed is what is shown.
    shown: box.masked != null && value === (box.value ?? "") ? box.masked : undefined,
    tall: box.tall === true,
    onChange: (typed: string) => void values.set(box.name, typed),
  };

  return {
    label: label(box, card),
    value,
    placeholder: box.placeholder ?? undefined,
    capture,
  };
}

/**
 * The box's label, marked where this property wants it and has not got it.
 *
 * **The mark is the property's answer, and this sheet never guesses it.** A box
 * carrying no mark is not optional everywhere — it is not required *here*, for
 * *this* guest.
 */
function label(box: CardField, card: RegistrationCard): string {
  return box.required && card.missing.includes(box.name) ? `${box.label} ·` : box.label;
}

/**
 * The block for a guest from outside.
 *
 * **It names why it is here.** A block of extra questions with no stated reason
 * reads to a receptionist as the software being difficult; naming the guest's
 * country and the property's reads as a rule they can explain to the person in
 * front of them.
 */
function foreign(
  card: RegistrationCard,
  line: (row: CardRow) => HTMLElement,
): HTMLElement | null {
  if (card.foreign === null) return null;

  const root = el("div", "card info");
  const heading = el("div", "ch info");

  heading.append(
    document.createTextNode(card.foreign.title),
    el("div", "grow", card.foreign.because),
  );

  const body = el("div", "cb");
  fill(body, ...card.foreign.rows.map(line));

  root.append(heading, body);
  return root;
}

/**
 * What the property still wants on this card.
 *
 * **A prompt, never a gate.** The card saves with fields missing, because a
 * guest at the desk at midnight is served and the card is completed after
 * (S19b). Absent when nothing is missing — a sentence saying so would be a
 * congratulation.
 */
function wanted(card: RegistrationCard): HTMLElement | null {
  if (card.missing.length === 0) return null;

  const root = el("div", "note warn");

  root.append(
    el("b", undefined, "This property asks for these, and they are blank."),
    document.createTextNode(` ${card.missing.join(", ")}. The card saves either way.`),
  );

  return root;
}

/**
 * When a filing is owed.
 *
 * Null where this property files nothing, or where this guest falls outside its
 * reporting scope — so a property with no obligation never sees the sentence.
 */
function obligation(card: RegistrationCard, host: HostApi): string | null {
  if (card.obligation === null) return null;

  return `Filing due ${instant(card.obligation.at, host.property, "date")} `
    + `(${card.obligation.hours} h after arrival)`;
}

/** Every box on the card, whichever block it sits in. */
function boxes(card: RegistrationCard): readonly CardField[] {
  const rows = [...card.rows, ...(card.foreign?.rows ?? []), ...card.closing];

  return rows.flatMap((row) => (row.kind === "one" ? [row.field] : [...row.fields]));
}

/**
 * A labelled value, and the two-up row the frames pair them in.
 */

import { el, fill } from "./element";

/** One labelled value, as the frames draw it. */
export interface Field {
  label: string;

  /**
   * What the field shows. Null draws the placeholder instead, which is a
   * different thing from an empty string: `null` is *nobody has supplied this*
   * and `""` is a value that happens to be empty.
   */
  value: string | null;

  /**
   * The part the frame sets bold — `now`, a room number.
   *
   * These four are written `?: T | undefined` rather than `?: T` because
   * `exactOptionalPropertyTypes` is on: a caller computing `aside` as
   * `state ?? undefined` is the ordinary way to say *there may not be one*, and
   * the stricter form would push every caller into a conditional spread to
   * express it.
   */
  strong?: string | undefined;

  /** Pushed to the right inside the box — `new guest`, `vacant · clean`. */
  aside?: string | undefined;

  /** Shown when `value` is null. */
  placeholder?: string | undefined;

  /** The sentence under the field, where the frame has one. */
  hint?: string | undefined;

  /**
   * How this box is captured. Absent draws the value, as every frame does.
   *
   * **A field is read-only until a caller says otherwise**, which is the right
   * default here: almost every box in the approved frames shows a value the
   * desk chose elsewhere, and an editable one is the exception somebody asks
   * for.
   */
  capture?: Capture | undefined;
}

/**
 * What makes a box editable — the registration card's need, and nobody else's yet.
 *
 * **`shown` exists for a masked value.** The card renders `P•••••4412` at rest
 * and the desk holding the passport has to be able to retype it, so the box
 * carries the whole value underneath and displays the mask until somebody
 * focuses it. A mask that could not be corrected would be a field the product
 * can read and the property cannot fix.
 */
export interface Capture {
  /** `text`, `date`, or `choice` over `choices`. */
  kind: "text" | "date" | "choice";

  /** The property's own list, for a chooser. */
  choices?: readonly string[] | undefined;

  /** Drawn instead of the value until the box is focused. */
  shown?: string | undefined;

  /** Drawn tall, for an address. */
  tall?: boolean | undefined;

  /** Called with the box's whole value, blank included. */
  onChange: (value: string) => void;
}

/**
 * Draw a field.
 *
 * **The box was never an `<input>`, and for one stated reason: nothing behind
 * these screens accepted a typed value.** This header said *"when the write
 * path lands, this is the one function that changes"*, and the registration
 * card's write path has landed — `registration.capture` now answers both a card
 * and its save. So a caller that supplies `capture` gets a control, and every
 * caller that does not is drawn exactly as before.
 *
 * **The default did not move.** An editable box everywhere would put keyboard
 * state on twenty screens whose values are chosen elsewhere; the one screen
 * that captures asks for it.
 *
 * @param field what to draw
 * @returns the field
 */
export function field(field_: Field): HTMLElement {
  const root = el("div", "fld");
  const label = el("label", undefined, field_.label);

  if (field_.capture !== undefined) {
    fill(
      root,
      label,
      control(field_, field_.capture),
      field_.hint === undefined ? null : el("div", "hint", field_.hint),
    );

    return root;
  }

  const box = el("div", field_.value === null ? "inp ph" : "inp");

  if (field_.value === null) {
    box.append(document.createTextNode(field_.placeholder ?? ""));
  } else {
    box.append(document.createTextNode(field_.value));
  }

  if (field_.strong !== undefined) {
    box.append(el("b", undefined, field_.strong));
  }

  if (field_.aside !== undefined) {
    box.append(el("span", "grow", field_.aside));
  }

  fill(
    root,
    label,
    box,
    field_.hint === undefined ? null : el("div", "hint", field_.hint),
  );

  return root;
}

/**
 * The control a capturing field draws.
 *
 * **A chooser offers only what the property configured, and says when that is
 * nothing.** An empty accepted-document list is a property that has not been
 * set up; inventing `Passport` to fill the gap would put one product's idea of
 * an identity document into every hotel's record.
 *
 * **A masked box reveals on focus rather than on a button.** The moment the
 * whole value is needed is the moment somebody is typing it, and a reveal
 * control beside the box would be a second thing to press for no extra safety.
 */
function control(field_: Field, capture: Capture): HTMLElement {
  if (capture.kind === "choice") {
    const list = el("select", "inp") as HTMLSelectElement;

    // The empty option is what lets a desk clear a chosen document, and it
    // carries the placeholder so a card with nothing chosen reads the same as
    // every other empty box on it.
    const blank = el("option", undefined, field_.placeholder ?? "") as HTMLOptionElement;
    blank.value = "";
    list.append(blank);

    for (const choice of capture.choices ?? []) {
      const option = el("option", undefined, choice) as HTMLOptionElement;
      option.value = choice;
      list.append(option);
    }

    list.value = field_.value ?? "";
    list.addEventListener("change", () => capture.onChange(list.value));

    return list;
  }

  const box = el(
    capture.tall === true ? "textarea" : "input",
    "inp",
  ) as HTMLInputElement;

  if (capture.tall !== true) box.type = capture.kind === "date" ? "date" : "text";
  if (field_.placeholder !== undefined) box.placeholder = field_.placeholder;

  const whole = field_.value ?? "";

  // The mask stands until somebody types into the box. `shown` is only ever
  // set where the value is secret, so an ordinary field reaches neither branch.
  box.value = capture.shown ?? whole;

  if (capture.shown !== undefined) {
    box.addEventListener("focus", () => {
      if (box.value === capture.shown) box.value = whole;
    });
  }

  box.addEventListener("input", () => capture.onChange(box.value));

  return box;
}

/**
 * Two fields side by side — the frames' `.row2`.
 *
 * @param left the first field
 * @param right the second
 * @returns the pair
 */
export function pair(left: Field, right: Field): HTMLElement {
  const root = el("div", "row2");
  root.append(field(left), field(right));
  return root;
}

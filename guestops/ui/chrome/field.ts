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
}

/**
 * Draw a field.
 *
 * **The box is not an `<input>`**, and that is deliberate. Every field in the
 * approved frames shows a value the desk has already chosen; the frames carry
 * no keyboard state, no validation and no focus ring, and nothing behind these
 * screens accepts a typed value yet. An editable box here would accept typing
 * and save none of it — a control that lies about what it does. When the write
 * path lands, this is the one function that changes.
 *
 * @param field what to draw
 * @returns the field
 */
export function field(field_: Field): HTMLElement {
  const root = el("div", "fld");
  const label = el("label", undefined, field_.label);

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

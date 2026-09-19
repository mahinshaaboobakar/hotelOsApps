/**
 * An instant or a day the wire may not have — drawn by the SDK, or as `—`.
 *
 * Page 64 §11: *"Absent renders `—`, never today."* The SDK's formatters take a
 * value and nothing else, and every nullable field on GuestOps' wire — a
 * departure time nobody recorded, a feed that has never spoken — would otherwise
 * repeat the same `=== null` at each call site, where one of them eventually
 * draws `""` or the word `null` instead. Shared by the screens and the widgets,
 * as `glyph.ts` is.
 */

import { formatDay, formatInstant, type DayStyle, type InstantStyle, type PropertyEnvironment } from "@hotelos/sdk";

/** The dash an absent value is drawn as — never a date somebody could act on. */
export const ABSENT = "—";

/** An ISO instant in the property's form, or the dash. */
export function instant(
  iso: string | null,
  property: PropertyEnvironment,
  style: InstantStyle,
): string {
  return iso === null ? ABSENT : formatInstant(iso, property, style);
}

/** An ISO day (`2026-09-03`) in the property's form, or the dash. */
export function day(
  iso: string | null,
  property: PropertyEnvironment,
  style: DayStyle,
): string {
  return iso === null ? ABSENT : formatDay(iso, property, style);
}

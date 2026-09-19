/**
 * Every count a Room Care screen shows, in the property's locale — the SDK's
 * `formatNumber` bound to what the host handed at connect (page 64 §12,
 * `NUM-Q1`, ADR 0174). A screen calls this and never `String()`,
 * `toLocaleString` or `Intl` for a number a person reads.
 *
 * Two kinds of number stay machine-form, and they are not display: a value a
 * person types back into a field (a grouped figure would not read back as a
 * number) and an attribute a browser reads (`colspan`, `aria-pressed`).
 */

import { formatNumber, type HostApi } from "@hotelos/sdk";

/** A count — whole, grouped as the property's locale groups, ungrouped where no locale is established. */
export function whole(host: HostApi, value: number): string {
  return formatNumber(value, host.property, "whole");
}

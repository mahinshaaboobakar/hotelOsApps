/**
 * What a catalogue row says about its use — written once, for both tables.
 *
 * Policy and Shifts draw the same catalogue, so the sentence belongs in one
 * place: two copies of a phrase drift exactly as two mappings of a colour did
 * (ledger D3), and the second copy is the one nobody updates.
 */

import { formatNumber, type PropertyEnvironment } from "@hotelos/sdk";

/**
 * How many assignments reference a shift, in words.
 *
 * @param count what the service sent — a number
 * @param property for the grouping the figure is written in
 * @returns the sentence
 *
 * @remarks
 * The service sent `"88 assignments"`, composed in its own culture: the noun
 * was English on every property's screen and the figure carried the service's
 * grouping, so a property that writes `1.234` read `1234`. NUM-Q1, ADR 0174.
 */
export function assignments(count: number, property: PropertyEnvironment): string {
  return `${formatNumber(count, property, "whole")} assignments`;
}

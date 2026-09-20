/**
 * Counting in English — the words a number turns into on these screens.
 *
 * ADR 0175 puts presentation at the reader, and a count is presentation twice
 * over: the DIGITS are a locale's, which is `formatNumber`'s job, and the
 * WORDS around them are a language's — which numbers get spelled, where the
 * plural falls, whether there is a word for *both*. The service sent finished
 * sentences until 2026-09-20 and could not have been right for a second
 * language.
 *
 * **This file is English, and says so.** It is not a translation layer and
 * pretending otherwise would be worse: a property reading Arabic gets English
 * words here today, and the honest form of that is one file naming the
 * language rather than the same assumption spread over four screens. When a
 * second language arrives this is the file it replaces.
 *
 * Nothing here formats a date or a number — `when.ts` and the SDK do that, in
 * the property's locale.
 */

/** Small counts, spelled the way the design spells them. */
const SPELLED = ["no", "one", "two", "three", "four"];

/**
 * `two`, or `17` — spelled while the design spells it, digits after that.
 *
 * The boundary is the design's and not a rule about English: *two stays* reads
 * as a sentence where *2 stays* reads as a field, and past four the spelling
 * stops helping. Anything above it is a plain number, and a large count that
 * wants grouping is the SDK's `formatNumber`, not this.
 */
export function spell(count: number): string {
  return SPELLED[count] ?? String(count);
}

/** `two stays`, `one stay` — the count and its noun, agreeing. */
export function many(count: number, one: string, other: string): string {
  return `${spell(count)} ${count === 1 ? one : other}`;
}

/** `Two stays`, for a sentence that starts with it. */
export function capital(text: string): string {
  return text.charAt(0).toUpperCase() + text.slice(1);
}

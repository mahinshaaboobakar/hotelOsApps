/**
 * What a mock's developer notes look like when they reach a screen, and finding them in text.
 *
 * Owner ruling, 2026-09-19: a mock carries two kinds of content — the screen, and notes for the
 * developer (where a figure comes from, why a choice was made, which ruling governs, which system
 * answers). The second is never built as UI, and no live screen shows staff a design-section
 * reference, an ADR, a register id, or a service or system name.
 *
 * **The union of three guards that had grown apart**, one copy now:
 *
 * ```text
 * Workforce   b001bfac → developer-content.test.ts   register ids · ADRs · § · "design page"
 *                                                    · the platform systems by name
 * GuestOps    8aae94e  document-citations.test.ts    "design §6" · "§4.2" · ADRs spaced or
 *                                                    hyphenated · register ids
 * shared      15e2654  scripts/developer-content.ts  "(S3 …)" · "(row 4)" · "Chapter 11"
 *                                                    · snake_case identifiers · correlation ids
 * ```
 *
 * Where two of them spelled one shape differently the wider spelling is kept: an ADR matches with
 * any run of spaces or hyphens before its number, and a section sign matches with or without a
 * number after it.
 *
 * **What no pattern can find, stated rather than implied**: a rationale in plain words — *"this
 * row would lie"*, *"from the location tree"* — has no shape. Those are found by reading every
 * rendered sentence, and each application's capability ledger records what its reading found.
 */

/** Each shape, named for what it is — the name is what a failure message shows. */
export const DEVELOPER_NOTES: readonly (readonly [string, RegExp])[] = [
  ["a register id", /\b[A-Z]+-Q[0-9]+[a-z]?\b/gu],
  ["an ADR", /\bADR[\s-]*[0-9]+/gu],
  ["a section reference", /\bdesign\s*§\s*[\d.]*|§\s*[\d.]*/giu],
  ["a design-section reference", /\(S[0-9]+\b[^)]*\)/gu],
  ["a design-row reference", /\(row [0-9]+\)/gu],
  ["a chapter reference", /\bChapter [0-9]+/gu],
  ["a design page", /\bdesign page\b/giu],
  ["a code identifier", /\b[a-z]+(?:_[a-z]+)+\b/gu],
  ["a correlation id", /\bcorrelation ids?\b/giu],
  // Only the SDK's comments name these — none of its strings do — so an application meeting this
  // shape on a failure state wrote the sentence itself (measured 2026-09-19).
  ["a platform system", /\b(?:Master Data|Kernel|OpenFGA|Context Service|Integration Hub)\b/gu],
];

/**
 * Every developer note in `text`, each as `what it is: "the text found"`.
 *
 * @param text what a person reads — see `readableText`
 * @returns the notes found, in list order; empty when there are none
 */
export function developerNotes(text: string): string[] {
  return DEVELOPER_NOTES.flatMap(([what, shape]) =>
    [...text.matchAll(shape)].map((found) => `${what}: "${found[0]}"`));
}

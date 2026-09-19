/**
 * Developer notes never reach a screen — the one guard every application's rendered-text walk
 * imports (owner ruling, 2026-09-19).
 *
 * Composition only: the shapes and their finder are `notes.ts`, what a person can read is
 * `readable.ts`. An application walks its own surfaces and asks
 * `developerNotes(readableText(surface))` to be empty.
 */

export { DEVELOPER_NOTES, developerNotes } from "./notes";
export { readableText } from "./readable";

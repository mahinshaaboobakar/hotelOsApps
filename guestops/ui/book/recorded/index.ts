/**
 * The facts the screens draw when the platform cannot be reached.
 *
 * **These are the approved frames' own data**, transcribed — the same guests,
 * rooms, references and times the gold mockup draws. That is deliberate: the
 * screens are being built to those frames, and a fixture invented separately
 * would make every capture a comparison against something nobody approved.
 *
 * They live behind `load()` in `book/index.ts`, so a screen never chooses
 * between live and recorded — it asks once and is told which it got.
 *
 * One file per frame, because a fixture is read beside the frame it transcribes
 * and a single file would be five transcriptions with one name (ADR 0038).
 */

export * from "./attention";
export * from "./availability";
export * from "./booking";
export * from "./day";
export * from "./stay";
export * from "./tabs";

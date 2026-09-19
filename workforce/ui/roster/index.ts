/**
 * The roster, as this module can reach it — the one write seam.
 *
 * Every screen writes through `write`. Nothing else in this module raises a
 * refusal, so a dialog's failure path is one file and no screen knows what the
 * transport is.
 *
 * # The read half moved to the SDK
 *
 * `Read<T>`, `ReadFailure` and `load` were here, and three bundles each had or
 * would have written their own copy. They now live in `@hotelos/sdk` and every
 * screen imports them from there — **anything two components must agree on
 * lives in one place**, and what would have drifted is the half nobody reads
 * until a person is standing in front of it: whether a fault offers a retry
 * that cannot succeed, whether the service's own sentence crosses.
 *
 * The write half stays. A refusal is this application's vocabulary — its
 * message is the one a supervisor acts on — and nothing outside Workforce has
 * asked for it.
 *
 * # Neither a read nor a write falls back
 *
 * This said *a read falls back; a write never does*, and described `load`
 * answering from `recorded.ts` when the platform could not. `APPS-Q26(4)`
 * rejected that mechanism, not merely its treatment: **a screen that cannot
 * read its data shows what failed and why, never a plausible list with an
 * apology under it.** Marking a fabrication honestly is still rendering one.
 *
 * So a failed read returns a `ReadFailure` and the screen draws it, exactly as
 * a refused write raises and the screen draws the refusal.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

export * from "./model";
// **The recorded fixtures are NOT re-exported.** They survive for previews and
// captures, where nobody mistakes them for a property's work, and they must not
// be reachable from the shipped path through this seam — `APPS-Q26(4)`. A
// barrel that re-exported them is how a screen imports one without deciding to.

/**
 * What a failed save may say when nothing answered: the save may have gone
 * through, so the outcome is not known and a blind retry could do it twice.
 */
export const NO_ANSWER =
  "No answer came back, so it is not known whether that went through. "
  + "Check before trying again.";

/**
 * What a failed save may say when the service reported a fault: it answered,
 * but nothing establishes that it saved none of it.
 */
export const FAULTED =
  "The service reported a fault, so it is not known whether any of that was saved. "
  + "Check before trying again.";

/**
 * What a dialog says when its save failed in a way it did not expect — an
 * exception it rethrows, from before or after the call. Whether the write ran
 * is not known, so the sentence does not claim either.
 */
export const UNKNOWN_OUTCOME =
  "Something went wrong, so it is not known whether that went through. "
  + "Check before trying again.";

/** What a failed save may say when the platform refused before anything ran. */
export const REFUSED = "That did not go through. Nothing was changed.";

/**
 * Which failures refused the write before it ran — so "nothing was changed" is
 * true of them and of nothing else. `model_unavailable` is here because an
 * authority that could not decide never let the call reach the service.
 */
const REFUSALS = new Set(["forbidden", "local_forbidden", "user_forbidden", "model_unavailable"]);

/** Why a write did not happen, in the words a person may be shown. */
export class WriteRefused extends Error {
  /**
   * @param message what to show, already filtered by ADR 0041
   * @param kind the platform's own classification
   */
  constructor(message: string, readonly kind: string) {
    super(message);
    this.name = "WriteRefused";
  }
}

/**
 * Write, and let the refusal reach the screen.
 *
 * @param host the bridge
 * @param capability the permission this write is approved under
 * @param method the application's own verb
 * @param params what the write needs
 * @returns whatever the handler answered
 * @throws WriteRefused when the platform or the service said no
 *
 * @remarks
 * **No fallback, and no swallowing.** Every refusal the person may see is
 * raised with its message; the kinds ADR 0041 keeps for a log are raised with
 * a sentence that says something happened and nothing about what. Returning a
 * quiet failure here would produce the worst outcome available: a dialog that
 * closes on a write that never occurred.
 */
export async function write(
  host: HostApi,
  capability: string,
  method: string,
  params: unknown,
): Promise<unknown> {
  if (!host.identity.capabilities.includes(capability)) {
    throw new WriteRefused(
      "This property has not granted Workforce permission to do that.", "forbidden");
  }

  try {
    return await host.call(capability, method, params);
  } catch (error) {
    if (error instanceof HostCallError) {
      // The diagnostic kinds never reach a screen — ADR 0041. What replaces
      // them is decided by what is KNOWN about the write, per kind.
      //
      // This said "That did not go through. Nothing was changed." for all of
      // them, under a comment claiming *"the sentence says the truth a person
      // can act on: it did not happen."* **For a save nobody answered, that was
      // never known** — it may have gone through, and the sentence invited
      // pressing Save again (KK's finding in Room Care, 2026-09-19). It stays
      // only where the platform refused before anything ran.
      throw new WriteRefused(
        error.isForPeople
          ? error.message
          : REFUSALS.has(error.kind)
            ? REFUSED
            : error.kind === "internal"
              ? FAULTED
              : NO_ANSWER,
        error.kind);
    }

    throw error;
  }
}

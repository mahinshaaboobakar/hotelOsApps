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
      throw new WriteRefused(
        error.isForPeople
          ? error.message
          // `internal` and `unavailable` carry a diagnostic, and putting one on
          // a hotel's screen leaks a platform detail to a supervisor. The
          // sentence says the truth a person can act on: it did not happen.
          : "That did not go through. Nothing was changed.",
        error.kind);
    }

    throw error;
  }
}

/**
 * The roster, as this module can reach it — the one data seam.
 *
 * Every screen reads through `load` and writes through `write`. Nothing else in
 * this module touches `host.call`, so the transport is one file and no screen
 * knows what it is.
 *
 # Neither a read nor a write falls back
 *
 * This said *a read falls back; a write never does*, and described `load`
 * answering from `recorded.ts` when the platform could not — telling the screen
 * which it got, so a manager could tell whether they were looking at their
 * hotel. `APPS-Q26(4)` rejected that mechanism, not merely its treatment: **a
 * screen that cannot read its data shows what failed and why, never a plausible
 * list with an apology under it.** Marking a fabrication honestly is still
 * rendering one.
 *
 * So a failed read returns a [`ReadFailure`] and the screen draws it, exactly as
 * a refused write raises and the screen draws the refusal. The two halves of
 * this seam now behave the same way, which is what the old comment's own
 * reasoning had always implied and stopped one step short of.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { causeOf, type ReadFailure } from "../chrome/failure";

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
 * What a read produced: the property's data, or the reason there is none.
 *
 * **There is no third case, and that is the change.** This was
 * `Loaded<T>` — a value, a `live` flag and a reason — and the value was a
 * recorded fixture whenever the read failed. `APPS-Q26(4)` rejected that
 * mechanism outright: *a screen that cannot read its data shows what failed
 * and why, never a plausible list with an apology under it.*
 *
 * A discriminated union rather than a flag, so a screen **cannot** reach the
 * data without having answered whether there is any. The old shape let a
 * caller read `.value` and never look at `.live`, which is exactly what five
 * of this application's eleven screens did.
 */
export type Read<T> =
  | { readonly ok: true; readonly value: T }
  | { readonly ok: false; readonly failure: ReadFailure };

/**
 * Ask the platform, and report what happened when it could not answer.
 *
 * @param host the bridge, and the only route out of this realm
 * @param capability the permission the manifest requested
 * @param method the operation within it
 * @param params what the question needs — which page, for the one list that has
 *   them. Absent for every other read, which is bounded by a natural key.
 * @returns the property's data, or the reason there is none — never both, and
 *   never a stand-in for either
 */
export async function load<T>(
  host: HostApi,
  capability: string,
  method: string,
  params?: unknown,
): Promise<Read<T>> {
  const at = new Date();

  // Asking for a capability that was not granted is not worth a round trip,
  // and the answer is the same one the service would give: refused, naming
  // what is missing. `forbidden` rather than a quiet fixture.
  if (!host.identity.capabilities.includes(capability)) {
    return { ok: false, failure: { cause: "forbidden", capability, method, said: null, at } };
  }

  try {
    return { ok: true, value: (await host.call(capability, method, params)) as T };
  } catch (error) {
    if (error instanceof HostCallError) {
      return {
        ok: false,
        failure: {
          cause: causeOf(error.kind),
          capability,
          method,
          // Only what ADR 0041 permits a person to see. A fault's own words
          // never crossed the boundary, so this is null and the screen says
          // so rather than leaving a blank where a reason should be.
          said: error.isForPeople ? error.message : null,
          at: new Date(),
        },
      };
    }

    throw error;
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

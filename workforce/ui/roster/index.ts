/**
 * The roster, as this module can reach it — the one data seam.
 *
 * Every screen reads through `load` and writes through `write`. Nothing else in
 * this module touches `host.call`, so the transport is one file and no screen
 * knows what it is.
 *
 * # A read falls back; a write never does
 *
 * `load` answers from `recorded.ts` when the platform cannot, and tells the
 * screen which it got — **a module that hid the difference is one somebody
 * eventually acts on**, and a manager must be able to tell whether they are
 * looking at their hotel.
 *
 * `write` has no such fallback and must not grow one. There is nothing to fall
 * back *to*: a button that reported success against recorded data would tell
 * somebody their rota was filled when nothing was written. So a refused write
 * raises, and the screen renders the refusal.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

export * from "./model";
export * from "./recorded";

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

/** What a screen got, and whether it is the property's own data. */
export interface Loaded<T> {
  value: T;

  /** True when this came from the platform. Screens render it. */
  live: boolean;

  /** Why it is not live, when it is not — shown only if ADR 0041 permits. */
  because: string | null;
}

/**
 * Ask the platform, and fall back to the recorded facts.
 *
 * @param host the bridge, and the only route out of this realm
 * @param capability the permission the manifest requested
 * @param method the operation within it
 * @param recorded what to show when the platform cannot answer
 * @param params what the question needs — which page, for the one list that has
 *   them. Absent for every other read, which is bounded by a natural key.
 * @returns the value, and whether it is real
 */
export async function load<T>(
  host: HostApi,
  capability: string,
  method: string,
  recorded: T,
  params?: unknown,
): Promise<Loaded<T>> {
  // Asking for a capability that was not granted is not worth a round trip, and
  // the refusal would read as an outage rather than as a permission a property
  // chose not to give.
  if (!host.identity.capabilities.includes(capability)) {
    return { value: recorded, live: false, because: null };
  }

  try {
    return {
      value: (await host.call(capability, method, params)) as T,
      live: true,
      because: null,
    };
  } catch (error) {
    if (error instanceof HostCallError) {
      // ADR 0041, asked by the SDK so a package does not rediscover the rule:
      // `internal` and `forbidden` carry a message for a log, and putting one on
      // a hotel's screen leaks a platform diagnostic to a supervisor.
      return { value: recorded, live: false, because: error.isForPeople ? error.message : null };
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

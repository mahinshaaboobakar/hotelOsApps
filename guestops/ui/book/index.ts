/**
 * The reservation book, as this module can reach it — the one data seam.
 *
 * Every screen reads through `load`. Nothing else in this module touches
 * `host.call`, so the day the desktop grows a GuestOps client there is exactly
 * one file to change and no screen knows it happened.
 *
 * # Why the recorded facts sit behind this seam
 *
 * **The desktop has no GuestOps gRPC client.** The bridge answers a capability
 * by dispatching to a client the shell holds, and there is none for this
 * application — the same gap BB reported for integration. A call therefore
 * fails `unavailable` today and will succeed unchanged when that client lands.
 *
 * The fallback is `recorded.ts`, and screens are told which they got so they
 * can say so. A module that hid the difference is one somebody eventually acts
 * on.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

export * from "./model";
export * from "./recorded";

/** What a screen got, and whether it is the property's own data. */
export interface Loaded<T> {
  value: T;

  /**
   * True when this came from the platform.
   *
   * Screens render it. A person looking at a stay must be able to tell whether
   * they are seeing their hotel or a stand-in.
   */
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
 * @returns the value, and whether it is real
 */
export async function load<T>(
  host: HostApi,
  capability: string,
  method: string,
  recorded: T,
): Promise<Loaded<T>> {
  // Asking for a capability that was not granted is not worth a round trip, and
  // the refusal would read as an outage rather than as a permission a property
  // chose not to give.
  if (!host.identity.capabilities.includes(capability)) {
    return { value: recorded, live: false, because: null };
  }

  try {
    return { value: (await host.call(capability, method)) as T, live: true, because: null };
  } catch (error) {
    if (error instanceof HostCallError) {
      // ADR 0041, asked by the SDK so a package does not rediscover the rule:
      // `internal` and `forbidden` carry a message for a log, and putting one
      // on a hotel's screen leaks a platform diagnostic to a receptionist.
      return { value: recorded, live: false, because: error.isForPeople ? error.message : null };
    }

    throw error;
  }
}

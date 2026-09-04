/**
 * The one data seam — every screen reads through `load`; nothing else in the
 * module touches `host.call`. The desktop has no Jobs gRPC client yet, so a
 * call fails `unavailable` today and the recorded facts stand in, with the
 * screen told which it got (ADR 0041). The day the client lands, this file
 * changes and no screen knows.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

export * from "./model";

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
 */
export async function load<T>(
  host: HostApi,
  capability: string,
  method: string,
  recorded: T,
  params?: unknown,
): Promise<Loaded<T>> {
  if (!host.identity.capabilities.includes(capability)) {
    return { value: recorded, live: false, because: null };
  }

  try {
    return { value: (await host.call(capability, method, params)) as T, live: true, because: null };
  } catch (error) {
    if (error instanceof HostCallError) {
      return { value: recorded, live: false, because: error.isForPeople ? error.message : null };
    }

    throw error;
  }
}

/** Whether the viewer may do this — the host's word, never the screen's guess. */
export function may(host: HostApi, capability: string): boolean {
  return host.identity.capabilities.includes(capability);
}

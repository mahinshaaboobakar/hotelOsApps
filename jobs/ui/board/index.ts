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

/** What a control got when it acted — and what to tell the person if it failed. */
export interface Acted {
  ok: boolean;

  /** The service's own sentence, when it refused in words a person can act on. */
  refused: string | null;

  /** What came back, for a caller that needs the new version or the new id. */
  value: unknown;
}

/**
 * Do something, and say what happened.
 *
 * The other half of {@link load}: a control that acts must know whether it did,
 * because the screen has to redraw from the service afterwards rather than from
 * what it hoped. A refusal is a sentence, never a thrown error — the service
 * wrote it for the person on shift, and "something went wrong" is what a screen
 * shows when it discards it.
 *
 * @param host the bridge, and the only route out of this realm
 * @param capability the permission this act needs
 * @param method the operation within it
 * @param params what the service is being asked to do
 */
export async function act(
  host: HostApi,
  capability: string,
  method: string,
  params?: unknown,
): Promise<Acted> {
  if (!host.identity.capabilities.includes(capability)) {
    return { ok: false, refused: "you do not have permission to do that here", value: null };
  }

  try {
    return { ok: true, refused: null, value: await host.call(capability, method, params) };
  } catch (error) {
    if (error instanceof HostCallError) {
      return {
        ok: false,
        refused: error.isForPeople ? error.message : "that could not be done just now",
        value: null,
      };
    }

    throw error;
  }
}

/** Whether the viewer may do this — the host's word, never the screen's guess. */
export function may(host: HostApi, capability: string): boolean {
  return host.identity.capabilities.includes(capability);
}

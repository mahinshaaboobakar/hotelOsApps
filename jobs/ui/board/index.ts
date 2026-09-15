/**
 * The write seam — and the read seam is the SDK's now.
 *
 * `load` lived here and took a `recorded` argument: a screen that could not
 * reach the platform drew the approved example instead. **The argument is
 * retired, not the branch** — with nowhere to pass a fallback, no screen can
 * reach one, which is the difference between a rule and a shape. Reads go
 * through `@hotelos/sdk`'s `load`, whose `Read<T>` carries a value or a reason
 * and never both (`38c5855e`, GG).
 *
 * Writes stay here: they answer a different question — what a control got when
 * it acted, and what to tell the person when it was refused.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

export * from "./model";

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

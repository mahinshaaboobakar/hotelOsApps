/**
 * Reading from Room Care's backend — the one seam, and the only file that
 * touches `host.call` for a read or a write.
 *
 * # A stand-in is not expressible
 *
 * Owner, 2026-09-09 (`APPS-Q26(4)`, `APPS-Q42`): *"Showing a hardcoded list is
 * wrong."* — and marking a fabrication honestly is still rendering one. So a
 * read produces the property's data or the reason there is none, never both
 * and never a stand-in for either. The shape is GuestOps' and Workforce's
 * `Read<T>`, with Workforce's typed failure, so a screen **cannot** reach a
 * value without first answering whether there is one.
 *
 * **There is no fallback parameter, and its absence is the mechanism.** The
 * recorded fixtures live in `preview/` for the harness and the tests; nothing
 * on the shipped path imports them, and `tests/seam.test.ts` parses the source
 * to keep it so.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { causeOf, type ReadFailure } from "./failure";

export const READ = "roomcare.read";

/** What a read produced: the property's data, or the reason there is none. There is no third case. */
export type Read<T> =
  | { readonly ok: true; readonly value: T }
  | { readonly ok: false; readonly failure: ReadFailure };

/** What a write did: its answer, or the service's own sentence for why not. */
export type Acted<T> =
  | { readonly ok: true; readonly value: T }
  | { readonly ok: false; readonly because: string };

/**
 * Ask Room Care's backend.
 *
 * @param method the operation within the capability
 * @param params this application's own body — the page, a room's id
 * @param capability the permission the manifest requested; reads are `roomcare.read`
 * @returns the value, or why there is none
 */
export async function load<T>(host: HostApi, method: string, params?: unknown, capability = READ): Promise<Read<T>> {
  const at = new Date();
  // A capability this person was not granted is a decision somebody made, not
  // an outage — answered without a round trip, naming what is missing.
  if (!holds(host, capability)) return { ok: false, failure: { cause: "forbidden", capability, method, said: null, at } };

  try {
    return { ok: true, value: (await host.call(capability, method, params)) as T };
  } catch (error) {
    if (!(error instanceof HostCallError)) throw error;
    return { ok: false, failure: { cause: causeOf(error.kind), capability, method, said: error.isForPeople ? error.message : null, at: new Date() } };
  }
}

/** Run a write; answer the service's own sentence when it refuses. */
export async function act(host: HostApi, capability: string, method: string, params?: unknown): Promise<Acted<unknown>> {
  try {
    return { ok: true, value: await host.call(capability, method, params) };
  } catch (error) {
    if (!(error instanceof HostCallError)) throw error;
    return { ok: false, because: saying(error) };
  }
}

/** The words a person sees for a refused act — the service's own when ADR 0041 lets them cross. */
export function saying(error: unknown): string {
  if (error instanceof HostCallError) {
    return error.isForPeople ? error.message : "Room Care could not do that just now — nothing was changed.";
  }
  return "Room Care did not answer — nothing was changed.";
}

/** Whether this person, in this module, was granted a capability. */
export function holds(host: HostApi, capability: string): boolean {
  return host.identity.capabilities.includes(capability);
}

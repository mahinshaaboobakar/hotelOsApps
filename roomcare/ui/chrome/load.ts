/**
 * Room Care's one seam to its backend — the SDK's read, and this module's write.
 *
 * # The read is the SDK's, and there is no second copy
 *
 * `load`, `Read` and `ReadFailure` are `@hotelos/sdk`'s (`read.ts`), shared with
 * GuestOps, Jobs and Workforce. Room Care kept its own copy of the same union
 * and the same three causes until 2026-09-18 — correct, and exactly the drift
 * `read.ts`'s header warns about: the half that differs between copies is what
 * each one refuses to say. So nothing here re-states a cause, a mapping or a
 * sentence; screens import the seam from this file, and this file only
 * re-exports it.
 *
 * **A stand-in is not expressible** (owner, 2026-09-09: *"Showing a hardcoded
 * list is wrong."*). The failing side of `Read<T>` has no value, and `load` has
 * no fallback parameter to supply one. The recorded fixtures live in `preview/`
 * for the harness and the suites; `tests/seam.test.ts` parses the shipped source
 * so nothing that ships can reach them.
 *
 * # The write stays here
 *
 * The SDK classifies a read; a write's refusal is a sentence a person acts on,
 * kept open in its sheet (page 64 §9) — GuestOps keeps its write half for the
 * same reason.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

export { load } from "@hotelos/sdk";
export type { Read, ReadFailure } from "@hotelos/sdk";

/** The capability every Room Care read is made under. */
export const READ = "roomcare.read";

/** What a write did: its answer, or the service's own sentence for why not. */
export type Acted<T> =
  | { readonly ok: true; readonly value: T }
  | { readonly ok: false; readonly because: string };

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

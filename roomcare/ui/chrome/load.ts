/**
 * Reading from Room Care's backend — and saying so when it could not.
 *
 * **A failed read renders a failure, never data** (page 64 §8, FF's harness
 * rule applied to the product). There is no recorded example to fall back on:
 * a screen that showed yesterday's approved fixture when the service did not
 * answer would be a board that looks current when it is not.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { el } from "./element";

export const READ = "roomcare.read";

export type Loaded<T> = { ok: true; value: T } | { ok: false; because: string };

export async function load<T>(host: HostApi, method: string, params?: unknown, capability = READ): Promise<Loaded<T>> {
  try {
    return { ok: true, value: (await host.call(capability, method, params)) as T };
  } catch (error) {
    return { ok: false, because: sentence(error) };
  }
}

/** Run a write; answer the service's own sentence when it refuses. */
export async function act(host: HostApi, capability: string, method: string, params?: unknown): Promise<Loaded<unknown>> {
  return load<unknown>(host, method, params, capability);
}

/** The words a person sees for a failure — the service's own when it sent some. */
export function sentence(error: unknown): string {
  if (error instanceof HostCallError) {
    return error.isForPeople ? error.message : "Room Care could not be reached just now — try again in a moment.";
  }
  return "Room Care did not answer.";
}

/** What a screen draws in place of its data when the read failed. */
export function failed(what: string, because: string): HTMLElement {
  const note = el("div", "note bad");
  note.append(el("b", undefined, `${what} could not be read. `), el("span", undefined, because));
  return note;
}

/** Whether this person, in this module, was granted a capability. */
export function holds(host: HostApi, capability: string): boolean {
  return host.identity.capabilities.includes(capability);
}

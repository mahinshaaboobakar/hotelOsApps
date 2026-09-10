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

/**
 * What a read returned: the property's data, or why there is none.
 *
 * **A union, so a stand-in is not expressible** — `APPS-Q42`. This was
 * `{ value, live, because }` with a recorded fallback in `value`, and every
 * screen drew the recorded facts under a banner saying they were not real. A
 * banner is read by whoever looks for it; a list of names is read by everyone.
 * Showing a hardcoded list is not made right by a label, and it is not made
 * right at 320×384 either — a widget is the frame most likely to be glanced at
 * and believed, because nobody opens one to interrogate it.
 *
 * The old shape could express *"here is data, and it is fake"*. This one cannot:
 * there is a value or there is a reason, and a screen that wants to draw
 * something has to have been given it.
 */
export type Read<T> =
  | { readonly ok: true; readonly value: T }
  | { readonly ok: false; readonly because: string };

/**
 * Ask the platform.
 *
 * @param host the bridge, and the only route out of this realm
 * @param capability the permission the manifest requested
 * @param method the operation within it
 * @param params the application's own JSON body — the page, a stay's id
 * @returns the value, or why there is none
 *
 * **There is no `recorded` parameter, and its absence is the mechanism.** While
 * one existed, every caller had a fallback within reach and sixteen of them took
 * it. Removing it means a screen cannot render recorded facts by accident: it
 * has nothing to render them from.
 *
 * `params` is this application's vocabulary, not the platform's: the envelope
 * defines the path and the status codes and nothing inside them, so a screen
 * and its backend agree on the body between themselves.
 */
export async function load<T>(
  host: HostApi,
  capability: string,
  method: string,
  params?: Record<string, unknown>,
): Promise<Read<T>> {
  // A capability the property did not grant is not an outage and does not read
  // as one. It is a decision somebody made, and the screen says which.
  if (!host.identity.capabilities.includes(capability)) {
    return {
      ok: false,
      because: `This property has not granted ${capability} to GuestOps.`,
    };
  }

  try {
    return { ok: true, value: (await host.call(capability, method, params)) as T };
  } catch (error) {
    if (error instanceof HostCallError) {
      // ADR 0041, asked by the SDK so a package does not rediscover the rule:
      // `internal` and `forbidden` carry a message for a log, and putting one on
      // a hotel's screen leaks a platform diagnostic to a receptionist. What is
      // left when the message may not be shown is still a reason, and it is
      // still not data.
      return {
        ok: false,
        because: error.isForPeople
          ? error.message
          : "GuestOps could not reach the platform. Nothing here is this property's.",
      };
    }

    throw error;
  }
}

/** What a write did, or why it did not. */
export interface Performed<T> {
  value: T | null;

  /** Null when it worked. The platform's own words when ADR 0041 permits them. */
  refused: string | null;
}

/**
 * Ask the platform to change something.
 *
 * @param host the bridge, and the only route out of this realm
 * @param capability the permission the manifest requested
 * @param method the operation within it
 * @param params the application's own JSON body
 * @returns what it did, or why it did not
 *
 * **There is no fallback, and that is the difference from `load`.** A read that
 * cannot reach the platform can show recorded facts and say so; a write that
 * cannot reach the platform has *not happened*, and anything that looked like
 * success would be a receptionist believing a room was released. So this
 * returns the refusal and the screen renders it.
 *
 * A capability the property did not grant is refused in the same shape rather
 * than thrown, because on this side of the seam it is the same fact: the thing
 * the person pressed did not happen, and they need to be told which reason.
 */
export async function perform<T>(
  host: HostApi,
  capability: string,
  method: string,
  params: Record<string, unknown>,
): Promise<Performed<T>> {
  if (!host.identity.capabilities.includes(capability)) {
    return {
      value: null,
      refused: `This property has not granted ${capability} to GuestOps.`,
    };
  }

  try {
    return { value: (await host.call(capability, method, params)) as T, refused: null };
  } catch (error) {
    if (error instanceof HostCallError) {
      return {
        value: null,
        refused: error.isForPeople
          ? error.message
          : "The platform refused this. Nothing was changed.",
      };
    }

    throw error;
  }
}

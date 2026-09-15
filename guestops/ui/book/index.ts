/**
 * The reservation book, as this module can reach it — the one data seam.
 *
 * Every screen reads through `load` and writes through `perform`. Nothing else
 * in this module touches `host.call`.
 *
 * # The read half is the SDK's now — `38c5855e`
 *
 * `Read<T>` and `load` were written here and are **GG's**, in
 * `@hotelos/sdk`, shared by three bundles. The local copy is deleted rather
 * than wrapped: a second definition of a union whose whole purpose is to make
 * one mistake unwriteable is exactly the thing that drifts, and what drifts
 * first is not the happy path — it is **what each copy refuses to say**, which
 * nobody reads until a person is standing in front of it.
 *
 * What the SDK's version has that this one did not: a **structured** failure
 * rather than a sentence. `cause` separates *did not answer* from *refused*
 * from *faulted*, and only the first offers a retry — so a screen can no longer
 * offer a button that cannot work. This module's copy collapsed all three into
 * a string, which read the same on every screen and told a person nothing about
 * whether waiting would help.
 *
 * # The write half stays here
 *
 * The SDK carries no `perform`, so {@link perform} and {@link Performed} are
 * still this module's. They are not re-exported as though they were the
 * platform's: a second application adopting them is the occasion to move them,
 * and until then a local seam that says it is local is honest.
 */

// `HostCallError` is still needed here: the write half below classifies its own
// failures, and the SDK's classifier is a read's.
import { HostCallError, type HostApi } from "@hotelos/sdk";

export * from "./model";

// **The fixtures are no longer re-exported here, and that is what got them out
// of the bundle.** Every screen typed its read as `load<typeof recordedToday>`
// — a type argument, which erases — but the borrow kept this re-export live, so
// `module.js` shipped with `Joseph Mathew` and `Same stay, or two?` inside it
// long after the last screen stopped drawing a fixture. The model types the
// fixtures are declared as were there the whole time; naming those instead
// costs nothing and takes the data with it.
//
// The harness and the suites import `book/recorded` directly. They are the two
// places the approved frames' data belongs, and neither ships.

// The read seam, re-exported so screens import from one place — `book` — and
// the file they import from is the only thing that knows where it comes from.
export { FAILURE_LABELS, causeOf, failureDrawing, load } from "@hotelos/sdk";
export type {
  Cause, FailureDrawing, FailureWords, Read, ReadFailure,
} from "@hotelos/sdk";

// Who is speaking, for every failure a screen draws. Defined in `../app` so the
// widget bundles can have the same string without importing the book, and
// re-exported here so a screen has one import rather than two.
export { APP } from "../app";

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

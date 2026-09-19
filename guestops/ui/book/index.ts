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

  /**
   * Whether it is KNOWN that nothing changed — null when it worked.
   *
   * `true` for a failure decided before anything ran; `false` where the write
   * may have landed (no answer, or a fault after it reached the service). A
   * screen that heads a failure with *"Nothing was cancelled"* reads this rather
   * than assuming it: the dialog said so over every failure, a non-answer
   * included, until 2026-09-19.
   */
  unchanged: boolean | null;
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
 * **There is no fallback**: anything that looked like success would be a
 * receptionist believing a room was released. So this returns why it did not
 * succeed, and the screen renders it.
 *
 * **What that sentence may claim depends on who stopped the write** — 2026-09-19,
 * the fault KK found in Room Care. This said *"a write that cannot reach the
 * platform has not happened"* and answered every unshowable error with *"The
 * platform refused this. Nothing was changed."* — a non-answer included. A
 * write that got no answer may have landed, and a person told nothing changed
 * presses again. (It also said a failed read "can show recorded facts", which
 * `APPS-Q42` had already removed.) See {@link outcome}; held by
 * `tests/perform.test.ts`.
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
    // Nothing was asked, so nothing changed.
    return {
      value: null,
      refused: `This property has not granted ${capability} to GuestOps.`,
      unchanged: true,
    };
  }

  try {
    return {
      value: (await host.call(capability, method, params)) as T,
      refused: null,
      unchanged: null,
    };
  } catch (error) {
    if (error instanceof HostCallError) {
      // `rejected` and `invalid` are the service deciding — a validation or a
      // domain refusal, and the one write this UI makes rolls back on either.
      return error.isForPeople
        ? { value: null, refused: error.message, unchanged: true }
        : { value: null, ...outcome(error.kind) };
    }

    throw error;
  }
}

/** Where the outcome of a write is not known, and what to do about it. */
const NOT_KNOWN = "so whether that was done is not known — check before trying again.";

/**
 * What a write that did not succeed may say about what happened.
 *
 * ```text
 * forbidden · local_forbidden · user_forbidden   decided before anything ran —
 * model_unavailable                              the one write this UI makes,
 *                                                cancelling a booking, commits
 *                                                in one transaction, so a
 *                                                refusal on any stay undoes all
 * internal                                       a fault after it reached the
 *                                                service: it may have landed
 * unavailable, and anything unknown              no answer: it may have landed
 * ```
 *
 * **The model state is not a refusal and never says one** — AUTHZ-Q34: it
 * *"must never be converted into a statement that the user lacks permission"*.
 * Nothing ran, so nothing changed; but the reason is that access could not be
 * checked, not that it was denied.
 *
 * **An unknown kind is "not known", never "nothing changed"**: a sentence that
 * cannot tell which happened must not choose the reassuring one.
 */
function outcome(kind: string): { refused: string; unchanged: boolean } {
  switch (kind) {
    case "forbidden":
    case "local_forbidden":
    case "user_forbidden":
      return { refused: "That was not permitted, so nothing was changed.", unchanged: true };
    case "model_unavailable":
      return {
        refused: "Whether that is permitted could not be checked, so nothing was changed.",
        unchanged: true,
      };
    case "internal":
      return { refused: `GuestOps could not finish that, ${NOT_KNOWN}`, unchanged: false };
    default:
      return { refused: `GuestOps did not answer, ${NOT_KNOWN}`, unchanged: false };
  }
}

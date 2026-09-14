/**
 * What a screen draws when it cannot read — owner, 2026-09-09 (`APPS-Q26(4)`):
 * a screen that cannot read its data shows what it could not read and why, and
 * offers the action that would fix it; never a plausible list with an apology.
 *
 * Workforce's three causes, so a person learns one vocabulary across the
 * applications — and three causes because they have three remedies:
 *
 *   unanswered   nothing is known, including whether there is anything. Retryable
 *   forbidden    the service refused. A retry cannot change it, so none is offered:
 *                the remedy is a grant, and the screen names it
 *   faulted      the service answered with its own fault or refused the question.
 *                It will not clear by asking again
 */

import { control, el } from "./element";

export type Cause = "unanswered" | "forbidden" | "faulted";

/** A read that failed, with everything the screen is allowed to say about it. */
export interface ReadFailure {
  cause: Cause;
  /** The capability asked for — named on a refusal, because it is the remedy. */
  capability: string;
  /** Room Care's own verb, so the line a person hands on names the call. */
  method: string;
  /** The platform's words when ADR 0041 lets a person see them; null otherwise, and said so. */
  said: string | null;
  /** When this screen observed the failure. */
  at: Date;
}

/**
 * The platform's kind as one of the three causes — Workforce's mapping.
 * `rejected` and `invalid` mean the service answered and declined (the shell
 * folds a missing handler onto `rejected` too), so they are never offered a
 * retry; the vocabulary has no *no handler* cause (`CORE-Q30`).
 */
export function causeOf(kind: string): Cause {
  switch (kind) {
    case "forbidden": return "forbidden";
    case "rejected":
    case "invalid":
    case "internal": return "faulted";
    default: return "unanswered";
  }
}

/** What could not be read, in words — never a claim that it is empty. */
export function sentence(failure: ReadFailure, subject: string): string {
  switch (failure.cause) {
    case "unanswered": return `Room Care did not answer, so ${subject} could not be read.`;
    case "forbidden": return `You do not have access to ${subject}.`;
    case "faulted": return `Room Care could not produce ${subject}.`;
  }
}

/** What it means, and what fixes it. */
export function remedy(failure: ReadFailure): string {
  switch (failure.cause) {
    case "unanswered": return "Nothing is known about it — not that it is empty. Asking again may reach it.";
    case "forbidden": return `This needs ${failure.capability}. It comes from your Workforce posting, or from the general manager's property-wide grant — ask whoever manages access at this property.`;
    case "faulted": return "Asking again will not change the answer. The line below is what to pass on.";
  }
}

/** The line a person hands to someone who can act — only what actually arrived. */
export function wire(failure: ReadFailure): string {
  const said = failure.said ?? (failure.cause === "forbidden" ? "not granted to this person" : "no reason crossed the boundary — logged by the service");
  return [failure.capability, failure.method, said, failure.at.toISOString()].join(" · ");
}

/**
 * Draw the failure in place of what could not be read.
 *
 * @param failure what went wrong, as the platform reported it
 * @param subject what this screen was reading — "the board", "this room"
 * @param retry re-run the read; offered only where asking again could work
 */
export function failed(failure: ReadFailure, subject: string, retry?: () => void): HTMLElement {
  const note = el("div", "note bad fail");
  note.append(el("b", undefined, sentence(failure, subject)), el("div", undefined, remedy(failure)), el("div", "mono", wire(failure)));
  if (failure.cause === "unanswered" && retry !== undefined) {
    const row = el("div", "row");
    row.style.marginTop = "10px";
    row.append(control("btn pri", "Try again", retry));
    note.append(row);
  }
  return note;
}

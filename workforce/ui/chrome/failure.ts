/**
 * What a screen draws when it cannot read — APPS-Q26(4), owner, 2026-09-09.
 *
 * # The mechanism, not the treatment, was rejected
 *
 * This replaces `standIn`. That drew the recorded fixture with a banner
 * admitting it, and the owner rejected all three stand-in treatments *and the
 * mechanism they dressed*: **a screen that cannot read its data shows what
 * failed and why, never a plausible list with an apology under it.** The gap
 * rule had already said it — *no value stands in for a measurement nobody
 * took* — and marking a fabrication honestly only made it polite.
 *
 * So there is no fallback here. A failed read renders a failure.
 *
 * # Three causes, three answers
 *
 * They are not one error with three messages. The causes have different
 * remedies, so they get different sentences and different actions — a timeout
 * and a refusal must never share a screen.
 *
 *   unanswered   the deadline expired. Nothing is known about the data,
 *                including whether any exists. Retryable
 *   forbidden    the service answered and refused. Retrying changes nothing,
 *                so no retry is offered: a button that cannot change the
 *                outcome is a second lie
 *   faulted      the service reached its own fault. Not the property's doing
 *                and it will not clear on its own
 *
 * # The frame stays; only the body is replaced
 *
 * The header and the column names remain, so a person keeps their bearings and
 * the screen never pretends the list is elsewhere. This draws the body.
 */

import { el } from "./element";

/** Why a read did not produce data. */
export type Cause = "unanswered" | "forbidden" | "faulted";

/**
 * A read that failed, with everything the screen is allowed to say about it.
 */
export interface ReadFailure {
  /** Which of the three, chosen by the platform's own kind. */
  cause: Cause;

  /** The capability asked for — named on a refusal, because it is the remedy. */
  capability: string;

  /** This application's own verb, so a fault names the call that failed. */
  method: string;

  /**
   * The platform's words, verbatim, or null when it gave none a person may see.
   *
   * **Null is the ordinary answer for a fault, and that is ADR 0041 working.**
   * The boundary maps anything unrecognised to `Internal` with a *generic*
   * message and logs the detail server-side, so the service's own sentence has
   * already been stripped before this module could render it. The approved
   * frame's example shows a fault line carrying the failing method and its
   * reason; that detail does not cross the wire, and inventing it here would be
   * the gap rule broken in the very surface written to honour it.
   */
  said: string | null;

  /** When this screen observed the failure. Not when the fault happened. */
  at: Date;
}

/** What the screen calls the thing it could not read — "the rota", "this board". */
export interface Subject {
  /** Used in the sentence: *Workforce could not build <the rota>*. */
  the: string;
}

/**
 * Draw the failure, in place of the body.
 *
 * @param failure what went wrong, as the platform reported it
 * @param subject what this screen was trying to read
 * @param retry re-run the read; omitted where a retry cannot succeed
 * @returns the body a person sees instead of rows
 */
export function failureBody(
  failure: ReadFailure,
  subject: Subject,
  retry?: () => void,
): HTMLElement {
  const body = el("div", "fail");

  body.append(
    mark(failure.cause),
    el("div", "fail-said", sentence(failure, subject)),
    el("div", "fail-why", why(failure.cause)),
    wire(failure),
  );

  const acts = actions(failure, retry);
  if (acts !== null) {
    body.append(acts);
  }

  return body;
}

/**
 * Where the call stopped — short of the service, turned back at it, or arrived
 * at a fault.
 *
 * **A diagram rather than an icon.** Three silhouettes a person learns once,
 * and each says something a symbol cannot: whether the service was reached at
 * all. An exclamation mark in a triangle would say *something is wrong* three
 * times over.
 */
export function mark(cause: Cause): HTMLElement {
  const glyph = el("div", `fail-mark fail-${cause}`);

  // Drawn from characters rather than an SVG: the realm has no network, so an
  // external sprite could not load, and an inline SVG for three marks is more
  // to keep in agreement than the marks are worth.
  glyph.textContent = cause === "unanswered" ? "· · ·"
    : cause === "forbidden" ? "· |"
    : "· ✕";

  glyph.setAttribute("aria-hidden", "true");
  return glyph;
}

/**
 * What could not be read, in the operator's words.
 *
 * **Careful not to claim the list is empty.** *Nothing here yet* would be a
 * measurement; this is the absence of one.
 */
export function sentence(failure: ReadFailure, subject: Subject): string {
  switch (failure.cause) {
    case "unanswered":
      return `Workforce did not answer in time`;
    case "forbidden":
      return `You do not have access to ${subject.the}`;
    case "faulted":
      return `Workforce could not build ${subject.the}`;
  }
}

/** The paragraph under the sentence — what it means, and what it does not. */
function why(cause: Cause): string {
  switch (cause) {
    case "unanswered":
      return "Nothing is known about this — not that it is empty, and not that "
        + "it is full.";
    case "forbidden":
      return "Workforce answered and refused. Nothing here is broken; this "
        + "account has not been granted the permission this screen needs.";
    case "faulted":
      return "The service reached its own fault. This is not something the "
        + "property has done, and it will not clear on its own.";
  }
}

/**
 * The platform's own words, in the monospace line.
 *
 * This is the line a person hands to somebody who can act, so it carries only
 * what actually arrived. Where the platform gave no message a person may see —
 * every fault, by ADR 0041 — the line says what this module knows for certain:
 * the capability, the verb, and the moment the failure was observed here.
 */
export function wire(failure: ReadFailure): HTMLElement {
  const parts = [failure.capability, failure.method];

  if (failure.said !== null) {
    parts.push(failure.said);
  } else if (failure.cause === "faulted") {
    // Stated, not omitted. A blank where a reason should be reads as a reason
    // nobody looked for; this says the platform withheld it deliberately and
    // where it went instead.
    parts.push("no reason crossed the boundary — logged by the service");
  } else if (failure.cause === "forbidden") {
    parts.push("no grant names this user at this property");
  }

  parts.push(failure.at.toISOString());

  return el("div", "fail-wire", parts.join(" · "));
}

/**
 * The action, and only one that could work.
 *
 * **Never a retry on a refusal.** A button that cannot change the outcome is a
 * second lie, so `forbidden` gets no *Try again* however convenient the
 * symmetry would be.
 */
function actions(failure: ReadFailure, retry?: () => void): HTMLElement | null {
  const row = el("div", "fail-acts");

  if (failure.cause === "unanswered" && retry !== undefined) {
    const again = el("button", "btn pri", "Try again");
    again.addEventListener("click", retry);
    row.append(again);
  }

  // Deliberately no second button. The approved frame offers *Open Operations
  // Center*, *Request access* and *Who can grant this* — none of which this
  // module can reach: a bundle calls only its own backend (design page 63 §3),
  // and Operations Center is another application. Drawing a control that
  // cannot act is the same defect as a retry that cannot succeed, so they are
  // absent and reported rather than drawn dead.
  return row.childElementCount > 0 ? row : null;
}

/**
 * The platform's kind, as one of the three causes.
 *
 * `rejected` and `invalid` are a write's business — a validation refusal a
 * person can act on — and never reach a read, so they are not mapped here.
 */
export function causeOf(kind: string): Cause {
  switch (kind) {
    case "forbidden":
      return "forbidden";
    case "internal":
      return "faulted";
    default:
      return "unanswered";
  }
}

/**
 * Replace a screen's contents with the failure, keeping the frame.
 *
 * @param main the screen's mount
 * @param title what this screen is, drawn where its header would be
 * @param failure what went wrong
 * @param subject what could not be read, for the sentence
 * @param retry re-run the read; omitted where a retry cannot succeed
 *
 * @remarks
 * **The header is the screen's name, not its data.** A real header carries the
 * day, the week or the person it is about, and a read that failed produced none
 * of those — so drawing the usual header would mean inventing the very values
 * the screen could not read. What survives is the part that is true regardless:
 * which screen this is. The approved frame keeps *header, column names, page*
 * so a person keeps their bearings; this keeps the half a failed read can
 * honestly supply, and the rest is reported rather than drawn from nothing.
 */
export function failureScreen(
  main: HTMLElement,
  title: string,
  failure: ReadFailure,
  subject: Subject,
  retry?: () => void,
): void {
  const head = el("div", "title");
  head.append(el("div", "ht", title));

  main.replaceChildren(head, failureBody(failure, subject, retry));
}

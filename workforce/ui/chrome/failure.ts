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
 * # The decisions moved to the SDK; the elements stayed
 *
 * The three causes, the kind mapping, every sentence and the rule that only an
 * unanswered read may be retried are now `@hotelos/sdk`'s, shared by the three
 * bundles that ship a UI. What remains here is what that package cannot supply:
 * it ships no CSS, so a function there returning an `HTMLElement` would emit
 * class names no application defines. **What must be identical is what a person
 * is told; the markup is ours.**
 *
 * The three causes still get three sentences and three actions, and a timeout
 * and a refusal still never share a screen — that reasoning simply lives one
 * package over now, where it can only be written once.
 *
 * # The frame stays; only the body is replaced
 *
 * The header and the column names remain, so a person keeps their bearings and
 * the screen never pretends the list is elsewhere. This draws the body.
 */

import { FAILURE_LABELS, failureDrawing, type FailureDrawing, type ReadFailure }
  from "@hotelos/sdk";

import { APPLICATION } from "./application";
import { el } from "./element";

/** What the screen calls the thing it could not read — "the rota", "this board". */
export interface Subject {
  /** Used in the sentence: *Workforce could not build <the rota>*. */
  the: string;
}

/**
 * The words for a failure, with this application speaking.
 *
 * @param failure what the read reported
 * @param subject what this screen was trying to read
 * @returns the parts of a failure surface, each already in its final words
 *
 * @remarks
 * The one call that supplies {@link APPLICATION}, so no screen has to remember
 * to. Every surface that draws a failure goes through here and they cannot
 * drift into naming the application differently.
 */
export function drawing(failure: ReadFailure, subject: Subject): FailureDrawing {
  return failureDrawing(failure, { app: APPLICATION, the: subject.the });
}

/**
 * Where the call stopped — short of the service, turned back at it, or arrived
 * at a fault.
 *
 * **A diagram rather than an icon.** Three silhouettes a person learns once,
 * and each says something a symbol cannot: whether the service was reached at
 * all. An exclamation mark in a triangle would say *something is wrong* three
 * times over.
 *
 * @param drawn the words and marks for this failure
 * @returns the glyph, hidden from a screen reader — the sentence beneath it
 *   carries the same fact in words
 */
export function markEl(drawn: FailureDrawing): HTMLElement {
  // Characters rather than an SVG: the realm has no network, so an external
  // sprite could not load, and the marks are the SDK's so that a person meets
  // the same three in every application.
  const glyph = el("div", `fail-mark fail-${drawn.cause}`, drawn.mark);

  glyph.setAttribute("aria-hidden", "true");
  return glyph;
}

/**
 * The platform's own words, in the monospace line.
 *
 * This is the line a person hands to somebody who can act, so it carries only
 * what actually arrived — and where the platform gave no message a person may
 * see, it says so. A blank where a reason should be reads as a reason nobody
 * looked for.
 *
 * @param drawn the words and marks for this failure
 * @returns the provenance line
 */
export function wireEl(drawn: FailureDrawing): HTMLElement {
  return el("div", "fail-wire", drawn.wire);
}

/**
 * Draw the failure, in place of the body.
 *
 * @param failure what went wrong, as the platform reported it
 * @param subject what this screen was trying to read
 * @param retry re-run the read; ignored where a retry cannot succeed
 * @returns the body a person sees instead of rows
 */
export function failureBody(
  failure: ReadFailure,
  subject: Subject,
  retry?: () => void,
): HTMLElement {
  const drawn = drawing(failure, subject);
  const body = el("div", "fail");

  body.append(
    markEl(drawn),
    el("div", "fail-said", drawn.said),
    el("div", "fail-why", drawn.why),
    wireEl(drawn),
  );

  const acts = actions(drawn, retry);
  if (acts !== null) {
    body.append(acts);
  }

  return body;
}

/**
 * The action, and only one that could work.
 *
 * **Never a retry on a refusal.** A button that cannot change the outcome is a
 * second lie, and the SDK decides which failures may offer one — so a screen
 * passing a `retry` for a refusal gets no button rather than a broken promise.
 */
function actions(drawn: FailureDrawing, retry?: () => void): HTMLElement | null {
  const row = el("div", "fail-acts");

  if (drawn.retryable && retry !== undefined) {
    const again = el("button", "btn pri", FAILURE_LABELS.retry);
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
 * Replace a screen's contents with the failure, keeping the frame.
 *
 * @param main the screen's mount
 * @param title what this screen is, drawn where its header would be
 * @param failure what went wrong
 * @param subject what could not be read, for the sentence
 * @param retry re-run the read; ignored where a retry cannot succeed
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

/**
 * What a screen draws when it has nothing to ask WITH.
 *
 * @param main the screen's mount
 * @param title what this screen is
 * @param said what cannot be shown, and why, in one sentence
 * @param why what it means for the person reading it
 *
 * @remarks
 * **Not a {@link failureScreen}, and the difference is the whole point.** That
 * one reports what the platform answered — a timeout, a refusal, a fault — and
 * carries the wire line a person hands to somebody who can act. This one
 * reports that *this bundle could not make the call at all*, because a value it
 * needs to ask with was never established. Nothing crossed the boundary, so
 * there is no capability, no method and no reason to quote, and drawing an
 * empty wire line would invent a call that never happened.
 *
 * Two absences that mean different things are two surfaces. Folding this into a
 * `ReadFailure` would have dressed a question nobody asked as an answer the
 * platform gave, and the remedies differ: one is retried or granted, this one
 * is somebody's record being made.
 */
export function unaskableScreen(
  main: HTMLElement,
  title: string,
  said: string,
  why: string,
): void {
  const head = el("div", "title");
  head.append(el("div", "ht", title));

  const body = el("div", "fail");
  const glyph = el("div", "fail-mark fail-unaskable", "· ?");
  glyph.setAttribute("aria-hidden", "true");

  body.append(glyph, el("div", "fail-said", said), el("div", "fail-why", why));
  main.replaceChildren(head, body);
}

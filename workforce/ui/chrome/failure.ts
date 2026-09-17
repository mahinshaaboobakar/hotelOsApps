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

import { failureDrawing, type FailureDrawing, type ReadFailure } from "@hotelos/sdk";

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
  // **Geometry, not a character** — page 64 §13. This drew `· · ·`, `· |` and
  // `· ✕` on a stated constraint: *the realm has no network, so an external
  // sprite could not load*. That was true about the NETWORK and was read as a
  // constraint about CHARACTERS. An inline `<svg>` loads nothing either, so the
  // reason never ruled out the better mark.
  const box = el("div", `fail-mark fail-${drawn.cause}`);
  const svg = document.createElementNS(SVG, "svg");

  svg.setAttribute("viewBox", drawn.glyph.viewBox);
  svg.setAttribute("fill", "none");
  svg.setAttribute("stroke", "currentColor");
  svg.setAttribute("stroke-width", "1.6");

  // Round caps are load-bearing rather than decorative: the SDK draws a dot as
  // a zero-length segment, which renders as nothing under a butt cap.
  svg.setAttribute("stroke-linecap", "round");
  svg.setAttribute("stroke-linejoin", "round");

  for (const d of drawn.glyph.paths) {
    const path = document.createElementNS(SVG, "path");
    path.setAttribute("d", d);
    svg.append(path);
  }

  box.append(svg);
  box.setAttribute("aria-hidden", "true");
  return box;
}

/** The SVG namespace — `createElement` builds an HTML element that never draws. */
const SVG = "http://www.w3.org/2000/svg";

/**
 * The four facts, each with its label.
 *
 * @param drawn the words and marks for this failure
 * @returns a definition list a person can read one line at a time
 *
 * @remarks
 * **They were one dotted line and that is what the owner objected to.** Every
 * fact in `roster.read · me · no grant names this user · 2026-09-17T08:53:23Z`
 * was correct, and the most useful part of it was set as the least readable —
 * a log entry on a screen. The line survives for the clipboard, where a run-on
 * is the right shape, and the screen gets rows.
 */
export function factsEl(drawn: FailureDrawing): HTMLElement {
  const list = el("div", "fail-facts");

  for (const fact of drawn.facts) {
    const row = el("div", "fail-fact");
    row.append(el("dt", "fail-fk", fact.label), el("dd", "fail-fv", fact.value));
    list.append(row);
  }

  return list;
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
 * The state, in two or three words — `Not permitted`.
 *
 * Above the sentence rather than inside it: a person scanning a screen they did
 * not expect reads the state first and the explanation second.
 */
export function labelEl(drawn: FailureDrawing): HTMLElement {
  return el("div", `fail-label fail-${drawn.cause}`, drawn.label);
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
    labelEl(drawn),
    el("div", "fail-said", drawn.said),
    el("div", "fail-why", drawn.why),
    factsEl(drawn),
  );

  body.append(actions(drawn, retry));
  return body;
}

/**
 * The action, and only one that could work.
 *
 * **Never a retry on a refusal.** A button that cannot change the outcome is a
 * second lie, and the SDK decides which failures may offer one — so a screen
 * passing a `retry` for a refusal gets no button rather than a broken promise.
 *
 * @remarks
 * **Switched on `kind`, never read for a `label`.** `grant` deliberately
 * carries none: a refusal does not name who can grant, so there is nothing to
 * put on a button, and the owner's ruling is expressed as the field's absence
 * rather than as a rule somebody has to remember. Code that reached for
 * `act.label` here would compile against the old shape and fail on this one —
 * so the note is drawn for every kind and the button only where a label exists
 * to sit on it.
 */
function actions(drawn: FailureDrawing, retry?: () => void): HTMLElement {
  const row = el("div", "fail-acts");
  const act = drawn.act;

  switch (act.kind) {
    case "retry":
      // The SDK says a retry is offerable; the screen says whether it has one
      // to run. Both are required, and neither implies the other.
      if (retry !== undefined) {
        const again = el("button", "btn pri", act.label);
        again.addEventListener("click", retry);
        row.append(again);
      }
      break;

    case "copy": {
      const copy = el("button", "btn", act.label);
      copy.addEventListener("click", () => {
        void navigator.clipboard?.writeText(drawn.wire);
      });
      row.append(copy);
      break;
    }

    case "grant":
      // No control, on purpose. The approved frame offered *Request access* and
      // *Who can grant this*; a bundle calls only its own backend (design page
      // 63 §3), so neither could act — and the platform does not know who holds
      // the grant either, which is why this arm has no label to draw.
      break;
  }

  row.append(el("div", "fail-note", act.note));
  return row;
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

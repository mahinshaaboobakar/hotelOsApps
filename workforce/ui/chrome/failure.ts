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

import {
  failureDrawing, formatInstant, type Fact, type FailureDrawing, type Phrase,
  type PropertyEnvironment, type ReadFailure,
} from "@hotelos/sdk";

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
export function factsEl(drawn: FailureDrawing, property: PropertyEnvironment): HTMLElement {
  const list = el("dl", "fail-facts");

  for (const fact of drawn.facts) {
    const row = el("div", "fail-fact");
    const value = el("dd", "fail-fv");
    value.append(...factValue(fact, property));
    row.append(el("dt", "fail-fk", fact.label), value);
    list.append(row);
  }

  return list;
}

/**
 * One fact's value, composed for the reader from its typed parts.
 *
 * @remarks
 * **The SDK hands over values, not only finished strings.** That lets this
 * screen format the instant in the property's locale and timezone
 * (JOBS-Q1(8)) instead of showing ISO.
 *
 * **`asked` draws the words, never the code name — owner, `64g` §2 B.** This
 * returned `roster.read · week`: a permission's code name and its method, on a
 * card a receptionist reads. The SDK moved the plain words onto `Fact.value`
 * on 2026-09-20 and says of `permission` and `method` that they are *"there
 * for the clipboard and for diagnostics, and **not for drawing**"* — and this
 * surface went on drawing them, alone among the four applications. The
 * separator this comment used to claim belonged here was the separator between
 * two things that are no longer drawn.
 *
 * **`date-time` is the closest style available, and it is not what 64b
 * draws.** The frame shows the year and the seconds, and no InstantStyle
 * carries seconds. That is noted here, not approximated. Whether to add the
 * style is the SDK's decision, not something an application should invent.
 */
function factValue(fact: Fact, property: PropertyEnvironment): Node[] {
  switch (fact.kind) {
    case "asked":
      return [document.createTextNode(fact.value)];
    case "at":
      return [document.createTextNode(formatInstant(fact.at, property, "date-time"))];
    case "answer":
      return [document.createTextNode(fact.value)];
  }
}

/**
 * A phrase, with its permission and its emphasis set apart.
 *
 * Both are drawn bold because 64b draws both bold. They are two run kinds in the
 * SDK because they are two facts — a name a person quotes, a clause the
 * sentence stresses — and this is where the choice to draw them alike is made.
 */
function phraseEl(phrase: Phrase, className: string): HTMLElement {
  const line = el("div", className);

  for (const run of phrase) {
    if (typeof run === "string") line.append(document.createTextNode(run));
    else if ("permission" in run) line.append(el("b", undefined, run.permission));
    else line.append(el("b", undefined, run.emphasis));
  }

  return line;
}

/**
 * The state, in two or three words — `Not permitted`.
 *
 * Above the sentence rather than inside it: a person scanning a screen they did
 * not expect reads the state first and the explanation second.
 */
export function labelEl(drawn: FailureDrawing): HTMLElement {
  // Untinted, deliberately — page 64b's `.st-label` is faint in all three
  // states. It carried the cause's colour class, so a refusal's label was amber
  // beside a mark that was also coloured: one fact, two signals.
  return el("div", "fail-label", drawn.label);
}

/**
 * Draw the failure, in place of the body.
 *
 * @param failure what went wrong, as the platform reported it
 * @param subject what this screen was trying to read
 * @param property the locale and zone the facts are read in
 * @param retry re-run the read; ignored where a retry cannot succeed
 * @returns the body a person sees instead of rows
 */
export function failureBody(
  failure: ReadFailure,
  subject: Subject,
  property: PropertyEnvironment,
  retry?: () => void,
): HTMLElement {
  const drawn = drawing(failure, subject);
  const body = el("div", "fail");

  // Page 64b's order: what happened, what it means, what to do about it — and
  // only then the facts. This drew the facts before the action, so a person
  // read the provenance before they learned what they could do.
  body.append(
    markEl(drawn),
    labelEl(drawn),
    el("div", "fail-said", drawn.said),
    phraseEl(drawn.whyPhrase, "fail-why"),
    actions(drawn, retry),
    factsEl(drawn, property),
  );

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
 * **Switched on `kind`.** This said *"never read for a `label`"*, because
 * `grant` deliberately carried none — the owner's 2026-09-17 ruling expressed
 * as the field's absence rather than as a rule somebody has to remember.
 * **That absence ended on 2026-09-22** (page `64h` frame 3): every refusal now
 * carries `Copy these details`, because taking the capability out of the
 * sentence left a refused card with no path to the identifier at all.
 *
 * The paragraph is corrected rather than deleted, because the reasoning it
 * records is still live for the part that did not change: **a retry is offered
 * only where trying again can work**, and a control that cannot change the
 * outcome is a second lie. What the copy button does is hand the line to
 * somebody who can act — it grants nothing, which is why it does not reopen
 * what 2026-09-17 settled.
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

    case "grant": {
      // **It carries the copy action — owner, 2026-09-22, page `64h` frame 3.**
      // This arm drew nothing, on purpose: the approved frame had offered
      // *Request access* and *Who can grant this*, a bundle calls only its own
      // backend (design page 63 §3), and the platform does not know who holds
      // the grant — so there was no label to draw.
      //
      // What changed is not that argument, which still holds: the button grants
      // nothing. It is that the sentence stopped naming the capability, and
      // with the facts already plain and `wire` drawn nowhere, a refused card
      // had **no path to the identifier at all**. Frame 2 is that state, drawn,
      // and it is what the owner rejected.
      const hand = el("button", "btn", act.label);
      hand.addEventListener("click", () => {
        void navigator.clipboard?.writeText(drawn.wire);
      });
      row.append(hand);
      break;
    }
  }

  // The phrase, not the note: the same words, with the permission kept as a
  // name so it can be set apart. 64b draws "needs **roster.read**".
  row.append(phraseEl(act.phrase, "fail-note"));
  return row;
}

/**
 * Replace a screen's contents with the failure, keeping the frame.
 *
 * @param main the screen's mount
 * @param title what this screen is, drawn where its header would be
 * @param failure what went wrong
 * @param subject what could not be read, for the sentence
 * @param property the locale and zone the facts are read in
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
  property: PropertyEnvironment,
  retry?: () => void,
): void {
  const head = el("div", "title");
  head.append(el("div", "ht", title));

  // Centred in what is left of the window — page 64b's `.body`. It sat
  // top-left, which is the position of a list that failed to load rather than
  // of a screen in the state it is actually in.
  const centre = el("div", "fail-body");
  centre.append(failureBody(failure, subject, property, retry));

  main.replaceChildren(head, centre);
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

  // Centred like the three platform causes, so the fourth state does not read
  // as a different product.
  const centre = el("div", "fail-body");
  centre.append(body);
  main.replaceChildren(head, centre);
}

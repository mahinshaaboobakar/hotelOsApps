import { failureDrawing, type FailureDrawing, type Glyph, type ReadFailure } from "@hotelos/sdk";

import { control, el, fill } from "./element";

/**
 * What a screen draws when a read did not arrive — the SDK's words, this
 * module's geometry.
 *
 * **No screen renders recorded rows. Ever** (owner, 2026-09-09: *"showing a
 * hardcoded list is wrong"*). This replaces `standIn()`, which drew the
 * approved example under a banner saying so: honest about the page, and still
 * a list of jobs that do not exist, in a hotel where somebody may act on it.
 *
 * The sentence, the mark, the four facts and what may be offered are
 * {@link failureDrawing}'s, so three applications say one thing; the elements
 * are this chrome's, because a module's surface is its own. The split is
 * deliberate — a shared component would cross the realm, and shared *words* do
 * not.
 *
 * # Three things the redraw of 2026-09-17 moved, and why each is here
 *
 * **The mark is geometry now, not a character.** The seam's old preset said *"a
 * mark, in characters — the realm has no network, so no sprite can load"*, and
 * that constraint is about the network: an inline `<svg>` fetches nothing and
 * needs no font. So this builds the `<svg>` from `drawing.glyph` rather than
 * setting text, and strokes it in `currentColor`, which carries the state's
 * colour to it without the geometry having to know what the state is.
 *
 * **The four facts are rows, not a dotted line.** They were one run-on string;
 * the owner read it and said the surface was not good. Every fact in it was
 * right and the most useful part was set as the least readable. The dotted form
 * survives as `drawing.wire` — for the clipboard, which is the only place a log
 * line belongs.
 *
 * **A refusal offers no button, and that is a ruling rather than an omission.**
 * `act.kind === "grant"` carries no label, because naming who can grant would
 * tell whoever is standing at the terminal who holds authority in this
 * property — a fact about the organisation, on a screen that was only asked why
 * a board would not load. The note still names the capability, so an operator
 * knows what to ask for; the screen does not decide whom they ask.
 */
/**
 * The failure surface's own rules, in the file that draws it.
 *
 * **Not in `chrome/styles.ts`, and that is the whole repair.** They were there,
 * and `chrome/styles.ts` builds the *module* realm's sheet — while the six
 * widgets are their own realm with their own sheet (`widgets/card.ts`), and
 * every one of them calls {@link failure} on a read that does not arrive. So
 * every widget failure this application has ever shown a property was unstyled
 * text, in the realm where **every** read is refused today.
 *
 * FF found the same thing in GuestOps on 2026-09-17, one layer out: classes
 * emitted with no rule anywhere. Here the rules existed and the realm that
 * needed them could not see them, which reads identically from outside and has
 * the same cause — a stylesheet and the markup it dresses living in files that
 * never refer to each other. A surface that carries its own rules cannot be
 * assembled into a realm that lacks them, so both sheets import this and
 * `tests/styled.test.ts` fails the build if either stops.
 */
export const FAILURE_CSS = `
.gap{display:flex;flex-direction:column;gap:6px;padding:26px 22px;max-width:62ch}
.gap-head{display:flex;align-items:center;gap:8px;color:var(--color-ink-faint,#5a6172)}
.gap-unanswered .gap-head{color:var(--color-warn,#fbbf24)}
.gap-forbidden .gap-head{color:var(--color-ink-muted,#8b93a7)}
.gap-faulted .gap-head{color:var(--color-bad,#f87171)}
.gap-mark{width:18px;height:18px;flex:none}
.gap-label{font-size:11px;font-weight:600;letter-spacing:.12em;text-transform:uppercase}
.gap-said{font-size:14px;font-weight:600;color:var(--color-ink,#e8ebf4)}
.gap-why{font-size:13px;color:var(--color-ink-muted,#8b93a7);line-height:1.5}
.gap-facts{display:grid;grid-template-columns:auto 1fr;gap:4px 14px;margin:10px 0 2px}
.gap-fact-label{font-size:11px;letter-spacing:.06em;text-transform:uppercase;color:var(--color-ink-faint,#5a6172)}
.gap-fact-value{margin:0;font-size:12px;color:var(--color-ink-muted,#8b93a7);
                font-family:ui-monospace,Menlo,monospace;overflow-wrap:anywhere}
.gap-note{font-size:12px;color:var(--color-ink-faint,#5a6172);line-height:1.5;margin-top:4px}
/* A widget is narrow, so the surface loses its page padding there and keeps
   everything else. One rule rather than a second copy of the surface. */
.wcard .gap{padding:4px 0 2px;max-width:none}
`;

export function failure(
  said: ReadFailure,
  the: string,
  again?: () => void,
): HTMLElement {
  const drawing = failureDrawing(said, { app: "Jobs", the });
  const panel = el("div", `gap gap-${drawing.cause}`);

  panel.append(
    fill(el("div", "gap-head"), mark(drawing.glyph), el("div", "gap-label", drawing.label)),
    el("div", "gap-said", drawing.said),
    el("div", "gap-why", drawing.why),
    facts(drawing),
  );

  const offered = offer(drawing, again);
  if (offered !== null) panel.append(offered);

  panel.append(el("div", "gap-note", drawing.act.note));
  return panel;
}

/**
 * The mark, stroked from the seam's paths.
 *
 * `createElementNS` rather than `el`, because an `<svg>` built with
 * `createElement` is an unknown HTML element that renders nothing — which is
 * this round's own failure one layer down.
 */
function mark(glyph: Glyph): SVGSVGElement {
  const SVG = "http://www.w3.org/2000/svg";
  const svg = document.createElementNS(SVG, "svg");

  svg.setAttribute("class", "gap-mark");
  svg.setAttribute("viewBox", glyph.viewBox);
  svg.setAttribute("fill", "none");
  svg.setAttribute("stroke", "currentColor");
  svg.setAttribute("stroke-width", "1.6");
  svg.setAttribute("stroke-linecap", "round");
  svg.setAttribute("stroke-linejoin", "round");

  // Decorative: the state is already in `gap-label` beside it, and a screen
  // reader announcing "lock" before "Not permitted" says one thing twice.
  svg.setAttribute("aria-hidden", "true");

  for (const path of glyph.paths) {
    const element = document.createElementNS(SVG, "path");
    element.setAttribute("d", path);
    svg.append(element);
  }

  return svg;
}

/** The four facts, each labelled, in the order the seam puts them. */
function facts(drawing: FailureDrawing): HTMLElement {
  const list = el("dl", "gap-facts");

  for (const fact of drawing.facts) {
    list.append(el("dt", "gap-fact-label", fact.label), el("dd", "gap-fact-value", fact.value));
  }

  return list;
}

/**
 * What the surface offers, which is the affordance and never the wording.
 *
 * The union is exhausted deliberately: a `grant` arm that fell through to a
 * shared button would compile, and would promise a person something the
 * platform cannot do.
 */
function offer(drawing: FailureDrawing, again?: () => void): HTMLElement | null {
  switch (drawing.act.kind) {
    case "retry":
      // The seam decides whether trying again could work; this only honours it,
      // and a screen that passed no retry gets no button either.
      if (!drawing.retryable || again === undefined) return null;
      return fill(el("div", "row"), control("btn", drawing.act.label, again));

    case "grant":
      // No button. There is no route to a person that does not disclose one.
      return null;

    case "copy": {
      const wire = drawing.wire;
      return fill(
        el("div", "row"),
        control("btn", drawing.act.label, () => void navigator.clipboard?.writeText(wire)),
      );
    }
  }
}

import { failureDrawing, type Cause, type FailureDrawing, type Glyph, type ReadFailure } from "@hotelos/sdk";

import { control, el, fill } from "./element";

/**
 * What a screen draws when a read did not arrive — page 64b, Treatment A.
 *
 * **The frame is the authority, and this file is measured against it**:
 * `docs/working/64b-when-a-screen-cannot-read.html` in HosPilotOS, approved by
 * the owner on 2026-09-17. The class names below are this module's; every
 * size, weight, colour and order is the frame's, and each rule names the frame
 * rule it answers to so the next reader can hold one beside the other.
 *
 * **No screen renders recorded rows. Ever** (owner, 2026-09-09). The words, the
 * mark and the three facts are {@link failureDrawing}'s, so three applications
 * say one thing; the elements are this chrome's, because a shared component
 * would cross the realm.
 *
 * # What 0.4.0 drew, and the owner saw on 2026-09-18
 *
 * *NOT PERMITTED*, in the top-left corner of the Board. 0.4.0 was built from
 * the SDK's *types* — a glyph, a label, some facts, an act — and styled from
 * this module's habits, so every part was present and the composition was
 * nobody's: no centring, the glyph inline with the label, a 14px headline, the
 * note after the facts, and no rule above them. **A surface that has every part
 * of an approved drawing is not the approved drawing**, and nothing that reads
 * a type can tell the two apart. The page-64 audit that caught it is recorded
 * in `docs/chapters/05`.
 *
 * # One size this frame does not draw
 *
 * 64b draws a failure at **screen** size and at **widget** size. The Board also
 * reads a second thing — the figures strip — and draws the board even when only
 * the strip failed. That is a third placement, inside a screen that did render,
 * and the frame has nothing for it. {@link failureState} gives it the frame's
 * state block without the screen's centring, which is the least that is not
 * invented; it is reported as undrawn rather than treated as settled.
 */

/**
 * Screen size — the frame's `.body` holding its `.state`.
 *
 * `.body { display:grid; place-items:center; min-height:330px }` and
 * `.state { width:min(560px, 92%); padding:34px 0; text-align:left }`: the block
 * sits in the middle of the content area and reads left-aligned inside it.
 */
export function failure(said: ReadFailure, the: string, again?: () => void): HTMLElement {
  return fill(el("div", "gap"), failureState(said, the, again));
}

/**
 * The state block alone, in the frame's order — mark, label, sentence, why,
 * **what to do, then** the facts.
 *
 * The order is the point of the redraw rather than a detail of it: FF's note on
 * the frame is that *"the action becomes the loudest thing after the
 * sentence"*, and 0.4.0 put the note below the facts, where it read as a
 * footnote to the log line.
 */
export function failureState(said: ReadFailure, the: string, again?: () => void): HTMLElement {
  const drawing = failureDrawing(said, { app: "Jobs", the });

  return fill(
    el("div", `gap-state gap-${drawing.cause}`),
    fill(el("div", "gap-mark"), glyph(drawing.glyph)),
    el("div", "gap-label", drawing.label),
    el("div", "gap-said", drawing.said),
    el("div", "gap-why", drawing.why),
    todo(drawing, said.capability, again),
    facts(drawing, said.capability),
  );
}

/**
 * The mark, stroked from the seam's paths — shared by both sizes.
 *
 * `createElementNS` rather than `el`: an `<svg>` made with `createElement` is an
 * unknown HTML element and renders nothing. Colour arrives through
 * `currentColor`, so the geometry never knows which state it is drawing.
 */
export function glyph(shape: Glyph): SVGSVGElement {
  const SVG = "http://www.w3.org/2000/svg";
  const svg = document.createElementNS(SVG, "svg");

  for (const [name, value] of [
    ["viewBox", shape.viewBox], ["fill", "none"], ["stroke", "currentColor"],
    ["stroke-width", "1.6"], ["stroke-linecap", "round"], ["stroke-linejoin", "round"],
    // Decorative: the state is in words beside it at both sizes.
    ["aria-hidden", "true"],
  ] as const) {
    svg.setAttribute(name, value);
  }

  for (const path of shape.paths) {
    const element = document.createElementNS(SVG, "path");
    element.setAttribute("d", path);
    svg.append(element);
  }

  return svg;
}

/**
 * A sentence with the capability set in bold, as the frame sets it.
 *
 * The frame draws `This screen needs <b>roster.read</b>…` and `Asked for
 * <b>roster.read</b> · me`: the capability is the one word a person can take to
 * somebody who can grant it. The seam hands over plain strings, so the emphasis
 * is found by the capability's own value rather than by position — a sentence
 * that does not contain it is returned untouched rather than guessed at.
 */
export function emphasised(sentence: string, capability: string): Node[] {
  const at = sentence.indexOf(capability);
  if (at < 0) return [document.createTextNode(sentence)];

  return [
    document.createTextNode(sentence.slice(0, at)),
    el("b", undefined, capability),
    document.createTextNode(sentence.slice(at + capability.length)),
  ];
}

/**
 * The frame's `.st-do` — the control first when there is one, then the note.
 *
 * The `Act` union is exhausted rather than read through. `grant` has no label
 * and so no control: a refusal names the grant and stops (owner, 2026-09-17),
 * because naming who can grant would tell whoever is at the terminal who holds
 * authority in this property.
 */
function todo(drawing: FailureDrawing, capability: string, again?: () => void): HTMLElement {
  const row = el("div", "gap-do");
  const act = drawing.act;

  switch (act.kind) {
    case "retry":
      // `.btn.pri` in the frame — the one state where trying again can work.
      if (drawing.retryable && again !== undefined) row.append(control("btn pri", act.label, again));
      break;

    case "grant":
      break;

    case "copy": {
      const wire = drawing.wire;
      row.append(control("btn", act.label, () => void navigator.clipboard?.writeText(wire)));
      break;
    }
  }

  row.append(fill(el("span", "gap-ask"), ...emphasised(act.note, capability)));
  return row;
}

/** The frame's `.prov` — the facts as a labelled grid, under a rule. */
function facts(drawing: FailureDrawing, capability: string): HTMLElement {
  const list = el("dl", "gap-facts");

  for (const fact of drawing.facts) {
    list.append(el("dt", undefined, fact.label), fill(el("dd"), ...emphasised(fact.value, capability)));
  }

  return list;
}

/**
 * The colour each state's mark takes — `.st-mark.wait / .no / .fault`.
 *
 * **The one list of causes in this module.** The screen's per-cause rules and
 * the stylesheet guard's expectations are both derived from its keys, so a
 * cause the SDK adds is one entry here and cannot be half-added — a colour with
 * no rule, or a rule the guard does not know to expect. `Record<Cause, …>`
 * makes a missing entry a compile error the moment the SDK's union grows, which
 * is the point: a new state must be given its drawn colour, not inherit one.
 */
export const TONE: Readonly<Record<Cause, string>> = {
  unanswered: "var(--color-warn,#fbbf24)",
  forbidden: "var(--color-ink-muted,#8b93a7)",
  faulted: "var(--color-bad,#f87171)",
  // ADR 0192's three, contract v2 (`d45f028d`). Colours read from page 64e,
  // approved 2026-09-19, by the class each state's mark carries there — not
  // chosen here. The names are the SDK's causes, not the wire kinds 64e labels
  // them by: `causeOf` maps local_forbidden → unadmitted, user_forbidden →
  // ungranted, model_unavailable → undecidable.
  unadmitted: "var(--color-ink-muted,#8b93a7)", //  64e c-no — a refusal, like forbidden
  ungranted: "var(--color-ink-muted,#8b93a7)", //   64e c-no — a refusal, like forbidden
  undecidable: "var(--color-bad,#f87171)", //       64e c-fault — the model could not decide
};

/**
 * The screen-size surface's rules, in the file that draws it.
 *
 * **Rules travel with their surface** (`144df2e`): a sheet cannot be assembled
 * without them, which is what stopped the widget realm drawing unstyled text.
 * The widget size has its own drawing in 64b and its own rules beside it, in
 * `widgets/failed.ts`.
 *
 * Every value is the frame's. The two monospace roles (`.st-label`, `.prov dt`)
 * name the frame's stack, because ADR 0106 publishes no mono token.
 */
export const FAILURE_CSS = `
.gap{display:grid;place-items:center;min-height:330px}
.gap-state{width:min(560px, 92%);padding:34px 0;text-align:left}
.gap-mark{margin-bottom:12px}
.gap-mark svg{width:26px;height:26px;display:block}
${Object.entries(TONE).map(([cause, tone]) => `.gap-${cause} .gap-mark{color:${tone}}`).join("\n")}
.gap-label{font-family:ui-monospace,"Cascadia Mono",Menlo,monospace;font-size:11px;letter-spacing:.1em;
           text-transform:uppercase;color:var(--color-ink-faint,#5a6172);margin-bottom:6px}
.gap-said{font-size:19px;font-weight:600;letter-spacing:-.01em;line-height:1.4;margin-bottom:8px;
          color:var(--color-ink,#e8ebf4)}
.gap-why{color:var(--color-ink-muted,#8b93a7);font-size:14px;max-width:52ch;margin-bottom:18px}
.gap-do{display:flex;align-items:center;gap:12px;margin-bottom:22px;flex-wrap:wrap}
.gap-do .btn{font-size:13px;font-weight:600;padding:7px 15px;border-radius:9px;
             border:1px solid var(--color-line-strong,rgb(255 255 255 / 0.14));
             background:color-mix(in srgb, var(--color-ink,#e8ebf4) 2%, transparent);color:var(--color-ink,#e8ebf4)}
.gap-do .btn.pri{background:color-mix(in srgb, var(--color-brand,#818cf8) 18%, transparent);
                 border-color:color-mix(in srgb, var(--color-brand,#818cf8) 50%, transparent)}
.gap-ask{color:var(--color-ink-muted,#8b93a7);font-size:13px}
.gap-ask b{color:var(--color-ink,#e8ebf4)}
.gap-facts{border-top:1px solid var(--color-line,rgb(255 255 255 / 0.07));padding-top:13px;margin:0;
           display:grid;grid-template-columns:auto 1fr;gap:3px 16px;font-size:12px}
.gap-facts dt{font-family:ui-monospace,"Cascadia Mono",Menlo,monospace;font-size:10.5px;letter-spacing:.08em;
              text-transform:uppercase;color:var(--color-ink-faint,#5a6172)}
.gap-facts dd{margin:0;color:var(--color-ink-muted,#8b93a7);font-variant-numeric:tabular-nums}
.gap-facts dd b{color:var(--color-ink,#e8ebf4);font-weight:500}
`;

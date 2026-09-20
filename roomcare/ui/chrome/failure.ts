/**
 * What a Room Care screen draws when a read did not arrive — page 64b,
 * Treatment A, owner-approved 2026-09-17 (`docs/working/64b-when-a-screen-cannot-read.html`
 * in HosPilotOS).
 *
 * **The words are the SDK's; the elements are this chrome's.** Every sentence,
 * the mark's geometry, the four facts and the rule that only an unanswered read
 * may be retried come from `failureDrawing` — so Room Care says what GuestOps,
 * Jobs and Workforce say. The SDK ships no CSS, so the markup and the rules are
 * here, each rule naming the frame rule it answers to.
 *
 * # Where 64b and the written standard disagree — built as the other three shipped
 *
 * Page 64d puts eight such items before the owner. Until the owner rules, this
 * builds what GuestOps, Jobs and Workforce ship, and says so:
 *
 * - **The retry button is page 64 §2's primary** (the brand gradient), as
 *   Workforce and GuestOps build it; Jobs scoped 64b's quiet tint instead.
 * - **The "needs…" line is 13px**, 64b's size, as all three build it; page 64a
 *   rules note text at 12px, and which role this line is has not been ruled.
 * - **The fact labels use a monospace stack**, as 64b draws them; ADR 0106
 *   publishes no mono token.
 * - **The moment is the property's**, formatted by the SDK in its locale and
 *   zone (`JOBS-Q1(8)`), as Workforce builds it — not 64b's UTC with seconds.
 */

import { failureDrawing, type Fact, type FailureDrawing, type HostApi, type Phrase, type ReadFailure } from "@hotelos/sdk";

import { control, el, fill } from "./element";
import { when } from "./instant";

/** Who is speaking, in every failure Room Care draws — the application's name as a person reads it. */
export const APP = "Room Care";

/** The words for a failure, with Room Care speaking. */
export function drawing(failure: ReadFailure, the: string): FailureDrawing {
  return failureDrawing(failure, { app: APP, the });
}

/**
 * The failure, centred in what is left of the window — 64b's `.body` holding
 * its `.state`, in the frame's order: mark, label, sentence, why, what to do,
 * and only then the facts.
 *
 * @param failure what the read reported
 * @param the what the screen was reading — "the board", "this room"
 * @param retry re-run the read; drawn only where the SDK says a retry can work
 */
export function failed(host: HostApi, failure: ReadFailure, the: string, retry?: () => void): HTMLElement {
  const drawn = drawing(failure, the);
  const state = fill(el("div", "fail"),
    mark(drawn, "fail-mark"),
    el("div", "fail-label", drawn.label),
    el("div", "fail-said", drawn.said),
    phrase(drawn.whyPhrase, "fail-why"),
    todo(drawn, retry),
    facts(host, drawn));
  return fill(el("div", "fail-body"), state);
}

/**
 * The mark, stroked from the SDK's paths — shared by both sizes so a screen and a
 * widget cannot draw two sets of glyphs. `createElementNS`, because an `<svg>`
 * made by `createElement` is an unknown HTML element and draws nothing.
 */
export function mark(drawn: FailureDrawing, className: string): HTMLElement {
  const box = el("div", `${className} fail-${drawn.cause}`);
  const svg = document.createElementNS(SVG, "svg");
  for (const [name, value] of [["viewBox", drawn.glyph.viewBox], ["fill", "none"], ["stroke", "currentColor"],
    ["stroke-width", "1.6"], ["stroke-linecap", "round"], ["stroke-linejoin", "round"]] as const) svg.setAttribute(name, value);
  for (const d of drawn.glyph.paths) {
    const path = document.createElementNS(SVG, "path");
    path.setAttribute("d", d);
    svg.append(path);
  }
  box.append(svg);
  // Decorative: the state is in words beside it at both sizes.
  box.setAttribute("aria-hidden", "true");
  return box;
}

const SVG = "http://www.w3.org/2000/svg";

/** A phrase with its permission and its emphasis set apart — 64b draws both bold. */
function phrase(runs: Phrase, className: string): HTMLElement {
  const line = el("div", className);
  for (const run of runs) {
    if (typeof run === "string") line.append(document.createTextNode(run));
    else line.append(el("b", undefined, "permission" in run ? run.permission : run.emphasis));
  }
  return line;
}

/**
 * 64b's `.st-do` — the control first where there is one, then the note. The act
 * is exhausted by kind: `grant` carries no label and so no control (owner,
 * 2026-09-17 — a refusal names the grant and stops).
 */
function todo(drawn: FailureDrawing, retry?: () => void): HTMLElement {
  const row = el("div", "fail-acts");
  const act = drawn.act;
  switch (act.kind) {
    case "retry":
      if (retry !== undefined) row.append(control("btn pri", act.label, retry));
      break;
    case "copy":
      row.append(control("btn", act.label, () => void navigator.clipboard?.writeText(drawn.wire)));
      break;
    case "grant":
      break;
  }
  row.append(phrase(act.phrase, "fail-note"));
  return row;
}

/** 64b's `.prov` — the four facts, labelled, under a rule. Values are composed here from their typed parts. */
function facts(host: HostApi, drawn: FailureDrawing): HTMLElement {
  const list = el("dl", "fail-facts");
  for (const fact of drawn.facts) {
    list.append(el("dt", "fail-fk", fact.label), fill(el("dd", "fail-fv"), ...value(host, fact)));
  }
  return list;
}

function value(host: HostApi, fact: Fact): Node[] {
  switch (fact.kind) {
    // What could not be read, in the screen's own words — owner, 2026-09-20, 64g §2 B. This drew `permission` and
    // `method`, the code name, which the SDK now documents as not for drawing; they stay on the fact for the
    // clipboard and diagnostics, and `wire` still carries them to support (tests/failure-words.test.ts).
    case "asked": return [el("b", undefined, fact.value)];
    case "at": return [document.createTextNode(when(host, fact.at))];
    case "answer": return [document.createTextNode(fact.value)];
  }
}

/** The screen-size rules, in the file that draws the surface — each value is 64b's. */
export const FAILURE_CSS = `
.fail-body{flex:1 0 auto;min-height:330px;display:grid;place-items:center}
.fail{width:min(560px,92%);padding:34px 0;text-align:left}
.fail-mark{margin-bottom:12px;color:var(--color-ink-muted,#8b93a7)}
.fail-mark svg{width:26px;height:26px;display:block}
.fail-mark.fail-unanswered{color:var(--color-warn,#fbbf24)}
.fail-mark.fail-forbidden{color:var(--color-ink-muted,#8b93a7)}
.fail-mark.fail-unadmitted{color:var(--color-ink-muted,#8b93a7)}
.fail-mark.fail-ungranted{color:var(--color-ink-muted,#8b93a7)}
.fail-mark.fail-undecidable{color:var(--color-bad,#f87171)}
.fail-mark.fail-faulted{color:var(--color-bad,#f87171)}
.fail-label{font-family:ui-monospace,"Cascadia Mono",Menlo,monospace;font-size:11px;letter-spacing:.1em;
            text-transform:uppercase;color:var(--color-ink-faint,#5a6172);margin-bottom:6px}
.fail-said{font-size:19px;font-weight:600;letter-spacing:-.01em;line-height:1.4;color:var(--color-ink,#e8ebf4);margin-bottom:8px}
.fail-why{font-size:14px;color:var(--color-ink-muted,#8b93a7);max-width:52ch;margin-bottom:18px}
.fail-why b{color:var(--color-ink,#e8ebf4)}
.fail-acts{display:flex;align-items:center;gap:12px;flex-wrap:wrap;margin-bottom:22px}
.fail-note{font-size:13px;color:var(--color-ink-muted,#8b93a7)}
.fail-note b{color:var(--color-ink,#e8ebf4)}
.fail-facts{margin:0;border-top:1px solid var(--color-line,rgb(255 255 255 / 0.07));padding-top:13px;
            display:grid;grid-template-columns:auto 1fr;gap:3px 16px;font-size:12px}
.fail-fk{font-family:ui-monospace,"Cascadia Mono",Menlo,monospace;font-size:10.5px;letter-spacing:.08em;
         text-transform:uppercase;color:var(--color-ink-faint,#5a6172)}
.fail-fv{margin:0;color:var(--color-ink-muted,#8b93a7);font-variant-numeric:tabular-nums;overflow-wrap:anywhere}
.fail-fv b{color:var(--color-ink,#e8ebf4);font-weight:500}
`;

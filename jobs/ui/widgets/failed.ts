/**
 * What a widget draws when its read did not arrive — page 64b, widget size.
 *
 * **64b draws this separately, and on purpose.** At 320 wide *"a long sentence
 * has nowhere to go"*: the same three states and marks, a smaller mark, no
 * state label, no facts, and one line that goes somewhere — *"the four facts
 * move to the screen the card opens"*, which the owner approved as the one
 * place they are not all on screen. 0.4.0 drew the whole screen surface inside
 * a card instead, so a glance at a dashboard got a log grid at 12px.
 *
 * # The words are the seam's, at this size too — since 0.4.2
 *
 * Until 0.4.1 this card drew the SCREEN's sentences, because the seam had one
 * set of words, and composed *"Open Jobs →"* / *"Try again →"* itself from the
 * application's name and `FAILURE_LABELS.retry` — both reported from the 0.4.1
 * audit rather than written a second way here. GG closed both in the SDK
 * (`885677ec`, `71458056`): `briefSaid` and `brief` are the frame's shorter
 * sentences, and `onward` is the link line, decided beside the rule that
 * decides the screen's button — only an unanswered read can be tried again. So
 * three applications' cards now say one thing, and this file writes no words.
 *
 * What stays this file's is the wiring: `retry` redraws the card, `open` asks
 * the shell for the screen that carries the facts — which screen is Jobs' to
 * say, since the SDK does not know Jobs' screens.
 */

import { failureDrawing, type HostApi, type Onward, type ReadFailure } from "@hotelos/sdk";

import { el, fill } from "../chrome/element";
import { TONE, glyph } from "../chrome/failure";
import { card, open } from "./card";

/** Where a failed card sends a person — the screen that carries the facts. */
const THE_SCREEN = "jobs:board";

/**
 * A widget's card, failed — the frame's `.card .in`.
 *
 * @param again redraws the whole card, and is offered only where the seam says
 *   trying again could work. It replaces the card in place rather than asking
 *   the host for a refresh, which would redraw every widget on the dashboard.
 */
export function failedCard(
  host: HostApi,
  title: string,
  scope: string,
  failure: ReadFailure,
  the: string,
  again: () => Promise<HTMLElement>,
): HTMLElement {
  const drawing = failureDrawing(failure, { app: "Jobs", the });

  const mark = fill(el("div", "wfail-mark"), glyph(drawing.glyph));
  mark.style.color = TONE[drawing.cause];

  return card(title, scope, [
    fill(
      el("div", "wfail"),
      mark,
      // `briefSaid`, not `said`: a fault's headline drops its subject at card
      // size — "Jobs could not build this" — as 64b's widget frame draws it.
      el("div", "wfail-said", drawing.briefSaid),
      // `brief`, not `why`: the frame's sentence written to fit a card, not the
      // screen's wrapped across four lines.
      el("div", "wfail-why", drawing.brief),
      onward(host, drawing.onward, again),
    ),
  ]);
}

/**
 * The seam's `onward`, wired — the frame's `.w-open`.
 *
 * Exhausted rather than read through: an `Onward` kind added to the SDK stops
 * this compiling, where a default branch would quietly draw a link that does
 * the wrong thing.
 */
function onward(
  host: HostApi,
  line: Onward,
  again: () => Promise<HTMLElement>,
): HTMLElement {
  switch (line.kind) {
    case "retry":
      return link(line.label, (self) => void again().then((fresh) => self.closest(".wcard")?.replaceWith(fresh)));
    case "open":
      return link(line.label, (self) => void open(host, self, THE_SCREEN));
  }
}

/** The frame's `.w-open` — a line that goes somewhere, set as a link. */
function link(text: string, go: (self: HTMLElement) => void): HTMLElement {
  const button = el("button", "wfail-open", text);
  button.setAttribute("type", "button");
  button.addEventListener("click", () => go(button));
  return button;
}

/**
 * The widget size's rules, beside the surface they dress (`144df2e`).
 *
 * Every value is the frame's: `.card .in`, `.ico.sm`, `.w-said`, `.w-why`,
 * `.w-open`. The mark's colour is set from {@link TONE}, the same map the
 * screen size uses, so the two sizes cannot drift apart on a state's colour.
 */
export const FAILED_CSS = `
.wfail{display:flex;flex-direction:column;justify-content:center;gap:7px}
.wfail-mark svg{width:20px;height:20px;display:block}
.wfail-said{font-size:13px;font-weight:600;line-height:1.4;color:var(--color-ink,#e8ebf4)}
.wfail-why{font-size:11.5px;color:var(--color-ink-muted,#8b93a7);line-height:1.5}
.wfail-open{font:inherit;font-size:11.5px;color:var(--color-brand,#818cf8);margin-top:2px;
            background:none;border:0;padding:0;text-align:left;cursor:pointer}
`;

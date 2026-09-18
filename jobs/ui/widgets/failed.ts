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
 * # Two things the frame draws that the seam does not carry
 *
 * The frame's widget sentences are **shorter** than the screen's — *"The
 * service refused. Nothing is broken; a permission is missing."* against the
 * screen's two clauses — and `failureDrawing` has one set of words, not two.
 * This draws the seam's words rather than writing a second set here, because a
 * second set is how three applications come to say three things; the gap is
 * reported for the SDK's owner to close once for all of them.
 *
 * The *"Open Jobs →"* and *"Try again →"* lines are composed here from the
 * application's name and `FAILURE_LABELS.retry`, for the same reason and with
 * the same report: the frame draws them and the seam has no field for them.
 */

import { FAILURE_LABELS, failureDrawing, type HostApi, type ReadFailure } from "@hotelos/sdk";

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

  const onward = drawing.retryable
    ? link(`${FAILURE_LABELS.retry} →`, (self) => void again().then((fresh) => self.closest(".wcard")?.replaceWith(fresh)))
    : link("Open Jobs →", (self) => void open(host, self, THE_SCREEN));

  return card(title, scope, [
    fill(
      el("div", "wfail"),
      mark,
      el("div", "wfail-said", drawing.said),
      el("div", "wfail-why", drawing.why),
      onward,
    ),
  ]);
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

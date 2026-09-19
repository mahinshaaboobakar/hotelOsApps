/**
 * A widget's card — its heading, its figures and its rows, on the published
 * tokens only (page 56; page 64 §1), and the card it becomes when its read did
 * not arrive (page 64b's widget frame). A row opens the room it names through the
 * shell; a refusal to open is said on the card, never swallowed.
 */

import type { FailureDrawing, HostApi, ReadFailure } from "@hotelos/sdk";

import { el, fill } from "../chrome/element";
import { drawing, mark } from "../chrome/failure";
import { saying } from "../chrome/load";

/**
 * The shell's own opener. The widget names a screen, never an application — the
 * shell knows which package the widget came from, as it does for Workforce's.
 */
const SHELL_OPEN = "shell.open";

/**
 * Every rule a Room Care widget renders with — this is the only sheet a widget
 * bundle mounts, so a class a widget emits is styled here or nowhere
 * (`tests/widget-sheet.test.ts` holds that, per widget and per cause).
 *
 * The card fills the widget's frame in every state, so a card that could not
 * read is the same rectangle as one that did — as Workforce builds it (page 64d,
 * item 3, is before the owner). The failure rules are 64b's `.card .in`,
 * `.w-said`, `.w-why` and `.w-open`.
 */
const WIDGET_CSS = `
*{box-sizing:border-box}
.wcard{font:14px/1.5 var(--font-sans,system-ui, -apple-system, "Segoe UI", sans-serif);color:var(--color-ink,#e8ebf4);
       background:var(--color-surface,#0b0d14);padding:16px;font-variant-numeric:tabular-nums;
       min-height:100vh;display:flex;flex-direction:column}
.whead{display:flex;justify-content:space-between;font-size:13px;font-weight:700;margin:0 0 12px}
.whead span{color:var(--color-ink-faint,#5a6172);font-weight:400;font-size:13px}
.wfig{display:flex;gap:26px;margin-bottom:10px}
.wfig b{display:block;font-size:30px;font-weight:600;line-height:1.1}
.wfig .lbl{font-family:ui-monospace,Menlo,monospace;color:var(--color-ink-muted,#8b93a7);font-size:11px}
.wfig .ok b{color:var(--color-ok,#34d399)}
.wfig .warn b{color:var(--color-warn,#fbbf24)}
.wfig .bad b{color:var(--color-bad,#f87171)}
.wfig .run b{color:var(--color-brand,#818cf8)}
.wrow{display:flex;justify-content:space-between;gap:10px;padding:8px 0;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07));
      font-size:13px;background:none;border-left:0;border-right:0;border-top:0;width:100%;text-align:left;color:inherit;font-family:inherit;line-height:inherit}
button.wrow{cursor:pointer}
.wrow:last-child{border-bottom:0}
.wrow .num{font-family:ui-monospace,Menlo,monospace;font-size:12px;color:var(--color-ink-muted,#8b93a7)}
.wrow .bad{color:var(--color-bad,#f87171)}
.wrow .warn{color:var(--color-warn,#fbbf24)}
.wrow .run{color:var(--color-brand,#818cf8)}
.wfoot span{font-family:ui-monospace,Menlo,monospace;font-size:11px;color:var(--color-ink-muted,#8b93a7)}
.wquiet{color:var(--color-ok,#34d399);font-size:12px}
.wnone{color:var(--color-ink-faint,#5a6172);font-size:12px;padding-top:8px}
.wrefusal{color:var(--color-bad,#f87171);font-size:12px;padding-top:8px}
.wfail{flex:1;display:flex;flex-direction:column;justify-content:center;gap:7px}
.wf-mark{color:var(--color-ink-muted,#8b93a7)}
.wf-mark svg{width:20px;height:20px;display:block}
.wf-mark.fail-unanswered{color:var(--color-warn,#fbbf24)}
.wf-mark.fail-forbidden{color:var(--color-ink-muted,#8b93a7)}
.wf-mark.fail-unadmitted{color:var(--color-ink-muted,#8b93a7)}
.wf-mark.fail-ungranted{color:var(--color-ink-muted,#8b93a7)}
.wf-mark.fail-undecidable{color:var(--color-bad,#f87171)}
.wf-mark.fail-faulted{color:var(--color-bad,#f87171)}
.wf-said{font-size:13px;font-weight:600;line-height:1.4}
.wf-why{font-size:11.5px;line-height:1.5;color:var(--color-ink-muted,#8b93a7)}
.wf-open{align-self:flex-start;margin-top:2px;padding:0;border:0;background:none;font:inherit;font-size:11.5px;
         color:var(--color-brand,#818cf8);cursor:pointer}
.wf-open:focus-visible{outline:2px solid var(--color-brand,#818cf8);outline-offset:2px;border-radius:3px}
`;

export function stylesheet(): HTMLStyleElement {
  const style = document.createElement("style");
  style.textContent = WIDGET_CSS;
  return style;
}

/** The card: the question, what it is about, and the answer. */
export function card(title: string, scope: string, body: readonly (Node | null)[]): HTMLElement {
  const root = el("section", "wcard");
  const head = el("h3", "whead");
  head.append(document.createTextNode(title), el("span", undefined, scope));
  root.append(head);
  return fill(root, ...body);
}

export function figures(values: readonly { value: string; label: string; tone: string }[]): HTMLElement {
  const row = el("div", "wfig");
  for (const f of values) {
    const cell = el("div", f.tone);
    cell.append(el("b", undefined, f.value), el("div", "lbl", f.label));
    row.append(cell);
  }
  return row;
}

/** A row that opens a room in Room Care; a refusal is said, never swallowed. */
export function openRow(host: HostApi, left: string, right: string, tone: string, roomId: string): HTMLElement {
  const row = el("button", "wrow");
  row.setAttribute("type", "button");
  row.append(el("span", "num", left), el("span", tone, right));
  row.addEventListener("click", () => void open(host, row, `room?id=${roomId}`));
  return row;
}

/** What a failure card can reach, from the panel that drew it. */
export interface Reach {
  host: HostApi;
  /** The screen that holds this card's subject — where the four facts are drawn. */
  opens: string;
  /** Draw the panel again; wired only where a retry could succeed. */
  again: () => Promise<HTMLElement>;
}

/**
 * A widget that could not read, as 64b's widget frame draws it — its own
 * surface, not the screen's cut down: the heading it always has, then the mark,
 * the SDK's short headline and short sentence, and one link onward. **No facts
 * at this size** — 64b moves them to the screen the card opens, and the owner
 * approved that divergence by name. Never a figure: a widget is the frame most
 * likely to be glanced at and believed.
 */
export function unread(title: string, scope: string, subject: string, failure: ReadFailure, reach: Reach): HTMLElement {
  const drawn = drawing(failure, subject);
  const body = fill(el("div", "wfail"), mark(drawn, "wf-mark"), el("div", "wf-said", drawn.briefSaid), el("div", "wf-why", drawn.brief));
  const root = card(title, scope, [body]);
  body.append(onward(drawn, root, reach));
  return root;
}

/** The one thing a glance can do — the SDK's words and choice; this wires them. */
function onward(drawn: FailureDrawing, root: HTMLElement, reach: Reach): HTMLElement {
  const link = el("button", "wf-open", drawn.onward.label);
  link.setAttribute("type", "button");
  switch (drawn.onward.kind) {
    case "retry":
      link.addEventListener("click", () => void reach.again().then((fresh) => root.replaceWith(fresh)));
      break;
    case "open":
      link.addEventListener("click", () => void open(reach.host, link, reach.opens));
      break;
  }
  return link;
}

async function open(host: HostApi, from: HTMLElement, destination: string): Promise<void> {
  try {
    await host.call(SHELL_OPEN, "at", { destination });
  } catch (error) {
    const holder = from.closest(".wcard");
    if (holder === null) return;
    holder.querySelector(".wrefusal")?.remove();
    holder.append(el("div", "wrefusal", `Not opened — ${saying(error)}`));
  }
}

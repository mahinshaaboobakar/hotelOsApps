/**
 * A widget's card — its heading, its figures and its rows, on the published
 * tokens only (page 56; page 64 §1). A row opens the room it names through the
 * shell; a refusal is said on the card, never swallowed.
 */

import type { HostApi } from "@hotelos/sdk";

import { el, fill } from "../chrome/element";
import { sentence } from "../chrome/load";

/** The shell's own opener — the same route Jobs' widgets take; the shell decides whether it opens. */
const SHELL_OPEN = "shell.open";

const WIDGET_CSS = `
*{box-sizing:border-box}
.wcard{font:14px/1.5 var(--font-sans,system-ui, -apple-system, "Segoe UI", sans-serif);color:var(--color-ink,#e8ebf4);
       background:var(--color-surface,#0b0d14);padding:16px;font-variant-numeric:tabular-nums}
.whead{display:flex;justify-content:space-between;font-size:13px;font-weight:700;margin:0 0 12px}
.whead span{color:var(--color-ink-faint,#5a6172);font-weight:400;font-size:12px}
.wfig{display:flex;gap:18px;margin-bottom:10px}
.wfig b{display:block;font-size:22px;line-height:1.1}
.wfig .lbl{color:var(--color-ink-faint,#5a6172);font-size:11px}
.wfig .ok b{color:var(--color-ok,#34d399)}
.wfig .warn b{color:var(--color-warn,#fbbf24)}
.wfig .bad b{color:var(--color-bad,#f87171)}
.wfig .run b{color:var(--color-brand,#818cf8)}
.wbar{height:6px;border-radius:3px;background:var(--color-line,rgb(255 255 255 / 0.07));overflow:hidden;margin:4px 0 8px}
.wbar i{display:block;height:100%;background:var(--color-ok,#34d399)}
.wrow{display:flex;justify-content:space-between;gap:10px;padding:8px 0;border-bottom:1px solid var(--color-line,rgb(255 255 255 / 0.07));
      font-size:13px;background:none;border-left:0;border-right:0;border-top:0;width:100%;text-align:left;color:inherit;font-family:inherit;line-height:inherit;cursor:pointer}
.wrow:last-child{border-bottom:0}
.wrow .num{font-family:ui-monospace,Menlo,monospace;font-size:12px;color:var(--color-ink-muted,#8b93a7)}
.wrow .bad{color:var(--color-bad,#f87171)}
.wrow .warn{color:var(--color-warn,#fbbf24)}
.wrow .run{color:var(--color-brand,#818cf8)}
.wfoot{display:flex;justify-content:space-between;color:var(--color-ink-faint,#5a6172);font-size:12px;padding-top:8px}
.wquiet{color:var(--color-ok,#34d399);font-size:12px}
.wrefusal{color:var(--color-ink-faint,#5a6172);font-size:12px;padding-top:8px}
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
  row.addEventListener("click", () => void open(host, row, `roomcare:room?id=${roomId}`));
  return row;
}

/** What a widget draws when its read failed — a failure, never a figure. */
export function unread(title: string, scope: string, because: string): HTMLElement {
  return card(title, scope, [el("div", "wrefusal", `Could not be read — ${because}`)]);
}

async function open(host: HostApi, row: HTMLElement, destination: string): Promise<void> {
  try {
    await host.call(SHELL_OPEN, "at", { destination });
  } catch (error) {
    const holder = row.closest(".wcard");
    if (holder === null) return;
    holder.querySelector(".wrefusal")?.remove();
    holder.append(el("div", "wrefusal", `Not opened — ${sentence(error)}`));
  }
}

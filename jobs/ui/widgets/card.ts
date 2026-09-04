/**
 * The widget's own chrome — a card, its figures and its rows, in the shell's
 * tokens. A widget is small and read-only: it never draws a control that
 * changes anything, only a row that opens the screen which can.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { el, fill } from "../chrome/element";

const SHELL_OPEN = "shell.open";

const WIDGET_CSS = `
.wcard{font:13px/1.5 var(--font-sans,"Segoe UI",system-ui,sans-serif);color:var(--color-ink,#e9ecf5);
       background:var(--color-surface,#0b0d14);padding:16px;font-variant-numeric:tabular-nums}
.whead{display:flex;justify-content:space-between;align-items:baseline;font-weight:600;margin-bottom:12px}
.whead span{color:var(--color-ink-faint,#5d657a);font-weight:400;font-size:12px}
.wfig{display:flex;gap:18px;margin-bottom:10px}
.wfig b{display:block;font-size:22px;line-height:1.1}
.wfig .lbl{color:var(--color-ink-faint,#5d657a);font-size:11px}
.wfig .ok b{color:var(--color-ok,#3ecf8e)}
.wfig .warn b{color:var(--color-warn,#f5b53f)}
.wfig .bad b{color:var(--color-bad,#ff5c7a)}
.wfig .run b{color:var(--color-brand,#6b7cff)}
.wrow{display:flex;justify-content:space-between;gap:10px;padding:8px 0;border-bottom:1px solid var(--color-line,rgb(255 255 255/.09));
      font-size:13px;background:none;border-left:0;border-right:0;border-top:0;width:100%;text-align:left;
      color:inherit;font-family:inherit;cursor:pointer}
.wrow:last-child{border-bottom:0}
.wrow .num{font-family:ui-monospace,Menlo,monospace;font-size:12px;color:var(--color-ink-muted,#9aa3b8)}
.wrow .bad{color:var(--color-bad,#ff5c7a)}
.wrow .warn{color:var(--color-warn,#f5b53f)}
.wrow .run{color:var(--color-brand,#6b7cff)}
.wquiet{color:var(--color-ok,#3ecf8e);font-size:12px}
.wrefusal{color:var(--color-ink-faint,#5d657a);font-size:12px;padding-top:8px}
`;

/** The widget's stylesheet, added once per draw. */
export function stylesheet(): HTMLStyleElement {
  const style = document.createElement("style");
  style.textContent = WIDGET_CSS;
  return style;
}

/** The card shell: a title, a scope, and whatever the panel drew. */
export function card(title: string, scope: string, body: readonly (Node | null)[]): HTMLElement {
  const root = el("div", "wcard");
  const head = el("div", "whead");
  head.append(document.createTextNode(title), el("span", undefined, scope));
  root.append(head);
  return fill(root, ...body);
}

/** A row of figures — the widget's three numbers. */
export function figures(values: readonly { value: string; label: string; tone: string }[]): HTMLElement {
  const row = el("div", "wfig");
  for (const f of values) {
    const cell = el("div", f.tone);
    cell.append(el("b", undefined, f.value), el("div", "lbl", f.label));
    row.append(cell);
  }
  return row;
}

/** A row that opens a screen in the shell; a refusal is said, never swallowed. */
export function openRow(host: HostApi, left: string, right: string, tone: string, destination: string): HTMLElement {
  const row = el("button", "wrow");
  row.setAttribute("type", "button");
  row.append(el("span", "num", left), el("span", tone, right));
  row.addEventListener("click", () => {
    void open(host, row, destination);
  });
  return row;
}

async function open(host: HostApi, row: HTMLElement, destination: string): Promise<void> {
  try {
    await host.call(SHELL_OPEN, "at", { destination });
  } catch (error) {
    const card_ = row.closest(".wcard");
    if (card_ === null) return;
    const because = error instanceof HostCallError && error.isForPeople ? error.message : null;
    card_.querySelector(".wrefusal")?.remove();
    card_.append(el("div", "wrefusal", because === null ? "That screen could not be opened." : `Not opened — ${because}`));
  }
}

/**
 * The card every GuestOps widget is drawn in, and the rules it enforces.
 *
 * # Three shape rules, made structural where they can be
 *
 * **Read-only.** Nothing here builds a control that writes. The only
 * interactive element this file can make is a tap-through, so a widget cannot
 * accidentally grow a button that checks somebody in.
 *
 * **Every element taps through to the filtered screen.** `row()` and `stat()`
 * both take a destination and are not constructible without one — a tap that
 * lands on the application's home is a tap that wasted the reader's time, and
 * an element that taps nowhere is one they will press anyway.
 *
 * **A number the backend cannot honestly compute is absent, never
 * approximate.** `stat()` takes `number | null` and draws nothing at all for
 * null: not a zero, not a dash, not a smaller font. A widget that cannot answer
 * says less rather than guessing.
 *
 * # Styled by the published set, and only that set
 *
 * The realm carries `SHELL-Q30`'s fourteen tokens and nothing else, so every
 * colour here is one of them or a `color-mix()` of them. A widget is styled by
 * the platform or it is not styled — there is no third source, and a literal
 * would be a dark-theme decision frozen into a card a light property will run.
 */

import type { HostApi } from "@hotelos/sdk";

/** What a tap opens: the app's own word for a screen, resolved by the module. */
export type Destination = string;

/** Make an element with a class and optional text. */
export function el(tag: string, className?: string, text?: string): HTMLElement {
  const element = document.createElement(tag);
  if (className !== undefined) element.className = className;
  if (text !== undefined) element.textContent = text;
  return element;
}

/**
 * The card's chrome: a titled frame with the application's name beside it.
 *
 * @param title what this widget answers
 * @returns the card, and the body to fill
 */
export function card(title: string): { root: HTMLElement; body: HTMLElement } {
  const root = el("div", "w");

  const head = el("div", "wh");
  head.append(el("span", "wt", title), el("span", "grow"), el("span", "wa", "GuestOps"));

  const body = el("div", "wb");
  root.append(head, body);
  return { root, body };
}

/**
 * One figure, with the word for what it counts.
 *
 * @param value the figure, or null when the domain cannot answer
 * @param label what it counts
 * @param tone the semantic colour, where the canvas gives one
 * @returns the tile, or null when there is no honest number
 */
export function stat(
  value: number | null,
  label: string,
  tone?: "ok" | "warn" | "bad",
): HTMLElement | null {
  // The honesty rule, and the reason this returns null rather than drawing a
  // placeholder: a dash reads as zero at a glance, and a zero is a claim.
  if (value === null) return null;

  const tile = el("div", "st");
  tile.append(
    el("b", tone === undefined ? "sv" : `sv ${tone}`, String(value)),
    el("span", "sl", label),
  );

  return tile;
}

/** A small uppercase heading over a list. */
export function label(text: string): HTMLElement {
  return el("div", "wl", text);
}

/**
 * One tappable row.
 *
 * @param cells left to right; the first is the subject and takes the weight
 * @param destination the filtered screen this row opens
 * @param open the tap-through
 * @returns the row
 */
export function row(
  cells: readonly (string | HTMLElement)[],
  destination: Destination,
  open: (destination: Destination) => void,
): HTMLElement {
  const element = el("button", "wr");
  element.setAttribute("type", "button");

  cells.forEach((cell, index) => {
    if (typeof cell === "string") {
      element.append(el("span", index === 0 ? "rs" : "rc", cell));
      return;
    }

    element.append(cell);
  });

  element.addEventListener("click", () => open(destination));
  return element;
}

/** The quiet sentence under a card, where the canvas carries one. */
export function note(text: string): HTMLElement {
  return el("div", "wn", text);
}

/**
 * Tap through, and never leave the widget looking like it worked.
 *
 * A refusal is drawn where the reader is looking. The design's rule is that
 * every element taps through, so a tap that silently does nothing is the one
 * outcome worse than a tap that says why — a person presses it twice, then
 * decides the widget is broken, and they are right.
 *
 * @param host the bridge
 * @param root where a refusal is shown
 * @returns the tap-through
 */
export function opener(host: HostApi, root: () => HTMLElement | null) {
  return (destination: Destination): void => {
    void host
      .call("shell.open", "at", { destination })
      .catch((error: unknown) => {
        const surface = root();
        if (surface === null) return;

        const said = surface.querySelector(".wn");
        const text = error instanceof Error ? error.message : "that screen did not open";

        if (said === null) surface.append(note(text));
        else said.textContent = text;
      });
  };
}

/**
 * The stylesheet, on the published fourteen.
 *
 * One card size because the popover has one — the widget is told nothing about
 * how it is shown (a widget that could detect a stack would eventually be
 * written to behave differently in one), so it draws the same card always.
 */
export function stylesheet(): HTMLStyleElement {
  const style = document.createElement("style");

  style.textContent = `
    .w{display:flex;flex-direction:column;height:100vh;overflow:hidden;
      font-family:var(--font-sans,system-ui,sans-serif);font-size:12.5px;
      color:var(--color-ink,#e8ebf4);background:var(--color-surface-raised,#11141f)}
    .wh{display:flex;align-items:center;gap:8px;padding:12px 14px 10px;
      border-bottom:1px solid var(--color-line,rgba(255,255,255,.07))}
    .wt{font-size:12.5px;font-weight:600;letter-spacing:.01em}
    .wa{font-size:10.5px;color:var(--color-ink-faint,#5a6172)}
    .grow{flex:1}
    .wb{flex:1;padding:12px 14px;display:flex;flex-direction:column;gap:10px;overflow:hidden}
    .sr{display:grid;grid-template-columns:repeat(4,minmax(0,1fr));gap:8px}
    .sr.three{grid-template-columns:repeat(3,minmax(0,1fr))}
    .st{display:flex;flex-direction:column;gap:2px;min-width:0}
    .sv{font-size:19px;font-weight:600;line-height:1.1}
    .sv.ok{color:var(--color-ok,#34d399)}
    .sv.warn{color:var(--color-warn,#fbbf24)}
    .sv.bad{color:var(--color-bad,#f87171)}
    .sl{font-size:10.5px;color:var(--color-ink-faint,#5a6172)}
    /* No margin: the body is a flex column with gap:10px, and a margin on top
       of that is double-spacing the canvas pays for twice on the one widget
       that carries two labels. Business Mix overflowed by exactly the 4px
       these two margins added. */
    .wl{font-size:10px;letter-spacing:.07em;text-transform:uppercase;
      color:var(--color-ink-faint,#5a6172)}
    .wr{display:flex;align-items:center;gap:8px;width:100%;text-align:left;
      padding:7px 0;background:none;border:0;border-bottom:1px solid
      var(--color-line,rgba(255,255,255,.07));font:inherit;color:inherit;cursor:pointer}
    .wr:last-of-type{border-bottom:0}
    .wr:hover{background:color-mix(in srgb, var(--color-brand,#818cf8) 7%, transparent)}
    .rs{flex:1;min-width:0;overflow:hidden;text-overflow:ellipsis;white-space:nowrap}
    .rc{color:var(--color-ink-faint,#5a6172);white-space:nowrap}
    .rc.t{color:var(--color-ink-muted,#8b93a7);font-variant-numeric:tabular-nums}
    .rc.late{color:var(--color-bad,#f87171)}
    .rc.miss{color:var(--color-warn,#fbbf24)}
    .bar{height:5px;border-radius:99px;overflow:hidden;
      background:color-mix(in srgb, var(--color-ink,#e8ebf4) 10%, transparent)}
    .bar i{display:block;height:100%;background:var(--color-brand,#818cf8)}
    .wn{margin-top:auto;padding-top:8px;font-size:10.5px;line-height:1.5;
      color:var(--color-ink-faint,#5a6172)}
  `;

  return style;
}

/**
 * The small marks every screen draws — status and concern pills, priority
 * badges, tags — with their colours resolved from the vocabulary to a token,
 * in one place, so a status never picks a colour of its own.
 */

import { el } from "./element";

const PILL_CSS = `
.pill{display:inline-block;padding:2px 9px;border-radius:999px;font-size:11px;font-weight:600;letter-spacing:.04em;
      border:1px solid var(--color-line,rgb(255 255 255 / 0.07));color:var(--color-ink-muted,#8b93a7);white-space:nowrap}
.pill.ok{color:var(--color-ok,#34d399);border-color:var(--color-ok,#34d399)}
.pill.warn{color:var(--color-warn,#fbbf24);border-color:var(--color-warn,#fbbf24)}
.pill.bad{color:var(--color-bad,#f87171);border-color:var(--color-bad,#f87171)}
.pill.run{color:var(--color-brand,#818cf8);border-color:var(--color-brand,#818cf8)}
.pill.hold{color:var(--color-ink-faint,#5a6172);border-color:var(--color-ink-faint,#5a6172)}
.pill.p1{background:var(--color-bad,#f87171);color:var(--color-ink-on-accent,#0b0d14);border-color:transparent}
.pill.p2{background:var(--color-warn,#fbbf24);color:var(--color-ink-on-accent,#0b0d14);border-color:transparent}
.pill.p3{background:var(--color-brand,#818cf8);color:var(--color-ink-on-accent,#0b0d14);border-color:transparent}
.pill.nt{border-style:dashed}
.tag{font-size:10px;letter-spacing:.1em;text-transform:uppercase;padding:2px 6px;border-radius:4px;margin-left:6px;
     border:1px solid var(--color-line,rgb(255 255 255 / 0.07));color:var(--color-ink-faint,#5a6172);vertical-align:middle}
`;

/** The marks' own sheet, added to the module's stylesheet once. */
export const MARKS_CSS = PILL_CSS;

/** A job status as a pill — S2's nine, each with its tone. */
export function status(value: string): HTMLElement {
  const tone: Record<string, string> = {
    IN_PROGRESS: "run", ACCEPTED: "warn", RESOLVED: "ok", ON_HOLD: "hold", CANCELLED: "hold",
  };
  return el("span", `pill ${tone[value] ?? ""}`.trim(), value.replace("_", " "));
}

/** A concern as a pill, with the reason after it when there is one. */
export function concern(value: string, detail?: string): HTMLElement {
  const tone: Record<string, string> = { ON_TRACK: "ok", AT_RISK: "warn", BREACHED: "bad", STUCK: "bad" };
  const text = detail === undefined ? value.replace("_", " ") : `${value.replace("_", " ")} · ${detail}`;
  return el("span", `pill ${tone[value] ?? ""}`.trim(), text);
}

/** A priority badge — P1, P2, P3, or NT for not triaged. */
export function priority(value: string): HTMLElement {
  if (value === "NOT_TRIAGED") return el("span", "pill nt", "NT");
  return el("span", `pill ${value.toLowerCase()}`, value);
}

/** A small uppercase tag after a title — "linked", "child 1/2 · blocked", "restricted". */
export function tag(text: string): HTMLElement {
  return el("span", "tag", text);
}

/** Stars, for a rating. */
export function stars(count: number): HTMLElement {
  return el("div", "stars", "★".repeat(count) + "☆".repeat(Math.max(0, 5 - count)));
}

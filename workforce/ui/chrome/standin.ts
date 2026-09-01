/**
 * What a screen says when it is not showing the property's own data.
 *
 * # Extracted at the third copy, before the fourth
 *
 * The rota, leave and attendance each wrote this note, and by the third they
 * had drifted into three slightly different sentences saying the same thing. A
 * fallback notice is exactly the text that must **not** vary by screen: a
 * manager who learns to recognise it on one screen has to recognise it on all
 * of them, and three wordings teach them it means three different things.
 *
 * ADR 0124: a surface fails **in place** and names what it awaits.
 */

import { el } from "./element";

/**
 * The note.
 *
 * @param what the screen's own word for its data — "week", "day", "month"
 * @param because the platform's reason, when ADR 0041 permits showing one
 * @returns the panel
 */
export function standIn(what: string, because: string | null): HTMLElement {
  const panel = el("div", "panel");
  const note = el("div", "note");

  note.append(
    el("b", undefined, `Showing the approved example ${what}. `),

    // The platform's message when there is one worth a person's eyes, and this
    // application's own words when there is not. `internal` and `forbidden`
    // carry text for a log, and putting one on a hotel's screen leaks a
    // diagnostic to a supervisor.
    el("span", undefined,
      because ?? "The desktop has no Workforce client yet, so this is a stand-in."),
  );

  panel.append(note);
  return panel;
}

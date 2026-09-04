/**
 * The amber band: a disagreement standing over an override — gold frame 3.
 *
 * # This is the frame's reason for existing
 *
 * The rule the whole PMS mode rests on is that **one truth leaves the
 * application**. The standing override is what the board shows, what Room Care
 * hears and what Context resolves; the disagreement is a flag on that answer and
 * never a second answer (GUEST-Q3). The band is where a person sees both values
 * at once and the platform still speaks with one voice.
 *
 * So it names the two values, says when Opera's arrived and that it was **not
 * applied**, attributes the override to the person who made it, and offers the
 * two ways out. Both values stay in history whichever is chosen — a decision
 * that discarded the losing value could not explain itself later, and the
 * property is the party that has to explain it.
 *
 * **Clearing takes the stay's own write permission** — the same one that made
 * the override. Author-only clearing fails across shifts and supervisor-only
 * escalates a routine reconciliation; GUEST-Q3 refused both by name, which is
 * why there is no separate control here for a supervisor.
 */

import type { Banner } from "../../book/model";
import { control, el } from "../../chrome/element";

/**
 * Draw the band.
 *
 * @param banner the disagreement standing on this stay
 * @returns the band
 */
export function banner(banner: Banner): HTMLElement {
  const element = el("div", "ban");
  const said = el("div");

  said.append(
    el("b", undefined, banner.headline),
    document.createTextNode(` ${banner.detail}`),
  );

  const why = el("span", "why");
  why.append(document.createTextNode(banner.attribution));
  said.append(why);

  const acts = el("div", "grow");

  banner.actions.forEach((label, index) => {
    acts.append(control(index === 0 ? "btn sm pri" : "btn sm", label));
  });

  element.append(said, acts);
  return element;
}

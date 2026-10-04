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
 *
 * # What drew these controls off until 2026-10-04
 *
 * *"Settling a disagreement with the PMS is not available from this screen
 * yet."* — **and the capability had been complete the whole time.**
 * `ReconciliationService.ClearAsync` carried the entire ruling with three tests
 * driving it, and no door served it: not this module's, not the gRPC surface.
 * The band was honest about what it could reach and wrong about what the
 * application could do.
 */

import type { Banner, ClearSide } from "../../book/model";
import { control, el } from "../../chrome/element";

/**
 * Draw the band.
 *
 * @param banner the disagreement standing on this stay
 * @param clear what settling it does — the row, and the side that stands
 * @returns the band
 */
export function banner(
  banner: Banner,
  clear: (disagreementId: string, side: ClearSide) => void,
): HTMLElement {
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

  // The side is read from the action rather than from its caption: the two
  // controls differ by which value they keep, and nothing about the English
  // says which. Reworded tomorrow, this still settles the right way.
  for (const action of banner.actions) {
    acts.append(control(
      "btn sm",
      action.label,
      () => clear(banner.disagreementId, action.side),
    ));
  }

  element.append(said, acts);
  return element;
}

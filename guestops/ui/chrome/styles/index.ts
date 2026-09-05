/**
 * The module's stylesheet — the approved design, on the published contract.
 *
 * # Two rules, and the second is the one that was broken
 *
 * 1. **Consume only published token names.** `@hotelos/sdk`'s `TOKENS` is the
 *    contract; a `var()` on anything else silently takes its fallback, and the
 *    module is then styled by nobody — SHELL-Q33's class, in a module rather
 *    than in the realm.
 * 2. **Derive everything else from those names.** The first build hardcoded
 *    seventeen `rgba(…)` literals for the chip tints and borrowed four
 *    unpublished names (`--color-aurora-1`, `--color-aurora-3`,
 *    `--color-surface-sunken`, `--r-md`). Literals cannot follow a theme: in a
 *    light property every chip kept its dark-theme wash.
 *
 * All derivation is `derived.ts`, so the rest of the stylesheet reads as
 * colour names and the contract is visible in one file. Those
 * `--go-*` variables are the **module's own** — it may define what it likes;
 * what it may not do is consume a name the host never promised.
 *
 * # The one place the contract is short, reported rather than worked around
 *
 * The design's marks need **two non-semantic accents** — `from Opera` (cyan in
 * the gold) and `override` (violet) — and the contract publishes exactly one
 * accent, `color-brand`. Rather than invent a token, `override` is mixed from
 * two published colours so it is a distinct hue that still follows the theme.
 * It is close to the drawn violet and it is not the drawn violet. A second
 * published accent would restore it exactly.
 *
 * # A directory, because the sheet arrived
 *
 * This was one file at 282 lines, in five named blocks. Group 1's frames need
 * two more — fields and the overlay — which puts it past ADR 0027's ceiling,
 * and the five blocks were already the boundary to split on (ADR 0036: the
 * extraction follows a boundary that exists, never one invented at the moment
 * of splitting). The public surface did not move: `./chrome/styles` still
 * resolves and no import changed, which is ADR 0042's rule for a file becoming
 * a directory.
 */

import { DERIVED } from "./derived";
import { FORMS } from "./forms";
import { MARKS } from "./marks";
import { PANEL } from "./panel";
import { SHEET } from "./sheet";
import { SHELL } from "./shell";
import { TABLE } from "./table";

/**
 * The stylesheet element, built once and re-attached with each screen.
 *
 * The order is the cascade: derivation first because everything below reads
 * those names, then the shape of the window, then what sits inside it. Nothing
 * here overrides anything above it by specificity, so the order is for reading
 * rather than for winning.
 */
export function stylesheet(): HTMLStyleElement {
  const style = document.createElement("style");
  style.textContent = [DERIVED, SHELL, TABLE, PANEL, FORMS, SHEET, MARKS].join("");
  return style;
}

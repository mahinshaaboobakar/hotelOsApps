/**
 * Every class a widget's failure renders is styled by the sheet THAT WIDGET loads.
 *
 * **Not "styled anywhere in the module" — that question passes straight through
 * the defect.** GG found Workforce's widget failure styles living in the screen
 * stylesheet, which no widget realm loads: on a real property the lock drew full
 * size and the sentence was unstyled, while every class was defined somewhere.
 *
 * A widget is its own realm and loads exactly one sheet, `stylesheet()` from
 * `widgets/card.ts`. That is **derived** from the five entry files below rather
 * than assumed, so an entry that starts reaching for another sheet is caught
 * here, and the class match is made against the sheet the realm actually has.
 *
 * It found one on its first run in a browser (2026-09-18): the refusal's tone
 * class `no` matched no rule, its colour an inheritance nobody had written down.
 * Proved able to fail by removing the mark's rules from a shipped bundle — the
 * lock then drew 292×243 in ink, GG's symptom exactly.
 */

import { readdirSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

import { describe, expect, it } from "vitest";

import { failureDrawing, type Cause } from "@hotelos/sdk";

import { stylesheet, unanswered } from "../widgets/card";

const ENTRY = join(dirname(fileURLToPath(import.meta.url)), "..", "widgets", "entry");
/** Contract v2's six causes (`d45f028d`). */
const CAUSES: readonly Cause[] = [
  "unanswered", "forbidden", "unadmitted", "ungranted", "undecidable", "faulted",
];

/** Every `.class` a sheet's selectors name. */
function styledBy(css: string): Set<string> {
  // Selectors only: strip the declaration blocks and comments first, so a value
  // such as `.5` or a class quoted in a comment cannot count as a rule.
  const selectors = css.replace(/\/\*[\s\S]*?\*\//g, "").replace(/\{[^}]*\}/g, " ");
  return new Set([...selectors.matchAll(/\.([A-Za-z_-][\w-]*)/g)].map((m) => m[1] as string));
}

/** Every class on every element of a rendered tree. */
function used(root: Element): Set<string> {
  const names = new Set<string>();
  for (const node of [root, ...root.querySelectorAll("[class]")]) {
    for (const name of (node.getAttribute("class") ?? "").split(/\s+/)) {
      if (name !== "") names.add(name);
    }
  }
  return names;
}

function unstyled(root: Element, css: string): string[] {
  const styled = styledBy(css);
  return [...used(root)].filter((name) => !styled.has(name)).sort();
}

describe("a widget realm's failure", () => {
  it("loads card.ts's sheet and no other — derived from every entry", () => {
    const entries = readdirSync(ENTRY).filter((file) => file.endsWith(".ts"));

    // The population is asserted, so a walk that quietly found nothing cannot
    // pass on an empty set.
    expect(entries).toHaveLength(5);

    for (const file of entries) {
      const source = readFileSync(join(ENTRY, file), "utf8");
      expect(source, file).toMatch(/import \{[^}]*\bstylesheet\b[^}]*\} from "\.\.\/card"/);
      expect(source, file).not.toMatch(/chrome\/styles/);

      // `widgets/recorded.ts` is the capture harness's loaded answer. A widget
      // importing it is the recorded fallback APPS-Q42 removed, coming back.
      expect(source, file).not.toMatch(/from "\.\.\/recorded"|from "\.\/recorded"/);
    }
  });

  it.each(CAUSES)("styles every class it renders, from that sheet — %s", (cause) => {
    const card = unanswered(
      "Today at the Desk",
      failureDrawing(
        { cause, capability: "reservation.read", method: "desk", said: null, at: new Date() },
        { app: "GuestOps", the: "today at this property" },
      ),
      { retry: () => {}, open: () => {} },
    );

    expect(unstyled(card, stylesheet().textContent ?? "")).toEqual([]);
  });

  it("can fail: a class the sheet does not name is reported", () => {
    // The instrument's own negative control, so a matcher that silently
    // matched everything could not pass the test above.
    const probe = document.createElement("div");
    probe.className = "wg no-such-rule";

    expect(unstyled(probe, stylesheet().textContent ?? "")).toEqual(["no-such-rule"]);
    expect(unstyled(probe, ".wf{color:red}")).toEqual(["no-such-rule", "wg"]);
  });
});

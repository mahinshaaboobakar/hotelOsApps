import { HostCallError, causeOf, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { attendanceToday } from "../widgets/panel/attendance-today";
import { comingUp } from "../widgets/panel/coming-up";
import { onLeave } from "../widgets/panel/on-leave";
import { pendingRequests } from "../widgets/panel/pending-requests";
import { shiftBoard } from "../widgets/panel/shift-board";
import { WIDGET_CSS } from "../widgets/styles";

/**
 * Every class a widget draws has a rule in the ONE sheet a widget mounts.
 *
 * # Why the module's stylesheet guard could not see this
 *
 * A widget mounts `WIDGET_CSS` and nothing else. Its failure card's rules —
 * including `.wfail`, written FOR widgets — lived in `chrome/styles.ts`, so on a
 * real property every failure card drew its lock with no size, filling the card,
 * and its sentence unstyled at the floor.
 *
 * `stylesheet.test.ts` passed throughout, because it asks whether a class is
 * defined in SOME stylesheet of the module. That is true and it is the wrong
 * question: the population it walks is the module, and the population that
 * decides what a widget looks like is the one sheet the widget mounts. **A
 * guard derived over the wrong axis is a hardcoded list wearing a walk.**
 *
 * # Why this renders rather than reads source
 *
 * A widget's DOM carries classes emitted by modules it IMPORTS — the mark comes
 * from `chrome/failure.ts` — so a source walk over `widgets/` would miss exactly
 * the class that broke. Rendering the card and reading its own DOM is the
 * population, with nothing left to enumerate.
 */

type Kind = ConstructorParameters<typeof HostCallError>[0]["kind"];

function failing(kind: Kind): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: () => Promise.reject(new HostCallError({ kind, message: "the test asked" })),
    on: () => () => {},
  };
}

const PANELS = [shiftBoard, attendanceToday, pendingRequests, comingUp, onLeave];

/** Every class token a rendered element carries, and every descendant's. */
function classes(root: Element): Set<string> {
  const found = new Set<string>();

  for (const node of [root, ...Array.from(root.querySelectorAll("*"))]) {
    for (const token of Array.from(node.classList)) found.add(token);
  }

  return found;
}

/** Whether the widget's own sheet names this class in any selector. */
function defined(token: string): boolean {
  // No escaping: a class token is word characters and hyphens only, and a
  // hyphen is literal outside a character class. The first version escaped it,
  // which is an INVALID escape under the u flag — so all three runs failed by
  // throwing, and read as orphans until the output was opened.
  return new RegExp(String.raw`\.${token}(?![\w-])`, "u").test(WIDGET_CSS);
}

/**
 * One host kind per cause — contract v2's six (ADR 0192, drawn in 64b and 64e).
 *
 * A list of kinds is not a list of causes: two kinds that map to one cause would
 * render the same card twice and leave a colour unexamined. So the list is
 * checked against the SDK's own mapping below, and each card is checked for the
 * cause its kind should produce — which is also what proves the kind reached
 * the host, rather than being refused locally for a capability `failing` does
 * not grant.
 */
const KINDS = [
  "unavailable",
  "forbidden",
  "local_forbidden",
  "user_forbidden",
  "model_unavailable",
  "internal",
] as const satisfies readonly Kind[];

describe("a widget's classes live in the sheet a widget mounts", () => {
  it("renders every cause once, so no colour goes unexamined", () => {
    // Six is the size of `Cause` in the SDK at contract v2. Spelled out rather
    // than derived, because the type has no runtime list to derive it from —
    // and a new cause should fail HERE, where its missing rule would otherwise
    // go unseen.
    expect(new Set(KINDS.map(causeOf)).size).toBe(6);
  });

  for (const kind of KINDS) {
    it(`draws nothing the widget sheet does not style, when a read is ${kind}`, async () => {
      const orphans: string[] = [];

      for (const panel of PANELS) {
        const card = await panel(failing(kind));
        if (card.querySelector(`.fail-${causeOf(kind)}`) === null) {
          orphans.push(`${panel.name}: did not draw .fail-${causeOf(kind)}`);
        }
        for (const token of classes(card)) {
          if (!defined(token)) orphans.push(`${panel.name}: .${token}`);
        }
      }

      expect(orphans).toEqual([]);
    });
  }

  it("sizes the mark, which is the rule whose absence filled the card", () => {
    // Named rather than left to the walk above: the SVG carries no class, so
    // the walk cannot see that it is unsized — only that its parent is styled.
    expect(WIDGET_CSS).toMatch(/\.wf-mark svg\{[^}]*width:20px/u);
  });

  it("finds something, so a silent zero cannot pass for conformance", async () => {
    const card = await shiftBoard(failing("forbidden"));

    // A walk that found no classes would pass the checks above by measuring
    // nothing. The failure card has at least its card, body, mark, sentence.
    expect(classes(card).size).toBeGreaterThan(5);
  });
});

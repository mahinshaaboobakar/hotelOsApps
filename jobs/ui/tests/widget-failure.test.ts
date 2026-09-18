import { readFileSync } from "node:fs";
import { join } from "node:path";

import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { PANELS } from "../preview/widgets";
import { jobsNow } from "../widgets/panel/jobs-now";
import { stylesheet } from "../widgets/sheet";

/**
 * Every widget, in every state it can fail in, draws only classes **its own
 * sheet** defines.
 *
 * **GG's finding in Workforce (`787560f`), run against Jobs.** Every failure
 * style there — the widget ones included — lived in the screen stylesheet, and a
 * widget loads only its own. So on a real property each widget that could not
 * read drew its lock at full card size with the sentence unstyled underneath,
 * while GG's stylesheet check passed throughout: it asked whether a class is
 * styled *somewhere in the module*, not in the sheet this widget loads.
 *
 * `styled.test.ts` walks source files per realm, which is close and not the
 * same: a walk sees the classes written in the files it lists, and a class
 * composed at runtime, or drawn by a file in another directory, is invisible to
 * it. This renders the widget and reads the classes that are actually in the
 * DOM, then matches each against the text of the one sheet `serve()` puts beside
 * it. What a property sees is the rendered card and that sheet, and nothing
 * else.
 *
 * **And it checks the harness produced the state it was asked for.** GG's
 * could only produce the timeout, so its forbidden and faulted rows were three
 * copies of one state. Each case here asserts the sentence and the onward line
 * that belong to its cause, so a host that failed every call the same way would
 * fail two thirds of this file rather than pass it.
 *
 * **Proved to fail before its green was reported** (2026-09-18), each probe
 * restored after it:
 *
 * ```text
 * delete .wfail-mark svg (GG's probe)             18 fail · wfail-mark
 * a host that can only time out                   12 fail · forbidden + faulted
 * delete .wfail-said                              18 fail · wfail-said
 * .wfail-mark svg → .wfail-mark i (hangs nothing)  18 fail · wfail-mark
 * .wfail-said → .wrow .wfail-said (compound trap) 18 fail · wfail-said
 * ```
 *
 * The last is the one a text search of the sheet cannot see: the name is still
 * in it, so GG's method and this file's own first version both pass it.
 */

const PROPERTY = { timezone: "Asia/Qatar", locale: "en-GB" };
const GRANTS = ["job.read"];

/** A host that refuses every call with one kind — the kinds `causeOf` maps. */
function failing(kind: "unavailable" | "forbidden" | "internal"): HostApi {
  return {
    identity: { id: "jobs", version: "0.4.1", capabilities: GRANTS },
    property: PROPERTY,
    call: (capability: string, method: string) =>
      Promise.reject(new HostCallError({ kind, message: `${capability}/${method} refused for the test` })),
    on: () => () => {},
  };
}

/** Each state: the kind that produces it, and what only that state says. */
const STATES = [
  { cause: "unanswered", kind: "unavailable", said: "Jobs did not answer in time", onward: "Try again →" },
  { cause: "forbidden", kind: "forbidden", said: "You do not have access to", onward: "Open Jobs →" },
  { cause: "faulted", kind: "internal", said: "Jobs could not build", onward: "Open Jobs →" },
] as const;

const WIDGETS: Record<string, (host: HostApi) => Promise<HTMLElement>> = { ...PANELS, "jobs-now": jobsNow };

const ui = join(import.meta.dirname, "..");

/** Every class on the rendered card and everything inside it, SVG included. */
function drawn(root: Element): string[] {
  return [root, ...Array.from(root.querySelectorAll("*"))].flatMap((node) =>
    (node.getAttribute("class") ?? "").split(/\s+/).filter((name: string) => name !== ""),
  );
}

/**
 * Mount the card beside the one sheet a widget loads, as `serve()` does, and
 * return that sheet's selectors as the browser parsed them.
 */
function mounted(card: HTMLElement): string[] {
  const sheet = stylesheet();
  document.body.replaceChildren(sheet, card);

  return Array.from((sheet.sheet as CSSStyleSheet).cssRules).flatMap((rule) =>
    "selectorText" in rule ? String(rule.selectorText).split(",").map((s) => s.trim()) : [],
  );
}

/**
 * The classes on the card that no rule in its own sheet **applies** to.
 *
 * **Applies, not merely mentions** — the step beyond GG's method, and the reason
 * is FF's finding in GuestOps: a refusal-tone class that matched no rule and
 * drew correctly only by inheritance. A class can appear in a sheet inside a
 * compound selector that never matches the element carrying it — `.wrow .bad`
 * on a `.bad` outside any row — and a check that searches the sheet's text
 * finds the name and passes. This asks, for each class on each element,
 * whether some selector naming that class actually matches that element.
 *
 * **Or is a hook for one that matches inside it — narrowed, not removed.** The
 * first run of this fired on all 18 cases for `.wfail-mark`, which the sheet
 * names only in `.wfail-mark svg`: a class whose job is to be the ancestor of a
 * rule, on a wrapper that draws nothing of its own (its colour is inline, as
 * 64b draws it). That is a legitimate case, and a guard that fires on one is
 * narrowed. The narrowing is exact: the class must sit in the **ancestor** part
 * of a selector that matches a real element below this one — a hook nothing
 * hangs from still fails.
 */
function unapplied(card: HTMLElement, selectors: readonly string[]): string[] {
  const missing = new Set<string>();

  for (const node of [card, ...Array.from(card.querySelectorAll("*"))]) {
    for (const cls of drawn(node).filter((c) => node.classList.contains(c))) {
      const names = new RegExp(String.raw`\.${cls}(?![\w-])`);
      const naming = selectors.filter((s) => names.test(s));

      const own = naming.some((s) => node.matches(s));
      const hook = naming.some((s) => {
        const target = s.split(/\s+|>|\+|~/).filter((part) => part !== "").pop() ?? "";
        return !names.test(target) && node.querySelectorAll(s).length > 0;
      });

      if (!own && !hook) missing.add(cls);
    }
  }

  return [...missing];
}

describe("each widget's failure draws only what its own sheet styles", () => {
  it("covers every widget the manifest ships, not the ones someone remembered", () => {
    const manifest = readFileSync(join(ui, "..", "manifest.yaml"), "utf8");
    const declared = [...manifest.matchAll(/^\s+- id: ([a-z-]+)\s*\n\s+name:/gm)].map((m) => m[1]).sort();

    expect(declared).toHaveLength(6);
    expect(Object.keys(WIDGETS).sort()).toEqual(declared);
  });

  it("every widget loads the sheet this file checks against", () => {
    // The sheet under test is `widgets/sheet.ts`'s because `serve()` loads it,
    // and every entry goes through `serve()`. Were one entry to mount its panel
    // some other way, the sheet above would not be the one it loads.
    expect(readFileSync(join(ui, "widgets", "serve.ts"), "utf8")).toContain('from "./sheet"');

    for (const name of Object.keys(WIDGETS)) {
      const entry = readFileSync(join(ui, "widgets", "entry", `${name}.ts`), "utf8");
      expect(entry, `${name} mounts through serve()`).toMatch(/\bserve\(/);
    }
  });

  for (const [name, panel] of Object.entries(WIDGETS)) {
    for (const state of STATES) {
      it(`${name} · ${state.cause}`, async () => {
        const card = await panel(failing(state.kind));

        // The harness produced the state it was asked for, not a timeout
        // wearing three labels.
        expect(card.querySelector(".wfail-said")?.textContent).toContain(state.said);
        expect(card.querySelector(".wfail-open")?.textContent).toBe(state.onward);

        // A silent zero cannot pass for conformance — GG's floor. A card that
        // drew almost nothing would match every rule it had.
        expect(new Set(drawn(card)).size, `${name} draws enough to judge`).toBeGreaterThan(5);

        const selectors = mounted(card);
        expect(unapplied(card, selectors), `${name}: classes no rule in its own sheet applies to`).toEqual([]);

        // The mark is an SVG with no class of its own, so a class match alone
        // cannot see it drawn at full card size — which is exactly what GG's
        // property showed. The sheet has to size it.
        expect(card.querySelector(".wfail-mark svg"), "the mark is drawn").not.toBeNull();
        expect(stylesheet().textContent).toMatch(/\.wfail-mark svg\{[^}]*width:20px;height:20px/);
      });
    }
  }
});

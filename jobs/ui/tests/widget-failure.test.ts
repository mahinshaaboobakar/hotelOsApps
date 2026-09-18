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
 * **Proved to fail both ways before its green was reported** (2026-09-18):
 * deleting `.wfail-mark svg` from the widget sheet — GG's probe — fails all 18
 * state rows, each naming `wfail-mark`; and a host that answers every call as
 * a timeout fails exactly the 12 forbidden and faulted rows.
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

/** Every class any selector in the widget's own sheet names. */
const own = new Set(
  [...(stylesheet().textContent ?? "").matchAll(/\.([A-Za-z][\w-]*)/g)].flatMap((match) => match[1] ?? []),
);

/** Every class on the rendered card and everything inside it, SVG included. */
function drawn(root: Element): string[] {
  return [root, ...Array.from(root.querySelectorAll("*"))].flatMap((node) =>
    (node.getAttribute("class") ?? "").split(/\s+/).filter((name: string) => name !== ""),
  );
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

        const unstyled = [...new Set(drawn(card))].filter((cls) => !own.has(cls));
        expect(unstyled, `${name} draws classes its own sheet does not define`).toEqual([]);

        // The mark is an SVG with no class of its own, so a class match alone
        // cannot see it drawn at full card size — which is exactly what GG's
        // property showed. The sheet has to size it.
        expect(card.querySelector(".wfail-mark svg"), "the mark is drawn").not.toBeNull();
        expect(stylesheet().textContent).toMatch(/\.wfail-mark svg\{[^}]*width:20px;height:20px/);
      });
    }
  }
});

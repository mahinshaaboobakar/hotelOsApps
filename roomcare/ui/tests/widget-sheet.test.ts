import { readFileSync } from "node:fs";
import { join } from "node:path";

import { HostCallError, type Cause, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { FAILURE_CSS } from "../chrome/failure";
import { stylesheet } from "../widgets/card";
import * as PANELS from "../widgets/panel/panels";
import { host } from "./host";

/**
 * Every class a Room Care widget draws is styled by a rule in the ONE sheet a
 * widget mounts — GG's method (`workforce/ui/tests/widget-sheet.test.ts`), with
 * the step Jobs took past it (`jobs/ui/tests/widget-failure.test.ts`, `751c328`):
 * a rule must APPLY to the element, not merely name its class.
 *
 * - **The sheet a widget mounts.** A widget bundle mounts `widgets/card.ts`'s
 *   `stylesheet()` and nothing else (`widgets/serve.ts`). The module's sheet in
 *   `chrome/styles.ts` never reaches it, so "styled somewhere in Room Care" is
 *   the wrong question.
 * - **Rendered, not read.** A widget carries classes from modules it imports —
 *   its failure mark comes from `chrome/failure.ts` — so the population is the
 *   rendered DOM, with nothing to enumerate by hand.
 * - **Applies, not mentions.** A sheet can name a class inside a compound that
 *   never matches the element carrying it (`.wfig .ok b` names `.ok`; an `.ok`
 *   span in a row is not styled by it). A search of the sheet's text passes that;
 *   this asks, per element and per class, whether a selector naming the class
 *   matches the element — or hooks a real element below it (`.wf-mark svg`).
 * - **Every widget the manifest ships**, answered and failing in each of the
 *   platform's six ways (contract v2), through the host's own error kinds.
 */

const ROOT = join(process.cwd(), "..");

/** The widgets the manifest ships, by bundle name — derived, so a sixth is covered the day it is declared. */
function shippedWidgets(): Record<string, (h: HostApi) => Promise<HTMLElement>> {
  const manifest = readFileSync(join(ROOT, "manifest.yaml"), "utf8");
  const names = [...manifest.matchAll(/file:\s*ui\/widgets\/([\w-]+)\.js/gu)].map((m) => m[1]!);
  return Object.fromEntries(names.map((name) => {
    const entry = readFileSync(join(process.cwd(), "widgets", "entry", `${name}.ts`), "utf8");
    const served = /serve\((\w+)\)/u.exec(entry)?.[1] ?? "";
    const panel = (PANELS as Record<string, unknown>)[served];
    if (typeof panel !== "function") throw new Error(`${name}: its entry serves "${served}", which panels.ts does not export`);
    return [name, panel as (h: HostApi) => Promise<HTMLElement>];
  }));
}

const WIDGETS = shippedWidgets();

type Kind = ConstructorParameters<typeof HostCallError>[0]["kind"];

/**
 * Contract v2's six causes, each reached through a host error kind that the
 * SDK's `causeOf` maps to it — the SDK exports the type, not a list, so the
 * list is here, and the drawn class names prove each kind reached its cause.
 */
const CAUSES = [
  ["unanswered", "unavailable"],
  ["forbidden", "forbidden"],
  ["unadmitted", "local_forbidden"],
  ["ungranted", "user_forbidden"],
  ["undecidable", "model_unavailable"],
  ["faulted", "internal"],
] as const satisfies readonly (readonly [Cause, Kind])[];

function failing(kind: Kind): HostApi {
  return { ...host(["roomcare.read"]), call: () => Promise.reject(new HostCallError({ kind, message: "the test asked" })) };
}

/** Mount the card beside the one sheet a widget loads, as `serve()` does, and return the selectors as parsed. */
function mounted(card: HTMLElement): string[] {
  const sheet = stylesheet();
  document.body.replaceChildren(sheet, card);
  return Array.from((sheet.sheet as CSSStyleSheet).cssRules).flatMap((rule) =>
    "selectorText" in rule ? String(rule.selectorText).split(",").map((s) => s.trim()) : []);
}

/** The classes on the card that no rule in its own sheet applies to, as `element: .class`. */
function unapplied(card: HTMLElement, selectors: readonly string[]): string[] {
  const missing = new Set<string>();
  for (const node of [card, ...Array.from(card.querySelectorAll("*"))]) {
    for (const cls of Array.from(node.classList)) {
      const names = new RegExp(String.raw`\.${cls}(?![\w-])`, "u");
      const naming = selectors.filter((s) => names.test(s));
      const own = naming.some((s) => node.matches(s));
      // A hook: the class sits in a selector's ANCESTOR part, and that selector matches a real element below
      // this one. Asked of the document and kept to this node's descendants — a browser's semantics.
      // happy-dom's scoped `node.querySelectorAll(".wfig .ok b")` answers 0 where a browser answers 1,
      // because it will not match an ancestor above `node`; measured 2026-09-19, and it made the
      // figures' tone divs read as unstyled when their `b` is.
      const hook = naming.some((s) => {
        const target = s.split(/\s+|>|\+|~/u).filter((part) => part !== "").pop() ?? "";
        return !names.test(target) && Array.from(document.querySelectorAll(s)).some((m) => m !== node && node.contains(m));
      });
      if (!own && !hook) missing.add(`${node.tagName.toLowerCase()}.${cls}`);
    }
  }
  return [...missing];
}

async function check(h: HostApi): Promise<string[]> {
  const found: string[] = [];
  for (const [name, panel] of Object.entries(WIDGETS)) {
    const card = await panel(h);
    for (const miss of unapplied(card, mounted(card))) found.push(`${name}: ${miss}`);
  }
  return found;
}

describe("a Room Care widget's classes are styled by the sheet a widget mounts", () => {
  it("covers the five widgets the manifest ships, not the ones someone remembered", () => {
    expect(Object.keys(WIDGETS)).toEqual(["rooms-ready", "arrivals-waiting", "attention", "attendants-now", "pending-policy"]);
  });

  it("draws nothing its own sheet does not style, when every read answers", async () => {
    expect(await check(host(["roomcare.read"]))).toEqual([]);
  });

  for (const [cause, kind] of CAUSES) {
    it(`draws nothing its own sheet does not style, when a read is ${cause}`, async () => {
      expect(await check(failing(kind))).toEqual([]);
    });
  }

  it("draws each cause as its own state, so the six runs above measured six different cards", async () => {
    const drawn = await Promise.all(CAUSES.map(([, kind]) => failing(kind)).map(async (h) => {
      const card = await WIDGETS["rooms-ready"]!(h);
      return [card.querySelector(".wf-mark")?.className, card.querySelector(".wf-said")?.textContent, card.querySelector(".wf-open")?.textContent];
    }));
    expect(drawn).toEqual([
      ["wf-mark fail-unanswered", "Room Care did not answer in time", "Try again →"],
      ["wf-mark fail-forbidden", "You do not have access to today's departures", "Open Room Care →"],
      ["wf-mark fail-unadmitted", "Room Care has not been allowed to read this", "Open Room Care →"],
      ["wf-mark fail-ungranted", "This account has not been granted this", "Open Room Care →"],
      ["wf-mark fail-undecidable", "Access to this could not be checked", "Open Room Care →"],
      ["wf-mark fail-faulted", "Room Care could not build this", "Open Room Care →"],
    ]);
  });

  it("colours each cause as 64e approved, in the widget's sheet and the screen's — refusals grey, the model state red", () => {
    for (const [sheet, owner] of [[stylesheet().textContent ?? "", "wf-mark"], [FAILURE_CSS, "fail-mark"]] as const) {
      const colours = CAUSES.map(([cause]) => {
        const rule = new RegExp(String.raw`\.${owner}\.fail-${cause}\{color:var\(--color-([\w-]+),`, "u").exec(sheet);
        return [cause, rule?.[1]];
      });
      expect(colours).toEqual([
        ["unanswered", "warn"],
        ["forbidden", "ink-muted"],
        ["unadmitted", "ink-muted"],
        ["ungranted", "ink-muted"],
        ["undecidable", "bad"],
        ["faulted", "bad"],
      ]);
    }
  });

  it("sizes the mark, which the class walk cannot see — the svg carries no class", () => {
    expect(stylesheet().textContent ?? "").toMatch(/\.wf-mark svg\{[^}]*width:20px/u);
  });

  it("finds something, so a silent zero cannot pass for conformance", async () => {
    for (const h of [host(["roomcare.read"]), failing("forbidden")]) {
      for (const panel of Object.values(WIDGETS)) {
        const card = await panel(h);
        expect(new Set([card, ...Array.from(card.querySelectorAll("*"))].flatMap((n) => Array.from(n.classList))).size).toBeGreaterThan(3);
      }
    }
  });
});

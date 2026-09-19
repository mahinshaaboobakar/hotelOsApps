/**
 * No surface cites a document to the person reading it.
 *
 * The owner's ruling of 2026-09-19: a mock carries the screen and notes for the
 * developer, and the notes are never built as screen. A design-section
 * reference, an ADR or a register id is the plainest kind of note — it finds a
 * ruling for an engineer and means nothing at a front desk. Extended from GG's
 * Workforce check (`register-ids.test.ts`, `b001bfac`) to the four shapes the
 * ruling names.
 *
 * **Rendered, not read from source**: a comment citing a ruling is a record
 * and is right; only what reaches a person is a claim to them. Read includes
 * the reasons carried as tooltips and accessible descriptions.
 *
 * This walks the harness's fixtures as well as what the service sends: a
 * fixture is what every capture shows, so a citation there is on the owner's
 * screen at review whether or not a property ever receives it.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { describe, expect, it } from "vitest";

import { mountWidget, readable, SCREENS, surfaceHost, WIDGETS } from "./surfaces";

/** The shapes of a document citation, each named for what it cites. */
const CITATIONS: readonly (readonly [string, RegExp])[] = [
  ["a design section", /\bdesign\s*§\s*[\d.]*|§\s*\d[\d.]*/giu],
  ["an ADR", /\bADR[\s-]*\d+/gu],
  ["a register id", /\b[A-Z]+-Q\d+[a-z]?\b/gu],
];

function cited(text: string): string[] {
  return CITATIONS.flatMap(([what, shape]) =>
    [...text.matchAll(shape)].map((found) => `${what}: "${found[0]}"`));
}

describe("document citations on a surface", () => {
  it("are found in the shapes the ruling names — the positive control", () => {
    expect(cited("Jobs' board (design §6)")).toEqual(['a design section: "design §6"']);
    expect(cited("per §4.2")).toEqual(['a design section: "§4.2"']);
    expect(cited("as ADR 0106 requires")).toEqual(['an ADR: "ADR 0106"']);
    expect(cited("accepted with GUEST-Q6")).toEqual(['a register id: "GUEST-Q6"']);
    expect(cited("Check in · 48 h of arrival · ₹ 8 400.00 · BK-4471")).toEqual([]);
  });

  for (const [name, draw] of SCREENS) {
    it(`${name} shows none`, async () => {
      const main = document.createElement("div");
      await draw(surfaceHost(), main);

      expect(main.querySelector(".fail"), `${name} could not read its fixture`).toBeNull();
      expect(cited(readable(main))).toEqual([]);
    });
  }

  for (const name of WIDGETS) {
    it(`the ${name} widget shows none`, async () => {
      const body = await mountWidget(name);

      expect(body.querySelector(".wx"), `${name} could not read its fixture`).toBeNull();
      expect(cited(readable(body))).toEqual([]);
    });
  }
});

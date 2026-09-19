/**
 * No surface shows a person a developer's citation.
 *
 * The owner's ruling of 2026-09-19: a mock carries the screen and notes for the
 * developer, and the notes are never built as screen. The shapes and the reader
 * are the shared list every application imports — `scripts/developer-content.ts`
 * (GG, `0a4e4e1a`), which this check's own patterns were merged into (the wider
 * ADR spelling and `aria-description`); GuestOps kept a private copy until it
 * landed, then switched, as the architect ruled.
 *
 * **Rendered, not read from source**: a comment citing a ruling is a record and
 * is right; only what reaches a person is a claim to them. The walk covers the
 * harness's fixtures as well as what the service sends — a fixture is what every
 * capture shows the owner.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { describe, expect, it } from "vitest";

import { developerContent, readableText } from "../../../scripts/developer-content";
import { mountWidget, SCREENS, surfaceHost, WIDGETS } from "./surfaces";

describe("developer citations on a surface", () => {
  it("are found — the positive control, against the shared list", () => {
    expect(developerContent("Jobs' board (design §6)")).toContain("a section sign: §");
    expect(developerContent("as ADR 0106 requires")).toEqual(["an ADR: ADR 0106"]);
    expect(developerContent("accepted with GUEST-Q6")).toEqual(["a decision-register id: GUEST-Q6"]);
    expect(developerContent("Check in · 48 h of arrival · ₹ 8 400.00 · BK-4471")).toEqual([]);
  });

  for (const [name, draw] of SCREENS) {
    it(`${name} shows none`, async () => {
      const main = document.createElement("div");
      await draw(surfaceHost(), main);

      expect(main.querySelector(".fail"), `${name} could not read its fixture`).toBeNull();
      expect(developerContent(readableText(main))).toEqual([]);
    });
  }

  for (const name of WIDGETS) {
    it(`the ${name} widget shows none`, async () => {
      const body = await mountWidget(name);

      expect(body.querySelector(".wx"), `${name} could not read its fixture`).toBeNull();
      expect(developerContent(readableText(body))).toEqual([]);
    });
  }
});

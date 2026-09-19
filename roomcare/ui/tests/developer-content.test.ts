import { describe, expect, it } from "vitest";

import { developerContent, readableText } from "../../../scripts/developer-content";
import { arrivalsWaiting, attendantsNow, attention, pendingPolicy, roomsReady } from "../widgets/panel/panels";
import { host } from "./host";
import { PLACES, pressable, reach } from "./places";

/**
 * No developer note reaches a person at the property (owner ruling, 2026-09-19): no register id, ADR, design
 * section, code identifier or correlation id in what Room Care renders. The patterns are the shared list
 * (`scripts/developer-content.ts`, extending Workforce's b001bfac); comments citing rulings are records and are
 * not read, because only what reaches the screen is a claim to staff.
 *
 * Every place is read as it opens, and again after each of its controls is pressed, so a sheet, a room's page
 * and a confirmation are read too — the walk reaches whatever a person can reach by pressing once.
 */
describe("developer content", () => {
  for (const [place, capabilities, section, tab, into] of PLACES) {
    it(`none on ${place}, or on anything one press away`, async () => {
      const found = new Set<string>();
      const first = await reach(capabilities, section, tab, [], into);
      for (const hit of developerContent(readableText(first))) found.add(`as it opens — ${hit}`);
      const count = pressable(first).length;
      for (let i = 0; i < count; i += 1) {
        const root = await reach(capabilities, section, tab, [], into);
        const button = pressable(root)[i];
        if (button === undefined) continue;
        const label = button.textContent?.trim() ?? "";
        button.click();
        await new Promise((done) => setTimeout(done, 0));
        await new Promise((done) => setTimeout(done, 0));
        for (const hit of developerContent(readableText(root))) found.add(`after "${label}" — ${hit}`);
      }
      expect([...found], place).toEqual([]);
    }, 60_000);
  }

  it("none on the five widgets", async () => {
    const h = host(["roomcare.read"]);
    const cards = await Promise.all([roomsReady(h), arrivalsWaiting(h), attention(h), attendantsNow(h), pendingPolicy(h)]);
    expect(cards.flatMap((card) => developerContent(readableText(card)))).toEqual([]);
  });

  it("finds each shape it names — a positive control, so a clean walk is not a blind one", () => {
    const planted = "WF-Q18 · ADR 0044 · design §6 · (S5 c4) · (row 7) · Chapter 21 · roomcare_manager · a correlation id · Master Data · 0192f100-0000-7000-8000-000000000003 · 2026-09-19T08:30";
    expect(developerContent(planted).map((hit) => hit.split(": ")[0])).toEqual([
      "a decision-register id", "an ADR", "a section sign", "a design-section reference", "a design-row reference",
      "a chapter reference", "a code identifier", "a correlation id", "a platform system", "a raw id", "an unformatted instant",
    ]);
  });
});

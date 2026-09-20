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
/**
 * A backend's code printed as it came, with its underscore: IN_PROGRESS, DND_APPROVED. HH (Jobs, 2026-09-19): screens
 * printed backend values as sent while every guard passed. Only the underscore form is refused: capitalised words
 * such as IN PROGRESS, DONE, DIRTY and CLEAN are drawn that way in the approved frames, and whether that is a style
 * or a code left in is the owner's to decide (queued, drawn both ways), not this test's.
 */
function codes(text: string): string[] {
  return [...text.matchAll(/\b[A-Z][A-Z0-9]*(?:_[A-Z0-9]+)+\b/g)].map((m) => m[0])
    .map((word) => `a backend's code: ${word}`);
}
/**
 * An event or capability name, `room.cleaned`, `roomcare.assign`, in text a backend composed (Room Care's timeline
 * said "room CLEAN, announced (room.cleaned)"). Kept here, not yet in the shared list: there it turns Jobs red on a
 * `job.read` Jobs has to clear first, the same order the system names took. A file's name (`gauge.jpg`) is not one.
 */
function names(text: string): string[] {
  return [...text.matchAll(/\b[a-z]{2,}\.(?!(?:jpe?g|png|gif|webp|pdf|heic)\b)[a-z]{2,}(?:[._][a-z]+)*\b/g)]
    .map((m) => `an event or capability name: ${m[0]}`);
}
const read = (root: Element): string[] => [...developerContent(readableText(root)), ...codes(readableText(root)), ...names(readableText(root))];

describe("developer content", () => {
  for (const [place, capabilities, section, tab, into] of PLACES) {
    it(`none on ${place}, or on anything one press away`, async () => {
      const found = new Set<string>();
      const first = await reach(capabilities, section, tab, [], into);
      for (const hit of read(first)) found.add(`as it opens — ${hit}`);
      const count = pressable(first).length;
      for (let i = 0; i < count; i += 1) {
        const root = await reach(capabilities, section, tab, [], into);
        const button = pressable(root)[i];
        if (button === undefined) continue;
        const label = button.textContent?.trim() ?? "";
        button.click();
        await new Promise((done) => setTimeout(done, 0));
        await new Promise((done) => setTimeout(done, 0));
        for (const hit of read(root)) found.add(`after "${label}" — ${hit}`);
      }
      expect(found.size, `${place}:\n${[...found].join("\n")}`).toBe(0);
    }, 60_000);
  }

  it("none on the five widgets", async () => {
    const h = host(["roomcare.read"]);
    const cards = await Promise.all([roomsReady(h), arrivalsWaiting(h), attention(h), attendantsNow(h), pendingPolicy(h)]);
    expect(cards.flatMap((card) => read(card))).toEqual([]);
  });

  // The two patterns above are Room Care's own, and both are the shape that fails silently: a lost `\b` leaves a
  // regex that matches nothing and a walk that passes everything (this file's own code check lost its boundaries
  // twice while being written, and HH hit the same in Jobs' guards, 2026-09-20). Planted through `read`, so the
  // reading path is proved too, not only the patterns.
  it("finds a backend's code and an event name in what a screen renders", () => {
    const rendered = document.createElement("div");
    rendered.innerHTML = '<p>state <b>IN_PROGRESS</b></p><p>room CLEAN, announced (room.cleaned)</p><img src="gauge.jpg" alt="gauge.jpg">';
    expect(read(rendered)).toEqual(["a backend's code: IN_PROGRESS", "an event or capability name: room.cleaned"]);
  });

  it("finds each shape it names — a positive control, so a clean walk is not a blind one", () => {
    const planted = "WF-Q18 · ADR 0044 · design §6 · (S5 c4) · (row 7) · Chapter 21 · roomcare_manager · a correlation id · Master Data · 0192f100-0000-7000-8000-000000000003 · 2026-09-19T08:30";
    expect(developerContent(planted).map((hit) => hit.split(": ")[0])).toEqual([
      "a decision-register id", "an ADR", "a section sign", "a design-section reference", "a design-row reference",
      "a chapter reference", "a code identifier", "a correlation id", "a platform system", "a raw id", "an unformatted instant",
    ]);
  });
});

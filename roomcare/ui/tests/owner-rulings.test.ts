import { describe, expect, it } from "vitest";

import { readableText } from "../../../scripts/developer-content";
import { SUPERVISOR, ATTENDANT, click, settle } from "./host";
import { PLACES, reach } from "./places";

/**
 * ADR 0229 (RC-Q9) — the owner's answers to 01d's ten questions, 2026-09-23. Seven went the way 01d recommended;
 * three are ruled AS BUILT and are not to be revisited (saying "PMS", the bulk change with a filter on, and
 * returning from a room by its section). Of the seven, one — naming the other applications — is what the build
 * already did, so six are changes and this file holds them.
 *
 * Each is checked on the screens a person actually reaches, through the shared walk.
 */

/**
 * A word a person would read as a code rather than as a word: three or more capital LETTERS. A room is called G01
 * and a hotel says DND and PMS, so neither of those is shouting.
 */
const SHOUTED = /\b(?!DND\b|PMS\b)[A-Z]{3,}\b/g;

async function everywhere(): Promise<string> {
  const read: string[] = [];
  for (const [, capabilities, section, tab, into] of PLACES) {
    const root = await reach(capabilities, section, tab, [], into);
    read.push(readableText(root));
    for (const button of root.querySelectorAll<HTMLButtonElement>(".body button")) {
      if (button.hasAttribute("disabled")) continue;
      button.click();
      await settle();
      read.push(readableText(root));
      break;
    }
  }
  return read.join("\n");
}

describe("06 · the same words, read as words", () => {
  it("shouts no state at anybody — every screen, and what one press opens", async () => {
    const shouted = [...new Set([...(await everywhere()).matchAll(SHOUTED)].map((m) => m[0]))];
    expect(shouted, "states are words, not codes; DND and PMS are the hotel's own").toEqual([]);
  }, 120_000);

  it("says a room's state in words on an attendant's list", async () => {
    const root = await reach(ATTENDANT, "My rooms", null, []);
    const text = root.querySelector(".body")?.textContent ?? "";
    expect(text).toMatch(/In progress|Done|Ready|Partial|Declined|To do|Waiting/u);
    expect(text).not.toMatch(/IN PROGRESS|DONE|READY|PARTIAL|DECLINED/u);
  });
});

describe("03 · no column that never answers", () => {
  it("drops Posting from property-wide access", async () => {
    const root = await reach(SUPERVISOR, "Setup", "Property-wide access", []);
    const heads = [...root.querySelectorAll(".body th")].map((h) => h.textContent);
    expect(heads).not.toContain("Posting (Workforce)");
    expect(heads).toContain("Person");
  });

  it("drops Typical length from the deep-clean plan", async () => {
    const root = await reach(SUPERVISOR, "Setup", "Deep clean plan", []);
    const heads = [...root.querySelectorAll(".body th")].map((h) => h.textContent);
    expect(heads).not.toContain("Typical length");
    expect(heads).toContain("Room type");
  });
});

describe("04 · a reason they can read", () => {
  it("says the photo is not available yet, and names no service", async () => {
    const root = await reach(ATTENDANT, "My rooms", null, [], ".body tr.pick button.opener");
    const photo = [...root.querySelectorAll(".body .btn.off")].find((b) => b.textContent?.startsWith("Photo"));
    expect(photo?.textContent).toBe("Photo — not available yet");
  });
});

describe("05 · one sentence, in their terms", () => {
  it("tells a supervisor what follows their entry, not how it is stored", async () => {
    const root = await reach(SUPERVISOR, "Board", null, [], ".body button.tile");
    click(root, ".body button", "Room state…");
    await settle();
    const note = root.querySelector(".scrim p")?.textContent ?? "";
    expect(note).toContain("Recorded by hand");
    expect(note).not.toMatch(/source manual|disagreement flag/u);
  });

  it("says who a room can go to, without explaining another application's gaps", async () => {
    const root = await reach(SUPERVISOR, "Board", null, [], ".body button.tile");
    click(root, ".body button", "Reassign…");
    await settle();
    const said = readableText(root.querySelector(".scrim")!);
    expect(said).not.toMatch(/Workforce's to add|are Workforce's/u);
  });
});

describe("07 · the whole house, every time", () => {
  // The owner ruled this on what the drawing showed: next morning, four rooms had arrived overnight and a
  // remembered filter hid them.
  it("forgets the filter when the person leaves, so nothing that arrived is hidden", async () => {
    const root = await reach(SUPERVISOR, "Room states", null, []);
    click(root, ".chips button", "Dirty");
    await settle();
    expect(root.querySelector(".body")?.textContent, "the filter took effect").toContain("Dirty");
    click(root, ".head .tab", "Board");
    await settle();
    click(root, ".head .tab", "Room states");
    await settle();
    const chips = [...root.querySelectorAll<HTMLButtonElement>(".chips button")];
    const on = chips.filter((c) => c.getAttribute("aria-pressed") === "true").map((c) => c.textContent);
    expect(on, "back on the whole house").toEqual(["All"]);
  });
});

describe("08 · the same start every time", () => {
  // A remembered setting is a setting nobody chose today; the safe side of the binary is what the service does
  // when nothing is set.
  it("forgets what the tap grid was setting", async () => {
    const root = await reach(SUPERVISOR, "Room states", null, []);
    click(root, ".strip button", "Tap grid");
    await settle();
    click(root, ".segs button", "inspected");
    await settle();
    click(root, ".head .tab", "Board");
    await settle();
    click(root, ".head .tab", "Room states");
    await settle();
    click(root, ".strip button", "Tap grid");
    await settle();
    const chosen = [...root.querySelectorAll<HTMLButtonElement>(".segs button")]
      .filter((c) => c.classList.contains("on")).map((c) => c.textContent);
    expect(chosen, "a tap sets dirty again, as it does on a first visit").toContain("dirty");
  });
});

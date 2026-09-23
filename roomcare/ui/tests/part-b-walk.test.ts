import { describe, expect, it } from "vitest";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

import { sectionsFor } from "../application";
import { TABS } from "../screens/setup";
import { host } from "./host";
import { PLACES, reach } from "./places";

/**
 * The Part B walk names every place and every door the product draws — `docs/part-b-walk.md`.
 *
 * The walk is the owner's, pressed by hand at a native window this session cannot reach, so the one failure mode
 * that matters is a screen gaining a control and the walk not gaining a row. That cannot be caught by reading the
 * document; it is caught by asking the product what it draws and looking for each answer in the text.
 *
 * **Only structural labels are checked.** A room's number and a person's name are the fixture's, and on the owner's
 * property they are different — the walk says "any room tile" on purpose. What must be named is what is the same on
 * every property: the sections, Setup's tabs, and every control that opens a dialog, which is where a write lives.
 */
const WALK = readFileSync(resolve(__dirname, "../../docs/part-b-walk.md"), "utf8");

/** A control that opens something: the product's own convention is a trailing ellipsis. */
const OPENS = /…$/u;

describe("the Part B walk", () => {
  it("names every section a person can hold", () => {
    const everyCapability = ["roomcare.read", "roomcare.assign", "roomcare.amend", "roomcare.plan", "roomcare.configure"];
    for (const section of sectionsFor(host(everyCapability))) {
      expect(WALK, `the walk does not name the ${section} section`).toContain(section);
    }
    expect(WALK, "and the attendant's one").toContain("My rooms");
  });

  it("names every Setup tab", () => {
    for (const tab of TABS) expect(WALK, `the walk does not name Setup's ${tab} tab`).toContain(tab);
  });

  it("names every control that opens a dialog", async () => {
    const missing: string[] = [];
    for (const [name, capabilities, section, tab, into] of PLACES) {
      const root = await reach(capabilities, section, tab, [], into);
      for (const button of root.querySelectorAll<HTMLButtonElement>("button")) {
        const label = (button.textContent ?? "").trim();
        if (!OPENS.test(label) || WALK.includes(label)) continue;
        missing.push(`${name}: ${label}`);
      }
    }
    expect([...new Set(missing)], "a door the walk does not open — add a row for it").toEqual([]);
  }, 120_000);
});

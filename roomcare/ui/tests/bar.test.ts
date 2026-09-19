import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { SUPERVISOR, host, mount, recorded, settle } from "./host";

/**
 * The bar names who is signed in as name · department · property (page 64 §3). Master Data allows a staff
 * record with no display name (HH, 2026-09-19). The bar then says so, where the name would be, and never
 * draws a placeholder name, nor quietly drops the clause so that two clauses pass for three.
 */
describe("the bar, for a person Master Data holds no name for", () => {
  const me = recorded<Record<string, unknown>>("me");

  for (const [what, name] of [["no display name", null], ["an empty one", ""], ["a blank one", "  "]] as const) {
    it(`says the name is missing when the record has ${what}`, async () => {
      const root = mount(activate, host(SUPERVISOR, { me: { ...me, name } }));
      await settle();
      expect(root.querySelector(".who")?.textContent).toBe("no name in Master Data · Housekeeping · Coral Cove Resort");
      expect(root.querySelector(".who .unnamed")?.textContent).toBe("no name in Master Data");
    });
  }

  it("names the person as before when the record has a name", async () => {
    const root = mount(activate, host(SUPERVISOR));
    await settle();
    expect(root.querySelector(".who")?.textContent).toBe("Meera Krishnan · Housekeeping · Coral Cove Resort");
    expect(root.querySelector(".who .unnamed")).toBeNull();
  });
});

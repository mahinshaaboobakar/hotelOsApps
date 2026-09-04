import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedFirstRun, recordedPeople } from "../roster/people";

/**
 * The one list in this module that pages.
 *
 * These assert the two things a pager gets wrong in ways nothing else catches:
 * that its numbers are the **server's** rather than the screen's, and that it
 * is **absent** over a list that fits rather than a disabled row of one.
 *
 * The arithmetic itself is not re-tested here — it is `@hotelos/sdk`'s
 * `pagedView`, shared with the desktop's React pager and tested there. Testing
 * it again from this side would assert that a copy exists.
 */

/**
 * A host that answers ONE method and refuses the rest.
 *
 * The module mounts on Rota before anybody reaches People, so a double that
 * answers every call with the same payload hands Rota a page of postings and
 * the screen throws inside a promise nothing is awaiting — five unhandled
 * rejections beside a green run, which is the shape this repository already
 * has a rule about. Refusing by method is also what the platform does: an
 * ungranted or unimplemented call comes back as a `HostCallError`, and `load`
 * falls back to the recorded fixture, so every other screen draws its own.
 */
function host(answer: unknown): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: null },
    call: (_capability: string, method: string) => method === "people"
      ? Promise.resolve(answer)
      : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" })),
    on: () => () => {},
  };
}

async function settle(): Promise<void> {
  for (let turn = 0; turn < 8; turn += 1) {
    await new Promise((resolve) => { setTimeout(resolve, 1); });
  }
}

async function people(answer: unknown): Promise<HTMLElement> {
  const root = document.createElement("div");
  activate(host(answer)).mount(root);
  await settle();

  const section = Array.from(root.querySelectorAll<HTMLElement>(".head .tab"))
    .find((one) => one.textContent?.includes("People") === true);
  section?.click();
  await settle();

  return root;
}

describe("the People pager", () => {
  it("draws the range from the server's own numbers", async () => {
    const root = await people(recordedPeople);

    // 1–25 of 42: the range is the SIZE APPLIED and the total the server
    // counted, never the length of the array in front of it. A pager numbered
    // from the rows on screen would say 1–25 of 25 under a property of 42.
    expect(root.querySelector(".pager .showing")?.textContent)
      .toBe("Showing 1–25 of 42");
  });

  it("offers every page, with the current one marked", async () => {
    const root = await people(recordedPeople);

    const numbers = Array.from(root.querySelectorAll<HTMLElement>(".pager .pg"))
      .map((one) => one.textContent);

    // Two pages of twenty-five, between the two arrows.
    expect(numbers).toEqual(["‹", "1", "2", "›"]);
    expect(root.querySelector(".pager .pg.on")?.textContent).toBe("1");
    expect(root.querySelector(".pager .pg.on")?.getAttribute("aria-current")).toBe("page");
  });

  it("disables the arrow that has nowhere to go", async () => {
    const root = await people(recordedPeople);
    const arrows = Array.from(root.querySelectorAll<HTMLElement>(".pager .pg"))
      .filter((one) => one.hasAttribute("aria-label"));

    // Present and dimmed rather than removed: a row whose controls move as you
    // page is a row you have to re-find on every click.
    expect(arrows.map((one) => one.hasAttribute("disabled"))).toEqual([true, false]);
  });

  it("is absent over a list that fits", async () => {
    const root = await people(recordedFirstRun);

    // Not a disabled row of one under an empty state promising pages of
    // nothing. The screen draws its first run and the pager draws nothing.
    expect(root.querySelector(".pager")).toBeNull();
    expect(root.querySelector(".first")).not.toBeNull();
  });

  it("counts the property in the sub-line, not the page", async () => {
    const root = await people(recordedPeople);

    // The subtitle sits above a list that pages, so a count of the rows in
    // front of you says something false about the property the moment somebody
    // turns to page two.
    expect(root.querySelector(".hsub")?.textContent).toContain("42 posted");
  });
});

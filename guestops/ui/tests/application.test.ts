/**
 * What a screenshot cannot assert cheaply, and what a screenshot found.
 *
 * The capture harness is the rendering guard — a suite cannot see layout or
 * colour. These tests cover the other half: the two defects the captures
 * exposed, so neither can come back silently, and the rules the design rests on
 * that are checkable as structure.
 *
 * Tests live here rather than beside the source: ADR 0025, and TypeScript is
 * explicitly not an exception to it.
 */

import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedAttention, recordedStay, recordedToday } from "../book";
import type { HostApi } from "@hotelos/sdk";

const GRANTED = ["reservation.read", "stay.override", "registration.capture", "request.handle"];

/** A host that answers from the recorded facts, granting what is asked for. */
function host(granted: readonly string[] = GRANTED): HostApi {
  return {
    identity: { id: "guestops", version: "0.1.0", capabilities: granted },
    call: (_capability, method) => {
      // Each method answers with its own shape. A double that returned one
      // shape for every method would make the module crash on a screen the
      // test never meant to exercise — which is what this one did first.
      if (method === "attention") return Promise.resolve(recordedAttention);
      if (method === "stay") return Promise.resolve(recordedStay);
      return Promise.resolve(recordedToday);
    },
    on: () => () => {},
  };
}

/** Mount the module and let its first screen resolve. */
async function mount(granted?: readonly string[]): Promise<HTMLElement> {
  const root = document.createElement("div");
  document.body.append(root);

  activate(host(granted)).mount(root);
  await new Promise((resolve) => setTimeout(resolve, 0));
  return root;
}

describe("the module's own stylesheet", () => {
  /**
   * The defect the first capture found: `mount` appended the style element and
   * the first `show()` removed it with `replaceChildren`, so the module drew as
   * an unstyled column. The type-check and the backend suite were both green.
   */
  it("survives the first render", async () => {
    const root = await mount();
    expect(root.querySelector("style")).not.toBeNull();
  });

  it("survives a screen change", async () => {
    const root = await mount();

    const attention = [...root.querySelectorAll<HTMLElement>(".ri")]
      .find((item) => item.textContent?.includes("Attention") === true);

    attention?.click();
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(root.querySelector("style")).not.toBeNull();
  });

  /**
   * The token contract has its own guard now — `tokens.test.ts`.
   *
   * This assertion used to live here and **encoded a superseded contract**
   * (ADR 0034): it asserted the stylesheet said `--r-md`, on the belief that
   * `--r-md` was the published radius. It is not published either — the
   * contract publishes `radius-panel` — so the test was pinning one unpublished
   * name in place of another and passing while the module was styled by
   * nobody. Replaced by a guard derived from `TOKEN_NAMES` rather than from a
   * name somebody believed in.
   */
  it("reaches the stylesheet the token guard checks", async () => {
    const root = await mount();
    expect(root.querySelector("style")?.textContent?.length ?? 0).toBeGreaterThan(2000);
  });
});

describe("the day's table", () => {
  /**
   * The design's rule: an empty room is an **action**, not a state. Six of
   * fourteen arrivals having no room is ordinary, and the list is built to be
   * worked in that state.
   */
  it("offers an assign action where a stay has no room", async () => {
    const root = await mount();
    const actions = [...root.querySelectorAll("button.link")]
      .filter((button) => button.textContent?.includes("assign") === true);

    const roomless = recordedToday.lists[0]?.rows.filter((row) => row.room === null) ?? [];
    expect(actions).toHaveLength(roomless.length);
    expect(roomless.length).toBeGreaterThan(0);
  });

  it("draws a header row with the design's columns", async () => {
    const root = await mount();
    const head = root.querySelector(".tr.hd");

    expect(head?.textContent).toContain("Guest");
    expect(head?.textContent).toContain("Booking");
    expect(head?.textContent).toContain("Nights");
  });

  /** A party member with no name yet is a real row, drawn italic. */
  it("keeps the unnamed row and marks it", async () => {
    const root = await mount();
    expect(root.querySelector(".nm b.un")?.textContent).toBe("Not yet named");
  });
});

describe("the rail", () => {
  /**
   * Gold frames 1 and 12 disagree about the Attention count — 2 and 4. One
   * running screen cannot hold both, so the count is derived from the list and
   * the rail cannot claim a number the screen does not show.
   */
  it("counts attention from the list itself", async () => {
    const root = await mount();

    const count = [...root.querySelectorAll<HTMLElement>(".ri")]
      .find((item) => item.textContent?.includes("Attention") === true)
      ?.querySelector(".cnt")?.textContent;

    expect(count).toBe(String(recordedAttention.length));
  });

  /** A stay is reached from the day and belongs to it — no back button. */
  it("keeps Today lit while a stay is open, and offers no back control", async () => {
    const root = await mount();

    root.querySelector<HTMLElement>(".tr.act")?.click();
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(root.querySelector(".ri.on")?.textContent).toContain("Today");
    expect(root.textContent).not.toContain("← Today");
  });
});

describe("fallback honesty", () => {
  it("says so when it is not reading the property's own data", async () => {
    const root = await mount([]);
    expect(root.querySelector(".stand")).not.toBeNull();
  });

  it("says nothing when the data is the property's", async () => {
    const root = await mount();
    expect(root.querySelector(".stand")).toBeNull();
  });
});

describe("the marks", () => {
  /** `missing` is an absence: a dashed outline, and deliberately no dot. */
  it("gives every mark a dot except missing", async () => {
    const root = await mount();

    for (const chip of root.querySelectorAll(".sh")) {
      const dot = chip.querySelector("i");
      expect(dot === null).toBe(chip.classList.contains("missing"));
    }
  });
});

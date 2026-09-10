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

    // The host tells a module its property's zone and locale. Both are `null`
    // here on purpose: the SDK types them nullable because a property that has
    // not been configured is a real state, and a double that invented
    // "Asia/Kolkata" would hide every place this module forgets to handle it.
    property: { timezone: null, locale: null },
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

    const attention = [...root.querySelectorAll<HTMLElement>(".head .tab")]
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

describe("the app bar", () => {
  /**
   * **Rewritten, not deleted — ADR 0034.** It asserted that Attention's count
   * was derived from the recorded list, which was the best available answer
   * while the bar read fixtures. It is the wrong contract now: nothing
   * establishes any of these counts, so drawing one derived from a fixture is
   * the same claim the frames' hardcoded `218` was, one indirection along.
   *
   * What it guards instead is the rule that replaced it: **every count is the
   * dash, and no count is a figure.** A `0` would say the hotel is empty and a
   * number would say somebody counted.
   */
  it("claims no count it cannot establish", async () => {
    const root = await mount();

    const counts = [...root.querySelectorAll<HTMLElement>(".head .tab .n")]
      .map((n) => n.textContent);

    expect(counts.length).toBeGreaterThan(0);
    expect(counts.every((c) => c === "—")).toBe(true);
  });

  /** The person is not in the host contract, so the bar says so — SHELL-Q52. */
  it("says the operator is not established rather than naming one", async () => {
    const root = await mount();
    const who = root.querySelector<HTMLElement>(".head .who");

    expect(who?.textContent).toBe("operator not established");
    expect(root.textContent).not.toContain("Anitha Menon");
  });

  /** A stay is reached from the day and belongs to it — no back button. */
  it("keeps Today lit while a stay is open, and offers no back control", async () => {
    const root = await mount();

    root.querySelector<HTMLElement>(".tr.act")?.click();
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(root.querySelector(".head .tab.on")?.textContent).toContain("Today");
    expect(root.textContent).not.toContain("← Today");
  });
});

/**
 * **Rewritten, not deleted — ADR 0034.** This was "fallback honesty", and it
 * asserted that a screen reading nothing drew a banner (`.stand`) above the
 * recorded facts, and no banner when the data was real. That was the honest
 * form of the wrong mechanism: whoever read the banner knew, and whoever read
 * the list of names did not — and the list is what a person at a desk reads.
 *
 * `APPS-Q42` ruled the fallback out, not the banner. So the contract now is
 * that a read which does not answer renders a **failure** and no data at all,
 * and the test that guarded the banner guards its absence.
 */
describe("a read that did not answer", () => {
  it("renders the failure and none of the data", async () => {
    const root = await mount([]);

    expect(root.querySelector(".fail")).not.toBeNull();
    expect(root.querySelector(".stand")).toBeNull();

    // The recorded book's own names must not be anywhere on a failed screen.
    // Asserting the failure exists would pass with the list still beneath it,
    // which is the shape this ruling removed.
    expect(root.querySelector(".tbl")).toBeNull();
    expect(root.textContent).not.toContain("Anand Menon");
  });

  it("draws no failure when the platform answered", async () => {
    const root = await mount();

    expect(root.querySelector(".fail")).toBeNull();
    expect(root.querySelector(".tbl")).not.toBeNull();
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

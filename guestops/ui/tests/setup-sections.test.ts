/**
 * Setup's tab strip, rendered — ADR 0378, the owner's ruling of 2026-10-04.
 *
 * > *"A surface element that is drawn and undesigned is drawn DISABLED with a
 * > reason stating what is absent. Not removed, not enabled, not promised."*
 *
 * **Written because changing the strip from three sections to five broke no
 * test.** The service sent `Registration · Card series · Reporting` while the
 * approved page draws `Registration · Guest reporting · Stop-sell · Stay
 * defaults`, and `fV2` adds `Reasons` — and nothing in either suite could see
 * the difference, which is how a strip no property would render survived in the
 * harness.
 *
 * **These drive the SCREEN rather than the fixture.** A test asserting the
 * recorded sections would be a claim about a claim: the fixture says what the
 * service sends, and only rendering says what an operator meets. The wire's own
 * half is asserted in the backend suite, where the service is.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { describe, expect, it } from "vitest";

import type { HostApi } from "@hotelos/sdk";

import type { Setup } from "../book";
import { recordedSetup } from "../book/recorded";
import { setup } from "../screens/setup";

const REASON = "This tab holds no settings. Nothing about a stay is configured from here.";

function host(sections: Setup["sections"]): HostApi {
  return {
    identity: { id: "guestops", version: "0.1.0", capabilities: ["desk.configure"] },
    property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    call: (_capability: string, method: string) =>
      method === "setup"
        ? Promise.resolve({ ...recordedSetup, sections })
        : Promise.reject(new Error(`no answer for ${method}`)),
    on: () => () => {},
  } as unknown as HostApi;
}

async function strip(sections: Setup["sections"]): Promise<HTMLButtonElement[]> {
  const into = document.createElement("div");
  await setup(host(sections), into, "Registration", () => {});

  // Filtered on the class rather than selected by it: the active tab is
  // `tab on`, which is two classes, and a selector naming it is where the first
  // attempt produced an invalid-selector DOMException rather than a result.
  return [...into.querySelectorAll<HTMLButtonElement>("button")]
    .filter((one) => one.className.split(" ")[0] === "tab");
}

const LIVE: Setup["sections"] = [
  { label: "Registration", on: true },
  { label: "Guest reporting", on: false },
];

const WITH_DISABLED: Setup["sections"] = [
  ...LIVE,
  { label: "Stay defaults", on: false, reason: REASON },
];

describe("Setup's tab strip", () => {
  it("draws every section the service sends", async () => {
    const tabs = await strip(WITH_DISABLED);

    expect(tabs.map((one) => one.textContent)).toEqual(
      ["Registration", "Guest reporting", "Stay defaults"]);
  });

  it("draws a section carrying a reason DISABLED, with that reason on it", async () => {
    // ADR 0378's subject. The owner's own answer when asked what the tab was for
    // was "i dont know for what this tab" — so it is neither removed nor enabled.
    const tabs = await strip(WITH_DISABLED);
    const undesigned = tabs.find((one) => one.textContent === "Stay defaults");

    expect(undesigned?.disabled).toBe(true);
    expect(undesigned?.title).toBe(REASON);
  });

  it("leaves a section with no reason live", async () => {
    // **The discriminator.** A strip that disabled everything would pass the test
    // above, and a working tab must not have to declare that nothing is wrong
    // with it — which is what makes the field optional rather than empty.
    const tabs = await strip(WITH_DISABLED);

    expect(tabs
      .filter((one) => !one.disabled)
      .map((one) => one.textContent)).toEqual(["Registration", "Guest reporting"]);
  });

  it("draws nothing disabled when every section has content", async () => {
    const tabs = await strip(LIVE);

    expect(tabs.some((one) => one.disabled)).toBe(false);
  });

  /**
   * The reason states what is ABSENT and promises nothing.
   *
   * "Coming soon" would satisfy the ruling's letter and break it: copy that
   * promises an outcome outruns the code that must deliver it, and nothing here
   * owes a mechanism.
   */
  it("promises nothing about what is absent", () => {
    const reasons = recordedSetup.sections
      .map((one) => one.reason)
      .filter((one): one is string => one !== undefined);

    expect(reasons).not.toHaveLength(0);

    for (const reason of reasons) {
      for (const promise of ["coming", "soon", "will be", "shortly", "planned"]) {
        expect(reason.toLowerCase()).not.toContain(promise);
      }
    }
  });
});

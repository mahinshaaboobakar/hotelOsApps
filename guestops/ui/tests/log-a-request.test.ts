/**
 * C7 — logging a guest's request, gold frames 5 and 5b.
 *
 * **The control drew and did nothing since the tab existed.** The service has
 * recorded requests since it was written and the module served no method that
 * reached it, so `＋ Log a request` was the ledger's oldest *looks live, does
 * nothing*.
 *
 * **Frame 5b is what these tests exist for.** The request is GuestOps' own
 * record whether or not Jobs is installed — *"what disappears is the raising,
 * not the guest's complaint"* — so logging is offered on a property with no
 * Jobs, and only the hand-off is conditional. A request screen that needed
 * Jobs would break on every property that never bought it.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { describe, expect, it } from "vitest";

import type { PropertyEnvironment } from "@hotelos/sdk";

import type { Requests } from "../book/model";
import { recordedRequests } from "../book/recorded/tabs";
import { requestsTab } from "../screens/stay/requests-tab";

const KOLKATA: PropertyEnvironment = { locale: "en-IN", timezone: "Asia/Kolkata" };

const drawn = (
  requests: Requests,
  log?: (text: string, handOff: boolean) => void,
): HTMLElement => {
  const into = document.createElement("div");
  into.append(...requestsTab(requests, KOLKATA, log));
  return into;
};

const controls = (root: HTMLElement): string[] =>
  [...root.querySelectorAll("button")].map((b) => b.textContent ?? "");

describe("logging a request", () => {
  it("offers Log, and hands the text and the hand-off to the caller", () => {
    const asked: [string, boolean][] = [];
    const tab = drawn(recordedRequests, (text, handOff) => asked.push([text, handOff]));

    const box = tab.querySelector("input");
    if (box === null) throw new Error("the tab offers nowhere to type a request");

    box.value = "Late checkout on 4 Sep";
    [...tab.querySelectorAll("button")].find((b) => b.textContent === "Log")?.click();

    expect(asked).toEqual([["Late checkout on 4 Sep", false]]);
  });

  it("asks for work only where the desk said so", () => {
    const asked: [string, boolean][] = [];
    const tab = drawn(recordedRequests, (text, handOff) => asked.push([text, handOff]));

    const box = tab.querySelector("input");
    if (box !== null) box.value = "AC not cooling";

    [...tab.querySelectorAll("button")]
      .find((b) => b.textContent === "Log and raise a job")?.click();

    expect(asked).toEqual([["AC not cooling", true]]);
  });

  it("records nothing for an empty box, rather than an empty request", () => {
    const asked: [string, boolean][] = [];
    const tab = drawn(recordedRequests, (text, handOff) => asked.push([text, handOff]));

    [...tab.querySelectorAll("button")].find((b) => b.textContent === "Log")?.click();

    expect(asked).toEqual([]);
  });

  // **Frame 5b.** The property never bought Jobs; the guest still complains.
  it("still logs where Jobs is not installed, and offers no hand-off", () => {
    const asked: [string, boolean][] = [];
    const tab = drawn(
      { ...recordedRequests, jobsInstalled: false, jobs: null },
      (text, handOff) => asked.push([text, handOff]));

    expect(controls(tab)).toContain("Log");
    expect(controls(tab)).not.toContain("Log and raise a job");

    const box = tab.querySelector("input");
    if (box !== null) box.value = "Extra towels";

    [...tab.querySelectorAll("button")].find((b) => b.textContent === "Log")?.click();

    expect(asked).toEqual([["Extra towels", false]]);
  });

  // The old state, kept as a test so the screen cannot silently return to it:
  // a tab given no way to log says so rather than drawing a control that lies.
  it("says it cannot log where no handler is given", () => {
    const tab = drawn(recordedRequests);

    expect(tab.textContent ?? "").toContain("not available yet");
    expect(tab.querySelector("input")).toBeNull();
  });
});

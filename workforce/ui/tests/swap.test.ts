import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { recordedLeave, type LeaveBoard, type SwapDetail } from "../roster/leave";
import { recordedPeople } from "../roster/people";
import { leave } from "../screens/leave";
import { swapCard } from "../screens/leave/approvals";

/**
 * The Approvals tab's swap pane, against what the service actually sends.
 *
 * `LeaveView.Board` answers `swap = null` on every call — no proposal is open
 * until somebody picks one, and nothing on the wire picks one yet. The screen
 * was typed as though a swap always arrived, and only the fixture ever sent
 * one, so the tab had never rendered against the wire at all.
 */

function host(board: unknown): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (_capability: string, method: string) => method === "leave"
      ? Promise.resolve(board)
      : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" })),
    on: () => () => {},
  };
}

describe("the swap pane", () => {
  it("draws the tab when the service sends no open swap, which is every time", async () => {
    const wire = { ...recordedLeave, swap: null } as unknown as LeaveBoard;
    const main = document.createElement("div");

    await leave(host(wire), main, "Approvals", () => {});

    expect(main.querySelector(".fail")).toBeNull();
    expect(main.querySelector(".swap")).toBeNull();
    expect(main.textContent).toContain("No swap is open.");
  });

  it("names only the two people in the swap", () => {
    // Two people no fixture carries, so a name drawn from anywhere else shows.
    const detail: SwapDetail = {
      on: "2026-10-08",
      proposer: "Fatima Noor", colleague: "Irfan Qadri",
      proposerWhere: "Receptionist · Zone 4", colleagueWhere: "Receptionist · Zone 5",
      proposerShifts: ["A", "M"], colleagueShifts: ["M", "A"],
      provenance: "Fatima proposed it. Irfan accepted it.",
    };

    // Words per text node: the card's textContent runs adjacent cells together
    // ("IrfanVishnu"), where a word boundary never falls — the second draft
    // passed on the fabricated preview for exactly that reason.
    const card = swapCard(detail, { timezone: "Asia/Kolkata", locale: "en-GB" });
    const walker = document.createTreeWalker(card, NodeFilter.SHOW_TEXT);
    const words: string[] = [];
    for (let node = walker.nextNode(); node !== null; node = walker.nextNode()) {
      words.push(...(node.textContent ?? "").split(/[^\p{L}]+/u));
    }
    const text = words.join(" ");

    // Every other person the recorded property has, by first name — derived,
    // so the check covers whoever the fixture holds rather than a list of three.
    const others = recordedPeople.postings.map((one) => one.who.split(" ")[0] ?? "");
    expect(others.length).toBeGreaterThan(3);
    // The two in the swap must not be among them, or a name from the fixture
    // and a name from the detail are indistinguishable — the first draft chose
    // "Arun", whom the fixture already holds.
    expect(others).not.toContain("Fatima");
    expect(others).not.toContain("Irfan");

    // Whole words: "Jose" is a substring of "Joseph".
    const named = others.filter((name) => name !== "" && new RegExp(`\\b${name}\\b`).test(text));
    expect(named).toEqual([]);
  });
});

import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { recordedLeave } from "../roster/leave";
import { decision } from "../screens/leave/decision";
import { queue } from "../screens/leave/approvals";

/**
 * The decision panel — owner, 2026-09-20, `64g` §4 B.
 *
 * A manager could not approve or decline anything from any screen (ledger D5),
 * and the read carried no id or version, so no control could have sent one
 * whatever it drew (D6). Both are now built; this holds what the panel says and
 * what it sends.
 */

interface Call { capability: string; method: string; params?: unknown }

const property = { timezone: "Asia/Kolkata", locale: "en-GB" };

function host(calls: Call[], fail = false): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["leave.approve"] },
    property,
    call: (capability: string, method: string, params?: unknown) => {
      calls.push({ capability, method, params });
      return fail
        ? Promise.reject(new HostCallError({ kind: "forbidden", message: "not yours to decide" }))
        : Promise.resolve({ id: "x", version: 2, state: "Approved" });
    },
    on: () => () => {},
  };
}

async function settle(): Promise<void> {
  for (let turn = 0; turn < 8; turn += 1) {
    await new Promise((resolve) => { setTimeout(resolve, 1); });
  }
}

/** Joseph's row: a balance, somebody else off, and a note. */
const withFacts = recordedLeave.waiting.find((one) => one.who === "Joseph Kurian")!;

/** Rani's row: nothing ever posted, nobody else off, no note. */
const withNothing = recordedLeave.waiting.find((one) => one.who === "Rani Rajan")!;

const CONFIRM = ".acts button:last-of-type";

describe("the queue", () => {
  it("opens a row's panel from a button a keyboard can reach", () => {
    const opened: string[] = [];
    const list = queue(recordedLeave.waiting, property, null, (id) => { opened.push(id); });

    const rows = Array.from(list.querySelectorAll<HTMLButtonElement>("button.row"));
    expect(rows).toHaveLength(recordedLeave.waiting.length);
    rows[1]!.click();

    expect(opened).toEqual([recordedLeave.waiting[1]!.id]);
  });

  it("marks the open row, and only that one", () => {
    const list = queue(recordedLeave.waiting, property, withFacts.id);
    const pressed = Array.from(list.querySelectorAll('button.row[aria-pressed="true"]'));

    expect(pressed).toHaveLength(1);
    expect(pressed[0]!.textContent).toContain("Joseph Kurian");
  });
});

describe("the decision panel", () => {
  it("says what the decision would leave behind, who else is off, and the note", () => {
    const panel = decision(host([]), withFacts, property, () => {});
    const text = panel.textContent ?? "";

    expect(text).toContain("Joseph Kurian");
    expect(text).toContain("9 of 11 days");
    expect(text).toContain("Sneha Iyer");
    expect(text).toContain("Family function");
  });

  it("says nothing is recorded rather than drawing a zero", () => {
    // The gap rule at a number: null means no ledger row, and 0 would claim one.
    const panel = decision(host([]), withNothing, property, () => {});
    const text = panel.textContent ?? "";

    expect(text).toContain("not recorded");
    expect(text).toContain("nobody else");
    expect(text).not.toMatch(/\b0 of\b/u);
  });

  it("approves the request it is open on, with its version", async () => {
    const calls: Call[] = [];
    const panel = decision(host(calls), withFacts, property, () => {});
    panel.querySelector<HTMLButtonElement>(CONFIRM)!.click();
    await settle();

    expect(calls).toEqual([{
      capability: "leave.approve",
      method: "approve",
      params: { id: withFacts.id, version: withFacts.version },
    }]);
  });

  it("declines with the note when one is typed, and without when it is not", async () => {
    const calls: Call[] = [];
    const panel = decision(host(calls), withFacts, property, () => {});

    const decline = Array.from(panel.querySelectorAll<HTMLButtonElement>("button"))
      .find((one) => one.textContent === "Decline")!;

    decline.click();
    await settle();

    const typed = decision(host(calls), withFacts, property, () => {});
    typed.querySelector<HTMLTextAreaElement>("textarea")!.value = "  Cover is short that week  ";
    Array.from(typed.querySelectorAll<HTMLButtonElement>("button"))
      .find((one) => one.textContent === "Decline")!.click();
    await settle();

    expect(calls.map((one) => one.params)).toEqual([
      { id: withFacts.id, version: withFacts.version },
      { id: withFacts.id, version: withFacts.version, note: "Cover is short that week" },
    ]);
  });

  it("keeps the panel open on a refusal, carrying the platform's own sentence", async () => {
    // Not the raw message: `write` maps a refusal to the sentence the platform
    // owns, which is right here because a refusal happens before anything runs
    // — "nothing was changed" is known to be true of it. The first version of
    // this test expected the message through, and the run said otherwise.
    const closed: string[] = [];
    const panel = decision(host([], true), withFacts, property, () => { closed.push("closed"); });

    panel.querySelector<HTMLButtonElement>(CONFIRM)!.click();
    await settle();

    expect(closed).toEqual([]);
    expect(panel.textContent).toContain("That did not go through. Nothing was changed.");
  });
});

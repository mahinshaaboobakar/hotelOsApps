/**
 * The failure surface's structure, per cause — `64b` Treatment A.
 *
 * **What this cannot see is the defect that prompted it.** The owner's report
 * of 2026-09-18 was that every failure sat at the top of its window; that is
 * layout, and happy-dom computes none. The captures measured it — every screen
 * and card centred to within a pixel, both ways, across all three causes. This
 * file holds the half a suite CAN hold: that every failure goes through the
 * stage that does the centring, and that each cause carries the action the
 * frame gives it and no other.
 */

import { describe, expect, it } from "vitest";

import { failureDrawing, type Cause, type ReadFailure } from "@hotelos/sdk";

import { cannot, failed } from "../chrome/marks";
import { unanswered } from "../widgets/card";

const CAUSES: readonly Cause[] = ["unanswered", "forbidden", "faulted"];

function drawing(cause: Cause) {
  const failure: ReadFailure = {
    cause,
    capability: "reservation.read",
    method: "today",
    said: null,
    at: new Date("2026-09-17T08:53:23Z"),
  };

  return failureDrawing(failure, { app: "GuestOps", the: "today at this property" });
}

describe("a screen's failure", () => {
  it.each(CAUSES)("is placed on the stage that centres it — %s", (cause) => {
    const stage = failed(drawing(cause), () => {});

    // The stage is the element the caller inserts. A `.fail` returned bare is
    // how every failure sat at the top: nothing around it had a height to
    // centre within.
    expect(stage.className).toBe("fs");
    expect(stage.children).toHaveLength(1);
    expect(stage.firstElementChild?.classList.contains("fail")).toBe(true);
  });

  it("puts a failure that is not a read on the same stage", () => {
    expect(cannot("No stay was chosen", "Open a stay.").className).toBe("fs");
  });

  it.each([
    ["unanswered", "Try again"],
    ["faulted", "Copy these details"],
  ] as const)("offers a button only where one can work — %s", (cause, label) => {
    const buttons = failed(drawing(cause), () => {}).querySelectorAll(".fd button");

    expect([...buttons].map((b) => b.textContent)).toEqual([label]);
  });

  it("offers a refusal no button, and names the grant beside it", () => {
    const stage = failed(drawing("forbidden"), () => {});

    expect(stage.querySelectorAll(".fd button")).toHaveLength(0);
    expect(stage.querySelector(".fn")?.textContent).toBe(
      "This screen needs reservation.read, and no grant at this property names this user.");
  });
});

describe("a widget's failure", () => {
  it.each(CAUSES)("draws mark, headline, reason and one action, in that order — %s", (cause) => {
    const card = unanswered("Today at the Desk", drawing(cause), { retry: () => {}, open: () => {} });
    const body = card.querySelector(".wb.wx");

    expect([...(body?.children ?? [])].map((e) => e.getAttribute("class"))).toEqual(
      ["wg", "wf", "wfw", "wo"]);
  });

  it("uses the frame's short sentence, not the screen's", () => {
    const card = unanswered("Today", drawing("unanswered"), { retry: () => {}, open: () => {} });

    expect(card.querySelector(".wfw")?.textContent)
      .toBe("Nothing is known about this — not empty, not full.");
  });

  it.each([
    ["unanswered", "Try again →", "retry"],
    ["forbidden", "Open GuestOps →", "open"],
    ["faulted", "Open GuestOps →", "open"],
  ] as const)("acts by the cause — %s", (cause, label, which) => {
    const called: string[] = [];
    const card = unanswered("Today", drawing(cause), {
      retry: () => called.push("retry"),
      open: () => called.push("open"),
    });

    const action = card.querySelector<HTMLButtonElement>(".wo");
    expect(action?.textContent).toBe(label);

    action?.click();
    expect(called).toEqual([which]);
  });
});

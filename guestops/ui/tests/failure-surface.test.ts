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

/** Contract v2's six causes (`d45f028d`). */
const CAUSES: readonly Cause[] = [
  "unanswered", "forbidden", "unadmitted", "ungranted", "undecidable", "faulted",
];

/**
 * The mark's colour per cause, spelled out from the approved drawings — never
 * imported from `chrome/glyph.ts`, which is the code under test.
 *
 * `64b`: unanswered warn, forbidden dim, faulted bad. `64e`: the two new
 * refusals *"neutral grey, like 64b's refusal"*, and the model state drawn
 * `c-fault`.
 */
const TONE: Record<Cause, string> = {
  unanswered: "wait",
  forbidden: "no",
  unadmitted: "no",
  ungranted: "no",
  undecidable: "fault",
  faulted: "fault",
};

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

describe("the mark's colour, per cause", () => {
  it.each(CAUSES)("on a screen — %s", (cause) => {
    const state = failed(drawing(cause), () => {}).querySelector(".fail");

    expect([...(state?.classList ?? [])].filter((c) => c !== "fail")).toEqual([TONE[cause]]);
  });

  it.each(CAUSES)("on a card — %s", (cause) => {
    const card = unanswered("Today", drawing(cause), { retry: () => {}, open: () => {} });
    const body = card.querySelector(".wb.wx");

    expect([...(body?.classList ?? [])].filter((c) => c !== "wb" && c !== "wx"))
      .toEqual([TONE[cause]]);
  });
});

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

  // X7: a retry only where waiting could work; copy for a fault and for the
  // model state, which 64e draws beside the fault.
  it.each([
    ["unanswered", "Try again"],
    ["faulted", "Copy these details"],
    ["undecidable", "Copy these details"],
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

  // X9: every refusal names what is missing and stops — no button, and no
  // sentence that sends the reader to a person, a name or a role.
  it.each(["forbidden", "unadmitted", "ungranted"] as const)(
    "a refusal offers no button and routes nobody to a person — %s",
    (cause) => {
      const stage = failed(drawing(cause), () => {});

      expect(stage.querySelectorAll(".fd button")).toHaveLength(0);
      expect(stage.textContent ?? "").not.toMatch(/administrator|\bask\b|manager|contact/i);
    });

  // X11: the model state names the model and never the person.
  it("the model state says nothing about the person or their account", () => {
    const state = failed(drawing("undecidable"), () => {}).querySelector(".fail");
    const words = [".fl", ".fh", ".fb", ".fn"]
      .map((selector) => state?.querySelector(selector)?.textContent ?? "").join(" ");

    expect(words).not.toMatch(/\byou\b|\byour\b|account/i);
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
    ["unadmitted", "Open GuestOps →", "open"],
    ["ungranted", "Open GuestOps →", "open"],
    ["undecidable", "Open GuestOps →", "open"],
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

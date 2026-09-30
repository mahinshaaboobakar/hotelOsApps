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

/**
 * What "Copy these details" actually does — the owner's report of 2026-09-29.
 *
 * **The button's existing test asserts its LABEL and can never see this.** It
 * reads `textContent` and never clicks, so a button wired to nothing passes it
 * exactly as well as a working one.
 *
 * The three outcomes had one rendering — none — because the call was
 * `void navigator.clipboard?.writeText(wire)`: `?.` swallows an absent
 * clipboard, `void` swallows a rejected promise, and success drew nothing. Two
 * outcomes with opposite remedies must not look identical, so each is asserted
 * here by the sentence it now produces.
 */
describe("nothing on a failure card reaches for the clipboard", () => {
  /**
   * **This block asserted three clipboard outcomes until 2026-09-30.** It tested
   * that an absent clipboard, a refused write and a successful one each produced
   * their own sentence, because the call before them — `void
   * navigator.clipboard?.writeText(wire)` — rendered all three as nothing.
   *
   * The reasoning was right and the affordance was impossible. A module's realm
   * is `sandbox="allow-scripts"` with no `allow=` and no `allow-same-origin`, so
   * its origin is opaque and `clipboard-write` is default-deny: the write could
   * only reject. **The owner's walk produced the refusal sentence**, which is how
   * that was established rather than argued.
   *
   * So the three sentences are gone with the button, and what stands in their
   * place is the stronger claim: the card touches the clipboard API **not at
   * all**. Asserted by making any access throw, because a test that merely
   * checked for no button would pass against a card that wrote silently on
   * render.
   */
  it("does not touch navigator.clipboard, on any cause", () => {
    const reached: string[] = [];
    Object.defineProperty(globalThis.navigator, "clipboard", {
      configurable: true,
      get() { reached.push("clipboard"); throw new Error("the card must not reach for this"); },
    });

    try {
      for (const cause of ["unanswered", "faulted", "undecidable", "forbidden",
        "unadmitted", "ungranted"] as const) {
        const stage = failed(drawing(cause), () => {});
        stage.querySelectorAll<HTMLButtonElement>(".fd button").forEach((b) => b.click());
      }
    } finally {
      Object.defineProperty(globalThis.navigator, "clipboard", {
        value: undefined, configurable: true, writable: true,
      });
    }

    expect(reached).toEqual([]);
  });

  /**
   * The note carries the failure's own phrase, so removing the button removed a
   * control and no information. It is `drawing.act.note` — never a sentence about
   * a button — which is why it still reads correctly with nothing beside it.
   */
  it("still carries the failure's own phrase with no button beside it", () => {
    const stage = failed(drawing("faulted"), () => {});

    expect(stage.querySelectorAll(".fd button")).toHaveLength(0);
    expect(stage.querySelector(".fd .fn")?.textContent).toBe(drawing("faulted").act.note);
  });

  it("announces the answer, because a person is not watching that line", () => {
    const stage = failed(drawing("faulted"), () => {});

    expect(stage.querySelector(".fd .fn")?.getAttribute("aria-live")).toBe("polite");
  });
});

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

  // X7: a retry only where waiting could work. Every other state offers the
  // labelled facts instead.
  //
  // **This asserted `"Copy these details"` on the five non-retry causes until
  // 2026-09-30, and the contract changed rather than the test drifting.** 64h
  // frame 3 gave a refused card a button so a person could reach the identifier;
  // the realm a module runs in cannot write to a clipboard at all — sandbox
  // `allow-scripts` alone, no `allow=`, opaque origin — so the button could only
  // ever refuse, and the owner's walk proved it. The card's labelled facts are
  // the path to the identifier now, which is what frame 3 was reaching for.
  // ADR 0034: corrected, not worked around.
  it("offers a retry where waiting could work", () => {
    const buttons = failed(drawing("unanswered"), () => {}).querySelectorAll(".fd button");

    expect([...buttons].map((b) => b.textContent)).toEqual(["Try again"]);
  });

  it.each(["faulted", "undecidable", "forbidden", "unadmitted", "ungranted"] as const)(
    "offers NO button where none can work — %s", (cause) => {
      const buttons = failed(drawing(cause), () => {}).querySelectorAll(".fd button");

      expect([...buttons].map((b) => b.textContent)).toEqual([]);
    });

  // **Corrected 2026-09-22, and it asserted the frame the owner rejected.**
  // This read: no button, and the sentence naming `reservation.read`. Both
  // halves were the old contract — 64h frame 3 took the code name out of the
  // sentence AND gave the card the line to copy, because either alone leaves a
  // refused card with no path to the identifier (ADR 0034: corrected, not
  // worked around).
  it("names no code, and offers the line rather than nothing", () => {
    const stage = failed(drawing("forbidden"), () => {});

    expect(stage.querySelector(".fn")?.textContent).toBe(
      "This screen needs a permission, and no grant at this property names this user.");

    // The code name is not lost — it is on the line the button copies.
    expect(stage.querySelector(".fn")?.textContent).not.toMatch(/reservation\.read/);
  });

  // X9: every refusal names what is missing and routes nobody to a person.
  //
  // **The "no button" half moved with 64h frame 3; the rest did not.** A
  // button that copies a line is not a route to a person — it hands the
  // identifier to whoever the reader already deals with — so what X9 is about
  // is unchanged, and this asserts that half alone.
  it.each(["forbidden", "unadmitted", "ungranted"] as const)(
    "a refusal routes nobody to a person — %s",
    (cause) => {
      const stage = failed(drawing(cause), () => {});

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

/**
 * No control looks live and does nothing.
 *
 * The capability ledger (2026-09-19) found sixteen buttons on the owner's
 * platform that looked pressable and had no action, because `control()` took
 * its action as optional. It is required now, and a control GuestOps cannot
 * perform is `unavailable()`, which is disabled and carries its reason.
 *
 * **The type is the guard, and this file holds the type.** `tsc` covers
 * `tests/`, so the `@ts-expect-error` below fails the typecheck the day the
 * action goes back to being optional — the compiler is what found all twenty
 * call sites when it became required, and it is what keeps the next one out.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { describe, expect, it } from "vitest";

import { control, unavailable } from "../chrome/element";

describe("a control", () => {
  it("cannot be written without what it does", () => {
    // @ts-expect-error — the action is required; a button with none is the defect
    const dead = control("btn", "Check in");
    expect(dead.tagName).toBe("BUTTON");
  });

  it("that cannot act is disabled, and says why in words", () => {
    const why = "Checking in is not available from this screen yet.";
    const off = unavailable("btn", "Check in", why) as HTMLButtonElement;

    expect(off.disabled).toBe(true);
    expect(off.classList.contains("off")).toBe(true);
    expect(off.title).toBe(why);
    expect(off.getAttribute("aria-description")).toBe(why);
  });
});

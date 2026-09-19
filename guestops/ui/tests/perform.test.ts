/**
 * What a failed write may claim — `book/index.ts`'s `perform`.
 *
 * **A sentence about a write is only as honest as what the write did.** Every
 * host error that could not be shown to a person used to end at *"The platform
 * refused this. Nothing was changed."* — including `unavailable`, a write that
 * got no answer and may have landed. Told nothing changed, a person presses
 * again (CLAUDE.md, *a false claim about a write*). KK found it in Room Care;
 * the architect sent it here, 2026-09-19.
 *
 * ```text
 * refused before anything ran   forbidden · local_forbidden · user_forbidden
 *                               model_unavailable   → nothing was changed
 * may have landed               unavailable · internal → not known
 * the service's own words       rejected · invalid (ADR 0041)
 * ```
 *
 * "Nothing was changed" is true of a refusal only because the one write this
 * UI makes — cancelling a booking — now commits in one transaction
 * (`CancelAtomicityTests`); before that a refusal on a group's second stay
 * followed the first stay's cancellation.
 */

import { describe, expect, it } from "vitest";

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { perform } from "../book";
import { notCancelled } from "../screens/booking";

function failingWith(kind: string, message = "the service's own sentence"): HostApi {
  return {
    identity: { id: "guestops", version: "0.3.2", capabilities: ["stay.override"] },
    property: { timezone: null, locale: null },
    call: () => Promise.reject(new HostCallError({ kind, message } as never)),
    on: () => () => {},
  } as HostApi;
}

async function said(kind: string): Promise<string> {
  const done = await perform(failingWith(kind), "stay.override", "cancel", {});
  expect(done.value).toBeNull();
  return done.refused ?? "";
}

async function unchanged(kind: string): Promise<boolean | null> {
  return (await perform(failingWith(kind), "stay.override", "cancel", {})).unchanged;
}

describe("the cancel dialog's heading says only what is known", () => {
  it.each([
    ["forbidden", "Nothing was cancelled."],
    ["model_unavailable", "Nothing was cancelled."],
    ["rejected", "Nothing was cancelled."],
    ["unavailable", "Whether the booking was cancelled is not known."],
    ["internal", "Whether the booking was cancelled is not known."],
  ] as const)("%s", async (kind, heading) => {
    const known = await unchanged(kind);
    expect(known).not.toBeNull();
    expect(notCancelled("why", known === true).querySelector("b")?.textContent).toBe(heading);
  });
});

describe("what a failed write may claim", () => {
  it.each(["forbidden", "local_forbidden", "user_forbidden"])(
    "a refusal was decided before anything ran — %s", async (kind) => {
      expect(await said(kind)).toMatch(/nothing was changed/i);
    });

  it("the model state: nothing changed, and never 'not permitted' (AUTHZ-Q34)", async () => {
    const words = await said("model_unavailable");
    expect(words).toMatch(/nothing was changed/i);
    expect(words).toMatch(/could not be checked/i);
    expect(words).not.toMatch(/not permitted|refused|you /i);
  });

  it.each(["unavailable", "internal"])(
    "a write that may have landed says the outcome is not known — %s", async (kind) => {
      const words = await said(kind);
      expect(words).not.toMatch(/nothing was changed/i);
      expect(words).toMatch(/not known/i);
      expect(words).toMatch(/check before trying again/i);
    });

  it.each(["rejected", "invalid"])(
    "the service's own sentence crosses when ADR 0041 lets it — %s", async (kind) => {
      expect(await said(kind)).toBe("the service's own sentence");
    });
});

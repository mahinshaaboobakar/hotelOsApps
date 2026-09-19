import { readFileSync } from "node:fs";
import { join } from "node:path";

import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { WriteRefused, write } from "../roster";

/**
 * What a failed save may claim about what happened.
 *
 * **"Nothing was changed" is a claim about a write, and it is only true where
 * the platform refused before the service did anything.** KK found Room Care
 * saying it after a save that got no answer — one that may well have gone
 * through — which invites the person to press Save again and do it twice. The
 * same sentence was in seven Workforce files, all fed by `write()`.
 *
 * So each host kind is asserted by what it means, not by what reads well:
 * the three refusals and an authority that could not decide never ran the
 * write; silence and a service fault leave the outcome unknown, and say so.
 */

function host(kind: ConstructorParameters<typeof HostCallError>[0]["kind"]): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.plan"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: () => Promise.reject(new HostCallError({ kind, message: "diagnostic, not for people" })),
    on: () => () => {},
  };
}

async function said(kind: Parameters<typeof host>[0]): Promise<string> {
  try {
    await write(host(kind), "roster.plan", "assign", {});
  } catch (error) {
    if (error instanceof WriteRefused) return error.message;
    throw error;
  }
  throw new Error("the write was expected to fail");
}

describe("a failed save says only what is known about it", () => {
  for (const kind of ["forbidden", "local_forbidden", "user_forbidden", "model_unavailable"] as const) {
    it(`says nothing was changed when the platform refused (${kind})`, async () => {
      expect(await said(kind)).toContain("Nothing was changed");
    });
  }

  it("says the outcome is not known, and warns against a blind retry, when nothing answered", async () => {
    const sentence = await said("unavailable");
    expect(sentence).not.toContain("Nothing was changed");
    expect(sentence).toMatch(/not known/u);
    expect(sentence).toMatch(/before trying again/u);
  });

  it("does not claim nothing changed when the service reported a fault", async () => {
    const sentence = await said("internal");
    expect(sentence).not.toContain("Nothing was changed");
    expect(sentence).toMatch(/before trying again/u);
  });
});

/**
 * The six dialogs that save, each with a fallback for an error that is not a
 * `WriteRefused` — an exception it rethrows, whose write may or may not have
 * run. **The list is the architect's, from KK's finding**; a dialog added later
 * that writes is covered by `write()` above, and this pins the six fallbacks
 * that each carried their own copy of the sentence.
 */
const DIALOGS = [
  "screens/duty/dialog.ts",
  "screens/people/end-posting.ts",
  "screens/rota/picker.ts",
  "screens/teams/form.ts",
  "screens/teams/member.ts",
  "screens/teams/stand-down.ts",
];

describe("a dialog's own fallback does not claim nothing changed", () => {
  for (const file of DIALOGS) {
    it(`${file} — an unexpected failure leaves the outcome unknown`, () => {
      const source = readFileSync(join(import.meta.dirname, "..", file), "utf8");
      // The fallback is the branch after `instanceof WriteRefused ? error.message :`.
      const fallback = /instanceof WriteRefused\s*\?\s*error\.message\s*:\s*([^)]+)\)/u.exec(source);
      expect(fallback, `${file} has no WriteRefused fallback to check`).not.toBeNull();
      expect(fallback?.[1]).not.toMatch(/Nothing was changed/u);
    });
  }
});

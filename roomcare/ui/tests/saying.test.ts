import { HostCallError } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { saying } from "../chrome/load";

/**
 * What a person reads when a write did not succeed may claim only what the
 * platform knows (CLAUDE.md, "a false claim about a write" — Z, 2026-09-10). A
 * refusal was decided before anything ran, so "nothing was changed" is true of
 * it. A non-answer or a fault may have landed, so claiming nothing changed
 * invites the person to do it twice — a room assigned twice, a grant revoked
 * twice. Found by the page-64 audit (2026-09-19), which pressed each overlay's
 * action against a host that answers nothing.
 */
const failing = (kind: ConstructorParameters<typeof HostCallError>[0]["kind"]): HostCallError =>
  new HostCallError({ kind, message: "the test asked" });

describe("a write that did not succeed", () => {
  it("says nothing was changed only when the write was refused before it ran", () => {
    for (const kind of ["forbidden", "local_forbidden", "user_forbidden", "model_unavailable"] as const) {
      expect(saying(failing(kind)), kind).toMatch(/nothing was changed/u);
    }
  });

  it("never calls the model's silence a refusal: it could not be checked, and nothing says the person lacks a grant", () => {
    // AUTHZ-Q34/35a and 64e: model_unavailable is the model unable to decide — it says nothing about a grant. FF's finding.
    const said = saying(failing("model_unavailable"));
    expect(said).not.toMatch(/not permitted|not allowed|no access|forbidden/iu);
    expect(said).toMatch(/could not be checked/u);
  });

  it("gives the service's own sentence where ADR 0041 lets it cross", () => {
    for (const kind of ["rejected", "invalid"] as const) expect(saying(failing(kind)), kind).toBe("the test asked");
  });

  it("never claims nothing was changed when nobody answered, or the service faulted", () => {
    for (const kind of ["unavailable", "internal"] as const) {
      const said = saying(failing(kind));
      expect(said, kind).not.toMatch(/nothing was changed/u);
      expect(said, kind).toMatch(/not known/u);
    }
    expect(saying(new TypeError("not a host failure"))).not.toMatch(/nothing was changed/u);
  });
});

import { describe, expect, it } from "vitest";

import { readable, SURFACES, surfaceHost } from "./surfaces";

/**
 * No entity id ever reaches a person — owner ruling, 2026-09-22, platform-wide.
 *
 * *"No id ever reaches a person in an operation or data flow — a person always
 * chooses by name."*
 *
 * # Why the existing walk does not already cover this
 *
 * `developer-content.test.ts` walks the same surfaces and catches a different
 * population: a decision-register id (`WF-Q18`), an ADR, a code identifier
 * (`roster_plan`), a design page, a platform system. That is a service's
 * **vocabulary**. This is a row's **identity** — `2f4c8a61-3d97-…` — which is
 * not developer content, carries no words, and passes that guard untouched.
 * Two guards, one walk (HH's rule: private screen lists drift in the half
 * nobody reads).
 *
 * # What it can and cannot see
 *
 * It renders every surface against its recorded read and looks for a UUID in
 * what a person can actually read — so it sees a value that reaches the screen
 * however it got there, which a source rule cannot. It is blind to an id that
 * no fixture carries, and to one rendered only on a path this walk does not
 * open; `SURFACES` is where that is fixed, once, for both guards.
 *
 * **Ids in the DOM are not the subject.** A write names its row — `leave.request
 * · withdraw` takes an id and a version — and a control may carry one in a
 * `data-` attribute or a handler closure. That is machine to machine. The
 * ruling is about what a person READS, so this asserts on rendered text.
 */

const host = surfaceHost("en-GB");

/** A UUID as this platform mints them, in any case. */
const UUID = /\b[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}\b/iu;

describe("no id reaches a person", () => {
  it("finds one when it is there — the positive control", () => {
    // A zero from a walk is a claim about the walk first. This proves the
    // pattern and the reader agree before any surface is trusted to be clean.
    const planted = document.createElement("div");
    planted.innerHTML = "<p>Withdrawn by 2f4c8a61-3d97-4e52-b8a0-6c1f9d3e7b25</p>";

    expect(UUID.test(readable(planted).textContent ?? "")).toBe(true);
  });

  for (const [name, draw] of SURFACES) {
    it(`${name} shows no id`, async () => {
      const main = document.createElement("div");
      await draw(host, main);

      const text = readable(main).textContent ?? "";

      // Positive control per surface: a screen that drew nothing would pass
      // this assertion for the wrong reason, which is how a walk quietly stops
      // measuring anything.
      expect(text.trim().length, `${name} drew nothing`).toBeGreaterThan(0);

      const hit = UUID.exec(text);
      expect(hit?.[0], `${name} shows an id a person cannot use`).toBeUndefined();
    });
  }
});

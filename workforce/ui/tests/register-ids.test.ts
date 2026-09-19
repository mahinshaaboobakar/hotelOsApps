import { describe, expect, it } from "vitest";

import { readable, SURFACES, surfaceHost } from "./surfaces";

/**
 * No decision-register id reaches a person at the property.
 *
 * `WF-Q18` is how this platform's engineers find a ruling. To a supervisor
 * reading why a column is dashes it is a code with nothing behind it — the
 * reason belongs on the screen in words, and the id in the code beside it,
 * where the next engineer arrives. Reports' note carried one (found during the
 * U1 sweep, 2026-09-19).
 *
 * Rendered rather than read from source: a comment citing a ruling is a record
 * and is right, and only what actually reaches the screen is a claim to staff.
 */
const REGISTER_ID = /\b[A-Z]+-Q[0-9]+[a-z]?\b/g;

describe("register ids", () => {
  for (const [name, draw] of SURFACES) {
    it(`${name} shows none to staff`, async () => {
      const main = document.createElement("div");
      await draw(surfaceHost("en-GB"), main);

      expect(main.querySelector(".fail"), `${name} could not read its fixture`).toBeNull();
      expect(readable(main).textContent?.match(REGISTER_ID) ?? []).toEqual([]);
    });
  }
});

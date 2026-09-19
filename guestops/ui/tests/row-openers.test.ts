/**
 * A row that opens something has a real button in its main cell — APPS-Q50.
 *
 * Page 64 §2 said such a row is a real `<button>` and §4 that the list is a
 * table; a table row cannot be a button. The planner ruled the shape both
 * standards meet in (2026-09-19): the row keeps its click for a pointer, and
 * its main cell holds a real button a keyboard reaches and a screen reader
 * announces. Rendered through the shared walk, so every list that opens rows is
 * checked — not the two somebody remembered.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { describe, expect, it } from "vitest";

import { SCREENS, surfaceHost } from "./surfaces";

describe("rows that open something", () => {
  const drawn = async (): Promise<[string, HTMLElement][]> => {
    const out: [string, HTMLElement][] = [];
    for (const [name, draw] of SCREENS) {
      const main = document.createElement("div");
      await draw(surfaceHost(), main);
      out.push([name, main]);
    }
    return out;
  };

  it("are found, so the check cannot pass over nothing", async () => {
    const rows = (await drawn()).flatMap(([, main]) => [...main.querySelectorAll(".tr.act")]);
    expect(rows.length).toBeGreaterThanOrEqual(10);
  });

  it("each carry a real button in the main cell, and only one", async () => {
    const missing = (await drawn()).flatMap(([name, main]) =>
      [...main.querySelectorAll(".tr.act")]
        .filter((row) => {
          const main_ = row.firstElementChild;
          return main_ === null || main_.querySelectorAll("button.opener").length !== 1;
        })
        .map((row) => `${name}: ${row.textContent?.slice(0, 40) ?? ""}`));

    expect(missing).toEqual([]);
  });
});

/**
 * The pager — what it draws, and the one case where it draws nothing.
 *
 * `CORE-Q13` made the pager numbered, which means it now asserts two things to
 * a receptionist: that there are more pages, and how many. Both are arithmetic
 * over a total the wire supplies, so both are checkable here — and neither is
 * visible to a capture, which can see that a pager is present but not that its
 * last page is the right number.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { describe, expect, it } from "vitest";

import { pager } from "../chrome/pager";

/**
 * The rendered pager, or null, with a recorder for what was chosen.
 *
 * `shown` defaults to a full page, which is what every page but the last one
 * holds. The tests that care about a short page pass their own.
 */
function draw(total: number, page: number, size = 25, shown = size) {
  const chosen: number[] = [];
  const element = pager(total, page, size, shown, (to: number) => chosen.push(to));

  return { element, chosen };
}

/** The page numbers on screen, in order, `…` for an elision. */
function labels(element: HTMLElement): string[] {
  return [...element.querySelectorAll(".pg, .gap")].map(
    (node) => node.textContent ?? "",
  );
}

describe("the pager", () => {
  /**
   * The count is information, even when there is one page of it.
   *
   * This asserted the opposite until 2026-09-05 — that a one-page list gets no
   * pager, on the argument that a disabled page button is a control that can
   * never do anything. Gold frame 1 draws `showing 1–14 of 14` over a
   * fourteen-row list, and the owner rejected the build for the pager's
   * absence. **A test encoding a superseded contract is replaced, not
   * suppressed** (ADR 0034), and what it now guards is the reason: a
   * receptionist checking the morning's arrivals needs to know the list in
   * front of them is the whole list, which a list that simply stops cannot say.
   */
  it("draws the range for a list that fits on one page", () => {
    const { element } = draw(14, 0, 25, 14);

    expect(element).not.toBeNull();
    expect(element?.querySelector("span")?.textContent).toBe("showing 1–14 of 14");
  });

  /**
   * One page, and neither arrow goes anywhere.
   *
   * The page's own button stays live and re-selects page 0, which is harmless
   * and is what every current-page button in the pager does. What must not
   * happen is an arrow moving off the only page there is.
   */
  it("offers nowhere to go from the only page", () => {
    const { element, chosen } = draw(14, 0, 25, 14);

    expect(labels(element as HTMLElement)).toEqual(["‹", "1", "›"]);

    for (const arrow of element?.querySelectorAll<HTMLElement>(".pg[disabled]") ?? []) {
      arrow.click();
    }

    expect(chosen).toEqual([]);
    expect(element?.querySelectorAll(".pg[disabled]")).toHaveLength(2);
  });

  it("states the range and the total the wire gave it", () => {
    const { element } = draw(47, 0);

    expect(element?.textContent).toContain("showing 1–25 of 47");
  });

  /**
   * The last page is short, and the range says so.
   *
   * `Math.min` rather than `(page + 1) * size`: page two of forty-seven ends at
   * 47, not at 50, and a range claiming rows that are not there is the same
   * class of fiction as a total nobody counted.
   */
  it("does not claim rows the last page does not have", () => {
    const { element } = draw(47, 1);

    expect(element?.textContent).toContain("showing 26–47 of 47");
  });

  it("marks the current page and nothing else", () => {
    const { element } = draw(200, 3);
    const on = element?.querySelectorAll(".pg.on") ?? [];

    expect(on).toHaveLength(1);
    expect(on[0]?.textContent).toBe("4");
  });

  /**
   * 0-based in the model, 1-based on screen, converted in exactly one place.
   */
  it("labels pages from one and reports them from zero", () => {
    const { element, chosen } = draw(200, 0);
    const four = [...(element?.querySelectorAll(".pg") ?? [])].find(
      (node) => node.textContent === "4",
    );

    (four as HTMLElement).click();

    expect(chosen).toEqual([3]);
  });

  /** Both ends refuse to move past themselves. */
  it("cannot step back from the first page or on from the last", () => {
    const first = draw(200, 0);
    (first.element?.querySelector(".pg") as HTMLElement).click();

    const pages = draw(200, 7);
    const arrows = pages.element?.querySelectorAll(".pg") ?? [];
    (arrows[arrows.length - 1] as HTMLElement).click();

    expect(first.chosen).toEqual([]);
    expect(pages.chosen).toEqual([]);
  });

  /**
   * A hundred pages is not the same design at a larger size.
   *
   * Jobs' board never has a hundred, so its rendering says nothing about them;
   * a property's stay list will. The window keeps the first, the last and five
   * around the current page, and the elision is drawn rather than implied.
   */
  it("elides rather than drawing two hundred buttons", () => {
    const { element } = draw(5_000, 100);

    expect(labels(element!)).toEqual(
      ["‹", "1", "…", "99", "100", "101", "102", "103", "…", "200", "›"],
    );
  });

  /** A short list is drawn whole, exactly as Jobs draws four pages. */
  it("draws every page when there are few enough", () => {
    const { element } = draw(100, 0);

    expect(labels(element!)).toEqual(["‹", "1", "2", "3", "4", "›"]);
  });

  /**
   * The range counts what is on screen, not what was asked for.
   *
   * A page that came back short — the last page of a list, or a server that
   * clamped — used to be described by the page size rather than by its rows,
   * so it stated a range nobody could check by counting. That is a number that
   * would read the same if the list had failed to load half of itself.
   */
  it("states the rows it actually has, not the page size", () => {
    const { element } = draw(218, 0, 25, 9);

    expect(element?.querySelector("span")?.textContent).toBe("showing 1–9 of 218");
  });

  it("says a page is empty rather than describing rows that are not there", () => {
    const { element } = draw(218, 3, 25, 0);

    expect(element?.querySelector("span")?.textContent).toBe(
      "no rows on this page · 218 in the list",
    );
  });
});

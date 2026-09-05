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

/** The rendered pager, or null, with a recorder for what was chosen. */
function draw(total: number, page: number, size = 25) {
  const chosen: number[] = [];
  const element = pager(total, page, size, (to) => chosen.push(to));

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
   * A control that can never do anything reads as a broken one.
   *
   * The recorded facts are a single page, so this is also the state the preview
   * harness renders — which is why it is asserted rather than left to a capture
   * that would show an absence and prove nothing about the reason.
   */
  it("draws nothing at all when the list fits on one page", () => {
    expect(draw(25, 0).element).toBeNull();
    expect(draw(5, 0).element).toBeNull();
    expect(draw(0, 0).element).toBeNull();
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
});

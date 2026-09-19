/**
 * §9, O4 — "The scrim dismisses; the surface does not." Held for both surfaces.
 *
 * The page-64 audit found the rule implemented in `chrome/overlay.ts` and
 * asserted nowhere (2026-09-19). A click inside a sheet is a person working in
 * it, and closing on it would throw away what they were composing.
 */

import { describe, expect, it } from "vitest";

import { dialog, sheet } from "../chrome/overlay";

function drawn(make: typeof sheet) {
  let dismissed = 0;
  const root = make({
    title: "Title",
    subtitle: "Subtitle",
    body: [document.createTextNode("inside")],
    foot: null,
    actions: [],
    onDismiss: () => { dismissed += 1; },
  });
  document.body.replaceChildren(root);
  return { root, dismissed: () => dismissed };
}

describe.each([["sheet", sheet], ["dialog", dialog]] as const)("a %s", (_, make) => {
  it("is dismissed by a click on the scrim", () => {
    const { root, dismissed } = drawn(make);
    root.click();
    expect(dismissed()).toBe(1);
  });

  it("is not dismissed by a click on its surface or anything in it", () => {
    const { root, dismissed } = drawn(make);
    const surface = root.firstElementChild as HTMLElement;

    surface.click();
    for (const inside of Array.from(surface.querySelectorAll<HTMLElement>("*"))) inside.click();

    expect(surface).not.toBeNull();
    expect(dismissed()).toBe(0);
  });
});

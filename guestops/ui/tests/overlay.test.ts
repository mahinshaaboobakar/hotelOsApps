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

/**
 * An action that cannot do its work — and what it used to do instead.
 *
 * **`off` added a class and nothing else.** The button stayed pressable, and an
 * action with no `onClick` falls back to `onDismiss`, so an *off* primary
 * CLOSED the overlay — throwing away whatever the desk had typed into it. The
 * one caller that used it defended itself by hand with
 * `onClick: reason === null ? () => undefined : …`, which is the tell: a rule
 * of the form *the caller must also neutralise the handler* is one that gets
 * forgotten, and it had already been forgotten once by a dead primary on the
 * registration card.
 *
 * Found while building the walk-in (C2, 2026-09-23), whose primary is off until
 * the service's required fields are present.
 */
describe("an action that cannot do its work", () => {
  const off = () => {
    let dismissed = 0;
    const root = sheet({
      title: "Walk-in",
      subtitle: "Subtitle",
      body: [],
      foot: null,
      actions: [{ label: "Create and check in", primary: true, off: true, why: "This needs a room." }],
      onDismiss: () => { dismissed += 1; },
    });
    document.body.replaceChildren(root);
    const button = [...root.querySelectorAll("button")]
      .find((element) => element.textContent === "Create and check in");
    return { button: button as HTMLButtonElement, dismissed: () => dismissed };
  };

  it("is genuinely disabled rather than merely dashed", () => {
    expect(off().button.disabled).toBe(true);
  });

  it("does not dismiss the overlay when it is pressed", () => {
    const { button, dismissed } = off();
    button.click();

    // The whole defect: a sheet the desk had half-filled closed itself.
    expect(dismissed()).toBe(0);
  });

  it("carries its reason where a pointer and a screen reader both find it", () => {
    const { button } = off();

    expect(button.title).toBe("This needs a room.");
    expect(button.getAttribute("aria-description")).toBe("This needs a room.");
  });
});

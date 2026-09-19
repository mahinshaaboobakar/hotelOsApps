import { afterEach, beforeEach, describe, expect, it } from "vitest";

import { SURFACES, surfaceHost } from "./surfaces";

/**
 * Anything a person can click, a keyboard can reach.
 *
 * `no-dead-controls` keys on the classes the stylesheet draws as pressable, so
 * it could not see the rota's cells: `div`s with no pressable class and a click
 * listener, reachable by a mouse and by nothing else (ledger D4). Leave's tabs
 * were the same shape and were found only because they happened to carry
 * `.tab`.
 *
 * **Keyed on the listener, not on a class** — the property that makes an
 * element act. While each surface draws, every `click` listener is recorded
 * with the element it was added to, and each such element must be a control
 * a keyboard reaches: `button`, `select`, `input` or `a`.
 *
 * What it cannot see, stated: a listener added only after an interaction (a
 * picker opened by a click). The overlays those open are walked by their own
 * tests; a scrim's click-to-dismiss is a backdrop, never the only way out, and
 * is out of this walk because no surface opens one on first draw.
 */
const CONTROLS = new Set(["BUTTON", "SELECT", "INPUT", "A"]);

/**
 * The prototype that actually owns `addEventListener` for an element, found by
 * walking up from one. **Derived, not named**: the DOM's elements here inherit
 * from happy-dom's own `EventTarget`, not the global one — `div instanceof
 * EventTarget` is false — and the first version of this guard patched the
 * global prototype, recorded nothing, and passed every surface. The positive
 * control below is what caught it.
 */
function owner(): { addEventListener: EventTarget["addEventListener"] } {
  let at: object | null = document.createElement("div");
  while (at !== null && !Object.prototype.hasOwnProperty.call(at, "addEventListener")) {
    at = Object.getPrototypeOf(at) as object | null;
  }
  if (at === null) throw new Error("no prototype owns addEventListener");
  return at as { addEventListener: EventTarget["addEventListener"] };
}

const target = owner();
const real = target.addEventListener;
let clicked: Element[] = [];

beforeEach(() => {
  clicked = [];
  target.addEventListener = function record(
    this: EventTarget, type: string, ...rest: [EventListenerOrEventListenerObject | null, ...unknown[]]
  ) {
    if (type === "click" && "tagName" in this) clicked.push(this as unknown as Element);
    return (real as (...args: unknown[]) => void).call(this, type, ...rest);
  } as typeof real;
});

afterEach(() => {
  target.addEventListener = real;
});

const host = surfaceHost("en-GB");
let seen = 0;

describe("no mouse-only control", () => {
  for (const [name, draw] of SURFACES) {
    it(`${name} gives every click listener to a control`, async () => {
      const main = document.createElement("div");
      await draw(host, main);
      expect(main.querySelector(".fail"), `${name} could not read its fixture`).toBeNull();

      seen += clicked.length;
      const reachedByMouseOnly = clicked
        .filter((one) => !CONTROLS.has(one.tagName))
        .map((one) => `${one.tagName.toLowerCase()}.${one.className} "${one.textContent?.trim()}"`);

      expect(reachedByMouseOnly).toEqual([]);
    });
  }

  // Positive control: the recorder saw listeners, so a clean run is a result
  // and not a patch that recorded nothing.
  it("recorded listeners to judge", () => {
    expect(seen).toBeGreaterThan(20);
  });
});

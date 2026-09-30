/**
 * A fault while drawing is drawn — it is never left as a blank pane.
 *
 * # The defect this covers, and why nothing could see it
 *
 * Every screen in this module already handles both states a read can be in: a
 * failure draws the SDK's failure card naming the cause, and an empty list says
 * so in its own words — `today` "Nothing in this list today.", `bookings` "No
 * booking matches this search.", `attention` "Nothing needs a person." All ten
 * screens were measured for both.
 *
 * The third state had no handler at all. `draw` launched each screen with a bare
 * `void`, so a throw *after* a successful read became an unhandled rejection:
 * `main` kept the empty children it was created with, and the pane went blank.
 *
 * **A blank pane is indistinguishable from the other two**, which is the whole
 * reason it is forbidden — a person at a desk cannot tell "this property has no
 * arrivals today" from "the platform refused" from "GuestOps broke". The three
 * have three different remedies and one appearance.
 *
 * And it was invisible to every instrument: the backend's Serilog is the
 * service's own, the desktop's log stops at the host, and a module realm's
 * console reaches nobody. The read returned 200, so no failure was recorded
 * anywhere by anyone.
 *
 * # Why the payload is malformed rather than the property
 *
 * The throw is provoked with a **200 carrying a shape the screen cannot use**,
 * which is a real state — a service and a module at different versions — and
 * depends only on GuestOps' own code. Provoking it through an invalid time zone
 * would depend on the SDK's formatters, whose internals are not this test's
 * subject and could stop throwing without this defect being fixed.
 *
 * Tests live here rather than beside the source: ADR 0025, and TypeScript is
 * explicitly not an exception to it.
 */

import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedToday } from "../book/recorded";
import type { HostApi } from "@hotelos/sdk";

/**
 * A host whose reads succeed, answering `today` with whatever is given.
 *
 * The capability is granted and the call resolves, so nothing here produces a
 * read failure — that is the arrangement the test needs. A host that refused
 * would draw the read-failure card and pass under the defect too.
 */
function host(answer: unknown): HostApi {
  return {
    identity: { id: "guestops", version: "0.1.0", capabilities: ["reservation.read"] },
    property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    call: () => Promise.resolve(answer),
    on: () => () => {},
  } as unknown as HostApi;
}

/** Mount the module and let its first screen settle, however it settles. */
async function mount(answer: unknown): Promise<HTMLElement> {
  const root = document.createElement("div");
  document.body.append(root);

  activate(host(answer)).mount(root);

  // Two turns: one for the read, one for the rejection to reach the catch.
  await new Promise((resolve) => setTimeout(resolve, 0));
  await new Promise((resolve) => setTimeout(resolve, 0));
  return root;
}

/** What the pane holds — the element the screens draw into. */
function pane(root: HTMLElement): HTMLElement {
  const main = root.querySelector(".main");
  expect(main).not.toBeNull();
  return main as HTMLElement;
}

describe("a screen that throws while drawing", () => {
  /**
   * The defect itself. `{}` is a successful read whose `lists` is absent, so
   * `today` throws reaching into it — and before this change the pane stayed
   * exactly as `show` created it.
   */
  it("leaves something on the screen rather than an empty pane", async () => {
    const root = await mount({});

    expect(pane(root).children.length).toBeGreaterThan(0);
    expect(pane(root).textContent?.trim()).not.toBe("");
  });

  /** It says who could not do what, in this application's own name. */
  it("says that GuestOps could not draw it", async () => {
    const root = await mount({});

    expect(pane(root).textContent).toContain("GuestOps could not draw this screen");
  });

  /**
   * **The reason the throw carried, not a generic apology.** Asserted on the
   * error's `name`, which is stable, rather than on a message whose wording is
   * the engine's. A card that named no reason would satisfy every other
   * assertion here.
   */
  it("names the reason", async () => {
    const root = await mount({});

    expect(pane(root).textContent).toContain("TypeError");
  });

  /**
   * **It must not read as a failed read, because the read succeeded.**
   *
   * The nearest SDK cause is `faulted`, which that vocabulary defines as *the
   * service's own fault* and points at support — so drawing the read-failure
   * card here would send somebody after a service that answered correctly.
   *
   * Anchored on the whole clause: a match on "answered" alone would pass over a
   * sentence that went on to blame the platform.
   */
  it("says the platform answered, so nobody is sent after the service", async () => {
    const root = await mount({});

    expect(pane(root).textContent).toContain("The platform answered and GuestOps failed while drawing");
  });

  /**
   * No retry. A fault in this application does the same thing next time, and a
   * button promising otherwise is a promise the platform cannot keep — the same
   * reasoning `failed()` gives for withholding it from a refusal.
   *
   * **The card's presence is asserted FIRST, and that is not ceremony.** Written
   * as the absence alone, this test passed under the mutation probe that removed
   * the `.catch` — a blank pane has no buttons either, so "no button says Try
   * again" was equally true of the defect and of the fix. It could not have
   * failed on the thing it sits beside. The presence assertion is what makes the
   * absence mean anything.
   */
  it("offers no retry, on a card that is actually there", async () => {
    const root = await mount({});

    expect(pane(root).querySelector(".fail")).not.toBeNull();

    const labels = [...pane(root).querySelectorAll("button")].map((button) => button.textContent);
    expect(labels).not.toContain("Try again");
  });

  /**
   * **The case that proves nothing about the catch, and is why it is here.**
   *
   * A guard that swallowed every render would satisfy all five assertions above
   * while drawing this application's screens as failures forever. So a
   * well-formed payload must still draw the day — and it is the recorded
   * fixture, not a hand-built shape, so it cannot drift from what the screen
   * expects.
   */
  it("still draws the screen when the payload is well formed", async () => {
    const root = await mount(recordedToday);

    expect(pane(root).textContent).not.toContain("could not draw this screen");
    expect(pane(root).querySelector(".tbl")).not.toBeNull();
  });
});

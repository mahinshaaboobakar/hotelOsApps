import { HostCallError } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { readableText } from "../../../scripts/developer-content";
import { roomsReady } from "../widgets/panel/panels";
import { SUPERVISOR, host, mount, settle } from "./host";

/**
 * A failure card names what could not be read, in plain words — owner, 2026-09-20, 64g §2 B, landed in the shared
 * surface (`6751c7de`). "Asked for" said `roomcare.read · board`: a permission's code name on a live screen, which
 * the developer-content ruling of 2026-09-19 forbids. The code name is not lost — it stays on the line a person
 * copies for support, and on the fact's own `permission`/`method`, which the SDK documents as **not for drawing**.
 *
 * **The shared change did not reach Room Care on its own**, which is why this file exists: `chrome/failure.ts` drew
 * the `asked` fact from those two fields rather than from the fact's `value`, so every card kept printing the code
 * name after the SDK stopped offering it. Measured across all six causes before the fix.
 */
const CAUSES = ["unavailable", "forbidden", "internal", "model_unavailable", "user_forbidden", "local_forbidden"] as const;

/** A dotted lowercase identifier — `roomcare.read` — as opposed to a sentence, which is what a card may say. */
const CODE_NAME = /\b[a-z]{2,}\.[a-z]{2,}(?:[._][a-z]+)*\b/g;

describe("a failure card", () => {
  for (const kind of CAUSES) {
    it(`names what could not be read, not a code name — ${kind}`, async () => {
      const root = mount(activate, host(SUPERVISOR, { board: new HostCallError({ kind, message: "the test asked" }) }));
      await settle();
      // Everything a person reads on the card except the refusal note, which is the shared surface's and still
      // names the permission — measured below, and reported rather than changed here.
      const note = root.querySelector(".fail-note");
      note?.remove();
      const text = readableText(root);
      expect(text, "the card was drawn").toContain("Asked for");
      expect([...text.matchAll(CODE_NAME)].map((m) => m[0]), kind).toEqual([]);
      // What it says instead is the screen's own words for the thing, the ones its sentences already use.
      expect(root.querySelector(".fail-facts")?.textContent).toContain("the board");
    });
  }

  it("keeps the code name for support, on the line the copy action writes", async () => {
    const root = mount(activate, host(SUPERVISOR, { board: new HostCallError({ kind: "internal", message: "the test asked" }) }));
    await settle();
    const copy = [...root.querySelectorAll("button")].find((b) => b.textContent?.trim() === "Copy these details");
    expect(copy, "a faulted card offers the copy action").toBeDefined();
    let written = "";
    Object.defineProperty(globalThis.navigator, "clipboard", { value: { writeText: (t: string) => { written = t; return Promise.resolve(); } }, configurable: true });
    copy!.click();
    await settle();
    expect(written).toContain("roomcare.read");
  });

  // THE DAY THE SURFACE CHANGED. This read the other way — "still names the permission in the refusal note" — and
  // was asserted as it stood so that the day the shared surface moved, it would say so rather than pass quietly.
  // It went red on 2026-09-23 against HosPilotOS `e330b7ca` (64h frame 3, owner 2026-09-22): the note is now
  // "This screen needs a permission, and no grant at this property names this user", and `grant()` gained the
  // `Copy these details` label so the code name still reaches whoever can act on it. Room Care drew no control on
  // `grant`, so the code name left this application's refusals with nothing put in its place; `chrome/failure.ts`
  // now draws it, and both halves are asserted together because either alone is the defect the owner named.
  it("names no code name in a refusal note, and still hands the code name to support", async () => {
    for (const kind of ["forbidden", "user_forbidden", "local_forbidden"] as const) {
      const root = mount(activate, host(SUPERVISOR, { board: new HostCallError({ kind, message: "the test asked" }) }));
      await settle();
      const note = root.querySelector(".fail-note")?.textContent ?? "";
      expect(note, kind).not.toContain("roomcare.read");
      expect([...note.matchAll(CODE_NAME)].map((m) => m[0]), kind).toEqual([]);
      const copy = [...root.querySelectorAll("button")].find((b) => b.textContent?.trim() === "Copy these details");
      expect(copy, `${kind}: a refused card offers the path to the identifier`).toBeDefined();
      let written = "";
      Object.defineProperty(globalThis.navigator, "clipboard", { value: { writeText: (t: string) => { written = t; return Promise.resolve(); } }, configurable: true });
      copy!.click();
      await settle();
      expect(written, kind).toContain("roomcare.read");
    }
  });

  it("names no code name on a widget's failure either", async () => {
    const card = await roomsReady(host(["roomcare.read"], { widgetRoomsReady: new HostCallError({ kind: "forbidden", message: "no" }) }));
    expect([...readableText(card).matchAll(CODE_NAME)].map((m) => m[0])).toEqual([]);
  });
});

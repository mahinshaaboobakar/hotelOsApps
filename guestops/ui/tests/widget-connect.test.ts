/**
 * A widget that is connected renders something — and one that is not, does not.
 *
 * # The bug this exists for
 *
 * Every widget rendered an **empty card**: styled, sized, silent. The preview
 * host was sending `{ type: "hotelos.connect", contract: 1 }` with no
 * `minContract`, and `isConnectMessage` requires **both** version fields to be
 * numbers — so the message was not recognised as a connect at all. No error, no
 * refusal, nothing in the console. The widget simply never activated.
 *
 * It was found by looking at a capture, and it could only be found that way:
 * nothing in the suite mounted a widget. **This is that gap closed.** The guard
 * is the shape of the message rather than the pixels, so it fails on the actual
 * defect and does not need a browser.
 *
 * # And the thing that made the fix look like it had failed
 *
 * After the shape was corrected the widgets still photographed empty, because
 * the probe was served a **stale cached bundle** — the artifact on disk was
 * right and the browser was running the old one. There was never a second
 * cause. A test that runs the current source has no such failure mode, which
 * is the second reason this is here rather than in a capture.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { HOST_CONTRACT_RANGE } from "@hotelos/sdk";
import { describe, expect, it, vi } from "vitest";

/** The five this application ships. */
const WIDGETS = [
  "today", "occupancy", "from-the-pms", "business-mix", "watchlist",
] as const;

/**
 * Mount a widget the way the host does, and give back what it drew.
 *
 * The bundle is imported for its side effect — a widget calls `connectToHost`
 * at module scope — so the listener is attached by the import and the connect
 * is posted after it.
 */
async function mount(name: string, message: Record<string, unknown>): Promise<string> {
  document.body.replaceChildren();

  // **The registry is reset, and without it the last test is a tautology.**
  // `connectToHost` removes its listener the moment it is connected, so a
  // second import of an already-loaded widget attaches nothing and draws
  // nothing — which is what the malformed-connect case asserts. It would then
  // pass for the wrong reason, and a mutation that made the message VALID
  // would still pass. Verified by running exactly that mutation.
  vi.resetModules();

  await import(`../widgets/entry/${name}`);

  // **The port goes through the constructor.** `MessageEvent.ports` is a
  // readonly getter, so `Object.assign`ing it silently does nothing and the SDK
  // sees no port, rejects, and renders nothing — which looks exactly like the
  // bug this test is for. The first draft did that and accused the widgets.
  const channel = new MessageChannel();
  answer(channel.port1);

  window.dispatchEvent(new MessageEvent("message", {
    data: message,
    ports: [channel.port2],
  }));

  // The connect resolves, the SDK mounts, and the widget then *asks* — and
  // waits. A host that took the port and answered nothing would leave every
  // widget mid-render forever, which is what the second draft of this test did
  // and what it wrongly read as the widgets failing to draw.
  await new Promise((resolve) => setTimeout(resolve, 30));

  return document.body.textContent ?? "";
}

/**
 * Refuse whatever the widget asks, the way the preview host refuses it.
 *
 * A refusal rather than data on purpose: every widget has to draw its recorded
 * facts and say so when the platform cannot answer, and that is the path a
 * property sees today. A test that fed them real data would exercise the one
 * branch that is not currently reachable.
 */
function answer(port: MessagePort): void {
  port.addEventListener("message", (event: MessageEvent) => {
    const message = event.data as { type?: string; id?: number };

    if (message.type !== "hotelos.call") return;

    port.postMessage({
      type: "hotelos.result",
      id: message.id,
      ok: false,
      error: { kind: "unavailable", message: "no client yet" },
    });
  });

  port.start();
}

/** What the host sends when it is sending it properly. */
function connect(): Record<string, unknown> {
  return {
    type: "hotelos.connect",
    contract: HOST_CONTRACT_RANGE.current,
    minContract: HOST_CONTRACT_RANGE.min,
    module: { id: "guestops", version: "0.1.0", capabilities: ["reservation.read"] },
    property: { timezone: null, locale: null },
  };
}

describe("a connected widget", () => {
  it.each(WIDGETS)("%s draws content when the connect carries both versions", async (name) => {
    const drawn = await mount(name, connect());

    expect(drawn.trim().length).toBeGreaterThan(20);
  });

  /**
   * The exact message that produced five empty cards.
   *
   * Asserted as a **non-render** rather than as a thrown error, because that is
   * what it was: `isConnectMessage` returns false, the listener ignores the
   * message, and nothing anywhere says so. The silence is the defect, and the
   * only way to catch it is to look at what was drawn.
   */
  it("draws nothing at all when minContract is missing — the original bug", async () => {
    const { minContract, ...incomplete } = connect();
    void minContract;

    const drawn = await mount("today", incomplete);

    expect(drawn.trim()).toBe("");
  });
});

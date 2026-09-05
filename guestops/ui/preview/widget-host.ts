import { HOST_CONTRACT_RANGE } from "@hotelos/sdk";
/**
 * The five widgets, side by side, at the popover's size.
 *
 * A capture harness rather than a runtime: it speaks the host half of the wire
 * directly — the same choice `bundle.test.ts` made and for the same reason. The
 * shell's widget host lives in the platform repository and is the *enforcing*
 * side; reaching across for it would put the enforcement inside the thing being
 * enforced, and this needs only to be a faithful correspondent.
 *
 * It grants `reservation.read` and refuses everything else, so a tap-through
 * shows its refusal rather than appearing to work — which is the behaviour the
 * design names as the one outcome worse than a tap that says why.
 */

// A module, so top-level `await` is legal — the file has no imports of its
// own and would otherwise be a script.
export {};

const WIDGETS = [
  "today", "occupancy", "from-the-pms", "business-mix", "watchlist",
] as const;

const TOKENS = await (await fetch("./tokens.css")).text();

/** The base rules the real realm writes into a widget's document. */
const BASE = `<style>${TOKENS}
  :root { background: var(--color-surface-raised); color: var(--color-ink); }
  * { box-sizing: border-box; }
  body { margin: 0; font-family: var(--font-sans); background: transparent; color: inherit; }
</style>`;

// `?only=<id>` draws one card, so a capture pairs with a single artboard.
const only = new URLSearchParams(location.search).get("only");
const drawn = only === null ? WIDGETS : WIDGETS.filter((w) => w === only);

for (const name of drawn) {
  const bundle = await (await fetch(`../widgets/${name}.js`)).text();

  const figure = document.createElement("figure");
  const caption = document.createElement("figcaption");
  caption.textContent = name;

  const frame = document.createElement("iframe");
  frame.srcdoc = `<!doctype html><html><head><meta charset="utf-8">${BASE}</head>`
    + `<body><script type="module">${bundle}<\/script></body></html>`;

  figure.append(caption, frame);
  document.getElementById("row")?.append(figure);

  frame.addEventListener("load", () => {
    const channel = new MessageChannel();

    channel.port1.addEventListener("message", (event: MessageEvent) => {
      const message = event.data as { type?: string; id?: number; capability?: string };
      if (message.type !== "hotelos.call") return;

      // Only `reservation.read` is granted. `shell.open` is refused, so a tap
      // shows why rather than looking like it worked.
      channel.port1.postMessage(message.capability === "reservation.read"
        ? { type: "hotelos.result", id: message.id, ok: false,
            error: { kind: "unavailable", message: "no client yet" } }
        : { type: "hotelos.result", id: message.id, ok: false,
            error: { kind: "rejected", message: "the desk is not open in this harness" } });
    });

    channel.port1.start();

    frame.contentWindow?.postMessage({
      type: "hotelos.connect",
      // Both halves, from the SDK — `isConnect` requires `minContract` to be a
      // number, and a message without it is not recognised as a connect at all.
      // Sent as a literal `1`, every widget in this harness rendered an EMPTY
      // CARD: connected to nothing, styled correctly, and silent about why.
      // Only a capture shows that; a suite that does not mount widgets cannot.
      contract: HOST_CONTRACT_RANGE.current,
      minContract: HOST_CONTRACT_RANGE.min,
      module: { id: "guestops", version: "0.1.0", capabilities: ["reservation.read"] },
      property: { timezone: null, locale: null },
    }, "*", [channel.port2]);
  });
}

setTimeout(() => document.documentElement.setAttribute("data-ready", "true"), 900);

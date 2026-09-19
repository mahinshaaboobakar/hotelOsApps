import { HOST_CONTRACT_RANGE } from "@hotelos/sdk";

import * as recorded from "../widgets/recorded";
import { statedProperty } from "./property";
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

/**
 * `?fail=<cause>` — which of contract v2's six causes the read returns, by the
 * host kind the SDK maps to it (`64b`, `64e`).
 *
 * **With no `?fail=`, the widget LOADS** — the checklist's `W` asks for every
 * widget loaded as well as failing, and until 2026-09-19 this harness answered
 * every read with a failure, so no loaded card had been photographed here. The
 * answers are `widgets/recorded.ts`, the approved artboards' figures, keyed by
 * the method each widget asks for.
 */
const KINDS = {
  unanswered: "unavailable",
  forbidden: "forbidden",
  unadmitted: "local_forbidden",
  ungranted: "user_forbidden",
  undecidable: "model_unavailable",
  faulted: "internal",
} as const;
const cause = new URLSearchParams(location.search).get("fail");
const kind = cause === null ? null : KINDS[cause as keyof typeof KINDS] ?? null;
if (cause !== null && kind === null) {
  throw new Error(`no cause '${cause}' — the harness knows ${Object.keys(KINDS).join(", ")}`);
}

/** What each widget reads, loaded. */
const LOADED: Record<string, unknown> = {
  desk: recorded.desk,
  occupancy: recorded.occupancy,
  feed: recorded.pms,
  mix: recorded.mix,
  watchlist: recorded.watchlist,
};

for (const name of drawn) {
  const bundle = await (await fetch(`../widgets/${name}.js`)).text();

  const figure = document.createElement("figure");
  const caption = document.createElement("figcaption");
  caption.textContent = name;

  const frame = document.createElement("iframe");
  // The error hook goes in FIRST, as a classic script, so anything the module
  // throws while parsing or connecting is captured rather than lost inside a
  // realm nobody can read. A widget that fails silently photographs as an empty
  // card, which is the one outcome worse than a refusal.
  frame.srcdoc = `<!doctype html><html><head><meta charset="utf-8">${BASE}`
    + `<script>window.__errs=[];`
    + `addEventListener("error",e=>window.__errs.push(String(e.message||e.error)));`
    + `addEventListener("unhandledrejection",e=>window.__errs.push("reject: "+String(e.reason)));`
    + `<\/script></head>`
    + `<body><script type="module">${bundle}<\/script></body></html>`;

  figure.append(caption, frame);
  document.getElementById("row")?.append(figure);

  frame.addEventListener("load", () => handshake(frame, ["reservation.read"]));
}

/**
 * Post the handshake until the widget answers.
 *
 * # The race this defends against
 *
 * **A `srcdoc` iframe's `<script type="module">` is deferred**, so the frame's
 * `load` can fire before the bundle has run and before `connectToHost` has a
 * listener. A connect posted at that moment lands in a realm that is not
 * listening and is dropped — no error, no refusal, an empty card and nothing to
 * read. The port is transferable once, so the retry re-posts with a **fresh**
 * channel each time until `hotelos.ready` comes back.
 *
 * # What actually caused the blank cards, and what did not
 *
 * The confirmed cause was the **connect's shape**: `isConnect` requires
 * `minContract` to be a number, and this harness sent `contract: 1` with no
 * `minContract`. The message was therefore not recognised as a connect at all,
 * and every widget rendered styled, empty and silent. Only a capture shows
 * that; a suite that does not mount widgets cannot.
 *
 * **This retry is a defence, not a second diagnosis.** It was added on the
 * theory that the race above was also firing, and there is no evidence it ever
 * was: with the connect shape corrected, the widgets render whether or not the
 * retry is needed. It stays because the race is real and cheap to close, and
 * this comment says so rather than claiming a cause it cannot show.
 *
 * # The reading that made the fix look like it had failed
 *
 * After the shape was corrected, a probe still reported empty realms — because
 * it was served from a **port whose earlier bundle was cached**. The artifact
 * on disk was correct and the browser was running the old one. Rebuilt and
 * served fresh, all five widgets render with content and zero captured errors.
 * *The file is not the state; what was served is* — and the error hook below
 * is what made the difference visible, by reporting `window.__errs` as absent
 * rather than as empty.
 */
function handshake(frame: HTMLIFrameElement, capabilities: readonly string[]): void {
  let settled = false;

  const open = (): void => {
    if (settled) return;

    const channel = new MessageChannel();

    channel.port1.addEventListener("message", (event: MessageEvent) => {
      const message = event.data as {
        type?: string; id?: number; capability?: string; method?: string;
      };

      if (message.type === "hotelos.ready") {
        settled = true;
        return;
      }

      if (message.type !== "hotelos.call") return;

      // Anything but the widgets' own read is refused, so a tap-through shows
      // its refusal rather than appearing to work. An unknown method is refused
      // too — a harness answers what it holds, and never a stand-in (§8, H5).
      const known = message.method !== undefined && message.method in LOADED;
      channel.port1.postMessage(message.capability !== "reservation.read"
        ? { type: "hotelos.result", id: message.id, ok: false,
            error: { kind: "rejected", message: "the desk is not open in this harness" } }
        : kind !== null
          ? { type: "hotelos.result", id: message.id, ok: false,
              error: { kind, message: `the harness failed this read as ${kind}` } }
          : known
            ? { type: "hotelos.result", id: message.id, ok: true,
                value: LOADED[message.method as string] }
            : { type: "hotelos.result", id: message.id, ok: false,
                error: { kind: "rejected", message: `no answer held for ${message.method}` } });
    });

    channel.port1.start();

    frame.contentWindow?.postMessage({
      type: "hotelos.connect",
      contract: HOST_CONTRACT_RANGE.current,
      minContract: HOST_CONTRACT_RANGE.min,
      module: { id: "guestops", version: "0.1.0", capabilities },
      property: statedProperty(),
    }, "*", [channel.port2]);
  };

  for (let attempt = 0; attempt < 20; attempt += 1) {
    setTimeout(open, attempt * 50);
  }
}

// `data-review-ready` beside the private flag, as `frame.ts` sets both — the
// shared instruments wait on that one, and a page setting only its own is a
// page they photograph on a timeout.
setTimeout(() => {
  document.documentElement.setAttribute("data-ready", "true");
  document.documentElement.setAttribute("data-review-ready", "true");
}, 900);

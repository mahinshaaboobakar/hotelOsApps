/**
 * How a widget mounts, draws, and draws again.
 *
 * **This is what is left of `answer.ts`, and what was removed is the point.**
 * That file carried a second copy of the read seam — `Answer<T>` and `read` —
 * and justified it in as many words: importing the module's seam would pull the
 * whole reservation book, every screen's shapes and fixtures, into a card that
 * shows four numbers. That was true and it is no longer the choice on offer.
 * `38c5855e` moved the seam to `@hotelos/sdk`, which carries no book, so a
 * widget now imports `load` directly and there is one definition in the estate
 * instead of three.
 *
 * What the duplicate would have cost is not the happy path. Both copies agreed
 * about success and disagreed about **what they refuse to say**: this one
 * collapsed a refusal, a timeout and a fault into one sentence, so a widget
 * offered no way to tell a grant that was never made from a service that was
 * briefly down — and a person seeing *could not reach the platform* on a card
 * waits for something that is never coming.
 *
 * One file, one purpose: this one mounts. The reading is the SDK's.
 */

import type { HostApi } from "@hotelos/sdk";

/**
 * Mount a widget, draw it, and draw it again when the shell says so.
 *
 * **Read on mount, without waiting for a tick.** The first `refresh` means
 * *again*; a widget that only drew on `refresh` would show an empty frame until
 * the first interval elapsed, which is the design's stated failure.
 *
 * @param host the bridge
 * @param root the element this widget owns
 * @param draw reads and renders; called on mount and on every refresh
 * @returns the unsubscribe, for the module's `unmount`
 */
export function serve(
  host: HostApi,
  root: HTMLElement,
  draw: (root: HTMLElement) => void,
): () => void {
  draw(root);
  return host.on("refresh", () => draw(root));
}

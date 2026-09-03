/**
 * A widget is a question answered — this is how it asks.
 *
 * The module's seam (`book/index.ts`) is not reused here on purpose. A widget
 * is its own bundle and resolves nothing at load, so importing the module's
 * seam would pull the whole reservation book — every screen's shapes and
 * fixtures — into a card that shows four numbers. Same rule, its own small
 * implementation, and the rule is what matters: **one place per bundle that
 * talks to the host.**
 *
 * The refusal handling is the module's, because ADR 0041 does not change
 * between surfaces: `internal` and `forbidden` carry a message for a log, and
 * putting one on a hotel's screen leaks a platform diagnostic to a receptionist.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

/** What a widget got, and whether it is the property's own. */
export interface Answer<T> {
  value: T;

  /**
   * True when this came from the platform.
   *
   * The card says so when it is false. A widget showing plausible numbers that
   * are nobody's is worse than one showing nothing, because it looks current —
   * the same reason the design refuses a figure from the last time it was
   * opened.
   */
  live: boolean;
}

/**
 * Ask the platform, and fall back to the canvas's own numbers.
 *
 * @param host the bridge, and the only route out of this realm
 * @param capability the permission the manifest requested
 * @param method the question within it
 * @param recorded what to show when the platform cannot answer
 * @returns the answer, and whether it is real
 */
export async function read<T>(
  host: HostApi,
  capability: string,
  method: string,
  recorded: T,
): Promise<Answer<T>> {
  // A capability the package was not granted is not worth a round trip, and its
  // refusal would read as an outage rather than as a permission a property
  // chose not to give.
  if (!host.identity.capabilities.includes(capability)) {
    return { value: recorded, live: false };
  }

  try {
    return { value: (await host.call(capability, method)) as T, live: true };
  } catch (error) {
    if (error instanceof HostCallError) return { value: recorded, live: false };
    throw error;
  }
}

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

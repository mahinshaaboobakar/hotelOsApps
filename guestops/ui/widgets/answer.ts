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

/**
 * What a widget's read returned: the property's figures, or why there are none.
 *
 * **A union, so the canvas cannot hold a stand-in** — `APPS-Q42`. This was
 * `{ value, live }` with recorded numbers in `value` and a footnote saying they
 * were examples, and the ruling covers widgets for the reason the footnote
 * could not answer: a widget is the frame most likely to be glanced at and
 * believed, because nobody opens one to interrogate it. Plausible numbers
 * belonging to nobody are worse in 320×384 than on a page, not better.
 */
export type Answer<T> =
  | { readonly ok: true; readonly value: T }
  | { readonly ok: false; readonly because: string };

/**
 * Ask the platform.
 *
 * @param host the bridge, and the only route out of this realm
 * @param capability the permission the manifest requested
 * @param method the question within it
 * @param params the widget's own body, where it has one
 * @returns the figures, or why there are none
 *
 * **No `recorded` parameter**, and its absence is the mechanism: a widget has
 * nothing to fall back to because it is handed nothing to fall back to.
 */
export async function read<T>(
  host: HostApi,
  capability: string,
  method: string,
  params?: Record<string, unknown>,
): Promise<Answer<T>> {
  if (!host.identity.capabilities.includes(capability)) {
    return { ok: false, because: `This property has not granted ${capability} to GuestOps.` };
  }

  try {
    return { ok: true, value: (await host.call(capability, method, params)) as T };
  } catch (error) {
    if (error instanceof HostCallError) {
      return {
        ok: false,
        because: error.isForPeople
          ? error.message
          : "GuestOps could not reach the platform.",
      };
    }

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

/**
 * Joining one widget panel to the host that loaded it.
 *
 * The widget half of `main.ts`. A widget bundle is inlined into its realm as a
 * module script and **nothing imports it**, so a bundle that only exports
 * evaluates, defines a function nobody holds, and mounts nothing — with no
 * error anywhere. Each entry calls this, and this calls `connectToHost`.
 *
 * # One file, five entries
 *
 * Five bundles need five entry points, because esbuild writes one output per
 * entry. What they have in common is here, so an entry is three lines that name
 * a panel and nothing else — and a change to how a widget is served is one
 * edit rather than five.
 *
 * # A panel is async and `Activate` is not
 *
 * The SDK's contract is synchronous: `activate` returns something with
 * `mount(root)`, and the host posts `ready` the moment mount returns. A panel
 * awaits the seam before it can draw anything, so mount starts the draw and
 * returns — the same shape `application.ts` uses for a screen. The card
 * appears a tick later, inside a realm the shell keeps hidden until `ready`.
 */

import { connectToHost, type HostApi } from "@hotelos/sdk";

import { stylesheet } from "./card";

/** What a widget's panel does: read, and return its card. */
export type Panel = (host: HostApi) => Promise<HTMLElement>;

/**
 * Serve one panel to whatever host connects.
 *
 * @param panel the widget's own drawing
 */
export function serve(panel: Panel): void {
  connectToHost((host) => {
    let stopListening: (() => void) | null = null;

    return {
      mount(root: HTMLElement): void {
        const draw = (): void => {
          void panel(host).then((element) => {
            // The sheet is attached here rather than by each panel: five copies
            // of one line is five places to forget it, and a widget that forgot
            // it draws an unstyled column that no suite can see.
            //
            // Replaced rather than appended: a refresh redraws the card, and
            // appending would leave the previous one above it — the shape of
            // bug that only shows after the second read.
            root.replaceChildren(stylesheet(), element);
          });
        };

        // **Read on mount, without waiting for a tick.** The first `refresh` is
        // the shell saying *again*; a widget that only drew on the event would
        // show an empty frame until the first interval elapsed.
        draw();

        // The shell publishes it on open and on a modest interval while the
        // popover is visible, and never while dismissed — a closed widget's
        // port is closed, so a tick that races a dismissal is dropped rather
        // than being a case to guard.
        stopListening = host.on("refresh", () => {
          draw();
        });
      },

      unmount(): void {
        // Nothing survives dismissal. The host discards the realm regardless
        // and does not wait for this — it exists so a well-behaved widget
        // releases what it holds, not so the host can rely on one.
        stopListening?.();
        stopListening = null;
      },
    };
  }).catch((error: unknown) => {
    // A handshake that never completes is invisible from outside the realm:
    // the shell keeps the frame hidden until `ready` and sets no deadline, so
    // a widget that rejects here is a blank popover with nothing said anywhere.
    console.error(
      "Workforce widget could not join the HotelOS shell:",
      error instanceof Error ? error.message : error,
    );
  });
}

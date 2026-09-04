/**
 * How a widget joins the shell — one `connectToHost`, one panel, redrawn on
 * the host's `refresh`. Every widget entry is this call with its panel.
 */

import { connectToHost, type HostApi } from "@hotelos/sdk";

import { stylesheet } from "./card";

/** A widget's whole content, drawn from the host. */
export type Panel = (host: HostApi) => Promise<HTMLElement>;

export function serve(panel: Panel): void {
  connectToHost((host) => {
    let stopListening: (() => void) | null = null;
    return {
      mount(root: HTMLElement): void {
        const draw = (): void => {
          void panel(host).then((element) => {
            root.replaceChildren(stylesheet(), element);
          });
        };
        draw();
        stopListening = host.on("refresh", () => {
          draw();
        });
      },
      unmount(): void {
        stopListening?.();
        stopListening = null;
      },
    };
  }).catch((error: unknown) => {
    console.error("Jobs widget could not join the HotelOS shell:", error instanceof Error ? error.message : error);
  });
}

/**
 * One widget bundle's life — join the shell, draw the panel, redraw when the
 * shell says refresh, stop listening when unmounted (page 56).
 */

import { connectToHost, type HostApi } from "@hotelos/sdk";

import { stylesheet } from "./card";

export type Panel = (host: HostApi) => Promise<HTMLElement>;

export function serve(panel: Panel): void {
  connectToHost((host) => {
    let stopListening: (() => void) | null = null;
    return {
      mount(root: HTMLElement): void {
        const draw = (): void => {
          void panel(host).then((element) => root.replaceChildren(stylesheet(), element));
        };
        draw();
        stopListening = host.on("refresh", draw);
      },
      unmount(): void {
        stopListening?.();
        stopListening = null;
      },
    };
  }).catch((error: unknown) => {
    console.error("A Room Care widget could not join the HotelOS shell:", error instanceof Error ? error.message : error);
  });
}

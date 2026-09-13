/**
 * Room Care's UI entry — join the shell, and hand it the module.
 */

import { connectToHost } from "@hotelos/sdk";

import activate from "./application";

connectToHost(activate).catch((error: unknown) => {
  console.error("Room Care could not join the HotelOS shell:", error instanceof Error ? error.message : error);
});

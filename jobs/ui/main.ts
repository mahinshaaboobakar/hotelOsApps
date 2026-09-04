/**
 * The package's entry point: join this module to the host that loaded it.
 *
 * `SHELL-Q32` requires `ui/module.js` to be self-contained and to call
 * `connectToHost` at the top level — a bundle that merely exports `activate`
 * registers no listener and `hotelos.ready` never posts. `application.ts` is
 * the composition root the tests and the capture harness import; this file is
 * the one the shared pipeline (`scripts/build-module.mjs`) bundles.
 */

import { connectToHost } from "@hotelos/sdk";

import activate from "./application";

connectToHost(activate).catch((error: unknown) => {
  console.error(
    "Jobs could not join the HotelOS shell:",
    error instanceof Error ? error.message : error,
  );
});

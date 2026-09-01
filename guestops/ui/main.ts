/**
 * The package's entry point — what `ui/module.js` is built from.
 *
 * # Why this is a second file
 *
 * `module.ts` *exports* `activate`. That is what the tests and the preview
 * harness want: they construct their own host and call it directly. It is not
 * what the platform wants.
 *
 * The realm inlines the bundle as `<script type="module">…</script>`, and
 * **nothing imports it**. A bundle that only exports `activate` therefore
 * evaluates, defines a function nobody holds, and mounts nothing — the frame
 * stays empty and no error is raised anywhere. The module has to start itself,
 * which is what `connectToHost` is for: it waits for the host's handshake,
 * takes the port, and mounts.
 *
 * Splitting the two means the harness can drive `activate` without a handshake
 * that will never arrive, and the shipped bundle connects without the harness
 * having to pretend to be a host at import time.
 *
 * **This is worth knowing before writing the second package**: `hello-hotel`,
 * the only shipped example of the contract, exports `activate` and never calls
 * `connectToHost`. Copying it produces a module that loads and does nothing.
 */

import { connectToHost } from "@hotelos/sdk";

import { activate } from "./module";

// Fire and forget: the promise resolves when the host has handshaken and the
// module is mounted, and there is nobody here to hand it to. A rejection is
// the host's to report — a package cannot draw its own failure to connect on a
// surface it was never given.
void connectToHost(activate);

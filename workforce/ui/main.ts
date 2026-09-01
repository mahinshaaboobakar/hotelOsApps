/**
 * The package's entry point: join this module to the host that loaded it.
 *
 * # Why this is not `module.ts`
 *
 * `SHELL-Q32` requires `ui/module.js` to be **self-contained** — no import the
 * host must resolve — and ADR 0101 defect 22's sibling finding is that a bundle
 * which merely *exports* `activate` and never calls `connectToHost` registers
 * no listener: the handshake reaches nobody and `hotelos.ready` can never post.
 * The shipped bundle therefore needs a top-level call, and this file is it.
 *
 * It is separate from `module.ts` because calling `connectToHost` at import
 * time is exactly wrong for every other consumer. The capture harness fakes the
 * host and activates the module itself; the test suite activates it with a
 * double. Both import `module.ts`, and neither wants a `message` listener
 * attached to their window as a side effect of importing a composition root.
 *
 * ```text
 * module.ts   what this module IS      exports activate — imported by tests
 *                                      and the harness
 * main.ts     how it REACHES a host    the packaged entry, bundled to
 *                                      ui/module.js and nothing else
 * ```
 *
 * **The name is `scripts/build-module.mjs`'s, not this application's.** That
 * script bundles `main.ts` and refuses to leave a bundle on disk that does not
 * self-start; a second spelling here would be one application's entry point
 * called something the shared pipeline does not build.
 */

import { connectToHost } from "@hotelos/sdk";

import activate from "./module";

/**
 * A handshake that never completes is invisible from outside the realm.
 *
 * The shell keeps the frame hidden until `hotelos.ready` arrives and sets no
 * deadline, so a module that rejects here renders as a blank pane with nothing
 * said anywhere. The realm's console is the one channel this side owns, so the
 * failure is named there rather than swallowed — ADR 0124's rule applied to the
 * only surface a package has.
 */
connectToHost(activate).catch((error: unknown) => {
  console.error(
    "Workforce could not join the HotelOS shell:",
    error instanceof Error ? error.message : error,
  );
});

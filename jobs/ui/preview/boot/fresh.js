/**
 * Load a harness bundle at a tag derived from its own bytes — `ARCH-Q25`.
 *
 * # Adopted, not written here — GG's `d7d084a`, copied verbatim
 *
 * This file is a COPY of `workforce/ui/preview/boot/fresh.js`, taken on
 * 2026-09-10 under `ARCH-Q26`. Jobs' harness loaded its bundle with a plain
 * `<script type="module" src="./frame.js">` — no buster at all, not even the
 * unused parameter GG's had — and this stream has been capturing all day.
 *
 * **The copy is deliberate and it is not free.** `INSTALL-Q88` asks where
 * shared application test-support is to live, and until that is answered a
 * third application cannot import a second application's file: an app is
 * installable on its own and may not reach into a sibling's tree. So the
 * choice was a copy or a wrong dependency, and the copy is labelled here
 * rather than left to look like independent invention. When Q88 answers, this
 * file is deleted in favour of whatever it names — that is the point of saying
 * where it came from.
 *
 * Everything below is GG's, including the two defects their round found and
 * fixed: `fetch` and `import()` resolving `./` against different bases, and the
 * refusal path that must not degrade to an untagged import.
 *
 * # The trap this replaces
 *
 * `frame.html` imported `./frame.js?build=` + a query parameter, defaulting to
 * the empty string, with a comment saying in as many words that *without it a
 * capture pass photographs the previous build*. **Nothing in either repository
 * ever passed that parameter** — so the module URL was constant, the browser
 * cached it, and the default was the trap the comment described.
 * `widgets.html` had no buster at all.
 *
 * **A stale capture is indistinguishable from a correct one.** It is a real
 * screen, rendered properly, of code that no longer exists — and a capture is
 * evidence. That is why this derives rather than documents: the comment was
 * already there, and it was read, and the trap was still fallen into twice in
 * one afternoon by the person who had just read it.
 *
 * # Why the bytes rather than a timestamp
 *
 * `Last-Modified` is a header a server may not send, and the file's mtime moves
 * on a rebuild that changes nothing. The digest of what was actually served is
 * the one tag that is true whatever the server does and whatever the clock
 * says, and it makes the module URL identical when the bundle is identical, so
 * the browser's cache still does its job on an unchanged build.
 *
 * # Refused rather than degraded
 *
 * If the bundle cannot be read, or the platform cannot hash it, this throws
 * with a sentence naming what was missing. **There is no fallback to an
 * un-busted import**, because that is the state this exists to make
 * unreachable — `INSTALL-Q88`'s test: refusing a value that was never
 * legitimate is not a narrowing, and there is no run in which somebody wants
 * yesterday's bundle photographed.
 *
 * This file is under `preview/boot/` because `preview/*.js` is git-ignored for
 * the generated bundles, and a loader written beside them would have been
 * silently uncommitted — the harness working on one machine and absent on
 * every other, which is the shape of the defect it fixes.
 */

/**
 * Import `url` at a tag taken from the bytes the server just returned.
 *
 * @param {string} url the bundle, relative to the harness page
 * @returns {Promise<unknown>} the module namespace
 */
export async function fresh(url) {
  // **Resolved against the PAGE, once, for both halves.**
  //
  // `fetch("./frame.js")` resolves against the document's base URL;
  // `import("./frame.js")` resolves against the URL of the module doing the
  // importing — which is this file, under `boot/`. The first version passed the
  // same relative string to both, so it hashed `/frame.js` and then tried to
  // import `/boot/frame.js`, and the harness did not load at all.
  //
  // It was found by running it. Five unit tests were green, and a guard that
  // correctly refuses both of the old harness pages was green, because every
  // one of them reads the *source*: the two functions disagreeing about what
  // `./` means is not visible in any text this module could check.
  const target = new URL(url, document.baseURI);

  // `no-store` rather than `reload`: reload revalidates and may still be
  // answered from cache on a 304, and what is wanted here is the bytes.
  const response = await fetch(target, { cache: "no-store" });

  if (!response.ok) {
    throw new Error(
      `harness: ${target.href} could not be read (${response.status}). ` +
      `Nothing is photographed rather than photographing a stale bundle.`);
  }

  const bytes = await response.arrayBuffer();

  if (globalThis.crypto?.subtle === undefined) {
    throw new Error(
      "harness: this context has no SubtleCrypto, so the bundle's tag cannot " +
      "be derived. Serve the harness from 127.0.0.1 or localhost, which is a " +
      "secure context. Refusing rather than importing an untagged bundle.");
  }

  const digest = await crypto.subtle.digest("SHA-256", bytes);
  const tag = [...new Uint8Array(digest)]
    .slice(0, 8)
    .map((byte) => byte.toString(16).padStart(2, "0"))
    .join("");

  // Set on the resolved URL rather than concatenated, so a bundle path that
  // already carries a query is not turned into `a.js?x=1?b=…`.
  target.searchParams.set("b", tag);

  return import(target.href);
}

/**
 * Refuse a caller that still passes `?build=`.
 *
 * The parameter no longer does anything, and a parameter that silently does
 * nothing is worse than one that is gone: whoever passes it believes they are
 * controlling which bundle is photographed. Nobody passes it today — that was
 * measured across both repositories before it was removed — so this fires only
 * for a caller written against the old contract.
 *
 * @param {URLSearchParams} params the harness page's own query
 */
export function refuseBuildParam(params) {
  if (params.has("build")) {
    throw new Error(
      "harness: ?build= is gone. The bundle's tag is derived from its own " +
      "bytes, so there is nothing to pass and nothing to forget.");
  }
}

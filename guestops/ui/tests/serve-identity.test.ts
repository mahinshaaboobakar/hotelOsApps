// @vitest-environment node

/**
 * The harness proves it is the one answering.
 *
 * **The rule this encodes, and the two failures behind it.** A port is a
 * cross-stream shared resource. Two preview harnesses bound 8853 on 2026-09-10
 * and one stream's browser was answered by another's; and a sweep of mine on
 * 8834 was answered by another stream's server while my own — right files,
 * right port, wildcard binding — never saw the request. I recorded that second
 * one as *"started from the wrong root"*, which was wrong.
 *
 * A status code cannot tell any of it apart. `200` says something answered, not
 * that the thing that answered was yours. So {@link serve} asserts the page's
 * own `<title>` before it returns, and these tests hold it to that — including
 * the case where the server *is* mine and the root is not, which is the one a
 * status check waves through as a 404 nobody reads.
 *
 * `node`, not `happy-dom`: this is the only suite here that binds a socket.
 */

import { fileURLToPath } from "node:url";

import { describe, expect, it } from "vitest";

// Dynamic, because the module is plain JS and this project's `tsc --noEmit`
// runs without `allowJs`. The URL is resolved against this file, not the cwd.
const { serve } = (await import(
  new URL("../preview/parta/serve.mjs", import.meta.url).href
)) as {
  serve: (options: {
    root: string;
    path: string;
    title: string;
    preferred?: number;
  }) => Promise<{ origin: string; port: number; close: () => Promise<void> }>;
};

// `fileURLToPath`, never `pathname` — a URL keeps the percent-encoding, so a
// path holding a space arrives as `Mahin%20Aboobakker` and resolves to
// nothing. The first run of this file failed exactly there, which is the
// identity assertion catching its own caller.
const HARNESS = {
  root: fileURLToPath(new URL("../preview", import.meta.url)),
  path: "/index.html",
  title: "GuestOps — preview harness",
};

describe("a harness is verified by identity", () => {
  it("serves the page whose title it was told to expect", async () => {
    const it_ = await serve(HARNESS);

    try {
      expect(it_.origin).toMatch(/^http:\/\/127\.0\.0\.1:\d+$/u);

      const page = await (await fetch(`${it_.origin}/index.html`)).text();
      expect(page).toContain(HARNESS.title);
    } finally {
      await it_.close();
    }
  });

  it("refuses a root that does not hold the page, though the server is ours", async () => {
    // The discarded run, reproduced: nothing is wrong with the server and
    // everything is wrong with what it can reach. A sweep pointed here reads
    // 404 bodies as content and closes its columns over them.
    await expect(serve({ ...HARNESS, root: `${HARNESS.root}/parta` }))
      .rejects.toThrow(/not served from this root/u);
  });

  it("says what it measured when it refuses", async () => {
    // A guard's failure message is a claim about the world and may claim only
    // what the guard measured — the condition the manifest-routing check was
    // landed under, applied to this one.
    const failure = await serve({ ...HARNESS, title: "something else" })
      .then(() => null, (error: Error) => error.message);

    expect(failure).toContain("127.0.0.1");
    expect(failure).toContain('"something else"');
    expect(failure).toContain("A status code cannot tell those apart");
  });
});

describe("two harnesses never converge on one number", () => {
  it("gives concurrent starts distinct ports", async () => {
    // Port 0 rather than a number anybody chose. Incrementing from where you
    // last were is how two streams pick the same one, and it is what I did:
    // 8830 through 8835, one per sweep, into somebody else's 8834.
    const all = await Promise.all([serve(HARNESS), serve(HARNESS), serve(HARNESS)]);

    try {
      expect(new Set(all.map((one) => one.port)).size).toBe(3);
    } finally {
      await Promise.all(all.map((one) => one.close()));
    }
  });

  it("probes upward when a person asked for a fixed port", async () => {
    const held = await serve({ ...HARNESS, preferred: 8871 });
    const next = await serve({ ...HARNESS, preferred: 8871 });

    try {
      expect(held.port).toBe(8871);
      expect(next.port).toBeGreaterThan(8871);
    } finally {
      await held.close();
      await next.close();
    }
  });
});

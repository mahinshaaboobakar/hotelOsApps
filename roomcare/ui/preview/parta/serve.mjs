// # Adopted, not written here — a COPY of guestops/ui/preview/parta/serve.mjs at 539daf4
//
// Copied verbatim on 2026-09-19 for Room Care's Part A, as Jobs copied serve.mjs:
// an application is installable on its own and may not reach into a
// neighbour's tree, so until ADR 0154's released test-support package exists
// the choice is a copy or a wrong dependency. When that package lands this file
// is deleted in favour of it — which is what this label is for.
//
// Serve a directory to the sweep, and prove the answer came from this server.
//
// **A port is a cross-stream shared resource, like the index and the working
// tree.** Two preview harnesses bound 8853 on 2026-09-10 and one stream's
// browser was answered by another's — GG reported it, and the same thing had
// already happened here without being recognised: a run of mine on 8834 was
// answered by another stream's server, and I diagnosed it as *"started from the
// wrong root"*. My root was right. My server never saw the request.
//
// ```text
// pid 17820  ::         :8834   http.server 8834                     mine
// pid 11460  127.0.0.1  :8834   http.server 8834 --bind 127.0.0.1    not mine
// ```
//
// A bare `http.server` binds the wildcard. Windows routes an IPv4 loopback
// connection to the *specific* 127.0.0.1 binding, so every request went next
// door and came back 404 — with a 200-shaped server of my own running the whole
// time, three feet away, holding the right files.
//
// The owner's three rules, and how each becomes a mechanism here rather than a
// habit somebody has to remember:
//
// **1 · Verify by identity, never by status code.** `curl → 200` cannot
// distinguish a server from *your* server. So {@link serve} does not return
// until it has fetched a page and matched that page's own `<title>`. This also
// catches the failure one level in: a server of mine, on a port of mine,
// rooted at the wrong directory answers `<title>Error response</title>` — which
// is the run I threw away last week and misattributed.
//
// **2 · Probe upward for a free port; never increment from where you last
// were.** That is how two streams pick the same number, and it is exactly what
// I did — 8830, 8831, 8832, 8833, 8834, 8835, one per sweep, straight into
// somebody else's 8834. The default here is **port 0**: the OS assigns a port
// nothing holds, which serves the rule's reason completely rather than racing
// to the same guess from a different direction. A `preferred` port is honoured
// for a human who wants a stable URL, and probes upward from there.
//
// **3 · Never kill by port.** Nothing here has a port to kill or a process to
// match by name: the server runs in this process and {@link Served.close} closes
// the handle it was given. A name match is not identity, and the safest form of
// that rule is having no name to match.

import { createReadStream, statSync } from "node:fs";
import { createServer } from "node:http";
import { extname, join, normalize, resolve, sep } from "node:path";

/** Enough to serve a harness. Anything else is served as bytes. */
const TYPES = new Map([
  [".html", "text/html; charset=utf-8"],
  [".js", "text/javascript; charset=utf-8"],
  [".mjs", "text/javascript; charset=utf-8"],
  [".css", "text/css; charset=utf-8"],
  [".json", "application/json; charset=utf-8"],
  [".svg", "image/svg+xml"],
  [".png", "image/png"],
  [".woff2", "font/woff2"],
]);

/**
 * @typedef {object} Served
 * @property {string} origin   `http://127.0.0.1:<port>`
 * @property {number} port     the port the OS actually gave us
 * @property {() => Promise<void>} close
 *
 * `close` resolves when the listener is down. If the caller then calls
 * `process.exit`, node can abort with a libuv `UV_HANDLE_CLOSING` assertion and
 * exit 127 — that is the global `fetch`'s keep-alive pool still holding
 * sockets, not this handle, and the fix is to let the process end on its own
 * (`process.exitCode`) rather than anything here. Written down because
 * `close()` is the obvious thing to blame and is not the cause.
 */

/** The file a request names, or null if it escapes the root or is not there. */
function fileFor(root, url) {
  const path = decodeURIComponent(new URL(url, "http://127.0.0.1").pathname);
  const target = resolve(root, `.${normalize(path)}`);

  // Traversal guard. `startsWith(root)` alone admits a sibling whose name
  // begins with the root's, so the separator is part of the test.
  if (target !== root && !target.startsWith(root + sep)) return null;

  try {
    const it = statSync(target);
    if (!it.isDirectory()) return target;
  } catch {
    return null;
  }

  try {
    const index = join(target, "index.html");
    statSync(index);
    return index;
  } catch {
    return null;
  }
}

/**
 * Start a server on the loopback, and refuse to return until a page it served
 * identifies itself.
 *
 * @param {object} options
 * @param {string} options.root      the directory to serve
 * @param {string} options.path      a page to prove identity with
 * @param {string} options.title     that page's own `<title>`, exactly
 * @param {number} [options.preferred]  a fixed port for a human; probed upward
 * @param {number} [options.tries]   how far upward to probe. Default 40
 * @returns {Promise<Served>}
 */
export async function serve({ root, path, title, preferred, tries = 40 }) {
  const base = resolve(root);

  const server = createServer((request, response) => {
    const file = fileFor(base, request.url);

    if (file === null) {
      // Distinguishable from a page, deliberately: a sweep that lands here
      // must not read it as content, and the title says whose miss it is.
      response.writeHead(404, { "content-type": "text/html; charset=utf-8" });
      response.end("<title>not served from this root</title>");
      return;
    }

    response.writeHead(200, {
      "content-type": TYPES.get(extname(file)) ?? "application/octet-stream",
    });
    createReadStream(file).pipe(response);
  });

  const port = await listen(server, preferred, tries);
  const origin = `http://127.0.0.1:${port}`;
  const close = () => new Promise((done) => server.close(() => done()));

  // **The identity assertion.** Everything above this line is a server; only
  // this makes it *this* server, holding *these* files.
  let served;

  try {
    const answer = await fetch(`${origin}${path}`);
    served = await answer.text();
  } catch (cause) {
    await close();
    throw new Error(`${origin}${path} could not be read: ${cause.message}`, { cause });
  }

  const found = /<title>(?<title>[^<]*)<\/title>/u.exec(served)?.groups.title;

  if (found !== title) {
    await close();
    throw new Error(
      `${origin}${path} answered with title ${JSON.stringify(found ?? null)}, `
      + `and this harness serves ${JSON.stringify(title)}. Something else is on `
      + "this port, or this root is not the one that holds the page. A status "
      + "code cannot tell those apart, which is why it is not what was checked.",
    );
  }

  return { origin, port, close };
}

/**
 * Bind, and say which port we got.
 *
 * Port 0 by default — the OS hands back something nothing holds, so there is no
 * number for two streams to converge on. A `preferred` port probes upward from
 * itself, which is the rule for the case where a person needs a stable URL.
 */
function listen(server, preferred, tries) {
  return new Promise((done, fail) => {
    let port = preferred ?? 0;
    let left = preferred === undefined ? 1 : tries;

    const attempt = () => {
      server.once("error", (error) => {
        if (error.code !== "EADDRINUSE" || --left <= 0) {
          fail(new Error(
            `could not bind 127.0.0.1:${port}${preferred === undefined ? "" : ` (probed ${tries})`}`,
            { cause: error },
          ));
          return;
        }

        port += 1;
        attempt();
      });

      // **Explicitly the loopback, never the wildcard.** A wildcard bind
      // succeeds while a specific 127.0.0.1 binding next door quietly wins
      // every request — which is the whole incident this file is named for.
      server.listen(port, "127.0.0.1", () => done(server.address().port));
    };

    attempt();
  });
}

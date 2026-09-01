/**
 * The built bundle connects and mounts — the packaging pipeline's proof.
 *
 * `module.test.ts` drives `activate` directly, which is what a module's own
 * suite should do. This asserts the thing `npm run build` actually *ships*:
 * that `ui/module.js` starts itself, takes the port, and puts something on
 * screen — the failure BB's capture photographed, where a bundle that only
 * exported `activate` mounted nothing and raised nothing.
 *
 * # Hand-rolled host, deliberately
 *
 * The shell's `serveModule` lives in the platform repository and is the
 * *enforcing* side — it is not shipped in `@hotelos/sdk`, and reaching across
 * for it would put the enforcement inside the thing being enforced. The wire is
 * small enough to speak directly, and doing so makes this a second
 * implementation of the host half, the same way `hello-hotel` is a second
 * implementation of the module half.
 *
 * # No skip
 *
 * The bundle is built by this package's own `npm run build`. A missing artifact
 * is a broken build, not an absent dependency, so this fails and says so —
 * a skip-when-absent here would be the silently-skipped-suite shape wearing a
 * justification.
 */

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

import { beforeEach, describe, expect, it } from "vitest";

/**
 * Resolved from the vitest root, not `import.meta.url`: the test is transformed
 * before it runs, so `import.meta.url` is not a `file:` URL here.
 */
const BUNDLE = resolve(process.cwd(), "module.js");

/**
 * Every permission the manifest requests, read from the manifest.
 *
 * Not a list written here. The grants a host offers must be the ones the
 * package asked for, and a second copy of that list is one that stops matching
 * the day a permission is added — which is how this test first failed, granting
 * a `stay.read` the manifest never declared.
 */
function declaredPermissions(): string[] {
  const manifest = readFileSync(resolve(process.cwd(), "../manifest.yaml"), "utf8");
  return [...manifest.matchAll(/^\s*-\s+id:\s*([a-z0-9_.]+)/gm)].map((match) => match[1]);
}

/** The shipped artifact. Absent means `npm run build` has not been run. */
function bundle(): string {
  try {
    return readFileSync(BUNDLE, "utf8");
  } catch {
    throw new Error(
      `ui/module.js is missing. Run \`npm run build\` — this suite asserts what the package ships, not what the sources would produce.`,
    );
  }
}

/** The host half of the wire, spoken directly. */
function host(
  grants: Record<string, (method: string) => unknown>,
  announced: string[] = Object.keys(grants),
) {
  const channel = new MessageChannel();
  const asked: string[] = [];
  let ready = false;

  channel.port1.addEventListener("message", (event: MessageEvent) => {
    const message = event.data as Record<string, unknown>;

    if (message["type"] === "hotelos.ready") {
      ready = true;
      return;
    }

    if (message["type"] !== "hotelos.call") return;

    const capability = String(message["capability"]);
    const grant = grants[capability];
    asked.push(`${capability}.${String(message["method"])}`);

    channel.port1.postMessage(
      grant === undefined
        ? {
            type: "hotelos.result",
            id: message["id"],
            ok: false,
            // A declined permission is absent, and the host says so in the
            // vocabulary ADR 0041 gives it.
            error: { kind: "forbidden", message: `${capability} was not granted` },
          }
        : {
            type: "hotelos.result",
            id: message["id"],
            ok: true,
            value: grant(String(message["method"])),
          },
    );
  });
  channel.port1.start();

  return {
    asked,
    isReady: () => ready,
    connect() {
      // Dispatched rather than posted: happy-dom, like jsdom, drops the
      // transfer list on `window.postMessage`, so `event.ports` would arrive
      // empty and the module would refuse a well-formed handshake.
      window.dispatchEvent(
        new MessageEvent("message", {
          data: {
            type: "hotelos.connect",
            contract: 1,
            module: { id: "guestops", version: "0.1.0", capabilities: announced },
          },
          ports: [channel.port2],
        }),
      );
    },
  };
}

/**
 * Wait until the module has connected, or fail saying it never did.
 *
 * A fixed `setTimeout(0)` is not enough: the handshake crosses a `MessagePort`
 * and the module's own first render awaits a host call, so the number of turns
 * is a property of the module rather than of this test. Polling for the
 * condition is what makes the assertion about the bundle instead of about the
 * scheduler.
 */
async function connected(running: { isReady: () => boolean }): Promise<void> {
  for (let turn = 0; turn < 200; turn += 1) {
    if (running.isReady()) return;
    await new Promise((done) => setTimeout(done, 5));
  }
  throw new Error("the bundle never sent hotelos.ready — it did not connect");
}

/** Anything the bundle throws where nobody catches it. */
function watchForErrors(): string[] {
  const seen: string[] = [];
  window.addEventListener("error", (event) => seen.push(String(event.message)));
  window.addEventListener("unhandledrejection", (event) =>
    seen.push(String((event as PromiseRejectionEvent).reason)),
  );
  return seen;
}

beforeEach(() => {
  document.body.replaceChildren();
});

describe("the bundle npm run build produces", () => {
  it("starts itself and mounts without anyone importing it", async () => {
    // The realm inlines the bundle as `<script type="module">` and nothing
    // imports it. Evaluating the text is what that does, minus the frame.
    const thrown = watchForErrors();
    const running = host({});
    new Function(bundle())();

    running.connect();
    await connected(running);

    const root = document.getElementById("hotelos-module-root");
    expect(root?.childElementCount ?? 0).toBeGreaterThan(0);
    expect(thrown).toEqual([]);
  });

  it("mounts when every capability it declared is refused", async () => {
    // The declined half, and the one a property actually produces: the module
    // announces what its manifest asked for, and the host grants none of it. A
    // module that threw here would take its whole surface down over permissions
    // an administrator deliberately withheld.
    const thrown = watchForErrors();
    const running = host({}, declaredPermissions());
    new Function(bundle())();

    running.connect();
    await connected(running);

    expect(running.isReady()).toBe(true);
    expect(document.getElementById("hotelos-module-root")?.childElementCount ?? 0).toBeGreaterThan(0);
    expect(thrown).toEqual([]);
  });
});

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
 * # Written to the strictest configuration in use
 *
 * This suite rides into every application, so it inherits each one's
 * `tsconfig`. It is therefore written to the strictest bar any of them sets —
 * today GuestOps's `noUncheckedIndexedAccess`, `exactOptionalPropertyTypes` and
 * `verbatimModuleSyntax`. Writing to the laxest package that happens to compile
 * it means the next app to adopt the suite is the one that discovers the
 * problem, which is the wrong person and the wrong moment.
 *
 * # No skip
 *
 * The bundle is built by this package's own `npm run build`. A missing artifact
 * is a broken build, not an absent dependency, so this fails and says so —
 * a skip-when-absent here would be the silently-skipped-suite shape wearing a
 * justification.
 */

import { readFileSync, readdirSync } from "node:fs";
import { resolve } from "node:path";

import { beforeEach, describe, expect, it } from "vitest";
import { HOST_CONTRACT_RANGE } from "@hotelos/sdk";

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
  // `flatMap` with an explicit undefined branch rather than `match[1]!`:
  // `noUncheckedIndexedAccess` is right that a capture group may be absent, and
  // the honest answer is that such a line declared no permission. Asserting it
  // away would silence the compiler about the one case worth handling.
  return [...manifest.matchAll(/^\s*-\s+id:\s*([a-z0-9_.]+)/gm)].flatMap((match) =>
    match[1] === undefined ? [] : [match[1]],
  );
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
            // Both halves of the range, from the SDK rather than as literals —
            // `isConnect` requires `minContract` to be a number, so a message
            // missing it is not recognised as a connect AT ALL and the module
            // waits forever for a handshake that never arrives. Written as
            // literals this fixture would go stale the next time the contract
            // moves, and would fail as "the bundle never connected" rather
            // than as "this test is out of date".
            contract: HOST_CONTRACT_RANGE.current,
            minContract: HOST_CONTRACT_RANGE.min,
            module: { id: "guestops", version: "0.1.0", capabilities: announced },
            // What a real host sends. Null on both, deliberately: the SDK types
            // them nullable because an unconfigured property is a real state.
            property: { timezone: null, locale: null },
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

/**
 * Every string literal in a file, crudely but consistently.
 *
 * Crude is the right level here: this is a *comparison* between two sets built
 * the same way, so whatever the pattern misses it misses on both sides. It is
 * not trying to parse TypeScript.
 */
function literals(source: string): Set<string> {
  // **Match every literal, then filter by length — never the other way round.**
  // The first version asked for `{8,}` inside the pattern, so a short literal
  // was skipped and its closing quote paired with the *next* literal's opening
  // quote: `label: "Room", value: "203"` yielded `", value: "`. The instrument
  // then reported code fragments as fixture data — a detector inventing its own
  // findings, and the reason to prove one against the artefact before trusting
  // a clean report from it.
  return new Set(
    // Double-quoted only. This module is written in double quotes throughout,
    // and admitting the single-quoted form made every apostrophe in ordinary
    // prose a delimiter: `the seller's control` opened a "string" that closed
    // at the next apostrophe, so the instrument reported `s control. The Deluxe
    // King` as fixture data. A pattern that can misread its own input reports
    // findings that are its own.
    [...source.matchAll(/"([^"\\\n]*)"/gu)]
      .map((match) => match[1])
      .filter((one): one is string => one !== undefined && one.length >= 8),
  );
}

/** Every `.ts` under a directory, recursively. */
function sources(from: string): string[] {
  return readdirSync(from, { withFileTypes: true }).flatMap((entry) => {
    const path = resolve(from, entry.name);

    if (entry.isDirectory()) return sources(path);
    return entry.name.endsWith(".ts") ? [readFileSync(path, "utf8")] : [];
  });
}

describe("the fixtures do not reach the property", () => {
  /**
   * Strings that exist in the approved frames' data and nowhere else.
   *
   * **Derived, not listed.** A hand-written list of names to look for is a list
   * somebody must extend the day a fixture gains a guest, and the one nobody
   * extends is the one that ships. Subtracting the rest of the source is what
   * makes the set *fixture-only*: a label the book and a screen share — a tab
   * name, a column heading — is legitimately in the bundle, and would be a
   * false positive on every run until somebody deleted the test.
   */
  function fixtureOnly(): string[] {
    const inFixtures = new Set<string>();
    for (const source of sources(resolve(process.cwd(), "book/recorded"))) {
      for (const one of literals(source)) inFixtures.add(one);
    }

    const elsewhere = new Set<string>();
    for (const dir of ["application.ts", "screens", "chrome", "widgets", "book/model"]) {
      const at = resolve(process.cwd(), dir);
      const found = dir.endsWith(".ts") ? [readFileSync(at, "utf8")] : sources(at);
      for (const source of found) for (const one of literals(source)) elsewhere.add(one);
    }

    // **Substring, not equality.** A fixture says `"Deluxe King"` on its own;
    // a screen says *"the seller's control. The Deluxe King's out-of-order room
    // is a different thing"* as explanatory copy. Subtracting only exact
    // matches left the short one in the set, and it then matched inside the
    // long one in the bundle — the guard reporting a sentence it wrote itself.
    const embedded = [...elsewhere];

    return [...inFixtures].filter(
      (one) => !elsewhere.has(one) && !embedded.some((other) => other.includes(one)),
    );
  }

  it("has fixture-only strings to look for, so the check cannot be vacuous", () => {
    // A zero here would mean the derivation found nothing, and a guard that
    // searches for nothing passes forever — the shape this repository has
    // recorded under "a pattern that matches nothing returns zero".
    expect(fixtureOnly().length).toBeGreaterThan(20);
  });

  it("ships none of them in module.js", () => {
    const shipped = bundle();

    // **The quoted form, not a substring.** A bare `includes` matched
    // `standing` inside *outstanding* and `complete` inside *incomplete*, so
    // the guard reported the module's own prose as fixture data. What "shipped
    // as data" actually means is that the string is a *literal* in the
    // artifact — delimited — and a word occurring inside a sentence is not.
    const leaked = fixtureOnly().filter((one) => shipped.includes(JSON.stringify(one)));

    // **The bundle, not the source.** `load<typeof recordedToday>` is a type
    // argument and erases at build, so reading the source would flag every
    // screen that names a fixture to borrow its shape — which is the exact
    // false positive that made a grep report fourteen files when six were
    // real. What a property runs is the artifact, and the artifact either
    // carries a guest's invented name or it does not.
    expect(
      leaked,
      "the built module carries strings that exist only in the approved frames' "
      + "data — a screen is drawing a fixture on a property's desk: "
      + leaked.slice(0, 5).join(" · "),
    ).toEqual([]);
  });
});

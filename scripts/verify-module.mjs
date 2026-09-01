/**
 * Verify a built `ui/module.js` against SHELL-Q32's contract.
 *
 * ```
 * node ../../scripts/verify-module.mjs ../.build/pkg/ui/module.js
 * ```
 *
 * Run from an app's `ui/` directory, chained after the bundle step:
 *
 * ```
 * "package": "esbuild main.ts --bundle … && node ../../scripts/verify-module.mjs …"
 * ```
 *
 * # It verifies and never builds
 *
 * The bundling is the app's own `package` script — `--platform=browser
 * --target=es2022`, an explicit `--alias` for the SDK, output into `.build/`
 * because a packaged module is built at packaging time and never committed.
 * This does not repeat any of that. Two commands that both know how to build
 * one artifact is the duplication that drifts; one builds, one judges.
 *
 * # Why the checks run here rather than at install
 *
 * `carry_ui` copies the payload verbatim and `hopkg` inventories what it finds,
 * so the bundle is signed exactly as produced and nothing downstream rewrites
 * it. This is the last point at which a person is still holding the thing that
 * went wrong; the alternative is a refusal an administrator reads at install,
 * about a package they cannot fix.
 *
 * # A failing bundle is removed
 *
 * A rejected artifact left in `.build/` is one the next packaging step picks up
 * and signs. It is a build output and regenerating it costs a second.
 */

import { createRequire } from "node:module";
import { readFile, rm } from "node:fs/promises";
import { dirname, resolve } from "node:path";
import { fileURLToPath, pathToFileURL } from "node:url";

const app = process.cwd();
const outfile = resolve(app, process.argv[2] ?? "../.build/pkg/ui/module.js");
const require_ = createRequire(pathToFileURL(`${app}/`));
const esbuild = require_("esbuild");

/** Where `@hotelos/sdk` resolves, according to the app's own tsconfig. */
async function sdkEntry() {
  const raw = await readFile(resolve(app, "tsconfig.json"), "utf8");
  // Comments are legal in tsconfig and JSON.parse rejects them.
  const mapped = /"@hotelos\/sdk"\s*:\s*\[\s*"([^"]+)"/.exec(
    raw.replace(/^\s*\/\/.*$/gm, ""),
  );

  if (mapped === null) {
    throw new Error(
      `${app}/tsconfig.json has no paths entry for "@hotelos/sdk", so this script cannot tell where the SDK is.`,
    );
  }

  return resolve(app, mapped[1]);
}

/** The token names the platform publishes, read from the SDK itself. */
async function publishedTokens() {
  const entry = await sdkEntry();
  const out = resolve(app, ".build-tokens.mjs");

  // Bundled and imported rather than regexed: the record is TypeScript, and a
  // second parser for it is a second thing to keep in step with the first.
  await esbuild.build({
    entryPoints: [resolve(dirname(entry), "tokens.ts")],
    bundle: true,
    format: "esm",
    outfile: out,
    logLevel: "silent",
  });

  const { TOKEN_NAMES } = await import(pathToFileURL(out).href);
  await rm(out, { force: true });
  return new Set(TOKEN_NAMES);
}

/** Import specifiers the host would have to resolve. There must be none. */
function unresolved(bundle) {
  return [
    ...new Set(
      [...bundle.matchAll(/(?:^|\n)\s*(?:import|export)[^;\n]*?from\s*["']([^"'.][^"']*)["']/g)].map(
        (match) => match[1],
      ),
    ),
  ];
}

/** The bundle must start itself, not merely define `activate`. */
function selfStarts(bundle) {
  return bundle.includes("hotelos.connect");
}

/** Custom properties the bundle reads but never sets — the host supplies these. */
function expectedFromHost(bundle) {
  const consumed = new Set(
    [...bundle.matchAll(/var\(\s*--([a-z0-9-]+)/g)].map((match) => match[1]),
  );
  for (const [, defined] of bundle.matchAll(/--([a-z0-9-]+)\s*:/g)) {
    consumed.delete(defined);
  }
  return [...consumed].sort();
}

/** Same-realm assumptions the sandbox makes impossible. */
function realmAssumptions(bundle) {
  return ["window.parent", "window.top", "document.cookie", "document.domain"].filter((reach) =>
    bundle.includes(reach),
  );
}

let bundle;
try {
  bundle = await readFile(outfile, "utf8");
} catch {
  console.error(
    `
Nothing to verify at ${outfile}. Run the package step first — this script judges what the build produced, and cannot stand in for it.
`,
  );
  process.exit(1);
}
const published = await publishedTokens();

const failures = [];

const bare = unresolved(bundle);
if (bare.length > 0) {
  failures.push(
    `imports the host would have to resolve, and it cannot: ${bare.join(", ")}. Bundle them in.`,
  );
}

if (!selfStarts(bundle)) {
  failures.push(
    "never connects to the host. The entry must call connectToHost(activate) — a bundle that only exports mounts nothing and reports nothing.",
  );
}

const unpublished = expectedFromHost(bundle).filter((name) => !published.has(name));
if (unpublished.length > 0) {
  failures.push(
    `reads custom properties the platform does not publish, so they resolve to nothing: ${unpublished.map((n) => `--${n}`).join(", ")}.`,
  );
}

const reached = realmAssumptions(bundle);
if (reached.length > 0) {
  failures.push(`assumes it shares the shell's realm: ${reached.join(", ")}.`);
}

if (failures.length > 0) {
  // Removed, deliberately. A rejected bundle left on disk is one carry_ui
  // copies and hopkg signs, and the next person to see it is an administrator
  // reading a refusal.
  await rm(outfile, { force: true });
  console.error(`\nui/module.js was NOT written — ${failures.length} problem(s):\n`);
  for (const failure of failures) console.error(`  • the bundle ${failure}`);
  console.error("");
  process.exit(1);
}

console.log(`${outfile.split(/[\/]/).slice(-1)[0]} verified: self-starting, self-contained, ${published.size} published tokens available.`);

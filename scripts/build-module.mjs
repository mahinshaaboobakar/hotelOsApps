/**
 * Build an application's `ui/module.js`, and refuse to leave one that is wrong.
 *
 * ```
 * cd <app>/ui && npm run build
 * ```
 *
 * # Bundle, verify, delete-on-failure — one script, not a chain
 *
 * The chained form (`esbuild … && node verify …`) propagates failure honestly,
 * and still leaves a rejected bundle on disk for the moment between the two
 * commands — and permanently if anyone runs the first alone. Doing all three
 * here means **an unverified artifact never exists**, which is a structural
 * guarantee rather than a sequencing one. `carry_ui` copies whatever it finds.
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
const outfile = resolve(app, process.argv[2] ?? "module.js");
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

/**
 * The bundle must start itself, not merely define `activate`.
 *
 * # This depends on tree-shaking, and that dependency is not obvious
 *
 * The check is *does the handshake code survive into the output* — which is
 * only a signal because esbuild **drops** what nothing reaches. An entry that
 * merely re-exports `activate` never calls `connectToHost`, so the SDK's
 * connect path is eliminated and `hotelos.connect` does not appear.
 *
 * GG verified the route rather than assuming it: an exports-only bundle carries
 * **0** occurrences, the real entry **1**. If a future flag turns tree-shaking
 * off, this check keeps passing while the defect returns — so the probe in the
 * report is not decoration, it is what re-establishes the signal.
 */
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

/**
 * A declared `ui.icon` must exist in the package and be an SVG — `SHELL-Q34`.
 *
 * The desktop verifies the icon against the signed inventory at every load and
 * refuses the whole package when it does not match. That is the right
 * behaviour there and the wrong place to *discover* a missing file: by then it
 * is signed, shipped and installed, and an administrator is reading a refusal
 * about a package they cannot fix. Checked here, where the author is still
 * holding it.
 *
 * The parse is deliberately shallow — it opens with `<svg` or an XML
 * declaration. Anything deeper would be a second opinion about SVG validity
 * beside the browser's, and the browser's is the one that decides.
 */
async function declaredIcon(app) {
  let manifest;
  try {
    manifest = await readFile(resolve(app, "../manifest.yaml"), "utf8");
  } catch {
    return [];
  }

  const declared = /^\s*icon:\s*(\S+)\s*$/m.exec(manifest);
  if (declared === null) return [];

  const path = declared[1];
  if (!path.startsWith("ui/")) {
    return [`declares ui.icon "${path}", which is not inside ui/.`];
  }

  let svg;
  try {
    svg = await readFile(resolve(app, path.slice("ui/".length)), "utf8");
  } catch {
    return [`declares ui.icon "${path}", and there is no such file in the package.`];
  }

  const head = svg.trimStart();
  if (!head.startsWith("<svg") && !head.startsWith("<?xml")) {
    return [`declares ui.icon "${path}", which is not an SVG.`];
  }

  // A plain containment check, not a regex: the path comes from the manifest
  // and would have to be escaped to be matched, which is a second thing to get
  // right for no benefit.
  if (!manifest.includes(`"${path}"`)) {
    return [
      `declares ui.icon "${path}" but does not list it in files:, so the signature would not cover it.`,
    ];
  }

  return [];
}

/** Same-realm assumptions the sandbox makes impossible. */
function realmAssumptions(bundle) {
  return ["window.parent", "window.top", "document.cookie", "document.domain"].filter((reach) =>
    bundle.includes(reach),
  );
}

await esbuild.build({
  entryPoints: [resolve(app, "main.ts")],
  bundle: true,
  format: "esm",
  // The realm is a browser document served by `srcdoc`, and ES2022 is what the
  // shell's own build targets. Stated rather than defaulted: esbuild's default
  // platform would resolve node conditions a realm cannot honour.
  platform: "browser",
  target: "es2022",
  outfile,
  logLevel: "info",
});

const bundle = await readFile(outfile, "utf8");
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

for (const problem of await declaredIcon(app)) failures.push(problem);

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

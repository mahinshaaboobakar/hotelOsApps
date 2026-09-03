/**
 * Build and sign a `kind: provider` package.
 *
 * ```
 * node scripts/build-provider.mjs ollama --key <path-outside-this-repo>
 * node scripts/build-provider.mjs ollama            # stage only, no signing
 * ```
 *
 * # Why a package with no payload needs a build step at all
 *
 * ADR 0130 §3 makes a provider declarative-only, so the signed archive carries
 * `files: {}` and the whole package is its manifest. It looks like there is
 * nothing to build — and signing the application directory directly **fails**,
 * because `hopkg` inventories every file it finds beneath the directory it is
 * given, recursively, and everything except `manifest.yaml` is payload:
 *
 * ```text
 * $ hopkg sign ollama/ --key …
 * Error: the manifest would be refused: a provider ships no files,
 *        and this one declares 1 in its inventory
 * ```
 *
 * That one file is `docs/chapters/01-the-ollama-provider.md`. Every provider
 * package will have documentation — platform ADR 0121 requires it — so every
 * provider package hits this. The build stages the manifest **alone** and signs
 * that, which is why this script exists: to make the mistake impossible rather
 * than survivable.
 *
 * # Stage, verify, sign — and never leave a wrong artifact
 *
 * `build-module.mjs`'s rule, for the same reason: an unverified artifact that
 * exists for a moment is one the next step picks up. The staging directory is
 * rebuilt from nothing on every run, and a failure removes what it produced.
 *
 * # This script does not validate the manifest, deliberately
 *
 * `hotelos-package`'s validator is the authority on whether a provider manifest
 * is admissible, and it runs inside `hopkg sign` and again at install on every
 * property. A second opinion here would be a second validator — the thing the
 * Kernel's own `ListProviders` comment refuses to become — and it would drift.
 * The only thing checked before signing is the one thing the *packager* is
 * responsible for: that what gets staged is the manifest and nothing else.
 *
 * # The signing key never enters this repository
 *
 * It is passed at invocation, from somewhere outside. Without `--key` this
 * stages and stops, which is the useful default for a check in CI that has no
 * key and should not have one.
 */

import { spawnSync } from "node:child_process";
import { copyFile, mkdir, readdir, readFile, rm } from "node:fs/promises";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const repository = resolve(dirname(fileURLToPath(import.meta.url)), "..");

/** Command-line shape: one application name, then optional flags. */
function parse(argv) {
  const [app, ...rest] = argv;
  if (!app || app.startsWith("-")) {
    throw new Error(
      "usage: node scripts/build-provider.mjs <app> [--key <path>] [--hopkg <path>]",
    );
  }

  const flag = (name) => {
    const at = rest.indexOf(name);
    return at === -1 ? undefined : rest[at + 1];
  };

  return { app, key: flag("--key"), hopkg: flag("--hopkg") };
}

/**
 * The `hopkg` executable.
 *
 * A sibling checkout of the platform is how this repository takes the
 * platform's tools today — the README's implementation choice, until an
 * SDK-publishing round makes them artifacts. Named explicitly with `--hopkg`
 * or `HOPKG` when the checkout is elsewhere; the failure below says so rather
 * than reporting a missing file.
 */
function hopkgAt(named) {
  return (
    named ??
    process.env.HOPKG ??
    resolve(repository, "../HosPilotOS/target/debug/hopkg.exe")
  );
}

/** Every file beneath a directory, relative, in no particular order. */
async function filesUnder(root, prefix = "") {
  const found = [];
  for (const entry of await readdir(join(root, prefix), { withFileTypes: true })) {
    const relative = prefix ? `${prefix}/${entry.name}` : entry.name;
    if (entry.isDirectory()) found.push(...(await filesUnder(root, relative)));
    else found.push(relative);
  }
  return found;
}

async function main() {
  const { app, key, hopkg } = parse(process.argv.slice(2));

  const source = resolve(repository, app);
  const build = join(source, ".build");
  const payload = join(build, "payload");

  const manifest = await readFile(join(source, "manifest.yaml"), "utf8");

  // The one check that is this script's business. `kind:` decides which build
  // an application gets, and staging an application's manifest alone would
  // produce a package missing its own backend — signed, installable, and
  // broken in a way nothing downstream could attribute to the packager.
  if (!/^kind:\s*provider\s*$/m.test(manifest)) {
    throw new Error(
      `${app}/manifest.yaml is not \`kind: provider\` — this script stages the ` +
        `manifest alone, which would drop an application's payload`,
    );
  }

  await rm(build, { recursive: true, force: true });
  await mkdir(payload, { recursive: true });
  await copyFile(join(source, "manifest.yaml"), join(payload, "manifest.yaml"));

  // Derived from the staging directory, never from the list above: the claim
  // is about what is on disk, and re-stating the copy would make this assert
  // that two lines of this file agree with each other.
  const staged = await filesUnder(payload);
  if (staged.length !== 1 || staged[0] !== "manifest.yaml") {
    await rm(build, { recursive: true, force: true });
    throw new Error(
      `staged ${staged.length} file(s) — a provider ships only its manifest: ${staged.join(", ")}`,
    );
  }

  console.log(`staged ${app}: ${staged[0]}`);

  if (!key) {
    console.log("no --key: staged only. Sign with --key <path outside this repository>.");
    return;
  }

  const signed = spawnSync(
    hopkgAt(hopkg),
    ["sign", payload, "--key", key, "--out", build],
    { stdio: "inherit" },
  );

  if (signed.error?.code === "ENOENT") {
    await rm(build, { recursive: true, force: true });
    throw new Error(
      `no hopkg at ${hopkgAt(hopkg)} — build it in the platform checkout ` +
        `(\`cargo build -p hotelos-hopkg\`) or pass --hopkg / set HOPKG`,
    );
  }

  if (signed.status !== 0) {
    // The refusal is already on stderr, from the thing entitled to make it.
    await rm(build, { recursive: true, force: true });
    throw new Error(`hopkg refused ${app}`);
  }
}

main().catch((error) => {
  console.error(error.message);
  process.exitCode = 1;
});

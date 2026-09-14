/**
 * Build every consumer of the platform's contracts, and say which platform.
 *
 * ```
 * node scripts/check-platform-consumers.mjs
 * node scripts/check-platform-consumers.mjs --list
 * ```
 *
 * # The gap this closes
 *
 * CLAUDE.md already carries the rule: *"a contract change is verified by
 * building every consumer, not the crate you edited."* It then enumerates the
 * commands — `cargo check --workspace --all-targets`, `vite build`, every
 * service's `dotnet build`, `tsc` — **and every one of them stops at the
 * repository boundary.** This is not a missing rule. It is ADR 0051 → 0052
 * again: the rule was applied to the list somebody had already written down.
 *
 * Four platform surfaces are consumed from here, and a change to any of them
 * can break this repository without anything in the platform's own round
 * failing:
 *
 * ```text
 * packages/sdk-dotnet/HotelOS.Platform              the .NET SDK
 * packages/sdk-dotnet/HotelOS.Platform.TestSupport  test support
 * shared/protos                                     the contract language
 * packages/sdk-typescript                           the TS SDK
 * ```
 *
 * `716f8b10` — *"EVT-Q4: an event consumer's admission becomes a required
 * input"* — touched five files, every one under `packages/sdk-dotnet`. It left
 * both .NET callers of `AddApplicationEventConsumer` red, in this repository,
 * with the platform's own build clean. It was found a day later by somebody
 * doing something else.
 *
 * # Derived, never a list
 *
 * The consumers are found by walking the tree. The platform's equivalent —
 * `DOTNET_PROJECTS` at its `Makefile:798` — is hand-kept, and CLAUDE.md already
 * records it drifting: *"the count was 'nine' and had been wrong since the
 * Context Service shipped."* A hand-kept list of **applications** would drift
 * faster, because applications are added by other people. Deriving also picks
 * up `pms-oracle`, which is a connector rather than an application and just as
 * much a consumer.
 *
 * # Absent is not a pass, and it is not degraded either
 *
 * ADR 0122 permits an application to live in a repository whose author has no
 * platform checkout beside it, and a platform author may have no applications
 * checked out. When something cannot be checked this reports **unverified** and
 * names what it could not reach. It never reports that as success: a check that
 * could not have failed is not evidence about the thing it was pointed at.
 *
 * **And unverified blocks phase-close** — ADR 0168. Not because a missing
 * sibling is anyone's fault, but because a contract-changing round cannot
 * certify itself without the consumers the contract is for.
 *
 * # What it says it measured
 *
 * Every run prints the platform commit it built against and whether that tree
 * was clean. BB's rule, in the place it bites hardest: a green here means
 * nothing without *against what*, and the platform is a moving checkout rather
 * than a pinned artifact.
 *
 * # This is scaffolding, and its end is part of the ruling
 *
 * ADR 0168 does not merely permit this to be retired — it says so:
 *
 * > **Do not let today's filesystem-based check become the permanent
 * > contract-consumption architecture.**
 *
 * ADR 0122 calls the sibling-path reference an implementation choice *"until an
 * SDK-publishing round makes them proper artifacts"*. With a versioned SDK an
 * application pins a version, a platform change cannot break it silently, and
 * the break moves to a deliberate upgrade — which is where it belongs. **Delete
 * this script then.**
 *
 * It is written here because a check that blocks phase-close is exactly the
 * kind that acquires permanence by being load-bearing: the more rounds it
 * stops, the more it looks like architecture. It is not. It is a filesystem
 * standing in for a version number.
 */

import { execFileSync } from "node:child_process";
import { existsSync, readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

/**
 * The exit status for a run that could not check every consumer.
 *
 * **Unverified blocks, and there is no flag** — ADR 0168, 2026-09-14. This was
 * built as a switch while the branch was open; it is not one now, because there
 * is no configuration of anybody's machine in which a contract-changing round
 * may legitimately certify itself without its consumers. A switch set to the
 * right value is a switch somebody sets back, and this repository has already
 * recorded what happens to a limit that lives in prose beside the value it
 * fails to constrain.
 *
 * The distinction the ruling turns on, and it is not the obvious one:
 * **absence stays a legitimate environmental state — certifying without the
 * consumer does not.** ADR 0122 lets an application repository live elsewhere,
 * so a platform author may honestly *encounter* a missing sibling. What they
 * may not do is call that round complete.
 */
const UNVERIFIED = 2;

/** This repository's root, from this file rather than from a working directory. */
const REPO = resolve(dirname(fileURLToPath(import.meta.url)), "..");

/**
 * The platform checkout, identified by a file only it has.
 *
 * Existence of a directory called `HosPilotOS` is not identity — the same
 * lesson a preview harness paid for by measuring another stream's server. What
 * is needed is the SDK this repository compiles against, so that is what is
 * looked for.
 */
const PLATFORM_MARKER = join(
  "packages", "sdk-dotnet", "HotelOS.Platform", "HotelOS.Platform.csproj");

/** The four surfaces a change to which obliges a run of this. */
const SURFACES = [
  join("packages", "sdk-dotnet"),
  join("packages", "sdk-typescript"),
  join("shared", "protos"),
];

/**
 * The platform checkout, or null.
 *
 * `HOTELOS_PLATFORM_ROOT` overrides the sibling for a developer who keeps it
 * elsewhere. **Either way the marker decides**, so an override pointing
 * somewhere wrong reports unverified rather than passing against nothing —
 * which is the same reason it is a marker and not a directory name.
 */
function platformRoot() {
  const named = process.env.HOTELOS_PLATFORM_ROOT;
  const candidate = named === undefined ? resolve(REPO, "..", "HosPilotOS") : resolve(named);

  return existsSync(join(candidate, PLATFORM_MARKER)) ? candidate : null;
}

/** Every file matching a predicate, skipping what no consumer lives in. */
function walk(from, take, found = []) {
  for (const entry of readdirSync(from)) {
    if (["node_modules", "bin", "obj", ".git", "target"].includes(entry)) continue;

    const path = join(from, entry);

    if (statSync(path).isDirectory()) walk(path, take, found);
    else if (take(entry)) found.push(path);
  }

  return found;
}

/** What this repository builds, and which of it names the platform directly. */
function consumers() {
  const projects = walk(REPO, (name) => name.endsWith(".csproj")).map((path) => ({
    kind: "dotnet",
    path,
    direct: readFileSync(path, "utf8").includes("PlatformRoot)"),
  }));

  // Every TypeScript unit in the tree, with no filter on where it sits. A rule
  // like "directories called ui" would be a hand-kept list wearing a pattern,
  // and the first module organised differently would be skipped silently.
  const uis = walk(REPO, (name) => name === "tsconfig.json").map((path) => ({
    kind: "typescript",
    path: dirname(path),
    direct: readFileSync(path, "utf8").includes("sdk-typescript"),
  }));

  return [...projects, ...uis];
}

/** Run one consumer's check. */
function check(consumer) {
  const where = consumer.kind === "dotnet" ? dirname(consumer.path) : consumer.path;

  // TypeScript's own entry, run by this node. Not `npx`, which is ENOENT here,
  // and not `npx.cmd`, which node 24 refuses with EINVAL unless a shell is
  // spawned — and `shell: true` is deprecated precisely because it concatenates
  // arguments instead of escaping them. A `.js` file needs none of that.
  const tsc = join(consumer.path, "node_modules", "typescript", "bin", "tsc");

  if (consumer.kind === "typescript" && !existsSync(tsc)) {
    return {
      state: "unverified",
      because: "no local typescript — run `npm install` here before this can say anything",
    };
  }

  try {
    if (consumer.kind === "dotnet") {
      execFileSync("dotnet", ["build", consumer.path], { stdio: "pipe", cwd: REPO });
    } else {
      execFileSync(process.execPath, [tsc, "--noEmit"], { stdio: "pipe", cwd: where });
    }

    return { state: "built" };
  } catch (failure) {
    // **A tool that would not start is not a consumer that does not build.**
    // The first version of this reported all four UIs BROKEN with an empty
    // reason, because the spawn failed and there was no output to quote — a
    // verdict with no evidence, which is worse than no verdict. A failure with
    // no exit status never reached the compiler.
    if (failure.status === null || failure.status === undefined) {
      return {
        state: "unverified",
        because: `the check could not be run here (${failure.code ?? "no exit status"}) — `
          + "nothing was compiled, so this says nothing about the consumer",
      };
    }

    // Deduplicated and stripped of the repository prefix. MSBuild reports one
    // error once per pass and again per referencing project, so the raw output
    // says the same thing four times — and a reader counting lines would be
    // counting the build's passes rather than the defects.
    const output = `${failure.stdout ?? ""}${failure.stderr ?? ""}`;
    const lines = [...new Set(
      output.split("\n")
        .filter((line) => /\b(error|Error)\b/u.test(line))
        .map((line) => line.replaceAll(`${REPO}\\`, "").replaceAll(`${REPO}/`, "").trim())
        .filter((line) => line.length > 0 && !/^\d+ Error/u.test(line)),
    )];

    return { state: "broken", because: lines.slice(0, 3).join("\n      ") };
  }
}

/** The platform commit this was measured against, or why that is unknown. */
function provenance(platform) {
  if (platform === null) return { known: false };

  const git = (...args) => {
    try {
      return execFileSync("git", args, { cwd: platform, encoding: "utf8" }).trim();
    } catch {
      return null;
    }
  };

  const head = git("rev-parse", "--short", "HEAD");
  const dirty = (git("status", "--porcelain", "--", ...SURFACES) ?? "")
    .split("\n").filter((line) => line.trim().length > 0);

  return { known: head !== null, head, dirty };
}

/** This repository's own commit, and what is uncommitted in it. */
function consumerTree() {
  const git = (...args) => {
    try {
      return execFileSync("git", args, { cwd: REPO, encoding: "utf8" }).trim();
    } catch {
      return null;
    }
  };

  // Sources only. A dirty design page says nothing about whether these build,
  // and listing it would train the reader to skim the line that matters.
  // Parsed, not sliced. Porcelain is two status characters and a space, and a
  // fixed `slice(3)` ate a character of the first path it met — printing
  // `obs/backend/...` for `jobs/backend/...`, which is a wrong location wearing
  // the shape of a real one.
  const dirty = (git("status", "--porcelain") ?? "")
    .split("\n")
    .map((line) => /^..\s(?<path>.+)$/u.exec(line)?.groups.path.trim())
    .filter((path) => path !== undefined && /\.(cs|csproj|ts|tsx|json|proto)$/u.test(path));

  return { head: git("rev-parse", "--short", "HEAD"), dirty };
}

const platform = platformRoot();
const listing = process.argv.includes("--list");
const all = consumers();

process.stdout.write(
  `${all.length} consumers — ${all.filter((c) => c.direct).length} name a platform surface directly\n`);

if (listing) {
  for (const one of all) {
    process.stdout.write(
      `  ${one.kind.padEnd(10)} ${one.direct ? "direct  " : "indirect"} ${relative(REPO, one.path)}\n`);
  }

  process.exit(0);
}

// **Said before the results, not after.** A reader who sees the verdict first
// has already formed a view by the time they learn what it was measured
// against — and against a moving checkout that is the half that matters.
const against = provenance(platform);

if (platform === null) {
  // **Name where it actually looked.** An override sends this somewhere other
  // than the sibling, and a message that said "beside this repository" would be
  // describing a search it did not perform — a guard may claim only what it
  // measured, and this one is read by somebody who already believes the
  // platform is present.
  const looked = process.env.HOTELOS_PLATFORM_ROOT === undefined
    ? `${resolve(REPO, "..", "HosPilotOS")} (the sibling checkout)`
    : `${resolve(process.env.HOTELOS_PLATFORM_ROOT)} (HOTELOS_PLATFORM_ROOT)`;

  process.stdout.write(
    "\nUNVERIFIED — no platform checkout was found.\n"
    + `  Looked in ${looked}\n`
    + `  for ${PLATFORM_MARKER}, which is what this repository compiles against.\n`
    + "\n"
    + "  ADR 0122 permits an application to be developed without a platform beside\n"
    + "  it, so having none is a legitimate state and not a mistake. What it is not\n"
    + "  is a pass: nothing here has been checked against any platform.\n"
    + "\n"
    + "  ADR 0168: this blocks phase-close. If a contract surface changed in this\n"
    + "  round, check out the platform beside this repository (or point\n"
    + "  HOTELOS_PLATFORM_ROOT at it) and run this again. If none did, this check\n"
    + "  was not owed and the round does not need it.\n");

  process.exit(UNVERIFIED);
}

process.stdout.write(
  `against platform ${against.head ?? "(git could not answer)"}`
  + `${against.dirty.length > 0 ? ` + ${against.dirty.length} uncommitted on a contract surface` : ""}\n`);

// **And this side's tree, which is the half that nearly fooled its author.**
// On the first real run every consumer built — because a colleague's fix to
// `jobs/backend/src/Program.cs` was sitting uncommitted in the shared checkout.
// At HEAD that file had no `admission` argument at all. A green that depends on
// somebody's unlanded work is a statement about this afternoon, not about what
// the next person checks out, and nothing prompts a second look at a result you
// were hoping for.
const here = consumerTree();

process.stdout.write(
  `in this tree ${here.head ?? "(git could not answer)"}`
  + `${here.dirty.length === 0 ? " (clean)" : ` + ${here.dirty.length} uncommitted`}\n`);

for (const path of here.dirty) process.stdout.write(`    uncommitted  ${path}\n`);

if (here.dirty.length > 0) {
  process.stdout.write(
    "    ^ a result below may depend on these rather than on HEAD\n");
}

process.stdout.write("\n");

const results = all.map((one) => ({ ...one, ...check(one) }));

for (const one of results) {
  const mark = { built: "ok        ", broken: "BROKEN    ", unverified: "unverified" }[one.state];
  process.stdout.write(`  ${mark} ${relative(REPO, one.path)}\n`);

  if (one.because !== undefined) process.stdout.write(`      ${one.because}\n`);
}

const built = results.filter((one) => one.state === "built").length;
const broken = results.filter((one) => one.state === "broken").length;
const unverified = results.filter((one) => one.state === "unverified").length;

// The columns close, or the totals are describing something other than the
// population. `unverified` is emitted at zero deliberately: a missing figure
// and a measured none are different facts.
process.stdout.write(
  `\n${results.length} consumers = ${built} built + ${broken} broken + ${unverified} unverified\n`);

if (broken > 0) {
  process.stdout.write(
    "\nA consumer of the platform's contracts does not build. If a platform surface\n"
    + "changed in this round, this is that change arriving — the platform's own build\n"
    + "cannot see these callers.\n");
  process.exit(1);
}

if (unverified > 0) {
  process.stdout.write(
    `\nUNVERIFIED — ${unverified} consumer(s) could not be checked, named above. Nothing\n`
    + "in this run is a statement about them, and the ones that did build say nothing\n"
    + "on their behalf.\n"
    + "\n"
    + "ADR 0168: this blocks phase-close. Make them checkable and run this again.\n");
  process.exit(UNVERIFIED);
}

process.stdout.write("\nevery consumer builds against this platform\n");

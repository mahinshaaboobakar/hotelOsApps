/**
 * Lay out a package directory `hopkg sign` can take — derived, never a copy list.
 *
 * ```
 * node scripts/pack-package.mjs <package-dir> [--out <dir>]
 * node scripts/pack-package.mjs pms-oracle
 * ```
 *
 * # The gap this closes
 *
 * Nineteen `.hopkg` archives exist and nothing in either repository assembles a
 * package directory. `hopkg` has `keygen` and `sign <package-dir>`; there is no
 * `pack`. `build-module.mjs` is an esbuild UI bundler and mentions `hopkg` only
 * in prose. So every archive was laid out by hand, and how was recorded nowhere
 * — which is why `pms-oracle-0.1.0` ships `backend/PmsOracle.Connector.dll` and
 * nothing else: one assembly, no `.deps.json`, no `.runtimeconfig.json`, none of
 * its dependencies. It is signed, installed, and cannot run.
 *
 * # What hopkg's contract actually is, read from its own code
 *
 * `inventory.rs`: *"Computed, never written down… The manifest in a repository
 * carries a placeholder; this replaces the whole map, every time."* And
 * `manifest.yaml` and `MANIFEST.sig` are never payload.
 *
 * So the layout contract is minimal and total: **whatever is in the directory
 * ships, hashed into `files:` and covered by the signature.** A file nobody
 * meant to ship is `Refusal::UndeclaredFile` at the edge if it is missed, and a
 * signed part of the payload if it is not. That makes deciding what goes in the
 * directory the whole job, and it is what this derives.
 *
 * # The payload is derived from the manifest and the build, in both directions
 *
 * ```
 * kind:              absent -> application · connector · provider
 * runtime.assembly:  HotelOS.Jobs.dll -> HotelOS.Jobs.deps.json -> the closure
 * ```
 *
 * A provider is `manifest.yaml` alone — `openai-1.1.0` is exactly that, and ADR
 * 0130 makes v1 providers declarative. An application and a connector carry
 * `backend/` and `ui/`.
 *
 * `deps.json` is the authority on the backend, because it is what the runtime
 * itself reads. A floor taken from the predecessor archive is asserted as well,
 * where a predecessor exists: deriving from the predecessor ALONE cannot see an
 * assembly that did not exist then — `HotelOS.Formats` was required by
 * `HotelOS.Jobs.deps.json` and absent from `jobs-0.4.3`, and a staging that
 * closed its arithmetic perfectly would have shipped a backend missing a
 * dependency its own runtime names. Both directions, or the check passes on a
 * set it never examined.
 *
 * # What it does NOT do
 *
 * It does not sign: `hopkg` holds the key, and a tool that both assembles and
 * signs is a tool that can publish. It does not copy into the registry either —
 * shelving is a separate act from assembling, and conflating them is how three
 * archives sat in `.build/` while an operator's Update found nothing newer.
 */

import { execFileSync } from "node:child_process";
import { cpSync, existsSync, mkdirSync, readdirSync, readFileSync, rmSync, statSync } from "node:fs";
import { basename, join, relative, resolve } from "node:path";

/** The runtime identifier this platform ships. Natives for any other are dropped. */
const RID = "win-x64";

/** Extensions a published tree carries that a property has no use for. */
const NOT_SHIPPED = [".pdb", ".xml"];

/** Read the handful of manifest facts the layout is derived from. */
function manifestFacts(dir) {
  const text = readFileSync(join(dir, "manifest.yaml"), "utf8");
  const scalar = (key) => {
    const m = text.match(new RegExp(`^${key}:\\s*"?([^"\\n#]+?)"?\\s*(?:#.*)?$`, "m"));
    return m === null ? null : m[1].trim();
  };
  const nested = (block, key) => {
    const b = text.match(new RegExp(`^${block}:\\n((?:[ \\t].*\\n|\\n)*)`, "m"));
    if (b === null) return null;
    const m = b[1].match(new RegExp(`^\\s+${key}:\\s*"?([^"\\n#]+?)"?\\s*(?:#.*)?$`, "m"));
    return m === null ? null : m[1].trim();
  };
  const facts = {
    id: scalar("id"),
    version: scalar("version"),
    // Absent means application, exactly as `installable.rs`'s `#[serde(default)]`
    // does. A derivation that required the word would read every application as
    // a kind it has never heard of.
    kind: scalar("kind") ?? "application",
    assembly: nested("runtime", "assembly"),
    declaresFiles: /^files:/m.test(text),
  };
  for (const key of ["id", "version"]) {
    if (facts[key] === null) throw new Error(`manifest.yaml declares no \`${key}:\``);
  }
  // hopkg refuses a manifest with no `files:` block at column zero, so catching
  // it here names the manifest rather than letting `sign` name the archive.
  if (!facts.declaresFiles) {
    throw new Error("manifest.yaml has no `files:` block at column zero — hopkg replaces it, but it must exist");
  }
  return facts;
}

/** Every file a published tree holds, by the archive path it would take. */
function published(root) {
  const found = new Map();
  const walk = (dir) => {
    for (const entry of readdirSync(dir, { withFileTypes: true })) {
      const full = join(dir, entry.name);
      if (entry.isDirectory()) { walk(full); continue; }
      const rel = relative(root, full).replaceAll("\\", "/");
      // A native sits at runtimes/<rid>/native/x.dll in a publish tree and at
      // backend/x.dll in a package — the flattening `jobs-0.4.3` shows.
      found.set(rel.startsWith("runtimes/") ? basename(rel) : rel, full);
    }
  };
  walk(root);
  return found;
}

/** What the built assembly itself says it needs at runtime. */
function runtimeClosure(root, assembly) {
  const deps = join(root, `${assembly.replace(/\.dll$/u, "")}.deps.json`);
  if (!existsSync(deps)) throw new Error(`no ${basename(deps)} beside the published assembly`);
  const parsed = JSON.parse(readFileSync(deps, "utf8"));
  const needed = new Set();
  for (const target of Object.values(parsed.targets ?? {})) {
    for (const body of Object.values(target)) {
      for (const path of Object.keys(body.runtime ?? {})) needed.add(basename(path));
      for (const [path, meta] of Object.entries(body.native ?? {})) {
        // Natives are listed per RID; only this platform's ship.
        if ((meta.rid ?? RID) === RID || path.includes(RID)) needed.add(basename(path));
      }
    }
  }
  return needed;
}

/** Whatever a predecessor archive declared — a floor, never the answer. */
function predecessorFloor(id) {
  const local = process.env.LOCALAPPDATA;
  if (local === undefined) return null;
  const registry = join(local, "HotelOS", "packages", "registry");
  if (!existsSync(registry)) return null;
  const mine = readdirSync(registry)
    .filter((n) => n.startsWith(`${id}-`) && n.endsWith(".hopkg"))
    .sort();
  if (mine.length === 0) return null;
  const newest = mine[mine.length - 1];
  const listed = execFileSync("powershell", ["-NoProfile", "-Command",
    `Add-Type -A System.IO.Compression.FileSystem;`
    + `[IO.Compression.ZipFile]::OpenRead('${join(registry, newest)}').Entries`
    + `| ForEach-Object { $_.FullName }`], { encoding: "utf8" });
  return { archive: newest, entries: listed.split("\n").map((l) => l.trim()).filter((l) => l.length > 0) };
}

function main(argv) {
  const args = argv.slice(2).filter((a) => !a.startsWith("--"));
  if (args.length !== 1) throw new Error("usage: pack-package.mjs <package-dir> [--out <dir>]");
  const dir = resolve(args[0]);
  const outFlag = argv.indexOf("--out");
  const facts = manifestFacts(dir);
  const out = outFlag === -1
    ? join(dir, ".build", `stage-${facts.version}`)
    : resolve(argv[outFlag + 1]);

  process.stdout.write(`packaging ${facts.id} ${facts.version}, kind ${facts.kind}\n`);
  rmSync(out, { recursive: true, force: true });
  mkdirSync(out, { recursive: true });
  cpSync(join(dir, "manifest.yaml"), join(out, "manifest.yaml"));

  if (facts.kind === "provider") {
    // `openai-1.1.0` is manifest and signature alone, and ADR 0130 makes a v1
    // provider declarative. Staging a backend for one would ship a process the
    // platform has no mechanism to run.
    process.stdout.write("  declarative: manifest.yaml only, no backend and no ui\n");
  } else {
    if (facts.assembly === null) throw new Error(`kind ${facts.kind} declares no runtime.assembly`);
    const publishDir = join(dir, ".build", `publish-${facts.version}`);
    if (!existsSync(publishDir)) {
      throw new Error(`no published backend at ${relative(dir, publishDir)} — run dotnet publish -o it first`);
    }
    const tree = published(publishDir);
    const needed = runtimeClosure(publishDir, facts.assembly);

    const staged = new Map();
    for (const [rel, full] of tree) {
      const keep = needed.has(basename(rel))
        || basename(rel) === facts.assembly.replace(/\.dll$/u, ".exe")
        || /\.(runtimeconfig|deps)\.json$/u.test(rel)
        || basename(rel) === facts.assembly;
      if (keep && !NOT_SHIPPED.some((ext) => rel.endsWith(ext))) staged.set(rel, full);
    }
    const absent = [...needed].filter((n) => ![...staged.keys()].some((s) => basename(s) === n));
    if (absent.length > 0) throw new Error(`deps.json names these and the publish tree lacks them: ${absent.join(", ")}`);

    mkdirSync(join(out, "backend"), { recursive: true });
    for (const [rel, full] of staged) cpSync(full, join(out, "backend", basename(rel)));

    const ui = join(dir, "ui");
    const built = ["module.js", "icon.svg"].filter((n) => existsSync(join(ui, n)));
    mkdirSync(join(out, "ui"), { recursive: true });
    for (const n of built) cpSync(join(ui, n), join(out, "ui", n));
    const widgets = join(ui, "widgets");
    if (existsSync(widgets)) {
      const js = readdirSync(widgets).filter((n) => n.endsWith(".js"));
      if (js.length > 0) mkdirSync(join(out, "ui", "widgets"), { recursive: true });
      for (const n of js) cpSync(join(widgets, n), join(out, "ui", "widgets", n));
    }
    process.stdout.write(`  backend  ${staged.size} files, ${needed.size} named by deps.json, 0 absent\n`);
    process.stdout.write(`  ui       ${built.join(" · ")}${existsSync(widgets) ? ` · widgets/` : ""}\n`);
  }

  // The floor, asserted rather than used: a predecessor cannot see an assembly
  // that did not exist when it was cut, so it is a cross-check and never the
  // source. Anything it declared and this did not stage is named, not dropped.
  const floor = predecessorFloor(facts.id);
  if (floor === null) {
    process.stdout.write("  no predecessor in the registry — nothing to cross-check against\n");
  } else {
    const here = new Set();
    const walk = (d) => { for (const e of readdirSync(d, { withFileTypes: true })) {
      const f = join(d, e.name);
      if (e.isDirectory()) walk(f); else here.add(relative(out, f).replaceAll("\\", "/"));
    } };
    walk(out);
    const lost = floor.entries.filter((e) => e !== "MANIFEST.sig" && !here.has(e));
    process.stdout.write(`  floor    ${floor.archive}: ${floor.entries.length} entries, ${lost.length} not staged here\n`);
    for (const e of lost) process.stdout.write(`      only in the predecessor  ${e}\n`);
  }

  const count = (() => {
    let n = 0;
    const walk = (d) => { for (const e of readdirSync(d, { withFileTypes: true })) {
      const f = join(d, e.name);
      if (e.isDirectory()) walk(f); else n += 1;
    } };
    walk(out);
    return n;
  })();
  process.stdout.write(`\n${count} files staged at ${relative(process.cwd(), out)}\n`);
  process.stdout.write(`hopkg sign "${relative(process.cwd(), out)}" --key <key> --out <dir>\n`);
  process.stdout.write("this does not sign and does not publish — both are separate acts\n");
  return 0;
}

process.exit(main(process.argv));

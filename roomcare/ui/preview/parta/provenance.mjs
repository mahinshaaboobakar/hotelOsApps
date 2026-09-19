// # Adopted, not written here — a COPY of guestops/ui/preview/parta/provenance.mjs at 2b21ebd
//
// Copied verbatim on 2026-09-19 for Room Care's Part A, as Jobs copied serve.mjs:
// an application is installable on its own and may not reach into a
// neighbour's tree, so until ADR 0154's released test-support package exists
// the choice is a copy or a wrong dependency. When that package lands this file
// is deleted in favour of it — which is what this label is for.
//
// What was measured, and what it was built from.
//
// **BB's rule, in the runtime that inherits it silently.** A Part B run either
// BUILDS what it launches, or RECORDS THE COMMIT of every out-of-process binary
// it started — otherwise the ledger says *the run happened* and cannot say
// *against what*. BB found it in .NET: `dotnet test --no-build` pins the test
// assembly and not the services a fixture launches, so two signed certificates
// were real runs of a binary that no longer exists.
//
// **Part A has the same shape and no flag to blame.** The sweep measures a
// browser rendering nine built artifacts —
//
// ```text
// module.js · preview/frame.js · preview/gallery.js · preview/widget-frame.js
// widgets/{today,occupancy,from-the-pms,business-mix,watchlist}.js
// ```
//
// — and **not one of them is tracked by git.** They are pure filesystem state,
// built at whatever moment somebody last ran `npm run build`. I have already
// measured a stale one once and recorded it as *"the binary is the state,
// again"*; that was treated as a mistake to avoid rather than as a hole in the
// method, which is exactly BB's point about their own certificate.
//
// So a fidelity number quoted from this harness is meaningless without the
// answer to *which build* — and because the artifacts are untracked, the answer
// cannot be recovered afterwards. It has to be taken at the time.
//
// **This module takes it.** {@link provenance} rebuilds every artifact from
// source and then stamps what it produced: the commit, whether the sources were
// clean at it, and a digest per artifact. Rebuilding is the strong half — after
// it, *unknown provenance* is not a state the run can be in — and the stamp is
// what a certificate quotes so a reader can tell which system was measured.
//
// **What it deliberately does not do is trust an mtime.** A bundle newer than
// its sources proves the last build came after the last edit on this machine,
// which is not the same as proving it came from these sources: a checkout, a
// branch switch or a colleague's rebuild all reorder mtimes without touching
// content. The digest is taken after a build this process ran, which is the
// only ordering that establishes anything.

// **WHAT THIS INSTRUMENT PRODUCED BEFORE IT EXISTED.** A defect in an
// instrument invalidates its output *backwards*, and the fix travels only
// forwards — so repairing one means asking what it has already produced that
// somebody may still quote. A commit cannot carry that sentence: it records
// what changed, never *and everything measured up to now carries this*. Two
// caveats live here because here is where a reader arrives.
//
// **Part A figures quoted before `5371443`** — 929 drawn · 1060 built · 715
// paired · 55 differing — are real passes whose reproducibility claim was not
// supportable at the time, for BB's reason and not a smaller one. Figures after
// it name their build.
//
// **Any widget capture taken before 2026-09-10** rendered against a
// `preview/widget-frame.js` that was five days stale, two SDK contract fixes
// behind, and short `color-scroll-thumb` and `color-scroll-thumb-strong` —
// because no build script produced it and nothing anywhere would have said so.
// The page is the one every widget capture comes from. **If you are about to
// quote one from before that date, re-take it** — the caveat is not a warning
// to carry, it is a capture to redo, and redoing it now costs one command.

import { execFileSync } from "node:child_process";
import { createHash } from "node:crypto";
import { readFileSync } from "node:fs";
import { join, resolve } from "node:path";

/**
 * Everything the harness serves that is a build product rather than a source.
 *
 * Derived from `package.json`'s own build scripts rather than listed from
 * memory — a widget added tomorrow appears here the day its build line does,
 * which is the failure this repository has recorded under several other names.
 */
function artifacts(ui) {
  const scripts = JSON.parse(readFileSync(join(ui, "package.json"), "utf8")).scripts;
  const found = new Set();

  for (const line of Object.values(scripts)) {
    // `build-module.mjs [outfile] [entry]`, and esbuild's `--outfile=`.
    for (const m of line.matchAll(/build-module\.mjs\s+(?<out>[\w./-]+\.js)/gu)) {
      found.add(m.groups.out);
    }

    for (const m of line.matchAll(/--outfile=(?<out>[\w./-]+\.js)/gu)) {
      found.add(m.groups.out.replace(/^\.\//u, ""));
    }
  }

  // `build:module` takes no outfile and defaults to `module.js` — the one case
  // the pattern above cannot see, because the argument is absent.
  if (/build-module\.mjs\s*(?:&&|$|")/u.test(scripts["build:module"] ?? "")) found.add("module.js");

  return [...found].sort();
}

/** `git` in the application's repository, or null where it cannot answer. */
function git(ui, ...args) {
  try {
    return execFileSync("git", args, { cwd: ui, encoding: "utf8" }).trim();
  } catch {
    return null;
  }
}

/**
 * Rebuild every artifact, then say what was built and from what.
 *
 * @param {string} root  the `ui` directory
 * @param {object} [options]
 * @param {boolean} [options.build]  false to stamp without rebuilding. Only for
 *   a reader inspecting an existing tree — a run that quotes numbers builds.
 * @returns {object} the record a certificate quotes
 */
export function provenance(root, { build = true } = {}) {
  const ui = resolve(root);
  const files = artifacts(ui);

  if (build) {
    // Fails loudly. A sweep that continued past a failed build would measure
    // the previous build and report it as this one, which is the whole defect
    // wearing a green.
    execFileSync("npm", ["run", "build"], { cwd: ui, stdio: "pipe", shell: true });
    execFileSync("npm", ["run", "harness"], { cwd: ui, stdio: "pipe", shell: true });
  }

  const head = git(ui, "rev-parse", "HEAD");

  // Scoped to this application's sources. A dirty file elsewhere in the shared
  // tree says nothing about what these bundles were built from, and reporting
  // the repository as dirty because a colleague is mid-edit would make every
  // run of this look unreproducible.
  const dirty = (git(ui, "status", "--porcelain", "--", ".") ?? "")
    .split("\n").map((line) => line.trim()).filter(Boolean);

  return {
    schema: 1,

    // **The half BB's certificates were missing.** `built` says this process
    // produced the artifacts; `head` and `dirty` say from what. A record with
    // `built: false` is quotable and weaker, and says so rather than reading
    // like the other kind.
    built: build,
    takenAt: new Date().toISOString(),

    source: {
      head,
      clean: dirty.length === 0,

      // Named, not counted. "3 files dirty" cannot be checked by a reader and
      // cannot be reproduced by one either.
      dirty,
    },

    // Untracked by git, every one of them — which is why the digest is here
    // and not a commit reference. Two runs quoting the same digests measured
    // the same bytes whatever either tree looked like.
    artifacts: files.map((path) => {
      const bytes = readFileSync(join(ui, path));

      return {
        path,
        bytes: bytes.length,
        sha256: createHash("sha256").update(bytes).digest("hex").slice(0, 16),
      };
    }),
  };
}

/**
 * The one-line form a gate line carries.
 *
 * Deliberately states the weak case in the same words as the strong one, so a
 * reader meeting either knows which they have without knowing the format.
 */
export function line(record) {
  const at = record.source.head === null
    ? "no commit (git could not answer)"
    : `${record.source.head.slice(0, 7)}${record.source.clean ? "" : ` + ${record.source.dirty.length} uncommitted`}`;

  return `${record.built ? "built by this run" : "NOT rebuilt by this run"}, `
    + `from ${at}, ${record.artifacts.length} artifacts`;
}

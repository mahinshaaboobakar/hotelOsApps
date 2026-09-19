import { readFileSync, readdirSync, statSync } from "node:fs";
import { join, relative } from "node:path";

import { describe, expect, it } from "vitest";

import { stylesheet as moduleSheet } from "../chrome/styles";
import { MARKS_CSS } from "../chrome/marks";
import { stylesheet as widgetSheet } from "../widgets/sheet";

/**
 * The app surface checklist's SOURCE lines (`S`), for Jobs.
 *
 * `HotelOsApps/docs/app-surface-checklist.md` (GG, `3d521ce`), derived from
 * page 64 and 64a–64e. Each `it` is named by the checklist line it answers, so
 * the audit's table and this file cite the same IDs. Lines checked by
 * measurement or capture are not here — a DOM without layout cannot see where
 * a pager sits (§6) — they are in `docs/mockups/surface-audit.mjs`.
 *
 * **Every check here was run against the code BEFORE its fix**, and the ones
 * that failed are recorded in the audit table as fails with that run as their
 * proof. They stay afterwards as guards.
 */

const ui = join(import.meta.dirname, "..");
const SHIPPED = ["application.ts", "main.ts", "board", "chrome", "screens", "widgets"];

function files(from: string): string[] {
  const at = join(ui, from);
  if (!statSync(at).isDirectory()) return at.endsWith(".ts") ? [at] : [];
  return readdirSync(at, { withFileTypes: true }).flatMap((entry) => {
    const here = join(from, entry.name);
    // Recorded fixtures are the tests' and the harness's; `tests/fixtures.test.ts`
    // already proves no shipped file imports them.
    if (here.startsWith(join("board", "recorded"))) return [];
    return entry.isDirectory() ? files(here) : here.endsWith(".ts") ? [join(ui, here)] : [];
  });
}

const sources = SHIPPED.flatMap(files).map((file) => ({
  file: relative(ui, file),
  text: readFileSync(file, "utf8"),
}));

/** The code of a source, without its comments — a rule's quotation is not a use. */
function code(text: string): string {
  return text.replace(/\/\*[\s\S]*?\*\//g, "").replace(/(^|[^:])\/\/.*$/gm, "$1");
}

/** Every stylesheet a Jobs realm assembles, as the builders build them. */
const sheets = [moduleSheet([MARKS_CSS]).textContent ?? "", widgetSheet().textContent ?? ""];
const css = sheets.join("\n");

function hits(pattern: RegExp, within = sources): string[] {
  return within.flatMap(({ file, text }) =>
    code(text).split("\n").flatMap((line, i) => (pattern.test(line) ? [`${file}:${String(i + 1)}  ${line.trim()}`] : [])),
  );
}

describe("app surface checklist — source lines, Jobs", () => {
  it("walks something, so a silent zero cannot pass", () => {
    expect(sources.length).toBeGreaterThan(20);
    expect(css.length).toBeGreaterThan(5000);
  });

  it("P3 — no BACKGROUND color-mix of ok/warn/bad that duplicates a published soft tone", () => {
    // The soft tones are the published TINT behind a state; a local color-mix of
    // the same colour into transparent, as a background, is a second spelling of
    // one. **Narrowed on its first run**: it fired on .btn.danger's border,
    // color-mix(bad 45%), which §2's C5 prescribes word for word — a border
    // strength, not a tint. A guard that fires on a legitimate case is narrowed.
    const mixes = [...css.matchAll(
      /background(?:-color)?:\s*color-mix\(in srgb,\s*var\(--color-(ok|warn|bad)[^)]*\)\s*\d+%,\s*transparent\)/g,
    )].map((m) => m[0]);
    expect(mixes, "local soft tones").toEqual([]);
  });

  it("P4 — no colour literal outside a var() fallback", () => {
    const stripped = css.replace(/var\(--[a-z0-9-]+,\s*(#[0-9a-fA-F]{3,8}|rgba?\([^)]*\))\)/g, "var()");
    const literals = [...stripped.matchAll(/#[0-9a-fA-F]{3,8}\b|rgba?\([^)]*\)/g)].map((m) => m[0]);
    expect(literals).toEqual([]);
  });

  it("P6 — every box-shadow colour is derived from --color-surface", () => {
    const shadows = [...css.matchAll(/box-shadow:([^;}]*)/g)].map((m) => m[1] ?? "");
    expect(shadows.filter((s) => !/none/.test(s) && !/--color-surface/.test(s))).toEqual([]);
  });

  it("C10 — .btn.danger.confirm is defined once, in the chrome stylesheet", () => {
    const module = moduleSheet([MARKS_CSS]).textContent ?? "";
    expect([...module.matchAll(/\.btn\.danger\.confirm\s*\{/g)]).toHaveLength(1);
    expect(hits(/\.btn\.danger\.confirm\s*\{/, sources.filter((s) => !s.file.startsWith("chrome")))).toEqual([]);
  });

  it("L6 — a td padded below 10px says why, beside it", () => {
    const narrow = [...css.matchAll(/td[^{]*\{[^}]*padding:\s*([0-9]+)px/g)]
      .filter((m) => Number(m[1]) < 10).map((m) => m[0]);
    // Found rules must carry a reason in the stylesheet source; with none found
    // there is nothing to justify.
    for (const rule of narrow) {
      const at = sources.find((s) => s.text.includes(rule));
      expect(at?.text.slice(Math.max(0, (at?.text.indexOf(rule) ?? 0) - 400), at?.text.indexOf(rule)), rule)
        .toMatch(/\/\*|reason|because/i);
    }
  });

  it("C2 (S) — the primary fill is written once, as --accent, and both users read it", () => {
    const module = moduleSheet([MARKS_CSS]).textContent ?? "";
    expect([...module.matchAll(/linear-gradient\(135deg/g)], "the 135deg gradient, written out").toHaveLength(1);
    expect([...module.matchAll(/--accent\s*:/g)], "one --accent declaration").toHaveLength(1);
    expect(module).toMatch(/\.mark\{[^}]*background:var\(--accent\)/);
    expect(module).toMatch(/\.btn\.pri\{[^}]*background:var\(--accent\)/);
  });

  it("I6 — an example date in a comment names the locale that produced it", () => {
    // "Where an example is a locale's output, write which locale." A comment
    // quoting "02 Sep 13:31" is one locale's output; without naming it the
    // example cannot be checked in eleven months of the year.
    const unnamed = sources.flatMap(({ file, text }) =>
      text.split("\n").flatMap((line, i) =>
        /(\/\/|\*).*\b\d{1,2} (Sep|Sept)\b/.test(line) && !/en-GB|locale|Asia\/Qatar/i.test(line)
          ? [`${file}:${String(i + 1)}  ${line.trim()}`]
          : [],
      ),
    );
    expect(unnamed).toEqual([]);
  });

  it("G2 — the pager renders from the SDK's pagedView and PAGER_LABELS", () => {
    const pager = sources.find((s) => s.file === join("chrome", "tabs.ts"));
    expect(pager?.text).toMatch(/\bpagedView\(/);
    expect(pager?.text).toMatch(/\bPAGER_LABELS\b/);
  });

  it("I1 — no module formats a date itself", () => {
    expect(hits(/Intl\.DateTimeFormat|toLocale(Date|Time)?String\(/)).toEqual([]);
  });

  it("I3/I5 — no displayed figure computed from this machine's clock", () => {
    // Machine time is for machine facts; an elapsed or projected figure comes
    // from the service. Date.now() / new Date() feeding a rendered value fails.
    expect(hits(/Date\.now\(\)|new Date\(\s*\)/)).toEqual([]);
  });

  it("U1 — no module formats a number itself", () => {
    expect(hits(/toLocaleString\(|Intl\.NumberFormat|\.toFixed\(/)).toEqual([]);
  });

  it("U1 — every user-facing number goes through formatNumber, not String()", () => {
    // **Wider than the checklist's own check, and reported as such.** The rule is
    // "EVERY user-facing number goes through formatNumber"; the checklist checks
    // for toLocaleString / Intl.NumberFormat / toFixed, and Jobs passed that while
    // writing 40 displayed numbers with String(n) — a strip figure, a widget
    // figure, a pager range, a tab count — none in the property's locale. What
    // String() may still do here is named, never inferred: a form value read back
    // (held… / values(…)) or a CSS length (.style).
    const allowed = /String\((held\.|values\()|\.style\.[a-zA-Z]+\s*=/;
    expect(hits(/\bString\(/).filter((line) => !allowed.test(line))).toEqual([]);
  });

  it("X10 — every read-failure sentence comes from failureDrawing", () => {
    // A read that fails is drawn by chrome/failure.ts or widgets/failed.ts, both
    // of which take their words from the SDK. Any other file composing a
    // "could not" / "did not answer" / "not granted" sentence is writing them.
    const own = hits(/["'`][^"'`]*(did not answer|could not (build|read)|not been granted|has not been allowed)[^"'`]*["'`]/)
      .filter((line) => !line.startsWith(join("chrome", "failure.ts")) && !line.startsWith(join("widgets", "failed.ts")));
    expect(own).toEqual([]);
  });
});

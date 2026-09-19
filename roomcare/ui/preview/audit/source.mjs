// The page-64 audit's source walk — the checklist's `S` lines (3d521cef), read
// from Room Care's shipped source rather than from a rendering.
//
//   node preview/audit/source.mjs      prints one verdict per line, with the hits
//
// The population is DERIVED — every `.ts` under `ui/` that ships (not tests, not
// the harness) — because the question is "which files hold CSS or render text",
// not "which files I remember" (GG's walker, 2026-09-10). The token set and the
// shell's values are read from HosPilotOS at HEAD, never copied here.

import { execFileSync } from "node:child_process";
import { readdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, relative, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const UI = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..");
// The platform checkout: the sibling of this repository, or HOSPILOTOS_ROOT for a worktree that has no sibling.
const PLATFORM = process.env.HOSPILOTOS_ROOT ?? resolve(UI, "..", "..", "..", "HosPilotOS");
const atHead = (path) => execFileSync("git", ["-C", PLATFORM, "show", `HEAD:${path}`], { encoding: "utf8" });

function walk(dir) {
  return readdirSync(dir).flatMap((name) => {
    const path = join(dir, name);
    if (["node_modules", "tests", "preview", ".parta", ".audit"].includes(name)) return [];
    if (statSync(path).isDirectory()) return walk(path);
    return path.endsWith(".ts") && !path.endsWith(".d.ts") ? [path] : [];
  });
}

const files = walk(UI).map((path) => ({ path: relative(UI, path).replaceAll("\\", "/"), text: readFileSync(path, "utf8") }));
if (files.length < 20) throw new Error(`the walk found ${files.length} files — a walk that stopped finding things would pass every line below`);

// The published set, from the SDK's own TOKENS object.
const tokensTs = atHead("packages/sdk-typescript/src/tokens.ts");
const tokensBody = tokensTs.slice(tokensTs.indexOf("export const TOKENS"), tokensTs.indexOf("} as const", tokensTs.indexOf("export const TOKENS")));
const TOKEN_NAMES = new Set([...tokensBody.matchAll(/^\s*"([a-z-]+)":/gmu)].map((m) => m[1]));
if (TOKEN_NAMES.size < 17) throw new Error(`read ${TOKEN_NAMES.size} token names from the SDK`);

// The shell's values: the first declaration of each, which is the default (dark) block.
const shellCss = atHead("apps/desktop/src/styles.css");
const SHELL = new Map();
for (const m of shellCss.matchAll(/--([a-z0-9-]+):\s*([^;]+);/gu)) if (!SHELL.has(m[1])) SHELL.set(m[1], m[2].trim());

const norm = (v) => v.toLowerCase().replace(/\s+/g, " ").replace(/\s*,\s*/g, ", ").trim();
const lineOf = (text, index) => text.slice(0, index).split("\n").length;
const hits = (re, filter = () => true) => files.flatMap((f) => [...f.text.matchAll(re)].filter((m) => filter(m, f)).map((m) => ({ at: `${f.path}:${lineOf(f.text, m.index)}`, match: m[0], m, f })));

const report = [];
const verdict = (id, v, why, list = []) => report.push({ id, v, why, hits: list.map((h) => (typeof h === "string" ? h : `${h.at}  ${h.match.slice(0, 90)}`)) });

// P1 — every custom property named is published, or derived from published ones.
const vars = hits(/var\(--([a-z0-9-]+)/gu);
// An app-local name is allowed where its every definition is built only from published tokens (§1: "derive it").
const localDefs = hits(/--([a-z0-9-]+):([^;}]+)[;}]/gu).filter((h) => !TOKEN_NAMES.has(h.m[1]));
const derived = new Set(localDefs.filter((h) => [...h.m[2].matchAll(/var\(--([a-z0-9-]+)/gu)].every((v) => TOKEN_NAMES.has(v[1])) && !/#[0-9a-f]{3,8}|rgba?\(\s*\d/iu.test(h.m[2].replace(/var\(--[a-z0-9-]+,\s*(?:[^()]|\([^()]*\))+\)/gu, ""))).map((h) => h.m[1]));
const unpublished = vars.filter((h) => !TOKEN_NAMES.has(h.m[1]) && !derived.has(h.m[1]));
verdict("P1", unpublished.length === 0 ? "PASS" : "FAIL", `${vars.length} var() uses, ${new Set(vars.map((h) => h.m[1])).size} names; ${unpublished.length} outside TOKEN_NAMES (${TOKEN_NAMES.size} published)${derived.size ? `; app-local and derived: ${[...derived].map((d) => `--${d}`).join(", ")}` : ""}`, unpublished);

// P2 — each fallback literal is the shell's own value.
const fallbacks = hits(/var\(--([a-z0-9-]+),\s*((?:[^()]|\([^()]*\))+)\)/gu);
const wrong = fallbacks.filter((h) => SHELL.has(h.m[1]) && norm(SHELL.get(h.m[1])) !== norm(h.m[2]));
verdict("P2", wrong.length === 0 ? "PASS" : "FAIL", `${fallbacks.length} fallbacks; ${wrong.length} differ from apps/desktop/src/styles.css at HEAD`,
  wrong.map((h) => `${h.at}  --${h.m[1]} falls back to ${h.m[2]}, the shell's is ${SHELL.get(h.m[1])}`));

// P3 — no local colour-mix of ok/warn/bad that the soft tones already publish.
const mixes = hits(/color-mix\(in srgb,\s*var\(--color-(ok|warn|bad)[^)]*\)\s*(\d+)%,\s*transparent\)/gu);
const duplicating = mixes.filter((h) => !(h.m[1] === "bad" && h.m[2] === "45"));
verdict("P3", duplicating.length === 0 ? "PASS" : "FAIL", `${mixes.length} colour-mixes of ok/warn/bad; the only one kept is .btn.danger's border, bad 45%, which §2 rules`, duplicating);

// P4 — no literal colour outside a var() fallback.
const stripped = files.map((f) => ({ ...f, text: f.text.replace(/var\(--[a-z0-9-]+,\s*(?:[^()]|\([^()]*\))+\)/gu, (s) => " ".repeat(s.length)) }));
const literals = stripped.flatMap((f) => [...f.text.matchAll(/#[0-9a-fA-F]{3,8}\b|\brgba?\(\s*\d/gu)].map((m) => ({ at: `${f.path}:${lineOf(f.text, m.index)}`, match: f.text.slice(m.index, m.index + 40).split("\n")[0] })));
verdict("P4", literals.length === 0 ? "PASS" : "FAIL", `${literals.length} literal colour(s) outside a fallback`, literals);

// P6 — no shadow unless derived from the surface.
const shadows = hits(/box-shadow:[^;}`]+/gu);
const badShadow = shadows.filter((h) => !/none/.test(h.match) && !/--color-surface/.test(h.match));
verdict("P6", badShadow.length === 0 ? "PASS" : "FAIL", `${shadows.length} box-shadow declaration(s)`, badShadow);

// C2 (S half) — the primary fill is written ONCE, as --accent, and derived (135deg, brand → mix(brand 62%, bad)).
// Only the 135deg brand gradient is the fill; the 45deg hatches on pending tiles are another drawing.
const fills = hits(/linear-gradient\(135deg[^`;]*/gu);
const accentDefs = hits(/--accent:\s*linear-gradient\(135deg,\s*var\(--color-brand[^)]*\),\s*color-mix\(in srgb,\s*var\(--color-brand[^)]*\)\s*62%,\s*var\(--color-bad/gu);
verdict("C2", fills.length === 1 && accentDefs.length === 1 ? "PASS" : "FAIL",
  `${fills.length} 135deg fill(s) written out; ${accentDefs.length} --accent definition(s)`, fills);

// C7 — one button class, modified.
const baseClasses = hits(/control\("([a-z-]+)/gu).map((h) => h.m[1]);
const others = [...new Set(baseClasses)].filter((c) => c !== "btn");
verdict("C7", others.length === 0 ? "PASS" : "FAIL", `control() is called with base class ${[...new Set(baseClasses)].join(", ")}`, others.map((c) => `base class "${c}"`));

// C10 — the destructive twin, defined once, in the chrome.
const twins = hits(/\.btn\.danger\.confirm\s*\{/gu);
verdict("C10", twins.length === 1 && twins[0].at.startsWith("chrome/") ? "PASS" : "FAIL", `${twins.length} definition(s)`, twins);

// L6 — a td padded under 10px says why.
const tight = hits(/td\{[^}]*padding:\s*(\d+)px[^}]*\}/gu).filter((h) => Number(h.m[1]) < 10);
// The reason sits on the line above the rule — read from the rule's own line start, not from its "td{".
const unexplained = tight.filter((h) => {
  const lineStart = h.f.text.lastIndexOf("\n", h.m.index) + 1;
  return !/\/\*[^*]*\*\/\s*$/.test(h.f.text.slice(Math.max(0, lineStart - 400), lineStart));
});
verdict("L6", unexplained.length === 0 ? "PASS" : "FAIL", `${tight.length} td rule(s) under 10px; ${unexplained.length} without a reason beside it`, unexplained);

// G1 — every paged read takes its window from the SDK's server half (HotelOS.Platform Paging.Of,
// CORE-Q13), never its own `Skip(page * size)`: two clamps drift. Read from the backend's projections.
const PROJECTIONS = resolve(UI, "..", "backend", "src", "Module", "Projections");
const projections = readdirSync(PROJECTIONS).filter((n) => n.endsWith(".cs")).map((n) => ({ path: `backend/src/Module/Projections/${n}`, text: readFileSync(join(PROJECTIONS, n), "utf8") }));
const handWindows = projections.flatMap((f) => [...f.text.matchAll(/\.Skip\((?:[^()]|\([^()]*\))*\*(?:[^()]|\([^()]*\))*\)/gu)].map((m) => `${f.path}:${lineOf(f.text, m.index)}  ${m[0]}`));
const sdkWindows = projections.flatMap((f) => [...f.text.matchAll(/Paging\.Of\(/gu)].map((m) => `${f.path}:${lineOf(f.text, m.index)}`));
verdict("G1", handWindows.length === 0 && sdkWindows.length > 0 ? "PASS" : "FAIL",
  `${sdkWindows.length} paged read(s) windowed by the SDK's Paging.Of; ${handWindows.length} computing their own. The module seam carries page → {page, pageSize, total}: the paged pattern, not a third`, [...handWindows, ...sdkWindows]);

// G1 · G2 — the pager's contract and its arithmetic.
const pagerUses = hits(/pagedView\(|PAGER_LABELS\./gu);
const handRange = hits(/\(page \+ 1\) \* |page \* pageSize/gu);
verdict("G2", pagerUses.length > 0 && handRange.length === 0 ? "PASS" : "FAIL", `${pagerUses.length} use(s) of pagedView/PAGER_LABELS; ${handRange.length} hand range computation(s). The sentence around the numbers ("showing a–b of n · k per page") is Room Care's — PAGER_LABELS publishes no range sentence`, [...pagerUses, ...handRange]);

// G10 — a pager strip coloured from the surface, never a literal.
const pagerCss = hits(/\.pager\{[^}]*\}/gu);
verdict("G10", pagerCss.every((h) => !/background/.test(h.match) || /--color-surface/.test(h.match)) ? "PASS" : "FAIL", "the pager strip draws no background at all (only the list scrolls, no sticky)", pagerCss);

// I1 · I5 — dates through the SDK, elapsed from the service.
const dates = hits(/Intl\.DateTimeFormat|toLocale(Date|Time)?String|new Date\(|Date\.now\(|\.getHours\(|\.getDate\(/gu);
verdict("I1", dates.length === 0 ? "PASS" : "FAIL", `${dates.length} use(s) of Date or Intl outside the SDK`, dates);
const elapsed = hits(/Date\.now\(\)\s*-|new Date\(\)\s*-|-\s*Date\.now\(\)/gu);
verdict("I5", elapsed.length === 0 ? "PASS" : "FAIL", `${elapsed.length} elapsed figure(s) computed from the machine clock`, elapsed);
verdict("I3", dates.length === 0 ? "PASS" : "FAIL", "machine time appears nowhere, so nowhere as a non-machine fact", dates);

// I6 — a date example in a comment names its locale.
const examples = hits(/\/\/[^\n]*\b\d{1,2} (Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Sept|Oct|Nov|Dec)\b[^\n]*|\*[^\n]*\b\d{1,2} (Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sep|Sept|Oct|Nov|Dec)\b[^\n]*/gu);
const unnamed = examples.filter((h) => !/en-GB|en-US|locale/i.test(h.match));
verdict("I6", unnamed.length === 0 ? "PASS" : "FAIL", `${examples.length} date example(s) in comments; ${unnamed.length} without their locale`, unnamed);

// U1 — every user-facing number through formatNumber.
const machineNumbers = hits(/toLocaleString\(|Intl\.NumberFormat|\.toFixed\(/gu);
const formatted = hits(/formatNumber\(/gu);
// Not display: an attribute a browser reads (aria-pressed, colspan) and a field's value a person types back.
const bare = hits(/String\((?:v|data|row|counts?|n|lane|page|paging|zone|\w+)\.?\w*\)|el\([^)]*String\(/gu).filter((h) => {
  const line = h.f.text.split("\n")[lineOf(h.f.text, h.m.index) - 1];
  return !/setAttribute\(|\.value\s*=/.test(line);
});
verdict("U1", machineNumbers.length === 0 && bare.length === 0 ? "PASS" : "FAIL",
  `${machineNumbers.length} machine formatter(s); ${formatted.length} formatNumber call(s); ${bare.length} number(s) rendered with String()`, [...machineNumbers, ...bare]);

// X10 — no failure sentence written by the application.
// Read failures only: §13 is "when a screen cannot read". A WRITE's refusal sentence (chrome/load.ts's
// saying) is §9's O5 and is reported on its own — the words are Room Care's there by design.
const own = hits(/"[^"\n]*(did not answer|could not (read|build|load)|not permitted|has not been allowed|no access)[^"\n]*"/giu,
  (m, f) => f.path !== "chrome/failure.ts" && !/saying|because|refuse/.test(f.text.slice(Math.max(0, m.index - 300), m.index))
    && !(f.path === "chrome/load.ts" && /const NOT_KNOWN/.test(f.text.slice(Math.max(0, m.index - 40), m.index))));
verdict("X10", own.length === 0 ? "PASS" : "FAIL", `${own.length} read-failure sentence(s) outside failureDrawing`, own);

for (const r of report) {
  process.stdout.write(`${r.id.padEnd(4)} ${r.v.padEnd(5)} ${r.why}\n`);
  for (const h of r.hits.slice(0, 40)) process.stdout.write(`       ${h}\n`);
  if (r.hits.length > 40) process.stdout.write(`       … ${r.hits.length - 40} more\n`);
}
process.stdout.write(`\n${files.length} files walked; ${TOKEN_NAMES.size} tokens and ${SHELL.size} shell values read at HosPilotOS HEAD\n`);

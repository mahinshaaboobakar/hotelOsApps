import { readFileSync, readdirSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";

/**
 * No screen renders a wire date raw.
 *
 * # Why this needs a test at all
 *
 * Moving a field from a rendered string to an ISO one is invisible to the
 * compiler: both are `string`, so every call site keeps building. The whole of
 * `ADR 0175` has that shape — the backend stops composing and the surface starts
 * — and the failure mode is not a red build, it is a heading reading
 * `2026-08-24` on a property's screen.
 *
 * Three of those got through this sweep and were caught by reading: the rota's
 * day headings, the printed week's, and the schedule's month. A fourth was
 * caught only because a FIXTURE still held the old shape, which is the worst
 * way to find one — the test passed for the reason the wire was wrong.
 *
 * # What this checks, and what it cannot
 *
 * Every field this module receives as an ISO date, named here, must not reach a
 * template literal or an element's text without passing through `formatDay`,
 * `formatClock` or `chrome/clock`'s `span`. It is a source rule and it is
 * therefore blind to the things a source rule is always blind to — a value
 * renamed, a new ISO field nobody adds here, an indirection through a variable.
 * It catches the shape that actually occurred three times in one afternoon.
 */

const SRC = join(import.meta.dirname, "..");

/**
 * The fields the wire carries as ISO, and which a screen must format.
 *
 * **`from` and `to` are deliberately NOT here**, and their absence is the
 * narrowing rather than an exemption. Three models in this module use those two
 * names for three different things: `DutySpan.from` is a NUMBER — a fraction of
 * the week, used to position a bar — `Span.from` is a clock string that
 * `chrome/clock` composes, and the Reports range is an ISO date. A check keyed
 * on the name conflates all three, and its first run reported
 * `ribbon.ts:49 draws .from unformatted` on `${(span.from * 100).toFixed(3)}%`.
 *
 * A matching set of NAMES is not a matching set of MEANINGS. A guard that had
 * to carry an exemption for the arithmetic would be one exemption from covering
 * nothing, so the ambiguous names leave the list and the reason stays here.
 */
const ISO = ["monday", "sunday", "postedAt", "month"];

/** Reading them THROUGH one of these is the whole point. */
const FORMATTERS = /formatDay|formatClock|formatInstant|\bspan\(/u;

function sources(directory: string): string[] {
  const found: string[] = [];

  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const path = join(directory, entry.name);

    if (entry.isDirectory() && entry.name !== "node_modules" && entry.name !== "tests") {
      found.push(...sources(path));
    } else if (entry.isFile() && entry.name.endsWith(".ts")) {
      found.push(path);
    }
  }

  return found;
}

describe("a wire date never reaches a screen raw", () => {
  it("passes every ISO field through a formatter where it is drawn", () => {
    const offenders: string[] = [];

    for (const path of sources(join(SRC, "screens")).concat(sources(join(SRC, "widgets")))) {
      const text = readFileSync(path, "utf8");

      for (const [index, line] of text.split("\n").entries()) {
        // Only lines that DRAW: a template literal or an element's content.
        const draws = /\$\{[^}]*\}|el\(/u.test(line);
        if (!draws || FORMATTERS.test(line) || line.trim().startsWith("//")) continue;

        for (const field of ISO) {
          if (new RegExp(String.raw`\.${field}\b`, "u").test(line)) {
            offenders.push(
              `${path.slice(SRC.length + 1)}:${index + 1} draws .${field} unformatted`);
          }
        }
      }
    }

    expect(offenders).toEqual([]);
  });

  it("finds something, so a silent zero cannot pass for conformance", () => {
    // The population this walks must be non-empty, or the check above passes by
    // measuring nothing. A zero from a walk is a claim about the walk first.
    const files = sources(join(SRC, "screens")).concat(sources(join(SRC, "widgets")));

    expect(files.length).toBeGreaterThan(15);
    expect(files.some((one) => readFileSync(one, "utf8").includes("formatDay"))).toBe(true);
  });
});

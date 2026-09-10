import { readFileSync } from "node:fs";
import { join } from "node:path";

import { describe, expect, it } from "vitest";

import { PANELS } from "../preview/widgets";

/**
 * What the capture harness can drive, against what the manifest declares.
 *
 * **The population nobody enumerates.** On 2026-09-10 the certificate said
 * *"built and captured"* of six widgets and `preview/frame.ts` could drive
 * exactly one: `?widget=` mapped to `jobsNow`, and every other value fell
 * through to it. Nothing caught it, because **a capture that was never taken
 * leaves no artefact to be stale** — every instrument here looks at what
 * exists, and none looks at what a harness is *able* to do.
 *
 * So this compares a capability against a declaration: every widget the
 * manifest ships must be reachable in the harness. A walker rather than a
 * declaration table, because both sides are already written down — the manifest
 * declares and the harness maps; nothing new has to be maintained.
 */
describe("the capture harness can reach everything the manifest ships", () => {
  const root = join(import.meta.dirname, "..", "..");

  it("drives every declared widget", () => {
    const manifest = readFileSync(join(root, "manifest.yaml"), "utf8");
    const declared = [...manifest.matchAll(/^\s+file: ui\/widgets\/([a-z-]+)\.js$/gm)]
      .map((found) => found[1])
      .filter((widget): widget is string => widget !== undefined);

    // **The map itself, not the file's text.** The first version of this read
    // `frame.ts` for the widget's name and reported `blocked` unreachable —
    // the map used shorthand property syntax, so the string was never written
    // and the widget was perfectly drivable. A check that reads text cannot see
    // behaviour, which is the defect this whole guard exists to answer.
    const unreachable = declared.filter(
      // `jobs-now` is reached by its three drawn states rather than by its id.
      (widget) => widget !== "jobs-now" && !Object.hasOwn(PANELS, widget),
    );

    expect(declared.length, "the manifest declares widgets to capture").toBeGreaterThan(0);
    expect(unreachable, "every declared widget can be driven by the capture harness").toEqual([]);
  });
});

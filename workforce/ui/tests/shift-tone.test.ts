import { readFileSync } from "node:fs";
import { join } from "node:path";

import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { recordedPolicy, type Policy } from "../roster/policy";
import { PALETTE } from "../screens/policy/dialog";
import { policy } from "../screens/policy";
import { shifts } from "../screens/shifts";

/**
 * A shift's colour is drawn in the tone the SERVICE gives it (ledger D3).
 *
 * Four places mapped a colour name to a tone, and they disagreed: `swatch()` in
 * Shifts and in Policy drew Rose neutral and Violet brand, while the service's
 * `Wording.Tone` (what the rota and the schedule draw) makes Rose bad and Violet
 * neutral. So one shift was two colours on two screens.
 *
 * The rows now carry `tone`, and the screens map nothing. The one list left in
 * the UI is the New shift dialog's palette: the drawn choice a person picks
 * from, which has to exist before any row does. It is held to the service's own
 * table by reading `Wording.cs` itself, so the two cannot drift silently.
 *
 * The fixture row is Rose because Rose is where the old copies disagreed with
 * the service: a screen still mapping locally draws `neutral` and fails here.
 */
const rose: Policy = {
  ...recordedPolicy,
  catalogue: [{ ...recordedPolicy.catalogue[0]!, colour: "Rose", tone: "bad" }],
};

function host(): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (_capability: string, method: string) => method === "policy"
      ? Promise.resolve(rose)
      : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" })),
    on: () => () => {},
  };
}

describe("a shift's tone", () => {
  for (const [name, draw] of [
    ["Policy", (h: HostApi, m: HTMLElement) => policy(h, m)],
    ["Shifts", (h: HostApi, m: HTMLElement) => shifts(h, m)],
  ] as const) {
    it(`${name} draws the tone the service sent`, async () => {
      const main = document.createElement("div");
      await draw(host(), main);

      const chips = Array.from(main.querySelectorAll(".code, .dot"));
      // Positive control: the row was drawn, so a missing class is a finding.
      expect(chips.length).toBeGreaterThan(0);
      expect(chips.every((one) => one.classList.contains("bad"))).toBe(true);
    });
  }
});

/** The service's colour→tone arms, read from its source rather than restated. */
function serviceTones(): Map<string, string> {
  const source = readFileSync(
    join(import.meta.dirname, "..", "..", "backend", "src", "Module", "Views", "Wording.cs"), "utf8");
  const body = /public static string Tone\(string colour\)[\s\S]*?\};/.exec(source)?.[0] ?? "";
  const tones = new Map<string, string>();
  for (const arm of body.matchAll(/((?:"[A-Z]+"(?:\s+or\s+)?)+)\s*=>\s*"(\w+)"/g)) {
    for (const colour of arm[1]!.matchAll(/"([A-Z]+)"/g)) tones.set(colour[1]!, arm[2]!);
  }
  return tones;
}

/** Every palette entry whose tone is not the one the service would give it. */
function drift(palette: readonly { name: string; tone: string }[]): string[] {
  const tones = serviceTones();
  return palette
    .filter(({ name, tone }) => tone !== (tones.get(name.toUpperCase()) ?? "neutral"))
    .map(({ name, tone }) => `${name} → ${tone}`);
}

describe("the New shift palette", () => {
  it("reads the service's table", () => {
    // Positive control: the parser found the service's arms, including the
    // colour the old copies got wrong.
    const tones = serviceTones();
    expect(tones.get("ROSE")).toBe("bad");
    expect(tones.size).toBeGreaterThanOrEqual(8);
  });

  it("finds a colour drawn in a tone the service would not give it", () => {
    // The old Shifts copy's answer for Rose, planted: the check must refuse it.
    expect(drift([{ name: "Rose", tone: "neutral" }])).toEqual(["Rose → neutral"]);
  });

  it("draws each colour in the tone the service will give it", () => {
    expect(drift(PALETTE)).toEqual([]);
  });
});

/** The catalogue row `PolicyView.Read` actually builds, read from its source. */
function catalogueRow(): string {
  const source = readFileSync(
    join(import.meta.dirname, "..", "..", "backend", "src", "Module", "Views", "PolicyView.cs"),
    "utf8");

  return /rows\.Add\(new[\s\S]*?\}\);/.exec(source)?.[0] ?? "";
}

/**
 * What the read sends, checked against the source rather than against a fixture.
 *
 * # A fixture cannot see a field the service never sends
 *
 * `CatalogueRow` requires `tone`, both screens render `row.tone`, and the
 * fixture carries it — so every test above passes while **`PolicyView.Read`
 * never emitted the field at all**. Against a real property the swatch and the
 * code chip carried `undefined` as a class, and a shift drawn Rose on the rota
 * was unstyled on the screen that defines it.
 *
 * The suite could not have failed on that: it renders the fixture, and the
 * fixture is a claim about the wire that nothing was checking. So the check is
 * on the service's own source, where the absence is.
 */
describe("what the catalogue read sends", () => {
  it("reads the row the service builds", () => {
    // Positive control: a zero from a parser is a claim about the parser first.
    const row = catalogueRow();
    expect(row).toContain("colour =");
    expect(row).toContain("kind =");
  });

  it("sends the tone, from the service's own table", () => {
    // Not `tone = "brand"` and not a second mapping: the one table, the one the
    // rota and the schedule already draw.
    expect(catalogueRow()).toContain("tone = Wording.Tone(");
  });

  it("sends the assignment count as a number", () => {
    const inUse = /inUse = .*/.exec(catalogueRow())?.[0] ?? "";

    expect(inUse).not.toBe("");
    // It was `count + " assignments"` — an English noun composed in a service,
    // with the figure in the service's own grouping inside it. A string literal
    // anywhere on this line is that defect returning (NUM-Q1, ADR 0174).
    expect(inUse).not.toMatch(/"/);
  });
});

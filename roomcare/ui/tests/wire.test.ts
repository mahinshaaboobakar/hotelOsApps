import { existsSync, readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";

import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { arrivalsWaiting, attendantsNow, attention, pendingPolicy, roomsReady } from "../widgets/panel/panels";
import { ATTENDANT, SUPERVISOR, click, host, mount, settle } from "./host";

/**
 * Mock to wire — proved, not asserted: nothing a person sees comes from the
 * recorded fixtures, on any screen or widget.
 *
 * FF's completeness proof was to delete the fallback parameter and read what
 * the compiler then said about the fixtures' importers. Room Care never had a
 * fallback parameter — `load` is the SDK's and takes none — so there is nothing
 * to delete, and the proof has to come from the other side. Three instruments,
 * each over a population derived from the fixtures themselves rather than typed
 * here:
 *
 * 1. **The bundles.** Every value the fixtures carry that could only be data —
 *    every id, and every name, room number, zone and room type — is searched
 *    for in every built bundle the package ships. A fixture reachable from the
 *    shipped graph would carry its values into the bundle, as GuestOps' did.
 * 2. **The rendered surfaces, failing.** Every screen and widget is drawn with
 *    every read failing; none may show a fixture value. A screen that drew from
 *    anywhere but its read would show one.
 * 3. **The positive control.** The same instrument, with reads answering, must
 *    find those values — or a clean result would only mean it could not see.
 *
 * The compiler's half is `tests/seam.test.ts`: a value cannot be read off a
 * failed `Read<T>`, and no shipped file imports a fixture.
 */

const RECORDED = join(process.cwd(), "preview", "recorded");

/** Keys whose values are the property's data, not Room Care's vocabulary. */
const DATA_KEYS = new Set(["name", "number", "room", "attendant", "by", "property", "setBy", "grantedBy", "preparedBy", "runBy", "decidedBy", "oursBy", "roomType", "type", "zone"]);

/**
 * Two fixture values are also Room Care's own words and are excluded, by name:
 * **Housekeeping** is the canon department (ADR 0119) the copy names on purpose,
 * and **PREPARE** is a trigger mode's wire word. Neither is evidence of a fixture.
 */
const VOCABULARY = new Set(["Housekeeping", "PREPARE"]);

const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/u;

function tokens(): Set<string> {
  const found = new Set<string>();
  const walk = (value: unknown, key: string | null): void => {
    if (Array.isArray(value)) value.forEach((v) => walk(v, key));
    else if (value !== null && typeof value === "object") Object.entries(value).forEach(([k, v]) => walk(v, k));
    else if (typeof value === "string" && !VOCABULARY.has(value) && (UUID.test(value) || (key !== null && DATA_KEYS.has(key) && value.length >= 3))) found.add(value);
  };
  for (const file of readdirSync(RECORDED).filter((f) => f.endsWith(".json"))) walk(JSON.parse(readFileSync(join(RECORDED, file), "utf8")), null);
  return found;
}

const TOKENS = tokens();
const shown = (text: string): string[] => [...TOKENS].filter((t) => text.includes(t));

const failingAll = (capabilities: readonly string[]): HostApi => ({
  ...host(capabilities),
  call: (_capability, method) => Promise.reject(new HostCallError({ kind: "unavailable", message: `the test failed ${method}` })),
});

/** Every section a person can reach, and every Setup tab — driven through the bar, as a person would. */
const SUPERVISOR_WALK: readonly (readonly string[])[] = [
  [], ["Prepare"], ["Room states"], ["Supervision"], ["Deep clean"],
  ...["Windows & trigger", "Services & minutes", "Rules", "Assignment & zones", "Areas", "Deep clean plan", "Property-wide access"].map((tab) => ["Setup", tab]),
];

async function walk(hostApi: HostApi, section: readonly string[]): Promise<HTMLElement> {
  const root = mount(activate, hostApi);
  await settle();
  if (section[0] !== undefined) click(root, "button.tab", section[0]);
  await settle();
  if (section[1] !== undefined) click(root, ".subnav button.tab", section[1]);
  await settle();
  return root;
}

describe("the population is derived and real", () => {
  it("draws its values from every fixture, and they are the property's data", () => {
    expect(TOKENS.size).toBeGreaterThan(60);
    for (const value of ["Coral Cove Resort", "Anita Pillai", "G01", "L09"]) expect(TOKENS).toContain(value);
  });
});

describe("no fixture value ships in a bundle", () => {
  const bundles = ["module.js", ...readdirSync(join(process.cwd(), "widgets")).filter((f) => f.endsWith(".js")).map((f) => `widgets/${f}`)];

  it("finds the six bundles the package ships — build them first; a missing bundle is a failure, not a pass", () => {
    expect(bundles.sort()).toEqual(["module.js", "widgets/arrivals-waiting.js", "widgets/attendants-now.js", "widgets/attention.js", "widgets/pending-policy.js", "widgets/rooms-ready.js"]);
    for (const bundle of bundles) expect(existsSync(join(process.cwd(), bundle))).toBe(true);
  });

  for (const bundle of ["module.js", "widgets/arrivals-waiting.js", "widgets/attendants-now.js", "widgets/attention.js", "widgets/pending-policy.js", "widgets/rooms-ready.js"]) {
    it(`${bundle} carries none of them`, () => {
      expect(shown(readFileSync(join(process.cwd(), bundle), "utf8"))).toEqual([]);
    });
  }
});

describe("no surface draws a fixture value when its reads fail", () => {
  for (const section of SUPERVISOR_WALK) {
    it(`the supervisor's ${section.join(" › ") || "Board"}`, async () => {
      const root = await walk(failingAll(SUPERVISOR), section);
      expect(root.querySelector(".fail")).not.toBeNull();
      expect(shown(root.textContent ?? "")).toEqual([]);
    });
  }

  it("the attendant's My rooms", async () => {
    const root = await walk(failingAll(ATTENDANT), []);
    expect(root.querySelector(".fail")).not.toBeNull();
    expect(shown(root.textContent ?? "")).toEqual([]);
  });

  for (const panel of [roomsReady, arrivalsWaiting, attention, attendantsNow, pendingPolicy]) {
    it(`the ${panel.name} widget`, async () => {
      const card = await panel(failingAll(["roomcare.read"]));
      expect(card.querySelector(".wfail")).not.toBeNull();
      expect(shown(card.textContent ?? "")).toEqual([]);
    });
  }
});

describe("the positive control — the same instrument sees the values when reads answer", () => {
  it("finds fixture values on the board, My rooms and a widget", async () => {
    expect(shown((await walk(host(SUPERVISOR), [])).textContent ?? "").length).toBeGreaterThan(10);
    expect(shown((await walk(host(ATTENDANT), [])).textContent ?? "")).toContain("G01");
    expect(shown((await attendantsNow(host(["roomcare.read"]))).textContent ?? "")).toContain("Anita Pillai");
  });
});

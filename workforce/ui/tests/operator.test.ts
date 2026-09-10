import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";

/**
 * The line naming who is signed in, and every way it can be unknown.
 *
 * The module used to draw `Priya Thomas · Front Office · Kochi Beach Resort`
 * from a constant, on every screen, above every write those screens attribute.
 * It now asks its own backend — `roster.read/me` — and these assert the part
 * that a type cannot: that an answer it could not get is **drawn as nothing**
 * rather than as a placeholder, a separator with nothing beside it, or the word
 * `undefined`.
 *
 * The last of those is not hypothetical. The bar joined three fields into one
 * template literal, so any of them arriving absent would have rendered the
 * string "undefined" between two middle dots — a fabricated name in the one
 * place a person reads to find out whose account they are looking at.
 */

/** A host that answers `me` with whatever a test hands it, and refuses the rest. */
function host(me: unknown, granted: readonly string[] = ["roster.read"]): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: granted },
    property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    call: (_capability: string, method: string) => method === "me"
      ? Promise.resolve(me)
      : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" })),
    on: () => () => {},
  };
}

async function settle(): Promise<void> {
  for (let turn = 0; turn < 8; turn += 1) {
    await new Promise((resolve) => { setTimeout(resolve, 1); });
  }
}

async function mounted(me: unknown, granted?: readonly string[]): Promise<HTMLElement> {
  const root = document.createElement("div");
  activate(host(me, granted)).mount(root);
  await settle();

  return root;
}

/** What the bar actually shows, or null when it shows no operator at all. */
function line(root: HTMLElement): string | null {
  return root.querySelector(".head .who")?.textContent ?? null;
}

describe("the operator line", () => {
  it("names the person, their department and the property", async () => {
    const root = await mounted({
      name: "Priya Thomas",
      department: "Front Office",
      property: "Kochi Beach Resort",
      role: "Head of Front Office",
    });

    // Three clauses, owner ruling 2026-09-04. The property looks redundant on a
    // single-property desk and stops looking redundant the day an organization
    // has two.
    expect(line(root)).toBe("Priya Thomas · Front Office · Kochi Beach Resort");
  });

  it("draws nothing at all when the backend could name nobody", async () => {
    const root = await mounted({
      name: null, department: null, property: null, role: null,
    });

    // A service caller, or a login with no staff record at a property Master
    // Data has not named. There is no element — not an empty one, and not a
    // placeholder. The bar simply does not say who is looking at it.
    expect(line(root)).toBeNull();
  });

  it("drops the clauses it does not know and keeps the ones it does", async () => {
    const root = await mounted({
      name: "Priya Thomas", department: "Front Office", property: null, role: null,
    });

    // No trailing separator, and nothing invented to sit after one. A person
    // known by name at a hotel nobody named is exactly this line.
    expect(line(root)).toBe("Priya Thomas · Front Office");
  });

  it("treats a blank string as an absence rather than as a value", async () => {
    const root = await mounted({
      name: "Priya Thomas", department: "   ", property: "Kochi Beach Resort", role: null,
    });

    // This is FF's scar, and the reason the guard is a non-empty string rather
    // than `!== null`: a field that arrives as whitespace is not null, passes
    // every null check, and draws `Priya Thomas ·   · Kochi Beach Resort`.
    expect(line(root)).toBe("Priya Thomas · Kochi Beach Resort");
  });

  it("draws nothing when a field arrives absent rather than null", async () => {
    const root = await mounted({ property: "Kochi Beach Resort" });

    // JSON omits as readily as it nulls, and `undefined` is the value that
    // reaches a template literal as the six letters of its own name.
    expect(line(root)).toBe("Kochi Beach Resort");
  });

  it("draws no operator when the read fails", async () => {
    const root = await mounted(
      { name: "Priya Thomas", department: "Front Office", property: "Kochi", role: null },
      []);

    // `roster.read` not granted, so the seam refuses before the round trip.
    // A failed `me` is not a failure of the screen the person asked for — the
    // module still draws Rota, and simply does not caption it with a name it
    // could not establish.
    expect(line(root)).toBeNull();
    expect(root.querySelector(".head")).not.toBeNull();
  });
});

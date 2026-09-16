import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { policy } from "../screens/policy";
import { recordedPolicy } from "../roster/policy";

/**
 * What a property has not set, drawn as not set.
 *
 * # This assertion moved here, and the move is the point
 *
 * `PolicyView` used to send the em-dash: `"9 h / day"` where a threshold was
 * configured and `"—"` where it was not, so a service chose both the unit and
 * the character standing for absence. `ADR 0174` sends the number and null
 * instead, which put the choice where a reader's own notation lives — and left
 * the rule itself, *absent is not zero*, checked on the wire but drawn nowhere.
 *
 * The backend's own test kept the half it can still see: the field crosses as
 * `null`, and explicitly not as a number. This is the other half. **A rule that
 * moves layers has to be re-asserted at the layer it moved to, or it survives
 * as a sentence in a commit message.**
 */

/** A host that answers `policy` with whatever a test hands it. */
function host(answer: unknown, locale = "en-IN"): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale },
    call: (_capability: string, method: string) => method === "policy"
      ? Promise.resolve(answer)
      : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" })),
    on: () => () => {},
  };
}

async function drawn(answer: unknown, locale?: string): Promise<string> {
  const main = document.createElement("div");
  await policy(host(answer, locale), main, false, () => {}, () => {});

  return main.textContent ?? "";
}

describe("policy, where a property has set nothing", () => {
  it("draws an em-dash for an overtime threshold nobody set", async () => {
    const text = await drawn({ ...recordedPolicy, overtimeDaily: null });

    expect(text).toContain("—");

    // The half the name of the old backend test carried: **not as zero**. A
    // property that set no threshold and one that set zero are different facts,
    // and zero is the one that reads as "never warn".
    expect(text).not.toContain("0 h / day");
  });

  it("draws the threshold with its unit when there is one", async () => {
    const text = await drawn({ ...recordedPolicy, overtimeDaily: 9 });

    // Composed on this side now — the number, the unit and the word for the
    // period. The service sends 9 and says nothing about how it reads.
    expect(text).toContain("9 h / day");
  });

  it("renders the accrual in the property's locale, not the machine's", async () => {
    const rate = { ...recordedPolicy.leave[0], type: "Casual", accruesPerMonth: 1.25 };
    const text = await drawn(
      { ...recordedPolicy, leave: [rate] }, "de-DE");

    // `1,25` is CORRECT when the property says de-DE and an accident when a
    // process culture does — and the difference is not visible in the output,
    // which is why this asserts the locale rather than the digits alone.
    expect(text).toContain("1,25 / month");
    expect(text).not.toContain("1.25 / month");
  });

  it("says a type is granted by hand rather than showing no rate at all", async () => {
    const granted = { ...recordedPolicy.leave[0], type: "Comp-off", accruesPerMonth: null };
    const text = await drawn({ ...recordedPolicy, leave: [granted] });

    // Absent is an answer here, and it is a different one from "accrues 0".
    expect(text).toContain("Granted by HR");
    expect(text).not.toContain("0 / month");
  });
});

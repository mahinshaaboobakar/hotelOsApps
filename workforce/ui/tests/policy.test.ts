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
  // **Both rewritten under ADR 0034 when the thresholds became inputs
  // (2026-09-19).** They read the page's text for "—" and "9 h / day"; an
  // input's value is not text. And the em-dash test had gone vacuous before
  // that — other em-dashes on the page satisfied `toContain("—")` whatever the
  // threshold drew. The rule is unchanged and re-asserted on the box itself.
  it("opens an empty box, never zero, for an overtime threshold nobody set", async () => {
    const main = document.createElement("div");
    await policy(host({ ...recordedPolicy, overtimeDaily: null }), main, false, () => {}, () => {});

    // The half the name of the old backend test carried: **not as zero**. A
    // property that set no threshold and one that set zero are different facts,
    // and zero is the one that reads as "never warn".
    expect(main.querySelector<HTMLInputElement>("input[name=daily]")?.value).toBe("");
  });

  it("opens the threshold's number, with its unit beside it", async () => {
    const main = document.createElement("div");
    await policy(host({ ...recordedPolicy, overtimeDaily: 9 }), main, false, () => {}, () => {});

    // The service sends 9 and says nothing about how it reads; the unit is
    // the screen's.
    const box = main.querySelector<HTMLInputElement>("input[name=daily]");
    expect(box?.value).toBe("9");
    expect(box?.parentElement?.textContent).toContain("hours a day");
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

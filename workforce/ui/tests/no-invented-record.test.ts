import type { HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { recordedRegister } from "../roster/duty";
import { recordedPolicy } from "../roster/policy";
import { recordedWeek } from "../roster/recorded";
import { printed } from "../screens/printed";
import { shifts } from "../screens/shifts";

/**
 * A screen states only what a service told it — and paper most of all.
 *
 * The app surface audit (2026-09-19) found production code writing a property,
 * people, an issue time and four personnel records as literals: the printed
 * rota carried *"Kochi Beach Resort · … issued Fri 21 Aug, 16:40 by P. Thomas"*,
 * *"Page 1 of 1 · Printed 24 Aug 2026"*, and a *"Changes since this rota was
 * issued"* list — *"S. Iyer marked sick … approved by P. Thomas"* — on every
 * property's paper. Shifts named the same property in its header.
 *
 * The phrases below are the invented ones, not the fixture's own rows: a
 * printed week legitimately shows the names its week holds.
 */

const INVENTED = [
  "Kochi Beach Resort",
  "issued Fri 21 Aug",
  "by P. Thomas",
  "Page 1 of 1",
  "Printed 24 Aug 2026",
  "Changes since this rota was issued",
  "R. Nair took MOD",
  "S. Iyer marked sick",
  "approved by P. Thomas",
];

function host(answers: Record<string, unknown>): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (_capability, method) => Promise.resolve(answers[method]),
    on: () => () => {},
  };
}

function clean(text: string, where: string): void {
  for (const phrase of INVENTED) {
    expect(text, `${where} states "${phrase}", which no service said`).not.toContain(phrase);
  }
}

describe("no screen states a record nobody made", () => {
  it("the printed rota", async () => {
    const root = document.createElement("div");
    await printed(host({ week: recordedWeek, register: recordedRegister }), root);
    expect(root.querySelector(".page"), "the printed page did not render").not.toBeNull();
    clean(root.textContent ?? "", "the printed rota");
  });

  it("the print preview names the week it was given, not the one it was drawn with", async () => {
    // **The fixture is chosen so a literal cannot pass.** The recorded week is
    // Front Office, 24–30 August — the same words the preview once wrote as a
    // literal — so a test on that week passes whether the line is derived or
    // typed. A different department and week can only appear if derived.
    const root = document.createElement("div");
    const week = { ...recordedWeek, department: "Housekeeping", monday: "2026-10-05", sunday: "2026-10-11" };
    await printed(host({ week, register: recordedRegister }), root);

    const line = root.querySelector(".pbar .hsub")?.textContent ?? "";
    expect(line).toContain("Housekeeping");
    expect(line).not.toContain("Front Office");
    expect(line).not.toContain("August");
    expect(line).not.toContain("A4 landscape");
  });

  it("the shift catalogue", async () => {
    const main = document.createElement("main");
    await shifts(host({ policy: recordedPolicy }), main);
    expect(main.querySelector(".hsub"), "the shifts header did not render").not.toBeNull();
    clean(main.textContent ?? "", "the shift catalogue");
  });
});

import type { HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import type { Person, Week } from "../roster";
import { recordedWeek } from "../roster/recorded";
import { picker } from "../screens/rota/picker";

/**
 * A rota cell is assigned under the ROW's department, not the week's filter.
 *
 * On an installed property the week is read with no department named — the
 * screen has never had a way to name one — so the read answers
 * `department: null, departmentCode: ""`. The picker sent that `""` and the
 * service refused every assignment: *"department_code is required"*
 * (`RotaAssignWireTests`). The fixture carried `"FO"` for both, so no
 * rendering could show it.
 *
 * The values are chosen so the two sources disagree: the week says nothing,
 * the row says `HK`. A picker still reading the week sends `""`.
 */

interface Call { capability: string; method: string; params?: unknown }

function host(calls: Call[]): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.plan"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (capability: string, method: string, params?: unknown) => {
      calls.push({ capability, method, params });
      return Promise.resolve({ id: "c-1", version: 1 });
    },
    on: () => () => {},
  };
}

/** The week as a real property's read answers it: no department named. */
const unfiltered: Week = { ...recordedWeek, department: null, departmentCode: "" };

const meera: Person = {
  id: "p-meera", name: "Meera Pillai", initials: "MP", role: "Room attendant",
  departmentCode: "HK", head: false,
  week: recordedWeek.people[0]!.week.map(() => ({ shift: null, override: null, leave: null, gap: false })),
};

async function settle(): Promise<void> {
  for (let turn = 0; turn < 8; turn += 1) {
    await new Promise((resolve) => { setTimeout(resolve, 1); });
  }
}

describe("assigning a rota cell", () => {
  it("names the row's department when the week named none", async () => {
    const calls: Call[] = [];
    const pop = picker(host(calls), meera, 0, unfiltered, () => {}, () => {});
    document.body.append(pop);

    pop.querySelector<HTMLElement>(".picks > *")!.click();
    pop.querySelector<HTMLButtonElement>(".acts button:last-of-type")!.click();
    await settle();

    const assign = calls.find((one) => one.method === "assign");
    expect(assign, "the picker wrote nothing").toBeDefined();
    expect((assign!.params as { department: string }).department).toBe("HK");
    pop.remove();
  });

  it("says no department it was not given", () => {
    const pop = picker(host([]), meera, 0, unfiltered, () => {}, () => {});
    const words = Array.from(pop.querySelectorAll(".hsub"), (one) => one.textContent ?? "");

    // Positive control: the sub-line is there, so an absent word is a finding.
    expect(words.join("")).toContain("one shift per day");
    expect(words.join("")).not.toMatch(/null|undefined|^ · /);
  });
});

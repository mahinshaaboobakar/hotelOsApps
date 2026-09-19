import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { recordedPolicy } from "../roster/policy";
import { policy } from "../screens/policy";

/**
 * Policy's Save — the overtime thresholds, which the owner met dead on 0.3.3.
 *
 * `roster.configure · setOvertime` takes `daily` and `weekly`, and writes both
 * every time: an omitted one is cleared (`PolicyService.SetOvertimeAsync`). So
 * the form sends what its two boxes hold, and an empty box means *no threshold*
 * — which the screen says. The service refuses zero, more than 24 hours a day
 * and more than 168 a week, so Save waits on those with the same reasons.
 *
 * Leave types and shift rows stay read-only here: their writes need an id and a
 * version the Policy read does not send.
 */

interface Call { capability: string; method: string; params?: unknown }

function host(calls: Call[]): HostApi {
  return {
    identity: {
      id: "workforce", version: "0.1.0", capabilities: ["roster.read", "roster.configure"],
    },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (capability: string, method: string, params?: unknown) => {
      calls.push({ capability, method, params });
      if (method === "policy") return Promise.resolve(recordedPolicy);
      if (method === "setOvertime") return Promise.resolve({ version: 2 });
      return Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" }));
    },
    on: () => () => {},
  };
}

const settle = async (): Promise<void> => {
  for (let turn = 0; turn < 6; turn += 1) {
    await new Promise((resolve) => { setTimeout(resolve, 1); });
  }
};

function type(main: HTMLElement, name: string, value: string): void {
  const input = main.querySelector<HTMLInputElement>(`input[name=${name}]`);
  if (input === null) throw new Error(`no ${name} threshold on the screen`);
  input.value = value;
  input.dispatchEvent(new Event("input"));
}

const save = (main: HTMLElement): HTMLButtonElement | null =>
  Array.from(main.querySelectorAll<HTMLButtonElement>("button"))
    .find((one) => one.textContent === "Save changes") ?? null;

const reason = (main: HTMLElement): string =>
  save(main)?.closest(".unavail")?.querySelector(".why")?.textContent ?? "";

describe("policy save", () => {
  it("opens holding the property's thresholds, with nothing to save yet", async () => {
    const main = document.createElement("div");
    await policy(host([]), main);

    expect(main.querySelector<HTMLInputElement>("input[name=daily]")?.value).toBe("9");
    expect(main.querySelector<HTMLInputElement>("input[name=weekly]")?.value).toBe("48");
    expect(save(main)?.hasAttribute("disabled")).toBe(true);
    expect(reason(main)).toBe("Change a threshold to save it");
  });

  it("saves exactly what the boxes hold, and an empty box clears its threshold", async () => {
    const calls: Call[] = [];
    const main = document.createElement("div");
    await policy(host(calls), main);

    type(main, "daily", "10");
    type(main, "weekly", "");
    expect(save(main)?.hasAttribute("disabled")).toBe(false);

    save(main)?.click();
    await settle();

    expect(calls.filter((one) => one.method === "setOvertime")).toEqual([{
      capability: "roster.configure", method: "setOvertime", params: { daily: 10 },
    }]);
    // Re-read after the save: the screen draws what is stored, not what was typed.
    expect(calls.filter((one) => one.method === "policy").length).toBe(2);
  });

  it("waits, with the service's own reasons, on a threshold it would refuse", async () => {
    const calls: Call[] = [];
    const main = document.createElement("div");
    await policy(host(calls), main);

    type(main, "daily", "0");
    expect(reason(main)).toBe("A threshold is a number of hours above zero");
    type(main, "daily", "25");
    expect(reason(main)).toBe("A day has 24 hours");
    type(main, "daily", "9");
    type(main, "weekly", "200");
    expect(reason(main)).toBe("A week has 168 hours");

    save(main)?.click();
    await settle();
    expect(calls.some((one) => one.method === "setOvertime")).toBe(false);
  });
});

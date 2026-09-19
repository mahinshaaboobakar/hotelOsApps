import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedWeek } from "../roster/recorded";

/**
 * The rota's week arrows and Copy last week — both dead on the owner's 0.3.3.
 *
 * `roster.read · week` takes the week to answer (any day in it); the arrows
 * ask for the one before or after the week on screen. `roster.plan ·
 * copyWeek` takes the Monday to copy from, the Monday to copy into and the
 * department, and fills empty cells only — it confirms first (§9), because it
 * writes across a whole week.
 *
 * Driven through the mounted application, so the week the arrows choose is
 * the week the next read asks for — the state is the application's, and a test
 * of the screen function alone could not see it.
 */

interface Call { capability: string; method: string; params?: unknown }

function host(calls: Call[]): HostApi {
  return {
    identity: {
      id: "workforce", version: "0.1.0", capabilities: ["roster.read", "roster.plan"],
    },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (capability: string, method: string, params?: unknown) => {
      calls.push({ capability, method, params });
      if (method === "week") return Promise.resolve(recordedWeek);
      if (method === "copyWeek") return Promise.resolve({ filled: 12 });
      return Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" }));
    },
    on: () => () => {},
  };
}

async function settle(): Promise<void> {
  for (let turn = 0; turn < 8; turn += 1) {
    await new Promise((resolve) => { setTimeout(resolve, 1); });
  }
}

async function mount(calls: Call[]): Promise<HTMLElement> {
  const root = document.createElement("div");
  activate(host(calls)).mount(root);
  await settle();
  return root;
}

function press(root: HTMLElement, label: string): void {
  const control = Array.from(root.querySelectorAll<HTMLButtonElement>("button"))
    .find((one) => one.getAttribute("aria-label") === label || one.textContent === label);
  if (control === undefined) throw new Error(`no "${label}" control`);
  if (control.disabled) throw new Error(`"${label}" is drawn but disabled`);
  control.click();
}

const weeks = (calls: Call[]): unknown[] =>
  calls.filter((one) => one.method === "week").map((one) => one.params);

describe("the rota's week", () => {
  it("opens the next week and the previous one, asking the read for each", async () => {
    const calls: Call[] = [];
    const root = await mount(calls);

    // However many times mounting draws the rota (it is twice: again when the
    // signed-in person's read lands), none of those reads names a week — the
    // service answers its current one.
    const mounted = weeks(calls).length;
    expect(weeks(calls)).toEqual(Array(mounted).fill(undefined));

    press(root, "Next week");
    await settle();
    press(root, "Previous week");
    await settle();

    // Each arrow asks for the week beside the one on screen. The fixture always
    // answers its own week, so both arrows step from 24 Aug.
    expect(weeks(calls).slice(mounted)).toEqual([{ week: "2026-08-31" }, { week: "2026-08-17" }]);
  });

  it("prints the week on screen, not the current one", async () => {
    // The arrows made this reachable: a person steps to next week and presses
    // Print. The printed page reads the week and the duty register itself, so
    // both have to be asked for the week that was open.
    const calls: Call[] = [];
    const root = await mount(calls);

    press(root, "Next week");
    await settle();
    const before = calls.length;

    press(root, "⎙ Print");
    await settle();

    const printing = calls.slice(before).filter((one) => ["week", "register"].includes(one.method));
    expect(printing.map((one) => [one.method, one.params])).toEqual([
      ["week", { week: "2026-08-31" }],
      ["register", { week: "2026-08-31" }],
    ]);
  });

  it("copies last week into this one after confirming, and names the department", async () => {
    const calls: Call[] = [];
    const root = await mount(calls);

    press(root, "⧉ Copy last week");
    await settle();

    const dialog = root.querySelector(".dlg");
    expect(dialog?.textContent).toContain("Only empty cells are filled");
    expect(calls.some((one) => one.method === "copyWeek")).toBe(false);

    dialog?.querySelector<HTMLButtonElement>(".acts button:last-of-type")?.click();
    await settle();

    expect(calls.filter((one) => one.method === "copyWeek")).toEqual([{
      capability: "roster.plan",
      method: "copyWeek",
      params: { from: "2026-08-17", to: "2026-08-24", department: "FO" },
    }]);
    // Closed and re-read: the grid is what changed.
    expect(root.querySelector(".dlg")).toBeNull();
    expect(weeks(calls).length).toBeGreaterThan(1);
  });
});

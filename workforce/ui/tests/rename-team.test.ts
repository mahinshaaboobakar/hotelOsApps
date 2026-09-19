import type { HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { recordedTeams } from "../roster/teams";
import { detail } from "../screens/teams/detail";
import { renameTeam } from "../screens/teams/rename";

/**
 * Renaming a team — `posting.assign · rename`, which takes the team's id, the
 * version the read sent, and the new name. The owner met Rename dead on 0.3.3.
 */

const open = recordedTeams.detail;
if (open === null) throw new Error("the recorded teams carry an open team");

function host(calls: unknown[]): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["posting.assign"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (capability: string, method: string, params?: unknown) => {
      calls.push({ capability, method, params });
      return Promise.resolve({ id: open!.team.id, version: open!.team.version + 1, name: "x" });
    },
    on: () => () => {},
  };
}

const CONFIRM = ".acts button:last-of-type";
const settle = (): Promise<void> => new Promise((resolve) => { setTimeout(resolve, 1); });

describe("rename a team", () => {
  it("is a live control in the team's pane, opening the rename dialog", () => {
    const opened: string[] = [];
    const pane = detail(open, {
      dialog: null, open: (what) => { opened.push(what); }, close: () => {},
      team: open.team.id, onTeam: () => {},
    }, { timezone: "Asia/Kolkata", locale: "en-GB" });

    const rename = Array.from(pane.querySelectorAll<HTMLButtonElement>("button"))
      .find((one) => one.textContent === "Rename");
    expect(rename?.hasAttribute("disabled")).toBe(false);

    rename?.click();
    expect(opened).toEqual(["rename"]);
  });

  it("sends the id, the version it read, and the new name", async () => {
    const calls: unknown[] = [];
    let done = false;
    const dialog = renameTeam(host(calls), open, () => {}, () => { done = true; });

    const input = dialog.querySelector<HTMLInputElement>("input[name=name]");
    expect(input?.value).toBe(open.team.name);
    // Unchanged is nothing to send.
    expect(dialog.querySelector(CONFIRM)?.hasAttribute("disabled")).toBe(true);

    input!.value = "Tower Block Crew";
    input!.dispatchEvent(new Event("input"));
    dialog.querySelector<HTMLButtonElement>(CONFIRM)?.click();
    await settle();

    expect(calls).toEqual([{
      capability: "posting.assign",
      method: "rename",
      params: { id: open.team.id, version: open.team.version, name: "Tower Block Crew" },
    }]);
    expect(done).toBe(true);
  });
});

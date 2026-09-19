import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { recordedRegister } from "../roster/duty";
import { recordedPeople } from "../roster/people";
import { recordedWeek } from "../roster/recorded";
import { recordedPostingEnding, recordedTeams } from "../roster/teams";
import { assignDuty } from "../screens/duty/dialog";
import { requestForm } from "../screens/leave/form";
import { endPosting } from "../screens/people/end-posting";
import { people } from "../screens/people";
import { picker } from "../screens/rota/picker";
import { newShift } from "../screens/policy/dialog";
import { formTeam } from "../screens/teams/form";
import { addMember } from "../screens/teams/member";
import { standDown } from "../screens/teams/stand-down";

/**
 * A sheet composes, a dialog confirms — §9, and one head, body and foot.
 *
 * The app surface audit (2026-09-19, O1 · O7) found every Workforce overlay
 * assembled by hand as `scrim` + `dlg`: the composing forms drawn as centred
 * boxes, the confirm at 440 rather than 520, one sheet at 390, and no overlay
 * on §9's shared `.dh .db .df`. Each is asserted here by **what the person is
 * doing when they arrive** — §9's own test — and every one on the shared parts.
 */

const host: HostApi = {
  identity: { id: "workforce", version: "0.1.0", capabilities: [] },
  property: { timezone: "Asia/Kolkata", locale: "en-GB" },
  call: () => Promise.reject(new Error("not called")),
  on: () => () => {},
};
const nothing = (): void => {};
const detail = recordedTeams.detail;
if (detail === null) throw new Error("the recorded teams carry an open team");

const COMPOSING: [string, () => HTMLElement][] = [
  ["assign duty", () => assignDuty(host, nothing, recordedRegister.candidates, recordedRegister.days[0], host.property, nothing)],
  ["request leave", () => requestForm(nothing)],
  ["new shift", () => newShift(nothing)],
  ["form a team", () => formTeam(host, recordedTeams.departments, recordedTeams.teams, nothing, nothing)],
  ["add a member", () => addMember(host, recordedTeams.onDate, nothing, detail, nothing)],
];

const CONFIRMING: [string, () => HTMLElement][] = [
  ["end a posting", () => endPosting(host, nothing, recordedPostingEnding, nothing)],
  ["stand a team down", () => standDown(host, nothing, detail, nothing)],
];

function parts(surface: Element, name: string): void {
  for (const part of ["dh", "db", "df"]) {
    expect(surface.querySelector(`:scope > .${part}`), `${name} has no .${part}`).not.toBeNull();
  }
}

describe("overlays", () => {
  for (const [name, open] of COMPOSING) {
    it(`${name} is a sheet — the person is composing`, () => {
      const scrim = open();
      const surface = scrim.firstElementChild;
      expect(surface?.classList.contains("sheet"), `${name} is not a sheet`).toBe(true);
      expect(scrim.classList.contains("mid"), `${name}'s scrim centres it`).toBe(false);
      parts(surface as Element, name);
    });
  }

  for (const [name, open] of CONFIRMING) {
    it(`${name} is a dialog — the person is confirming`, () => {
      const scrim = open();
      const surface = scrim.firstElementChild;
      expect(surface?.classList.contains("dlg"), `${name} is not a dialog`).toBe(true);
      expect(surface?.classList.contains("sheet")).toBe(false);
      expect(scrim.classList.contains("mid"), `${name}'s scrim does not centre it`).toBe(true);
      parts(surface as Element, name);
    });
  }

  // The dialog that opens when the service could not say what ending a posting
  // closes: nothing to compose, a failure to acknowledge — so a dialog.
  it("a posting nobody could read the ending of is a dialog", async () => {
    const failing: HostApi = {
      ...host,
      identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
      call: (_capability: string, method: string) => method === "people"
        ? Promise.resolve(recordedPeople)
        : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" })),
    };
    const main = document.createElement("div");
    await people(failing, main, "any-posting");

    const scrim = main.querySelector(":scope > .scrim");
    const surface = scrim?.firstElementChild;
    expect(surface?.classList.contains("dlg")).toBe(true);
    expect(scrim?.classList.contains("mid")).toBe(true);
    parts(surface as Element, "cannot read");
  });

  // Not a §9 overlay: a popover over the cell a person clicked. It relied on
  // the old `.scrim` centring everything; the sheet's scrim holds the right
  // edge, so it has to ask for the centring one by name or it moves.
  it("the rota's shift picker stays centred", () => {
    const person = recordedWeek.people[0];
    if (person === undefined) throw new Error("the recorded week carries a person");
    const scrim = picker(host, person, 0, recordedWeek, nothing, nothing);
    expect(scrim.classList.contains("mid"), "the picker's scrim does not centre it").toBe(true);
  });
});

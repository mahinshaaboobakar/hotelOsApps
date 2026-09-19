import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { recordedRegister } from "../roster/duty";
import { recordedLeave } from "../roster/leave";
import { recordedFirstRun } from "../roster/people";
import { recordedWeek } from "../roster/recorded";
import { recordedNoTeams, recordedPostingEnding, recordedTeams } from "../roster/teams";
import { assignDuty } from "../screens/duty/dialog";
import { requestForm } from "../screens/leave/form";
import { people } from "../screens/people";
import { endPosting } from "../screens/people/end-posting";
import { newShift } from "../screens/policy/dialog";
import { copyWeek } from "../screens/rota/copy";
import { picker } from "../screens/rota/picker";
import { teams } from "../screens/teams";
import { formTeam } from "../screens/teams/form";
import { addMember } from "../screens/teams/member";
import { renameTeam } from "../screens/teams/rename";
import { standDown } from "../screens/teams/stand-down";
import { developerContent, readableText } from "../../../scripts/developer-content";
import { readable, SURFACES, surfaceHost } from "./surfaces";

/**
 * No developer content reaches a person at the property — owner ruling,
 * 2026-09-19: *a mock carries the screen and notes for the developer, and the
 * second is never built as UI.*
 *
 * Widened from the register-id check (`b001bfac`), which found `WF-Q18` in
 * Reports' copy, rather than written a second time beside it. What it refuses,
 * in what a person reads — text, and the `title`/`aria-label` a person meets on
 * hover or through a screen reader:
 *
 * ```text
 * a document citation   WF-Q18 · ADR 0174 · § · design page
 * a platform system     Master Data · Kernel · OpenFGA · Context Service
 *                       · Integration Hub
 * ```
 *
 * **What it cannot catch, stated rather than implied**: a rationale in plain
 * words ("…is a row that lies") has no pattern. Those were found by reading
 * every rendered sentence (the sweep, 2026-09-19) and are held by that reading,
 * not by this check. A failure state's own reason is the screen's sentence for
 * a person and is not developer content — but see the queued question on the
 * facts block's capability id.
 *
 * Rendered rather than read from source: a comment citing a ruling is a record
 * and is right, and only what reaches the screen is a claim to staff.
 *
 * **The citation shapes are the estate's list**, `scripts/developer-content.ts`
 * (15e2654), which widened this check across the four applications and asks
 * each to import it rather than keep a private copy. Its reader walks text
 * nodes one by one and reads `placeholder` too; this one joined `textContent`,
 * which welds neighbouring elements and can cost a pattern its word boundary.
 *
 * **What stays here is what that list does not carry**: the platform systems
 * by name, and "design page". Kept local rather than added to the shared file,
 * which the other three applications' guards also read — widening it is theirs
 * to agree to, and a name there that one of them renders on purpose would turn
 * a neighbour red from this commit.
 */
const SYSTEMS: readonly (readonly [string, RegExp])[] = [
  ["a design page", /\bdesign page\b/gi],
  ["a platform system", /\b(?:Master Data|Kernel|OpenFGA|Context Service|Integration Hub)\b/g],
];

function found(root: HTMLElement): string[] {
  const text = readableText(readable(root));
  return [
    ...developerContent(text),
    ...SYSTEMS.flatMap(([what, pattern]) =>
      [...text.matchAll(pattern)].map((match) => `${what}: ${match[0]}`)),
  ];
}

const host = surfaceHost("en-GB");
const nothing = (): void => {};
const open = recordedTeams.detail;
if (open === null) throw new Error("the recorded teams carry an open team");

function answering(method: string, value: unknown): HostApi {
  return {
    ...host,
    call: (_capability: string, asked: string) => asked === method
      ? Promise.resolve(value)
      : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" })),
  };
}

/** Dialogs and states the surface list does not reach. */
const MORE: readonly (readonly [string, () => Promise<HTMLElement>])[] = [
  ["dialog: assign duty", async () => assignDuty(
    host, nothing, recordedRegister.candidates, recordedRegister.days[0], host.property, nothing)],
  ["dialog: request leave", async () => requestForm(host, recordedLeave.balances, nothing, nothing)],
  ["dialog: new shift", async () => newShift(host, nothing, nothing)],
  ["dialog: form team", async () => formTeam(
    host, recordedTeams.departments, recordedTeams.teams, nothing, nothing)],
  ["dialog: add member", async () => addMember(host, recordedTeams.onDate, nothing, open, nothing)],
  ["dialog: end posting", async () => endPosting(host, nothing, recordedPostingEnding, nothing)],
  ["dialog: stand down", async () => standDown(host, nothing, open, nothing)],
  ["dialog: rename team", async () => renameTeam(host, open, nothing, nothing)],
  ["dialog: copy week", async () => copyWeek(host, recordedWeek, nothing, nothing)],
  ["dialog: shift picker", async () => picker(
    host, recordedWeek.people[0]!, 0, recordedWeek, nothing, nothing)],
  ["people, first run", async () => {
    const main = document.createElement("div");
    await people(answering("people", recordedFirstRun), main);
    return main;
  }],
  ["teams, none formed", async () => {
    const main = document.createElement("div");
    await teams(answering("teams", recordedNoTeams), main);
    return main;
  }],
];

describe("developer content", () => {
  for (const [name, draw] of SURFACES) {
    it(`${name} shows none to staff`, async () => {
      const main = document.createElement("div");
      await draw(host, main);

      expect(main.querySelector(".fail"), `${name} could not read its fixture`).toBeNull();
      expect(found(main)).toEqual([]);
    });
  }

  for (const [name, draw] of MORE) {
    it(`${name} shows none to staff`, async () => {
      expect(found(await draw())).toEqual([]);
    });
  }

  // Positive control: every clean result above is only a result if this reader
  // finds each shape when it IS there — split across elements, in an attribute,
  // and in a placeholder, the three places a joined `textContent` went blind.
  it("finds each shape when it is there", () => {
    const planted = document.createElement("div");
    planted.innerHTML =
      '<span>See</span><b>WF-Q18</b>'
      + '<button title="Owned by Master Data">x</button>'
      + '<input placeholder="roster_plan">'
      + "<p>per ADR 0174 and the design page</p>";

    expect(found(planted).map((one) => one.split(":")[0])).toEqual(expect.arrayContaining([
      "a decision-register id", "an ADR", "a code identifier",
      "a design page", "a platform system",
    ]));
  });
});

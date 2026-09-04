import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedNoTeams, recordedPostingEnding, recordedTeams } from "../roster/teams";
import { recordedPeople } from "../roster/people";

/**
 * Teams, and the consequence panel that ends a posting.
 *
 * These assert **rules and structure**, never layout — what a suite cannot see
 * is what the capture pass is for. Two of them exist because the rule they
 * check is one a screen could plausibly get wrong in a way that reads fine:
 * the refusal that must stay visible, and the panel that must state a
 * consequence before the button rather than after it.
 */

/**
 * A host answering the named methods and refusing the rest.
 *
 * **Refusing, not substituting.** An unknown method that returned some other
 * screen's fixture is how the rota came to be handed a list of teams: each
 * screen falls back to its own recording when the seam says `unavailable`,
 * which is what a property without a Workforce client actually sees.
 */
function host(answers: Record<string, unknown>): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: null },
    call: (capability: string, method: string) => {
      const answer = answers[method];

      return answer === undefined
        ? Promise.reject(new HostCallError(
          { kind: "unavailable", message: `no answer for ${capability}/${method}` }))
        : Promise.resolve(answer);
    },
    on: () => () => {},
  };
}

async function mount(api: HostApi): Promise<HTMLElement> {
  const root = document.createElement("div");
  activate(api).mount(root);
  await settle();
  return root;
}

/**
 * Let the screen finish drawing.
 *
 * A dialog is loaded with a dynamic `import()` — the module splits them so a
 * screen nobody opens costs nothing — so one turn of the loop is not enough:
 * the module resolves, then the screen appends.
 *
 * **Eight turns rather than three**, because three passed alone and failed once
 * in a full run: the import is a real module fetch and the whole suite is
 * slower than one file. A wait tuned to the fastest case is a flake with a
 * schedule.
 */
async function settle(): Promise<void> {
  for (let turn = 0; turn < 8; turn += 1) {
    await new Promise((resolve) => { setTimeout(resolve, 1); });
  }
}

/** Click the first element matching `selector` whose text contains `text`. */
async function click(root: HTMLElement, selector: string, text: string): Promise<void> {
  const node = Array.from(root.querySelectorAll<HTMLElement>(selector))
    .find((one) => one.textContent?.includes(text) === true);

  expect(node, `nothing matching ${selector} reads "${text}"`).toBeDefined();
  node?.click();
  await settle();
}

async function open(root: HTMLElement, screen: string): Promise<void> {
  await click(root, ".ri", screen);
}

describe("Teams", () => {
  it("is a destination the rail reaches", async () => {
    const root = await mount(host({ teams: recordedTeams }));

    const labels = Array.from(root.querySelectorAll(".ri")).map((one) => one.textContent);
    expect(labels.some((label) => label?.includes("Teams") === true)).toBe(true);
  });

  it("lists every team with its department and its state", async () => {
    const root = await mount(host({ teams: recordedTeams }));
    await open(root, "Teams");

    const rows = Array.from(root.querySelectorAll(".tgrid")).slice(1);
    expect(rows).toHaveLength(recordedTeams.teams.length);

    // The stood-down one is listed, not hidden: "not offered" and "gone" are
    // different facts and a list that shows only the first cannot say so.
    const down = rows.find((row) => row.textContent?.includes("Pool Bar") === true);
    expect(down?.className).toContain("down");
    expect(down?.textContent).toContain("Stood down");
  });

  it("names the department it belongs to rather than a zone", async () => {
    const root = await mount(host({ teams: recordedTeams }));
    await open(root, "Teams");

    // A team is people and a zone is a place — `WF-Q7` keeps the zone on the
    // posting, so a team row that carried one would be the confusion the
    // ruling settled, drawn.
    expect(root.textContent).not.toMatch(/\bZone\b/);
  });

  it("draws the first run when the property has formed none", async () => {
    const root = await mount(host({ teams: recordedNoTeams }));
    await open(root, "Teams");

    expect(root.querySelector(".tvoid")).not.toBeNull();
    expect(root.querySelectorAll(".tgrid")).toHaveLength(0);

    // The honest second line. A property that never forms a team loses
    // nothing, and an empty state that implied otherwise would be selling.
    expect(root.textContent).toContain("Nothing is waiting on this");
  });

  it("opens the team whose roll the answer carries, and only that one", async () => {
    const root = await mount(host({ teams: recordedTeams }));
    await open(root, "Teams");

    // Frame 1 first: a list, with no pane beside it. The fixture used to carry
    // an open team, so this state could not be produced at all.
    expect(root.querySelector(".tsplit")).toBeNull();

    const openable = Array.from(root.querySelectorAll("button.tgrid"));
    expect(openable).toHaveLength(1);
    expect(openable[0]?.textContent).toContain("Morning Crew");

    (openable[0] as HTMLElement).click();
    await settle();

    expect(root.querySelector(".tsplit")).not.toBeNull();
    expect(root.querySelector(".tdetail")?.textContent).toContain("Morning Crew");
  });

  it("shows the candidate it refuses, with the reason", async () => {
    const root = await mount(host({ teams: recordedTeams }));
    await open(root, "Teams");
    await click(root, "button.tgrid", "Morning Crew");
    await click(root, ".btn", "Add a member");

    const refused = Array.from(root.querySelectorAll<HTMLElement>(".tmem.no"));
    expect(refused).toHaveLength(1);
    expect(refused[0]?.textContent).toContain("Joseph Kurian");
    expect(refused[0]?.textContent).toContain("Not posted here");

    // Filtering him out would leave a supervisor looking for somebody who is
    // simply absent, which teaches nothing and reads as a broken picker.
    expect(refused[0]?.getAttribute("aria-disabled")).toBe("true");
  });

  it("offers the toggle as a switch a keyboard can reach", async () => {
    const root = await mount(host({ teams: recordedTeams }));
    await open(root, "Teams");
    await click(root, "button.tgrid", "Morning Crew");
    await click(root, ".btn", "Stand down");

    const toggle = root.querySelector<HTMLElement>(".tsw");
    expect(toggle?.tagName).toBe("BUTTON");
    expect(toggle?.getAttribute("role")).toBe("switch");

    // On by default: standing a seasonal crew down is not disbanding it.
    expect(toggle?.getAttribute("aria-checked")).toBe("true");

    toggle?.click();
    expect(toggle?.getAttribute("aria-checked")).toBe("false");
  });
});

describe("ending a posting", () => {
  it("states what else it closes before the button that does it", async () => {
    const root = await mount(host({ people: recordedPeople, teams: recordedTeams }));
    await open(root, "People");
    await click(root, ".row", recordedPostingEnding.who);

    const panel = root.querySelector<HTMLElement>(".conseq");
    expect(panel, "the consequence panel").not.toBeNull();

    for (const also of recordedPostingEnding.alsoEnds) {
      expect(panel?.textContent).toContain(also.team);
    }

    // **The order is the whole point.** A toast afterwards reports what a
    // person can no longer choose about; the panel has to precede the control
    // that commits it, and "precede" is a fact about the DOM rather than a
    // matter of taste.
    const dialog = panel?.closest(".dlg");
    const confirm = Array.from(
      dialog?.querySelectorAll<HTMLElement>(".btn.danger") ?? [])[0];

    expect(confirm, "the button the panel precedes").toBeDefined();
    expect(
      (panel?.compareDocumentPosition(confirm as Node) ?? 0)
      & Node.DOCUMENT_POSITION_FOLLOWING,
    ).toBeTruthy();
  });

  it("says nothing when the posting holds nothing open", async () => {
    const root = await mount(host({ people: recordedPeople, teams: recordedTeams }));
    await open(root, "People");

    const other = recordedPeople.postings
      .find((one) => one.who !== recordedPostingEnding.who);
    expect(other).toBeDefined();

    await click(root, ".row", other!.who);

    // Absent, not empty. A panel headed "This also ends" over nothing is a
    // warning about a consequence that does not exist.
    expect(root.querySelector(".dlg")).not.toBeNull();
    expect(root.querySelector(".conseq")).toBeNull();
  });
});

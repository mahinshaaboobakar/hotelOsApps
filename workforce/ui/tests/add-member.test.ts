import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedTeams } from "../roster/teams";

/**
 * Adding a member — the second dialog, and the one that shows the pattern holds.
 *
 * Form a team had two typed fields; this has a **choice from a list**, a date it
 * does not own, and a team it inherits from the pane behind it. If the shape
 * only worked for typed fields it would have failed here rather than at dialog
 * seven, which is why this one came second.
 *
 * What it shares with the first, and what makes it a pattern rather than a
 * coincidence: nothing is chosen when it opens, the confirm is drawn `off` with
 * the reason it is waiting on, the write goes through the one seam, and a
 * refusal keeps the overlay open carrying the service's own sentence.
 */

interface Sent {
  capability: string;
  method: string;
  params: unknown;
}

function host(sent: Sent[], refuse?: HostCallError): HostApi {
  return {
    identity: {
      id: "workforce",
      version: "0.1.0",
      capabilities: ["roster.read", "posting.assign"],
    },
    property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    call: (capability: string, method: string, params?: unknown) => {
      if (method === "teams") return Promise.resolve(recordedTeams);
      if (capability === "roster.read") {
        return Promise.reject(new HostCallError({
          kind: "unavailable", message: "not this test",
        }));
      }

      sent.push({ capability, method, params });
      if (refuse !== undefined) return Promise.reject(refuse);
      return Promise.resolve({ id: "m-new", version: 1, since: "2026-09-04" });
    },
    on: () => () => {},
  };
}

async function settle(): Promise<void> {
  for (let turn = 0; turn < 10; turn += 1) {
    await new Promise((resolve) => { setTimeout(resolve, 1); });
  }
}

function find<T extends HTMLElement>(root: HTMLElement, selector: string, text: string): T {
  const hit = Array.from(root.querySelectorAll<T>(selector))
    .find((node) => node.textContent?.includes(text) === true);
  if (hit === undefined) throw new Error(`no ${selector} reading ${text}`);
  return hit;
}

/** Open the team the pane holds, then the dialog over it. */
async function dialog(sent: Sent[], refuse?: HostCallError): Promise<HTMLElement> {
  const root = document.createElement("div");
  activate(host(sent, refuse)).mount(root);
  await settle();

  find(root, ".head .tab", "People").click();
  await settle();
  find(root, ".tabs .tab", "Teams").click();
  await settle();
  find(root, "button.tgrid", "Morning Crew").click();
  await settle();
  find(root, "button.btn", "Add a member").click();
  await settle();

  return root;
}

describe("adding a member", () => {
  it("chooses nobody when it opens", async () => {
    const root = await dialog([]);

    // The first eligible candidate used to arrive selected, which supplies an
    // answer on the person's behalf — and the one it supplies is whoever the
    // service happened to list first.
    expect(root.querySelectorAll(".tcand.on")).toHaveLength(0);
    expect(root.querySelector(".acts .note")?.textContent).toBe("Choose somebody");

    const confirm = find<HTMLButtonElement>(root, "button.btn", "Add to team");
    expect(confirm.classList.contains("off")).toBe(true);
    expect(confirm.disabled).toBe(true);
  });

  it("cannot choose the person the service refuses", async () => {
    const root = await dialog([]);
    const refused = root.querySelector<HTMLButtonElement>(".tcand.no")!;

    // A real disabled button: a click does nothing and focus does not land on
    // it, where `aria-disabled` on a div announces unavailability and still
    // takes both.
    expect(refused.disabled).toBe(true);
    refused.click();
    await settle();

    expect(root.querySelectorAll(".tcand.on")).toHaveLength(0);
    expect(root.querySelector(".acts .note")?.textContent).toBe("Choose somebody");
  });

  it("marks the one chosen, and only that one", async () => {
    const root = await dialog([]);
    const eligible = Array.from(root.querySelectorAll<HTMLButtonElement>(".tcand:not(.no)"));
    expect(eligible.length).toBeGreaterThan(1);

    eligible[0]!.click();
    await settle();
    eligible[1]!.click();
    await settle();

    // One membership is being added, so one row carries the mark.
    const marked = Array.from(root.querySelectorAll(".tcand.on"));
    expect(marked).toHaveLength(1);
    expect(marked[0]).toBe(eligible[1]);
    expect(root.querySelector(".acts .note")?.textContent).toBe("");
  });

  it("sends the team, the person and the board's own day", async () => {
    const sent: Sent[] = [];
    const root = await dialog(sent);

    root.querySelector<HTMLButtonElement>(".tcand:not(.no)")!.click();
    await settle();
    find<HTMLButtonElement>(root, "button.btn", "Add to team").click();
    await settle();

    expect(sent).toHaveLength(1);
    expect(sent[0]!.capability).toBe("posting.assign");
    expect(sent[0]!.method).toBe("addMember");

    const params = sent[0]!.params as { teamId: string; staffId: string; on: string };
    expect(params.teamId).toBe("t-mc");
    expect(params.staffId).not.toBe("");

    // **The ISO day the read answered**, not a date parsed back out of the
    // rendering beside it. `Thu 4 Sep` has no year, and a screen that
    // reconstructs one is a screen that invents it.
    expect(params.on).toBe("2026-09-04");
  });

  it("shows the day in the property's own form, from that same value", async () => {
    const root = await dialog([]);

    // The field used to read `Thu 4 Sep 2026` as a literal — the frame's date,
    // on every property, forever. It is now the board's ISO day through the
    // SDK, so the field and the payload cannot disagree.
    const shown = root.querySelector(".fld .inp")?.textContent ?? "";
    expect(shown).not.toBe("");
    expect(shown).toContain("2026");
    expect(shown).toContain("Sep");
  });

  it("keeps the dialog open on a refusal, carrying the reason", async () => {
    const sent: Sent[] = [];
    const root = await dialog(sent, new HostCallError({
      kind: "rejected",
      message: "Anjali Menon already belongs to Morning Crew on that day.",
    }));

    root.querySelector<HTMLButtonElement>(".tcand:not(.no)")!.click();
    await settle();
    find<HTMLButtonElement>(root, "button.btn", "Add to team").click();
    await settle();

    expect(root.querySelector(".scrim")).not.toBeNull();
    expect(root.textContent)
      .toContain("Anjali Menon already belongs to Morning Crew on that day.");
    expect(find<HTMLButtonElement>(root, "button.btn", "Add to team").disabled).toBe(false);
  });

  it("closes when the member is added", async () => {
    const sent: Sent[] = [];
    const root = await dialog(sent);

    root.querySelector<HTMLButtonElement>(".tcand:not(.no)")!.click();
    await settle();
    find<HTMLButtonElement>(root, "button.btn", "Add to team").click();
    await settle();

    expect(root.querySelector(".scrim")).toBeNull();
  });
});

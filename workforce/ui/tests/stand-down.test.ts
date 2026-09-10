import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedTeams } from "../roster/teams";

/**
 * Standing a team down — dialog three, and the one that tests the prediction.
 *
 * Dialogs one and two each carried the same three faults: an answer supplied on
 * the person's behalf, a control wearing `aria-disabled` where a real disabled
 * one belonged, and a literal where a value should ride the wire. This dialog
 * was the check on whether that pattern predicts.
 *
 * **Two of the three are not here, and saying so is the point.** The toggle's
 * default is reasoned, documented, and the same default the service applies
 * when the field is absent — a preselected safe side of a binary is not the
 * same fault as a picker that answers for you. And nothing here is disabled at
 * all. The third arrived in a different form: not a literal, but an **absent**
 * value — the read carried no `version`, so the write could not be made
 * whatever the button did.
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
      return Promise.resolve({ id: "t-mc", version: 2, active: false });
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
  find(root, "button.btn", "Stand down").click();
  await settle();

  return root;
}

describe("standing a team down", () => {
  it("offers a filled destructive confirm, live from the start", async () => {
    const root = await dialog([]);
    const confirm = find<HTMLButtonElement>(root, ".acts button.btn", "Stand down");

    // §2: the confirm step of a destructive flow is FILLED. An outline here
    // would make the most consequential control the quietest on the screen.
    expect(confirm.classList.contains("danger")).toBe(true);
    expect(confirm.classList.contains("confirm")).toBe(true);
    expect(confirm.classList.contains("pri")).toBe(false);

    // Nothing is outstanding — the team is the pane's and the toggle has a
    // position — so this one is live rather than `off` with a reason. A dialog
    // whose confirm is never `off` is not one missing the rule.
    expect(confirm.disabled).toBe(false);
    expect(confirm.classList.contains("off")).toBe(false);
  });

  it("keeps the members unless somebody says otherwise", async () => {
    const sent: Sent[] = [];
    const root = await dialog(sent);

    const toggle = root.querySelector<HTMLButtonElement>("button.tsw")!;
    expect(toggle.classList.contains("on")).toBe(true);
    expect(toggle.getAttribute("aria-checked")).toBe("true");

    find<HTMLButtonElement>(root, ".acts button.btn", "Stand down").click();
    await settle();

    // The default is the SERVICE's default, so screen and service agree about
    // what happens when nobody touches anything.
    expect(sent[0]!.params).toEqual({
      id: "t-mc", version: 1, active: false, keepMembers: true,
    });
  });

  it("carries the toggle's position into the write", async () => {
    const sent: Sent[] = [];
    const root = await dialog(sent);

    root.querySelector<HTMLButtonElement>("button.tsw")!.click();
    await settle();

    const toggle = root.querySelector<HTMLButtonElement>("button.tsw")!;
    expect(toggle.classList.contains("on")).toBe(false);
    expect(toggle.getAttribute("aria-checked")).toBe("false");

    find<HTMLButtonElement>(root, ".acts button.btn", "Stand down").click();
    await settle();

    expect((sent[0]!.params as { keepMembers: boolean }).keepMembers).toBe(false);
  });

  it("quotes the version the read gave it", async () => {
    const sent: Sent[] = [];
    const root = await dialog(sent);

    find<HTMLButtonElement>(root, ".acts button.btn", "Stand down").click();
    await settle();

    // **The write could not be made at all before this.** `Standing` requires
    // `ExpectedVersion` and the teams read carried no version — so the dialog
    // had no way to say which row it was looking at, and a listener on the
    // confirm would have been refused every time.
    expect((sent[0]!.params as { version: number }).version).toBe(1);
  });

  it("keeps the dialog open on a refusal, carrying the reason", async () => {
    const sent: Sent[] = [];
    const root = await dialog(sent, new HostCallError({
      kind: "rejected",
      message: "Morning Crew was changed by somebody else. Read it again.",
    }));

    find<HTMLButtonElement>(root, ".acts button.btn", "Stand down").click();
    await settle();

    // Closing on a failed stand-down would leave a supervisor believing a crew
    // had been taken off the board.
    expect(root.querySelector(".scrim")).not.toBeNull();
    expect(root.textContent)
      .toContain("Morning Crew was changed by somebody else. Read it again.");
    expect(find<HTMLButtonElement>(root, ".acts button.btn", "Stand down").disabled)
      .toBe(false);
  });

  it("closes when the team is stood down", async () => {
    const sent: Sent[] = [];
    const root = await dialog(sent);

    find<HTMLButtonElement>(root, ".acts button.btn", "Stand down").click();
    await settle();

    expect(root.querySelector(".scrim")).toBeNull();
  });
});

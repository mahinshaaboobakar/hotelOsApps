import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedTeams } from "../roster/teams";

/**
 * Forming a team — the module's first write that reaches the platform.
 *
 * Until this landed the bundle read and did not write: the seam's `write` had
 * no caller anywhere, three of the four capability constants were imported by
 * nothing, and this sheet's confirm had no click listener at all. Both of its
 * fields were `<div>`s carrying the drawing's own words — `Housekeeping` and
 * `Morning Crew` — so a confirm wired to them would have posted the drawing to
 * the property.
 *
 * These drive the screen rather than the sheet, because the thing that was
 * missing was the **wiring**: the host, the departments and the property's own
 * teams all have to reach the sheet for any of it to be true, and a test that
 * called `formTeam` directly would supply all three itself and prove none of
 * them.
 */

/** What the host was asked to do, in order. */
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
      // **Answer one read and refuse the rest.** The module mounts on Rota and
      // five widgets read on the way past, so a double that answers every
      // method with the write's payload hands a rota a team id — five
      // unhandled rejections beside a green run. Refusing is also what the
      // platform does, so every other screen draws its failure state.
      if (method === "teams") return Promise.resolve(recordedTeams);
      if (capability === "roster.read") {
        return Promise.reject(new HostCallError({
          kind: "unavailable", message: "not this test",
        }));
      }

      sent.push({ capability, method, params });
      if (refuse !== undefined) return Promise.reject(refuse);
      return Promise.resolve({ id: "t-new", version: 1 });
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

/** Mount, reach Teams, and open the sheet the way a person does. */
async function sheet(sent: Sent[], refuse?: HostCallError): Promise<HTMLElement> {
  const root = document.createElement("div");
  activate(host(sent, refuse)).mount(root);
  await settle();

  find(root, ".head .tab", "People").click();
  await settle();
  find(root, ".tabs .tab", "Teams").click();
  await settle();
  find(root, "button.btn", "Form a team").click();
  await settle();

  return root;
}

describe("forming a team", () => {
  it("offers the property's departments, and chooses none of them", async () => {
    const root = await sheet([]);
    const picker = root.querySelector<HTMLSelectElement>("select.inp");

    // Every department the property has, not the ones that already have a
    // team: the first team in a department is the one this screen exists for.
    expect(picker).not.toBeNull();

    // **In the reader's order, not the read's.** The service answers by code —
    // ENG, FO, HK, KIT — because nothing sets a culture in that process and a
    // culture-sensitive sort there would order a hotel's departments by
    // whichever account the service runs under. The picker re-orders by name
    // with the property's own locale, which is why Maintenance moves from
    // first to last.
    expect(Array.from(picker!.options).map((one) => one.textContent))
      .toEqual(["Choose a department", "Front Office", "Housekeeping",
                "Kitchen", "Maintenance"]);

    // Unchosen, and drawn as unchosen. A picker arriving on "Front Office"
    // has supplied an answer on the person's behalf.
    expect(picker!.value).toBe("");
    expect(picker!.classList.contains("ph")).toBe(true);
  });

  it("keeps the confirm off, with the reason, until there is something to send",
    async () => {
      const root = await sheet([]);
      const confirm = find<HTMLButtonElement>(root, "button.btn", "Form team");

      // §2: a primary action with nothing to send is drawn `off`, with the
      // reason beside it — never live and refusing.
      expect(confirm.classList.contains("off")).toBe(true);
      expect(confirm.hasAttribute("disabled")).toBe(true);
      expect(root.querySelector(".acts .note")?.textContent)
        .toBe("Choose a department");

      const picker = root.querySelector<HTMLSelectElement>("select.inp")!;
      picker.value = "HK";
      picker.dispatchEvent(new Event("change"));
      await settle();

      // One field answered is still nothing to send, and the reason moves on
      // rather than going blank.
      expect(confirm.hasAttribute("disabled")).toBe(true);
      expect(root.querySelector(".acts .note")?.textContent).toBe("Name the team");

      const input = root.querySelector<HTMLInputElement>("input.inp")!;
      input.value = "Late Turn";
      input.dispatchEvent(new Event("input"));
      await settle();

      expect(confirm.classList.contains("off")).toBe(false);
      expect(confirm.hasAttribute("disabled")).toBe(false);
      expect(root.querySelector(".acts .note")?.textContent).toBe("");
    });

  it("sends the code and the name, under posting.assign", async () => {
    const sent: Sent[] = [];
    const root = await sheet(sent);

    const picker = root.querySelector<HTMLSelectElement>("select.inp")!;
    picker.value = "HK";
    picker.dispatchEvent(new Event("change"));

    const input = root.querySelector<HTMLInputElement>("input.inp")!;
    input.value = "  Late Turn  ";
    input.dispatchEvent(new Event("input"));
    await settle();

    find<HTMLButtonElement>(root, "button.btn", "Form team").click();
    await settle();

    // The CODE, not the name a person read — ADR 0119's canon form is what a
    // write carries. And the name trimmed, because a team called "Late Turn "
    // is a team nobody can search for.
    expect(sent).toEqual([{
      capability: "posting.assign",
      method: "form",
      params: { department: "HK", name: "Late Turn" },
    }]);
  });

  it("keeps the sheet open on a refusal, carrying the service's own sentence",
    async () => {
      const sent: Sent[] = [];
      const root = await sheet(sent, new HostCallError({
        kind: "rejected",
        message: "Housekeeping already has a team called Late Turn.",
      }));

      const picker = root.querySelector<HTMLSelectElement>("select.inp")!;
      picker.value = "HK";
      picker.dispatchEvent(new Event("change"));
      const input = root.querySelector<HTMLInputElement>("input.inp")!;
      input.value = "Late Turn";
      input.dispatchEvent(new Event("input"));
      await settle();

      find<HTMLButtonElement>(root, "button.btn", "Form team").click();
      await settle();

      // §9: a refusal keeps the overlay open, carrying the reason. Closing on
      // failure leaves a person believing the team was formed.
      expect(root.querySelector(".scrim")).not.toBeNull();
      expect(root.textContent)
        .toContain("Housekeeping already has a team called Late Turn.");

      // And the confirm is live again — a person may correct the name and try
      // once more without reopening the sheet.
      expect(find<HTMLButtonElement>(root, "button.btn", "Form team")
        .hasAttribute("disabled")).toBe(false);
    });

  it("closes when the team is formed", async () => {
    const sent: Sent[] = [];
    const root = await sheet(sent);

    const picker = root.querySelector<HTMLSelectElement>("select.inp")!;
    picker.value = "FO";
    picker.dispatchEvent(new Event("change"));
    const input = root.querySelector<HTMLInputElement>("input.inp")!;
    input.value = "Late Turn";
    input.dispatchEvent(new Event("input"));
    await settle();

    find<HTMLButtonElement>(root, "button.btn", "Form team").click();
    await settle();

    // Closing redraws the screen, and the redraw re-reads — so the new team
    // arrives through the same read every other row came from.
    expect(root.querySelector(".scrim")).toBeNull();
  });

  it("warns about a name already taken, and only in the chosen department",
    async () => {
      const root = await sheet([]);
      const picker = root.querySelector<HTMLSelectElement>("select.inp")!;
      const input = root.querySelector<HTMLInputElement>("input.inp")!;

      // `Morning Crew` exists in Housekeeping in this property's own teams.
      picker.value = "FO";
      picker.dispatchEvent(new Event("change"));
      input.value = "Morning Crew";
      input.dispatchEvent(new Event("input"));
      await settle();

      // Front Office may have its own Morning Crew. Warning here would be a
      // rule nobody made — and the sheet used to state it unconditionally.
      expect(root.querySelector(".twarn")?.textContent ?? "").toBe("");

      picker.value = "HK";
      picker.dispatchEvent(new Event("change"));
      await settle();

      expect(root.textContent).toContain("Housekeeping already has a team called");
    });
});

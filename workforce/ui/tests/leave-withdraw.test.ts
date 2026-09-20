import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedLeave } from "../roster/leave";
import { withdrawable } from "../screens/leave/withdraw";

/**
 * Withdrawing your own leave request — `64g` §4 B, the other half of the panel.
 *
 * The approver's side decides somebody else's request. This side is the
 * person's own row, and it had no control at all: `leave.request · withdraw`
 * has taken an id and a version since the read carried them, and nothing on
 * this screen could reach it.
 *
 * **The control is offered only where the service would accept it.**
 * `LeaveService.CancelAsync` refuses a request that is already `Declined` or
 * `Cancelled`, so those rows carry no button — a control that exists to be
 * refused is one that looks live and does nothing.
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
      capabilities: ["roster.read", "leave.request"],
    },
    property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    call: (capability: string, method: string, params?: unknown) => {
      if (method === "leave") return Promise.resolve(recordedLeave);
      if (capability === "roster.read") {
        return Promise.reject(new HostCallError({
          kind: "unavailable", message: "not this test",
        }));
      }

      sent.push({ capability, method, params });
      if (refuse !== undefined) return Promise.reject(refuse);
      return Promise.resolve({ id: "r", version: 4, state: "Cancelled" });
    },
    on: () => () => {},
  };
}

async function settle(): Promise<void> {
  for (let turn = 0; turn < 10; turn += 1) {
    await new Promise((resolve) => { setTimeout(resolve, 1); });
  }
}

/** The Leave screen, on the Requests tab. */
async function screen(sent: Sent[], refuse?: HostCallError): Promise<HTMLElement> {
  const root = document.createElement("div");
  activate(host(sent, refuse)).mount(root);
  await settle();

  const section = Array.from(root.querySelectorAll<HTMLButtonElement>(".head > button.tab"))
    .find((one) => one.textContent?.includes("Leave") === true);
  if (section === undefined) throw new Error("no Leave section in the bar");

  section.click();
  await settle();

  return root;
}

/** Every withdraw control on the list, in row order. */
function controls(root: HTMLElement): HTMLButtonElement[] {
  return Array.from(root.querySelectorAll<HTMLButtonElement>(".rows .btn.sm"))
    .filter((one) => one.textContent === "Withdraw");
}

describe("withdrawable", () => {
  it("is what the service accepts, not what reads well", () => {
    // Read from `CancelAsync`'s own guard. `Declined` is in the fixture nowhere,
    // so the predicate is checked here rather than only through a rendering —
    // a state the fixture lacks is a state the screen test cannot reach.
    const row = recordedLeave.requests[0]!;

    expect(withdrawable({ ...row, state: "Requested" })).toBe(true);
    expect(withdrawable({ ...row, state: "Approved" })).toBe(true);
    expect(withdrawable({ ...row, state: "Declined" })).toBe(false);
    expect(withdrawable({ ...row, state: "Cancelled" })).toBe(false);
  });
});

describe("withdrawing your own request", () => {
  it("offers the control on the rows the service would accept", async () => {
    const root = await screen([]);

    // The fixture is three withdrawable rows and one already cancelled, so a
    // control on every row and a control on none both fail here.
    const open = recordedLeave.requests.filter(withdrawable).length;

    expect(open).toBeGreaterThan(0);
    expect(open).toBeLessThan(recordedLeave.requests.length);
    expect(controls(root)).toHaveLength(open);
  });

  it("sends the id and the version that row carried", async () => {
    const sent: Sent[] = [];
    const root = await screen(sent);

    controls(root)[0]!.click();
    await settle();

    const confirm = Array.from(root.querySelectorAll<HTMLButtonElement>(".acts button.btn"))
      .find((one) => one.textContent?.includes("Withdraw") === true);
    confirm!.click();
    await settle();

    expect(sent).toHaveLength(1);
    expect(sent[0]!.capability).toBe("leave.request");
    expect(sent[0]!.method).toBe("withdraw");

    // The row's own version, not a literal and not a fresh read: a withdraw
    // with no version would overwrite a decision made while the dialog was open.
    const first = recordedLeave.requests.find(withdrawable)!;
    expect(sent[0]!.params)
      .toEqual({ id: first.id, version: first.version });
  });

  it("says what happens to the balance, differently for the two states", async () => {
    const root = await screen([]);
    const waiting = recordedLeave.requests.findIndex((row) => row.state === "Requested");
    const approved = recordedLeave.requests.findIndex((row) => row.state === "Approved");

    // Both exist in the fixture, and they are different rows — otherwise this
    // would be one sentence checked twice.
    expect(waiting).toBeGreaterThanOrEqual(0);
    expect(approved).toBeGreaterThanOrEqual(0);

    controls(root)[waiting]!.click();
    await settle();
    // Nothing is debited until approval — `ApproveAsync` says so in as many
    // words — so a note about days being held would describe a mechanism this
    // service deliberately does not have.
    expect(root.querySelector(".scrim")?.textContent)
      .toContain("nothing has been taken from your");

    root.querySelector<HTMLElement>(".scrim")!
      .querySelectorAll<HTMLButtonElement>("button")[0]!.click();
    await settle();

    controls(root)[approved]!.click();
    await settle();
    expect(root.querySelector(".scrim")?.textContent)
      .toContain("go back to your balance");
  });

  it("keeps the dialog open on a refusal, carrying the reason", async () => {
    const sent: Sent[] = [];
    const root = await screen(sent, new HostCallError({
      kind: "rejected",
      message: "this request is already Declined",
    }));

    controls(root)[0]!.click();
    await settle();

    const confirm = Array.from(root.querySelectorAll<HTMLButtonElement>(".acts button.btn"))
      .find((one) => one.textContent?.includes("Withdraw") === true);
    confirm!.click();
    await settle();

    expect(root.querySelector(".scrim")).not.toBeNull();
    expect(root.textContent).toContain("this request is already Declined");
  });
});

import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedRegister } from "../roster/duty";

/**
 * Assigning a Manager on Duty — and the dialog I reported as blocked and wasn't.
 *
 * The span drew `Fri 28 · 20:00` and `Sat 29 · 08:00` as though somebody had
 * chosen them. I replaced those with placeholders and left the confirm
 * permanently `off`, reporting the span as a frame the owner owed — on the
 * reasoning that no default could honestly be invented, since `20:00 → 08:00`
 * appears in this codebase only inside comments.
 *
 * **That was a step too early.** Page 64 §10 says a field is a `<div>` *until
 * there is a write path behind it that accepts what is typed*, and
 * `AssignDutyCommand` takes two datetimes. The question was never what the span
 * defaults to; it was that nothing here could be typed into. A control removes
 * the question rather than answering it, and no default is invented because the
 * person supplies the value.
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
      capabilities: ["roster.read", "duty.assign"],
    },
    property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    call: (capability: string, method: string, params?: unknown) => {
      if (method === "register") return Promise.resolve(recordedRegister);
      if (capability === "roster.read") {
        return Promise.reject(new HostCallError({
          kind: "unavailable", message: "not this test",
        }));
      }

      sent.push({ capability, method, params });
      if (refuse !== undefined) return Promise.reject(refuse);
      return Promise.resolve({ id: "d-new", version: 1 });
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

  find(root, ".head .tab", "Duty").click();
  await settle();
  find(root, ".btn", "Assign duty").click();
  await settle();

  return root;
}

/** Type into one of the two datetime controls. */
function type(root: HTMLElement, which: 0 | 1, value: string): void {
  const input = Array.from(
    root.querySelectorAll<HTMLInputElement>("input.inp"))[which]!;
  input.value = value;
  input.dispatchEvent(new Event("input"));
}

describe("assigning a duty", () => {
  it("offers the property's own people, choosing none", async () => {
    const root = await dialog([]);

    // Three names used to be written into the module, so a property saw the
    // same three strangers whoever it employed.
    const offered = Array.from(root.querySelectorAll<HTMLElement>("button.pk"))
      .map((one) => one.textContent);

    expect(offered.length).toBe(recordedRegister.candidates.length);
    expect(offered[0]).toContain(recordedRegister.candidates[0]!.name);
    expect(root.querySelectorAll(".pk.on")).toHaveLength(0);
  });

  it("asks for a span rather than supplying one", async () => {
    const root = await dialog([]);
    const inputs = Array.from(root.querySelectorAll<HTMLInputElement>("input.inp"));

    // Two real controls, both empty. `datetime-local` because a duty may cross
    // midnight — WF-Q8 — so a time alone cannot say which day it lands on.
    expect(inputs).toHaveLength(2);
    for (const input of inputs) {
      expect(input.type).toBe("datetime-local");
      expect(input.value).toBe("");
    }
  });

  it("names each thing it is waiting for, in turn", async () => {
    const root = await dialog([]);
    const reason = (): string => root.querySelector(".acts .note")?.textContent ?? "";

    expect(reason()).toBe("Choose somebody");

    root.querySelector<HTMLButtonElement>("button.pk")!.click();
    await settle();
    expect(reason()).toBe("Set when the duty starts and ends");

    type(root, 0, "2026-08-28T20:00");
    type(root, 1, "2026-08-28T08:00");
    await settle();

    // Ends before it starts — refused before the write rather than by it.
    expect(reason()).toBe("A duty has to end after it starts");

    type(root, 1, "2026-08-29T08:00");
    await settle();
    expect(reason()).toBe("");
  });

  it("sends the person and both instants", async () => {
    const sent: Sent[] = [];
    const root = await dialog(sent);

    root.querySelector<HTMLButtonElement>("button.pk")!.click();
    type(root, 0, "2026-08-28T20:00");
    type(root, 1, "2026-08-29T08:00");
    await settle();

    find<HTMLButtonElement>(root, ".acts button.btn", "Assign duty").click();
    await settle();

    expect(sent).toHaveLength(1);
    expect(sent[0]!.capability).toBe("duty.assign");
    expect(sent[0]!.method).toBe("assign");

    const params = sent[0]!.params as { staffId: string; from: string; to: string };
    expect(params.staffId).toBe(recordedRegister.candidates[0]!.staffId);

    // Instants on the wire, and the two ends really do cross midnight.
    expect(new Date(params.to).getTime())
      .toBeGreaterThan(new Date(params.from).getTime());
  });

  it("keeps the dialog open on a refusal, carrying the reason", async () => {
    const sent: Sent[] = [];
    const root = await dialog(sent, new HostCallError({
      kind: "rejected",
      message: "Rahul Nair already holds a duty overlapping that span.",
    }));

    root.querySelector<HTMLButtonElement>("button.pk")!.click();
    type(root, 0, "2026-08-28T20:00");
    type(root, 1, "2026-08-29T08:00");
    await settle();

    find<HTMLButtonElement>(root, ".acts button.btn", "Assign duty").click();
    await settle();

    expect(root.querySelector(".scrim")).not.toBeNull();
    expect(root.textContent)
      .toContain("Rahul Nair already holds a duty overlapping that span.");
  });
});

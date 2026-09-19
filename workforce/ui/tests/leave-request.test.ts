import type { HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import type { Balance } from "../roster/leave";
import { requestForm } from "../screens/leave/form";

/**
 * Request leave — the form raises a request the service accepts.
 *
 * `leave.request · raise` takes `typeId`, `from`, `to` and an optional `note`,
 * and derives whose leave it is from the caller (ADR 0172): there is no field
 * for anybody else, so the form has none. The owner met this form dead on
 * 0.3.3 — every box a drawing, and the primary live over nothing.
 */

const TYPES: Balance[] = [
  { id: "7d1c5e20-4b3a-4f6e-9a18-2c5d7e9f0a13", type: "Casual", days: 4, of: 8, accruesPerMonth: 2 },
  { id: "c3a9f1e4-8d27-4b5c-a061-9e2f4d7b8c55", type: "Sick", days: 6, of: 12, accruesPerMonth: null },
];

function host(calls: unknown[]): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["leave.request"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (capability: string, method: string, params?: unknown) => {
      calls.push({ capability, method, params });
      return Promise.resolve({ id: "new", version: 1, days: 3 });
    },
    on: () => () => {},
  };
}

function set(form: HTMLElement, selector: string, value: string): void {
  const field = form.querySelector<HTMLInputElement | HTMLSelectElement>(selector);
  if (field === null) throw new Error(`no ${selector} on the form`);
  field.value = value;
  field.dispatchEvent(new Event(field.tagName === "SELECT" ? "change" : "input"));
}

const settle = (): Promise<void> => new Promise((resolve) => { setTimeout(resolve, 1); });

/**
 * The confirm, by position. Not `.btn.pri`: the shared foot drops `pri` while
 * it waits (`chrome/confirm.ts`), so that selector finds nothing in exactly the
 * state this file asserts about.
 */
const CONFIRM = ".acts button:last-of-type";

describe("request leave", () => {
  it("raises exactly what was entered, and names no requester", async () => {
    const calls: unknown[] = [];
    let done = false;
    const form = requestForm(host(calls), TYPES, () => {}, () => { done = true; });

    set(form, "select[name=type]", TYPES[1]!.id);
    set(form, "input[name=from]", "2026-10-06");
    set(form, "input[name=to]", "2026-10-08");
    set(form, "input[name=note]", "Clinic appointment");

    form.querySelector<HTMLButtonElement>(CONFIRM)?.click();
    await settle();

    expect(calls).toEqual([{
      capability: "leave.request",
      method: "raise",
      params: { typeId: TYPES[1]!.id, from: "2026-10-06", to: "2026-10-08", note: "Clinic appointment" },
    }]);
    expect(done).toBe(true);
  });

  it("waits, with its reason, until a type and both days are chosen — and sends nothing", async () => {
    const calls: unknown[] = [];
    const form = requestForm(host(calls), TYPES, () => {}, () => {});
    const primary = form.querySelector<HTMLButtonElement>(CONFIRM);

    expect(primary?.hasAttribute("disabled")).toBe(true);
    expect(form.querySelector(".acts .note")?.textContent).toBe("Choose a type of leave");

    set(form, "select[name=type]", TYPES[0]!.id);
    expect(form.querySelector(".acts .note")?.textContent).toBe("Choose the first and last day");

    set(form, "input[name=from]", "2026-10-08");
    set(form, "input[name=to]", "2026-10-06");
    expect(form.querySelector(".acts .note")?.textContent).toBe("The last day is before the first");

    primary?.click();
    await settle();
    expect(calls).toEqual([]);
  });

  it("shows the chosen type's balance, and still lets an overdrawn request be raised", () => {
    // WF-Q5: warn, never block. The balance appears once a type is chosen —
    // a balance beside no type is a figure about nothing.
    const overdrawn: Balance[] = [{ ...TYPES[0]!, days: -1 }];
    const form = requestForm(host([]), overdrawn, () => {}, () => {});

    expect(form.querySelector(".balance")).toBeNull();
    set(form, "select[name=type]", overdrawn[0]!.id);
    expect(form.querySelector(".balance")?.textContent).toBe("-1 of 8 left");

    set(form, "input[name=from]", "2026-10-06");
    set(form, "input[name=to]", "2026-10-06");
    expect(form.querySelector(CONFIRM)?.hasAttribute("disabled")).toBe(false);
  });
});

import type { HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { newShift } from "../screens/policy/dialog";

/**
 * New shift — the form defines a shift the service accepts.
 *
 * `roster.configure · defineShift` takes a name, a short code, a colour, the
 * two ends of up to two spans (none for an off shift) and the first day the
 * shift may be used. The owner met this form dead on 0.3.3: the name field
 * would not take typing and Create did nothing.
 */

function host(calls: unknown[]): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.configure"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (capability: string, method: string, params?: unknown) => {
      calls.push({ capability, method, params });
      return Promise.resolve({ id: "new", version: 1 });
    },
    on: () => () => {},
  };
}

function type(form: HTMLElement, name: string, value: string): void {
  const field = form.querySelector<HTMLInputElement>(`input[name=${name}]`);
  if (field === null) throw new Error(`no ${name} field on the form`);
  field.value = value;
  field.dispatchEvent(new Event("input"));
}

function press(form: HTMLElement, text: string): void {
  const button = Array.from(form.querySelectorAll<HTMLButtonElement>("button"))
    .find((one) => one.textContent === text || one.getAttribute("aria-label") === text);
  if (button === undefined) throw new Error(`no "${text}" control on the form`);
  button.click();
}

const CONFIRM = ".acts button:last-of-type";
const reason = (form: HTMLElement): string => form.querySelector(".acts .note")?.textContent ?? "";
const settle = (): Promise<void> => new Promise((resolve) => { setTimeout(resolve, 1); });

describe("new shift", () => {
  it("defines a split working shift with exactly what was entered", async () => {
    const calls: unknown[] = [];
    let done = false;
    const form = newShift(host(calls), () => {}, () => { done = true; });

    type(form, "name", "Banquet split");
    type(form, "code", "BQ");
    press(form, "Working");
    type(form, "startsAt", "10:00");
    type(form, "endsAt", "14:00");
    type(form, "secondStartsAt", "18:00");
    type(form, "secondEndsAt", "23:00");
    press(form, "Amber");
    type(form, "from", "2026-10-01");

    form.querySelector<HTMLButtonElement>(CONFIRM)?.click();
    await settle();

    expect(calls).toEqual([{
      capability: "roster.configure",
      method: "defineShift",
      params: {
        name: "Banquet split", code: "BQ", colour: "Amber", from: "2026-10-01",
        startsAt: "10:00", endsAt: "14:00", secondStartsAt: "18:00", secondEndsAt: "23:00",
      },
    }]);
    expect(done).toBe(true);
  });

  it("defines an off shift with no times at all", async () => {
    const calls: unknown[] = [];
    const form = newShift(host(calls), () => {}, () => {});

    type(form, "name", "Week off");
    type(form, "code", "WO");
    press(form, "Off");
    press(form, "Slate");
    type(form, "from", "2026-10-01");

    form.querySelector<HTMLButtonElement>(CONFIRM)?.click();
    await settle();

    expect(calls).toEqual([{
      capability: "roster.configure",
      method: "defineShift",
      params: { name: "Week off", code: "WO", colour: "Slate", from: "2026-10-01" },
    }]);
  });

  it("waits with the reason the service would refuse for, and sends nothing", async () => {
    const calls: unknown[] = [];
    const form = newShift(host(calls), () => {}, () => {});

    expect(reason(form)).toBe("Name the shift");
    type(form, "name", "Morning");
    expect(reason(form)).toBe("Give it a short code");
    type(form, "code", "M");
    expect(reason(form)).toBe("Choose working or off");
    press(form, "Working");
    expect(reason(form)).toBe("Set when it starts and ends");
    type(form, "startsAt", "07:00");
    type(form, "endsAt", "07:00");
    expect(reason(form)).toBe("A shift cannot end when it starts");
    type(form, "endsAt", "15:00");
    type(form, "secondStartsAt", "18:00");
    expect(reason(form)).toBe("Set both ends of the second span");
    type(form, "secondStartsAt", "");
    expect(reason(form)).toBe("Choose a colour");
    press(form, "Cyan");
    expect(reason(form)).toBe("Choose the first day it can be used");

    form.querySelector<HTMLButtonElement>(CONFIRM)?.click();
    await settle();
    expect(calls).toEqual([]);
  });
});

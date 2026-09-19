import type { HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { recordedLeave } from "../roster/leave";
import { leave } from "../screens/leave";
import { newShift } from "../screens/policy/dialog";

/**
 * A form nothing can be typed into shows nothing typed — and does not offer to
 * save it.
 *
 * **The app surface audit (2026-09-19, C11 · F1 · C8) found both forms showing a
 * finished request nobody made.** Leave named two real-looking people — *"for"*
 * one and *"raised by"* another, as literals in the screen — picked the
 * overdrawn balance *"because that is the one the frame raises the warning
 * against"*, and stated *"3 days. 2 of your team are already away on the 15th."*
 * Shift showed a name, a code, times, a chosen kind and a chosen colour. Under
 * each, a live primary over fields that accept nothing: *"never
 * live-and-refusing"* (§2), and *"A field renders a value the desk has already
 * chosen"* (§10) — nobody chose these.
 */

const INVENTED = [
  "Joseph Kurian", "Priya Thomas", "14 Sep 2026", "16 Sep 2026", "Brother's wedding",
  "2 of your team", "Split — Banquet", "10:00", "14:00", "18:00", "22:00",
];

function leaveHost(): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: () => Promise.resolve(recordedLeave),
    on: () => () => {},
  };
}

async function leaveForm(): Promise<HTMLElement> {
  const main = document.createElement("main");
  await leave(leaveHost(), main, "Requests", () => {}, true);
  const form = main.querySelector(".dlg");
  if (form === null) throw new Error("the leave form did not open");
  return form as HTMLElement;
}

function shiftForm(): HTMLElement {
  const form = newShift(() => {}).querySelector(".dlg");
  if (form === null) throw new Error("the shift form did not open");
  return form as HTMLElement;
}

function inert(form: HTMLElement, name: string): void {
  const text = form.textContent ?? "";
  for (const value of INVENTED) {
    expect(text, `${name} shows "${value}", which nobody entered`).not.toContain(value);
  }

  // Every value box is the placeholder: nobody has supplied a value (§10).
  for (const box of Array.from(form.querySelectorAll(".inp"))) {
    expect(box.classList.contains("ph"), `${name}: a value box shows a value`).toBe(true);
  }

  // Nothing is pre-chosen.
  expect(form.querySelector(".choice.on, .sw.on"), `${name}: a choice is pre-selected`).toBeNull();

  // The primary is a real control, off and disabled, with its reason beside it.
  const primary = form.querySelector(".acts .btn.pri");
  expect(primary?.tagName, `${name}: the primary is not a button`).toBe("BUTTON");
  expect(primary?.classList.contains("off"), `${name}: the primary is live over nothing`).toBe(true);
  expect(primary?.hasAttribute("disabled")).toBe(true);
  expect(form.querySelector(".acts .why")?.textContent ?? "", `${name}: no reason beside it`).not.toBe("");

  // Every control is a button (C8).
  for (const control of Array.from(form.querySelectorAll(".btn"))) {
    expect(control.tagName, `${name}: "${control.textContent}" is a ${control.tagName}`).toBe("BUTTON");
  }
}

describe("a form that accepts nothing", () => {
  it("leave shows no invented request, and offers nothing to save", async () => {
    inert(await leaveForm(), "leave");
  });

  it("shift shows no invented shift, and offers nothing to save", () => {
    inert(shiftForm(), "shift");
  });
});

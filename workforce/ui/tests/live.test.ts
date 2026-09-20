import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { ANSWERS } from "./surfaces";

/**
 * No control looks live and does nothing — pressed, not inspected.
 *
 * `no-dead-controls` asks whether a thing drawn as pressable IS a control, and
 * `mouse-only` asks whether a click listener went to one. **Neither asks
 * whether pressing it does anything**, and HH found the hole in exactly that
 * gap: two Jobs tabs wired to `() => {}` passed a guard that read "has a
 * handler" as "is alive" (`be1c6730`). Proved here before this file existed:
 * planting `() => {}` on Workforce's Leave tabs, in a worktree, failed nothing
 * at all. On Copy it failed one test, so the cover was partial and uneven.
 *
 * So this presses every enabled button on every place a person can reach, one
 * press per fresh mount, and the press must do something **observable**: a call
 * to the host, a redraw, a field's value changing, focus moving, or something
 * scrolled into view. It never asks whether a handler exists.
 *
 * **Through the real application** (`activate`), never through `SURFACES`: that
 * list hands each screen no-op navigation callbacks, so a live week arrow would
 * look dead and this walk would be measuring its own harness.
 *
 * **The current choice is exempt**, because pressing what is already chosen
 * correctly changes nothing: a section or view tab already carrying `on`, and
 * the pager's current page. That exemption is the one thing this walk cannot
 * see, so a choice with no alternative is not covered here.
 *
 * Modelled on Room Care's `live.test.ts` (KK), which is that application's own
 * walk over its own places. The technique is shared; the places cannot be.
 */

interface Call { capability: string; method: string; params?: unknown }

/** Every capability the module declares, so nothing is refused for lack of one. */
const CAPABILITIES = [
  "roster.read", "roster.plan", "roster.configure", "posting.assign",
  "leave.request", "leave.approve", "duty.assign", "swap.propose", "swap.approve",
  "attendance.record", "attendance.amend",
];

function host(calls: Call[]): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: CAPABILITIES },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (capability: string, method: string, params?: unknown) => {
      calls.push({ capability, method, params });
      if (method in ANSWERS) return Promise.resolve(ANSWERS[method]);
      // A write: answered the way a service answers one, so the screen can
      // close and redraw rather than drawing a refusal every time.
      if (method === "ending") {
        return Promise.reject(new HostCallError({ kind: "unavailable", message: "not this walk" }));
      }
      return Promise.resolve({ id: "00000000-0000-4000-8000-000000000001", version: 2, state: "Approved" });
    },
    on: () => () => {},
  };
}

async function settle(): Promise<void> {
  for (let turn = 0; turn < 10; turn += 1) {
    await new Promise((resolve) => { setTimeout(resolve, 1); });
  }
}

/** A place is a section, and a view within it when the section offers a choice. */
interface Place { section: string; view: string | null }

function label(button: Element): string {
  return (button.textContent ?? "").trim().replace(/\s+/gu, " ");
}

/** The tabs of one strip, as labels — the bar's sections, or a section's views. */
function tabs(root: HTMLElement, strip: string): string[] {
  return Array.from(root.querySelectorAll<HTMLElement>(`${strip} > button.tab`), label);
}

async function mount(calls: Call[]): Promise<HTMLElement> {
  const root = document.createElement("div");
  activate(host(calls)).mount(root);
  await settle();
  return root;
}

/** Press the tab with this label, in the bar or the view strip. */
async function press(root: HTMLElement, strip: string, name: string): Promise<void> {
  const tab = Array.from(root.querySelectorAll<HTMLButtonElement>(`${strip} > button.tab`))
    .find((one) => label(one) === name);
  if (tab === undefined) throw new Error(`no "${name}" tab in ${strip}`);
  tab.click();
  await settle();
}

/** Go to a place from a fresh mount. */
async function at(place: Place, calls: Call[]): Promise<HTMLElement> {
  const root = await mount(calls);
  await press(root, ".head", place.section);
  if (place.view !== null) await press(root, ".tabs", place.view);
  return root;
}

/**
 * Every button this walk presses: enabled, and not the choice already made.
 *
 * A drawn-off control is `disabled` and is not a promise to anybody, so it is
 * not pressed. A tab or page already carrying `on` is the current choice.
 */
function pressable(root: HTMLElement): HTMLButtonElement[] {
  return Array.from(root.querySelectorAll<HTMLButtonElement>("button"))
    .filter((one) => !one.disabled && !one.classList.contains("on"));
}

/** Fill every text field, so a Clear or a Cancel has something to act on. */
function fill(root: HTMLElement): void {
  for (const field of Array.from(root.querySelectorAll<HTMLInputElement | HTMLTextAreaElement>(
    "input:not([type=checkbox]):not([type=radio]), textarea"))) {
    if (field.type === "date" || field.type === "time" || field.type === "number") continue;
    field.value = "typed for the walk";
  }
}

/** Everything a person could notice: what is drawn, what is typed, where focus is, what was asked. */
function state(root: HTMLElement, calls: Call[]): string {
  const typed = Array.from(
    root.querySelectorAll<HTMLInputElement | HTMLSelectElement>("input, textarea, select"),
    (one) => one.value).join("\u0000");
  return [root.innerHTML, typed, document.activeElement?.outerHTML ?? "", String(calls.length)].join("\u0000");
}

/** Press it, and say whether anything observable followed. */
async function acts(root: HTMLElement, calls: Call[], button: HTMLButtonElement): Promise<boolean> {
  fill(root);
  const before = state(root, calls);

  let scrolled = false;
  const scroll = Element.prototype.scrollIntoView;
  Element.prototype.scrollIntoView = function stub() { scrolled = true; };
  try {
    button.click();
    await settle();
  } finally {
    Element.prototype.scrollIntoView = scroll;
  }

  return scrolled || state(root, calls) !== before;
}

/** The places, read from the application rather than written out here. */
async function places(): Promise<Place[]> {
  const calls: Call[] = [];
  const root = await mount(calls);
  const found: Place[] = [];

  for (const section of tabs(root, ".head")) {
    const here = await at({ section, view: null }, calls);
    const views = tabs(here, ".tabs");
    if (views.length === 0) found.push({ section, view: null });
    for (const view of views) found.push({ section, view });
  }

  return found;
}

describe("every control on every place", () => {
  it("presses each one, and each does something", async () => {
    const walked = await places();
    expect(walked.length).toBeGreaterThan(4);

    const dead: string[] = [];
    let pressed = 0;

    for (const place of walked) {
      const calls: Call[] = [];
      const first = await at(place, calls);
      const many = pressable(first).length;

      for (let index = 0; index < many; index += 1) {
        const own: Call[] = [];
        const root = await at(place, own);
        const button = pressable(root)[index];
        if (button === undefined) continue;

        const name = label(button) || button.getAttribute("aria-label") || "(unlabelled)";
        pressed += 1;
        if (!await acts(root, own, button)) {
          dead.push(`${place.section}${place.view === null ? "" : ` · ${place.view}`} — "${name}"`);
        }
      }
    }

    // Positive control: the walk reached places and pressed things. A clean
    // result is only a result if it judged something.
    expect(walked.length, "no places").toBeGreaterThan(4);
    expect(pressed, "nothing was pressed").toBeGreaterThan(20);
    expect(dead).toEqual([]);
  }, 120_000);
});

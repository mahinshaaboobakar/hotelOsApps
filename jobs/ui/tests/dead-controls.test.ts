import type { HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { host, open, SCREENS, settle } from "./walk";

/**
 * No control looks live and does nothing (owner, 2026-09-19: "the owner found
 * dead buttons after approving Part A pages").
 *
 * Every enabled `<button>` on every screen — and on whatever each of its
 * controls opens — is pressed once, from a fresh mount, and must have an
 * **observable effect**: the screen changed, a field's value changed, focus
 * moved, or a request was made of the host. Having a handler is not enough —
 * the first version of this guard asked only that (`data-acts`), and the
 * Catalogue's two sub-tabs, wired to `() => {}`, passed it.
 *
 * Before each press every text field is filled, so a Cancel that clears what
 * was typed has something to clear. The current tab or chip (`.on`) is exempt:
 * being current is its state, and pressing it again rightly changes nothing.
 *
 * **Red before its green**: at `eab8dd07`, nine buttons with no handler; with
 * the Catalogue's `() => {}` tabs restored in a detached worktree, both tabs
 * (the handler-only version passed them).
 *
 * **An armed control is walked, and that is by construction.** KK asked (Room
 * Care, 2026-09-20) whether a control that only becomes live after another
 * control in the same place is seen by a walk that always opens fresh. Here it
 * is: whenever a press changes the screen, the walk presses every control in
 * THAT state — the armed one — which is how "Revoke…" pressed twice was found.
 * Probed by hand across Raise, Catalogue, both policy places and three job
 * tabs: nothing inert. (The first probe reported 100 pairs; it re-armed on a
 * fresh mount and went on measuring the old one. Each one it named acts when
 * measured properly, so the list was the probe's fault, not the build's.)
 * KK's other blindness — a probe listing its targets BEFORE arming misses a
 * control drawn off until armed — cannot apply here twice over: the walk lists
 * what each state holds when it walks that state, and no Jobs control toggles
 * `disabled` at runtime (two static disables, `element.ts` `off()` and the
 * board's My-departments chip; nothing arms on typing).
 *
 * **How deep, stated so a later reader is not misled by a green run.** The walk
 * itself goes two presses deep: every control on a screen, then every control
 * inside whatever a press opened. Deeper places are walked only when named in
 * `PLACES` — the new-policy flow's step 3 is, because a hand probe of that
 * unwalked place on 2026-09-20 found three inert "＋ step" buttons, drawn off in
 * the same change. **Anything three deep and unnamed is still unwalked**, in
 * Jobs and in Room Care alike (KK, same day), so a green run here is not a claim
 * about those.
 */

const PLACES = [
  ...SCREENS,
  { name: "Raise", open: ["＋ Raise a job"] },
  // Three deep, and walked by name because the walk itself stops at two: this is
  // where the probe of 2026-09-20 found three inert "＋ step" buttons.
  { name: "Settings · New policy · The ladder", open: ["Settings", "All policies", "＋ New policy", "3 · The ladder"] },
];

interface Watched { root: HTMLElement; calls: () => number }

async function reached(steps: readonly string[]): Promise<Watched> {
  const base = host();
  let calls = 0;
  const counted: HostApi = { ...base, call: (c, m, p) => { calls += 1; return base.call(c, m, p); } };
  const root = document.createElement("div");
  document.body.replaceChildren(root);
  activate(counted).mount(root);
  await settle();
  await open(root, steps);
  return { root, calls: () => calls };
}

function fields(root: HTMLElement): (HTMLInputElement | HTMLTextAreaElement)[] {
  return Array.from(root.querySelectorAll<HTMLInputElement | HTMLTextAreaElement>(
    "input:not([type=checkbox]):not([type=radio]):not([type=date]), textarea",
  )).filter((f) => !f.disabled && !f.readOnly);
}

function state(w: Watched): string {
  const values = fields(w.root).map((f) => f.value).join("");
  return [w.root.innerHTML, values, document.activeElement?.outerHTML ?? "", String(w.calls())].join("");
}

function pressable(root: HTMLElement): HTMLButtonElement[] {
  return Array.from(root.querySelectorAll<HTMLButtonElement>("button")).filter((b) => !b.disabled);
}

/** The labels of the controls on the screen `steps` reaches that have no effect when pressed. */
async function inert(steps: readonly string[]): Promise<string[]> {
  const found: string[] = [];
  const first = await reached(steps);
  const count = pressable(first.root).length;
  for (let i = 0; i < count; i += 1) {
    const w = await reached(steps);
    const button = pressable(w.root)[i];
    if (button === undefined || button.classList.contains("on")) continue;
    for (const f of fields(w.root)) f.value = "typed for the test";
    const before = state(w);
    const label = (button.textContent ?? "").trim();
    button.click();
    await settle();
    if (state(w) === before) found.push(label);
  }
  return found;
}

describe("no control looks live and does nothing", () => {
  for (const place of PLACES) {
    it(`${place.name}: every control has an effect, and so does every control one press away`, async () => {
      const found = new Set<string>();
      for (const label of await inert(place.open)) found.add(label);
      // One press away: a tab, a sheet or a form opened from here, read the same way.
      const opened = await reached(place.open);
      const labels = new Set(pressable(opened.root).map((b) => (b.textContent ?? "").trim()));
      for (const label of labels) {
        const w = await reached(place.open);
        const target = pressable(w.root).find((b) => (b.textContent ?? "").trim() === label);
        if (target === undefined) continue;
        const before = w.root.innerHTML;
        target.click();
        await settle();
        if (w.root.innerHTML === before) continue;
        for (const inner of await inert([...place.open, label])) found.add(`after "${label}" — ${inner}`);
      }
      expect([...found].sort(), place.name).toEqual([]);
    }, 600_000);
  }
});

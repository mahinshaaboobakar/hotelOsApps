import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { host, open, SCREENS, settle } from "./walk";

/**
 * No control looks live and does nothing (owner, 2026-09-19: "the owner found
 * dead buttons after approving Part A pages").
 *
 * Every enabled `<button>` on every screen — as it opens, and after each of its
 * controls is pressed once from a fresh mount, so a sheet, a tab and a form are
 * read too — must be wired: `control()` marks what it wires with `data-acts`,
 * because a listener is invisible to the DOM. A button with nothing behind it is
 * drawn off with its reason, or is not a button.
 *
 * **Red before its green** (2026-09-19): nine buttons on the code at `eab8dd07` —
 * the Work tab's Pause and Stop, Link a job…, Add a step…, Unlink, Remind me…,
 * the Catalogue's and the policy list's Edit, and every resolution chip.
 */

const PLACES = [...SCREENS, { name: "Raise", open: ["＋ Raise a job"] }];

async function reached(steps: readonly string[]): Promise<HTMLElement> {
  const root = document.createElement("div");
  document.body.replaceChildren(root);
  activate(host()).mount(root);
  await settle();
  await open(root, steps);
  return root;
}

function dead(root: HTMLElement): string[] {
  return Array.from(root.querySelectorAll<HTMLButtonElement>("button"))
    .filter((b) => !b.disabled && !b.hasAttribute("data-acts"))
    .map((b) => (b.textContent ?? "").trim());
}

describe("no control looks live and does nothing", () => {
  for (const place of PLACES) {
    it(`${place.name}, and everything one press away`, async () => {
      const found = new Set<string>();
      const first = await reached(place.open);
      for (const label of dead(first)) found.add(label);
      const count = first.querySelectorAll("button").length;
      for (let i = 0; i < count; i += 1) {
        const root = await reached(place.open);
        const button = root.querySelectorAll<HTMLButtonElement>("button")[i];
        if (button === undefined || button.disabled) continue;
        button.click();
        await settle();
        for (const label of dead(root)) found.add(label);
      }
      expect([...found].sort(), place.name).toEqual([]);
    }, 120_000);
  }
});

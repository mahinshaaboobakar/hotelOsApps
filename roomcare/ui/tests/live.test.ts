import { describe, expect, it } from "vitest";

import { SUPERVISOR, click, settle } from "./host";
import { PLACES, current, pressable, reach } from "./places";
import type { Call } from "./host";

/**
 * No control looks live and does nothing (owner ruling, 2026-09-19: dead
 * buttons found by using the product, after the pages were approved).
 *
 * Every enabled `<button>` on every place a person can reach is pressed, one
 * per fresh mount, and the press must do something observable: make a call,
 * change what is drawn (a redraw, a sheet, a navigation), or scroll something
 * into view. A control that cannot act is drawn off — a `.btn.off` span with
 * its reason, or `disabled` — and is not a button this walk presses. The one
 * exemption is the CURRENT choice (`aria-pressed="true"`, or the pager's page
 * being shown): pressing what is already chosen changes nothing, correctly.
 * That exemption is why a choice with no alternative is tested on its own
 * below — the walk cannot see it. The places are `tests/places.ts`'s, shared with
 * the developer-content walk, and include a room's page and the door, which are
 * reached by navigating rather than by a sheet.
 *
 * Widened on 2026-09-19 after HH's Jobs guard (be1c6730) passed tabs wired to
 * `() => {}`. It already asked for an effect, not a handler; what it lacked was
 * depth and inputs. Now every button inside whatever a press opens is pressed too,
 * each from a fresh mount; text fields are filled before every press, so a Cancel
 * has something to clear; and a field's value or where focus is counts as an
 * effect. "The walk itself" below proves it tells a dead handler from a live one.
 * First run: the door's End… sheet drew its chosen ending without `aria-pressed`.
 */
/** The text fields a press could act on: filled before each press, so a Cancel or a Clear has something to clear. */
function fill(root: HTMLElement): void {
  for (const field of root.querySelectorAll<HTMLInputElement | HTMLTextAreaElement>(
    "input:not([type=checkbox]):not([type=radio]):not([type=date]):not([type=number]):not([type=time]), textarea")) field.value = "typed for the test";
}

/** Everything a press could change that a person would notice: the page, a field's value, where focus is, a request. */
function state(root: HTMLElement, calls: Call[]): string {
  const values = [...root.querySelectorAll<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>("input, textarea, select")].map((f) => f.value).join("");
  return [root.innerHTML, values, document.activeElement?.outerHTML ?? "", String(calls.length)].join("");
}

/** Press a button and say whether anything observable followed — never whether it merely has a handler (HH, be1c6730). */
async function acts(root: HTMLElement, calls: Call[], button: HTMLButtonElement): Promise<boolean> {
  fill(root);
  const before = state(root, calls);
  let scrolled = false;
  const scroll = Element.prototype.scrollIntoView;
  Element.prototype.scrollIntoView = () => { scrolled = true; };
  button.click();
  await settle();
  Element.prototype.scrollIntoView = scroll;
  return scrolled || state(root, calls) !== before;
}

/** The enabled buttons in whatever a press opened — a sheet, a dialog, a confirmation. */
function opened(root: HTMLElement): HTMLButtonElement[] {
  return [...root.querySelectorAll<HTMLButtonElement>(".scrim button")].filter((b) => !b.hasAttribute("disabled"));
}

describe("every control a person can press", () => {
  for (const [place, capabilities, section, tab, into] of PLACES) {
    it(`does something when pressed — ${place}, and in everything a press opens`, async () => {
      const count = pressable(await reach(capabilities, section, tab, [], into)).length;
      expect(count, "the walk found no controls; it is not measuring this place").toBeGreaterThan(0);
      const dead: string[] = [];
      let inner = 0;
      for (let i = 0; i < count; i += 1) {
        const calls: Call[] = [];
        const root = await reach(capabilities, section, tab, calls, into);
        const button = pressable(root)[i];
        if (button === undefined || current(button)) continue;
        const label = button.textContent?.trim() ?? "";
        if (!(await acts(root, calls, button))) dead.push(`${i}: "${label}"`);
        // Whatever that press opened is walked too, each of its buttons from a fresh mount and a fresh first press.
        const within = opened(root).length;
        for (let j = 0; j < within; j += 1) {
          const again: Call[] = [];
          const fresh = await reach(capabilities, section, tab, again, into);
          const first = pressable(fresh)[i];
          if (first === undefined) continue;
          first.click();
          await settle();
          const target = opened(fresh)[j];
          if (target === undefined || current(target)) continue;
          inner += 1;
          if (!(await acts(fresh, again, target))) dead.push(`${i}: "${label}" → "${target.textContent?.trim()}"`);
        }
      }
      expect(dead, `looks live and does nothing on ${place} (${inner} controls pressed inside what opened)`).toEqual([]);
    }, 180_000);
  }
});

describe("the walk itself", () => {
  // A walk that cannot tell a dead button from a live one passes everything. Planted: a button whose handler exists
  // and does nothing (HH's Jobs tabs, be1c6730), a Cancel that clears a filled field, one that moves focus, and one
  // that makes a request. Only the first may read as dead.
  it("tells a handler that does nothing from a press that changes something", async () => {
    const root = document.createElement("div");
    document.body.replaceChildren(root);
    const field = document.createElement("input");
    root.append(field);
    const make = (text: string, onClick: () => void): HTMLButtonElement => {
      const b = document.createElement("button");
      b.textContent = text;
      b.addEventListener("click", onClick);
      root.append(b);
      return b;
    };
    const calls: Call[] = [];
    const nothing = make("Tab", () => {});
    const clears = make("Cancel", () => { field.value = ""; });
    const focuses = make("Next", () => field.focus());
    const asks = make("Save", () => { calls.push({ capability: "roomcare.configure", method: "savePolicy", params: {} }); });
    expect([await acts(root, calls, nothing), await acts(root, calls, clears), await acts(root, calls, focuses), await acts(root, calls, asks)])
      .toEqual([false, true, true, true]);
  });
});

describe("a choice with nothing to choose between", () => {
  // Zone is the only grouping the board and the Room states sheet have. Drawn as a pressed chip it looks like one
  // option of several, and pressing it does nothing — so it is said as a label, not offered as a control.
  it("is said, not offered as a control — the board and the Room states sheet group by zone", async () => {
    for (const section of ["Board", "Room states"]) {
      const root = await reach(SUPERVISOR, section, null, []);
      const zone = [...root.querySelectorAll(".body button")].filter((b) => b.textContent?.trim() === "Zone");
      expect(zone, section).toEqual([]);
      expect(root.querySelector(".body")?.textContent, section).toContain("grouped by zone");
    }
  });
});

describe("Apply to selected, on the Room states sheet", () => {
  // With rows selected and nothing chosen to set, it used to be a live button that changed nothing — the one dead
  // case the press-every-control walk could not reach, because the walk never selects a row.
  it("is drawn off, saying why, until there is something to apply — then applies it", async () => {
    const root = await reach(SUPERVISOR, "Room states", null, []);
    // One room's box, not the header's (which selects every row). The sheet keeps its selection in module state
    // across mounts, so another test may have left rows selected: start from none.
    const clear = async (): Promise<void> => {
      for (const on of root.querySelectorAll<HTMLInputElement>('.body input[aria-label^="select "]:not([aria-label="select every room shown"]):checked')) on.click();
      await settle();
    };
    await clear();
    // The "show" filter is module state too; a walk may have left it on one that shows no rows.
    click(root, ".chips button", "All");
    await settle();
    const box = root.querySelector<HTMLInputElement>('.body input[aria-label^="select "]:not([aria-label="select every room shown"])');
    expect(box, "the sheet draws a box to select a room with").not.toBeNull();
    box!.click();
    await settle();
    const dock = (): HTMLElement => root.querySelector<HTMLElement>(".dock")!;
    try {
      expect(dock().querySelector("button")?.textContent ?? "").not.toContain("Apply to selected");
      expect(dock().textContent).toContain("Apply to selected — choose what to set first");
      const condition = dock().querySelector<HTMLSelectElement>("select")!;
      condition.value = condition.options[condition.options.length - 1]!.value; // "inspected": not the fixture room's own
      condition.dispatchEvent(new Event("change"));
      const apply = [...dock().querySelectorAll("button")].find((b) => b.textContent === "Apply to selected");
      expect(apply, "choosing a value makes Apply live").toBeDefined();
      apply!.click();
      await settle();
      expect(root.querySelector(".strip")?.textContent ?? "", "the applied values are waiting to be saved").toMatch(/Save [0-9]+ changes?/u);
    } finally {
      await clear();
    }
  });
});

describe("a selection on the Room states sheet", () => {
  // "Apply to selected" is a bulk change. A selection that outlives the screen lets it act on rows the person is no
  // longer looking at (architect, 2026-09-19: a safety problem, not a small one). Leaving — for another section, or
  // into a room — forgets it. The "show" filter is also remembered; whether it should be is the owner's choice and
  // is not changed here.
  const selectOne = async (root: HTMLElement): Promise<void> => {
    click(root, ".chips button", "All");
    await settle();
    const box = root.querySelector<HTMLInputElement>('.body input[aria-label^="select "]:not([aria-label="select every room shown"])');
    expect(box, "the sheet draws a box to select a room with").not.toBeNull();
    if (!box!.checked) box!.click();
    await settle();
    expect(root.querySelector(".dock")?.textContent).not.toMatch(/^0 rows selected/u);
  };
  const nothingSelected = (root: HTMLElement): void => {
    const dock = root.querySelector(".dock")?.textContent ?? "";
    expect(dock).toMatch(/^0 rows selected/u);
    expect(dock).toContain("Apply to selected — select rows first");
  };

  it("is forgotten when the person leaves for another section and comes back", async () => {
    const root = await reach(SUPERVISOR, "Room states", null, []);
    await selectOne(root);
    click(root, ".head .tab", "Board");
    await settle();
    click(root, ".head .tab", "Room states");
    await settle();
    nothingSelected(root);
  });

  it("is forgotten when the person opens a room from the sheet and returns to Room states", async () => {
    const root = await reach(SUPERVISOR, "Room states", null, []);
    await selectOne(root);
    click(root, ".body button", "▸");
    await settle();
    // A room's page has no back control; a person returns through the section tab.
    click(root, ".head .tab", "Room states");
    await settle();
    nothingSelected(root);
  });
});

describe("Apply to selected", () => {
  // The invariant is not the owner's choice; how it holds is. Interim: a filter change clears the selection. The
  // alternative the owner is shown is Apply acting only on the rows shown. This test holds either.
  it("never changes a row the person can't see", async () => {
    const root = await reach(SUPERVISOR, "Room states", null, []);
    const shown = (): string[] => [...root.querySelectorAll<HTMLInputElement>('.body input[aria-label^="select "]:not([aria-label="select every room shown"])')]
      .map((box) => box.getAttribute("aria-label")!.slice("select ".length));
    click(root, ".chips button", "All");
    await settle();
    root.querySelector<HTMLInputElement>('.body input[aria-label="select every room shown"]')!.click();
    await settle();
    click(root, ".chips button", "Vacant");
    await settle();
    const visible = shown();
    expect(visible.length, "some rooms are vacant, and some are not").toBeGreaterThan(0);
    const condition = root.querySelector<HTMLSelectElement>(".dock select")!;
    condition.value = condition.options[condition.options.length - 1]!.value;
    condition.dispatchEvent(new Event("change"));
    const apply = [...root.querySelectorAll<HTMLButtonElement>(".dock button")].find((b) => b.textContent === "Apply to selected");
    apply?.click();
    await settle();
    click(root, ".chips button", "Changed");
    await settle();
    const changed = shown();
    expect(changed.filter((room) => !visible.includes(room)), "rows changed while hidden by the filter").toEqual([]);
    click(root, ".chips button", "All");
    await settle();
  });
});

/**
 * The day list's `＋ assign` — drawn live on seven rows of the owner's gold
 * page, and drawn OFF by the build until 2026-10-04.
 *
 * **The sheet was never missing.** `overlays.ts` routes `overlay: "assign"`
 * and its comment names *"the day list's `＋ assign`"*; so does
 * `screens/assign/index.ts`'s own header. Two files asserted the route and
 * nothing wired it — a guarantee-comment pair, checked by nobody, while the
 * control said the capability was unavailable.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { beforeEach, describe, expect, it } from "vitest";

import type { PropertyEnvironment } from "@hotelos/sdk";

import type { DayRow } from "../book";
import { table } from "../screens/today/table";

const property: PropertyEnvironment = { timezone: "Asia/Kolkata", locale: "en-IN" };

const opened: string[] = [];
const assigned: string[] = [];

function row(over: Partial<DayRow> = {}): DayRow {
  return {
    id: "s1", version: 1, mayRecordNoShow: false,
    guest: "Joseph Thomas", contact: null, party: null, unnamed: false,
    booking: "BK-4471", roomType: "Deluxe", room: null,
    arrive: "2026-09-01", depart: "2026-09-02", chips: [],
    ...over,
  };
}

function drawn(r: DayRow): HTMLElement {
  return table([r], 1, {
    open: (x) => opened.push(x.id),
    noShow: () => {},
    assign: (x) => assigned.push(x.id),
  }, property);
}

const link = (root: HTMLElement): HTMLButtonElement | undefined =>
  [...root.querySelectorAll("button")].find((b) => b.textContent === "＋ assign");

describe("the day list's ＋ assign", () => {
  beforeEach(() => { opened.length = 0; assigned.length = 0; });

  it("is live on a row with no room, and opens the sheet", () => {
    const control = link(drawn(row({ room: null })));

    expect(control).toBeDefined();
    expect(control?.disabled).toBe(false);

    control?.click();
    expect(assigned).toEqual(["s1"]);
  });

  it("is absent where the row already has a room", () => {
    // **Not drawn off — absent.** A room number is what that cell holds once
    // there is one; an assigned row has nothing to assign, so there is no
    // control rather than a disabled one.
    const root = drawn(row({ room: "214" }));

    expect(link(root)).toBeUndefined();
    expect(root.textContent).toContain("214");
  });

  it("does not also open the stay behind the sheet", () => {
    // The row itself opens the stay, so the inline control has to stop the
    // click reaching it — the same property `nobody came` needed, and the one
    // a reader would not predict from either call site.
    link(drawn(row({ room: null })))?.click();

    expect(assigned).toEqual(["s1"]);
    expect(opened).toEqual([]);
  });
});

/**
 * N2 — recording a no-show from the day's own list. The owner's ruling of
 * 2026-09-24.
 *
 * **The screen draws what the service decided.** `NoShowRule` answers for both
 * this list and the stay page, so the two cannot offer different things about
 * one stay; what is checked here is that the row renders the flag it is given
 * and that pressing it does not also open the stay behind the dialog.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { beforeEach, describe, expect, it } from "vitest";

import type { PropertyEnvironment } from "@hotelos/sdk";

import type { DayRow } from "../book";
import { table } from "../screens/today/table";

const property: PropertyEnvironment = { timezone: "Asia/Kolkata", locale: "en-IN" };

const opened: string[] = [];
const recorded: string[] = [];

function row(over: Partial<DayRow>): DayRow {
  return {
    id: "s1", version: 1, mayRecordNoShow: false,
    guest: "Thomas George", contact: null, party: null, unnamed: false,
    booking: "BK-4361", roomType: "Deluxe Twin", room: null,
    arrive: "2026-08-19", depart: "2026-08-20", chips: [],
    ...over,
  };
}

const drawn = (r: DayRow): HTMLElement =>
  table([r], 1, (x) => opened.push(x.id), (x) => recorded.push(x.id), property);

const action = (root: HTMLElement): HTMLButtonElement | undefined =>
  [...root.querySelectorAll("button")].find((b) => b.textContent === "nobody came");

describe("N2 — the day list's no-show", () => {
  beforeEach(() => { opened.length = 0; recorded.length = 0; });

  it("offers the action on a row the service marked", () => {
    const control = action(drawn(row({ mayRecordNoShow: true })));

    expect(control).toBeDefined();
    control?.click();

    expect(recorded).toEqual(["s1"]);
  });

  it("offers nothing on a row it did not", () => {
    // The SAME dates. The screen does not re-derive the business day — if it
    // did, this row and the one above would render identically, and the list
    // would disagree with the stay page about one stay.
    expect(action(drawn(row({ mayRecordNoShow: false })))).toBeUndefined();
  });

  it("does not also open the stay when the action is pressed", () => {
    // **Without the row's own click rule this fails**, and it fails in the
    // worst way: the dialog opens over a stay page that also navigated, so
    // the desk confirms a forfeit on a screen that moved under them.
    const control = action(drawn(row({ mayRecordNoShow: true })));
    control?.click();

    expect(recorded).toEqual(["s1"]);
    expect(opened).toEqual([]);
  });

  it("still opens the stay when the row itself is clicked", () => {
    // The rule narrows to controls and must not swallow the row's own click —
    // a guard that stopped every click would cost the list its whole purpose.
    const root = drawn(row({ mayRecordNoShow: true }));
    (root.querySelector(".tr.act > div:nth-child(2)") as HTMLElement).click();

    expect(opened).toEqual(["s1"]);
    expect(recorded).toEqual([]);
  });
});

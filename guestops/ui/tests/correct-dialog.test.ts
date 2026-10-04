/**
 * C1 and RI — the two corrections the owner drew, and the words that tell them
 * apart.
 *
 * **Written because a mutation probe found nothing.** Removing fRI1's arm from
 * `words()` left 264 tests passing: `correct()` was driven by no test at all,
 * which was true of the departure arm shipped on 2026-09-24 as well. A mutation
 * that compiles and does not fail is a branch nobody is looking at.
 *
 * **The fixture is chosen so the two arms DISAGREE.** One stay with one target
 * would pass under a dialog that ignored the target entirely, so each case
 * asserts a sentence the other arm does not have — and `Nothing comes off` is
 * asserted ABSENT on the departure, which is the discriminator.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { beforeEach, describe, expect, it } from "vitest";

import type { PropertyEnvironment } from "@hotelos/sdk";

import type { CorrectPlan } from "../book";
import { correct, subjectOf } from "../screens/stay/correct";

const property: PropertyEnvironment = { timezone: "Asia/Kolkata", locale: "en-IN" };

const confirmed: { to: string; reason: string }[] = [];

/** fRI1's own stay — Thomas George, BK-4361, recorded a no-show on 1 Sept. */
function plan(over: Partial<CorrectPlan> = {}): CorrectPlan {
  return {
    stayId: "01J8F22B1",
    version: 7,
    to: "Booked",
    from: "NoShow",
    room: null,
    guest: "Thomas George",
    departedAt: null,
    noShowAt: "2026-09-01T23:14:00+05:30",
    reference: "BK-4361",
    arrive: "2026-08-19",
    depart: "2026-08-20",
    roomStillFree: null,
    reasons: ["Guest did arrive — the night desk missed them"],
    ...over,
  };
}

/** C1's stay — a departure recorded in error, which fRI1 is not. */
const departure: Partial<CorrectPlan> = {
  to: "InHouse",
  from: "Departed",
  room: "214",
  guest: "Rajesh Pillai",
  departedAt: "2026-09-24T07:02:00+05:30",
  noShowAt: null,
  roomStillFree: true,
};

function drawn(over: Partial<CorrectPlan> = {}): HTMLElement {
  const made = plan(over);
  const root = correct(made, subjectOf(made, property), () => {},
    (to, reason) => confirmed.push({ to, reason }));

  expect(root).not.toBeNull();
  return root as HTMLElement;
}

const title = (root: HTMLElement) => root.querySelector(".dh b")?.textContent;
const subtitle = (root: HTMLElement) => root.querySelector(".dh span")?.textContent;
const body = (root: HTMLElement) => root.querySelector(".db")?.textContent ?? "";
const labels = (root: HTMLElement) =>
  [...root.querySelectorAll("button")].map((one) => one.textContent);

describe("the correction dialog", () => {
  beforeEach(() => { confirmed.length = 0; });

  it("asks fRI1's question when a no-show goes back on the list", () => {
    const root = drawn();

    expect(title(root)).toBe("Put this stay back on the arrivals list?");
    expect(body(root)).toContain("The no-show is not erased.");

    // And the head carries the composed line, so the dialog is proven to USE
    // `subjectOf` rather than the test only exercising it beside the dialog.
    expect(subtitle(root)).toBe(subjectOf(plan(), property));
    expect(labels(root)).toEqual(["Leave it", "Put back on the list"]);
  });

  it("answers the forfeit, because ADR 0310 says there is nothing to reverse", () => {
    // `RecordNoShowAsync` writes no forfeiture record of any kind, so the
    // dialog says so rather than leaving the desk to wonder whether putting
    // the stay back undoes a charge.
    expect(body(drawn())).toContain("no charge was ever applied");
  });

  it("asks C1's question when a departure goes back in house", () => {
    const root = drawn(departure);

    expect(title(root)).toBe("Put this stay back in house?");
    expect(body(root)).toContain("The departure is not erased.");
    expect(labels(root)).toEqual(["Leave it", "Put back in house"]);
  });

  it("draws no forfeit row on a departure, which has none to discuss", () => {
    // **The discriminator.** Without this, a dialog that drew the row on both
    // arms would pass every assertion above.
    expect(body(drawn(departure))).not.toContain("no charge was ever applied");
  });

  it("composes fRI1's second line from the facts, and the range the owner ruled", () => {
    // The long form, `19 Aug → 20 Aug`, not fRI1's prose `19 – 20 Aug`: the
    // owner ruled the long one on 2026-09-20, eight days before that frame
    // landed. Asserted here so the divergence cannot be closed in either
    // direction without meeting the ruling.
    const line = subjectOf(plan(), property);

    expect(line).toContain("Thomas George");
    expect(line).toContain("BK-4361");
    expect(line).toContain("→");
    expect(line).toContain("recorded as a no-show");
    expect(line).not.toContain("– 20 Aug");
  });

  it("omits the part a plan does not carry rather than drawing a dash", () => {
    // A reinstatement has no room and no departure; a departure correction has
    // no no-show time. Each arm's line carries only what it has.
    expect(subjectOf(plan(), property)).not.toContain("checked out");
    expect(subjectOf(plan(departure), property)).not.toContain("no-show");
  });

  it("withholds the write where no reason is configured, and says which", () => {
    // Nothing configures the vocabulary yet — the same recorded gap frame 8's
    // cancellation has. The primary is drawn off rather than sending a
    // correction the service refuses.
    const root = drawn({ reasons: [] });
    const primary = [...root.querySelectorAll("button")].at(-1) as HTMLButtonElement;

    expect(primary.disabled).toBe(true);
    expect(primary.title).toContain("reason");
    expect(confirmed).toEqual([]);
  });

  it("returns null for a target nobody drew, rather than composing one", () => {
    // **A deliberate limit, asserted so nobody widens it quietly.** Two arms
    // are drawn; a third would be an application design the owner has not
    // seen, and it would read as approved to whoever found it next.
    const made = plan({ to: "Cancelled", from: "Booked" });

    expect(correct(made, subjectOf(made, property), () => {}, () => {})).toBeNull();
  });

  it("sends the target and the reason the plan named", () => {
    const root = drawn();
    ([...root.querySelectorAll("button")].at(-1) as HTMLButtonElement).click();

    expect(confirmed).toEqual([
      { to: "Booked", reason: "Guest did arrive — the night desk missed them" },
    ]);
  });
});

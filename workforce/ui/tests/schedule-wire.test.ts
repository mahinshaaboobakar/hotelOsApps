import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import type { Schedule } from "../roster/schedule";
import { schedule } from "../screens/schedule";

/**
 * My Schedule, against what `ScheduleView.Month` actually sends.
 *
 * The service sends `balance: null` on every call — *"the balance sentence
 * belongs to Leave and is read there"*. The screen typed `balance` as a string
 * and split it, so on a real property it threw before drawing a day (found
 * during the U1 sweep, 2026-09-19). This month is the wire's shape exactly.
 *
 * **Typed, so it cannot quietly stop being that shape.** It was a bare object
 * literal, and when the service grew `on`, `today` and `dutyPart` this fixture
 * kept the old shape and the suite stayed green — a fixture-versus-wire hole
 * inside the file written to close one.
 */
const WIRE: Schedule = {
  who: "Irfan Qadri",
  initials: "IQ",
  month: "2026-10-01",
  shifts: 3,
  leaveDays: 0,
  duty: 1,
  dutyFrom: "2026-10-02T14:30:00.000Z",
  dutyTo: "2026-10-03T02:30:00.000Z",
  balance: null,

  // The 3rd, which is the day the duty's tail falls on — so the marked cell
  // and the tail are different cells and one cannot stand in for the other.
  today: "2026-10-03",
  days: [
    { date: 29, on: "2026-09-29", mark: null, tone: null,
      dutyFrom: null, dutyTo: null, dutyPart: null },
    { date: 30, on: "2026-09-30", mark: null, tone: null,
      dutyFrom: null, dutyTo: null, dutyPart: null },
    { date: 1, on: "2026-10-01", mark: "M", tone: "brand",
      dutyFrom: null, dutyTo: null, dutyPart: null },
    { date: 2, on: "2026-10-02", mark: "A", tone: "ok",
      dutyFrom: "2026-10-02T14:30:00.000Z", dutyTo: "2026-10-03T02:30:00.000Z",
      dutyPart: "starts" },
    // The morning the person is still holding it — 20:00 → 08:00 at +05:30.
    { date: 3, on: "2026-10-03", mark: "OFF", tone: "neutral",
      dutyFrom: "2026-10-02T14:30:00.000Z", dutyTo: "2026-10-03T02:30:00.000Z",
      dutyPart: "tail" },
  ],
};

function host(month: Schedule = WIRE): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (_capability: string, method: string) => method === "schedule"
      ? Promise.resolve(month)
      : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" })),
    on: () => () => {},
  };
}

const PERSON = {
  staffId: "a3f1c064-5d21-4e8b-9f02-1a7c6b40d911",
  name: "Irfan Qadri", department: "Front Office", role: "Receptionist", property: "",
};

describe("My Schedule against the wire", () => {
  it("draws the month when the service sends no balance, which is every time", async () => {
    const main = document.createElement("div");
    await schedule(host(), main, { ok: true, value: PERSON });

    expect(main.querySelector(".fail")).toBeNull();
    expect(main.querySelectorAll(".cday").length).toBe(WIRE.days.length);

    // Absent, not invented: nothing stands where a balance would be.
    expect(main.querySelector(".mpush")).toBeNull();
  });

  it("draws a duty badge on the duty's own day and on no day the wire sends null for", async () => {
    // The wire sends `dutyFrom: null` on an ordinary day, not an absent field,
    // and the cell tested `!== undefined` — so every ordinary day reached the
    // formatter with null.
    const main = document.createElement("div");
    await schedule(host(), main, { ok: true, value: PERSON });

    // Two cells, because the duty crosses midnight — `64g` §5. The second names
    // only the hour it ends: it is the morning the person is still holding the
    // first duty, not a second one.
    expect(Array.from(main.querySelectorAll(".cday .cduty"), (one) => one.textContent))
      .toEqual(["MOD 20:00→08:00", "MOD →08:00"]);

    expect(main.querySelectorAll(".cduty.tail")).toHaveLength(1);
  });

  it("marks the property's operating day, and only that cell", async () => {
    const main = document.createElement("div");
    await schedule(host(), main, { ok: true, value: PERSON });

    const marked = Array.from(main.querySelectorAll(".cday.today s"), (one) => one.textContent);

    // The day the service named — not the day the machine drawing this is on,
    // which is what any local `new Date()` would have answered (ADR 0211).
    expect(marked).toEqual(["3"]);
  });

  it("marks no day at all where the operating day could not be read", async () => {
    // Absent is an answer. A grid that marked today anyway would be asserting a
    // day nobody established — and `undefined === undefined` across a missing
    // field would have marked every cell in the month.
    const main = document.createElement("div");
    await schedule(host({ ...WIRE, today: null }), main, { ok: true, value: PERSON });

    expect(main.querySelectorAll(".cday.today")).toHaveLength(0);
    // Positive control: the grid was drawn, so zero marked cells is a finding
    // about the marking rather than about an empty month.
    expect(main.querySelectorAll(".cday").length).toBe(WIRE.days.length);
  });
});

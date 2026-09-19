import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { schedule } from "../screens/schedule";

/**
 * My Schedule, against what `ScheduleView.Month` actually sends.
 *
 * The service sends `balance: null` on every call — *"the balance sentence
 * belongs to Leave and is read there"* — and no `tail` on any day. The screen
 * typed `balance` as a string and split it, and drew a tail only the fixture
 * carried, so on a real property the screen threw before drawing a day (found
 * during the U1 sweep, 2026-09-19). This month is the wire's shape exactly.
 */
const WIRE = {
  who: "Irfan Qadri",
  initials: "IQ",
  month: "2026-10-01",
  shifts: 3,
  leaveDays: 0,
  duty: 1,
  dutyFrom: "2026-10-02T14:30:00.000Z",
  dutyTo: "2026-10-03T02:30:00.000Z",
  balance: null,
  days: [
    { date: 29, mark: null, tone: null, duty: null },
    { date: 30, mark: null, tone: null, duty: null },
    { date: 1, mark: "M", tone: "brand", dutyFrom: null, dutyTo: null },
    { date: 2, mark: "A", tone: "ok",
      dutyFrom: "2026-10-02T14:30:00.000Z", dutyTo: "2026-10-03T02:30:00.000Z" },
    { date: 3, mark: "OFF", tone: "neutral", dutyFrom: null, dutyTo: null },
  ],
};

function host(): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (_capability: string, method: string) => method === "schedule"
      ? Promise.resolve(WIRE)
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

    expect(Array.from(main.querySelectorAll(".cday .cduty"), (one) => one.textContent))
      .toEqual(["MOD 20:00→08:00"]);
  });
});

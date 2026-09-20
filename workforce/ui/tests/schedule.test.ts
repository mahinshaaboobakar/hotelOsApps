import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { schedule } from "../screens/schedule";

/**
 * The one read on this module that could not succeed, and the two states around
 * it.
 *
 * `ScheduleView.Month` opens `call.Id("staffId")` — required, and absent is an
 * `InvalidRequestException` rather than a default. This screen sent **no body
 * at all**, so against a real backend it could only ever answer *`'staffId'` is
 * required*, which a person met as *Workforce could not build this person's
 * month* three components from the omission.
 *
 * Nothing in the module could see it. The harness answers from a fixture and
 * never reaches the view that requires the field, and the backend's own test
 * passes a `staffId` — written from the same reading as the view, so it asserts
 * the call the screen does not make. What follows asserts the call it does.
 */

/** A host that records what it was asked, and answers a month. */
function host(): { api: HostApi; asked: { method: string; params: unknown }[] } {
  const asked: { method: string; params: unknown }[] = [];

  return {
    asked,
    api: {
      identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
      property: { timezone: "Asia/Kolkata", locale: "en-IN" },
      call: (_capability: string, method: string, params?: unknown) => {
        asked.push({ method, params });

        return method === "schedule"
          // Built from `Schedule`'s own declaration rather than from what the
          // screen appeared to need: a stub written from a reading shares the
          // reading's gaps, and the first version of this one was three fields
          // short in exactly the places the screen does not guard.
          ? Promise.resolve({
            who: "Anjali Menon", initials: "AM", month: "August 2026",
            shifts: 18, leaveDays: 2, duty: 1,
            dutyFrom: null, dutyTo: null,
            balance: "4 of 8 casual remaining",
            days: [],
          })
          : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" }));
      },
      on: () => () => {},
    },
  };
}

const PERSON = {
  staffId: "a3f1c064-5d21-4e8b-9f02-1a7c6b40d911",
  name: "Anjali Menon", department: "Front Office",
  role: "Receptionist", property: "Kochi Beach Resort",
};

describe("staff schedule", () => {
  it("names the person its own read requires", async () => {
    const main = document.createElement("div");
    const { api, asked } = host();

    await schedule(api, main, { ok: true, value: PERSON });

    // The assertion is the PARAMS, not the render. A screen that drew a month
    // while sending no id would pass any test written about what it shows.
    expect(asked).toEqual([
      { method: "schedule", params: { staffId: PERSON.staffId } },
    ]);
  });

  it("asks nothing at all while the operator read is still in flight", async () => {
    const main = document.createElement("div");
    const { api, asked } = host();

    await schedule(api, main, null);

    // Not an empty screen and not a failure: both would be contradicted a
    // moment later when `me` lands and the module redraws.
    expect(asked).toEqual([]);
    expect(main.childElementCount).toBe(0);
  });

  it("says there is no staff record rather than asking with nothing", async () => {
    const main = document.createElement("div");
    const { api, asked } = host();

    await schedule(api, main, { ok: true, value: { ...PERSON, staffId: null } });

    expect(asked).toEqual([]);

    // A question this bundle never asked is not a platform failure, so there
    // are no facts to quote and none are drawn. Asserting the ABSENCE is the
    // point: a failure surface here would report the platform for something it
    // was never given the chance to do.
    expect(main.querySelector(".fail-facts")).toBeNull();
    expect(main.querySelector(".fail-said")?.textContent)
      .toBe("There is no staff record for the signed-in account");
  });

  it("reports the operator read's own failure, not a second one of its own", async () => {
    const main = document.createElement("div");
    const { api } = host();

    await schedule(api, main, {
      ok: false,
      failure: {
        cause: "forbidden", capability: "roster.read", method: "me",
        said: null, at: new Date("2026-09-15T04:25:00.000Z"),
      },
    });

    // The failure that happened, carried whole — this screen's inability to
    // ask is a consequence of that refusal, not a separate thing that went
    // wrong.
    //
    // Asserted on the VALUE of the "Asked for" row rather than on the body's
    // whole text: a substring search across the block would also match the
    // sentence above it, and would keep passing if the facts stopped rendering.
    //
    // **This asserted `roster.read · me`, and the point of naming the method
    // was that the facts said `me` rather than `schedule`.** The owner's `64g`
    // §2 B ruling takes the code name off the card, so the facts now carry the
    // screen's own words and **that distinction is no longer visible to
    // anybody** — it survives only on `wire`, which a refusal card renders
    // nowhere and offers no control to copy. Reported as part of the same gap;
    // recorded here rather than quietly replaced, because the old assertion is
    // what shows the change was deliberate (ADR 0034).
    const asked_for = Array.from(main.querySelectorAll(".fail-fact"))
      .find((row) => row.querySelector(".fail-fk")?.textContent === "Asked for");

    expect(asked_for?.querySelector(".fail-fv")?.textContent).toBe("this person's month");
  });
});

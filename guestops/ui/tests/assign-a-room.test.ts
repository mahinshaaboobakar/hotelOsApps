/**
 * C3 — giving a stay a room, gold frame 3's *Move room*.
 *
 * **The conflict is the behaviour worth holding.** GUEST-Q5 made a
 * double-booked room a possible truth, so the service refuses the first
 * attempt, names what is in the way, and accepts the same call again. A screen
 * that drew that as an error would leave the desk with a refusal and nothing to
 * do about it — and a screen that sent `acceptConflict` on the first attempt
 * would agree to something nobody had been shown.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { beforeEach, describe, expect, it } from "vitest";

import { HostCallError, type HostApi } from "@hotelos/sdk";

import type { FreeRooms, StayPage } from "../book/model";
import { recordedStay } from "../book/recorded/stay";
import { assignRoom } from "../screens/assign";

interface Call {
  capability: string;
  method: string;
  body: Record<string, unknown>;
}

const asked: Call[] = [];

const rooms: FreeRooms = {
  rooms: [
    { id: "room-305", number: "305" },
    { id: "room-311", number: "311" },
  ],
  ofType: 6,
};

/** Answers for the assignment: the first is a conflict where one is asked for. */
function host(answers: unknown[], stay: StayPage = recordedStay): HostApi {
  const remaining = [...answers];

  return {
    identity: {
      id: "guestops",
      version: "0.1.0",
      capabilities: ["reservation.read", "stay.assign"],
    },
    property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    call: (capability: string, method: string, body: Record<string, unknown>) => {
      asked.push({ capability, method, body: body ?? {} });

      if (method === "stay") return Promise.resolve(stay);
      if (method === "rooms") return Promise.resolve(rooms);
      if (method === "assign") return Promise.resolve(remaining.shift());

      return Promise.reject(new Error(`no answer for ${method}`));
    },
    on: () => () => {},
  } as unknown as HostApi;
}

const took = { assigned: true, version: 8, moved: true };
const held = { assigned: false, conflict: true, version: 7, moved: true };

const stage = (): HTMLElement => document.createElement("div");
const settle = (): Promise<void> => new Promise((done) => setTimeout(done, 0));

const pick = async (root: HTMLElement, number: string): Promise<void> => {
  const box = root.querySelector<HTMLSelectElement>("select");
  if (box === null) throw new Error("no room chooser");
  box.value = number;
  box.dispatchEvent(new Event("change"));
  await settle();
};

const press = (root: HTMLElement, label: string): void => {
  const button = [...root.querySelectorAll("button")].find((b) => b.textContent === label);
  if (button === undefined) {
    throw new Error(`no control reading ${label} — offers: `
      + [...root.querySelectorAll("button")].map((b) => b.textContent).join(", "));
  }
  button.click();
};

const writes = () => asked.filter((call) => call.method === "assign");

describe("assigning a room", () => {
  beforeEach(() => { asked.length = 0; });

  it("reads the stay for its version rather than being handed one", async () => {
    const into = stage();
    await assignRoom(host([took]), into, "s1", () => {}, () => {});
    await pick(into, "305");

    press(into, "Move");
    await settle();

    // The version the stay was READ at. A screen that sent a version it was
    // given by whoever opened it could write against a stay it never saw.
    expect(writes()[0]?.body["version"]).toBe(recordedStay.version);
    expect(writes()[0]?.capability).toBe("stay.assign");
    expect(writes()[0]?.body["roomId"]).toBe("room-305");
  });

  it("never agrees to a conflict nobody has been shown", async () => {
    const into = stage();
    await assignRoom(host([held, took]), into, "s1", () => {}, () => {});
    await pick(into, "305");

    press(into, "Move");
    await settle();

    // The first attempt must not carry it. The desk has seen nothing yet.
    expect(writes()[0]?.body["acceptConflict"]).toBeUndefined();
  });

  it("draws a conflict as a question, keeps the room, and offers to mean it", async () => {
    const done: number[] = [];
    const into = stage();

    await assignRoom(host([held, took]), into, "s1", () => {}, () => done.push(1));
    await pick(into, "305");

    press(into, "Move");
    await settle();

    expect(into.textContent).toContain("305 is already held over these dates");
    expect(into.textContent).toContain("Assign anyway, or pick another room");

    // Nothing was recorded, and the sheet has not closed over it.
    expect(done).toEqual([]);

    // The second press is the SAME assignment with the desk's agreement —
    // the room is still chosen, so it is not re-picked.
    press(into, "Assign anyway");
    await settle();

    expect(writes()).toHaveLength(2);
    expect(writes()[1]?.body["acceptConflict"]).toBe(true);
    expect(writes()[1]?.body["roomId"]).toBe("room-305");
    expect(done).toEqual([1]);
  });

  it("says Assign for a stay with no room, and Move for one that has one", async () => {
    const waiting = { ...recordedStay, currentRoomId: null, room: null };

    const first = stage();
    await assignRoom(host([took], waiting), first, "s1", () => {}, () => {});
    expect([...first.querySelectorAll("button")].map((b) => b.textContent)).toContain("Assign");

    const moving = stage();
    await assignRoom(host([took]), moving, "s1", () => {}, () => {});
    expect([...moving.querySelectorAll("button")].map((b) => b.textContent)).toContain("Move");
  });

  it("sends no reason, because the service derives it", async () => {
    const into = stage();
    await assignRoom(host([took]), into, "s1", () => {}, () => {});
    await pick(into, "311");

    press(into, "Move");
    await settle();

    // A client that could send a reason could record a move as a first
    // assignment. The body has nowhere to put one.
    expect(Object.keys(writes()[0]?.body ?? {})).toEqual(["stayId", "roomId", "version"]);
  });

  it("cannot be pressed before a room is chosen", async () => {
    const into = stage();
    await assignRoom(host([took]), into, "s1", () => {}, () => {});

    const primary = [...into.querySelectorAll("button")].at(-1) as HTMLButtonElement;

    expect(primary.disabled).toBe(true);
    expect(primary.title).toBe("Choose a room first.");

    await pick(into, "305");

    expect(([...into.querySelectorAll("button")].at(-1) as HTMLButtonElement).disabled).toBe(false);
  });

  it("draws a real refusal as a refusal", async () => {
    const into = stage();
    const refusing = {
      ...host([took]),
      call: (_capability: string, method: string) => {
        if (method === "stay") return Promise.resolve(recordedStay);
        if (method === "rooms") return Promise.resolve(rooms);
        return Promise.reject(new HostCallError(
          { kind: "rejected", message: "somebody else changed this stay" }));
      },
    } as unknown as HostApi;

    await assignRoom(refusing, into, "s1", () => {}, () => {});
    await pick(into, "305");

    press(into, "Move");
    await settle();

    expect(into.textContent).toContain("somebody else changed this stay");

    // And not as a conflict, which is a different thing with a different remedy.
    expect(into.textContent).not.toContain("Assign anyway");
  });

  it("tells an unconfigured type from a full one", async () => {
    const none: FreeRooms = { rooms: [], ofType: 0 };
    const empty = {
      ...host([took]),
      call: (_capability: string, method: string) =>
        method === "stay" ? Promise.resolve(recordedStay) : Promise.resolve(none),
    } as unknown as HostApi;

    const into = stage();
    await assignRoom(empty, into, "s1", () => {}, () => {});

    expect(into.textContent).toContain("This property has no rooms of that type");
  });
});

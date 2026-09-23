/**
 * C2 — the walk-in, gold frame 10.
 *
 * **The sheet drew and captured nothing.** `stay.create/walkIn` was declared,
 * served and unreachable — and what kept it that way was not the write. Nothing
 * in this application could name a ROOM, and check-in needs one (S8), so the
 * sheet could be drawn and could not be completed by any caller.
 *
 * **The test that matters most is the partial outcome.** One press, two
 * authorized phases: a refused second phase LEAVES the stay (RC-Q8a), so
 * `checkedIn: false` is a successful call reporting what happened — not a
 * failure. A sheet that drew it as a failure would send a receptionist to
 * create a second stay for a guest already in the book.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { beforeEach, describe, expect, it } from "vitest";

import { HostCallError, type HostApi } from "@hotelos/sdk";

import type { FreeRooms } from "../book/model";
import { recordedAvailability, recordedFreeRooms } from "../book/recorded/availability";
import { walkIn } from "../screens/walkin";

interface Call {
  capability: string;
  method: string;
  body: Record<string, unknown>;
}

const asked: Call[] = [];
let answers: Record<string, unknown> = {};

/**
 * A host answering the three reads the sheet makes, and recording the write.
 *
 * **The business date is deliberately not today.** The sheet must take the
 * property's day from the service; one that used this machine's clock would
 * pass a test written on the same machine and put a stay on the wrong day for
 * a desk in another zone.
 */
function host(taken: unknown = { stayId: "s9", bookingId: "b9", checkedIn: true, secondStep: null }): HostApi {
  return {
    identity: {
      id: "guestops",
      version: "0.1.0",
      capabilities: ["reservation.read", "stay.create"],
    },
    property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    call: (capability: string, method: string, body: Record<string, unknown>) => {
      asked.push({ capability, method, body: body ?? {} });

      if (method in answers) return Promise.resolve(answers[method]);
      if (method === "today") return Promise.resolve({ businessDate: "2026-08-31" });
      if (method === "availability") return Promise.resolve(recordedAvailability);
      if (method === "rooms") return Promise.resolve(recordedFreeRooms);
      if (method === "walkIn") return Promise.resolve(taken);

      return Promise.reject(new Error(`no answer for ${method}`));
    },
    on: () => () => {},
  } as unknown as HostApi;
}

const stage = (): HTMLElement => document.createElement("div");
const settle = (): Promise<void> => new Promise((done) => setTimeout(done, 0));

const boxFor = (root: HTMLElement, label: string): HTMLInputElement | HTMLSelectElement => {
  const found = [...root.querySelectorAll(".fld")]
    .find((element) => element.querySelector("label")?.textContent === label);

  const box = found?.querySelector<HTMLInputElement>("input, select, textarea");
  if (box == null) throw new Error(`no box labelled ${label}`);
  return box;
};

const type = (root: HTMLElement, label: string, value: string): void => {
  const box = boxFor(root, label);
  box.value = value;
  box.dispatchEvent(new Event("input"));
};

const choose = async (root: HTMLElement, label: string, text: string): Promise<void> => {
  const box = boxFor(root, label) as HTMLSelectElement;
  box.value = text;
  box.dispatchEvent(new Event("change"));
  await settle();
};

const press = (root: HTMLElement, label: string): void => {
  const button = [...root.querySelectorAll("button")].find((b) => b.textContent === label);
  if (button === undefined) throw new Error(`no control reading ${label}`);
  button.click();
};

const primary = (root: HTMLElement): HTMLButtonElement => {
  const button = [...root.querySelectorAll("button")]
    .find((b) => b.textContent === "Create and check in");
  if (button === undefined) throw new Error("no primary action");
  return button as HTMLButtonElement;
};

/** Fill the sheet the way a desk does. */
async function complete(into: HTMLElement): Promise<void> {
  type(into, "Guest", "Joseph Mathew");
  await choose(into, "Room type", recordedAvailability.types[0]!.roomType);
  await choose(into, "Room", recordedFreeRooms.rooms[0]!.number);
}

describe("the walk-in", () => {
  beforeEach(() => {
    asked.length = 0;
    answers = {};
  });

  it("takes the arrival from the property's day, not this machine's clock", async () => {
    const into = stage();
    await walkIn(host(), into, () => {}, () => {});

    expect(boxFor(into, "Arrives").value).toBe("2026-08-31");
    expect(boxFor(into, "Departs").value).toBe("2026-09-01");
  });

  it("cannot be pressed until the service's required fields are all present", async () => {
    const into = stage();
    await walkIn(host(), into, () => {}, () => {});

    // A name, a type, a room and both dates. The dates arrive filled; the rest
    // do not, so the button starts off.
    expect(primary(into).disabled).toBe(true);

    await complete(into);

    expect(primary(into).disabled).toBe(false);
  });

  it("sends the room it was given, which is what check-in needs", async () => {
    const into = stage();
    await walkIn(host(), into, () => {}, () => {});
    await complete(into);

    press(into, "Create and check in");
    await settle();

    const write = asked.find((call) => call.method === "walkIn");

    expect(write?.capability).toBe("stay.create");
    expect(write?.body["guest"]).toBe("Joseph Mathew");
    expect(write?.body["roomId"]).toBe(recordedFreeRooms.rooms[0]!.id);
    expect(write?.body["roomTypeId"]).toBe(recordedAvailability.types[0]!.roomTypeId);
    expect(write?.body["arrives"]).toBe("2026-08-31");
  });

  it("opens the stay when the guest is in house", async () => {
    const opened: string[] = [];
    const into = stage();

    await walkIn(host(), into, () => {}, (stayId) => opened.push(stayId));
    await complete(into);

    press(into, "Create and check in");
    await settle();

    expect(opened).toEqual(["s9"]);
  });

  it("says the stay EXISTS when only the second phase was refused", async () => {
    const opened: string[] = [];
    const into = stage();

    const partial = { stayId: "s9", bookingId: "b9", checkedIn: false, secondStep: "not-authorized" };
    await walkIn(host(partial), into, () => {}, (stayId) => opened.push(stayId));
    await complete(into);

    press(into, "Create and check in");
    await settle();

    // **Not a failure.** The write succeeded and the stay is in the book; only
    // the room and the arrival were refused. A desk told "nothing happened"
    // creates a second stay for the same guest.
    expect(into.textContent).toContain("The stay was created");
    expect(into.textContent).toContain("this desk may not give a room");

    // And the sheet stays where it is rather than opening a stay that has no
    // room — the day's list is where it is picked up.
    expect(opened).toEqual([]);
  });

  it("draws a refusal as a refusal, with nothing created", async () => {
    const into = stage();
    const refusing = {
      ...host(),
      call: (_capability: string, method: string) => {
        if (method === "walkIn") {
          return Promise.reject(new HostCallError(
            { kind: "rejected", message: "the departure is before the arrival" }));
        }
        if (method === "today") return Promise.resolve({ businessDate: "2026-08-31" });
        if (method === "availability") return Promise.resolve(recordedAvailability);
        if (method === "rooms") return Promise.resolve(recordedFreeRooms);
        return Promise.reject(new Error(`no answer for ${method}`));
      },
    } as unknown as HostApi;

    await walkIn(refusing, into, () => {}, () => {});
    await complete(into);

    press(into, "Create and check in");
    await settle();

    expect(into.textContent).toContain("the departure is before the arrival");
    expect(into.textContent).not.toContain("The stay was created");
  });

  it("tells an unconfigured property from a full one", async () => {
    // **Two empties with opposite remedies.** A bare "no rooms" would send
    // somebody to change the type when the property has none of it at all.
    const none: FreeRooms = { rooms: [], ofType: 0 };
    answers = { rooms: none };

    const into = stage();
    await walkIn(host(), into, () => {}, () => {});
    type(into, "Guest", "Joseph Mathew");
    await choose(into, "Room type", recordedAvailability.types[0]!.roomType);

    expect(into.textContent).toContain("This property has no rooms of that type");

    answers = { rooms: { rooms: [], ofType: 6 } satisfies FreeRooms };

    const other = stage();
    await walkIn(host(), other, () => {}, () => {});
    type(other, "Guest", "Joseph Mathew");
    await choose(other, "Room type", recordedAvailability.types[0]!.roomType);

    expect(other.textContent).toContain("Every room of that type is taken");
  });

  it("does not ask for rooms before a type is chosen", async () => {
    const into = stage();
    await walkIn(host(), into, () => {}, () => {});

    // A room is free FOR A TYPE over a range; asking without one would be
    // asking a question the service cannot answer.
    expect(asked.some((call) => call.method === "rooms")).toBe(false);

    await choose(into, "Room type", recordedAvailability.types[0]!.roomType);

    expect(asked.some((call) => call.method === "rooms")).toBe(true);
  });
});

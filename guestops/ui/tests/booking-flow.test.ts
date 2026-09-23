/**
 * The 05 flow, driven end to end — the frames the owner approved on
 * 2026-09-19, with ADR 0223's treatment C at the confirm.
 *
 * **This is the test the capacity warning was missing.** `capacity-warning`
 * asserts the confirm card in isolation, and a card nothing reaches is a drawn
 * screen with no door: until this flow existed, New booking read availability
 * with no dates and had nowhere to go from the answer. What this drives is the
 * path a person actually takes.
 *
 * The write is asserted by what reaches the host, because that is the contract
 * — the screen's own state proves nothing about what the service was asked.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { beforeEach, describe, expect, it } from "vitest";

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { recordedAvailability } from "../book/recorded/availability";
import { flow } from "../screens/newbooking/flow";

const asked: { capability: string; method: string; body: Record<string, unknown> }[] = [];
let created: string[] = [];

/** A host that answers availability and records every write. */
function host(): HostApi {
  return {
    identity: {
      id: "guestops",
      version: "0.1.0",
      capabilities: ["reservation.read", "stay.create"],
    },
    property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    call: (capability: string, method: string, body: Record<string, unknown>) => {
      asked.push({ capability, method, body: body ?? {} });

      if (method === "availability") return Promise.resolve(recordedAvailability);
      if (method === "book") return Promise.resolve({ created: true, bookingId: "b9" });

      return Promise.reject(new Error(`no answer for ${method}`));
    },
    on: () => () => {},
  } as unknown as HostApi;
}

const stage = (): HTMLElement => document.createElement("div");

const click = (root: HTMLElement, label: string | RegExp): void => {
  const button = [...root.querySelectorAll("button")]
    .find((b) => (typeof label === "string" ? b.textContent === label : label.test(b.textContent ?? "")));

  if (button === undefined) {
    throw new Error(`no control reading ${String(label)} — the flow offers: `
      + [...root.querySelectorAll("button")].map((b) => b.textContent).join(", "));
  }

  button.click();
};

const type = (root: HTMLElement, selector: string, value: string): void => {
  const input = root.querySelector<HTMLInputElement>(selector);
  if (input === null) throw new Error(`no ${selector} on this step`);
  input.value = value;
};

const settle = (): Promise<void> => new Promise((done) => setTimeout(done, 0));

describe("the booking flow", () => {
  beforeEach(() => {
    asked.length = 0;
    created = [];
  });

  it("asks for availability with the dates a person chose, not without them", async () => {
    const into = stage();
    await flow(host(), into, (id) => created.push(id));

    // Nothing is read until somebody says when — the defect this flow fixes.
    expect(asked).toHaveLength(0);

    type(into, 'input[type="date"]', "2026-09-03");
    click(into, "Check availability");
    await settle();

    expect(asked[0]?.method).toBe("availability");
    expect(asked[0]?.body["arrive"]).toBe("2026-09-03");
  });

  it("carries a chosen type, a guest and the party into the write", async () => {
    const into = stage();
    await flow(host(), into, (id) => created.push(id));

    click(into, "Check availability");
    await settle();

    click(into, "Choose");                       // step 2 — the first type with rooms free
    type(into, 'input[type="text"]', "Fatima Sheikh");
    click(into, "Review booking");               // step 3
    click(into, "Create booking");               // step 4
    await settle();

    const write = asked.find((call) => call.method === "book");

    expect(write?.capability).toBe("stay.create");
    expect(write?.body["guest"]).toBe("Fatima Sheikh");
    expect(write?.body["roomTypeId"]).toBe(recordedAvailability.types[0]?.roomTypeId);
    expect(write?.body["adults"]).toBe(2);

    // The booking is opened by its id, from the service's answer.
    expect(created).toEqual(["b9"]);
  });

  it("warns at the confirm where the party exceeds the type, and books anyway", async () => {
    const into = stage();
    await flow(host(), into, (id) => created.push(id));

    // **Four, and the number is the point.** The fixture's first type sleeps
    // two and takes a THIRD on an extra bed, so a party of three does not
    // exceed it — this test first asked for three and passed the warning by,
    // proving nothing. The fixture must be able to tell the two rules apart.
    type(into, 'input[type="number"]', "4");
    click(into, "Check availability");
    await settle();

    click(into, "Choose");
    type(into, 'input[type="text"]', "Fatima Sheikh");
    click(into, "Review booking");

    expect(into.querySelector(".note.warn")?.textContent).toMatch(/It can still be booked\.$/);

    click(into, "Create booking");
    await settle();

    expect(asked.some((call) => call.method === "book")).toBe(true);
  });

  it("draws the failure rather than an empty table when the read refuses", async () => {
    // **A `HostCallError`, because that is what the bridge throws.** Rejecting
    // with a bare Error made this test assert the flow's behaviour on a
    // condition the platform cannot produce — and the flow rightly let it
    // escape rather than drawing a failure card for a programming fault.
    const refusing = {
      ...host(),
      call: () => Promise.reject(
        new HostCallError({ kind: "unavailable", message: "the service did not answer" })),
    } as unknown as HostApi;

    const into = stage();
    await flow(refusing, into, (id) => created.push(id));

    click(into, "Check availability");
    await settle();

    expect(into.querySelector(".fail")).not.toBeNull();
    expect(into.querySelectorAll(".tr.list")).toHaveLength(0);
  });
});

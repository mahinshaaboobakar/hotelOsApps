/**
 * C4 — what frame 3's header actions do, and what they still do not.
 *
 * **The ledger's claim is that these are live, so this is where that claim is
 * checked.** A row saying BUILT is a sentence; a control that calls its
 * callback is the evidence.
 *
 * **Two of the four the service can do are deliberately absent.**
 * `RecordNoShowAsync` and `CorrectAsync` are built and tested, and no approved
 * frame draws either — "No-show" appears once in the gold, as a STATE on a
 * bookings row, and "Correct" as no affordance at all. Building the buttons
 * would be richer than the design, so the surface maps neither and this file
 * asserts that the header offers neither.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { beforeEach, describe, expect, it } from "vitest";

import type { HostApi } from "@hotelos/sdk";

import type { StayPage } from "../book/model";
import { recordedStay } from "../book/recorded/stay";
import { recordedRequests, recordedServicing } from "../book/recorded/tabs";
import { stay as stayScreen, type Acts } from "../screens/stay";

const pressed: string[] = [];

const acts: Acts = {
  register: () => pressed.push("register"),
  assign: () => pressed.push("assign"),
  checkOut: () => pressed.push("checkOut"),
  cancel: () => pressed.push("cancel"),
};

function host(page: StayPage): HostApi {
  return {
    identity: {
      id: "guestops",
      version: "0.1.0",
      capabilities: ["reservation.read", "stay.override", "stay.assign"],
    },
    property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    call: (_capability: string, method: string) => {
      if (method === "stay") return Promise.resolve(page);
      if (method === "requests") return Promise.resolve(recordedRequests);
      if (method === "servicing") return Promise.resolve(recordedServicing);
      return Promise.reject(new Error(`no answer for ${method}`));
    },
    on: () => () => {},
  } as unknown as HostApi;
}

/** Frame 3's stay: in house, so it offers Check out and Move room. */
const inHouse: StayPage = {
  ...recordedStay,
  actions: [
    { label: "Check out", danger: false },
    { label: "Move room", danger: false },
  ],
};

/** A booked stay: Check in, and the cancellation. */
const booked: StayPage = {
  ...recordedStay,
  actions: [
    { label: "Check in", danger: false },
    { label: "Cancel", danger: true },
  ],
};

async function drawn(page: StayPage): Promise<HTMLElement> {
  const into = document.createElement("div");
  await stayScreen(host(page), into, "s1", "Overview", () => {}, acts);
  return into;
}

const button = (root: HTMLElement, label: string): HTMLButtonElement => {
  const found = [...root.querySelectorAll("button")].find((b) => b.textContent === label);
  if (found === undefined) {
    throw new Error(`no control reading ${label} — offers: `
      + [...root.querySelectorAll("button")].map((b) => b.textContent).join(", "));
  }
  return found as HTMLButtonElement;
};

const labels = (root: HTMLElement): string[] =>
  [...root.querySelectorAll("button")].map((b) => b.textContent ?? "");

describe("frame 3's header actions", () => {
  beforeEach(() => { pressed.length = 0; });

  it("records a departure directly, because the frame draws no ellipsis", async () => {
    const into = await drawn(inHouse);
    const out = button(into, "Check out");

    expect(out.disabled).toBe(false);
    out.click();

    expect(pressed).toEqual(["checkOut"]);
  });

  it("gives Cancel an ellipsis, because it opens frame 8's dialog", async () => {
    const into = await drawn(booked);

    // The frame's own convention: `Cancel…` opens something, `Check out` does
    // not. The label carries it so the desk knows before pressing.
    expect(labels(into)).toContain("Cancel…");

    button(into, "Cancel…").click();

    expect(pressed).toEqual(["cancel"]);
  });

  it("opens the registration card for a check-in, and the sheet for a move", async () => {
    const arriving = await drawn(booked);
    button(arriving, "Check in").click();

    const here = await drawn(inHouse);
    button(here, "Move room").click();

    expect(pressed).toEqual(["register", "assign"]);
  });

  it("offers no No-show and no Correct, because no frame draws them", async () => {
    const into = await drawn(booked);
    const offered = labels(into);

    // The service can do both — `RecordNoShowAsync` and `CorrectAsync` — and
    // inventing the control would be richer than the approved design. The
    // affordance is a question for the owner, drawn.
    expect(offered).not.toContain("No-show");
    expect(offered).not.toContain("Correct");
    expect(offered).not.toContain("Mark as no-show");
  });

  it("still draws an unmapped action off, with a reason", async () => {
    const odd: StayPage = {
      ...recordedStay,
      actions: [{ label: "Something nobody wired", danger: false }],
    };

    const into = await drawn(odd);
    const control = button(into, "Something nobody wired");

    // The rule that survives every one of these: a control GuestOps cannot
    // perform is disabled and carries its reason, never live and silent.
    expect(control.disabled).toBe(true);
    expect(control.title).not.toBe("");
  });
});

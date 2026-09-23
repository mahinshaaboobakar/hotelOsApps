/**
 * C5 — the registration card, gold frame 15.
 *
 * **Its `Save` drew and did nothing since the card existed.** The boxes
 * rendered values and captured none, so the primary action was `off` and the
 * ledger carried it as the oldest *looks live, does nothing* after the request
 * log. `registration.capture` now answers both a card and its save.
 *
 * **What these tests exist to hold is the caption, not the layout.** *The
 * fields are the design's proposal; which of them are required is the
 * property's setting* — so a mark on a label has to come off the wire, and
 * nothing here may decide it.
 *
 * **And the whole-card write.** The service writes every field on every save so
 * a desk can clear a mistyped number; the consequence is that this sheet must
 * post every box it was given. A save carrying only what somebody touched would
 * blank the rest, and no assertion about the box that was typed into would show
 * it — which is why the first test asserts about a box nobody touched.
 *
 * Tests live here rather than beside the source: ADR 0025.
 */

import { beforeEach, describe, expect, it } from "vitest";

import { HostCallError, type HostApi } from "@hotelos/sdk";

import type { RegistrationCard } from "../book/model";
import { recordedRegistration } from "../book/recorded/registration";
import { registrationCard } from "../screens/registration";

interface Call {
  capability: string;
  method: string;
  body: Record<string, unknown>;
}

const asked: Call[] = [];
let refuse: Record<string, HostCallError> = {};

/** A host that answers the card and records every write. */
function host(card: RegistrationCard = recordedRegistration): HostApi {
  return {
    identity: {
      id: "guestops",
      version: "0.1.0",
      capabilities: ["registration.capture", "stay.override"],
    },
    property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    call: (capability: string, method: string, body: Record<string, unknown>) => {
      asked.push({ capability, method, body: body ?? {} });

      const refused = refuse[method];
      if (refused !== undefined) return Promise.reject(refused);

      if (method === "card") return Promise.resolve(card);
      if (method === "capture") return Promise.resolve({ series: "GRC 2026/08/1152", missing: [] });
      if (method === "checkIn") return Promise.resolve({ version: 4, lifecycle: "InHouse" });

      return Promise.reject(new Error(`no answer for ${method}`));
    },
    on: () => () => {},
  } as unknown as HostApi;
}

const stage = (): HTMLElement => document.createElement("div");

const press = (root: HTMLElement, label: string): void => {
  const button = [...root.querySelectorAll("button")].find((b) => b.textContent === label);

  if (button === undefined) {
    throw new Error(`no control reading ${label} — the card offers: `
      + [...root.querySelectorAll("button")].map((b) => b.textContent).join(", "));
  }

  button.click();
};

const settle = (): Promise<void> => new Promise((done) => setTimeout(done, 0));

const boxFor = (root: HTMLElement, label: string): HTMLInputElement => {
  const field = [...root.querySelectorAll(".fld")]
    .find((element) => element.querySelector("label")?.textContent?.startsWith(label));

  const box = field?.querySelector<HTMLInputElement>("input, select, textarea");
  if (box == null) throw new Error(`no box labelled ${label}`);
  return box;
};

const sent = (): Record<string, string> =>
  (asked.find((call) => call.method === "capture")?.body["values"] ?? {}) as Record<string, string>;

describe("the registration card", () => {
  beforeEach(() => {
    asked.length = 0;
    refuse = {};
  });

  it("sends every box back, including the ones nobody touched", async () => {
    const into = stage();
    await registrationCard(host(), into, "s1", () => {}, () => {});

    boxFor(into, "Arriving from").value = "Abu Dhabi";
    boxFor(into, "Arriving from").dispatchEvent(new Event("input"));

    press(into, "Save and check in");
    await settle();

    const values = sent();

    expect(values["arriving_from"]).toBe("Abu Dhabi");

    // **The box nobody touched.** The service writes every field on every save,
    // so a card that posted only what changed would blank this.
    expect(values["proceeding_to"]).toBe("Bengaluru");

    // And the two rows this screen cannot capture travel too, or every save
    // would clear the scans it never showed a control for.
    expect(values["documents"]).toBe("Passport page · visa page");
  });

  it("shows a document number masked and sends it whole", async () => {
    const into = stage();
    await registrationCard(host(), into, "s1", () => {}, () => {});

    // At rest the card reads as frame 15 draws it.
    expect(boxFor(into, "Number").value).toBe("P•••••4412");

    press(into, "Save and check in");
    await settle();

    // **Whole, not the mask.** A card that posted what the box displayed would
    // overwrite a passport number with dots on every save of an untouched card.
    expect(sent()["id_number"]).toBe("P12344412");
  });

  it("reveals the whole number when the desk goes to retype it", async () => {
    const into = stage();
    await registrationCard(host(), into, "s1", () => {}, () => {});

    const box = boxFor(into, "Number");
    box.dispatchEvent(new Event("focus"));

    expect(box.value).toBe("P12344412");
  });

  it("captures the card and then records the arrival, in that order", async () => {
    const into = stage();
    let done = 0;
    await registrationCard(host(), into, "s1", () => {}, () => { done += 1; });

    press(into, "Save and check in");
    await settle();

    const writes = asked.filter((call) => call.method !== "card");

    expect(writes.map((call) => `${call.capability}/${call.method}`))
      .toEqual(["registration.capture/capture", "stay.override/checkIn"]);

    // The version the card was read at, so a stay somebody else moved refuses.
    expect(writes[1]?.body["version"]).toBe(recordedRegistration.version);
    expect(done).toBe(1);
  });

  it("says the card was saved when only the check-in failed", async () => {
    refuse = {
      checkIn: new HostCallError({
        kind: "rejected",
        message: "this stay has no room; assign one before checking the guest in",
      }),
    };

    const into = stage();
    let done = 0;
    await registrationCard(host(), into, "s1", () => {}, () => { done += 1; });

    press(into, "Save and check in");
    await settle();

    // **An error naming the step that failed must not imply the steps before it
    // did not run.** A desk told only that the check-in failed would type the
    // whole card again.
    expect(into.textContent).toContain("The card was saved.");
    expect(into.textContent).toContain("assign one before checking the guest in");
    expect(done).toBe(0);
  });

  it("offers Save alone where there is no arrival to record", async () => {
    const into = stage();
    await registrationCard(
      host({ ...recordedRegistration, arriving: false }), into, "s1", () => {}, () => {});

    const labels = [...into.querySelectorAll("button")].map((b) => b.textContent);

    expect(labels).toContain("Save");
    expect(labels).not.toContain("Save and check in");

    press(into, "Save");
    await settle();

    // Nothing claims an arrival that happened hours ago.
    expect(asked.some((call) => call.method === "checkIn")).toBe(false);
  });

  it("marks what this property asks for and has not got, from the wire", async () => {
    const into = stage();

    await registrationCard(
      host({ ...recordedRegistration, missing: ["id_number"] }),
      into, "s1", () => {}, () => {});

    const marked = [...into.querySelectorAll("label")]
      .map((element) => element.textContent ?? "")
      .filter((text) => text.endsWith(" ·"));

    // **One mark, and it is the property's answer.** `name_as_on_id` is
    // required here too and is filled in, so a card that marked every required
    // box would show two — and one that marked from a list of its own would
    // show whatever that list said.
    expect(marked).toEqual(["Number ·"]);
  });

  it("offers only the documents this property accepts", async () => {
    const into = stage();
    await registrationCard(host(), into, "s1", () => {}, () => {});

    const chooser = boxFor(into, "Identity document") as unknown as HTMLSelectElement;

    // The blank is what lets a desk clear a chosen document; the rest is the
    // property's own list, and nothing is added to it.
    expect([...chooser.options].map((option) => option.value))
      .toEqual(["", "Passport", "Aadhaar", "Driving licence"]);
  });

  it("draws the failure rather than an empty card when the read refuses", async () => {
    refuse = {
      card: new HostCallError({ kind: "unavailable", message: "the service did not answer" }),
    };

    const into = stage();
    await registrationCard(host(), into, "s1", () => {}, () => {});

    expect(into.querySelector(".fail")).not.toBeNull();
    expect(into.querySelectorAll(".fld")).toHaveLength(0);
  });

  it("says what is still wanted without refusing to save", async () => {
    const into = stage();

    await registrationCard(
      host({ ...recordedRegistration, missing: ["id_number", "id_type"] }),
      into, "s1", () => {}, () => {});

    expect(into.textContent).toContain("This property asks for these, and they are blank.");

    press(into, "Save and check in");
    await settle();

    // S19b: a guest at the desk at midnight is served, and the card is
    // completed after.
    expect(asked.some((call) => call.method === "capture")).toBe(true);
  });
});

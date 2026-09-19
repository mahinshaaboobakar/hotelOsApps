import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { people } from "../screens/people";

/**
 * People's header and standing column, drawn from what the service counts.
 *
 * The header read *"n in Front Office on this page · n certifications
 * expiring"* — a department code written into the screen for every property,
 * and a certificate count taken from one page's rows and their tone, which
 * counts people, counts expired as expiring, and stops growing at the page
 * size. The service now sends the property's own count, and a row's standing as
 * a band and a number.
 */

const row = (id: string, who: string, standing: string, certificates: number, tone: string) => ({
  id, version: 1, who, since: "2024-02-02", postings: 1, departments: ["HK"],
  zone: null, role: "Room attendant", reportsTo: "—", standing, certificates, tone,
});

// One page of a larger list: the page holds ONE expiring row and one expired
// one, while the property has seven expiring certificates — so a count taken
// from the page cannot land on the right number by accident.
const WIRE = {
  postings: [
    row("p1", "Fatima Noor", "expiring", 2, "warn"),
    row("p2", "Irfan Qadri", "expired", 1, "bad"),
    row("p3", "Leena Das", "valid", 3, "ok"),
  ],
  paging: { page: 1, pageSize: 3, total: 40 },
  expiring: 7,
};

function host(): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (_capability: string, method: string) => method === "people"
      ? Promise.resolve(WIRE)
      : Promise.reject(new HostCallError({ kind: "unavailable", message: "not this test" })),
    on: () => () => {},
  };
}

describe("People's counts", () => {
  it("says the property's certificate count, and names no department of its own", async () => {
    const main = document.createElement("div");
    await people(host(), main, null, () => {}, () => {}, () => {}, 1);

    const sub = main.querySelector(".tools .hsub")?.textContent ?? "";
    expect(sub).toBe("40 posted · 7 certifications expiring");
  });

  it("does not say nobody is posted over an empty page of a list that has people", async () => {
    const empty = { ...WIRE, postings: [], paging: { page: 20, pageSize: 3, total: 40 } };
    const main = document.createElement("div");
    const answering: HostApi = { ...host(), call: () => Promise.resolve(empty) };
    await people(answering, main, null, () => {}, () => {}, () => {}, 20);

    expect(main.querySelector(".tools .hsub")?.textContent).toBe("40 posted · 7 certifications expiring");
  });

  it("writes each row's standing from its band and count", async () => {
    const main = document.createElement("div");
    await people(host(), main, null, () => {}, () => {}, () => {}, 1);

    // The row's own pill, not the department chips inside `.deps`.
    const pills = Array.from(main.querySelectorAll(".row:not(.hd) > .pill"), (one) => one.textContent);
    expect(pills).toEqual(["2 expiring", "1 expired", "3 valid"]);
  });
});

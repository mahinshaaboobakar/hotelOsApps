import { HostCallError, type HostApi } from "@hotelos/sdk";
import { beforeEach, describe, expect, it } from "vitest";

import { activate } from "../module";
import { recordedOvertime, recordedWeek } from "../roster";

/**
 * The Team Rota's rules — the ones the backend enforces, held still in the UI.
 *
 * These assert **structure and rules**, never layout or colour. What a suite
 * cannot see is exactly what the capture harness exists for, and neither
 * substitutes for the other.
 */

/** A host granting everything and answering from a fixture. */
function host(answer: unknown, granted: readonly string[] = ["roster.read"]): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: granted },
    call: () => Promise.resolve(answer),
    on: () => () => {},
  };
}

/** A host that is granted the capability and cannot answer. */
function failing(): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    // `unavailable` is not `isForPeople`, so the module shows its own words
    // rather than a platform diagnostic — ADR 0041, asked by the SDK.
    call: () => Promise.reject(
      new HostCallError({ kind: "unavailable", message: "no Workforce client" })),
    on: () => () => {},
  };
}

async function mount(api: HostApi): Promise<HTMLElement> {
  const root = document.createElement("div");
  activate(api).mount(root);
  await new Promise((resolve) => setTimeout(resolve, 0));
  return root;
}

describe("the Team Rota", () => {
  beforeEach(() => document.body.replaceChildren());

  it("derives its counts from the week rather than carrying them", async () => {
    const root = await mount(host(recordedWeek));
    const subtitle = root.querySelector(".hsub")?.textContent ?? "";

    // The FF precedent: a header that carried its own totals would eventually
    // disagree with the grid beneath it, and the header is what a manager reads
    // first. Spelled out here rather than recomputed from the fixture, so the
    // test is not the same arithmetic wearing a second hat.
    expect(subtitle).toContain("6 people");
    expect(subtitle).toContain("1 on leave");
    expect(subtitle).toContain("1 slot uncovered");
  });

  it("draws an uncovered slot as a named cell, not as blankness", async () => {
    const root = await mount(host(recordedWeek));

    // "Nobody is on" and "nobody has decided yet" are different answers, and the
    // header counts one of them. A manager must be able to find the cell the
    // count refers to without reading every one.
    expect(root.querySelector(".gap")?.textContent).toContain("cover?");
  });

  it("carries the overtime number, and disables nothing", async () => {
    const root = await mount(host(recordedOvertime));
    const text = root.textContent ?? "";

    // WF-Q14, warn-never-block. "Vishnu is over" tells a manager nothing they
    // can act on; the number tells them how much to move.
    expect(text).toContain("60.0");
    expect(text).toContain("48");
    expect(root.querySelectorAll("[disabled]")).toHaveLength(0);
  });

  it("says nothing about overtime when there is nothing to say", async () => {
    const root = await mount(host(recordedWeek));

    // An empty warning list is not "within the threshold" — it is silence, and a
    // panel that appeared saying "no overtime" would be a claim the data does
    // not support.
    expect(root.textContent ?? "").not.toContain("Overtime");
  });

  it("admits it when the data is not the property's own", async () => {
    const root = await mount(failing());

    // ADR 0124: the surface fails in place and names what it awaits. A person
    // must be able to tell whether they are looking at their hotel.
    expect(root.textContent ?? "").toContain("approved example week");
  });

  it("does not ask when the capability was not granted", async () => {
    let asked = false;
    const api: HostApi = {
      identity: { id: "workforce", version: "0.1.0", capabilities: [] },
      call: () => { asked = true; return Promise.resolve(recordedWeek); },
      on: () => () => {},
    };

    await mount(api);

    // A refusal for a permission a property chose not to give would read as an
    // outage. Not asking is both faster and honest.
    expect(asked).toBe(false);
  });

  it("names an unbuilt screen instead of showing an empty one", async () => {
    const root = await mount(host(recordedWeek));

    const reports = Array.from(root.querySelectorAll<HTMLElement>(".ri"))
      .find((item) => item.textContent?.includes("Reports") === true);

    reports?.click();
    await new Promise((resolve) => setTimeout(resolve, 0));

    expect(root.textContent ?? "").toContain("not built in this slice");
  });
});

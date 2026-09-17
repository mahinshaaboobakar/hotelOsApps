import { HostCallError, type HostApi } from "@hotelos/sdk";
import { beforeEach, describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedOvertime, recordedWeek } from "../roster/recorded";

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
    property: { timezone: "Asia/Kolkata", locale: null },
    call: () => Promise.resolve(answer),
    on: () => () => {},
  };
}

/** A host that is granted the capability and cannot answer. */
function failing(): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: ["roster.read"] },
    property: { timezone: "Asia/Kolkata", locale: null },
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
    //
    // **The whole sentence, not the figure inside it** — ADR 0034, rewritten
    // rather than deleted. This asserted `"60.0"`, which was the FIXTURE's
    // spelling: the service formatted `"0.#"`, so it would have sent `60` and
    // no test could have seen the difference. And a fragment assertion cannot
    // see a join — this sentence used to render "is planned 60 h planned hours
    // against 48", because the service composed two of its words and the
    // surface composed the rest.
    expect(text).toContain("Vishnu Das is planned 60 hours against 48.");
    expect(root.querySelectorAll("[disabled]")).toHaveLength(0);
  });

  it("says nothing about overtime when there is nothing to say", async () => {
    const root = await mount(host(recordedWeek));

    // An empty warning list is not "within the threshold" — it is silence, and a
    // panel that appeared saying "no overtime" would be a claim the data does
    // not support.
    expect(root.textContent ?? "").not.toContain("Overtime");
  });

  it("shows the failure in place of the week, and no week at all", async () => {
    const root = await mount(failing());
    const text = root.textContent ?? "";

    // This asserted "approved example week" - the stand-in banner that sat
    // under a recorded rota. APPS-Q26(4) rejected the mechanism, not the
    // wording: a screen that cannot read shows what failed, so there is no
    // longer an example to admit to.
    //
    // The two halves matter together. Naming the failure is worth nothing if
    // the fabricated week is still drawn behind it, and that is exactly what
    // the old shape did.
    expect(text).toContain("did not answer in time");
    // The provenance, which moved from a dotted line to labelled rows — the
    // run-on survives for the clipboard, where one line is the right shape.
    expect(root.querySelector(".fail-facts")).not.toBeNull();
    expect(root.querySelector(".rota")).toBeNull();
  });

  it("does not ask when the capability was not granted", async () => {
    let asked = false;
    const api: HostApi = {
      identity: { id: "workforce", version: "0.1.0", capabilities: [] },
      property: { timezone: "Asia/Kolkata", locale: null },
      call: () => { asked = true; return Promise.resolve(recordedWeek); },
      on: () => () => {},
    };

    await mount(api);

    // A refusal for a permission a property chose not to give would read as an
    // outage. Not asking is both faster and honest.
    expect(asked).toBe(false);
  });

});

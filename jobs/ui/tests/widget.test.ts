import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { recordedEscalated, recordedMine, recordedQuiet } from "../board/recorded/widget";
import { jobsNow } from "../widgets/panel/jobs-now";

const PROPERTY = { timezone: "Asia/Qatar", locale: "en-GB" };

function answering(answer: unknown, granted: readonly string[] = ["job.read"]): HostApi {
  return {
    identity: { id: "jobs", version: "0.1.0", capabilities: granted },
    property: PROPERTY,
    call: () => Promise.resolve(answer),
    on: () => () => {},
  };
}

function unavailable(): HostApi {
  return {
    identity: { id: "jobs", version: "0.1.0", capabilities: ["job.read"] },
    property: PROPERTY,
    call: () => Promise.reject(new HostCallError({ kind: "unavailable", message: "no Jobs client" })),
    on: () => () => {},
  };
}

describe("the jobs-now widget", () => {
  it("draws the quiet state without a worst row", async () => {
    const panel = await jobsNow(answering(recordedQuiet));
    expect(panel.textContent).toContain("ON TRACK");
    expect(panel.querySelectorAll(".wrow").length).toBe(0);
  });

  it("draws the escalated state with breached and stuck, worst first", async () => {
    const panel = await jobsNow(answering(recordedEscalated));
    expect(panel.textContent).toContain("breached · stuck");
    const rows = Array.from(panel.querySelectorAll(".wrow")).map((r) => r.textContent);
    expect(rows[0]).toContain("MRN-ENG-142");
  });

  it("draws the viewer's own jobs and their unread nudges", async () => {
    const panel = await jobsNow(answering(recordedMine));
    expect(panel.textContent).toContain("you · Arjun");
    expect(panel.textContent).toContain("1 unread");
  });

  it("stands in when the platform cannot answer, and never renders empty", async () => {
    const panel = await jobsNow(unavailable());
    expect(panel.textContent).toContain("open");
    expect(panel.querySelector(".whead")?.textContent).toContain("Jobs now");
  });

  it("does not call the platform for a capability it was not granted", async () => {
    let called = false;
    const panel = await jobsNow({
      identity: { id: "jobs", version: "0.1.0", capabilities: [] },
      property: PROPERTY,
      call: () => { called = true; return Promise.resolve(recordedQuiet); },
      on: () => () => {},
    });
    expect(called).toBe(false);
    expect(panel.textContent).toContain("Jobs now");
  });
});

import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { recordedEscalated, recordedMine, recordedQuiet } from "../board/recorded/widget";
import { recordedBlockedNow, recordedBoardNow } from "../board/recorded/widgets-two";
import { blocked } from "../widgets/panel/blocked";
import { jobsNow } from "../widgets/panel/jobs-now";
import { theBoard } from "../widgets/panel/the-board";

const PROPERTY = { timezone: "Asia/Qatar", locale: "en-GB" };

function answering(answer: unknown, granted: readonly string[] = ["job.read"]): HostApi {
  return {
    identity: { id: "jobs", version: "0.1.0", capabilities: granted },
    property: PROPERTY,
    call: () => Promise.resolve(answer),
    on: () => () => {},
  };
}

/** A host that answers both widget reads and records what was asked of it. */
function widgetHost(calls: { capability: string; method: string; params: unknown }[]): HostApi {
  return {
    identity: { id: "jobs", version: "0.1.0", capabilities: ["job.read"] },
    property: PROPERTY,
    call: (capability, method, params) => {
      calls.push({ capability, method, params });
      if (method === "widgetBoard") return Promise.resolve(recordedBoardNow);
      if (method === "widgetBlocked") return Promise.resolve(recordedBlockedNow);
      return Promise.resolve(null);
    },
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

describe("the two widgets built from the approved canvas", () => {
  it("The Board draws its four figures and the longest unclaimed, each opening a filtered board", async () => {
    const calls: { capability: string; method: string; params: unknown }[] = [];
    const host = widgetHost(calls);
    const card = await theBoard(host);

    expect(card.textContent).toContain("The Board");
    for (const label of ["new", "in progress", "on hold", "done"]) {
      expect(card.textContent, `the frame's ${label} figure`).toContain(label);
    }

    expect(card.textContent).toContain("Longest in NEW");
    expect(card.textContent).toContain("ASSIGNED, ACCEPTED, PAUSED and CANCELLED are counted in the app");

    const rows = Array.from(card.querySelectorAll<HTMLElement>(".wrow"));
    expect(rows).toHaveLength(3);
    rows[0]?.click();
    await new Promise((done) => setTimeout(done, 0));

    // The tap-through carries the filter: the screen opens on the question the
    // widget answered, not on an unfiltered board.
    const opened = calls.find((call) => call.capability === "shell.open");
    expect(String((opened?.params as { destination: string }).destination)).toContain("status=RAISED");
  });

  it("Blocked keeps its two states apart, because whose clock runs differs", async () => {
    const host = widgetHost([]);
    const card = await blocked(host);

    expect(card.textContent).toContain("Blocked");
    expect(card.textContent).toContain("On hold — the SLA clock is stopped");
    expect(card.textContent).toContain("Paused — the clock keeps running");
    expect(card.textContent).toContain("part on order");
    expect(card.textContent).toContain("assignee break");
  });
});

import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { recordedEscalated, recordedMine, recordedQuiet } from "../board/recorded/widget";
import { recordedBlockedNow, recordedBoardNow } from "../board/recorded/widgets-two";
import { recordedDueNow, recordedPriorityNow, recordedRaisedNow } from "../board/recorded/widgets-three";
import { byPriority } from "../widgets/panel/by-priority";
import { dueSoon } from "../widgets/panel/due-soon";
import { raisedToday } from "../widgets/panel/raised-today";
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
      if (method === "widgetPriority") return Promise.resolve(recordedPriorityNow);
      if (method === "widgetDue") return Promise.resolve(recordedDueNow);
      if (method === "widgetRaised") return Promise.resolve(recordedRaisedNow);
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

  it("says why it has no figures rather than showing figures it does not have", async () => {
    // **Rewritten, not deleted** (ADR 0034). The old contract was "stands in
    // and never renders empty" — and a widget that stands in is a widget
    // showing a hotel numbers that are not its own, which the owner ruled out
    // on 2026-09-09. It still never renders empty: it renders a REASON.
    const panel = await jobsNow(unavailable());

    // **Rewritten again for 0.4.1** (ADR 0034): this asserted `.gap`, the
    // SCREEN-size surface, inside a card — which is exactly what 0.4.0 shipped
    // and what 64b draws against. At widget size the frame has its own drawing.
    expect(panel.querySelector(".wfail"), "an unanswered read draws the widget-size failure").not.toBeNull();
    expect(panel.querySelector(".gap"), "and not the screen surface squeezed into a card").toBeNull();
    expect(panel.textContent).toContain("Jobs did not answer in time");
    expect(panel.textContent).not.toContain("escalated");

    // The frame's one divergence, approved: the facts move to the screen the
    // card opens, so none of them is on the card — and the only line that goes
    // anywhere is the one the seam says could work.
    expect(panel.querySelector("dl"), "no facts at widget size").toBeNull();
    expect(panel.querySelector(".wfail-open")?.textContent).toBe("Try again →");
  });

  it("does not call the platform for a capability it was not granted", async () => {
    let called = false;
    const panel = await jobsNow({
      identity: { id: "jobs", version: "0.1.0", capabilities: [] },
      property: PROPERTY,
      call: () => { called = true; return Promise.resolve(recordedQuiet); },
      on: () => () => {},
    });
    // The seam refuses without a round trip — asking for a capability nobody
    // granted is not worth one, and the answer is the platform's own: refused.
    expect(called).toBe(false);
    expect(panel.querySelector(".wfail")).not.toBeNull();
    expect(panel.textContent).toContain("You do not have access");

    // A refusal cannot be retried, so the card sends a person to the screen
    // that carries the facts — "Open Jobs →", never "Try again →".
    expect(panel.querySelector(".wfail-open")?.textContent).toBe("Open Jobs →");
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

describe("the three widgets whose frames were amended", () => {
  it("By Priority speaks this design's vocabulary and keeps NOT_TRIAGED out of P3", async () => {
    const card = await byPriority(widgetHost([]));

    // The amendment's whole point: the frame said emergency · high · normal ·
    // low, and those words are ruled away. Both halves are asserted, because a
    // caption that drifts back is exactly what happened to Due Soon.
    for (const figure of ["P1", "P2", "P3", "not triaged"]) {
      expect(card.textContent, `the frame's ${figure} figure`).toContain(figure);
    }
    // Word-bounded, because "low" is inside "flow" — which this widget draws as
    // a priority source. A substring check here failed on its own row and would
    // have been "fixed" by dropping the assertion.
    for (const gone of ["emergency", "normal", "low"]) {
      expect(card.textContent ?? "", `the ruled-away word ${gone}`)
        .not.toMatch(new RegExp(`\b${gone}\b`, "i"));
    }

    expect(card.textContent).toContain("P1 and P2 — longest open first");
    expect(card.textContent).toContain("Not triaged is counted apart from P3");
  });

  it("Due Soon reads late first, and its headings sit on their own rows", async () => {
    const card = await dueSoon(widgetHost([]));
    const text = card.textContent ?? "";

    // The correction of 2026-09-06, guarded: the overdue heading must come
    // before the overdue rows and before the due-within-two-hours group, which
    // is precisely what the frame got wrong for a day.
    const overdue = text.indexOf("Overdue — furthest past due first");
    const late = text.indexOf("+12m");
    const soonHeading = text.indexOf("Due within two hours");
    const soon = text.indexOf("in 1h");

    expect(overdue).toBeGreaterThanOrEqual(0);
    expect(late).toBeGreaterThan(overdue);
    expect(soonHeading).toBeGreaterThan(late);
    expect(soon).toBeGreaterThan(soonHeading);

    // And the deadline-source split the model never carried stays gone.
    expect(text).not.toContain("guest flow");
    expect(text).toContain("One due_at per job");
  });

  it("Raised Today counts by category, and says categories where it means categories", async () => {
    const card = await raisedToday(widgetHost([]));

    expect(card.textContent).toContain("By category");
    expect(card.textContent).toContain("Air conditioning");
    expect(card.textContent).toContain("Everything else · 4 categories");

    // The intents the walkthrough removed must not reappear as labels.
    for (const intent of ["Fix", "Prepare", "Deliver", "Check"]) {
      expect(card.textContent, `the removed intent ${intent}`).not.toContain(`${intent} ·`);
    }
  });

  it("each of the three asks for its own read, and taps through filtered", async () => {
    const calls: { capability: string; method: string; params: unknown }[] = [];
    const host = widgetHost(calls);

    await byPriority(host);
    await dueSoon(host);
    await raisedToday(host);

    expect(calls.map((call) => call.method)).toEqual(["widgetPriority", "widgetDue", "widgetRaised"]);

    const card = await dueSoon(widgetHost(calls));
    const rows = Array.from(card.querySelectorAll<HTMLElement>(".wrow"));
    rows[0]?.click();
    await new Promise((done) => setTimeout(done, 0));

    const opened = calls.find((call) => call.capability === "shell.open");
    expect(String((opened?.params as { destination: string }).destination)).toContain("due=overdue");
  });
});

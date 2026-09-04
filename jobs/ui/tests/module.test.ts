import { HostCallError, type HostApi } from "@hotelos/sdk";
import { beforeEach, describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedBoard, recordedToday } from "../board/recorded/board";
import { recordedJob, recordedRatedJob } from "../board/recorded/job";
import { recordedLive, recordedScheduled } from "../board/recorded/live";
import { recordedCatalogue } from "../board/recorded/catalogue";
import { recordedSettings } from "../board/recorded/settings";

const ALL = ["job.read", "job.create", "job.assign", "job.complete", "job.cancel", "job.amend", "job.configure", "job.curate"];

const PROPERTY = { timezone: "Asia/Qatar", locale: "en-GB" };

function host(granted: readonly string[] = ALL, answers: Record<string, unknown> = live()): HostApi {
  return {
    identity: { id: "jobs", version: "0.1.0", capabilities: granted },
    property: PROPERTY,
    call: (capability, method) => {
      const answer = answers[method];
      return answer === undefined
        ? Promise.reject(new HostCallError({ kind: "unavailable", message: `no answer for ${capability}/${method}` }))
        : Promise.resolve(answer);
    },
    on: () => () => {},
  };
}

function live(): Record<string, unknown> {
  return {
    today: recordedToday, board: recordedBoard, job: recordedJob, live: recordedLive,
    scheduled: recordedScheduled, catalogue: recordedCatalogue, settings: recordedSettings,
  };
}

async function settle(): Promise<void> {
  await new Promise((done) => setTimeout(done, 0));
  await new Promise((done) => setTimeout(done, 0));
}

function mount(h: HostApi): HTMLElement {
  const root = document.createElement("div");
  document.body.replaceChildren(root);
  activate(h).mount(root);
  return root;
}

function click(root: HTMLElement, selector: string, text: string): void {
  for (const node of Array.from(root.querySelectorAll<HTMLElement>(selector))) {
    if (node.textContent?.includes(text) === true) {
      node.click();
      return;
    }
  }
  throw new Error(`no ${selector} reading "${text}"`);
}

describe("the Jobs module", () => {
  let root: HTMLElement;

  beforeEach(() => {
    root = mount(host());
  });

  it("draws the five top tabs of the current chrome era", async () => {
    await settle();
    const tabs = Array.from(root.querySelectorAll(".head .tab")).map((t) => t.textContent);
    expect(tabs).toEqual(["Board", "Live", "Scheduled", "Catalogue", "Settings"]);
    expect(root.querySelector(".rail")).toBeNull();
  });

  it("draws the board's twelve rows, the strip and the pager", async () => {
    await settle();
    expect(root.querySelectorAll("tbody tr, table tr").length).toBe(recordedBoard.rows.length + 1);
    expect(recordedBoard.paging.total).toBe(47);
    expect(root.querySelector(".strip")?.textContent).toContain("11open");
    expect(root.querySelector(".pager")?.textContent).toContain("1–12 of 47");
  });

  it("shows dates with times, in the property's form, never a bare clock", async () => {
    await settle();
    const due = Array.from(root.querySelectorAll("table tr td")).map((td) => td.textContent ?? "");
    // en-GB through Intl: "02 Sept, 14:10" — a day, a month and a time, never a bare clock.
    const stamps = due.filter((text) => /^\d{2} \w{3,4},? \d{2}:\d{2}$/.test(text.trim()));
    expect(stamps.length).toBeGreaterThan(0);
    expect(due.some((text) => /^\d{2}:\d{2}$/.test(text.trim()))).toBe(false);
  });

  it("opens a job on its row and draws every tab of the record", async () => {
    await settle();
    click(root, "tr.pick td", "MRN-ENG-142");
    await settle();
    const tabs = Array.from(root.querySelectorAll(".subnav .tab")).map((t) => t.textContent?.split(" · ")[0]);
    expect(tabs).toEqual(["Overview", "Work", "History", "Notes & photos", "Links & steps", "Rating", "Record"]);
    // The service's figure, not the machine's clock — the frame's 00:23:41.
    expect(root.querySelector(".timer")?.textContent).toContain("00:23:41");
  });

  it("draws no work controls for a viewer who does not hold the job", async () => {
    const other = mount(host(ALL, { ...live(), job: { ...recordedJob, row: { ...recordedJob.row, viewerIsAssignee: false } } }));
    await settle();
    click(other, "tr.pick td", "MRN-ENG-142");
    await settle();
    expect(other.textContent).toContain("Resolve…");
    expect(other.textContent).not.toContain("Pause");
  });

  it("draws the sessions on the Work tab, the running one as running", async () => {
    await settle();
    click(root, "tr.pick td", "MRN-ENG-142");
    await settle();
    click(root, ".subnav .tab", "Work");
    await settle();
    expect(root.textContent).toContain("fetch gauge");
    const sessions = root.querySelectorAll("table")[0];
    expect(sessions?.querySelector(".pill.run")?.textContent).toBe("running");
  });

  it("shows the guest's rating only on a guest-raised job, once closed", async () => {
    const rated = mount(host(ALL, { ...live(), job: recordedRatedJob, board: recordedBoard }));
    await settle();
    click(rated, "tr.pick td", "MRN-HK-388");
    await settle();
    click(rated, ".subnav .tab", "Rating");
    await settle();
    expect(rated.querySelector(".stars")?.textContent).toBe("★★★★★");
    expect(rated.textContent).toContain("Towels came in six minutes");
  });

  it("draws the action row from the grants, and nothing when only read is held", async () => {
    await settle();
    click(root, "tr.pick td", "MRN-ENG-142");
    await settle();
    expect(root.textContent).toContain("Resolve…");
    expect(root.textContent).toContain("Reassign…");

    const reader = mount(host(["job.read"]));
    await settle();
    click(reader, "tr.pick td", "MRN-ENG-142");
    await settle();
    expect(reader.textContent).not.toContain("Resolve…");
    expect(reader.textContent).not.toContain("Reassign…");
    expect(reader.textContent).not.toContain("Raise a job");
  });

  it("says so when it is drawing the recorded example rather than the property's data", async () => {
    const offline = mount(host(ALL, {}));
    await settle();
    expect(offline.querySelector(".note")?.textContent).toContain("Showing the approved example board");
  });

  it("draws Live's presence, including the department that runs on the property clock", async () => {
    await settle();
    click(root, ".head .tab", "Live");
    await settle();
    expect(root.textContent).toContain("no presence");
    expect(root.textContent).toContain("more load as you scroll");
    expect(root.textContent).toContain("ON TRACK rows are not listed here");
  });

  it("draws Scheduled without a cycle column", async () => {
    await settle();
    click(root, ".head .tab", "Scheduled");
    await settle();
    const headers = Array.from(root.querySelectorAll("th")).map((h) => h.textContent);
    expect(headers).toEqual(["Scheduled for", "Job", "Where", "What", "Raised by", "Assigned to", "Due"]);
    expect(headers).not.toContain("Cycle");
  });

  it("walks the settings from the clock to the policies list to the ladder builder", async () => {
    await settle();
    click(root, ".head .tab", "Settings");
    await settle();
    expect(root.textContent).toContain("Numbering · MRN-ENG-…");
    expect(root.textContent).toContain("Manager at risk");

    click(root, ".btn", "All policies");
    await settle();
    expect(root.textContent).toContain("Water — 10 minutes");
    expect(root.textContent).toContain("AC leak — ceiling risk");

    click(root, ".btn", "＋ New policy");
    await settle();
    click(root, ".subnav .tab", "3 · The ladder");
    await settle();
    expect(root.textContent).toContain("Add a step to P1");
    expect(root.textContent).toContain("Property jobs manager");
  });

  it("shows Access as read-only, sourced outside Jobs", async () => {
    await settle();
    click(root, ".head .tab", "Settings");
    await settle();
    click(root, ".subnav .tab", "Access");
    await settle();
    expect(root.textContent).toContain("Workforce headship");
    expect(root.textContent).toContain("none of it is in Jobs' database");
  });
});

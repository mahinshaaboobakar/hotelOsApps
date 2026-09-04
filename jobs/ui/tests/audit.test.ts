import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedBoard, recordedToday } from "../board/recorded/board";
import { recordedCatalogue } from "../board/recorded/catalogue";
import { recordedJob, recordedRatedJob } from "../board/recorded/job";
import { recordedLive, recordedScheduled } from "../board/recorded/live";
import { recordedSettings } from "../board/recorded/settings";

/**
 * **The build against the drawing** — one test per locked frame, asserting the
 * facts that frame draws. This is the frame-beside-capture audit's content
 * half, mechanised: a photograph shows layout and colour, and this shows that
 * every row, label and figure the owner locked is actually on the screen. It
 * stays as a regression net, so a later change that quietly drops a column
 * fails here rather than at the next audit.
 *
 * Frames: `docs/mockups/01-the-jobs-screens.html` and `02-the-jobs-settings.html`,
 * owner-locked 2026-09-04.
 */

const ALL = ["job.read", "job.create", "job.assign", "job.complete", "job.cancel", "job.amend", "job.configure", "job.curate"];
const PROPERTY = { timezone: "Asia/Qatar", locale: "en-GB" };

function host(answers: Record<string, unknown> = live(), granted: readonly string[] = ALL): HostApi {
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

function mount(h: HostApi = host()): HTMLElement {
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

/** Drive to a job's tab, the way a person reaches it. */
async function jobTab(label: string, h: HostApi = host()): Promise<HTMLElement> {
  const root = mount(h);
  await settle();
  click(root, "tr.pick td", h === undefined ? "MRN-ENG-142" : "MRN-ENG-142");
  await settle();
  if (label !== "Overview") {
    click(root, ".subnav .tab", label);
    await settle();
  }

  return root;
}

/** Drive to a settings tab or view. */
async function settings(tab: string, view?: string): Promise<HTMLElement> {
  const root = mount();
  await settle();
  click(root, ".head .tab", "Settings");
  await settle();
  if (tab !== "Concern policy") {
    click(root, ".subnav .tab", tab);
    await settle();
  }

  // The flow is reached the way a person reaches it: the clock, then the list,
  // then New policy.
  if (view !== undefined) {
    click(root, ".btn", "All policies");
    await settle();
    click(root, ".btn", view);
    await settle();
  }

  return root;
}

function has(root: HTMLElement, ...facts: readonly string[]): void {
  for (const fact of facts) expect(root.textContent, `the frame draws "${fact}"`).toContain(fact);
}

describe("frame 2c · One job · History", () => {
  it("draws the five columns and every transition, each keeping its kind", async () => {
    const root = await jobTab("History");
    has(root, "When", "Kind", "What", "By", "Detail");
    has(root, "BREACHED", "sweep", "accountable → Priya Nair (step 2) · nudge in-app");
    has(root, "session 1 paused, then stopped", "fetch gauge", "75 % of 40 min · nudge to Arjun Menon");
    has(root, "IN_PROGRESS", "ACCEPTED", "ASSIGNED", "RAISED");
    expect(root.querySelectorAll("table tr").length).toBe(recordedJob.history.length + 1);
  });
});

describe("frame 2d · One job · Notes & photos", () => {
  it("draws the three notes, the guest's raising text among them, and the photo panel", async () => {
    const root = await jobTab("Notes & photos");
    has(root, "Suction pressure low, likely refrigerant.");
    has(root, "Guest called too; offered fan meanwhile.");
    has(root, "Room feels warm since noon", "the raising text");
    has(root, "Write a note…", "Add note", "Attach photo", "Photos · 1", "gauge.jpg");
  });
});

describe("frame 2e · One job · Links & steps", () => {
  it("draws the steps in sequence and the group links apart", async () => {
    const root = await jobTab("Links & steps");
    has(root, "Steps of this job · sequence", "Step", "Clock");
    has(root, "MRN-ENG-130", "Air conditioning › Leak test", "ASSIGNED · blocked", "stopped until this job resolves");
    has(root, "MRN-ENG-144", "starts on the day", "AUTO");
    has(root, "Cancelling this job cancels its steps. Closing it never closes them");
    has(root, "Linked jobs · same room, related", "MRN-HK-388", "Meera Krishnan", "Unlink");
    has(root, "Link a job…", "Add a step…");
  });
});

describe("frame 2g · One job · Record", () => {
  it("draws identity, audit and the viewer's own reminders", async () => {
    const root = await jobTab("Record");
    has(root, "Identity", "job_id", "018f3c…9a1e", "Number", "MRN-ENG-142", "Property", "Marina Bay · mrn", "Version", "9");
    has(root, "Audit", "Created", "guest · stay 7F2A", "Updated", "Arjun Menon", "Deleted");
    has(root, "Reminders", "none", "Remind me…");
  });
});

describe("frame 3 · Raise a job", () => {
  it("draws the ten fields, the catalogue's hints and the restricted default", async () => {
    const root = mount();
    await settle();
    click(root, ".btn", "Raise a job");
    await settle();
    has(root, "Where", "Room 0817 · Floor 8 · Tower A");
    has(root, "What", "Lighting › Bedside lamp dead", "alias matched");
    has(root, "Asset · optional", "Pick from Room 0817's assets…");
    has(root, "Summary", "Guest says right-side bedside lamp is dead");
    has(root, "Details · optional", "Anything the technician should know first");
    has(root, "Department", "Engineering (ENG)", "from the catalogue item");
    has(root, "Priority", "P3", "catalogue default · you may override");
    has(root, "Due", "policy: P3 within 60 min");
    has(root, "Assign to · on shift today", "AUTO — or pick", "Team · Day shift");
    has(root, "Schedule for later · optional", "Leave empty to raise now");
    has(root, "Restricted · off (catalogue default for this item)");
    has(root, "Raise MRN-ENG-143", "Cancel");
  });
});

describe("frame 4 · Resolve", () => {
  it("draws the item's resolutions, Other, the note box and what follows", async () => {
    const root = mount();
    await settle();
    click(root, "tr.pick td", "MRN-ENG-142");
    await settle();
    click(root, ".btn", "Resolve…");
    await settle();
    has(root, "Resolve MRN-ENG-142", "work 00:31:12 across 2 sessions · stopping the clock now");
    has(root, "What fixed it", "Filter cleaned", "Filter replaced", "Refrigerant topped up",
      "Thermostat replaced", "Compressor fault — escalate to vendor", "No fault found", "Other…");
    has(root, "In your words · optional", "Suction 45 psi, charged to 68");
    has(root, "Photo · optional", "Add a photo");
    has(root, "Guest-raised: the guest will be asked to rate this after it closes. Auto-close in 4 h unless reopened.");
  });
});

describe("frame 02-2 · Settings · Shifts & presence", () => {
  it("draws four departments, the two switches and what presence says now", async () => {
    const root = await settings("Shifts & presence");
    has(root, "Department", "Presence", "Follow Workforce shifts", "Service hours (fallback)", "Now");
    has(root, "Engineering", "07:00 – 23:00", "present · day shift since 07:00");
    has(root, "Housekeeping", "07:00 – 22:00", "Food & Beverage", "no · hours only", "06:00 – 00:00");
    has(root, "Front Office", "property clock · always running");
    has(root, "Save", "Discard");
  });
});

describe("frame 02-3 · Settings · Who is told", () => {
  it("draws the four roles against the four concerns, with the repeat and the scope", async () => {
    const root = await settings("Who is told");
    has(root, "Role", "At risk", "Breached", "Stuck", "Not triaged", "Repeat every", "Departments");
    has(root, "Assignee", "10 min", "own jobs");
    has(root, "Department supervisor", "15 min", "own department");
    has(root, "Department manager", "P1 only", "> 30 min", "30 min");
    has(root, "Property jobs manager", "ladder's last step", "all");
    has(root, "In-app only. There is no channel, no quiet hours, no per-person setting — the role decides");
  });
});

describe("frame 02-4 · Settings · Holds & reminders", () => {
  it("draws what a hold must carry and who is warned when", async () => {
    const root = await settings("Holds & reminders");
    has(root, "Putting a job on hold", "Requires", "a reason and a", "hold_until", "date");
    has(root, "Clock", "stopped while on hold", "Longest hold", "30 days · then STUCK → supervisor");
    has(root, "Warn before the date", "1 day before", "department supervisor");
    has(root, "on the day, 08:00", "assignee", "date passed, still on hold", "supervisor · repeat daily");
    has(root, "Manual reminders (S9 D3) need no setting");
  });
});

describe("frame 02-5 · Settings · Closing & rating", () => {
  it("draws the auto-close hours per scope and the rating rules", async () => {
    const root = await settings("Closing & rating");
    has(root, "Auto-close after RESOLVED", "Scope", "Hours", "Property default", "4 h", "Housekeeping", "1 h", "Engineering");
    has(root, "Until then the raiser may reopen; after, it is CLOSED and the resolution stands.");
    has(root, "Resolving and rating", "Note required", 'when the resolution is "Other"');
    has(root, "Photo", "none · optional · required", "Guest rating", "ask on close of guest-raised jobs, in the guest app");
    has(root, "Rating scale", "1–5 and a line of text");
  });
});

describe("frame 02-6 · Settings · Access", () => {
  it("draws five labels, who holds them and where each fact comes from", async () => {
    const root = await settings("Access");
    has(root, "Label", "Who", "Comes from");
    has(root, "Property jobs manager", "Rohan Desai", "granted by the GM in Identity · 2026-08-28");
    has(root, "Department manager · ENG", "Kiran Bhat", "Workforce headship");
    has(root, "Department supervisor · ENG", "Priya Nair");
    has(root, "Department member · ENG", "9 people", "Workforce posting");
    has(root, "Department manager · HK", "Anjali Rao");
    has(root, "Jobs has no editor because it owns none of these facts");
  });
});

describe("frame 02-8 · New policy · 1 of 3 · Name and scope", () => {
  it("draws all three samples, narrowing department › category › item", async () => {
    const root = await settings("Concern policy", "＋ New policy");
    has(root, "1 · Name and scope", "2 · The clock", "3 · The ladder");
    has(root, "Sample A · Engineering, one category", "Engineering ▾", "AC not working ▾", "AC — guest in room");
    has(root, "Applies to: every AC job at Marina Bay, unless the item has its own policy.");
    has(root, "Sample B · Housekeeping, one category", "Bottle of water ▾", "Water — 10 minutes");
    has(root, "Applies to: Still water · Sparkling water — the two items of the category.");
    has(root, "Sample C · one item, narrower still", "Water dropping from unit", "AC leak — ceiling risk");
    has(root, "Leave category empty for a department policy; leave item empty for a category policy. One policy per scope");
  });
});

describe("frame 02-9 · New policy · 2 of 3 · The clock", () => {
  it("draws the four thresholds per priority, the untriaged row and the worked example", async () => {
    const root = await settings("Concern policy", "＋ New policy");
    click(root, ".subnav .tab", "2 · The clock");
    await settle();
    has(root, "Priority", "Due within", "At risk when", "Stuck if not accepted in", "Stuck if no work session in", "Outside presence");
    has(root, "P1", "40 min", "75 % of due", "8 min", "15 min", "keeps running");
    has(root, "P2", "2 h", "20 min", "45 min", "pauses");
    has(root, "P3", "same shift", "80 % of due", "60 min");
    has(root, "NT", "no clock", "15 min", "untriaged");
    has(root, "Example, P1 raised", "at risk from", "75 % of 40 min", "breached from", "stuck at");
  });
});

describe("frame 02-10 · New policy · 3 of 3 · The ladder", () => {
  it("draws a ladder per priority, the manager-at-risk switch and the add-step dialog", async () => {
    const root = await settings("Concern policy", "＋ New policy");
    click(root, ".subnav .tab", "3 · The ladder");
    await settle();
    has(root, "P1", "4 steps", "1 · Assignee", "at risk", "2 · Department supervisor", "breached");
    has(root, "3 · Department manager", "breached + 15 min", "4 · Property jobs manager", "breached + 45 min");
    has(root, "manager also accountable from at risk", "＋ step", "drag to reorder");
    has(root, "P2", "3 steps", "breached + 60 min", "P3", "2 steps");
    has(root, "Add a step to P1", "Role", "Department manager ▾", "Becomes accountable", "when breached ▾");
    has(root, "Stuck goes straight to the supervisor for every priority");
  });
});

describe("frame 02-11 · How a job finds its policy", () => {
  it("is the rule drawn as a table, and is deliberately not a screen the module builds", async () => {
    const root = mount();
    await settle();
    click(root, ".head .tab", "Settings");
    await settle();
    // The frame's own caption: "not a screen anyone opens — it is the rule".
    expect(root.textContent).not.toContain("MRN-ENG-146");
    expect(root.textContent).not.toContain("not consulted");
  });
});

describe("the header's operator", () => {
  it("draws nobody until the platform says who is looking", async () => {
    const root = mount();
    await settle();
    // The module has no user in `ModuleIdentity`, and the chrome has nowhere to
    // mark a stand-in; a name drawn here that the platform did not establish
    // would be a fabricated identity shown to a real one.
    expect(root.querySelector(".who")).toBeNull();
  });

  it("draws the person the platform named, once it has", async () => {
    const root = mount(host({ ...live(), me: { name: "Rohan Desai", where: "property jobs manager" } }));
    await settle();
    await settle();
    expect(root.querySelector(".who")?.textContent).toBe("Rohan Desai · property jobs manager");
  });
});

describe("the rated example", () => {
  it("carries its own history rather than the other job's", async () => {
    const root = mount(host({ ...live(), job: recordedRatedJob }));
    await settle();
    click(root, "tr.pick td", "MRN-HK-388");
    await settle();
    click(root, ".subnav .tab", "History");
    await settle();
    has(root, "RATED", "auto-close after 1 h", "Delivered");
    expect(root.textContent).not.toContain("Suction pressure low");
  });
});

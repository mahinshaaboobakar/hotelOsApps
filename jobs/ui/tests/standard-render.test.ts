import { FAILURE_LABELS, HostCallError, type HostApi, type ReadFailure } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedBoard, recordedToday } from "../board/recorded/board";
import { recordedCatalogue } from "../board/recorded/catalogue";
import { recordedJob } from "../board/recorded/job";
import { recordedLive, recordedScheduledPage } from "../board/recorded/live";
import { recordedMe } from "../board/recorded/me";
import { recordedSettings } from "../board/recorded/settings";
import { failure } from "../chrome/failure";
import { status } from "../chrome/marks";

/**
 * The app surface checklist's AUTOMATED lines (`T`), for Jobs.
 *
 * `HotelOsApps/docs/app-surface-checklist.md` (GG, `3d521ce`). Named by line ID,
 * as `standard-source.test.ts` is. Placement is not here — it is measured, in
 * `docs/mockups/surface-audit.mjs`. **Every line was run against the code
 * before its fix**; a line that failed is recorded in the audit table with that
 * run as its proof, and stays here as the guard.
 */

const PROPERTY = { timezone: "Asia/Qatar", locale: "en-GB" };

function host(answers: Record<string, unknown>, property: HostApi["property"] = PROPERTY): HostApi {
  return {
    identity: { id: "jobs", version: "0.4.2", capabilities: ["job.read", "job.create", "job.assign", "job.complete", "job.cancel", "job.amend", "job.configure", "job.curate"] },
    property,
    call: (_capability, method) => {
      const answer = answers[method];
      return answer === undefined
        ? Promise.reject(new HostCallError({ kind: "unavailable", message: "not answered here" }))
        : Promise.resolve(answer);
    },
    on: () => () => {},
  };
}

const ANSWERS = {
  me: recordedMe, today: recordedToday, board: recordedBoard, job: recordedJob,
  catalogue: recordedCatalogue, settings: recordedSettings, scheduled: recordedScheduledPage, live: recordedLive,
};

async function settle(): Promise<void> {
  for (let i = 0; i < 4; i += 1) await new Promise((done) => setTimeout(done, 0));
}

async function mounted(h: HostApi): Promise<HTMLElement> {
  const root = document.createElement("div");
  document.body.replaceChildren(root);
  activate(h).mount(root);
  await settle();
  return root;
}

function press(root: HTMLElement, text: string): void {
  const node = Array.from(root.querySelectorAll<HTMLElement>("button")).find((b) => b.textContent === text);
  node?.click();
}

const at = new Date("2026-09-19T08:53:23Z");
const failing = (cause: ReadFailure["cause"]): ReadFailure => ({ cause, capability: "job.read", method: "board", said: null, at });
const CAUSES: readonly ReadFailure["cause"][] = ["unanswered", "forbidden", "unadmitted", "ungranted", "undecidable", "faulted"];

describe("app surface checklist — automated lines, Jobs", () => {
  it("N4 — the bar draws no search box, because Jobs has no search", async () => {
    const root = await mounted(host(ANSWERS));
    expect(root.querySelector(".head .search"), "a search box over no search").toBeNull();
  });

  it("N5 — the bar reads name · department · property, and says the department is not established", async () => {
    // Page 64 §3. The name and property as Room Care reads them; the department
    // is ADR 0203's, so the clause says it is not established — never a stand-in.
    const root = await mounted(host({ ...ANSWERS, me: { name: "Priya Nair", department: null, property: "Marina" } }));
    const who = root.querySelector(".head .who");
    expect(who?.textContent).toBe("Priya Nair · department not established · Marina");
    expect(who?.querySelector(".unset")?.textContent, "the gap is marked as a gap, not as a value").toBe("department not established");
  });

  it("N5 — a name nobody established is not drawn, and nothing stands in for it", async () => {
    const root = await mounted(host({ ...ANSWERS, me: { name: null, department: null, property: "MRN" } }));
    expect(root.querySelector(".head .who")?.textContent).toBe("department not established · MRN");
  });

  it("C8 — a board row that opens a job is reachable as a real button", async () => {
    const root = await mounted(host(ANSWERS));
    const rows = Array.from(root.querySelectorAll("tr.pick"));
    expect(rows.length).toBeGreaterThan(0);
    for (const row of rows) {
      expect(row.querySelector("button"), "the row's opener is a <button> a keyboard reaches").not.toBeNull();
    }
  });

  it("C11 — no primary action is live with nothing behind it", async () => {
    // A primary with nothing to send is drawn `off` with its reason; a primary
    // that does NOTHING is worse. Engineering's clock and the add-a-step box.
    const root = await mounted(host(ANSWERS));
    const dead: string[] = [];
    // `control()` marks what it wires (data-acts): a listener is invisible to the DOM.
    const look = (where: string): void => {
      for (const b of Array.from(root.querySelectorAll<HTMLButtonElement>(".btn.pri:not(.off)"))) {
        if (!b.hasAttribute("data-acts")) dead.push(`${where}: ${b.textContent ?? ""}`);
      }
    };

    press(root, "Settings");
    await settle();
    look("clock");
    press(root, "All policies");
    await settle();
    look("policies");
    press(root, "＋ New policy");
    await settle();
    look("new policy · 1");
    press(root, "3 · The ladder");
    await settle();
    look("new policy · 3");

    // And the one that WAS wired and saved nothing: it must now be drawn off.
    const save = Array.from(root.querySelectorAll<HTMLButtonElement>("button")).find((b) => b.textContent === "Save policy");
    expect(save?.classList.contains("off"), "Save policy saves nothing, so it is drawn off").toBe(true);
    expect(dead).toEqual([]);
  });

  it("G3 — a single page draws its range and both arrows, disabled", async () => {
    const root = await mounted(host({ ...ANSWERS, board: { rows: recordedBoard.rows.slice(0, 5), paging: { page: 0, pageSize: 12, total: 5 } } }));
    const pager = root.querySelector(".pager");
    expect(pager?.textContent).toContain("1–5 of 5");
    const arrows = Array.from(pager?.querySelectorAll("button[aria-label]") ?? []);
    expect(arrows).toHaveLength(2);
    for (const arrow of arrows) expect(arrow.hasAttribute("disabled")).toBe(true);
  });

  it("G4 — the range is the rows shown, on a short last page", async () => {
    const root = await mounted(host({ ...ANSWERS, board: { rows: recordedBoard.rows.slice(0, 11), paging: { page: 3, pageSize: 12, total: 47 } } }));
    expect(root.querySelector(".pager")?.textContent).toContain("37–47 of 47");
  });

  it("G5 — an empty page of a non-empty list says so, with no range", async () => {
    const root = await mounted(host({ ...ANSWERS, board: { rows: [], paging: { page: 4, pageSize: 12, total: 47 } } }));
    const text = root.querySelector(".pager")?.textContent ?? "";
    expect(text).toContain("no rows on this page · 47 in the list");
    expect(text).not.toMatch(/\d+–\d+ of/);
  });

  it("I2 — an absent timestamp renders —, never today", async () => {
    const root = await mounted(host(ANSWERS));
    const due = recordedBoard.rows.find((r) => r.dueAt === null);
    expect(due).toBeDefined();
    const row = Array.from(root.querySelectorAll("tr.pick")).find((r) => r.textContent?.includes(due?.number ?? "?"));
    const cell = row?.lastElementChild?.textContent ?? "";
    expect(cell).not.toMatch(/\b\d{1,2} Sept?\b/);
  });

  it("I4 — a property with no locale or zone renders ISO, 24-hour, marked UTC", async () => {
    const root = await mounted(host(ANSWERS, { timezone: null, locale: null }));
    const due = root.querySelector("tr.pick td:last-child")?.textContent ?? "";
    expect(due).toMatch(/^\d{4}-\d{2}-\d{2} \d{2}:\d{2} UTC$/);
  });

  it("P5 — a cancelled job's pill is `bad`: cancelled is over, not paused", () => {
    // §1: "Four pill tones, not three: bad is over (cancelled, no-show),
    // distinct from warn which is needs a decision". CANCELLED took `hold`, the
    // tone of a job that will resume. The locked frame never draws CANCELLED, so
    // the tone was the build's own choice.
    expect(status("CANCELLED").classList.contains("bad")).toBe(true);
    expect(status("ON_HOLD").classList.contains("hold")).toBe(true);
  });

  for (const cause of CAUSES) {
    it(`X7 · X9 · X11 — ${cause}: the right affordance, and the refusal names no person`, () => {
      const panel = failure(PROPERTY, failing(cause), "this board", () => {});
      const buttons = Array.from(panel.querySelectorAll("button")).map((b) => b.textContent);
      const text = panel.textContent ?? "";

      // X7: retry only for unanswered; Copy for a fault and the model; a refusal stops.
      if (cause === "unanswered") expect(buttons).toEqual([FAILURE_LABELS.retry]);
      else if (cause === "faulted" || cause === "undecidable") expect(buttons).toEqual([FAILURE_LABELS.copy]);
      else expect(buttons).toEqual([]);

      // X9: a refusal never routes the reader to a person.
      if (cause === "forbidden" || cause === "unadmitted" || cause === "ungranted") {
        expect(text).not.toMatch(/administrator|manager|ask (your|an|the)/i);
      }

      // X11: the model state names the model and never the person.
      if (cause === "undecidable") expect(text).not.toMatch(/\byou\b|\baccount\b/i);

      // X8: the facts are a labelled list, the permission its own run.
      expect(panel.querySelector("dl.gap-facts dt")?.textContent).toBe("Asked for");
      expect(panel.querySelector("dl.gap-facts dd b")?.textContent).toBe("job.read");
    });
  }
});

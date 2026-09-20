import { FAILURE_LABELS, HostCallError, failureDrawing, type HostApi, type ReadFailure } from "@hotelos/sdk";
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

  it("N5 — the bar reads name · department · property, and says the department is not known yet", async () => {
    // Page 64 §3. The name and property as Room Care reads them. The department
    // is ADR 0203's, and for a person whose only role is organization-wide this
    // sentence is the RULED answer rather than a gap: CTX-Q10 withdrawn
    // 2026-09-20 (463b8df7), because ADR 0116 §2 keeps that role in the cloud.
    const root = await mounted(host({ ...ANSWERS, me: { name: "Priya Nair", department: null, property: "Marina" } }));
    const who = root.querySelector(".head .who");
    expect(who?.textContent).toBe("Priya Nair · department not known yet · Marina");
    expect(who?.querySelector(".unset")?.textContent, "the gap is marked as a gap, not as a value").toBe("department not known yet");
  });

  it("N5 — a name Master Data does not hold is said, in the name's place, and nothing stands in for it", async () => {
    // Room Care's reading (df8d44f): the clause is never dropped, so two clauses
    // never pass for three. Null and blank are the same unknown.
    for (const name of [null, "", "   "]) {
      const root = await mounted(host({ ...ANSWERS, me: { name, department: null, property: "MRN" } }));
      expect(root.querySelector(".head .who")?.textContent).toBe("no name on record · department not known yet · MRN");
      expect(root.querySelector(".head .who .unset")?.textContent).toBe("no name on record");
    }
  });

  it("D1 (redline 6) — a scheduled day is day and month, with no weekday and no year", async () => {
    const root = await mounted(host(ANSWERS));
    press(root, "Scheduled");
    await settle();
    const first = root.querySelectorAll("table tr")[1]?.querySelector("td")?.textContent ?? "";
    expect(first).toMatch(/^\d{2} [A-Z][a-z]+$/);
  });

  it("D2 (redline 6) — the strip says when it was read in date-time, not with a weekday", async () => {
    const root = await mounted(host(ANSWERS));
    const end = root.querySelector(".strip .end")?.textContent ?? "";
    expect(end).not.toMatch(/\b(Mon|Tue|Wed|Thu|Fri|Sat|Sun)\b/);
    expect(end).toMatch(/\d{2} [A-Z][a-z]+, \d{2}:\d{2}$/);
  });

  it("Record (owner, 2026-09-19 c) — the number and the property's name, no raw id, instants in the property's form", async () => {
    const root = await mounted(host(ANSWERS));
    root.querySelector<HTMLElement>("tr.pick .opener")?.click();
    await settle();
    press(root, "Record");
    await settle();
    const text = root.textContent ?? "";
    expect(text).not.toContain("Job id");
    expect(text).not.toMatch(/[0-9a-f]{8}-[0-9a-f]{4}-/);
    expect(text).not.toMatch(/\d{4}-\d{2}-\d{2}T/);
    expect(text).toContain("02 Sept, 13:31");
  });

  it("APPS-Q50 — a row opens through its main cell's button, stretched over the row, never the tr", async () => {
    const root = await mounted(host(ANSWERS));
    const row = root.querySelector<HTMLElement>("tr.pick");
    const where = row?.querySelectorAll("td")[1];
    where?.click();
    await settle();
    expect(root.querySelector("tr.pick"), "a click on the tr opened nothing").not.toBeNull();
    const css = Array.from(root.querySelectorAll("style")).map((s) => s.textContent ?? "").join("\n");
    expect(css).toMatch(/tr\.pick\{[^}]*position:relative/);
    expect(css).toMatch(/\.opener::after\{[^}]*position:absolute[^}]*inset:0/);
    root.querySelector<HTMLButtonElement>("tr.pick .opener")?.click();
    await settle();
    expect(root.querySelector("tr.pick"), "the button opens the job").toBeNull();
  });

  it("§9 (APPS-Q53) condition 3 — New item has an explicit Cancel that clears what was typed", async () => {
    const root = await mounted(host(ANSWERS));
    press(root, "Catalogue");
    await settle();
    const name = root.querySelector<HTMLInputElement>('.dlg input[name="name"]');
    expect(name).not.toBeNull();
    if (name) name.value = "Water dripping";
    press(root, "Cancel");
    expect(root.querySelector<HTMLInputElement>('.dlg input[name="name"]')?.value).toBe("");
  });

  it("the Catalogue's sub-tabs with nothing behind them are drawn off, with their reason", async () => {
    const root = await mounted(host(ANSWERS));
    press(root, "Catalogue");
    await settle();
    for (const label of ["This property", "Import / export"]) {
      const tab = Array.from(root.querySelectorAll<HTMLButtonElement>(".subnav button")).find((b) => b.textContent === label);
      expect(tab?.disabled, label).toBe(true);
      expect(tab?.title, label).not.toBe("");
    }
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

      // X8: the facts are a labelled list, and "Asked for" says what could not be
      // read in plain words — owner, 2026-09-20, 64g §2 B (SDK 6751c7de). It said
      // "job.read · board" until then, which is the developer-content ruling's own
      // case: a service's vocabulary in front of somebody at a front desk. This
      // line asserted that old contract and is corrected, not relaxed.
      expect(panel.querySelector("dl.gap-facts dt")?.textContent).toBe("Asked for");
      expect(panel.querySelector("dl.gap-facts dd")?.textContent).toBe("this board");
      // Nothing this screen composes carries the code name. The one place left is
      // the SDK's own onward note for the three refusals — "This screen needs
      // job.read, and no grant…" (failure.ts:564-570), which 6751c7de did not
      // change. Measured here rather than stripped: Jobs draws a platform
      // sentence as the platform writes it, and editing it here would make one
      // app's refusal read differently from the other three. Reported for GG.
      const note = panel.querySelector(".gap-ask");
      const withoutTheNote = panel.cloneNode(true) as HTMLElement;
      withoutTheNote.querySelector(".gap-ask")?.remove();
      expect(withoutTheNote.textContent ?? "", "a code name outside the SDK's note").not.toContain("job.read");

      const fromTheSdkNote = cause === "forbidden" || cause === "unadmitted" || cause === "ungranted";
      expect((note?.textContent ?? "").includes("job.read"), "the SDK's note, unchanged by 6751c7de").toBe(fromTheSdkNote);

      // And it is not lost: the line a person copies for support still carries it.
      expect(failureDrawing(failing(cause), { app: "Jobs", the: "this board" }).wire).toContain("job.read");
    });
  }
});

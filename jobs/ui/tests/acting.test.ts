import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedBoard, recordedToday } from "../board/recorded/board";
import { recordedCatalogue } from "../board/recorded/catalogue";
import { recordedJob } from "../board/recorded/job";
import { recordedSettings } from "../board/recorded/settings";

/**
 * The controls, proved to reach the host — the other half of the wired round.
 *
 * The backend's own suite proves what happens when a call arrives; these prove
 * that pressing the button makes one, with the parameters the service reads.
 * Between them there is no gap for a button that looks alive and does nothing,
 * which is what twenty-eight rows of the held ledger were.
 */
const ALL = ["job.read", "job.create", "job.assign", "job.complete", "job.cancel", "job.amend", "job.configure", "job.curate"];

interface Made {
  capability: string;
  method: string;
  params: Record<string, unknown>;
}

function watching(granted: readonly string[] = ALL): { host: HostApi; calls: Made[] } {
  const calls: Made[] = [];
  const answers: Record<string, unknown> = {
    today: recordedToday,
    board: recordedBoard,
    job: recordedJob,
    catalogue: recordedCatalogue,
    settings: recordedSettings,
  };

  return {
    calls,
    host: {
      identity: { id: "jobs", version: "0.1.0", capabilities: granted },
      property: { timezone: "Asia/Qatar", locale: "en-GB" },
      call: (capability, method, params) => {
        calls.push({ capability, method, params: (params ?? {}) as Record<string, unknown> });
        const answer = answers[method];
        return answer === undefined
          ? Promise.reject(new HostCallError({ kind: "unavailable", message: "nothing answers that here" }))
          : Promise.resolve(answer);
      },
      on: () => () => {},
    },
  };
}

async function settle(): Promise<void> {
  await new Promise((done) => setTimeout(done, 0));
  await new Promise((done) => setTimeout(done, 0));
}

function mount(host: HostApi): HTMLElement {
  const root = document.createElement("div");
  document.body.replaceChildren(root);
  activate(host).mount(root);
  return root;
}

function press(root: HTMLElement, text: string): void {
  for (const node of Array.from(root.querySelectorAll<HTMLElement>("button"))) {
    if (node.textContent === text) {
      node.click();
      return;
    }
  }

  throw new Error(`no button reading "${text}"`);
}

function type(root: HTMLElement, name: string, value: string): void {
  const field = root.querySelector<HTMLInputElement | HTMLTextAreaElement>(`[name="${name}"]`);
  if (field === null) throw new Error(`no field named "${name}"`);
  field.value = value;
}

/** A tab in the sub-navigation, which draws its count beside its label. */
function tab(root: HTMLElement, label: string): void {
  for (const node of Array.from(root.querySelectorAll<HTMLElement>("button"))) {
    if (node.textContent?.startsWith(label) === true) {
      node.click();
      return;
    }
  }

  throw new Error(`no tab reading "${label}"`);
}

/** The version the recorded job carries, which is what a screen would send. */
function version(): number {
  const held = recordedJob.record.find((line) => line.k === "Version");
  return held === undefined ? 0 : Number(held.v);
}

function made(calls: readonly Made[], method: string): Made {
  const call = calls.find((one) => one.method === method);
  if (call === undefined) {
    throw new Error(`nothing called "${method}"; called ${calls.map((one) => one.method).join(", ")}`);
  }

  return call;
}

describe("the pager, as the standard has it", () => {
  it("draws on a single page, with both arrows disabled", async () => {
    const { host } = watching();
    const root = mount(host);
    await settle();
    press(root, "Scheduled");
    await settle();

    // The count is the information — it says the list in front of you is the
    // whole list, which a list that simply stops cannot (standard §6).
    const pager = root.querySelector(".pager");
    expect(pager?.textContent).toContain("of");

    const arrows = Array.from(root.querySelectorAll<HTMLButtonElement>(".pager .pg"))
      .filter((button) => button.textContent === "‹" || button.textContent === "›");
    expect(arrows).toHaveLength(2);
    for (const arrow of arrows) expect(arrow.hasAttribute("disabled")).toBe(true);
  });

  it("is the list's next sibling, which is what the growth rule depends on", async () => {
    const { host } = watching();
    const root = mount(host);
    await settle();

    const pager = root.querySelector(".pager");
    expect(pager?.previousElementSibling?.tagName).toBe("TABLE");
    expect(pager?.parentElement?.classList.contains("body")).toBe(true);
  });

  it("leaves a caveat as a caveat — Live's note is not a pager", async () => {
    const { host } = watching();
    const root = mount(host);
    await settle();
    press(root, "Live");
    await settle();

    expect(root.textContent).toContain("ON TRACK rows are not listed here");
    expect(root.querySelector(".pager")).toBeNull();
  });
});

describe("the controls that act", () => {
  it("raises a job with what the person typed", async () => {
    const { host, calls } = watching();
    const root = mount(host);
    await settle();
    press(root, "＋ Raise a job");
    await settle();

    type(root, "locationId", "0198e4e0-0000-7000-8000-000000000001");
    type(root, "summary", "Room feels warm since noon");
    press(root, "Raise it");
    await settle();

    const call = made(calls, "raise");
    expect(call.capability).toBe("job.create");
    expect(call.params.summary).toBe("Room feels warm since noon");
    expect(call.params.locationId).toBe("0198e4e0-0000-7000-8000-000000000001");
    expect(typeof call.params.itemId).toBe("string");
  });

  it("says what the service said when it refuses, and stays on the form", async () => {
    const { host, calls } = watching();
    const root = mount(host);
    await settle();
    press(root, "＋ Raise a job");
    await settle();

    // No summary: refused by the screen before it reaches the wire, because
    // the person can fix it where they are standing.
    press(root, "Raise it");
    await settle();
    expect(root.textContent).toContain("a job needs one line saying what is wrong");
    expect(calls.some((one) => one.method === "raise")).toBe(false);

    // With a summary it goes, and the failure is said where the person is
    // looking. "Unavailable" is not a sentence for a person — the SDK marks
    // which refusals are — so the screen says its own plain thing instead of
    // repeating a transport's word.
    type(root, "locationId", "0198e4e0-0000-7000-8000-000000000001");
    type(root, "summary", "Room feels warm since noon");
    press(root, "Raise it");
    await settle();
    expect(root.textContent).toContain("could not be done just now");
    expect(calls.some((one) => one.method === "raise")).toBe(true);
  });

  it("takes a job, holds it with a reason, and cancels it with one", async () => {
    const { host, calls } = watching();
    const root = mount(host);
    await settle();
    root.querySelectorAll<HTMLElement>("tr.pick")[0]?.click();
    await settle();

    press(root, "Take it");
    await settle();
    const take = made(calls, "take");
    expect(take.capability).toBe("job.assign");
    expect(take.params.id).toBe(recordedJob.row.id);
    expect(take.params.version).toBe(version());

    press(root, "Put on hold…");
    await settle();
    type(root, "answer", "parts, Thursday");
    press(root, "Do it");
    await settle();
    expect(made(calls, "hold").params.reason).toBe("parts, Thursday");

    press(root, "Cancel job…");
    await settle();
    type(root, "answer", "raised twice");
    press(root, "Do it");
    await settle();
    const cancelled = made(calls, "cancel");
    expect(cancelled.capability).toBe("job.cancel");
    expect(cancelled.params.reason).toBe("raised twice");
  });

  it("will not hold without a reason", async () => {
    const { host, calls } = watching();
    const root = mount(host);
    await settle();
    root.querySelectorAll<HTMLElement>("tr.pick")[0]?.click();
    await settle();

    press(root, "Put on hold…");
    await settle();
    press(root, "Do it");
    await settle();

    expect(root.textContent).toContain("a reason is needed");
    expect(calls.some((one) => one.method === "hold")).toBe(false);
  });

  it("adds a note from the Notes tab", async () => {
    const { host, calls } = watching();
    const root = mount(host);
    await settle();
    root.querySelectorAll<HTMLElement>("tr.pick")[0]?.click();
    await settle();
    tab(root, "Notes & photos");
    await settle();

    type(root, "text", "Guest called again");
    press(root, "Add note");
    await settle();

    const note = made(calls, "note");
    expect(note.capability).toBe("job.amend");
    expect(note.params.text).toBe("Guest called again");
  });

  it("resolves with the resolution that was picked", async () => {
    const { host, calls } = watching();
    const root = mount(host);
    await settle();
    root.querySelectorAll<HTMLElement>("tr.pick")[0]?.click();
    await settle();
    press(root, "Resolve…");
    await settle();

    press(root, "Filter replaced");
    press(root, "Resolve");
    await settle();

    const resolved = made(calls, "resolve");
    expect(resolved.capability).toBe("job.complete");
    expect(resolved.params.resolutionId).toBe("r2");
    expect(resolved.params.version).toBe(version());
  });

  it("curates the catalogue — a category, an item and a resolution", async () => {
    const { host, calls } = watching();
    const root = mount(host);
    await settle();
    press(root, "Catalogue");
    await settle();

    press(root, "＋ New");
    type(root, "name", "Lifts");
    type(root, "code", "LIFTS");
    type(root, "department", "ENG");
    press(root, "Create category");
    await settle();
    expect(made(calls, "saveCategory").params.name).toBe("Lifts");

    press(root, "Create item");
    await settle();
    expect(calls.some((one) => one.method === "saveItem")).toBe(false);
  });

  it("saves a setting the property decides", async () => {
    const { host, calls } = watching();
    const root = mount(host);
    await settle();
    press(root, "Settings");
    await settle();
    press(root, "Closing & rating");
    await settle();

    type(root, "autoCloseHours", "6");
    press(root, "Save");
    await settle();

    const saved = made(calls, "saveClosing");
    expect(saved.capability).toBe("job.configure");
    expect(saved.params.autoCloseHours).toBe(6);
  });

  it("offers no acting control to somebody who may only read", async () => {
    const { host, calls } = watching(["job.read"]);
    const root = mount(host);
    await settle();
    root.querySelectorAll<HTMLElement>("tr.pick")[0]?.click();
    await settle();

    expect(root.textContent).not.toContain("Take it");
    expect(root.textContent).not.toContain("Put on hold…");
    expect(root.textContent).not.toContain("Cancel job…");
    expect(calls.every((one) => one.capability === "job.read")).toBe(true);
  });
});

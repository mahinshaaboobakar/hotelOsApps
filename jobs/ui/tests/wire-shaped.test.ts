import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import type { JobDetail, Live, Settings } from "../board";
import { recordedBoard, recordedToday } from "../board/recorded/board";
import { recordedJob } from "../board/recorded/job";
import { recordedLive } from "../board/recorded/live";
import { recordedSettings } from "../board/recorded/settings";
import { settle } from "./walk";

/**
 * The screens against what the BACKEND sends, not what the harness holds.
 *
 * The harness's recorded answers carry the approved frames' words — "02 Sep 13:47
 * · 14 min after assignment" — so every check that reads them passes. The
 * backend sends raw ISO instants and days, status and role tokens, and a work
 * session's seconds (`JobProjection.PriorityAndTime`, `Assignment`, `History`;
 * `LiveProjection`; `SettingsProjection`). Found 2026-09-19 after KK's note that
 * sentences can carry formatted times: on a live property the Overview printed
 * `2026-09-02T11:10:00.0000000+00:00`, and every guard was green.
 *
 * These are the backend's own shapes, copied from those projections; the screen
 * must turn each into the property's words.
 */

const INSTANT = "2026-09-02T11:10:00.0000000+00:00";

const job: JobDetail = {
  ...recordedJob,
  priorityAndTime: [
    { k: "Priority", v: "P1" },
    { k: "Decided by", v: "FLOW" },
    { k: "Due", v: INSTANT },
    { k: "Scheduled for", v: "2026-09-20" },
  ],
  assignment: [
    { k: "Holder", v: "Arjun Menon" },
    { k: "How", v: "AUTO" },
    { k: "Assigned", v: "2026-09-02T10:33:00.0000000+00:00" },
    { k: "Accepted", v: "not yet" },
    { k: "Assignments", v: "1" },
  ],
  record: [
    { k: "Number", v: "MRN-ENG-142" },
    { k: "Property", v: "The Marina Bay" },
    { k: "Created", v: INSTANT },
    { k: "Created by", v: "the guest of stay 7F2A" },
    { k: "Updated", v: "2026-09-02T10:33:00.0000000+00:00" },
    { k: "Updated by", v: "Arjun Menon" },
    { k: "Version", v: "9" },
    { k: "Deleted", v: "—" },
  ],
  history: [
    { at: INSTANT, kind: "concern", what: "BREACHED", by: "JOBS_MANAGER", detail: "the hold's date has passed" },
    { at: INSTANT, kind: "status", what: "ASSIGNED → IN_PROGRESS", by: "Arjun Menon", detail: "" },
    { at: INSTANT, kind: "work", what: "worked", by: "Staff member", detail: "1421s" },
  ],
};

const live: Live = {
  ...recordedLive,
  concern: recordedLive.concern.map((c) => ({ ...c, accountable: "SUPERVISOR", lastNudge: INSTANT })),
};

const settings: Settings = {
  ...recordedSettings,
  holdWarnings: [{ when: INSTANT, who: "MRN-ENG-141" }],
};

const WIRE: readonly (readonly [string, RegExp])[] = [
  ["an unformatted instant", /\b\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/g],
  ["an unformatted day", /\b\d{4}-\d{2}-\d{2}\b/g],
  ["a wire token", /\b[A-Z]+_[A-Z_]+\b/g],
  ["a count of seconds", /\b\d+s\b/g],
];

function host(): HostApi {
  const answers: Record<string, unknown> = { today: recordedToday, board: recordedBoard, job, live, settings };
  return {
    identity: { id: "jobs", version: "0.4.3", capabilities: ["job.read", "job.configure"] },
    property: { timezone: "Asia/Qatar", locale: "en-GB" },
    call: (_c, method) => (answers[method] === undefined
      ? Promise.reject(new HostCallError({ kind: "unavailable", message: "not answered" }))
      : Promise.resolve(answers[method])),
    on: () => () => {},
  };
}

async function reach(steps: readonly string[]): Promise<HTMLElement> {
  const root = document.createElement("div");
  document.body.replaceChildren(root);
  activate(host()).mount(root);
  await settle();
  for (const step of steps) {
    const target = step === "job"
      ? root.querySelector<HTMLElement>("tr.pick .opener")
      : Array.from(root.querySelectorAll<HTMLElement>("button")).find((b) => b.textContent?.startsWith(step) === true);
    target?.click();
    await settle();
  }
  return root;
}

/**
 * The text a person reads, one text node at a time with a separator between.
 * `textContent` joins neighbouring cells with nothing between them —
 * "SUPERVISOR2026-09-02T11:10…" — and a `\b`-anchored pattern then finds no
 * boundary and matches nothing: this test passed on Live and Settings while
 * both printed raw instants, until it read the nodes apart.
 */
function readable(root: HTMLElement): string {
  const parts: string[] = [];
  const walker = document.createTreeWalker(root, NodeFilter.SHOW_TEXT);
  for (let node = walker.nextNode(); node !== null; node = walker.nextNode()) {
    if (node.parentElement?.closest("style") === null) parts.push(node.nodeValue ?? "");
  }
  return parts.join(" | ");
}

function wire(root: HTMLElement): string[] {
  const text = readable(root);
  return WIRE.flatMap(([what, p]) => [...text.matchAll(p)].map((m) => `${what}: ${m[0]}`));
}

describe("the screens say what the backend sends in the property's words", () => {
  it("each pattern names its own planted value, and leaves what a person reads alone", () => {
    // A positive control per pattern (KK, 2026-09-20). Without one, a pattern
    // that loses a word boundary — as two of Jobs' did while being written —
    // matches nothing and every screen passes it.
    const planted = ["Due 2026-09-02T11:10:00.0000000+00:00", "Scheduled for 2026-09-20", "by JOBS_MANAGER", "worked 1421s"];
    expect(WIRE.length).toBe(planted.length);
    WIRE.forEach(([what, pattern], i) => {
      expect([...(planted[i] ?? "").matchAll(pattern)].length, `${what} did not name its own planted value`).toBeGreaterThan(0);
    });

    // What the screens draw instead: none of these may be named.
    const said = "Due 02 Sept, 14:10 | Scheduled for 20 Sept | by Jobs manager | worked 00:23:41 | IN PROGRESS";
    for (const [what, pattern] of WIRE) {
      expect([...said.matchAll(pattern)].map((m) => m[0]), `${what} named the property's own words`).toEqual([]);
    }
  });

  for (const [name, steps] of [
    ["One job · Overview", ["job"]],
    ["One job · History", ["job", "History"]],
    ["One job · Record", ["job", "Record"]],
    ["Live", ["Live"]],
    ["Settings · Holds & reminders", ["Settings", "Holds & reminders"]],
  ] as const) {
    it(name, async () => {
      expect(wire(await reach(steps))).toEqual([]);
    });
  }
});

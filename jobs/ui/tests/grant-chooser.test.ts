import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedBoard, recordedToday } from "../board/recorded/board";
import { recordedSettings } from "../board/recorded/settings";
import { settle } from "./walk";

/**
 * Granting a jobs manager chooses a **person**, never an identifier — ADR 0225 §1
 * (`JOBS-Q4`, owner, 2026-09-22): *"the id concept is wrong — no such concept in
 * the whole of any app… in db all we keep is id, but the user doesn't know the
 * id, so chooser always name."*
 *
 * The chooser is Context's staff search (ADR 0224, `CORE-Q33`), which composes
 * Master Data's identity with Workforce's position and department and returns a
 * `staff_id` while the person reads *John Mathew · Housekeeping · Room
 * Attendant*. **Jobs does not build its own**: one capability, not five.
 *
 * **Measured 2026-09-23**: Context answers eight RPCs and none of them searches
 * staff, so the capability this grant needs does not exist yet. Until it does,
 * the grant is drawn off with its reason — a screen that cannot offer the ruled
 * flow offers none, rather than keeping the field the ruling refused.
 */

function host(): HostApi {
  const answers: Record<string, unknown> = { today: recordedToday, board: recordedBoard, settings: recordedSettings };
  return {
    identity: { id: "jobs", version: "0.4.4", capabilities: ["job.read", "job.configure"] },
    property: { timezone: "Asia/Qatar", locale: "en-GB" },
    call: (_capability, method) => (answers[method] === undefined
      ? Promise.reject(new HostCallError({ kind: "unavailable", message: "not answered" }))
      : Promise.resolve(answers[method])),
    on: () => () => {},
  };
}

async function access(): Promise<HTMLElement> {
  const root = document.createElement("div");
  document.body.replaceChildren(root);
  activate(host()).mount(root);
  await settle();
  for (const step of ["Settings", "Access"]) {
    Array.from(root.querySelectorAll<HTMLElement>("button")).find((b) => b.textContent === step)?.click();
    await settle();
  }
  return root;
}

describe("granting a jobs manager chooses a person", () => {
  it("asks for no identifier", async () => {
    const root = await access();
    const fields = Array.from(root.querySelectorAll<HTMLInputElement>("input, textarea"));
    for (const field of fields) {
      expect(field.name, "a field asking for an id").not.toBe("userId");
    }
    expect(root.textContent ?? "", "the words of the old field").not.toContain("User id");
  });

  it("draws the grant off, with the reason, while the chooser does not exist", async () => {
    const root = await access();
    const control = Array.from(root.querySelectorAll<HTMLButtonElement>("button"))
      .find((b) => (b.textContent ?? "").startsWith("Choose a person"));
    expect(control, "the grant offers a chooser").toBeDefined();
    expect(control?.disabled, "drawn off until Context's staff search lands").toBe(true);
    expect(control?.title ?? "", "and saying why").not.toBe("");
  });

  it("still lists who holds it, and revoking is untouched", async () => {
    const root = await access();
    expect(root.textContent ?? "").toContain("Jobs managers");
    const revoke = Array.from(root.querySelectorAll<HTMLButtonElement>("button")).find((b) => b.textContent === "Revoke…");
    expect(revoke?.disabled, "revoking a grant still works").toBe(false);
  });
});

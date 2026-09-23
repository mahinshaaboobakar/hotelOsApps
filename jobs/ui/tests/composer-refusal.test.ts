import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { activate } from "../application";
import { recordedBoard, recordedToday } from "../board/recorded/board";
import { recordedCatalogue } from "../board/recorded/catalogue";
import { settle } from "./walk";

/**
 * A refused write speaks **inside the composer** — ADR 0225 §4 (`JOBS-Q4`), the
 * owner's ruling of 2026-09-22 on the four drawn decisions, taking §9's gloss:
 * *"a refused write keeps the composer open and shows the reason in it"*.
 *
 * It spoke on the Catalogue's own line under the page, which is where the rest
 * of Jobs says things. Measured then, ruled now: the reason belongs where the
 * person is composing the thing being refused, so each composer carries its own
 * line and the screen's line carries what is not a composer's.
 */

const REFUSAL = "that category isn't in this organisation's catalogue";

function host(): HostApi {
  const answers: Record<string, unknown> = { today: recordedToday, board: recordedBoard, catalogue: recordedCatalogue };
  return {
    identity: { id: "jobs", version: "0.4.4", capabilities: ["job.read", "job.curate"] },
    property: { timezone: "Asia/Qatar", locale: "en-GB" },
    call: (_capability, method) => {
      if (answers[method] !== undefined) return Promise.resolve(answers[method]);
      // Every write is refused, in the service's own words.
      return Promise.reject(new HostCallError({ kind: "invalid", message: REFUSAL }));
    },
    on: () => () => {},
  };
}

async function catalogue(): Promise<HTMLElement> {
  const root = document.createElement("div");
  document.body.replaceChildren(root);
  activate(host()).mount(root);
  await settle();
  const tab = Array.from(root.querySelectorAll<HTMLElement>("button")).find((b) => b.textContent === "Catalogue");
  tab?.click();
  await settle();
  return root;
}

/** The composer a control sits in — the card or the inline box, never the page. */
function composerOf(button: HTMLElement): HTMLElement | null {
  return button.closest(".dlg") ?? button.closest(".card");
}

describe("a refused write speaks inside the composer", () => {
  for (const [name, label] of [
    ["New item", "Create item"],
    ["a new category", "Create category"],
    ["a new resolution", "＋ Add resolution"],
  ] as const) {
    it(`${name}: the reason is in the composer, not on the screen's line`, async () => {
      const root = await catalogue();
      // Open the composer where one has to be opened.
      for (const opener of ["＋ New", "Edit"]) {
        const button = Array.from(root.querySelectorAll<HTMLButtonElement>("button")).find((b) => b.textContent === opener);
        if (button !== undefined && !button.disabled) { button.click(); await settle(); }
      }

      const press = Array.from(root.querySelectorAll<HTMLButtonElement>("button")).find((b) => b.textContent === label);
      expect(press, `${label} is on the screen`).toBeDefined();
      const composer = press === undefined ? null : composerOf(press);
      expect(composer, `${label} sits in a composer`).not.toBeNull();

      // Fill what the composer needs, so the refusal comes from the service.
      for (const field of Array.from(composer?.querySelectorAll<HTMLInputElement | HTMLTextAreaElement>("input, textarea") ?? [])) {
        field.value = "typed for the test";
      }
      press?.click();
      await settle();

      expect(composer?.textContent, "the reason is inside the composer").toContain(REFUSAL);
      const elsewhere = root.cloneNode(true) as HTMLElement;
      for (const box of Array.from(elsewhere.querySelectorAll(".dlg, .card"))) box.remove();
      expect(elsewhere.textContent ?? "", "and nowhere else on the screen").not.toContain(REFUSAL);
    });
  }
});

/**
 * ADR 0225 §2: the Record tab draws who created and who last changed the job,
 * beside each instant, as frame 2g draws it — "02 Sept, 13:31 · the guest of
 * stay 7F2A". The service sends the name as its own value and the screen writes
 * the line.
 */
describe("the Record tab says who, beside when", () => {
  it("joins each instant with the name the service sent", async () => {
    const { activate: mount } = await import("../application");
    const { recordedJob } = await import("../board/recorded/job");
    const detail = {
      ...recordedJob,
      record: [
        { k: "Number", v: "MRN-ENG-142" },
        { k: "Created", v: "2026-09-02T10:31:00.0000000+00:00" },
        { k: "Created by", v: "the guest of stay 7F2A" },
        { k: "Updated", v: "2026-09-02T11:07:00.0000000+00:00" },
        { k: "Updated by", v: "Arjun Menon" },
        { k: "Deleted", v: "—" },
      ],
    };
    const answers: Record<string, unknown> = {
      today: recordedToday, board: recordedBoard, catalogue: recordedCatalogue, job: detail,
    };
    const h: HostApi = {
      identity: { id: "jobs", version: "0.4.4", capabilities: ["job.read"] },
      property: { timezone: "Asia/Qatar", locale: "en-GB" },
      call: (_c, method) => (answers[method] === undefined
        ? Promise.reject(new HostCallError({ kind: "unavailable", message: "not answered" }))
        : Promise.resolve(answers[method])),
      on: () => () => {},
    };
    const root = document.createElement("div");
    document.body.replaceChildren(root);
    mount(h).mount(root);
    await settle();
    root.querySelector<HTMLElement>("tr.pick .opener")?.click();
    await settle();
    Array.from(root.querySelectorAll<HTMLElement>("button")).find((b) => b.textContent === "Record")?.click();
    await settle();

    const text = root.textContent ?? "";
    expect(text).toContain("02 Sept, 13:31 · the guest of stay 7F2A");
    expect(text).toContain("02 Sept, 14:07 · Arjun Menon");
    expect(text, "the name is not a row of its own").not.toMatch(/Created by\s*the guest/);
  });
});

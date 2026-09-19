import { HostCallError, type HostApi } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { developerContent, readableText } from "../../../scripts/developer-content";
import { activate } from "../application";
import { recordedBoard } from "../board/recorded/board";
import { recordedEscalated, recordedMine, recordedQuiet } from "../board/recorded/widget";
import { PANELS } from "../preview/widgets";
import { jobsNow } from "../widgets/panel/jobs-now";
import { host, open, SCREENS, settle } from "./walk";

/**
 * No developer note reaches a person at the property (owner ruling, 2026-09-19): no register id, ADR, design
 * section, code identifier or correlation id in what Jobs renders. The patterns are the shared list
 * (`scripts/developer-content.ts`, extending Workforce's b001bfac); comments citing rulings are records and are
 * not read, because only what reaches the screen is a claim to staff.
 *
 * Every screen is read as it opens, and again after each of its controls is pressed once from a fresh mount, so
 * Raise, Resolve, a policy's steps and a confirmation are read too. The empty Board, every failure cause at
 * screen size and the six widgets — quiet, escalated, mine and failed — are read beside them.
 */

type Kind = "unavailable" | "forbidden" | "internal" | "local_forbidden" | "user_forbidden" | "model_unavailable";
const KINDS: readonly Kind[] = ["unavailable", "forbidden", "internal", "local_forbidden", "user_forbidden", "model_unavailable"];

function mount(h: HostApi): HTMLElement {
  const root = document.createElement("div");
  document.body.replaceChildren(root);
  activate(h).mount(root);
  return root;
}

/** The walk's host, with one method answered differently. */
function answering(method: string, answer: unknown): HostApi {
  const base = host();
  return { ...base, call: (c, m, p) => (m === method ? Promise.resolve(answer) : base.call(c, m, p)) };
}

function refusing(kind: Kind, method?: string): HostApi {
  const base = host();
  return {
    ...base,
    call: (c, m, p) => (method === undefined || m === method
      ? Promise.reject(new HostCallError({ kind, message: `${c}/${m} refused for the test` }))
      : base.call(c, m, p)),
  };
}

async function reached(h: HostApi, steps: readonly string[]): Promise<HTMLElement> {
  const root = mount(h);
  await settle();
  await open(root, steps);
  return root;
}

/**
 * A system's name, which the shared list cannot yet hold (GG is adding it once no
 * application trips it). Jobs' own: "Master Data" and "Kernel". "Workforce" is
 * not one of them here — it is an application staff use, and "Follow Workforce
 * shifts" stays by the owner's ruling (2026-09-19).
 */
const SYSTEMS: readonly RegExp[] = [/\bMaster Data\b/g, /\bKernel\b/g];

// A raw id and an unformatted instant are in the shared list now (KK, d540eb9b,
// from be1c6730), so developerContent() finds them; no local copy.
const read = (root: Element): string[] => {
  const text = readableText(root);
  return [
    ...developerContent(text),
    ...SYSTEMS.flatMap((p) => [...text.matchAll(p)].map((m) => `a system's name: ${m[0]}`)),
  ];
};

describe("developer content", () => {
  for (const screen of SCREENS) {
    it(`none on ${screen.name}, or on anything one press away`, async () => {
      const found = new Set<string>();
      const first = await reached(host(), screen.open);
      for (const hit of read(first)) found.add(`as it opens — ${hit}`);
      const count = first.querySelectorAll("button").length;
      for (let i = 0; i < count; i += 1) {
        const root = await reached(host(), screen.open);
        const button = root.querySelectorAll<HTMLButtonElement>("button")[i];
        if (button === undefined || button.disabled) continue;
        const label = button.textContent?.trim() ?? "";
        button.click();
        await settle();
        for (const hit of read(root)) found.add(`after "${label}" — ${hit}`);
      }
      expect([...found], screen.name).toEqual([]);
    }, 60_000);
  }

  it("none on the empty Board, under every filter", async () => {
    const empty = { rows: [], paging: { page: 0, pageSize: 12, total: 0 } };
    const root = await reached(answering("board", empty), []);
    const found = read(root);
    for (const chip of Array.from(root.querySelectorAll<HTMLButtonElement>(".chip"))) {
      if (chip.disabled) continue;
      chip.click();
      await settle();
      found.push(...read(root));
    }
    expect(found).toEqual([]);
  });

  for (const kind of KINDS) {
    it(`none when the board cannot read — ${kind}`, async () => {
      const root = await reached(refusing(kind, "board"), []);
      expect(read(root)).toEqual([]);
    });
  }

  it("none on the six widgets, answered or failed", async () => {
    const found: string[] = [];
    const say = (h: HostApi): HostApi => h;
    for (const answer of [recordedQuiet, recordedEscalated, recordedMine]) {
      found.push(...read(await jobsNow(say({ ...host(), call: () => Promise.resolve(answer) }))));
    }
    for (const [name, panel] of Object.entries(PANELS)) {
      for (const hit of read(await panel(host()))) found.push(`${name}: ${hit}`);
    }
    for (const kind of KINDS) {
      for (const hit of read(await jobsNow(refusing(kind)))) found.push(`jobs-now · ${kind}: ${hit}`);
      for (const [name, panel] of Object.entries(PANELS)) {
        for (const hit of read(await panel(refusing(kind)))) found.push(`${name} · ${kind}: ${hit}`);
      }
    }
    expect(found).toEqual([]);
  });

  it("walks what it says it walks — the recorded Board has rows to open", () => {
    // A walk over a board with no rows would never open a job, and six screens would pass unread.
    expect(recordedBoard.rows.length).toBeGreaterThan(0);
  });
});

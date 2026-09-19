import { readdirSync, readFileSync, statSync } from "node:fs";
import { join, relative, sep } from "node:path";

import { HostCallError } from "@hotelos/sdk";
import { describe, expect, it } from "vitest";

import { load, type Read } from "../chrome/load";
import { roomsReady } from "../widgets/panel/panels";
import { host, recorded, settle } from "./host";

/**
 * The owner's rule, 2026-09-09: the product never fabricates, and marking a
 * fabrication honestly is still rendering one. These hold the mechanism —
 * GuestOps' and Workforce's `Read<T>` — rather than the discipline.
 */

const ROOT = process.cwd();

/** Every source file that ships: the module, its chrome, its screens and its widgets — never the harness or the suite. */
function shipped(dir = ROOT): string[] {
  return readdirSync(dir).flatMap((name) => {
    const path = join(dir, name);
    const rel = relative(ROOT, path).split(sep).join("/");
    if (["node_modules", "preview", "tests"].includes(rel) || rel.startsWith(".")) return [];
    if (statSync(path).isDirectory()) return shipped(path);
    return rel.endsWith(".ts") && !rel.endsWith(".d.ts") && !rel.endsWith(".config.ts") ? [rel] : [];
  });
}

/** The module specifiers a file imports, statically or dynamically. */
function specifiers(source: string): string[] {
  return [...source.matchAll(/(?:\bfrom\s*|\bimport\s*\(?\s*)["']([^"']+)["']/g)].map((m) => m[1]!);
}

/** A failed read's value is not reachable — tsc refuses the line below, and `tsc --noEmit` covers this file. */
export function valueOnlyWhenRead(got: Read<number>): number | null {
  if (!got.ok) {
    // @ts-expect-error — there is no value on a failure, not even an optional one, to draw from
    const drawn: unknown = got.value;
    return drawn === undefined ? null : 0;
  }
  return got.value;
}

describe("the read seam", () => {
  it("sees the shipped source it guards, so an empty sweep cannot pass", () => {
    const files = shipped();
    expect(files).toContain("screens/board/index.ts");
    expect(files).toContain("widgets/panel/panels.ts");
    expect(files).toContain("chrome/load.ts");
    expect(files.some((f) => f.startsWith("preview/") || f.startsWith("tests/"))).toBe(false);
  });

  it("ships nothing that imports the recorded fixtures or the harness", () => {
    const offenders = shipped().flatMap((file) =>
      specifiers(readFileSync(join(ROOT, file), "utf8"))
        .filter((spec) => /(^|\/)(preview|recorded)(\/|$)|\.json$/.test(spec))
        .map((spec) => `${file} imports ${spec}`));
    expect(offenders).toEqual([]);
  });

  it("asks the platform from one file for Room Care, and the shell's opener from the widget card", () => {
    // Any `.call(`, whatever the host is named where it was passed along.
    const callers = shipped().filter((file) => /\.call\(/.test(readFileSync(join(ROOT, file), "utf8")));
    expect(callers.sort()).toEqual(["chrome/load.ts", "widgets/card.ts"]);
  });

  it("renders no date itself — every one goes through the SDK (page 64 §11)", () => {
    // No exemption. The moment a read failed is stamped by the SDK's `load`; Room Care's own copy stamped it in
    // `chrome/load.ts`, which this guard exempted by name until that copy was deleted on 2026-09-18.
    const offenders = shipped().flatMap((file) =>
      readFileSync(join(ROOT, file), "utf8").split("\n")
        .map((line, i) => ({ line, at: `${file}:${i + 1}` }))
        .filter(({ line }) => /\bIntl\.|\bDate\.(now|parse)\(|\bnew Date\(|toLocale(Date|Time)?String\(/.test(line))
        .map(({ at }) => at));
    expect(offenders).toEqual([]);
  });

  it("returns a reason and no value when the service does not answer", async () => {
    const got = await load(host(["roomcare.read"], { board: new HostCallError({ kind: "unavailable", message: "down" }) }), "roomcare.read", "board");
    expect(got.ok).toBe(false);
    expect("value" in got).toBe(false);
    expect(!got.ok && got.failure.cause).toBe("unanswered");
  });

  it("refuses without a round trip, naming the capability, when this person was not granted it", async () => {
    const calls: { method: string }[] = [];
    const got = await load(host([], {}, calls as never), "roomcare.read", "board");
    expect(calls).toEqual([]);
    expect(!got.ok && [got.failure.cause, got.failure.capability]).toEqual(["forbidden", "roomcare.read"]);
  });

  it("does not turn a programming error into a sentence a person would believe", async () => {
    const broken = { ...host(["roomcare.read"]), call: () => Promise.reject(new TypeError("not a host failure")) };
    await expect(load(broken, "roomcare.read", "board")).rejects.toThrow(TypeError);
  });
});

describe("a widget that cannot read", () => {
  it("draws the reason where its figures were, and Try again draws the answer once there is one", async () => {
    let down = true;
    const h = host(["roomcare.read"]);
    const flaky = { ...h, call: (c: string, m: string, p?: unknown) => (down ? Promise.reject(new HostCallError({ kind: "unavailable", message: "down" })) : h.call(c, m, p)) };
    const holder = document.createElement("div");
    document.body.replaceChildren(holder);
    holder.append(await roomsReady(flaky));
    expect(holder.querySelector(".wfig")).toBeNull();
    expect(holder.textContent).toContain("Room Care did not answer in time");
    expect(holder.querySelector(".wf-open")?.textContent).toBe("Try again →");

    down = false;
    holder.querySelector<HTMLElement>(".wf-open")!.click();
    await settle();
    const counts = recorded<{ ready: number }>("widget-rooms-ready");
    expect(holder.querySelector(".wfig b")?.textContent).toBe(String(counts.ready));
  });

  it("offers no Try again on a refusal — asking again cannot change it — and opens Room Care instead", async () => {
    const card = await roomsReady(host([]));
    expect(card.querySelector(".wf-open")?.textContent).toBe("Open Room Care →");
    expect(card.textContent).toContain("You do not have access to today's departures");
  });
});

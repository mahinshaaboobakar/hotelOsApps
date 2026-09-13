/**
 * A stand-in host for the module's tests — the recorded answers of Coral Cove's
 * morning (written by the backend's DriveRecording from the real service), the
 * property's zone and locale, and a log of every call the module made.
 *
 * It refuses a method it has no answer for, rather than inventing one: a test
 * that reached an unrecorded read should fail loudly (page 64 §8).
 */

import { readFileSync } from "node:fs";
import { join } from "node:path";

import { HostCallError, type HostApi } from "@hotelos/sdk";

const FILES: Record<string, string> = {
  me: "me", board: "board", prepare: "prepare", attendants: "attendants", states: "states", supervision: "supervision",
  deepCleans: "deep-cleans", setup: "setup", services: "services", zones: "zones", areas: "areas", deepCleanPlan: "deep-clean-plan",
  grants: "grants", myRooms: "my-rooms", door: "door-g01", widgetRoomsReady: "widget-rooms-ready", widgetArrivals: "widget-arrivals",
  widgetAttention: "widget-attention", widgetAttendants: "widget-attendants", widgetPending: "widget-pending",
};

export const SUPERVISOR = ["roomcare.read", "roomcare.assign", "roomcare.amend", "roomcare.configure", "roomcare.plan"];

export const ATTENDANT = ["roomcare.read", "room.clean"];

export function recorded<T>(file: string): T {
  return JSON.parse(readFileSync(join(process.cwd(), "preview", "recorded", `${file}.json`), "utf8")) as T;
}

export interface Call {
  capability: string;
  method: string;
  params: unknown;
}

export function host(capabilities: readonly string[], overrides: Record<string, unknown> = {}, calls: Call[] = []): HostApi {
  return {
    identity: { id: "roomcare", version: "0.1.0", capabilities },
    property: { timezone: "Asia/Kolkata", locale: "en-GB" },
    call: (capability, method, params) => {
      calls.push({ capability, method, params });
      if (method in overrides) {
        const answer = overrides[method];
        return answer instanceof Error ? Promise.reject(answer) : Promise.resolve(answer);
      }
      if (capability !== "roomcare.read") return Promise.resolve({ ok: true });
      if (method === "room") return Promise.resolve(recorded((params as { roomId: string }).roomId === "L09" ? "room-l09" : "room-l09"));
      const file = FILES[method];
      return file === undefined
        ? Promise.reject(new HostCallError({ kind: "unavailable", message: `no recorded answer for ${capability}/${method}` }))
        : Promise.resolve(recorded(file));
    },
    on: () => () => {},
  };
}

export async function settle(): Promise<void> {
  for (let i = 0; i < 4; i += 1) await new Promise((done) => setTimeout(done, 0));
}

export function mount(activate: (h: HostApi) => { mount(root: HTMLElement): void }, h: HostApi): HTMLElement {
  // Each test starts on the property's default view, not on the last test's choice.
  globalThis.localStorage?.clear();
  const root = document.createElement("div");
  document.body.replaceChildren(root);
  activate(h).mount(root);
  return root;
}

export function click(root: ParentNode, selector: string, text: string): void {
  for (const node of Array.from(root.querySelectorAll<HTMLElement>(selector))) {
    if (node.textContent?.includes(text) === true) {
      node.click();
      return;
    }
  }
  throw new Error(`no ${selector} reading "${text}"`);
}

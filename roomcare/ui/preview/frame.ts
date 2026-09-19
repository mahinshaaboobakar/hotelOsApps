/**
 * The capture harness — Room Care's module in a page that stands in for the
 * shell's realm, driven to one screen by `?screen=`, on the answers the real
 * service gave for Coral Cove's morning (DriveRecording). It exists for the
 * frame-beside-capture audit against the locked mockups.
 *
 * It refuses rather than renders a stand-in (page 64 §8): an unrecorded read
 * fails the call, and a drive step that matched nothing is written across the
 * top of the capture, so a capture that was not driven to its screen cannot be
 * mistaken for one that was.
 *
 * `?fail=<cause>` makes reads fail in each of the three ways the platform has —
 * `unanswered`, `forbidden`, `faulted` — through the host's own error kinds, so
 * the real `load` classifies them: every widget read on `?screen=widgets`, or the
 * one read named by `&at=<method>` on a screen. Without it the harness could only
 * ever show a timeout, which is the one state a missing answer produces.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { activate } from "../application";
import { arrivalsWaiting, attendantsNow, attention, pendingPolicy, roomsReady } from "../widgets/panel/panels";
import { stylesheet } from "../widgets/card";
import areas from "./recorded/areas.json";
import attendants from "./recorded/attendants.json";
import board from "./recorded/board.json";
import deepCleanPlan from "./recorded/deep-clean-plan.json";
import deepCleans from "./recorded/deep-cleans.json";
import door from "./recorded/door-g01.json";
import grants from "./recorded/grants.json";
import me from "./recorded/me.json";
import myRooms from "./recorded/my-rooms.json";
import prepare from "./recorded/prepare.json";
import roomG03 from "./recorded/room-g03.json";
import roomL09 from "./recorded/room-l09.json";
import services from "./recorded/services.json";
import setup from "./recorded/setup.json";
import states from "./recorded/states.json";
import supervision from "./recorded/supervision.json";
import widgetArrivals from "./recorded/widget-arrivals.json";
import widgetAttendants from "./recorded/widget-attendants.json";
import widgetAttention from "./recorded/widget-attention.json";
import widgetPending from "./recorded/widget-pending.json";
import widgetRoomsReady from "./recorded/widget-rooms-ready.json";
import zones from "./recorded/zones.json";

const params = new URLSearchParams(location.search);
const screen = params.get("screen") ?? "board";
const attendant = screen === "myrooms" || screen.startsWith("door");
const ANSWERS: Record<string, unknown> = {
  me: attendant ? { ...me, name: "Anita Pillai" } : me, board, prepare, attendants, states, supervision, deepCleans, setup, services, zones, areas,
  deepCleanPlan, grants, myRooms, door, widgetRoomsReady, widgetArrivals, widgetAttention, widgetAttendants, widgetPending,
};
const L09 = (roomL09 as { line: { id: string } }).line.id;

/** The host kind each cause is produced by — the platform's, so `load`'s own mapping decides. */
const KINDS = { unanswered: "unavailable", forbidden: "forbidden", faulted: "internal" } as const;
const fail = params.get("fail") as keyof typeof KINDS | null;
const failing = (method: string): boolean =>
  fail !== null && (params.get("at") === null ? screen === "widgets" && method.startsWith("widget") : params.get("at") === method);

const host: HostApi = {
  identity: { id: "roomcare", version: "0.1.0", capabilities: attendant ? ["roomcare.read", "room.clean"] : ["roomcare.read", "roomcare.assign", "roomcare.amend", "roomcare.configure", "roomcare.plan"] },
  property: { timezone: "Asia/Kolkata", locale: "en-GB" },
  call(capability, method, body) {
    if (failing(method)) return Promise.reject(new HostCallError({ kind: KINDS[fail!], message: `the harness failed ${method} as ${fail}` }));
    if (method === "room") return Promise.resolve((body as { roomId: string }).roomId === L09 ? roomL09 : roomG03);
    const answer = ANSWERS[method];
    return answer === undefined
      ? Promise.reject(new HostCallError({ kind: "unavailable", message: `the harness holds no answer for ${capability}/${method}` }))
      : Promise.resolve(answer);
  },
  on: () => () => {},
};

const missed: string[] = [];
const settle = (): Promise<void> => new Promise((done) => setTimeout(done, 30));

function click(selector: string, text: string): void {
  const node = [...document.querySelectorAll<HTMLElement>(selector)].find((n) => n.textContent?.includes(text) === true);
  if (node === undefined) missed.push(`${selector} containing "${text}"`);
  else node.click();
}

async function drive(): Promise<void> {
  localStorage.clear();
  if (screen === "widgets") {
    const grid = document.createElement("div");
    grid.style.cssText = "display:grid;grid-template-columns:repeat(3,360px);gap:18px;padding:18px";
    for (const panel of [roomsReady, arrivalsWaiting, attention, attendantsNow, pendingPolicy]) {
      const holder = document.createElement("div");
      holder.style.cssText = "border:1px solid var(--color-line);border-radius:14px;overflow:hidden";
      holder.append(stylesheet(), await panel(host));
      grid.append(holder);
    }
    document.body.append(grid);
    return;
  }

  const root = document.createElement("div");
  document.body.append(root);
  activate(host).mount(root);
  await settle();
  const steps: Record<string, (() => void)[]> = {
    wall: [() => click("button.chip", "Wall")],
    room: [() => click("button.tile", "L09")],
    "room-g03": [() => click("button.tile", "G03")],
    "room-state": [() => click("button.tile", "L09"), () => click("button", "Room state…")],
    prepare: [() => click("button.tab", "Prepare")],
    sheet: [() => click("button.tab", "Room states")],
    grid: [() => click("button.tab", "Room states"), () => click("button.chip", "Tap grid")],
    compact: [() => click("button.tab", "Room states"), () => click("button.chip", "Compact")],
    supervision: [() => click("button.tab", "Supervision")],
    deepclean: [() => click("button.tab", "Deep clean")],
    door: [() => click("tr.pick", "G01")],
    "door-end": [() => click("tr.pick", "G01"), () => click("button", "End…")],
  };
  const tab = params.get("tab");
  const setupSteps = screen === "setup" ? [() => click("button.tab", "Setup"), ...(tab === null ? [] : [() => click(".subnav button.tab", tab)])] : [];
  for (const step of [...(steps[screen] ?? []), ...setupSteps]) {
    step();
    await settle();
  }
}

void drive().then(() => {
  document.documentElement.setAttribute("data-ready", "true");
  if (missed.length > 0) {
    const note = document.createElement("div");
    note.setAttribute("data-missed", "true");
    note.style.cssText = "padding:10px 14px;font:13px system-ui;color:var(--color-bad)";
    note.textContent = `This capture was not driven to its screen: ${missed.join("; ")} matched nothing.`;
    document.body.prepend(note);
  }
});

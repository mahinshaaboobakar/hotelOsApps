/**
 * At the door — one room in the attendant's hands, and how a room ends
 * (frame 3b; S5 c1). Done is the room clean, announced, in one commit; DND
 * keeps the room on the list with its re-check; nothing is ever just "not done".
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el } from "../../chrome/element";
import { clock } from "../../chrome/instant";
import { act, failed, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { actions, sheet } from "../../chrome/overlay";
import { lower, phase, service } from "../../chrome/words";
import { stateText, type MyRoom } from "./index";

interface Door {
  room: MyRoom;
  startedAt: string | null;
  minutesExpected: number;
  extraMinutes: number;
  minutesWorked: number;
  running: boolean;
  inspectionRule: string;
  phases: { sequence: number; phase: string; status: string }[];
  partialParts: string[];
}

export async function door(host: HostApi, body: HTMLElement, nav: Nav, taskId: string, back: () => void): Promise<void> {
  const got = await load<Door>(host, "door", { taskId });
  if (!got.ok) {
    body.append(control("btn sm", "‹ My rooms", back), failed("This room", got.because));
    return;
  }

  const v = got.value;
  const r = v.room;
  const said = el("p", "said");
  const run = (method: string, params: object) => async (): Promise<void> => {
    const done = await act(host, "room.clean", method, { taskId, ...params });
    if (done.ok) nav.show();
    else { said.className = "said bad"; said.textContent = done.because; }
  };

  const title = el("h2", undefined, `Room ${r.room} · ${service(r.service)}`);
  title.style.cssText = "margin:6px 0 4px;font-size:18px";
  const facts = el("div", "dim", [
    v.startedAt === null ? "not started" : `started ${clock(host, v.startedAt)}`,
    `${v.minutesExpected + v.extraMinutes} min expected`,
    r.soldAt === null ? null : `arrival ${clock(host, r.soldAt)}`,
    r.reduction,
  ].filter((x) => x !== null).join(" · "));
  const phases = el("div", undefined, `phases: ${v.phases.map((p) => `${phase(p.phase, r.service)}${p.status === "DONE" ? " ✓" : ""}`).join(" · ")}${v.inspectionRule === "NONE" ? "" : ` · inspect (${lower(v.inspectionRule)})`}`);
  const state = el("div", "note", stateText(host, r));

  const buttons = el("div", "row");
  buttons.style.margin = "12px 0";
  buttons.append(v.running ? control("btn", "Pause", () => void run("pause", {})()) : control("btn pri", v.startedAt === null ? "Start" : "Resume", () => void run("start", {})()));
  buttons.append(control("btn", "Ask for extra time…", () => extraTime(host, nav, taskId)), control("btn", "Found an issue…", () => issue(host, nav, taskId)), control("btn", "End…", () => end(host, nav, v)));
  body.append(control("btn sm", "‹ My rooms", back), title, facts, phases, state, buttons, said,
    el("p", "dim", "A restock line appears here when Inventory is installed at this property."));
}

function end(host: HostApi, nav: Nav, v: Door): void {
  const overlay = sheet(nav.frame, `Room ${v.room.room} — how did it go?`);
  let found = "DONE";
  const parts = new Set<string>();
  const choice = el("div", "row");
  const partsRow = el("div", "row");
  for (const [label, value] of [["Done", "DONE"], ["Partial", "PARTIAL"], ["Declined by guest", "DECLINED"], ["DND board", "DND"]] as const) {
    const button = control(value === found ? "btn chip on" : "btn chip", label, () => {
      found = value;
      choice.querySelectorAll("button").forEach((b) => b.classList.toggle("on", b === button));
      partsRow.hidden = found !== "PARTIAL";
    });
    choice.append(button);
  }
  for (const part of v.partialParts) {
    const box = el("input") as HTMLInputElement;
    box.type = "checkbox";
    box.addEventListener("change", () => (box.checked ? parts.add(part) : parts.delete(part)));
    const label = el("label");
    label.append(box, document.createTextNode(` ${part.toLowerCase()}`));
    partsRow.append(label);
  }
  partsRow.hidden = true;
  const linen = el("input") as HTMLInputElement;
  linen.type = "checkbox";
  const linenLabel = el("label");
  linenLabel.append(linen, document.createTextNode(" linen changed — resets the room's date"));
  const note = el("textarea", "field") as HTMLTextAreaElement;
  overlay.body.append(choice, partsRow, linenLabel, el("label", "lbl", "Note"), note,
    el("p", "dim", "Done makes the room clean, announced. DND keeps the room on your list with its re-check."));
  actions(overlay, "Confirm", () => void (async () => {
    const done = await act(host, "room.clean", "attempt", { taskId: v.room.taskId, found, partialDone: [...parts], note: note.value || null, linenChanged: linen.checked });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })());
}

function extraTime(host: HostApi, nav: Nav, taskId: string): void {
  const overlay = sheet(nav.frame, "Ask for extra time");
  const amount = el("input", "field") as HTMLInputElement;
  amount.type = "number";
  amount.value = "10";
  const why = el("textarea", "field") as HTMLTextAreaElement;
  overlay.body.append(el("label", "lbl", "Minutes"), amount, el("label", "lbl", "Why"), why);
  actions(overlay, "Ask", () => void (async () => {
    const done = await act(host, "room.clean", "extraTime", { taskId, minutes: Number(amount.value), reason: why.value || null });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })());
}

function issue(host: HostApi, nav: Nav, taskId: string): void {
  const overlay = sheet(nav.frame, "Found an issue");
  const note = el("textarea", "field") as HTMLTextAreaElement;
  overlay.body.append(el("label", "lbl", "What is wrong"), note,
    el("p", "dim", "Recorded on this room's task, never lost. It is published with a correlation id; Jobs, when installed, creates the job and the room shows its number."));
  actions(overlay, "Record the issue", () => void (async () => {
    const done = await act(host, "room.clean", "issue", { taskId, note: note.value });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })());
}

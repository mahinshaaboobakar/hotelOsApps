/**
 * At the door — one room in the attendant's hands, and how a room ends
 * (frame 3b; S5 c1). Done is the room clean, announced, in one commit; DND
 * keeps the room on the list with its re-check; nothing is ever just "not done".
 */

import type { HostApi } from "@hotelos/sdk";

import { failed } from "../../chrome/failure";
import { control, el } from "../../chrome/element";
import { clock } from "../../chrome/instant";
import { READ, act, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { whole } from "../../chrome/number";
import { actions, readyWhen, sheet } from "../../chrome/overlay";
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
  const got = await load<Door>(host, READ, "door", { taskId });
  if (!got.ok) {
    body.append(control("btn", "‹ My rooms", back), failed(host, got.failure, "this room", nav.show));
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

  const title = el("div", "row");
  title.style.gap = "14px";
  const heading = el("h2", undefined, `Room ${r.room} · ${service(r.service)}`);
  heading.style.cssText = "margin:0;font-size:18px";
  const facts = el("span", "mono", [
    v.startedAt === null ? "not started" : `started ${clock(host, v.startedAt)}`,
    `${whole(host, v.minutesExpected + v.extraMinutes)} min expected`,
    r.soldAt === null ? null : `arrival ${clock(host, r.soldAt)}`,
    r.reduction,
  ].filter((x) => x !== null).join(" · "));
  const running = r.state.kind === "IN_PROGRESS";
  title.append(heading, el("span", `pill p${Math.min(r.priority, 3)}`, whole(host, r.priority)), el("span", running ? "pill run" : "pill", running ? "IN PROGRESS" : stateText(host, r)), facts);
  const phases = el("div", "mono");
  phases.style.margin = "6px 0 0";
  phases.append(document.createTextNode("phases: "));
  v.phases.forEach((p, i) => {
    if (i > 0) phases.append(document.createTextNode(" · "));
    phases.append(p.status === "DONE" ? el("b", "said ok", `${phase(p.phase, r.service)} ✓`) : document.createTextNode(phase(p.phase, r.service)));
  });
  if (v.inspectionRule !== "NONE") phases.append(document.createTextNode(` · inspect (${lower(v.inspectionRule)})`));

  const buttons = el("div", "row");
  buttons.style.margin = "12px 0";
  buttons.append(v.running ? control("btn", "Pause", () => void run("pause", {})()) : control("btn", v.startedAt === null ? "Start" : "Resume", () => void run("start", {})()));
  buttons.append(el("span", "btn off", "Photo — the media service's to add"), control("btn", "Ask for extra time…", () => extraTime(host, nav, taskId)),
    control("btn", "Found an issue…", () => issue(host, nav, taskId)), control("btn pri", "End…", () => end(host, nav, v)));
  body.append(title, phases, buttons, said, el("p", "dim", "A restock line appears here when Inventory is installed at this property."));
  void back;
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
      choice.querySelectorAll("button").forEach((b) => { b.classList.toggle("on", b === button); b.setAttribute("aria-pressed", String(b === button)); });
      partsRow.style.display = found === "PARTIAL" ? "flex" : "none";
    });
    // The chosen ending is announced, not only coloured, as `chip()` does: a screen reader hears which is chosen.
    button.setAttribute("aria-pressed", String(value === found));
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
  partsRow.style.display = "none";
  partsRow.style.margin = "8px 0";
  const linen = el("input") as HTMLInputElement;
  linen.type = "checkbox";
  const linenLabel = el("label");
  linenLabel.append(linen, document.createTextNode(" linen changed — resets the room's date"));
  const note = el("textarea", "field") as HTMLTextAreaElement;
  const kv = el("div", "kv");
  kv.style.marginTop = "12px";
  kv.append(el("div", "k", "Linen"), linenLabel,
    el("div", "k", "Then"), el("div", undefined, `${v.room.room} becomes CLEAN, announced${v.inspectionRule === "NONE" ? "" : "; inspection requested"}`),
    el("div", "k", "Otherwise"), el("div", undefined, "a DND keeps the room on your list with its re-check; partial and declined are records too"),
    el("div", "k", "Recorded as"), el("div", undefined, "you, at the moment you confirm, with what you chose"));
  overlay.body.append(choice, partsRow, kv, el("label", "lbl", "Note"), note);
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
    el("p", "dim", "Recorded on this room's task, never lost. Jobs, when installed, creates the job and the room shows its number."));
  actions(overlay, "Record the issue", () => void (async () => {
    const done = await act(host, "room.clean", "issue", { taskId, note: note.value });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })());
  readyWhen(overlay, () => (note.value.trim() === "" ? "say what is wrong" : null));
}

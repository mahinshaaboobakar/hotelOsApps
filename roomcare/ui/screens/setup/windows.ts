/**
 * 7a · Windows & trigger — when the day is attempted, and who starts it
 * (S0's trigger, S5 c2). A window may cross midnight; nothing here is a UTC hour.
 */

import type { HostApi } from "@hotelos/sdk";

import { card } from "../../chrome/card";
import { el } from "../../chrome/element";
import { act } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { whole } from "../../chrome/number";
import { radio, refuse, saveLine, toggle } from "./controls";
import type { SetupData } from "./index";

const LABEL: Record<string, string> = { MORNING: "Morning — departures, daily service, refresh", EVENING: "Turndown — evening" };

export function windows(host: HostApi, body: HTMLElement, nav: Nav, data: SetupData): void {
  const table = el("table");
  const head = el("tr");
  for (const name of ["Window", "On", "From", "To", "Assign outside it"]) head.append(el("th", undefined, name));
  table.append(head);
  const rows = data.windows.map((w) => {
    const on = toggle(w.enabled, "On");
    const from = time(w.starts);
    const to = time(w.ends);
    const outside = toggle(w.allowAssignmentOutside, "Assign outside it");
    const tr = el("tr");
    const cells = [el("td", undefined, LABEL[w.window] ?? w.window), el("td"), el("td"), el("td"), el("td")];
    cells[1]!.append(on);
    cells[2]!.append(from);
    cells[3]!.append(to);
    cells[4]!.append(outside);
    if (w.allowAssignmentOutside) cells[4]!.append(document.createTextNode("the supervisor may, for an arrival before it opens"));
    tr.append(...cells);
    table.append(tr);
    return { w, on, from, to, outside };
  });
  const count = el("div", "count");
  count.append(el("span", undefined, `${whole(host, data.windows.length)} of ${whole(host, data.windows.length)} — the two windows a hotel has; a window may cross midnight (a night shift is 22:00 → 06:00, not two ranges)`));
  const windowsCard = card("Windows", table, count,
    el("div", "mono aside", "A room is attempted in each window it is due in; outside a window nothing is attempted — and nothing is dropped: it is collected (S5 c2, S0)."));

  let trigger = data.policy.triggerMode;
  const choose = (value: string): void => { trigger = value; };
  const kv = el("div", "kv");
  kv.style.marginTop = "12px";
  const rerun = el("div");
  rerun.append(document.createTextNode("adds new rooms · updates open "), el("i", undefined, "unstarted"), document.createTextNode(" tasks whose facts changed · never takes a room off an attendant · never reshuffles what the supervisor accepted"));
  kv.append(el("div", "k", "HosPilot"), el("div", undefined, "always on — not a mode: it presses the button as the person asking (S7)"),
    el("div", "k", "Re-run after the button"), rerun,
    el("div", "k", "Changes between presses"), el("div", undefined, "counted on the board — \"N new since 08:00\" — created by nobody"));
  const triggerCard = card("Trigger — who prepares the day",
    radio("trigger", "PREPARE", trigger === "PREPARE", "Prepare by button or HosPilot", "(default) — changes are collected; the housekeeping manager presses Prepare the day when the window starts; later presses add and never remove", choose),
    radio("trigger", "AUTOMATIC", trigger === "AUTOMATIC", "Automatic", "— the decision runs on every change and places the work in the next window", choose),
    kv);

  const cols = el("div", "cols");
  cols.append(windowsCard, triggerCard);
  const { line, said } = saveLine(host, data, () => void (async () => {
    for (const r of rows) {
      const done = await act(host, "roomcare.configure", "saveWindow", { window: r.w.window, starts: r.from.value, ends: r.to.value, enabled: r.on.checked, allowAssignmentOutside: r.outside.checked, version: r.w.version });
      if (!done.ok) return refuse(said, done.because);
    }
    const done = await act(host, "roomcare.configure", "savePolicy", { triggerMode: trigger, version: data.policy.version });
    if (!done.ok) return refuse(said, done.because);
    nav.show();
  })(), nav.show);
  body.append(cols, line);
}

function time(value: string): HTMLInputElement {
  const input = el("input", "inline") as HTMLInputElement;
  input.type = "time";
  input.value = value;
  return input;
}

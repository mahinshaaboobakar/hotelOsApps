/**
 * 7f · Deep clean plan — how often each room type is taken out and
 * deep-cleaned (S0). The plan sets "due"; the window is picked per room on the
 * Deep clean tab. Nothing here schedules by weekday.
 */

import type { HostApi } from "@hotelos/sdk";

import { card } from "../../chrome/card";
import { failed } from "../../chrome/failure";
import { el } from "../../chrome/element";
import { READ, act, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { whole } from "../../chrome/number";
import { inlineNumber, refuse, saveLine } from "./controls";
import type { SetupData } from "./index";

interface Plan {
  rows: { roomTypeId: string; roomType: string; everyMonths: number | null; rooms: number; dueThisQuarter: number; version: number }[];
}

export async function plan(host: HostApi, body: HTMLElement, nav: Nav, data: SetupData): Promise<void> {
  const got = await load<Plan>(host, READ, "deepCleanPlan");
  if (!got.ok) {
    body.append(failed(host, got.failure, "the deep clean plan", nav.show));
    return;
  }

  const table = el("table");
  const head = el("tr");
  for (const name of ["Room type", "Every", "Rooms", "Due this quarter"]) head.append(el("th", undefined, name));
  table.append(head);
  const inputs = got.value.rows.map((row) => {
    const months = inlineNumber(row.everyMonths);
    const tr = el("tr");
    const every = el("td");
    every.style.whiteSpace = "nowrap";
    every.append(months, document.createTextNode(" months"));
    tr.append(el("td", undefined, row.roomType), every, el("td", "num", whole(host, row.rooms)),
      el("td", undefined, row.everyMonths === null ? "—" : whole(host, row.dueThisQuarter)));
    table.append(tr);
    return { row, months };
  });
  const count = el("div", "count", `${whole(host, got.value.rows.length)} of ${whole(host, got.value.rows.length)} room types · the plan sets "due"; the window is picked per room on the Deep clean tab, in low occupancy`);

  const kv = el("div", "kv");
  const work = el("div");
  work.append(document.createTextNode("a Jobs job — the hands change by day and shift in Jobs"));
  const room = el("div");
  room.append(document.createTextNode("out of order for the window"));
  kv.append(el("div", "k", "The work"), work,
    el("div", "k", "The room"), room,
    el("div", "k", "Return to sale"), el("div", undefined, "departure clean + inspection when its app is installed → clean again → release requested"),
    el("div", "k", "Typical length"), el("div", undefined, "informational, never a promise"));

  const cols = el("div", "cols");
  cols.append(card("The plan — per room type", table, count), card("What a deep clean is, at this property", kv));
  const { line, said } = saveLine(host, data, () => void (async () => {
    for (const i of inputs) {
      if (i.months.value === "" || Number(i.months.value) === i.row.everyMonths) continue;
      const done = await act(host, "roomcare.configure", "saveDeepCleanPlan", { roomTypeId: i.row.roomTypeId, everyMonths: Number(i.months.value), version: i.row.version });
      if (!done.ok) return refuse(said, `${i.row.roomType}: ${done.because}`);
    }
    nav.show();
  })(), nav.show, [cols]);
  body.append(cols, line);
}

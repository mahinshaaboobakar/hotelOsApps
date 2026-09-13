/**
 * 7f · Deep clean plan — how often each room type is taken out and
 * deep-cleaned (S0). The plan sets "due"; the window is picked per room on the
 * Deep clean tab.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el } from "../../chrome/element";
import { act, failed, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";

interface Plan {
  rows: { roomTypeId: string; roomType: string; everyMonths: number | null; rooms: number; dueThisQuarter: number; version: number }[];
}

export async function plan(host: HostApi, body: HTMLElement, nav: Nav): Promise<void> {
  const got = await load<Plan>(host, "deepCleanPlan");
  if (!got.ok) {
    body.append(failed("The deep clean plan", got.because));
    return;
  }

  const said = el("p", "said");
  const table = el("table");
  const head = el("tr");
  for (const name of ["Room type", "Every", "Typical length", "Rooms", "Due this quarter"]) head.append(el("th", undefined, name));
  table.append(head);
  const inputs = got.value.rows.map((row) => {
    const months = el("input", "cell") as HTMLInputElement;
    months.type = "number";
    months.value = row.everyMonths === null ? "" : String(row.everyMonths);
    const tr = el("tr");
    const every = el("td");
    every.append(months, document.createTextNode(" months"));
    tr.append(el("td", undefined, row.roomType), every, el("td", "dim", "— from the last jobs of this type, once Jobs publishes them"), el("td", "num", String(row.rooms)),
      el("td", "num", row.everyMonths === null ? "—" : String(row.dueThisQuarter)));
    table.append(tr);
    return { row, months };
  });

  const card = el("section", "card");
  card.style.marginTop = "14px";
  const kv = el("div", "kv");
  const work = el("div");
  work.append(document.createTextNode("a Jobs job, raised by Room Care with a correlation id; the hands change by day and shift in Jobs "), el("span", "tag port", "JOBS-Q2"));
  kv.append(el("div", "k", "The work"), work,
    el("div", "k", "The room"), el("div", undefined, "out of order for the window — requested by Room Care, placed by the state's owner"),
    el("div", "k", "Return to sale"), el("div", undefined, "departure clean → clean again → release requested"));
  card.append(el("h3", undefined, "What a deep clean is, at this property"), kv);

  const foot = el("div", "row");
  foot.style.marginTop = "14px";
  foot.append(control("btn pri", "Save", () => void (async () => {
    for (const i of inputs) {
      if (i.months.value === "" || Number(i.months.value) === i.row.everyMonths) continue;
      const done = await act(host, "roomcare.configure", "saveDeepCleanPlan", { roomTypeId: i.row.roomTypeId, everyMonths: Number(i.months.value), version: i.row.version });
      if (!done.ok) { said.className = "said bad"; said.textContent = `${i.row.roomType}: ${done.because}`; return; }
    }
    nav.show();
  })()), control("btn", "Discard", nav.show));
  body.append(table, el("div", "legend", `${got.value.rows.length} of ${got.value.rows.length} room types · the plan sets due; the window is picked per room on the Deep clean tab`), card, foot, said);
}

/**
 * 7e · Areas — Master Data's public nodes and each one's routine, paged at
 * twelve (S3). The areas are read, never created here; a spill or a one-off is a
 * Jobs request, never a routine.
 */

import type { HostApi } from "@hotelos/sdk";

import { pager } from "../../chrome/bar";
import { control, el } from "../../chrome/element";
import { act, failed, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { actions, sheet } from "../../chrome/overlay";
import { lower } from "../../chrome/words";
import type { Paging } from "../../model";

interface Area {
  locationId: string;
  name: string;
  kind: string;
  times: string[];
  minutes: number | null;
  enabled: boolean;
  version: number;
}

interface Areas {
  areas: number;
  withRoutine: number;
  rows: Area[];
  paging: Paging;
}

let page = 0;

export async function areas(host: HostApi, body: HTMLElement, nav: Nav): Promise<void> {
  const got = await load<Areas>(host, "areas", { page });
  if (!got.ok) {
    body.append(failed("Areas", got.because));
    return;
  }

  const v = got.value;
  const strip = el("div", "strip");
  const count = (n: number, label: string): HTMLElement => { const c = el("span"); c.append(el("b", undefined, String(n)), document.createTextNode(label)); return c; };
  strip.append(count(v.areas, "public areas at this property"), count(v.withRoutine, "with a routine"), count(v.areas - v.withRoutine, "without"),
    el("span", "end", "from Master Data's location tree · read, never created here"));

  const table = el("table", "list");
  const head = el("tr");
  for (const name of ["Area", "Kind", "Routine", "Minutes", "Inspection", ""]) head.append(el("th", undefined, name));
  table.append(head);
  for (const area of v.rows) {
    const tr = el("tr", area.times.length === 0 ? "dim" : "");
    const edit = el("td");
    edit.append(control("btn sm", area.times.length === 0 ? "Add a routine" : "Edit…", () => routine(host, nav, area)));
    tr.append(el("td", undefined, area.name), el("td", "mono", area.kind), el("td", undefined, area.times.length === 0 ? "no routine" : `at ${area.times.join(" · ")}${area.enabled ? "" : " · paused"}`),
      el("td", "num", area.minutes === null ? "" : String(area.minutes)), el("td", "dim", "none"), edit);
    table.append(tr);
  }
  body.append(strip, table, pager(v.paging, v.rows.length, "public areas", (p) => { page = p; nav.show(); }));
}

function routine(host: HostApi, nav: Nav, area: Area): void {
  const overlay = sheet(nav.frame, `${area.name} — its routine`);
  const times = el("input", "field") as HTMLInputElement;
  times.value = area.times.join(", ");
  times.setAttribute("aria-label", "Times");
  const minutes = el("input", "field") as HTMLInputElement;
  minutes.type = "number";
  minutes.value = String(area.minutes ?? 20);
  const on = el("input") as HTMLInputElement;
  on.type = "checkbox";
  on.checked = area.times.length === 0 || area.enabled;
  const onLabel = el("label");
  onLabel.append(on, document.createTextNode(" the routine runs"));
  overlay.body.append(el("p", "dim", `${lower(area.kind)} · each time becomes a task on the day it falls`),
    el("label", "lbl", "Times of day, HH:mm, comma-separated"), times, el("label", "lbl", "Minutes"), minutes, onLabel);
  actions(overlay, "Save", () => void (async () => {
    const list = times.value.split(",").map((t) => t.trim()).filter((t) => t.length > 0);
    const done = await act(host, "roomcare.configure", "saveArea", { locationId: area.locationId, times: list, minutes: Number(minutes.value), enabled: on.checked, version: area.version });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })());
}

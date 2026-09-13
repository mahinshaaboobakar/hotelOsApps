/**
 * 7e · Areas — Master Data's public nodes and each one's routine, paged at
 * twelve (S3). The areas are read, never created here; a spill or a one-off is a
 * Jobs request, never a routine. An area with no routine is drawn dim and
 * produces nothing.
 */

import type { HostApi } from "@hotelos/sdk";

import { chip, pager, scroller } from "../../chrome/bar";
import { control, el } from "../../chrome/element";
import { act, failed, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { actions, sheet } from "../../chrome/overlay";
import { lower } from "../../chrome/words";
import type { Paging } from "../../model";
import { inlineNumber, inlineSelect, refuse, saveLine } from "./controls";
import type { SetupData } from "./index";

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
let withoutRoutine = false;

export async function areas(host: HostApi, body: HTMLElement, nav: Nav, data: SetupData): Promise<void> {
  const got = await load<Areas>(host, "areas", { page, withoutRoutine });
  if (!got.ok) {
    body.append(failed("Areas", got.because));
    return;
  }

  const v = got.value;
  const strip = el("div", "strip");
  const count = (n: number, label: string): HTMLElement => { const c = el("span"); c.append(el("b", undefined, String(n)), document.createTextNode(label)); return c; };
  strip.append(count(v.areas, "public areas at this property"), count(v.withRoutine, "with a routine"), count(v.areas - v.withRoutine, "without"),
    el("span", "end", "from Master Data's location tree · read, never created here"));
  const chips = el("div", "chips");
  chips.append(chip("All", !withoutRoutine, () => { withoutRoutine = false; page = 0; nav.show(); }),
    chip("Without a routine", withoutRoutine, () => { withoutRoutine = true; page = 0; nav.show(); }));

  const table = el("table");
  const head = el("tr");
  for (const name of ["Area", "Kind", "Routine", "Minutes", "Inspection", ""]) head.append(el("th", undefined, name));
  table.append(head);
  const edits = v.rows.map((area) => {
    const has = area.times.length > 0;
    const tr = el("tr", has ? "" : "dim");
    const minutes = has ? inlineNumber(area.minutes) : null;
    const cells = [el("td", undefined, area.name), el("td", "mono", area.kind), el("td", undefined, has ? `at ${area.times.join(" · ")}${area.enabled ? "" : " · paused"}` : "no routine"), el("td"), el("td"), el("td")];
    if (minutes !== null) cells[3]!.append(minutes);
    if (has) cells[4]!.append(inlineSelect([["NONE", "none"]], "NONE"));
    cells[5]!.append(control("btn sm", has ? "Edit…" : "Add a routine…", () => routine(host, nav, area)));
    tr.append(...cells);
    table.append(tr);
    return { area, minutes };
  });

  const { line, said } = saveLine(host, data, () => void (async () => {
    for (const { area, minutes } of edits) {
      if (minutes === null || Number(minutes.value) === area.minutes) continue;
      const done = await act(host, "roomcare.configure", "saveArea", { locationId: area.locationId, times: area.times, minutes: Number(minutes.value), enabled: area.enabled, version: area.version });
      if (!done.ok) return refuse(said, `${area.name}: ${done.because}`);
    }
    nav.show();
  })(), nav.show);
  body.append(strip, chips, scroller(table), pager(v.paging, v.rows.length, withoutRoutine ? "public areas without a routine" : "public areas", (p) => { page = p; nav.show(); }), line);
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

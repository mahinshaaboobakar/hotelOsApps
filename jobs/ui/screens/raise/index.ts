/**
 * Raise a job — frame 3: where, what (alias-matched), asset, summary, details;
 * department and priority from the catalogue item, due from the policy, the
 * assignee list from today's roster, schedule for later, restricted.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { JOB_READ } from "../../chrome/permissions";
import { load, type Catalogue } from "../../board";
import { recordedCatalogue } from "../../board/recorded/catalogue";

export async function raise(host: HostApi, main: HTMLElement, onDone: () => void): Promise<void> {
  const catalogue = await load(host, JOB_READ, "catalogue", recordedCatalogue);
  const body = el("div", "body");
  body.append(el("div", "sect", "Raise a job"), fill(el("div", "cols"), left(catalogue.value), right(onDone)));
  main.replaceChildren(body);
}

function field(label: string, value: string, hint?: string, placeholder = false): HTMLElement {
  const wrap = el("div");
  wrap.append(el("label", "lbl", label));
  const box = el("div", placeholder ? "field ph" : "field", value);
  if (hint !== undefined) box.append(el("span", "hint mono", hint));
  wrap.append(box);
  return wrap;
}

function left(catalogue: Catalogue): HTMLElement {
  const item = catalogue.items[0];
  return fill(
    el("div"),
    field("Where", "Room 0817 · Floor 8 · Tower A"),
    field("What", `Lighting › Bedside lamp dead`, item === undefined ? undefined : `alias matched: "${item.aliases[0] ?? "lamp not working"}"`),
    field("Asset · optional", "Pick from Room 0817's assets…", undefined, true),
    field("Summary", "Guest says right-side bedside lamp is dead, bulb changed by HK, still dead."),
    field("Details · optional", "Anything the technician should know first", undefined, true),
  );
}

function right(onDone: () => void): HTMLElement {
  const restricted = fill(el("div", "row"), fill(el("span", "tog"), el("i")), el("span", "mono", "Restricted · off (catalogue default for this item)"));
  const actions = el("div", "row");
  actions.append(control("btn pri", "Raise MRN-ENG-143", onDone), control("btn", "Cancel", onDone));
  return fill(
    el("div"),
    field("Department", "Engineering (ENG)", "from the catalogue item"),
    field("Priority", "P3", "catalogue default · you may override"),
    field("Due", "Today 15:40", "policy: P3 within 60 min"),
    field("Assign to · on shift today", "AUTO — or pick: Arjun Menon · Deepak Rao · Team · Day shift", undefined, true),
    field("Schedule for later · optional", "Leave empty to raise now", undefined, true),
    restricted,
    actions,
  );
}

/**
 * 7d · Assignment & zones — the strategy, and which room is in which zone.
 * The zone a room is in is Room Care's (ADR 0044); the zones themselves are
 * Master Data's; who is posted to a zone is Workforce's. Nothing names a person.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, option } from "../../chrome/element";
import { day } from "../../chrome/instant";
import { act, failed, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { actions, sheet } from "../../chrome/overlay";
import { versionLine, type SetupData } from "./index";

interface Zones {
  strategy: string;
  zones: { zoneId: string; code: string; name: string; rooms: number; firstRoom: string | null; lastRoom: string | null; since: string | null }[];
  rooms: number;
  unzoned: number;
  roomsList: { roomId: string; number: string; zoneId: string | null }[];
}

export async function zones(host: HostApi, body: HTMLElement, nav: Nav, data: SetupData): Promise<void> {
  const got = await load<Zones>(host, "zones");
  if (!got.ok) {
    body.append(failed("Zones", got.because));
    return;
  }

  const v = got.value;
  const said = el("p", "said");
  const strategy = el("section", "card");
  const select = el("select", "cell") as HTMLSelectElement;
  for (const [value, label] of [["SAME_ZONE", "same zone, then lowest load"], ["LOWEST_LOAD", "lowest load"], ["CONTINUITY", "continuity — who did the room yesterday"]] as const) {
    select.append(option(label, value, v.strategy === value));
  }
  const kv = el("div", "kv");
  const here = el("div");
  here.append(document.createTextNode("Workforce's posting, read as it is announced "), el("span", "tag port", "Workforce ask · zone on the posting"));
  kv.append(el("div", "k", "Strategy"), select, el("div", "k", "Within"), el("div", undefined, "the shift's minutes are Workforce's — never a number set here"),
    el("div", "k", "If nobody fits"), el("div", undefined, "\"nobody available\" on the board and in the supervision lane — never silence"), el("div", "k", "Who is posted where"), here);
  strategy.append(el("h3", undefined, "Strategy — how the proposal matches rooms to the attendants on shift"), kv);

  const table = el("table");
  table.style.marginTop = "14px";
  const head = el("tr");
  for (const name of ["Zone", "Rooms", "Effective", ""]) head.append(el("th", undefined, name));
  table.append(head);
  for (const zone of v.zones) {
    const tr = el("tr");
    const edit = el("td");
    edit.append(control("btn sm", "Edit…", () => move(host, nav, v, zone.zoneId, zone.name)));
    tr.append(el("td", undefined, zone.name), el("td", undefined, zone.rooms === 0 ? "no rooms yet" : `${zone.firstRoom} – ${zone.lastRoom} · ${zone.rooms} rooms`),
      el("td", undefined, zone.since === null ? "—" : `since ${day(host, zone.since)}`), edit);
    table.append(tr);
  }

  const foot = el("div", "row");
  foot.style.marginTop = "14px";
  foot.append(control("btn pri", "Save", () => void (async () => {
    const done = await act(host, "roomcare.configure", "savePolicy", { assignmentStrategy: select.value, version: data.policy.version });
    if (done.ok) nav.show();
    else { said.className = "said bad"; said.textContent = done.because; }
  })()), control("btn", "Discard", nav.show), versionLine(host, data));
  body.append(strategy, table,
    el("div", "legend", `${v.zones.length} of ${v.zones.length} zones · ${v.rooms} rooms${v.unzoned > 0 ? ` · ${v.unzoned} in no zone yet — a room left out is grouped as "no zone yet"` : ", every room in exactly one zone"}`),
    foot, said);
}

function move(host: HostApi, nav: Nav, v: Zones, zoneId: string, name: string): void {
  const overlay = sheet(nav.frame, `${name} — which rooms belong to it`);
  const boxes = v.roomsList.map((room) => {
    const box = el("input") as HTMLInputElement;
    box.type = "checkbox";
    box.checked = room.zoneId === zoneId;
    const label = el("label");
    label.style.display = "inline-block";
    label.style.width = "84px";
    label.append(box, document.createTextNode(` ${room.number}`));
    overlay.body.append(label);
    return { room, box };
  });
  actions(overlay, "Move the ticked rooms here", () => void (async () => {
    const roomIds = boxes.filter((b) => b.box.checked && b.room.zoneId !== zoneId).map((b) => b.room.roomId);
    if (roomIds.length === 0) return overlay.refuse("tick at least one room that is not already in this zone");
    const done = await act(host, "roomcare.configure", "assignZone", { roomIds, zoneId });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })());
}

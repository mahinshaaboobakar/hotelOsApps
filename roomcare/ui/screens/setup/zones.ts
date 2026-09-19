/**
 * 7d · Assignment & zones — the strategy, and which room is in which zone.
 * The zone a room is in is Room Care's (ADR 0044); the zones themselves are
 * Master Data's; who is posted to a zone is Workforce's. Nothing names a person.
 *
 * The frame draws three controls — first, then, continuity — over the one
 * `assignment_strategy` word chapter 03 §2.8 holds: continuity on is
 * CONTINUITY, otherwise the first choice is the word, and "then" is always
 * lowest load.
 */

import type { HostApi } from "@hotelos/sdk";

import { card } from "../../chrome/card";
import { failed } from "../../chrome/failure";
import { control, el, option } from "../../chrome/element";
import { day } from "../../chrome/instant";
import { READ, act, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { actions, sheet } from "../../chrome/overlay";
import { inlineSelect, refuse, saveLine, toggle } from "./controls";
import type { SetupData } from "./index";

interface Zone {
  zoneId: string;
  code: string;
  name: string;
  rooms: number;
  firstRoom: string | null;
  lastRoom: string | null;
  since: string | null;
}

interface Zones {
  strategy: string;
  zones: Zone[];
  rooms: number;
  unzoned: number;
  roomsList: { roomId: string; number: string; zoneId: string | null }[];
}

export async function zones(host: HostApi, body: HTMLElement, nav: Nav, data: SetupData): Promise<void> {
  const got = await load<Zones>(host, READ, "zones");
  if (!got.ok) {
    body.append(failed(host, got.failure, "the zones", nav.show));
    return;
  }

  const v = got.value;
  const first = inlineSelect([["SAME_ZONE", "same zone"], ["LOWEST_LOAD", "lowest load"]], v.strategy === "LOWEST_LOAD" ? "LOWEST_LOAD" : "SAME_ZONE");
  const continuity = toggle(v.strategy === "CONTINUITY", "Continuity");
  const line = (control: HTMLElement, text: string): HTMLElement => { const d = el("div"); d.append(control, document.createTextNode(text)); return d; };
  const posted = el("div", "mono aside");
  posted.append(document.createTextNode("who is posted to which zone today is Workforce's posting, read through Context "), el("span", "tag port", "Workforce ask · zone on the posting"));
  const kv = el("div", "kv");
  kv.append(el("div", "k", "First"), line(first, " — an attendant gets rooms in the zone they are posted to"),
    el("div", "k", "Then"), line(inlineSelect([["LOWEST_LOAD", "lowest load"]], "LOWEST_LOAD"), " — the fewest minutes already planned"),
    el("div", "k", "Continuity"), line(continuity, "the attendant who did the room yesterday gets it again"),
    el("div", "k", "Within"), el("div", undefined, "the shift's minutes, from Workforce through Context — never a number set here"),
    el("div", "k", "If nobody fits"), el("div", undefined, "\"nobody available\" on the board and in the supervision lane — never silence (S0)"));
  const strategy = card("Strategy — how the proposal matches rooms to the attendants on shift", kv, posted);

  const table = el("table");
  const head = el("tr");
  for (const name of ["Zone", "Rooms", "Effective", ""]) head.append(el("th", undefined, name));
  table.append(head);
  for (const zone of v.zones) {
    const tr = el("tr");
    const edit = el("td");
    edit.append(control("btn sm", "Edit…", () => move(host, nav, v, zone.zoneId)));
    tr.append(el("td", undefined, zone.name), el("td", "num", zone.rooms === 0 ? "no rooms yet" : `${zone.firstRoom} – ${zone.lastRoom} · ${zone.rooms} rooms`),
      el("td", undefined, zone.since === null ? "—" : `since ${day(host, zone.since)}`), edit);
    table.append(tr);
  }
  const count = el("div", "count", `${v.zones.length} of ${v.zones.length} zones · ${v.rooms} rooms${v.unzoned > 0 ? ` · ${v.unzoned} in no zone yet — a room left out is grouped as "no zone yet"` : ", every room in exactly one zone; a room left out is a refusal, not a gap"}`);
  const moveRow = el("div", "row");
  moveRow.style.marginTop = "8px";
  moveRow.append(control("btn sm", "Move rooms between zones…", () => move(host, nav, v, v.zones[0]?.zoneId ?? "")));
  const title = el("span");
  title.append(document.createTextNode("Zones — which room belongs to which zone "), el("span", "mono", "(RoomZoneAssignment, ADR 0044 — Room Care's)"));
  const zonesCard = card(title, table, count, moveRow);

  const cols = el("div", "cols");
  cols.append(strategy, zonesCard);
  const { line: save, said } = saveLine(host, data, () => void (async () => {
    const word = continuity.checked ? "CONTINUITY" : first.value;
    const done = await act(host, "roomcare.configure", "savePolicy", { assignmentStrategy: word, version: data.policy.version });
    if (done.ok) nav.show();
    else refuse(said, done.because);
  })(), nav.show);
  body.append(cols, save);
}

function move(host: HostApi, nav: Nav, v: Zones, zoneId: string): void {
  const overlay = sheet(nav.frame, "Move rooms between zones");
  const target = el("select", "field") as HTMLSelectElement;
  for (const zone of v.zones) target.append(option(zone.name, zone.zoneId, zone.zoneId === zoneId));
  const boxes = v.roomsList.map((room) => {
    const box = el("input") as HTMLInputElement;
    box.type = "checkbox";
    const label = el("label");
    label.style.cssText = "display:inline-block;width:84px";
    label.append(box, document.createTextNode(` ${room.number}`));
    return { room, box, label };
  });
  const tick = (): void => boxes.forEach((b) => { b.box.checked = b.room.zoneId === target.value; });
  target.addEventListener("change", tick);
  tick();
  overlay.body.append(el("label", "lbl", "Into"), target, el("p", "dim", "Ticked rooms belong to this zone from today; the earlier assignment keeps its dates."), ...boxes.map((b) => b.label));
  actions(overlay, "Move the ticked rooms", () => void (async () => {
    const roomIds = boxes.filter((b) => b.box.checked && b.room.zoneId !== target.value).map((b) => b.room.roomId);
    if (roomIds.length === 0) return overlay.refuse("tick at least one room that is not already in this zone");
    const done = await act(host, "roomcare.configure", "assignZone", { roomIds, zoneId: target.value });
    if (!done.ok) return overlay.refuse(done.because);
    overlay.close();
    nav.show();
  })());
}

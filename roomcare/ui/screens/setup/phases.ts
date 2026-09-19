/**
 * 7b's two cards — the chosen service's phases in the property's order, and
 * copying one room type's numbers to others as a starting point. The v1 phases
 * are the five the owner named, so "Add a phase" is drawn off.
 */

import type { HostApi } from "@hotelos/sdk";

import { card } from "../../chrome/card";
import { control, el } from "../../chrome/element";
import { drawing } from "../../chrome/failure";
import { READ, act, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { whole } from "../../chrome/number";
import { actions, sheet } from "../../chrome/overlay";
import { phase, service } from "../../chrome/words";
import { reorderSheet } from "./controls";

export interface ServiceRow {
  service: string;
  minutes: number;
  credits: number;
  phases: string[];
  inspectionRule: string;
  checklistRef: string | null;
  version: number;
  saved: boolean;
}

export interface Services {
  roomTypes: { id: string; code: string; name: string; rooms: number }[];
  roomTypeId: string | null;
  services: ServiceRow[];
  inspectionApplicationInstalled: boolean;
}

const MEANS: Record<string, string> = {
  STRIP: "linen and towels out · rubbish · lost property · damage",
  CLEAN: "bathroom · surfaces · floors",
  MAKE_UP: "fresh bed · restock to standard · set-up",
  DONE: "the attendant's done — the room is CLEAN, announced",
};

/** The chosen service's phases; reordering changes the row, and the tab's Save keeps it. */
export function phasesCard(host: HostApi, nav: Nav, row: ServiceRow, reordered: () => void): HTMLElement {
  const kv = el("div", "kv");
  row.phases.forEach((p, i) => {
    const means = el("div", undefined, MEANS[p] ?? "");
    if (p === "MAKE_UP") means.append(el("span", "tag absent", "Inventory"));
    kv.append(el("div", "k", `${whole(host, i + 1)} · ${capital(phase(p, row.service))}`), means);
  });
  if (row.service === "DEPARTURE_CLEAN") {
    const inspect = el("div");
    inspect.append(document.createTextNode("only if the rule says — requested from the inspection app"), el("span", "tag port", "RC-Q1(6)"));
    kv.append(el("div", "k", `${whole(host, row.phases.length + 1)} · Inspect`), inspect);
  }
  const buttons = el("div", "row");
  buttons.style.marginTop = "8px";
  buttons.append(control("btn sm", "Reorder…", () => reorder(host, nav, row, reordered)), el("span", "btn sm off", "Add a phase"));
  return card(`Phases — ${service(row.service)}`, kv, buttons);
}

/** A starting point for other room types — each keeps its own version after. */
export function copyCard(host: HostApi, nav: Nav, v: Services): HTMLElement {
  const others = v.roomTypes.filter((t) => t.id !== v.roomTypeId);
  const line = el("div", "row");
  line.style.marginTop = "8px";
  line.append(others.length === 0 ? el("span", "btn sm off", "Copy…") : control("btn sm", "Copy…", () => copy(host, nav, v)));
  return card("Copy this room type's numbers to…",
    el("div", "mono", others.length === 0 ? "this property has one room type" : `${others.map((t) => t.name).join(" · ")} — a starting point, edited per type after`), line);
}

function reorder(host: HostApi, nav: Nav, row: ServiceRow, reordered: () => void): void {
  reorderSheet(host, nav, `${service(row.service)} — the order of its phases`, row.phases, (p) => phase(p, row.service),
    "Done closes the work, so it stays last. The tab's Save keeps the new order as a new version.", (order) => {
      if (order.includes("DONE") && order[order.length - 1] !== "DONE") return "done closes the work — it stays the last phase";
      row.phases = order;
      reordered();
      return null;
    });
}

function copy(host: HostApi, nav: Nav, v: Services): void {
  const overlay = sheet(nav.frame, "Copy these numbers to other room types");
  const boxes = v.roomTypes.filter((t) => t.id !== v.roomTypeId).map((type) => {
    const box = el("input") as HTMLInputElement;
    box.type = "checkbox";
    const label = el("label", "radio");
    label.append(box, document.createTextNode(`${type.name} · ${whole(host, type.rooms)} rooms`));
    overlay.body.append(label);
    return { type, box };
  });
  overlay.body.append(el("p", "dim", "Minutes, credits, phases and the inspection rule of all four services, as saved. Each room type gets a new version of its own."));
  actions(overlay, "Copy", () => void (async () => {
    const chosen = boxes.filter((b) => b.box.checked);
    if (chosen.length === 0) return overlay.refuse("tick at least one room type");
    for (const { type } of chosen) {
      const target = await load<Services>(host, READ, "services", { roomTypeId: type.id });
      if (!target.ok) return overlay.refuse(`${type.name}: ${drawing(target.failure, "its numbers").said}. Nothing was copied to it.`);
      for (const s of v.services) {
        const version = target.value.services.find((t) => t.service === s.service)?.version ?? 0;
        const done = await act(host, "roomcare.configure", "saveService", {
          roomTypeId: type.id, service: s.service, minutes: s.minutes, credits: s.credits, inspectionRule: s.inspectionRule, phases: s.phases, version,
        });
        if (!done.ok) return overlay.refuse(`${type.name}, ${service(s.service)}: ${done.because}`);
      }
    }
    overlay.close();
    nav.show();
  })());
}

function capital(words: string): string {
  return words.charAt(0).toUpperCase() + words.slice(1);
}

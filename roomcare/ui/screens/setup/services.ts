/**
 * 7b · Services & minutes — per room type, what each service is, how long, and
 * whether it is inspected (S0; ADR 0044's row). With no inspection application
 * installed the rule offers none only and says why.
 */

import type { HostApi } from "@hotelos/sdk";

import { failed } from "../../chrome/failure";
import { chip } from "../../chrome/bar";
import { el, option } from "../../chrome/element";
import { act, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { phase, service } from "../../chrome/words";
import { inlineNumber, refuse, saveLine } from "./controls";
import type { SetupData } from "./index";
import { copyCard, phasesCard, type ServiceRow, type Services } from "./phases";

const WHAT: Record<string, string> = {
  DEPARTURE_CLEAN: "the guest checked out — full turnover",
  DAILY_SERVICE: "every occupied room, every day",
  TURNDOWN: "evening, occupied rooms",
  REFRESH: "a clean room unsold N days, or a day-use pickup",
};

let chosenType: string | null = null;
let chosenService = "DEPARTURE_CLEAN";

export async function services(host: HostApi, body: HTMLElement, nav: Nav, data: SetupData): Promise<void> {
  const got = await load<Services>(host, "services", { roomTypeId: chosenType });
  if (!got.ok) {
    body.append(failed(got.failure, "the services", nav.show));
    return;
  }

  const v = got.value;
  const typeName = v.roomTypes.find((t) => t.id === v.roomTypeId)?.name ?? "this room type";
  const chips = el("div", "tabline");
  for (const type of v.roomTypes) chips.append(chip(type.name, type.id === v.roomTypeId, () => { chosenType = type.id; nav.show(); }));
  chips.append(el("span", "mono", "room types are Master Data's; this tab holds Room Care's numbers for each"));

  const table = el("table");
  const head = el("tr");
  for (const name of ["Service", "Minutes", "Credits", "Phases, in order", "Inspection", "Checklist"]) head.append(el("th", undefined, name));
  table.append(head);
  const phaseCells = new Map<string, HTMLElement>();
  const cols = el("div", "cols");
  cols.style.marginTop = "16px";
  const drawPhases = (): void => {
    const row = v.services.find((s) => s.service === chosenService) ?? v.services[0];
    if (row === undefined) return;
    cols.replaceChildren(phasesCard(nav, row, () => { phaseCells.get(row.service)!.textContent = phaseLine(row); drawPhases(); }), copyCard(host, nav, v));
  };

  const inputs = v.services.map((s) => {
    const minutes = inlineNumber(s.minutes);
    const credits = inlineNumber(s.credits, undefined, "0.1");
    const inspection = el("select", "inline") as HTMLSelectElement;
    inspection.append(option("none", "NONE"));
    const tr = el("tr", s.service === chosenService ? "pick sel" : "pick");
    tr.addEventListener("click", (event) => {
      if ((event.target as HTMLElement).closest("input,select") !== null) return;
      chosenService = s.service;
      table.querySelectorAll("tr.sel").forEach((row) => row.classList.remove("sel"));
      tr.classList.add("sel");
      drawPhases();
    });
    const name = el("td");
    name.append(el("b", undefined, service(s.service)), el("div", "mono", WHAT[s.service] ?? ""));
    const phases = el("td", undefined, phaseLine(s));
    phaseCells.set(s.service, phases);
    const checklist = el("td", "dim", v.inspectionApplicationInstalled ? s.checklistRef ?? "—" : "—");
    if (!v.inspectionApplicationInstalled && s.service === "DEPARTURE_CLEAN") checklist.append(el("span", "tag absent", "inspection app"));
    const cells = [name, el("td"), el("td"), phases, el("td"), checklist];
    cells[1]!.append(minutes);
    cells[2]!.append(credits);
    cells[4]!.append(inspection);
    if (!v.inspectionApplicationInstalled && s.service === "DEPARTURE_CLEAN") cells[4]!.append(el("div", "mono", "none only — no inspection application installed"));
    tr.append(...cells);
    table.append(tr);
    return { s, minutes, credits, inspection };
  });
  drawPhases();

  const count = el("div", "count");
  count.append(el("span", undefined, `${v.services.length} of ${v.services.length} services for ${typeName} — the four a hotel has; deep clean is not a service, it is a project (Deep clean plan)`));

  const { line, said } = saveLine(host, data, () => void (async () => {
    for (const i of inputs) {
      const done = await act(host, "roomcare.configure", "saveService", {
        roomTypeId: v.roomTypeId, service: i.s.service, minutes: Number(i.minutes.value), credits: Number(i.credits.value),
        inspectionRule: i.inspection.value, phases: i.s.phases, version: i.s.version,
      });
      if (!done.ok) return refuse(said, `${service(i.s.service)}: ${done.because}`);
    }
    nav.show();
  })(), nav.show);
  body.append(chips, table, count, cols, line);
}

/** "strip → clean → make up → done", with inspect when the rule asks for it. */
function phaseLine(s: ServiceRow): string {
  const words = s.phases.map((p) => phase(p, s.service));
  if (s.inspectionRule !== "NONE" && !s.phases.includes("INSPECT")) words.push("inspect");
  return words.join(" → ");
}

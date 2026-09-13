/**
 * 7b · Services & minutes — per room type, what each service is, how long, and
 * whether it is inspected (S0; ADR 0044's row). With no inspection application
 * installed the rule offers none only and says why.
 */

import type { HostApi } from "@hotelos/sdk";

import { chip } from "../../chrome/bar";
import { control, el, option } from "../../chrome/element";
import { act, failed, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { phase, service } from "../../chrome/words";
import { versionLine, type SetupData } from "./index";

interface Services {
  roomTypes: { id: string; code: string; name: string; rooms: number }[];
  roomTypeId: string | null;
  services: { service: string; minutes: number; credits: number; phases: string[]; inspectionRule: string; checklistRef: string | null; version: number; saved: boolean }[];
  inspectionApplicationInstalled: boolean;
}

const WHAT: Record<string, string> = {
  DEPARTURE_CLEAN: "the guest checked out — full turnover",
  DAILY_SERVICE: "every occupied room, every day",
  TURNDOWN: "evening, occupied rooms",
  REFRESH: "a clean room unsold N days, or a day-use pickup",
};

let chosenType: string | null = null;

export async function services(host: HostApi, body: HTMLElement, nav: Nav, data: SetupData): Promise<void> {
  const got = await load<Services>(host, "services", { roomTypeId: chosenType });
  if (!got.ok) {
    body.append(failed("Services", got.because));
    return;
  }

  const v = got.value;
  const said = el("p", "said");
  const chips = el("div", "chips");
  for (const type of v.roomTypes) chips.append(chip(type.name, type.id === v.roomTypeId, () => { chosenType = type.id; nav.show(); }));
  chips.append(el("span", "lbl", "room types are Master Data's; this tab holds Room Care's numbers for each"));

  const table = el("table");
  const head = el("tr");
  for (const name of ["Service", "Minutes", "Credits", "Phases, in order", "Inspection", "Checklist"]) head.append(el("th", undefined, name));
  table.append(head);
  const inputs = v.services.map((s) => {
    const minutes = el("input", "cell") as HTMLInputElement;
    minutes.type = "number";
    minutes.value = String(s.minutes);
    const credits = el("input", "cell") as HTMLInputElement;
    credits.type = "number";
    credits.step = "0.1";
    credits.value = String(s.credits);
    const inspection = el("select", "cell") as HTMLSelectElement;
    inspection.append(option("none", "NONE"));
    const tr = el("tr");
    const name = el("td");
    name.append(el("b", undefined, service(s.service)), el("div", "dim", WHAT[s.service] ?? ""));
    const cells = [name, el("td"), el("td"), el("td", undefined, s.phases.map((p) => phase(p, s.service)).join(" → ")), el("td"), el("td", "dim", v.inspectionApplicationInstalled ? s.checklistRef ?? "—" : "—")];
    cells[1]!.append(minutes);
    cells[2]!.append(credits);
    cells[4]!.append(inspection);
    if (!v.inspectionApplicationInstalled) cells[4]!.append(el("div", "dim", "no inspection application installed"));
    tr.append(...cells);
    table.append(tr);
    return { s, minutes, credits, inspection };
  });

  const save = async (): Promise<void> => {
    for (const i of inputs) {
      const done = await act(host, "roomcare.configure", "saveService", {
        roomTypeId: v.roomTypeId, service: i.s.service, minutes: Number(i.minutes.value), credits: Number(i.credits.value),
        inspectionRule: i.inspection.value, phases: i.s.phases, version: i.s.version,
      });
      if (!done.ok) { said.className = "said bad"; said.textContent = `${service(i.s.service)}: ${done.because}`; return; }
    }
    nav.show();
  };
  const foot = el("div", "row");
  foot.style.marginTop = "14px";
  foot.append(control("btn pri", "Save", () => void save()), control("btn", "Discard", nav.show), versionLine(host, data));
  body.append(chips, table, el("div", "legend", `${v.services.length} of ${v.services.length} services for ${v.roomTypes.find((t) => t.id === v.roomTypeId)?.name ?? "this room type"} — deep clean is a project, not a service (Deep clean plan)`), foot, said);
}

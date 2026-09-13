/**
 * Prepare — the window's state, what changed since the last press and what the
 * next press would do with it, the proposal, and the button (frame 2; S0).
 * Between presses nothing is created, only counted.
 */

import type { HostApi } from "@hotelos/sdk";

import { pager } from "../../chrome/bar";
import { control, el } from "../../chrome/element";
import { clock, minutes } from "../../chrome/instant";
import { act, failed, holds, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { lower, service } from "../../chrome/words";
import type { Paging } from "../../model";

interface PrepareView {
  window: string | null;
  windowStarts: string | null;
  windowEnds: string | null;
  open: boolean;
  day: string;
  preparedAt: string | null;
  preparedBy: string | null;
  tasks: number;
  changesSince: number;
  triggerMode: string;
  at: string;
  changes: { at: string; roomId: string; room: string; source: string; what: string; soldAt: string | null; onNextPress: string }[];
  changesPaging: Paging;
  proposal: {
    strategy: string;
    people: { userId: string; name: string; rooms: number; roomNumbers: string[]; minutes: number; accepted: boolean }[];
    nobodyAvailable: { taskId: string; taskVersion: number; room: string; service: string; priority: number }[];
    proposed: number;
    candidates: number;
    zoneOnPosting: boolean;
  };
}

export async function prepare(host: HostApi, body: HTMLElement, nav: Nav, page: number, goPage: (page: number) => void): Promise<void> {
  const got = await load<PrepareView>(host, "prepare", { page });
  if (!got.ok) {
    body.append(failed("The day's preparation", got.because));
    return;
  }

  const v = got.value;
  const said = el("p", "said");
  const press = async (): Promise<void> => {
    const done = await act(host, "roomcare.assign", "prepare", { window: v.window });
    if (done.ok) nav.show();
    else { said.className = "said bad"; said.textContent = done.because; }
  };

  const strip = el("div", "strip");
  strip.append(
    el("span", undefined, `${lower(v.window ?? "no window")} ${clock(host, v.windowStarts)} – ${clock(host, v.windowEnds)} · ${v.open ? "open" : "closed"}`),
    el("span", undefined, v.preparedAt === null ? "not prepared yet" : `prepared ${clock(host, v.preparedAt)} by ${v.preparedBy ?? "the system"}`),
    el("span", undefined, `${v.tasks} tasks`),
    el("span", undefined, `${v.changesSince} changes since ${v.preparedAt === null ? "—" : clock(host, v.preparedAt)}`),
    el("span", undefined, `trigger: ${v.triggerMode === "AUTOMATIC" ? "automatic" : "Prepare by button / HosPilot"}`),
    el("span", "end", clock(host, v.at)),
  );

  const buttons = el("div", "row");
  buttons.style.marginBottom = "12px";
  if (holds(host, "roomcare.assign")) {
    if (v.preparedAt === null) buttons.append(control("btn pri", "Prepare the day", () => void press()));
    else {
      buttons.append(
        v.changesSince > 0 ? control("btn pri", `Add the new rooms (${v.changesSince})`, () => void press()) : el("span", "btn off", "Add the new rooms — nothing new since the last press"),
        el("span", "btn off", `Prepare the day — done ${clock(host, v.preparedAt)}`),
      );
    }
  }

  const changes = el("table", "list");
  const head = el("tr");
  for (const name of ["When", "Room", "What arrived", "On the next press"]) head.append(el("th", undefined, name));
  changes.append(head);
  for (const c of v.changes) {
    const row = el("tr", "pick");
    row.addEventListener("click", () => nav.openRoom(c.roomId));
    const what = c.soldAt === null ? c.what : `${c.what} · sold ${clock(host, c.soldAt)}`;
    row.append(el("td", "num", clock(host, c.at)), el("td", "num", c.room), el("td", undefined, what), el("td", undefined, c.onNextPress));
    changes.append(row);
  }

  body.append(strip, buttons, said, el("h3", "sect", `Changes since ${v.preparedAt === null ? "—" : clock(host, v.preparedAt)} — collected, nothing created`), proposal(host, nav, v), changes,
    pager(v.changesPaging, v.changes.length, "changes since the last press", goPage));
}

function proposal(host: HostApi, nav: Nav, v: PrepareView): HTMLElement {
  const p = v.proposal;
  const card = el("section", "card");
  card.style.marginBottom = "14px";
  card.append(el("h3", undefined, "The proposal"));
  const kv = el("div", "kv");
  kv.append(el("div", "k", "Strategy"), el("div", undefined, `${lower(p.strategy)} — the property's (Setup)`));
  for (const person of p.people) {
    kv.append(el("div", "k", person.name), el("div", undefined,
      `${person.rooms} rooms · ${person.roomNumbers.slice(0, 6).join(" ")}${person.roomNumbers.length > 6 ? " …" : ""} · ${minutes(person.minutes)} planned${person.accepted ? " · accepted" : ""}`));
  }
  if (p.nobodyAvailable.length > 0) {
    kv.append(el("div", "k", "Nobody available"), el("div", undefined, p.nobodyAvailable.map((n) => `${n.room} (${service(n.service)}, priority ${n.priority})`).join(" · ")));
  }
  const here = el("div");
  here.append(document.createTextNode(`from Workforce — ${p.candidates} posted to Housekeeping; grouped by department until the zone is on the posting `), el("span", "tag port", "Workforce ask · zone on the posting"));
  kv.append(el("div", "k", "Who is here"), here);
  card.append(kv);
  if (holds(host, "roomcare.assign") && p.proposed > 0) {
    const said = el("p", "said");
    const row = el("div", "row");
    row.style.marginTop = "12px";
    row.append(control("btn pri", "Accept the proposal", () => void (async () => {
      const done = await act(host, "roomcare.assign", "acceptProposal", { day: v.day, window: v.window });
      if (done.ok) nav.show();
      else { said.className = "said bad"; said.textContent = done.because; }
    })()));
    card.append(row, said);
  }
  return card;
}

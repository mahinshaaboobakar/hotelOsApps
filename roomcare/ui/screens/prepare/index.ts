/**
 * Prepare — the window's state, what changed since the last press and what the
 * next press would do with it, the proposal, and the button (frame 2; S0).
 * Between presses nothing is created, only counted.
 */

import type { HostApi } from "@hotelos/sdk";

import { card } from "../../chrome/card";
import { failed } from "../../chrome/failure";
import { pager } from "../../chrome/bar";
import { control, el } from "../../chrome/element";
import { clock, minutes, when } from "../../chrome/instant";
import { READ, act, holds, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { whole } from "../../chrome/number";
import { lower } from "../../chrome/words";
import type { Paging } from "../../model";
import { moveRooms } from "./move";

export interface PrepareView {
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
  const got = await load<PrepareView>(host, READ, "prepare", { page });
  if (!got.ok) {
    body.append(failed(host, got.failure, "the day's preparation", nav.show));
    return;
  }

  const v = got.value;
  const said = el("p", "said");
  const refuse = (because: string): void => { said.className = "said bad"; said.textContent = because; };
  const press = async (): Promise<void> => {
    const done = await act(host, "roomcare.assign", "prepare", { window: v.window });
    if (done.ok) nav.show();
    else refuse(done.because);
  };
  const since = v.preparedAt === null ? "—" : clock(host, v.preparedAt);

  const strip = el("div", "strip");
  const bold = (b: string, rest: string, lead = ""): HTMLElement => { const s = el("span"); s.append(document.createTextNode(lead), el("b", undefined, b), document.createTextNode(rest)); return s; };
  strip.append(
    bold(v.window === "EVENING" ? "Turndown" : "Morning", ` ${clock(host, v.windowStarts)} – ${clock(host, v.windowEnds)} · ${v.open ? "open" : "closed"}`),
    v.preparedAt === null ? el("span", undefined, "not prepared yet") : bold(since, v.preparedBy === null ? "" : ` by ${v.preparedBy}`, "prepared "),
    bold(whole(host, v.tasks), " tasks"),
    bold(whole(host, v.changesSince), ` changes since ${since}`),
    bold(v.triggerMode === "AUTOMATIC" ? "Automatic" : "Prepare by button / HosPilot", "", "trigger: "),
    el("span", "end", when(host, v.at)),
  );

  const changesCard = card(`Changes since ${since} — collected, nothing created`);
  const buttons = el("div", "row");
  buttons.style.marginBottom = "12px";
  if (holds(host, "roomcare.assign")) {
    if (v.preparedAt === null) buttons.append(control("btn pri", "Prepare the day", () => void press()));
    else {
      buttons.append(
        v.changesSince > 0 ? control("btn pri", `Add the new rooms (${whole(host, v.changesSince)})`, () => void press()) : el("span", "btn off", "Add the new rooms — nothing new"),
        control("btn", "Show what changed", () => changesCard.scrollIntoView({ block: "nearest" })),
        el("span", "btn off", `Prepare the day — done ${since}`),
      );
    }
  }

  const table = el("table");
  const head = el("tr");
  for (const name of ["When", "Room", "What arrived", "On the next press"]) head.append(el("th", undefined, name));
  table.append(head);
  for (const c of v.changes) {
    const row = el("tr", "pick");
    row.addEventListener("click", () => nav.openRoom(c.roomId));
    const what = c.soldAt === null ? c.what : `${c.what} · sold ${clock(host, c.soldAt)}`;
    row.append(el("td", "num", clock(host, c.at)), el("td", undefined, c.room), el("td", undefined, what), el("td", undefined, c.onNextPress));
    table.append(row);
  }
  changesCard.append(table, pager(host, v.changesPaging, v.changes.length, "changes since the last press", goPage));

  const grid = el("div", "cols");
  grid.append(changesCard, proposal(host, nav, v, refuse));
  body.append(strip, buttons, grid, said);
}

function proposal(host: HostApi, nav: Nav, v: PrepareView, refuse: (because: string) => void): HTMLElement {
  const p = v.proposal;
  const view = card(`The proposal — Housekeeping (${p.people.map((x) => x.name).join(" · ") || "nobody posted"})`);
  const kv = el("div", "kv");
  kv.append(el("div", "k", "Strategy"), el("div", undefined, `${lower(p.strategy)} — the property's (Setup)`));
  for (const person of p.people) {
    kv.append(el("div", "k", person.name), el("div", undefined,
      `${whole(host, person.rooms)} rooms · ${person.roomNumbers.slice(0, 6).join(" ")}${person.roomNumbers.length > 6 ? " …" : ""} · ${minutes(host, person.minutes)} planned${person.accepted ? " · accepted" : ""}`));
  }
  if (p.nobodyAvailable.length > 0) {
    const cell = el("div");
    for (const n of p.nobodyAvailable) {
      cell.append(el("span", "pill bad", n.room), document.createTextNode(" "));
      if (holds(host, "roomcare.assign")) cell.append(control("btn sm", "Assign anyway…", () => void moveRooms(host, nav, v, n.taskId)), document.createTextNode(" "));
    }
    kv.append(el("div", "k", "Nobody available"), cell);
  }
  const here = el("div");
  here.append(document.createTextNode(`from Workforce — ${whole(host, p.candidates)} posted to Housekeeping; grouped by department until the zone is on the posting `), el("span", "tag port", "Workforce ask · zone on the posting"));
  kv.append(el("div", "k", "Who is here"), here);
  view.append(kv);
  if (holds(host, "roomcare.assign")) {
    const row = el("div", "row");
    row.style.marginTop = "12px";
    if (p.proposed > 0) {
      row.append(control("btn pri", "Accept the proposal", () => void (async () => {
        const done = await act(host, "roomcare.assign", "acceptProposal", { day: v.day, window: v.window });
        if (done.ok) nav.show();
        else refuse(done.because);
      })()));
    }
    row.append(control("btn", "Move rooms…", () => void moveRooms(host, nav, v, null)));
    view.append(row);
  }
  return view;
}

/**
 * A room — its condition with its source, the disagreement, the day in order,
 * the decision, inspection, jobs; and the acts a person holds (frames 4, 4b).
 * This page is the target of the desk's link from GuestOps (S5 c12).
 */

import type { HostApi } from "@hotelos/sdk";

import { failed } from "../../chrome/failure";
import { subnav } from "../../chrome/bar";
import { control, el } from "../../chrome/element";
import { clock, shortDay, when } from "../../chrome/instant";
import { act, holds, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { conditionClass, lower, source } from "../../chrome/words";
import type { BoardRoom } from "../../model";
import { recordException, reassign, roomState } from "./acts";
import { cards, history, record } from "./cards";

export interface RoomPage {
  line: BoardRoom;
  type: string;
  zone: string;
  disagreement: { ours: string; oursSource: string; oursBy: string | null; oursAt: string; theirs: string; theirsSource: string; theirsAt: string } | null;
  facts: { occupancy: string; stayStatuses: string[]; soldAt: string | null; linenLastChangedOn: string | null; linenDueOn: string | null; deepCleanDueOn: string | null; daysWithoutService: number; supervisedSince: string | null };
  today: { at: string; kind: string; status: string | null; what: string; by: string | null }[];
  decision: { condition: string | null; occupancy: string | null; stayStatuses: string[]; soldAt: string | null; window: string | null; ruleVersion: number; reason: string | null; service: string; minutes: number; priority: number; inspectionRule: string; decidedBy: string; runBy: string | null } | null;
  inspection: { applicationInstalled: boolean; rule: string; requestedAt: string | null; answeredAt: string | null; result: string | null; note: string | null };
  jobs: { jobId: string; jobNumber: string | null; at: string; summary: string | null; kind: string; by: string | null }[];
  history: { day: string; services: string[]; outcomes: (string | null)[] }[];
  supervisionId: string | null;
  whoLeads: string;
}

let tab = "Today";

export async function room(host: HostApi, body: HTMLElement, nav: Nav, roomId: string): Promise<void> {
  const got = await load<RoomPage>(host, "room", { roomId });
  if (!got.ok) {
    body.append(failed(got.failure, "this room", nav.show));
    return;
  }

  const page = got.value;
  const line = page.line;
  const title = el("div", "row");
  title.style.gap = "14px";
  const heading = el("h2", undefined, `Room ${line.number} · ${page.type} · ${page.zone}`);
  heading.style.cssText = "margin:0;font-size:18px";
  const tone = conditionClass(line.condition) === "dirty" ? "bad" : conditionClass(line.condition) === "clean" ? "ok" : "run";
  title.append(heading, el("span", `pill ${tone}`, line.condition), el("span", "mono", `set by ${line.setBy ?? source(line.source)} · ${when(host, line.setAt)}`));
  if (line.marks.manual) title.append(el("span", "tag man", "manual"));
  if (page.disagreement !== null) title.append(el("span", "pill warn", `DISAGREEMENT · ${source(page.disagreement.theirsSource)} says ${page.disagreement.theirs} ${when(host, page.disagreement.theirsAt)}`));

  const f = page.facts;
  const facts = el("div", "mono", [
    lower(f.occupancy),
    f.soldAt !== null && line.marks.soldTonight ? `sold tonight, arrival ${clock(host, f.soldAt)}` : "not sold tonight",
    f.linenLastChangedOn !== null ? `linen last changed ${shortDay(host, f.linenLastChangedOn)} (due ${shortDay(host, f.linenDueOn)})` : "linen date not recorded",
    f.deepCleanDueOn !== null ? `deep clean due ${shortDay(host, f.deepCleanDueOn)}` : null,
  ].filter((x) => x !== null).join(" · "));
  facts.style.margin = "6px 0 0";

  const actions = el("div", "row");
  actions.style.margin = "12px 0 14px";
  if (holds(host, "roomcare.amend")) actions.append(control("btn", "Room state…", () => roomState(host, nav, line)));
  actions.append(el("span", "btn off", "Raise a job for this room… — Jobs decides who may"));
  if (holds(host, "roomcare.amend") && line.taskId !== null) actions.append(control("btn", "Record an exception…", () => recordException(host, nav, line)));
  const ended = ["DONE", "INSPECTION_REQUESTED", "READY", "PARTIAL", "ENDED"].includes(line.outcome.kind);
  if (holds(host, "roomcare.assign") && line.taskId !== null) actions.append(ended ? el("span", "btn off", "Reassign…") : control("btn", "Reassign…", () => void reassign(host, nav, line)));

  body.append(title, facts, actions, subnav(["Today", "History · 14 days", "Record"], tab, (t) => { tab = t; nav.show(); }));
  if (tab === "History · 14 days") body.append(history(host, page));
  else if (tab === "Record") body.append(record(host, page));
  else body.append(cards(host, page, page.disagreement === null ? null : disagreement(host, nav, page)));
}

function disagreement(host: HostApi, nav: Nav, page: RoomPage): HTMLElement {
  const d = page.disagreement!;
  const card = el("section", "card");
  card.style.cssText = "border-color:var(--color-brand,#818cf8);background:color-mix(in srgb, var(--color-brand,#818cf8) 6%, transparent)";
  card.append(el("div", "sect first", `Disagreement — ${page.whoLeads === "PMS" ? "the PMS leads" : "Room Care leads"} at this property (S4)`));
  const kv = el("div", "kv");
  kv.append(
    el("div", "k", "Ours"), el("div", undefined, `${d.ours} · ${d.oursBy ?? source(d.oursSource)} · ${when(host, d.oursAt)} (a deliberate act)`),
    el("div", "k", `The ${source(d.theirsSource)}`), el("div", undefined, `${d.theirs} · occurred ${when(host, d.theirsAt)} · newer than our act → flagged, not applied`),
    el("div", "k", "Clear as"),
  );
  const buttons = el("div", "row");
  const said = el("p", "said");
  const clear = (kept: string) => async (): Promise<void> => {
    const done = await act(host, "roomcare.amend", "clearDisagreement", { roomId: page.line.id, version: page.line.version, kept });
    if (done.ok) nav.show();
    else { said.className = "said bad"; said.textContent = done.because; }
  };
  if (holds(host, "roomcare.amend")) {
    buttons.append(control("btn", "Keep ours", () => void clear("OURS")()), control("btn", "Take theirs", () => void clear("THEIRS")()));
  }
  buttons.append(el("span", "dim", "— recorded: who, when, which side won"));
  kv.append(buttons);
  card.append(kv, said);
  return card;
}

/**
 * A room — its condition with its source, the disagreement, the day in order,
 * the decision, inspection, jobs; and the acts a person holds (frames 4, 4b).
 * This page is the target of the desk's link from GuestOps (S5 c12).
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el } from "../../chrome/element";
import { day as dayText, when } from "../../chrome/instant";
import { act, failed, holds, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { conditionClass, lower, service, source } from "../../chrome/words";
import type { BoardRoom } from "../../model";
import { outcome } from "../board/wall";
import { cards } from "./cards";
import { recordException, reassign, roomState } from "./acts";

export interface RoomPage {
  line: BoardRoom;
  type: string;
  zone: string;
  disagreement: { ours: string; oursSource: string; oursBy: string | null; oursAt: string; theirs: string; theirsSource: string; theirsAt: string } | null;
  facts: { occupancy: string; stayStatuses: string[]; soldAt: string | null; linenLastChangedOn: string | null; linenDueOn: string | null; deepCleanDueOn: string | null; daysWithoutService: number; supervisedSince: string | null };
  today: { at: string; kind: string; what: string; by: string | null }[];
  decision: { condition: string | null; occupancy: string | null; stayStatuses: string[]; soldAt: string | null; window: string | null; ruleVersion: number; reason: string | null; service: string; minutes: number; priority: number; inspectionRule: string; decidedBy: string; runBy: string | null } | null;
  inspection: { applicationInstalled: boolean; rule: string; requestedAt: string | null; answeredAt: string | null; result: string | null; note: string | null };
  jobs: { jobId: string; jobNumber: string | null; at: string; summary: string | null; kind: string; by: string | null }[];
  history: { day: string; services: string[]; outcomes: (string | null)[] }[];
  supervisionId: string | null;
  whoLeads: string;
}

export async function room(host: HostApi, body: HTMLElement, nav: Nav, roomId: string): Promise<void> {
  const got = await load<RoomPage>(host, "room", { roomId });
  if (!got.ok) {
    body.append(control("btn sm", "‹ Board", nav.back), failed("This room", got.because));
    return;
  }

  const page = got.value;
  const line = page.line;
  const top = el("div", "row");
  top.append(control("btn sm", "‹ Back", nav.back));
  const title = el("h2", undefined, `Room ${line.number} · ${page.type} · ${page.zone}`);
  title.style.margin = "6px 0 4px";
  title.style.fontSize = "18px";

  const condition = el("div", "row");
  condition.append(el("span", `pill ${conditionClass(line.condition) === "dirty" ? "bad" : conditionClass(line.condition) === "clean" ? "ok" : "run"}`, line.condition));
  condition.append(el("span", "dim", `set by ${line.setBy ?? source(line.source)} · ${when(host, line.setAt)}`));
  if (line.marks.manual) condition.append(el("span", "tag man", "manual"));
  if (page.disagreement !== null) {
    condition.append(el("span", "pill warn", `DISAGREEMENT · ${source(page.disagreement.theirsSource)} says ${page.disagreement.theirs} ${when(host, page.disagreement.theirsAt)}`));
  }

  const f = page.facts;
  const facts = el("div", "dim", [
    lower(f.occupancy),
    f.stayStatuses.length > 0 ? `stays ${f.stayStatuses.map(lower).join(", ")}` : null,
    f.soldAt !== null && line.marks.soldTonight ? `sold tonight, arrival ${when(host, f.soldAt)}` : "not sold tonight",
    f.linenLastChangedOn !== null ? `linen last changed ${dayText(host, f.linenLastChangedOn)} (due ${dayText(host, f.linenDueOn)})` : "linen date not recorded",
    f.deepCleanDueOn !== null ? `deep clean due ${f.deepCleanDueOn}` : null,
  ].filter((x) => x !== null).join(" · "));

  const actions = el("div", "row");
  actions.style.margin = "10px 0 14px";
  if (holds(host, "roomcare.amend")) actions.append(control("btn", "Room state…", () => roomState(host, nav, line)));
  actions.append(el("span", "btn off", "Raise a job for this room — Jobs decides who may"));
  if (holds(host, "roomcare.amend") && line.taskId !== null) actions.append(control("btn", "Record an exception…", () => recordException(host, nav, line)));
  if (holds(host, "roomcare.assign") && line.taskId !== null) actions.append(control("btn", "Reassign…", () => void reassign(host, nav, line)));

  body.append(top, title, condition, facts, actions);
  if (page.disagreement !== null) body.append(disagreement(host, nav, page));
  body.append(summary(host, line), cards(host, page));
}

function summary(host: HostApi, line: BoardRoom): HTMLElement {
  const today = el("div", "note");
  today.append(el("b", undefined, `${service(line.service)} · `), document.createTextNode(`${line.attendant ?? "unassigned"} · ${outcome(host, line)}`));
  return today;
}

function disagreement(host: HostApi, nav: Nav, page: RoomPage): HTMLElement {
  const d = page.disagreement!;
  const card = el("section", "card");
  card.style.marginBottom = "14px";
  card.append(el("h3", undefined, `Disagreement — ${page.whoLeads === "PMS" ? "the PMS leads" : "Room Care leads"} at this property (S4)`));
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
  buttons.append(el("span", "dim", "recorded: who, when, which side won"));
  kv.append(buttons);
  card.append(kv, said);
  return card;
}

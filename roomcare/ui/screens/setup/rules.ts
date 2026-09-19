/**
 * 7c · Rules — linen, towels, departure, stay facts, default views, who leads,
 * the ladder, unsold departures, refresh, DND, the supervisor (S4, S5). One
 * policy row, one version; a vocabulary of one word is drawn as that word.
 */

import type { HostApi } from "@hotelos/sdk";

import { card } from "../../chrome/card";
import { control, el } from "../../chrome/element";
import { act } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { whole } from "../../chrome/number";
import { inlineNumber, inlineSelect, radio, refuse, reorderSheet, saveLine, sentence } from "./controls";
import type { SetupData } from "./index";

const BAND: Record<string, string> = { SOLD_TONIGHT: "sold tonight", DEPARTURE: "departures, unsold", DAILY: "daily service, by zone", REFRESH: "refresh" };

export function rules(host: HostApi, body: HTMLElement, nav: Nav, data: SetupData): void {
  const p = data.policy;
  const edit: Record<string, unknown> = {};
  const set = (name: string) => (value: unknown): void => { edit[name] = value; };
  const aside = (text: string): HTMLElement => el("div", "mono aside", text);
  const sub = (text: string): HTMLElement => { const h = el("h3", undefined, text); h.style.marginTop = "14px"; return h; };

  const linen = card("Linen",
    sentence("radio", radio("linenRuleKind", "EVERY_N_DEFERRABLE", p.linenRuleKind === "EVERY_N_DEFERRABLE", "Every N nights, the guest may defer", "— N =", set("linenRuleKind")), inlineNumber(p.linenEveryDays, set("linenEveryDays"))),
    radio("linenRuleKind", "MUST_BY_N", p.linenRuleKind === "MUST_BY_N", "Must be changed by day N", "— no deferral; the door offers Done or Partial-with-linen only", set("linenRuleKind")),
    aside("counted on the ROOM — \"linen last changed\", reset by every departure clean; nights empty do not count (S5 c4, c11)"));

  const towels = card("Towels",
    radio("towels", "DAILY", p.towels === "DAILY", "Replace daily", "", set("towels")),
    radio("towels", "GREEN_PROGRAMME", p.towels === "GREEN_PROGRAMME", "Green programme", "— hung towels kept, floor towels replaced", set("towels")),
    aside("a property rule — the guest does not choose (S5 c5)"),
    sub("On departure"), sentence("radio", "the room becomes", inlineSelect([["DIRTY", "dirty"]], p.onDepartureCondition)),
    sub("Stay facts usually come from"),
    sentence("row", radio("staySource", "PMS", p.staySource === "PMS", "the PMS", "", set("staySource")), radio("staySource", "GUESTOPS", p.staySource === "GUESTOPS", "GuestOps", "(no PMS)", set("staySource")),
      radio("staySource", "MANUAL", p.staySource === "MANUAL", "entered here", "(neither)", set("staySource"))),
    aside("decides only what the board expects and warns about (\"PMS silent since\"); editing states by hand — the Room states tab — is available at every property regardless"),
    sub("Default views"),
    sentence("radio", "Board opens as", inlineSelect([["MAP", "Map"], ["WALL", "Wall"]], p.boardDefaultView, set("boardDefaultView")),
      " Room states opens as", inlineSelect([["SHEET", "Sheet"], ["TAP_GRID", "Tap grid"], ["COMPACT", "Compact"]], p.statesDefaultView, set("statesDefaultView"))),
    el("div", "mono", "the chip on each screen remembers a person's last choice on that desk"));

  const leads = card("Who decides a room's condition",
    radio("whoLeads", "ROOM_CARE", p.whoLeads === "ROOM_CARE", "Room Care leads", "(default) — attendants mark rooms in the app; a PMS change that disagrees is flagged for a supervisor", set("whoLeads")),
    radio("whoLeads", "PMS", p.whoLeads === "PMS", "The PMS / front desk leads", "— rooms change when the PMS says so; Room Care records and announces it", set("whoLeads")),
    aside("either way an observation is applied unless it contradicts a later act here (S4)"));

  let ladder = [...p.priorityLadder];
  const rungs = el("div", "kv");
  const drawLadder = (): void => { rungs.replaceChildren(...ladder.flatMap((band, i) => [el("div", "k", whole(host, i + 1)), el("div", undefined, BAND[band] ?? band)])); };
  drawLadder();
  const reorder = el("div", "row");
  reorder.style.marginTop = "8px";
  reorder.append(control("btn sm", "Reorder…", () => reorderSheet(host, nav, "The priority ladder", ladder, (band) => BAND[band] ?? band,
    "Sooner first. Each of the four bands stays on the ladder exactly once; the tab's Save keeps the order as a new version.",
    (order) => { ladder = order; edit.priorityLadder = order; drawLadder(); return null; })));
  const priority = card("Priority ladder", rungs, reorder, sub("Unsold departure"),
    sentence("row", radio("unsoldDeparture", "TODAY", p.unsoldDeparture === "TODAY", "clean today", "", set("unsoldDeparture")),
      radio("unsoldDeparture", "MAY_WAIT", p.unsoldDeparture === "MAY_WAIT", "may wait", "— the pending lane, one click promotes it (the charter)", set("unsoldDeparture"))));

  const refresh = card("Refresh",
    sentence("radio", "a clean vacant room unsold for", inlineNumber(p.refreshAfterDays, set("refreshAfterDays")), "days gets a refresh"),
    sub("DND at the door"),
    sentence("radio", "re-check every", inlineNumber(p.dndRecheckMinutes, set("dndRecheckMinutes")), "min, until", inlineSelect([["WINDOW_END", "the window ends"]], "WINDOW_END")),
    el("div", "mono", "turndown is a fresh attempt on a DND room (S5 c1)"));

  const supervisor = card("The supervisor steps in",
    sentence("radio", "after", inlineNumber(p.supervisorAfterDays, set("supervisorAfterDays")), "days without service — declined or DND, either counts (row 7)"),
    aside("on day N+1 the room is the supervisor's: a decision is required before the window closes on it, and every later DND day stays the supervisor's (S5 c9)"));

  const top = el("div", "cols3");
  top.append(linen, towels, leads);
  const bottom = el("div", "cols3");
  bottom.style.marginTop = "14px";
  bottom.append(priority, refresh, supervisor);
  const { line, said } = saveLine(host, data, () => void (async () => {
    const done = await act(host, "roomcare.configure", "savePolicy", { ...edit, version: p.version });
    if (done.ok) nav.show();
    else refuse(said, done.because);
  })(), nav.show);
  body.append(top, bottom, line);
}

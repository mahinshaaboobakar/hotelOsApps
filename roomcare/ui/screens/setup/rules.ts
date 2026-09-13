/**
 * 7c · Rules — linen, towels, departure, stay facts, default views, who leads,
 * the ladder, unsold departures, refresh, DND, the supervisor (S4, S5).
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el } from "../../chrome/element";
import { act } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { lower } from "../../chrome/words";
import { versionLine, type SetupData } from "./index";

export function rules(host: HostApi, body: HTMLElement, nav: Nav, data: SetupData): void {
  const p = data.policy;
  const edit: Record<string, unknown> = {};
  const said = el("p", "said");
  const grid = el("div", "cols");
  const left = el("div");
  const right = el("div");

  const radios = (name: string, current: string, options: readonly (readonly [string, string])[]): HTMLElement => {
    const box = el("div");
    for (const [value, text] of options) {
      const label = el("label");
      const radio = el("input") as HTMLInputElement;
      radio.type = "radio";
      radio.name = name;
      radio.checked = current === value;
      radio.addEventListener("change", () => { edit[name] = value; });
      label.append(radio, document.createTextNode(` ${text}`));
      box.append(el("div").appendChild(label).parentElement as HTMLElement);
    }
    return box;
  };
  const number = (name: string, value: number): HTMLElement => {
    const input = el("input", "cell") as HTMLInputElement;
    input.type = "number";
    input.value = String(value);
    input.addEventListener("change", () => { edit[name] = Number(input.value); });
    return input;
  };
  const card = (title: string, ...content: (Node | string)[]): HTMLElement => {
    const c = el("section", "card");
    c.style.marginBottom = "14px";
    c.append(el("h3", undefined, title), ...content);
    return c;
  };
  const sentence = (...parts: (Node | string)[]): HTMLElement => { const s = el("div"); s.append(...parts); return s; };

  left.append(
    card("Linen", radios("linenRuleKind", p.linenRuleKind, [["EVERY_N_DEFERRABLE", "Every N nights, the guest may defer"], ["MUST_BY_N", "Must be changed by day N — no deferral"]]),
      sentence("N = ", number("linenEveryDays", p.linenEveryDays)), el("p", "dim", "counted on the ROOM — linen last changed, reset by every departure clean (S5 c4, c11)")),
    card("Towels", radios("towels", p.towels, [["DAILY", "Replace daily"], ["GREEN_PROGRAMME", "Green programme — hung towels kept, floor towels replaced"]]), el("p", "dim", "a property rule — the guest does not choose (S5 c5)")),
    card("On departure", el("div", undefined, `the room becomes ${lower(p.onDepartureCondition)}`)),
    card("Stay facts usually come from", radios("staySource", p.staySource, [["PMS", "the PMS"], ["GUESTOPS", "GuestOps (no PMS)"], ["MANUAL", "entered here (neither)"]]),
      el("p", "dim", "decides only what the board expects and warns about; editing states by hand is available at every property regardless")),
    card("Default views", radios("boardDefaultView", p.boardDefaultView, [["MAP", "Board opens as Map"], ["WALL", "Board opens as Wall"]]),
      radios("statesDefaultView", p.statesDefaultView, [["SHEET", "Room states opens as Sheet"], ["TAP_GRID", "as Tap grid"], ["COMPACT", "as Compact"]])),
  );
  right.append(
    card("Who decides a room's condition", radios("whoLeads", p.whoLeads, [["ROOM_CARE", "Room Care leads (default) — a PMS change that disagrees is flagged for a supervisor"], ["PMS", "The PMS / front desk leads — Room Care records and announces it"]]),
      el("p", "dim", "either way an observation is applied unless it contradicts a later act here (S4)")),
    card("Priority ladder", el("ol", undefined, p.priorityLadder.map(lower).join(" · "))),
    card("Unsold departure", radios("unsoldDeparture", p.unsoldDeparture, [["TODAY", "clean today"], ["MAY_WAIT", "may wait — the pending lane, one click promotes it"]])),
    card("Refresh", sentence("a clean vacant room unsold for ", number("refreshAfterDays", p.refreshAfterDays), " days gets a refresh")),
    card("DND at the door", sentence("re-check every ", number("dndRecheckMinutes", p.dndRecheckMinutes), " min, until the window ends")),
    card("The supervisor steps in", sentence("after ", number("supervisorAfterDays", p.supervisorAfterDays), " days without service — declined or DND, either counts"),
      el("p", "dim", "then every later DND day on the room stays the supervisor's (S5 c9)")),
  );
  grid.append(left, right);

  const foot = el("div", "row");
  foot.append(control("btn pri", "Save", () => void (async () => {
    const done = await act(host, "roomcare.configure", "savePolicy", { ...edit, version: p.version });
    if (done.ok) nav.show();
    else { said.className = "said bad"; said.textContent = done.because; }
  })()), control("btn", "Discard", nav.show), versionLine(host, data));
  body.append(grid, foot, said);
}

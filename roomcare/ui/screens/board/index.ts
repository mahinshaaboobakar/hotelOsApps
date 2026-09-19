/**
 * The board — two views of one data set, the Map / Wall chip between them
 * (frames 1a, 1b; owner's redline 4, 2026-09-13). The whole house on one
 * screen, grouped by zone, no pages; filters dim, they never remove.
 */

import type { HostApi } from "@hotelos/sdk";

import { failed } from "../../chrome/failure";
import { chip } from "../../chrome/bar";
import { el, option } from "../../chrome/element";
import { clock, when } from "../../chrome/instant";
import { READ, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { remember, remembered } from "../../chrome/remember";
import type { Board, BoardRoom, Strip } from "../../model";
import { map } from "./map";
import { wall } from "./wall";

/** A filter: which rooms stay lit. */
export type Lit = (room: BoardRoom) => boolean;

const EVERY: Lit = () => true;

const FILTERS: readonly (readonly [string, Lit])[] = [
  ["Sold tonight", (r) => r.marks.soldTonight],
  ["Attention", (r) => r.marks.supervision || r.marks.disagreement || r.marks.pending || r.outcome.kind === "NOBODY_AVAILABLE"],
];

export async function board(host: HostApi, body: HTMLElement, nav: Nav): Promise<void> {
  const got = await load<Board>(host, READ, "board");
  if (!got.ok) {
    body.append(failed(host, got.failure, "the board", nav.show));
    return;
  }

  const data = got.value;
  let view = remembered("board.view", ["MAP", "WALL"], data.defaultView);
  let filter: string | null = null;
  let person: string | null = null;
  const collapsed = new Set<string>();
  const people = [...new Map(data.zones.flatMap((z) => z.rooms).filter((r) => r.attendant !== null).map((r) => [r.attendantId, r.attendant])).entries()];

  function lit(): Lit {
    const chosen = FILTERS.find(([label]) => label === filter)?.[1] ?? EVERY;
    return (room) => chosen(room) && (person === null || room.attendantId === person);
  }

  function redraw(): void {
    const chips = el("div", "chips");
    chips.append(
      chip("Map", view === "MAP", () => { view = "MAP"; remember("board.view", view); redraw(); }),
      chip("Wall", view === "WALL", () => { view = "WALL"; remember("board.view", view); redraw(); }),
      el("span", "lbl", "group by"),
      chip("Zone", true, () => {}),
    );
    chips.append(el("span", "grow"));
    for (const [label] of FILTERS) chips.append(chip(label, filter === label, () => { filter = filter === label ? null : label; redraw(); }));
    if (people.length > 0) {
      const who = el("select", person === null ? "btn chip" : "btn chip on") as HTMLSelectElement;
      who.setAttribute("aria-label", "One attendant's rooms");
      who.append(option("Any attendant", "", person === null));
      for (const [id, name] of people) {
        const short = `${(name ?? "").split(" ")[0]} ${(name ?? "").split(" ")[1]?.[0] ?? ""}.`.trim();
        who.append(option(short, id ?? "", person === id));
      }
      who.addEventListener("change", () => { person = who.value === "" ? null : who.value; redraw(); });
      chips.append(who);
    }
    if (view === "WALL") {
      chips.append(chip(collapsed.size === data.zones.length ? "Expand all" : "Collapse all", false, () => {
        if (collapsed.size === data.zones.length) collapsed.clear();
        else data.zones.forEach((z) => collapsed.add(z.name));
        redraw();
      }));
    }
    const house = view === "MAP" ? map(data, lit(), nav) : wall(host, data, lit(), collapsed, redraw, nav);
    body.replaceChildren(strip(host, data.strip), chips, house);
  }

  redraw();
}

/** The strip — the house in counts, and whether the PMS is speaking. */
export function strip(host: HostApi, s: Strip): HTMLElement {
  const line = el("div", "strip");
  const count = (value: number, label: string): HTMLElement => {
    const cell = el("span");
    cell.append(el("b", undefined, String(value)), document.createTextNode(label));
    return cell;
  };
  line.append(
    count(s.rooms, "rooms"), count(s.dirty, "dirty"), count(s.inProgress, "in progress"), count(s.ready, "ready"),
    count(s.pending, "pending"), count(s.blocked, "blocked"), count(s.supervision, "supervision"),
  );
  const pms = s.silentSince !== null
    ? `PMS silent since ${clock(host, s.silentSince)}`
    : s.lastFactAt !== null ? `PMS ok · last fact ${clock(host, s.lastFactAt)}` : "no PMS fact yet";
  line.append(el("span", "end", `${pms} · ${when(host, s.at)}`));
  return line;
}

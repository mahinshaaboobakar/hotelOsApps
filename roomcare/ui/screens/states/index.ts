/**
 * Room states — every room on one screen, four facts editable per room, one
 * Save for many rooms; three views of one data set, a chip between them
 * (frames 4c–4e; owner's redline 5, 2026-09-13). Every write is a manual
 * observation — a deliberate act under S4's clause. Rides `roomcare.amend`.
 */

import type { HostApi } from "@hotelos/sdk";

import { failed } from "../../chrome/failure";
import { chip } from "../../chrome/bar";
import { control, el } from "../../chrome/element";
import { clock } from "../../chrome/instant";
import { READ, act, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { whole } from "../../chrome/number";
import { remember, remembered } from "../../chrome/remember";
import type { RoomStates, StateRow } from "../../model";
import { compact } from "./compact";
import { Edits } from "./edits";
import { grid } from "./grid";
import { sheetView } from "./sheet";

export interface Conflict {
  roomId: string;
  version: number;
  source: string;
  lastObservedAt: string | null;
}

export async function states(host: HostApi, body: HTMLElement, nav: Nav): Promise<void> {
  const got = await load<RoomStates>(host, READ, "states");
  if (!got.ok) {
    body.append(failed(host, got.failure, "the room states", nav.show));
    return;
  }

  const data = got.value;
  const rows = new Map<string, StateRow>(data.zones.flatMap((z) => z.rooms).map((r) => [r.roomId, r]));
  const edits = new Edits(rows);
  let view = remembered("states.view", ["SHEET", "TAP_GRID", "COMPACT"], data.defaultView);
  let conflicts: Conflict[] = [];
  let said = "";

  const save = async (): Promise<void> => {
    const done = await act(host, "roomcare.amend", "saveStates", edits.payload());
    if (!done.ok) { said = done.because; return redraw(); }
    const answer = done.value as { saved: number; conflicts: Conflict[] };
    conflicts = answer.conflicts;
    if (conflicts.length === 0) nav.show();
    else { said = `${whole(host, answer.saved)} saved · ${whole(host, conflicts.length)} changed by something else since this screen was drawn — look again before saving those`; redraw(); }
  };

  function redraw(): void {
    const top = el("div", "strip");
    const count = (n: number, label: string): HTMLElement => { const c = el("span"); c.append(el("b", undefined, whole(host, n)), document.createTextNode(label)); return c; };
    top.append(
      chip("Sheet", view === "SHEET", () => pick("SHEET")),
      chip("Tap grid", view === "TAP_GRID", () => pick("TAP_GRID")),
      chip("Compact", view === "COMPACT", () => pick("COMPACT")),
      count(data.rooms, "rooms"), count(data.dirty, "dirty"), count(data.occupied, "occupied"), count(data.soldTonight, "sold tonight"),
      el("b", undefined, data.silentSince === null ? "" : `PMS silent since ${clock(host, data.silentSince)}`),
    );
    const saveButton = edits.size === 0 ? el("span", "btn off", "Save — nothing changed") : control("btn pri", `Save ${whole(host, edits.size)} changes`, () => void save());
    const discard = control("btn", "Discard", () => { edits.discard(); conflicts = []; said = ""; redraw(); });
    const end = el("span", "end row");
    end.append(saveButton, discard);
    top.append(end);
    const conflicted = new Set(conflicts.map((c) => c.roomId));
    const house = view === "TAP_GRID" ? grid(host, data, edits, conflicted, redraw) : view === "COMPACT" ? [compact(host, data, edits, conflicted, redraw, nav)] : sheetView(host, data, edits, conflicted, redraw, nav);
    body.replaceChildren(top, ...(said === "" ? [] : [el("p", "said bad", said)]), ...house);
  }

  function pick(next: string): void {
    view = next;
    remember("states.view", next);
    redraw();
  }

  redraw();
}

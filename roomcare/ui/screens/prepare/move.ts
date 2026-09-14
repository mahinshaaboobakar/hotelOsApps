/**
 * Move rooms… — the supervisor's hand on the proposal: each room of the window
 * against the people posted to Housekeeping; a changed choice is an assignment,
 * each under the version it was drawn at (§7.8 — one row, never a lock).
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, option } from "../../chrome/element";
import { failed } from "../../chrome/failure";
import { act, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { actions, sheet } from "../../chrome/overlay";
import { service } from "../../chrome/words";
import type { Board } from "../../model";
import type { PrepareView } from "./index";

export async function moveRooms(host: HostApi, nav: Nav, v: PrepareView, onlyTask: string | null): Promise<void> {
  const overlay = sheet(nav.frame, onlyTask === null ? "Move rooms" : "Assign anyway");
  const [board, people] = await Promise.all([load<Board>(host, "board"), load<{ userId: string; name: string }[]>(host, "attendants")]);
  // No rooms or no people to draw means no Assign to press: the failure, and the way back.
  const again = (): void => { overlay.close(); void moveRooms(host, nav, v, onlyTask); };
  const unread = !board.ok ? failed(board.failure, "the rooms to move", again) : !people.ok ? failed(people.failure, "the people posted to Housekeeping", again) : null;
  if (!board.ok || !people.ok) {
    if (unread !== null) overlay.body.append(unread);
    overlay.foot.append(control("btn", "Back", () => overlay.close()));
    return;
  }

  const rooms = board.value.zones.flatMap((z) => z.rooms)
    .filter((r) => r.taskId !== null && (onlyTask === null ? !r.marks.inProgress && r.outcome.kind !== "DONE" : r.taskId === onlyTask));
  overlay.body.append(el("p", "dim", `${v.proposal.candidates} people posted to Housekeeping. A room already started stays with its attendant.`));
  const choices = rooms.map((room) => {
    const select = el("select", "cell") as HTMLSelectElement;
    select.append(option("— nobody", ""));
    for (const p of people.value) select.append(option(p.name, p.userId, p.userId === room.attendantId));
    const line = el("div", "row");
    line.style.marginBottom = "6px";
    line.append(el("span", "num", room.number), el("span", "dim", service(room.service)), select);
    overlay.body.append(line);
    return { room, select };
  });

  actions(overlay, "Assign", () => void (async () => {
    for (const { room, select } of choices) {
      if (select.value === "" || select.value === room.attendantId) continue;
      const done = await act(host, "roomcare.assign", "assign", { taskId: room.taskId, version: room.taskVersion, userId: select.value });
      if (!done.ok) return overlay.refuse(`${room.number}: ${done.because}`);
    }
    overlay.close();
    nav.show();
  })());
}

/**
 * A room's acts — the single-room state sheet (the advanced edit), recording an
 * exception on another's room, and reassigning. Each composes in a sheet and
 * keeps it open, with the service's sentence, when refused (page 64 §9).
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, option } from "../../chrome/element";
import { failed } from "../../chrome/failure";
import { READ, act, load } from "../../chrome/load";
import type { Nav } from "../../chrome/nav";
import { actions, readyWhen, sheet } from "../../chrome/overlay";
import type { BoardRoom } from "../../model";

/** Enter a fact by hand for one room — source manual, a deliberate act (frame 4, redline 4). */
export function roomState(host: HostApi, nav: Nav, room: BoardRoom): void {
  const overlay = sheet(nav.frame, `Room ${room.number} — enter a fact by hand`);
  let stay: string | null = null;
  const choices = el("div", "row");
  for (const [label, word] of [["Guest arrived", "ARRIVED"], ["Guest departed", "DEPARTED"], ["Occupied", "IN_HOUSE"], ["Vacant", "NONE"]] as const) {
    const button = control("btn chip", label, () => {
      stay = word;
      choices.querySelectorAll("button").forEach((b) => { b.classList.toggle("on", b === button); b.setAttribute("aria-pressed", String(b === button)); });
    });
    // Nothing is chosen until the person chooses; the choice is announced, not only coloured, as `chip()` does.
    button.setAttribute("aria-pressed", "false");
    choices.append(button);
  }
  const arrival = el("input", "field") as HTMLInputElement;
  arrival.type = "time";
  arrival.setAttribute("aria-label", "Arrival expected today");
  const condition = el("select", "field") as HTMLSelectElement;
  for (const value of ["", "DIRTY", "CLEAN", "INSPECTED"]) condition.append(option(value === "" ? "condition unchanged" : value.toLowerCase(), value));
  overlay.body.append(
    el("label", "lbl", "What happened"), choices,
    el("label", "lbl", "Arrival expected today · sets sold tonight"), arrival,
    el("label", "lbl", "Condition"), condition,
    el("p", "dim", "Recorded by hand. If the front desk says something different afterwards, a supervisor settles it."),
  );
  actions(overlay, "Record", () => {
    void (async () => {
      const done = await act(host, "roomcare.amend", "saveStates", {
        rooms: [{ roomId: room.id, version: room.version, stay, soldAt: arrival.value === "" ? null : arrival.value, condition: condition.value === "" ? null : condition.value }],
      });
      if (!done.ok) return overlay.refuse(done.because);
      const conflicts = (done.value as { conflicts: unknown[] }).conflicts;
      if (conflicts.length > 0) return overlay.refuse("Something changed this room after the page was drawn — close and look again before recording.");
      overlay.close();
      nav.show();
    })();
  });
  readyWhen(overlay, () => (stay === null && arrival.value === "" && condition.value === "" ? "choose what happened, an arrival or a condition" : null));
}

/** Record what was found at the door, on the attendant's behalf (roomcare.amend). */
export function recordException(host: HostApi, nav: Nav, room: BoardRoom): void {
  const overlay = sheet(nav.frame, `Room ${room.number} — record an exception`);
  const found = el("select", "field") as HTMLSelectElement;
  for (const [label, value] of [["DND board", "DND"], ["Declined by guest", "DECLINED"], ["Done", "DONE"]] as const) found.append(option(label, value));
  const note = el("textarea", "field") as HTMLTextAreaElement;
  overlay.body.append(el("label", "lbl", "Found"), found, el("label", "lbl", "Note"), note);
  actions(overlay, "Record", () => {
    void (async () => {
      const done = await act(host, "roomcare.amend", "recordOnBehalf", { taskId: room.taskId, version: room.taskVersion, found: found.value, note: note.value || null });
      if (!done.ok) return overlay.refuse(done.because);
      overlay.close();
      nav.show();
    })();
  });
}

/** Give the room to someone else — one of the people Workforce posted to Housekeeping. */
export async function reassign(host: HostApi, nav: Nav, room: BoardRoom): Promise<void> {
  const overlay = sheet(nav.frame, `Room ${room.number} — reassign`);
  const person = el("select", "field") as HTMLSelectElement;
  person.setAttribute("aria-label", "Person");
  const people = await load<{ userId: string; name: string }[]>(host, READ, "attendants");
  if (!people.ok) {
    // Nobody to choose from, so nothing to reassign to: the failure, and the way back.
    overlay.body.append(failed(host, people.failure, "the people posted to Housekeeping", () => { overlay.close(); void reassign(host, nav, room); }));
    overlay.foot.append(control("btn", "Back", () => overlay.close()));
    return;
  }
  for (const p of people.value) person.append(option(p.name, p.userId, p.userId === room.attendantId));
  overlay.body.append(
    el("p", "dim", `Now: ${room.attendant ?? "nobody"}. Anyone posted to Housekeeping can take it.`),
    el("label", "lbl", "Person"), person,
  );
  actions(overlay, "Reassign", () => {
    void (async () => {
      const done = await act(host, "roomcare.assign", "assign", { taskId: room.taskId, version: room.taskVersion, userId: person.value });
      if (!done.ok) return overlay.refuse(done.because);
      overlay.close();
      nav.show();
    })();
  });
  readyWhen(overlay, () => (person.value === "" || person.value === room.attendantId ? "choose someone else" : null));
}

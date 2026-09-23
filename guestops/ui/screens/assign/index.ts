/**
 * Giving a stay a room, and moving it to another — gold frame 3's *Move room*,
 * and the day list's `＋ assign`.
 *
 * # The conflict warns and never forbids
 *
 * GUEST-Q5 made a double-booked room a possible truth: when staff answer *"two
 * different stays"* to a candidate link, the second stay is real and the room
 * genuinely has two. So the service refuses the first attempt, names what is in
 * the way, and accepts the same call again — **a hard block would put a ruled
 * outcome out of reach.**
 *
 * That makes a conflict part of the ANSWER rather than a failure, and this
 * sheet draws it that way: the room stays chosen, the sentence says what holds
 * it, and the button becomes *Assign anyway*. A screen that showed it as an
 * error would leave the desk with a refusal and nothing to do about it.
 *
 * # One sheet for both affordances
 *
 * A stay with no room is being given its first; one that has a room is being
 * moved. The difference is a word on the button and a sentence about what
 * happens to the old room — the **reason recorded is the service's to derive**,
 * because a client that could send one could record a move as a first
 * assignment.
 */

import type { HostApi } from "@hotelos/sdk";

import {
  APP,
  failureDrawing,
  load,
  perform,
  type FreeRooms,
  type StayPage,
} from "../../book";
import { el } from "../../chrome/element";
import { field, type Field } from "../../chrome/field";
import { failed } from "../../chrome/marks";
import { sheet } from "../../chrome/overlay";

/**
 * What the sheet was last told, and by which of two different things.
 *
 * **A conflict and a refusal are not one state.** A conflict is retryable with
 * the desk's agreement — that is the whole of GUEST-Q5. A refusal is not: a
 * stale version does not become current because somebody presses again, and
 * offering *Assign anyway* over one would send `acceptConflict` for a reason
 * that has nothing to do with conflicts. They looked alike while both were a
 * sentence, and the button read *Assign anyway* over both.
 */
type Told =
  | { kind: "conflict"; text: string }
  | { kind: "refused"; text: string };

/** What the assignment write answers. */
interface Assigned {
  assigned: boolean;
  conflict?: boolean;
  version: number;
  moved: boolean;
}

/**
 * Open the sheet over whatever screen is behind.
 *
 * @param host the bridge — the only route out of this realm
 * @param into where the overlay goes
 * @param stayId the stay being given a room
 * @param close what dismissing it does
 * @param done called once the room is recorded, so the screen behind redraws
 */
export async function assignRoom(
  host: HostApi,
  into: HTMLElement,
  stayId: string,
  close: () => void,
  done: () => void,
): Promise<void> {
  // **The stay answers for itself.** The sheet is opened from a row and from a
  // page, and asking either to hand over the version, the type and the nights
  // would put the stay's own facts in a caller's hands to send back wrong.
  const stay = await load<StayPage>(host, "reservation.read", "stay", { stayId });

  if (!stay.ok) {
    into.append(failed(
      failureDrawing(stay.failure, { app: APP, the: "this stay" }),
      () => void assignRoom(host, into, stayId, close, done),
    ));
    return;
  }

  const free = await load<FreeRooms>(host, "reservation.read", "rooms", { stayId });

  if (!free.ok) {
    into.append(failed(
      failureDrawing(free.failure, { app: APP, the: "the rooms free for this stay" }),
      () => void assignRoom(host, into, stayId, close, done),
    ));
    return;
  }

  draw(host, into, stay.value, free.value, "", null, close, done);
}

/**
 * Draw the sheet.
 *
 * `told` is what the service last said, and WHICH of two things said it. A
 * conflict is kept across the redraw with the room still chosen, because the
 * next press is the same assignment with the desk's agreement attached; a
 * refusal is not something agreement can fix.
 */
function draw(
  host: HostApi,
  into: HTMLElement,
  stay: StayPage,
  free: FreeRooms,
  chosen: string,
  told: Told | null,
  close: () => void,
  done: () => void,
): void {
  let roomId = chosen;

  const again = (said: Told | null): void => {
    into.replaceChildren();
    draw(host, into, stay, free, roomId, said, close, done);
  };

  const assign = async (accepting: boolean): Promise<void> => {
    const answer = await perform<Assigned>(host, "stay.assign", "assign", {
      stayId: stay.id,
      roomId,
      version: stay.version,

      // **Absent on the first attempt, always.** The desk has to see what is in
      // the way before it can mean it.
      ...(accepting ? { acceptConflict: true } : {}),
    });

    if (answer.refused !== null || answer.value === null) {
      again({
        kind: "refused",
        text: answer.refused ?? "The platform did not answer.",
      });
      return;
    }

    if (answer.value.assigned) {
      done();
      return;
    }

    // Not an error — a question. The room is held over these dates, and that
    // can be true on purpose.
    again({
      kind: "conflict",
      text: `${numberOf(free, roomId)} is already held over these dates by another `
        + "stay. Assign anyway, or pick another room.",
    });
  };

  const moving = stay.currentRoomId !== null;

  into.append(sheet({
    title: moving ? "Move room" : "Assign a room",
    subtitle: moving
      ? `${stay.guest} is in ${stay.room ?? "a room"}`
      : `${stay.guest} has no room yet`,

    body: [
      field(chooser(free, roomId, (picked) => { roomId = picked; sync(into, roomId); })),

      told === null ? null : el("div", "note warn", told.text),

      // **What the write will do, said before it is pressed.** A move releases
      // the old room, which is the half a person does not see in the chooser.
      el("div", "note", moving
        ? `Moving publishes the same room-changed fact an override does, so `
          + `${stay.room ?? "the old room"} is released and Room Care re-plans from the event stream.`
        : "The room is recorded against this stay and Room Care re-plans from the event stream."),
    ],

    foot: null,

    actions: [
      { label: "Cancel", onClick: close },
      {
        // The label says which of the two presses this is. *Assign anyway* is
        // not a second action — it is the same call with the desk's agreement.
        // **Only a conflict becomes *Assign anyway*.** A refusal keeps the
        // ordinary label: pressing it again is the same attempt, which is right
        // for a platform that did not answer and honest for one that refused.
        label: told?.kind === "conflict" ? "Assign anyway" : (moving ? "Move" : "Assign"),
        primary: true,
        onClick: () => void assign(told?.kind === "conflict"),
      },
    ],

    onDismiss: close,
  }));

  sync(into, roomId);
}

/**
 * The chooser, over what is free for this stay.
 *
 * **Three states, not one empty list** — no rooms of the type at all, every
 * room taken, and rooms to choose from. The first two have opposite remedies.
 */
function chooser(
  free: FreeRooms,
  value: string,
  onChange: (value: string) => void,
): Field {
  const chosen = free.rooms.find((room) => room.id === value);

  return {
    label: "Room",
    value: chosen?.number ?? "",
    placeholder: prompt(free),
    capture: {
      kind: "choice",
      choices: free.rooms.map((room) => room.number),
      onChange: (picked: string) =>
        onChange(free.rooms.find((room) => room.number === picked)?.id ?? ""),
    },
  };
}

function prompt(free: FreeRooms): string {
  if (free.ofType === 0) return "This property has no rooms of that type";
  if (free.rooms.length === 0) return "Every room of that type is taken";
  return "Choose a room";
}

/**
 * Put the primary in the state the chosen room warrants.
 *
 * Choosing does not redraw — the sheet has nothing new to read — so the button
 * is synced in place. A primary left as it was drawn stays disabled over a
 * complete sheet, which reads as the platform refusing work it would accept.
 */
function sync(into: HTMLElement, roomId: string): void {
  // The foot's last control. `chrome/overlay.ts` appends the actions in order
  // and the primary is last, which is also the order the frames draw them in.
  const primary = [...into.querySelectorAll("button")].at(-1);

  if (primary === undefined) return;

  const can = roomId !== "";

  (primary as HTMLButtonElement).disabled = !can;
  primary.className = can ? "btn sm pri" : "btn sm off";
  primary.title = can ? "" : "Choose a room first.";
  primary.setAttribute("aria-description", can ? "" : "Choose a room first.");
}

/** The number of a chosen room, for the conflict sentence. */
function numberOf(free: FreeRooms, id: string): string {
  return free.rooms.find((room) => room.id === id)?.number ?? "That room";
}

/**
 * Walk-in — booking and arrival in one action. Gold frame 10.
 *
 * **One action, because booking and arrival are one moment** (S13). A two-step
 * *create, then check in* would produce a stay in `Booked` that nobody ever
 * leaves, and the walk-in ratio — a number every hotel reports on — cannot be
 * recovered later if the flag is not set when the stay is created.
 *
 * **Check-in requires a room**, which is why the room field sits in this sheet
 * and not behind a later step (S8, the one hard gate the assignment ruling
 * creates). Until 2026-09-23 nothing in this application could name a room:
 * `WalkInCommand` had required a `roomId` since it was written and no read
 * listed rooms, so this sheet could not be completed by any caller.
 *
 * # One button, two phases, and the sheet must be able to say so
 *
 * The desk presses once; underneath, the stay is created and committed, and
 * then the room and the arrival are a second authorized operation (ADR 0193,
 * RC-Q8a). **A refused second phase leaves the stay** — no compensating cancel
 * is invented — so `checkedIn: false` arriving with a reason is a SUCCESSFUL
 * call reporting a partial outcome, not a failure. A sheet that drew it as a
 * failure would tell a receptionist nothing happened while a booked, room-less
 * stay sat in the book.
 *
 * # The day is the property's
 *
 * The arrival defaults to the business date the service reports, never to this
 * machine's clock: a desk in Kochi and a browser in another zone disagree about
 * what *today* is, and the stay is the property's.
 */

import type { HostApi } from "@hotelos/sdk";

import {
  APP,
  failureDrawing,
  load,
  perform,
  type Availability,
  type FreeRooms,
} from "../../book";
import { el } from "../../chrome/element";
import { field, pair, type Field } from "../../chrome/field";
import { failed } from "../../chrome/marks";
import { sheet } from "../../chrome/overlay";

/** What the desk has entered so far. */
interface Draft {
  guest: string;
  phone: string;
  arrives: string;
  departs: string;
  roomTypeId: string;
  roomId: string;
}

/** What the walk-in write answers. */
interface Taken {
  bookingId: string;
  stayId: string;
  checkedIn: boolean;
  secondStep: string | null;
}

/**
 * Open the sheet.
 *
 * @param host the bridge — the only route out of this realm
 * @param into where the overlay goes
 * @param close what dismissing it does
 * @param opened called with the new stay, to show it
 */
export async function walkIn(
  host: HostApi,
  into: HTMLElement,
  close: () => void,
  opened: (stayId: string) => void,
): Promise<void> {
  // The property's day, not this machine's. Null where no business day is
  // established — then the desk types the dates and nothing is assumed.
  const day = await load<{ businessDate: string | null }>(
    host, "reservation.read", "today", {});

  if (!day.ok) {
    into.append(failed(
      failureDrawing(day.failure, { app: APP, the: "the property's day" }),
      () => void walkIn(host, into, close, opened),
    ));
    return;
  }

  const today = day.value.businessDate;

  const draft: Draft = {
    guest: "",
    phone: "",
    arrives: today ?? "",
    departs: today === null ? "" : nextDay(today),
    roomTypeId: "",
    roomId: "",
  };

  await render(host, into, draft, close, opened, null);
}

/**
 * Read what the dates and the chosen type allow, then draw.
 *
 * **Two reads, and the second depends on the first.** A room is free for a type
 * over a range, so the room list cannot be asked for until both the dates and
 * the type are chosen — which is why this re-reads rather than filtering a list
 * it fetched once.
 */
async function render(
  host: HostApi,
  into: HTMLElement,
  draft: Draft,
  close: () => void,
  opened: (stayId: string) => void,
  said: string | null,
): Promise<void> {
  const dated = draft.arrives !== "" && draft.departs !== "";

  const offered = dated
    ? await load<Availability>(host, "reservation.read", "availability",
      { arrive: draft.arrives, depart: draft.departs })
    : null;

  const types = offered !== null && offered.ok ? offered.value.types : [];

  // A type that is no longer on offer cannot stay chosen — the dates moved
  // under it, and a hidden id would be sent for a type the desk cannot see.
  if (draft.roomTypeId !== "" && !types.some((type) => type.roomTypeId === draft.roomTypeId)) {
    draft.roomTypeId = "";
    draft.roomId = "";
  }

  const free = dated && draft.roomTypeId !== ""
    ? await load<FreeRooms>(host, "reservation.read", "rooms",
      { roomTypeId: draft.roomTypeId, arrive: draft.arrives, depart: draft.departs })
    : null;

  const rooms = free !== null && free.ok ? free.value : null;

  if (rooms !== null && !rooms.rooms.some((room) => room.id === draft.roomId)) {
    draft.roomId = "";
  }

  const again = (told: string | null): void => {
    into.replaceChildren();
    void render(host, into, draft, close, opened, told);
  };

  const change = (key: keyof Draft) => (value: string): void => {
    draft[key] = value;

    // A date or a type changes what is ON OFFER, so the sheet re-reads. A name,
    // a phone number or a room does not — and redrawing on every keystroke
    // would take the cursor out of the box the desk is typing in.
    if (key === "arrives" || key === "departs" || key === "roomTypeId") {
      again(said);
      return;
    }

    // **But whether the sheet can be sent has changed, and the button has to
    // say so without a redraw.** Choosing a room was the last thing the service
    // requires; a primary left as it was drawn stays disabled over a complete
    // sheet, which reads as the platform refusing work it would accept.
    syncPrimary();
  };

  /**
   * Put the primary in the state the draft now warrants.
   *
   * The two class names are `chrome/overlay.ts`'s, written here because this is
   * the one screen whose primary changes between renders. If a second screen
   * needs it, it moves to the overlay rather than being copied.
   */
  const syncPrimary = (): void => {
    const button = [...into.querySelectorAll("button")]
      .find((element) => element.textContent === "Create and check in");

    if (button === undefined) return;

    const can = ready(draft);

    (button as HTMLButtonElement).disabled = !can;
    button.className = can ? "btn sm pri" : "btn sm off";
    button.title = can ? "" : missing(draft);
    button.setAttribute("aria-description", can ? "" : missing(draft));
  };

  const create = async (): Promise<void> => {
    const taken = await perform<Taken>(host, "stay.create", "walkIn", {
      guest: draft.guest,
      phone: draft.phone,
      roomTypeId: draft.roomTypeId,
      roomId: draft.roomId,
      arrives: draft.arrives,
      departs: draft.departs,
    });

    if (taken.refused !== null || taken.value === null) {
      again(taken.refused ?? "The platform did not answer.");
      return;
    }

    if (taken.value.checkedIn) {
      opened(taken.value.stayId);
      return;
    }

    // **The stay EXISTS.** A message that read as a failure here would send the
    // desk to create a second one for a guest already in the book.
    again(
      `The stay was created and the guest is not yet in house: ${why(taken.value.secondStep)}. `
      + "Open the stay from the day's list to give a room.");
  };

  into.append(sheet({
    title: "Walk-in",
    subtitle: "Creates the stay and checks it in — one action, one business day",

    body: [
      typed("Guest", draft.guest, change("guest"), "The name the stay is in"),

      typed("Contact", draft.phone, change("phone"), "No contact — the stay is still valid",
        "One contact is enough. A stay with none is valid and says so — it is "
        + "never filled with a placeholder."),

      pair(
        dateBox("Arrives", draft.arrives, change("arrives"), dayKnown(draft)),
        dateBox("Departs", draft.departs, change("departs"), true),
      ),

      pair(
        chooser("Room type", draft.roomTypeId, change("roomTypeId"),
          types.map((type) => ({ value: type.roomTypeId, label: type.roomType })),
          dated ? "Choose a type" : "Set the dates first"),

        chooser("Room", draft.roomId, change("roomId"),
          (rooms?.rooms ?? []).map((room) => ({ value: room.id, label: room.number })),
          roomPrompt(draft, rooms)),
      ),

      // **Drawn and not captured, with the reason.** `WalkInCommand` has
      // nowhere to put a rate: the sheet cannot send one, and a box that
      // accepted a number and dropped it is the defect this round is closing.
      field({
        label: "Rate",
        value: null,
        placeholder: "Not set here — this application has no field for it yet",
        hint:
          "An amount carries three things or it is not an amount — value, "
          + "currency, and whether tax is included.",
      }),

      field({
        label: "Registration",
        value: null,
        placeholder: "GRC number · ID · signature — captured on the stay, after this",
      }),

      recorded(),
      said === null ? null : el("div", "note warn", said),
    ],

    foot: draft.roomId === ""
      ? null
      : `Room ${numberOf(rooms?.rooms ?? [], draft.roomId)} will be marked occupied.`,

    actions: [
      { label: "Cancel", onClick: close },
      // **Off until it can succeed, and it says what is missing.** The service
      // refuses without a name, a type, a room and both dates; a pressable
      // button that always refuses teaches the desk to distrust it.
      // **Always the live shape, then disabled in place.** The off shape carries
      // no handler by construction, so a sheet drawn incomplete and completed
      // without a re-read would have a button that enables and does nothing.
      // `syncPrimary` below sets the disabled state this is drawn in.
      { label: "Create and check in", primary: true, onClick: () => void create() },
    ],

    onDismiss: close,
  }));

  syncPrimary();
}

/** What the sheet still needs, for the disabled button's own words. */
function missing(draft: Draft): string {
  const wanted = [
    draft.guest.trim() === "" ? "a name" : null,
    draft.arrives === "" || draft.departs === "" ? "both dates" : null,
    draft.roomTypeId === "" ? "a room type" : null,

    // Named last and never omitted: it is the one hard gate, and a desk that
    // has filled everything else needs to know the room is what is left.
    draft.roomId === "" ? "a room" : null,
  ].filter((part) => part !== null);

  return `This needs ${wanted.join(", ")} before the stay can be created.`;
}

/** Everything the service requires, present. */
function ready(draft: Draft): boolean {
  return draft.guest.trim() !== ""
    && draft.roomTypeId !== ""
    && draft.roomId !== ""
    && draft.arrives !== ""
    && draft.departs !== "";
}

/** Whether the service gave a business day to start the arrival box from. */
function dayKnown(draft: Draft): boolean {
  return draft.arrives !== "";
}

/**
 * Why the room and the arrival did not happen, for a person.
 *
 * The service sends a kind rather than its own sentence — the exception's words
 * are for the log, not for the desk.
 */
function why(kind: string | null): string {
  if (kind === "not-authorized") return "this desk may not give a room";
  if (kind === "unanswered") return "the platform did not answer";
  if (kind === "changed") return "somebody else changed the stay first";
  return "the room could not be given";
}

/**
 * What the room chooser says when it has nothing to offer.
 *
 * **Four states, not one empty list.** No dates, rooms unreadable, no rooms of
 * the type at all, and every room taken have different remedies — set the
 * dates, retry, configure the property, or choose another type — and a single
 * *no rooms* would report them alike.
 */
function roomPrompt(draft: Draft, rooms: FreeRooms | null): string {
  if (draft.roomTypeId === "") return "Choose a room type first";
  if (rooms === null) return "Rooms could not be read";
  if (rooms.ofType === 0) return "This property has no rooms of that type";
  if (rooms.rooms.length === 0) return "Every room of that type is taken";
  return "Choose a room";
}

/** A typed box. */
function typed(
  name: string,
  value: string,
  onChange: (value: string) => void,
  placeholder: string,
  hint?: string,
): HTMLElement {
  const box: Field = {
    label: name,
    value,
    placeholder,
    capture: { kind: "text", onChange },
  };

  return field(hint === undefined ? box : { ...box, hint });
}

/** A date box. */
function dateBox(
  name: string,
  value: string,
  onChange: (value: string) => void,
  dayKnown: boolean,
): Field {
  return {
    label: name,
    value,
    placeholder: dayKnown ? "" : "No business day — type the date",
    capture: { kind: "date", onChange },
  };
}

/** A chooser over what the service offered. */
function chooser(
  name: string,
  value: string,
  onChange: (value: string) => void,
  options: readonly { value: string; label: string }[],
  placeholder: string,
): Field {
  const chosen = options.find((option) => option.value === value);

  return {
    label: name,
    value: chosen?.label ?? "",
    placeholder,
    capture: {
      kind: "choice",
      choices: options.map((option) => option.label),
      onChange: (picked: string) =>
        onChange(options.find((option) => option.label === picked)?.value ?? ""),
    },
  };
}

/** The number of a chosen room, for the consequence line. */
function numberOf(rooms: readonly { id: string; number: string }[], id: string): string {
  return rooms.find((room) => room.id === id)?.number ?? "";
}

/** The day after an ISO day, without going through a locale. */
function nextDay(iso: string): string {
  const at = new Date(`${iso}T00:00:00Z`);
  at.setUTCDate(at.getUTCDate() + 1);
  return at.toISOString().slice(0, 10);
}

/** The flag, and why it cannot wait. */
function recorded(): HTMLElement {
  const note = el("div", "note");

  note.append(
    el("b", undefined, "This is a walk-in and it will be recorded as one."),
    document.createTextNode(
      " The walk-in ratio is a number every hotel reports on, and it cannot be "
        + "recovered later if the flag is not set when the stay is created.",
    ),
  );

  return note;
}

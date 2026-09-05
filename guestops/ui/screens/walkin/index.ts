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
 * creates).
 *
 * In a PMS-connected property this same sheet is available and the resulting
 * stay is marked *Opera doesn't know* (GUEST-Q5). There is no second mode.
 */

import type { WalkInDraft } from "../../book";
import { el } from "../../chrome/element";
import { field, pair } from "../../chrome/field";
import { sheet } from "../../chrome/overlay";

/**
 * Draw the sheet.
 *
 * @param draft what the desk has entered
 * @param close what dismissing it does
 * @returns the scrim, with the sheet on it
 */
export function walkIn(draft: WalkInDraft, close: () => void): HTMLElement {
  return sheet({
    title: "Walk-in",
    subtitle: "Creates the stay and checks it in — one action, one business day",

    body: [
      field({ label: "Guest", value: draft.guest, aside: draft.guestNote }),

      field({
        label: "Contact",
        value: draft.contact,
        aside: draft.contact === null ? undefined : draft.contactKind,
        placeholder: "No contact — the stay is still valid",
        hint:
          "One contact is enough. A stay with none is valid and says so — it is "
          + "never filled with a placeholder.",
      }),

      pair(
        { label: "Room type", value: draft.roomType },
        {
          label: "Room",
          value: draft.room,
          aside: draft.roomState ?? undefined,
          placeholder: "Check-in needs a room",
        },
      ),

      pair(
        // `now` is bold in the frame because it is the whole point of the
        // screen: the arrival is not a date somebody picked, it is this moment.
        { label: "Arrives", value: `${draft.arrives.split(" · ")[0]} · `,
          strong: draft.arrives.split(" · ")[1] },
        { label: "Departs", value: draft.departs },
      ),

      field({
        label: "Rate",
        value: draft.rate,
        aside: draft.rateBasis,
        placeholder: "No rate set",
        hint:
          "An amount carries three things or it is not an amount — value, "
          + "currency, and whether tax is included.",
      }),

      field({
        label: "Registration",
        value: null,
        placeholder: "GRC number · ID · signature — capture now or at the desk",
      }),

      recorded(),
    ],

    foot: draft.consequence,

    actions: [
      { label: "Cancel", onClick: close },
      { label: "Create and check in", primary: true },
    ],

    onDismiss: close,
  });
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

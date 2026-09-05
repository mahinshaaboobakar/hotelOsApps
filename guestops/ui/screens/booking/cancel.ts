/**
 * Cancelling a booking — the dialog. Gold frame 8.
 */

import type { CancelPlan } from "../../book";
import { el, fill } from "../../chrome/element";
import { field } from "../../chrome/field";
import { dialog } from "../../chrome/overlay";
import { tags } from "../../chrome/marks";

/**
 * Draw the confirmation.
 *
 * **The confirmation names the object, the consequence, and the limit** — the
 * platform's confirmation rule (ADR 0106 §3). Here the limit is the one that
 * matters most: nothing GuestOps records reaches the PMS in v1 (CONN-Q5, ADR
 * 0128 §4). A cancellation screen that stayed silent about it would let a
 * receptionist believe the room had been released in Opera, and the room would
 * be sold twice.
 *
 * **Cancelling a booking is n cancellations of stays**, said out loud, because
 * that is what the model does and because either stay can be reinstated on its
 * own (GUEST-Q2, S23).
 *
 * The penalty is **computed from the stored offset at the moment it is shown**
 * (R18) and **recorded, never charged** (GUEST-Q6) — charging is Finance's.
 *
 * @param plan what cancelling will actually do
 * @param close what dismissing it does
 * @param confirm what to do with the reason when it is confirmed
 * @returns the scrim, with the dialog on it
 */
export function cancel(
  plan: CancelPlan,
  close: () => void,
  confirm: (reason: string) => void,
): HTMLElement {
  // The reason showing, which is the one that would be recorded. Null when the
  // property has configured none — a real state, because nothing in GuestOps's
  // settings owns this list yet and the projection reports that rather than
  // inventing a vocabulary (see `CancelPlanView`).
  const reason = plan.reasons[0] ?? null;

  const consequence = el("div", "note");
  const [lead, ...rest] = plan.consequence.split(". ");

  consequence.append(
    el("b", undefined, `${lead ?? ""}.`),
    document.createTextNode(` ${rest.join(". ")}`),
  );

  const rows = plan.rows.map((row) => {
    const element = el("div", "fr");
    const value = el("div", "v");

    value.append(document.createTextNode(row.value));

    if (row.strong !== undefined) {
      value.append(el("b", undefined, row.strong));
    }

    fill(value, ...tags(row.tags));
    element.append(el("div", "k", row.label), value);
    return element;
  });

  return dialog({
    title: "Cancel this booking?",
    subtitle: plan.subject,

    body: [
      consequence,
      ...rows,
      plan.notTold === null ? null : refusal(plan.notTold),
      field({
        label: "Reason",
        value: reason,
        aside: reason === null ? undefined : "▾",
        placeholder: "No reason is configured — this cancellation cannot be recorded",
      }),
      el(
        "div",
        "hint",
        "The penalty is calculated and recorded, not charged. Charging is "
          + "Finance's, a later round.",
      ),
    ],

    foot: "Recorded against your name.",

    actions: [
      { label: "Keep the booking", onClick: close },
      {
        // The count is in the label because the dialog's whole argument is that
        // this is n cancellations rather than one — a button saying "Cancel the
        // booking" over a two-stay group would undo the sentence above it. It
        // comes from `plan.stays` and never from `plan.rows.length`, which is a
        // mixed list and once made this button offer to cancel three stays of a
        // two-stay booking.
        label: plan.stays === 1 ? "Cancel this stay" : `Cancel all ${plan.stays} stays`,
        danger: true,

        // **No reason, no write.** The command refuses a cancellation without
        // one, and this refuses to send one — so a property that has configured
        // no reasons gets a button drawn as unavailable with the field above it
        // saying why, rather than a server refusal after the fact.
        off: reason === null,
        onClick: reason === null ? () => undefined : () => confirm(reason),
      },
    ],

    onDismiss: close,
  });
}

/**
 * The sentence that must not be omitted.
 *
 * Bad-toned rather than warn: it is not a condition to watch, it is a limit of
 * what the button about to be pressed does.
 */
function refusal(text: string): HTMLElement {
  const banner = el("div", "ban gone");
  const body = el("div");
  const [lead, ...rest] = text.split(". ");

  body.append(
    el("b", undefined, `${lead ?? ""}.`),
    document.createTextNode(` ${rest.join(". ")}`),
  );

  banner.append(body);
  return banner;
}

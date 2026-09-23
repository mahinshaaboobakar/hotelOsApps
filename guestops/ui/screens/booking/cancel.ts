/**
 * Cancelling a booking — the dialog. Gold frame 8.
 */

import type { PropertyEnvironment } from "@hotelos/sdk";

import type { CancelPlan } from "../../book";
import { span } from "../../chrome/when";
import { many, spell } from "../../chrome/words";
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
  property: PropertyEnvironment,
): HTMLElement {
  // The reason showing, which is the one that would be recorded. Null when the
  // property has configured none — a real state, because nothing in GuestOps's
  // settings owns this list yet and the projection reports that rather than
  // inventing a vocabulary (see `CancelPlanView`).
  const reason = plan.reasons[0] ?? null;

  // `This cancels two stays, one at a time.` — the lead bold, the rest after
  // it. The service sent the finished sentence until 2026-09-20 and the screen
  // split it back apart on ". " to bold the first half; now it is composed in
  // the two pieces it is drawn in, and nothing has to survive a round trip
  // through prose (ADR 0175).
  const consequence = el("div", "note");

  consequence.append(
    el("b", undefined, `This cancels ${many(plan.stays, "stay", "stays")}.`),
    document.createTextNode(plan.stays === 1 ? "" : " One at a time."),
  );

  const rows = plan.rows.map((row) => {
    const element = el("div", "fr");
    const value = el("div", "v");

    // The span the row is about, where it is about one, ahead of the value —
    // which is where the service used to put it as text. `Afterwards` is the
    // row whose whole value is a sentence about the rooms, so it is said here.
    const dates = row.arrive === undefined || row.arrive === null
      ? null
      : span(row.arrive, row.depart ?? null, property);

    value.append(document.createTextNode(
      row.label === "Afterwards" ? afterwards(plan.stays, dates) : joined(dates, row.value),
    ));

    if (row.strong !== undefined) {
      value.append(el("b", undefined, row.strong));
    }

    fill(value, ...tags(row.tags));
    element.append(el("div", "k", row.label), value);
    return element;
  });

  return dialog({
    title: "Cancel this booking?",
    subtitle: subject(plan, property),

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
        // It went on to say charging was "Finance's, a later round" — a
        // roadmap note, removed under the owner's ruling of 2026-09-19.
        "The penalty is recorded, not charged.",
      ),
    ],

    foot: "Recorded against your name.",

    actions: [
      { label: "Keep the booking", onClick: close },
      // **No reason, no write.** The command refuses a cancellation without
      // one, and this refuses to send one — so a property that has configured
      // no reasons gets the button drawn as unavailable, saying so, rather than
      // a server refusal after the fact.
      //
      // It used to be `off: reason === null` beside a hand-written
      // `onClick: reason === null ? () => undefined : …`, because `off` only
      // added a class and left the button live. The neutralising handler is no
      // longer the caller's to remember: an off action is a different shape and
      // cannot carry one.
      reason === null
        ? {
          label: destructive(plan),
          danger: true,
          off: true,
          why: "Choose a reason first — a cancellation is recorded against one.",
        }
        : {
          label: destructive(plan),
          danger: true,
          onClick: () => confirm(reason),
        },
    ],

    onDismiss: close,
  });
}

/**
 * What the destructive button says.
 *
 * **The count is in the label** because the dialog's whole argument is that this
 * is n cancellations rather than one — a button saying *Cancel the booking* over
 * a two-stay group would undo the sentence above it. It comes from
 * `plan.stays` and never from `plan.rows.length`, which is a mixed list and
 * once made this button offer to cancel three stays of a two-stay booking.
 *
 * A function rather than a `const`, so the two shapes of the action below say
 * it once each rather than drifting apart.
 */
function destructive(plan: CancelPlan): string {
  return plan.stays === 1 ? "Cancel this stay" : `Cancel all ${plan.stays} stays`;
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

/** `3 Sep → 7 Sep · no penalty agreed`, or either half alone. */
function joined(dates: string | null, value: string): string {
  if (dates === null) return value;
  return value === "" ? dates : `${dates} · ${value}`;
}

/**
 * What happens to the rooms, once the stays are cancelled.
 *
 * **A plan with nothing to cancel says so, and says why** — not *0 rooms
 * return*, which is arithmetic where the screen owes a reason. The service
 * wrote this whole sentence until 2026-09-20, including *both* for two rooms
 * and a date range compressed the way `en-GB` compresses one (ADR 0175).
 */
function afterwards(stays: number, dates: string | null): string {
  if (stays === 0) {
    return "nothing returns to inventory — no stay on this booking can be cancelled";
  }

  const rooms = stays === 1 ? "the room returns" : stays === 2 ? "both rooms return" : `all ${spell(stays)} rooms return`;

  return dates === null ? `${rooms} to inventory` : `${rooms} to inventory for ${dates}`;
}

/**
 * `BK-4506 · Fatima Sheikh · two stays, 3 – 7 September`.
 *
 * Reference and guest are the booking's own and may each be absent; the count
 * is the plan's, and the span is drawn in the property's locale. The count is
 * the CANCELLABLE stays rather than every stay on the booking, which is why it
 * comes from the plan and is not counted off the rows.
 */
function subject(plan: CancelPlan, property: PropertyEnvironment): string {
  const parts: string[] = [];

  if (plan.subject.reference !== null) parts.push(plan.subject.reference);
  if (plan.subject.guest !== null) parts.push(plan.subject.guest);

  const stays = many(plan.stays, "stay", "stays");

  parts.push(plan.subject.arrive === null
    ? stays
    : `${stays}, ${span(plan.subject.arrive, plan.subject.depart, property, "day-month-year")}`);

  return parts.join(" · ");
}

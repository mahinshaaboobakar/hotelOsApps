/**
 * Putting right a lifecycle fact recorded in error — the owner's C1 dialog,
 * ruled 2026-09-24.
 */

import type { HostApi, PropertyEnvironment } from "@hotelos/sdk";

import { APP, failureDrawing, load, perform, type CorrectPlan } from "../../book";
import { el } from "../../chrome/element";
import { field } from "../../chrome/field";
import { cannot, failed } from "../../chrome/marks";
import { instant } from "../../chrome/when";
import { detail } from "../../chrome/panel";
import { dialog, sheet } from "../../chrome/overlay";

/**
 * Read what correcting would do, then draw the dialog.
 *
 * **Where the correction is undrawn it says so rather than drawing one.**
 * `correct` returns null for a target no frame covers — today, everything but
 * a departure going back in house. The desk gets a sentence naming what is
 * missing, which is a design gap and not a platform failure, and the two are
 * not told in the same words.
 *
 * @param host the module host
 * @param into where the scrim goes
 * @param stayId the stay the desk is looking at
 * @param property whose zone the departure is read in
 * @param close what dismissing it does
 * @param done what to do once it is recorded
 */
export async function correctDialog(
  host: HostApi,
  into: HTMLElement,
  stayId: string,
  property: PropertyEnvironment,
  close: () => void,
  done: () => void,
): Promise<void> {
  const loaded = await load<CorrectPlan>(host, "stay.override", "correctPlan", { stayId });

  if (!loaded.ok) {
    into.append(failed(
      failureDrawing(loaded.failure, { app: APP, the: "what this correction would do" }),
      () => void correctDialog(host, into, stayId, property, close, done),
    ));
    return;
  }

  const plan = loaded.value;

  // `Rajesh Pillai · 214 · checked out 07:02` — facts, joined here, with the
  // instant read in the property's own zone rather than the machine's.
  const subject = [
    plan.guest,
    plan.room,
    plan.departedAt === null
      ? null
      : `checked out ${instant(plan.departedAt, property, "time")}`,
  ].filter((part): part is string => part !== null).join(" · ");

  const drawn = correct(plan, subject, close, (to, reason) => {
    void perform(host, "stay.override", "correct",
      { stayId, version: plan.version, to, reason }).then(done);
  });

  if (drawn !== null) {
    into.append(drawn);
    return;
  }

  // **Not a failure, and not drawn as one.** The platform answered; no frame
  // covers a correction out of this state. Saying "could not read" here would
  // send somebody to check the service, which is working.
  into.append(sheet({
    title: "This correction is not designed yet",
    subtitle: subject,
    body: [cannot(
      `Putting a stay back from ${plan.from} has no approved screen`,
      "The platform can record it and no frame draws what it should ask you"
      + " first, so it is not offered here. Nothing was attempted.",
    )],
    foot: null,
    actions: [{ label: "Close", onClick: close }],
    onDismiss: close,
  }));
}

/**
 * Draw the confirmation.
 *
 * **It says the mistake is not erased, because that is what makes this a
 * correction rather than an undo.** The departure happened, it was announced,
 * and other applications acted on it — so it is recorded beside the correction
 * and both stay in the stay's history. Somebody asking in November why 214
 * shows a departure clean on the 24th finds the answer in the list.
 *
 * **No reason, no write.** `CorrectAsync` refuses a correction without one in
 * as many words — *"without one it is indistinguishable from a mistake"* — and
 * this refuses to send one, so a property that has configured no reasons gets
 * the button drawn as unavailable saying so, rather than a server refusal
 * after the fact. Nothing configures that list yet, which is the same recorded
 * gap cancellation has and is reported the same way.
 *
 * **The conflicting room is refused rather than resolved.** The approved
 * frame's own caption says what happens when the room has been given to
 * somebody else "is not drawn here, and is the one case this pair leaves
 * open", so the dialog states the conflict and withholds the action instead of
 * inventing a resolution the owner has not seen.
 *
 * **ONE TARGET IS DRAWN AND ONE IS NOT.** Frame C1 draws a departure recorded
 * in error going back in house, to its end. `Reinstate…` appears on N1's end
 * state as a BUTTON and the dialog behind it was never drawn — so `words`
 * answers for `InHouse` and returns null for everything else, and the caller
 * does not offer what it cannot draw. Composing a plausible title here would
 * be inventing an application design the owner has not seen.
 *
 * @param plan what correcting it would do
 * @param subject who and which stay, as the head's second line
 * @param close what dismissing it does
 * @param confirm what to do with the reason when it is confirmed
 * @returns the scrim, or null where this correction is undrawn
 */
export function correct(
  plan: CorrectPlan,
  subject: string,
  close: () => void,
  confirm: (to: string, reason: string) => void,
): HTMLElement | null {
  const said = words(plan.to);
  if (said === null || plan.to === null) return null;

  // The reason showing, which is the one that would be recorded. Null when the
  // property has configured none — a real state, and the same one frame 8's
  // cancellation reports.
  const reason = plan.reasons[0] ?? null;

  const kept = el("div", "note");

  kept.append(
    el("b", undefined, "The departure is not erased."),
    document.createTextNode(
      " It happened, it was announced, and consumers acted on it — so this is"
      + " recorded as a correction beside it, and both stay in the stay's"
      + " history.",
    ),
  );

  return dialog({
    title: said.title,
    subtitle: subject,

    body: [
      kept,
      detail({ label: "Back to", value: "", strong: said.backTo, tags: [] }),
      room(plan),
      field({
        label: "Why",
        value: reason,
        aside: reason === null ? undefined : "▾",
        placeholder: "No reason is configured — this correction cannot be recorded",
      }),
      el(
        "div",
        "hint",
        "A reason is required. Without one a correction is indistinguishable"
        + " from a mistake, and this is the record somebody reads months later.",
      ),
    ],

    foot: "Recorded against your name.",

    actions: [
      { label: "Leave it", onClick: close },
      confirmation(plan, plan.to, said.confirm, reason, confirm),
    ],

    onDismiss: close,
  });
}

/**
 * What stands in the way of the room, where a room is involved at all.
 *
 * **Three states, and the third is not a missing boolean.** `null` means no
 * room is in the question — reinstating a no-show returns the stay to *booked*,
 * which holds none — while true and false are answers about a room. Reading
 * `null` as *taken* would tell the desk a room was held when there is no room.
 */
function room(plan: CorrectPlan): HTMLElement | null {
  if (plan.roomStillFree === null) return null;

  return plan.roomStillFree
    ? detail({
      label: "Room",
      value: `${plan.room ?? "the room"} is still free, so it returns to this stay`,
      tags: [{ kind: "pill", tone: "ok", text: "no conflict" }],
    })
    : detail({
      label: "Room",
      value: `${plan.room ?? "the room"} is held by another stay, so it cannot return here`,
      tags: [{ kind: "pill", tone: "warn", text: "conflict" }],
    });
}

/**
 * The primary, live only when it can actually do its work.
 *
 * Two independent things can withhold it, and each says which: no reason is
 * configured, or the room has gone. They are separate sentences because they
 * have opposite remedies — one is a setting nobody has filled in, the other is
 * a room somebody else now holds.
 */
function confirmation(
  plan: CorrectPlan,
  to: string,
  label: string,
  reason: string | null,
  confirm: (to: string, reason: string) => void,
) {
  if (plan.roomStillFree === false) {
    return {
      label,
      off: true as const,
      why: "The room is held by another stay. Free it or move that stay first.",
    };
  }

  if (reason === null) {
    return {
      label,
      off: true as const,
      why: "Choose a reason first — a correction is recorded against one.",
    };
  }

  return {
    label,
    primary: true,
    // Narrowed at the top: `words` returned non-null, so `to` is a state
    // this screen has drawn.
    onClick: () => confirm(to, reason),
  };
}

/**
 * The words for a correction into a given state — where they were drawn.
 *
 * **Null is the honest answer for a target nobody drew.** `Reinstate…` is on
 * N1's end state and what it opens is not in any frame; a title composed here
 * would read as approved design to the next person who finds it, which is the
 * provenance hazard no test can catch. The caller offers nothing rather than
 * offering an invention.
 */
function words(to: string | null): { title: string; backTo: string; confirm: string } | null {
  return to === "InHouse"
    ? {
      title: "Put this stay back in house?",
      backTo: "In house",
      confirm: "Put back in house",
    }
    : null;
}

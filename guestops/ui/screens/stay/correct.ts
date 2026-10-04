/**
 * Putting right a lifecycle fact recorded in error — the owner's C1 dialog,
 * ruled 2026-09-24.
 */

import type { HostApi, PropertyEnvironment } from "@hotelos/sdk";

import { APP, failureDrawing, load, perform, type CorrectPlan } from "../../book";
import { el } from "../../chrome/element";
import { field } from "../../chrome/field";
import { cannot, failed } from "../../chrome/marks";
import { instant, span } from "../../chrome/when";
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
  const loaded = await load<CorrectPlan>(host, "reservation.read", "correctPlan", { stayId });

  if (!loaded.ok) {
    into.append(failed(
      failureDrawing(loaded.failure, { app: APP, the: "what this correction would do" }),
      () => void correctDialog(host, into, stayId, property, close, done),
    ));
    return;
  }

  const plan = loaded.value;

  const drawn = correct(plan, subjectOf(plan, property), close, (to, reason) => {
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
    subtitle: subjectOf(plan, property),
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
 * The head's second line — `Rajesh Pillai · 214 · checked out 07:02`, and
 * fRI1's four-part form for a no-show.
 *
 * **Facts, joined here**, with every instant and day read in the property's own
 * zone rather than the machine's (ADR 0175). Each part is omitted where the
 * plan does not carry it, so a departure correction draws no no-show time and
 * a reinstatement draws no room.
 *
 * **`span` is consumed rather than composed.** A range's separator and the
 * month's position are a locale's grammar: the owner ruled the LONG form on
 * 2026-09-20 against mockup 07 B, and `when.ts` carries that ruling with *"do
 * not improve it into a compressed range"*. **fRI1's prose draws the short
 * `19 – 20 Aug`, which that ruling refused eight days before the frame
 * landed** — so the ruling governs here and the difference is reported rather
 * than matched, because matching it would reverse a ruling and ignoring it
 * would hide a conflict.
 *
 * Exported because it is the part a test can reach: `correctDialog` reads the
 * platform first, and the composition is where a locale's grammar would
 * otherwise be asserted by nobody.
 *
 * @param plan what correcting it would do
 * @param property whose zone and conventions the parts are read in
 * @returns the parts the plan carries, joined
 */
export function subjectOf(plan: CorrectPlan, property: PropertyEnvironment): string {
  return [
    plan.guest,
    plan.reference,
    plan.room,
    plan.arrive === null ? null : span(plan.arrive, plan.depart, property),
    plan.departedAt === null
      ? null
      : `checked out ${instant(plan.departedAt, property, "time")}`,
    plan.noShowAt === null
      ? null
      : `recorded as a no-show ${instant(plan.noShowAt, property, "date-time")}`,
  ].filter((part): part is string => part !== null).join(" · ");
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

  // **The clause names the fact being corrected**, because that is what the
  // desk is being told is kept. C1 says *departure*; fRI1 says *no-show*, and
  // a dialog putting back a stay that never arrived has no departure to talk
  // about.
  const kept = el("div", "note");

  kept.append(
    el("b", undefined, said.notErased),
    document.createTextNode(said.because),
  );

  return dialog({
    title: said.title,
    subtitle: subject,

    body: [
      kept,
      detail({ label: "Back to", value: "", strong: said.backTo, tags: [] }),
      room(plan),
      comesOff(plan),
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
 * What a reinstatement takes off — and ADR 0310 is why the answer is nothing.
 *
 * **Drawn only where there is a question.** A departure correction has no
 * forfeit to discuss; fRI1 draws this row because a no-show looks like the kind
 * of thing that charges something. It does not: `RecordNoShowAsync` moves the
 * lifecycle, bumps the version, records the override and publishes
 * `stay.no_show`, and **writes no forfeiture record of any kind** — planner,
 * ADR 0310. So the dialog says so rather than leaving the desk to wonder
 * whether putting the stay back undoes a charge.
 *
 * **If a forfeit ever becomes a recorded thing, this row moves WITH the service
 * change** and not ahead of it — ADR 0310's own sequencing.
 */
function comesOff(plan: CorrectPlan): HTMLElement | null {
  if (plan.to !== "Booked") return null;

  return detail({
    label: "Nothing comes off",
    value: "no charge was ever applied",
    tags: [{ kind: "pill", tone: "ok", text: "nothing to reverse" }],
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
 * **Null is still the honest answer for a target nobody drew**, and the set of
 * drawn targets is now two rather than one.
 *
 * ⚠ **This said: *"`Reinstate…` is on N1's end state and what it opens is not
 * in any frame"*.** That was true when it was written, on 2026-09-24, and
 * `247c854` falsified it four days later: the owner's `fRI1` draws the dialog
 * to its end — the title below, the no-show-not-erased clause, `Back to
 * Booked`, `Nothing comes off`, the reason required from ADR 0305's configured
 * list, and `Leave it` beside `Put back on the list`. **The old sentence is
 * kept because a reader meeting only the second arm cannot otherwise tell a
 * design that was drawn from one somebody composed here.**
 */
function words(to: string | null): Said | null {
  if (to === "InHouse") {
    return {
      title: "Put this stay back in house?",
      backTo: "In house",
      confirm: "Put back in house",
      notErased: "The departure is not erased.",
      because: " It happened, it was announced, and consumers acted on it — so"
        + " this is recorded as a correction beside it, and both stay in the"
        + " stay's history.",
    };
  }

  if (to === "Booked") {
    return {
      title: "Put this stay back on the arrivals list?",
      backTo: "Booked",
      confirm: "Put back on the list",
      notErased: "The no-show is not erased.",
      because: " It was recorded, it was announced, and it stays in the stay's"
        + " history — this is a correction beside it, exactly as a mistaken"
        + " check-out is.",
    };
  }

  return null;
}

/** The sentences one correction needs, all of them the screen's (ADR 0175). */
interface Said {
  title: string;
  backTo: string;
  confirm: string;
  notErased: string;
  because: string;
}

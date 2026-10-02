/**
 * Recording that nobody came — the owner's N1 dialog, ruled 2026-09-24.
 */

import type { HostApi, PropertyEnvironment } from "@hotelos/sdk";

import { APP, failureDrawing, load, perform, type NoShowPlan } from "../../book";
import { span } from "../../chrome/when";
import { spell } from "../../chrome/words";
import { el } from "../../chrome/element";
import { failed } from "../../chrome/marks";
import { detail } from "../../chrome/panel";
import { dialog } from "../../chrome/overlay";

/**
 * Read what recording it would do, then draw the dialog.
 *
 * **A failed read renders a FAILURE, never a plausible dialog.** An owner
 * ruling of 2026-09-09: a screen that cannot read shows what it could not read
 * and why, and offers the action that could fix it. A confirmation drawn over
 * an unread plan would ask the desk to forfeit a night on a figure nobody
 * fetched.
 *
 * @param host the module host
 * @param into where the scrim goes
 * @param stayId the stay the desk is looking at
 * @param property whose money and whose reading conventions
 * @param close what dismissing it does
 * @param done what to do once it is recorded
 */
export async function nobodyCameDialog(
  host: HostApi,
  into: HTMLElement,
  stayId: string,
  property: PropertyEnvironment,
  close: () => void,
  done: () => void,
): Promise<void> {
  const loaded = await load<NoShowPlan>(host, "reservation.read", "noShowPlan", { stayId });

  if (!loaded.ok) {
    into.append(failed(
      failureDrawing(loaded.failure, { app: APP, the: "what recording a no-show would do" }),
      () => void nobodyCameDialog(host, into, stayId, property, close, done),
    ));
    return;
  }

  const plan = loaded.value;

  // `Thomas George · BK-4361 · 19 – 20 Aug`, composed from facts. The shared
  // `span` decides the range's grammar in the property's locale — a second
  // range formatter here would drift in the half nobody reads.
  const subject = [
    plan.guest,
    plan.reference,
    span(plan.arrive, plan.depart, property),
  ].filter((part): part is string => part !== null).join(" · ");

  into.append(nobodyCame(plan, subject, close, () => {
    // The version the DIALOG read at, carried back — so a stay somebody else
    // moved between the read and the press is refused rather than overwritten.
    void perform(host, "stay.override", "noShow", { stayId, version: plan.version })
      .then(done);
  }, property));
}

/**
 * Draw the confirmation.
 *
 * **It says what a no-show is NOT.** The approved frame leads on the
 * distinction because the two are a keystroke apart on this screen and carry
 * different money: a no-show forfeits the night the terms name and is counted
 * in the property's own reporting; a cancellation is a different fact with a
 * different penalty. Both stay in the list, and the record has to be able to
 * tell them apart months later.
 *
 * **The forfeit is the property's own figure, composed here.** ADR 0175
 * §NUM-Q2: the service sends a decimal string and an ISO 4217 code, and the
 * screen renders them — a currency symbol and a grouping separator are the
 * reader's. Where the terms named no forfeit the dialog says so in words
 * rather than showing a zero, because zero is a forfeit of nothing and *no
 * terms* is nobody having agreed one.
 *
 * **The subject line is the caller's.** `Thomas George · BK-4361 · 19 – 20 Aug`
 * is composed prose carrying a compressed date range, and a range is a
 * grammar rather than a format — `en-GB` puts the month at the second end
 * because it puts the month after the day. The screen that already holds the
 * stay composes it; a service that sent it would have written one locale's
 * word order into a projection (ADR 0175).
 *
 * @param plan what recording it would do
 * @param subject who and which stay, as the head's second line
 * @param close what dismissing it does
 * @param confirm what to do when it is confirmed
 * @param property whose money and whose reading conventions
 * @returns the scrim, with the dialog on it
 */
export function nobodyCame(
  plan: NoShowPlan,
  subject: string,
  close: () => void,
  confirm: () => void,
  property: PropertyEnvironment,
): HTMLElement {
  const distinction = el("div", "note");

  distinction.append(
    el("b", undefined, "This is not a cancellation."),
    document.createTextNode(
      " A no-show forfeits the night the terms name and is counted in the"
      + " property's own reporting; a cancellation is a different fact with a"
      + " different penalty. Both stay in the list, and the two are told apart"
      + " by which one was recorded.",
    ),
  );

  return dialog({
    title: "Record that nobody came?",
    subtitle: subject,

    body: [
      distinction,
      forfeit(plan, property),
      detail({ label: "Afterwards", value: plan.afterwards, tags: [] }),
    ],

    foot: "Recorded against your name.",

    actions: [
      { label: "Not yet", onClick: close },
      { label: "Nobody came", primary: true, onClick: confirm },
    ],

    onDismiss: close,
  });
}

/**
 * The forfeited night, or the absence of one said out loud.
 *
 * **Three states, not two.** No terms stored at all, terms stored without a
 * currency, and an amount — and each gets its own sentence, because an amount
 * with no currency is a number a guest could be charged in the wrong
 * denomination, and that is a different problem from nobody having agreed a
 * forfeit.
 */
function forfeit(plan: NoShowPlan, property: PropertyEnvironment): HTMLElement {
  if (plan.forfeit === null) {
    return detail({
      label: "Forfeited",
      value: "no terms are stored for this stay, so no forfeit is named",
      quiet: true,
      tags: [],
    });
  }

  if ("unstated" in plan.forfeit) {
    return detail({
      label: "Forfeited",
      value: `the terms name a forfeit ${plan.forfeit.unstated}`,
      quiet: true,
      tags: [],
    });
  }

  const { amount, currency, basis, nights } = plan.forfeit;

  // The reader's conventions, from the property's locale — never the server's.
  // `Intl` is given the ISO code and decides the symbol, the grouping and where
  // they go; a screen that wrote `₹ ` in front of a number has fixed one
  // property's currency into every property's build.
  //
  // **A property with no locale gets the code and the figure, not the
  // runtime's default.** `Intl` with `undefined` silently adopts whatever
  // locale the machine happens to have, which would render one property's
  // money in another's conventions and look entirely correct. `INR 4800.00`
  // is plainer and is true everywhere.
  const shown = property.locale === null
    ? `${currency} ${amount}`
    : new Intl.NumberFormat(property.locale, {
      style: "currency",
      currency,
    }).format(Number(amount));

  const tags = [basis];

  // Spelled, and omitted rather than guessed where the terms named none.
  if (nights !== null && nights !== undefined) {
    tags.push(`${spell(nights).toUpperCase()} ${nights === 1 ? "NIGHT" : "NIGHTS"}`);
  }

  tags.push("PER THE BOOKING'S TERMS");

  return detail({
    label: "Forfeited",
    value: "",
    strong: shown,
    tags: tags.map((text) => ({ kind: "lock", tone: "neutral", text })),
  });
}

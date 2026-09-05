/**
 * One booking, and its stays. Gold frames 8 and 9.
 *
 * The same page in two conditions: frame 8 is a complete booking being
 * cancelled, frame 9 is an incomplete one drawn honestly. They are one screen
 * because the difference is in the data, not in the design — a booking whose
 * source claimed more rooms than it has sent shows the stays it has and says
 * what is missing in words.
 *
 * This screen keeps a title: it names a **record** — `Fatima Sheikh · BK-4506`
 * — which is what docs/working/64 §3 says a title is for. What it removes is a
 * heading that repeats a section name the bar already carries.
 */

import type { HostApi } from "@hotelos/sdk";

import {
  load,
  perform,
  recordedBooking,
  recordedCancelPlan,
  type BookingDetail,
  type GroupFact,
} from "../../book";
import { control, el, fill } from "../../chrome/element";
import { mark, standIn } from "../../chrome/marks";
import { card } from "../../chrome/panel";
import { cancel } from "./cancel";
import { table } from "./table";

/**
 * Render the booking.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 * @param id which booking
 * @param confirming true when the cancellation dialog is open
 * @param ask what the `Cancel…` action does
 * @param close what dismissing the dialog does
 * @param done what a completed cancellation does
 */
export async function booking(
  host: HostApi,
  into: HTMLElement,
  id: string,
  confirming: boolean,
  ask: () => void,
  close: () => void,
  done: () => void,
): Promise<void> {
  const loaded = await load(
    host, "reservation.read", "booking", recordedBooking, { bookingId: id });

  const record = loaded.value;

  const title = el("div", "title");
  const heading = el("div");
  const subtitle = el("div", "hsub");

  subtitle.append(document.createTextNode(record.summary));

  if (record.managedBy !== null) {
    subtitle.append(mark({ mark: "pms", text: record.managedBy }));
  }

  heading.append(el("div", "ht", names(record)), subtitle);

  title.append(
    heading,
    el("div", "grow"),
    // Outline, because this STARTS the destructive flow rather than performing
    // it — docs/working/64 §2. The filled one is in the dialog.
    control("btn danger", "Cancel…", ask),
  );

  const body = el("div", "body");
  fill(
    body,
    loaded.live ? null : standIn(loaded.because),

    // **Above the table, because it explains why there is one row.** A person
    // meeting a single row under a booking reference reads it as the whole
    // booking unless something says otherwise first.
    record.incomplete === null ? null : says(record.incomplete),

    table(record.stays),

    // The same fact under the table, answering the other question: not *what
    // am I looking at* but *why are the missing two not here*. Frame 9 says it
    // twice on purpose.
    record.incompleteDetail === null ? null : el("div", "note", record.incompleteDetail),

    record.elsewhere === null ? null : says(record.elsewhere),
    record.facts.length === 0 ? null : facts(record.facts),
  );

  // The scrim is a sibling of the body, not a child of it. `.body` scrolls, so
  // an overlay inside it would scroll away with the rows behind it and be
  // clipped at the body's edge — the dialog would slide off its own screen.
  into.replaceChildren(title, body);

  if (!confirming) {
    return;
  }

  // The plan is asked for when the dialog opens, not when the page loads: it
  // computes penalties from the stored offset **at the moment it is shown**
  // (R18), so fetching it with the page would put a stale number in front of
  // somebody about to agree to it.
  const plan = await load(
    host, "reservation.read", "cancelPlan", recordedCancelPlan, { bookingId: id });

  into.append(cancel(
    plan.value,
    close,
    (reason: string) => confirm(host, id, reason, done, into),
  ));
}

/**
 * What to call this booking.
 *
 * **A complete booking is named after its guest; an incomplete one is not.**
 * Frame 8 titles itself `Fatima Sheikh · BK-4506` and frame 9 titles itself
 * `Booking BK-4471` — and that is not the drawing being loose. A group whose
 * source has sent one of three rooms has no single guest, and naming the page
 * after the one who happens to have arrived would state something about the
 * booking that only holds for a third of it.
 */
function names(record: BookingDetail): string {
  return record.incomplete === null
    ? `${record.guest} · ${record.reference}`
    : `Booking ${record.reference}`;
}

/**
 * A statement about the booking, in the design's info band.
 *
 * Info-toned rather than warning: an incomplete group is an ordinary condition
 * of running a hotel with a PMS, and a group with legs at another property is
 * simply a fact. Neither is a problem to be resolved, so neither takes a colour
 * that asks somebody to act.
 */
function says(text: string): HTMLElement {
  const banner = el("div", "ban info");
  const body = el("div");
  const [lead, ...rest] = text.split(". ");

  body.append(
    el("b", undefined, `${lead ?? ""}.`),
    document.createTextNode(` ${rest.join(". ")}`),
  );

  banner.append(body);
  return banner;
}

/** How a group behaves, as three peer cards — frame 9. */
function facts(list: readonly GroupFact[]): HTMLElement {
  const grid = el("div", "grid3");

  for (const fact of list) {
    const { root, body } = card(fact.title);
    const row = el("div", "fr");
    const value = el("div", "v");

    value.append(document.createTextNode(fact.value));
    row.append(el("div", "k", fact.key), value);

    fill(body, row, el("div", "hint", fact.hint));
    grid.append(root);
  }

  return grid;
}

/**
 * Do it, and say what happened.
 *
 * On success the dialog closes and the page redraws from the server, so the
 * stays show their new status rather than the screen editing its own copy of
 * them — a client that patched its rows would be a second place the lifecycle
 * is decided.
 *
 * On refusal the dialog **stays open** carrying the reason. Closing it would
 * leave a person believing the booking was cancelled, which is the exact
 * failure the dialog's own PMS sentence exists to prevent.
 */
async function confirm(
  host: HostApi,
  bookingId: string,
  reason: string,
  done: () => void,
  into: HTMLElement,
): Promise<void> {
  const result = await perform(host, "stay.override", "cancel", { bookingId, reason });

  if (result.refused === null) {
    done();
    return;
  }

  const scrim = into.querySelector(".scrim");
  const body = scrim?.querySelector(".db");

  if (body === null || body === undefined) {
    return;
  }

  const banner = el("div", "ban gone");
  banner.append(el("b", undefined, "Nothing was cancelled."), el("span", "why", result.refused));

  // Prepended, because a refusal a person has to scroll to is a refusal they
  // will press the button again without reading.
  body.prepend(banner);
}

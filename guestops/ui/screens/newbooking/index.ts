/**
 * New booking — the answer before the sale. Gold frame 14.
 *
 * **Availability is computed from what we already hold; it is not a table
 * anyone feeds** (GUEST-Q7). Both modes are fully v1, because a standalone
 * property that cannot say what is free is not a property that can open — and
 * no new inventory owner was created to do it.
 *
 * **What this is not**: no pricing by occupancy, no minimum stay, no
 * closed-to-arrival, no travel-agent allotments. Those are revenue-management
 * concepts and the platform has named no owner for them.
 *
 * This screen keeps a title, unlike the list screens. `New booking` is not a
 * word the bar says — it names what is being done here, and docs/working/64 §3
 * removes only the heading that repeats a section name.
 */

import type { HostApi } from "@hotelos/sdk";

import { load, recordedAvailability, recordedConflict } from "../../book";
import { control, el, fill } from "../../chrome/element";
import { standIn } from "../../chrome/marks";
import { availability } from "./availability";
import { conflict } from "./conflict";
import { sources } from "./sources";

/**
 * Render the screen.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 * @param walkIn what the Walk-in action does
 */
export async function newBooking(
  host: HostApi,
  into: HTMLElement,
  walkIn: () => void,
): Promise<void> {
  // **The dates travel, and the backend refuses a request without them.** A
  // missing arrival could be read as *today*, and the answer would then be
  // availability for dates nobody asked about — in the column a guest is
  // quoted from. These are the dates the recorded query names until the sheet
  // captures a person's own.
  const loaded = await load(
    host, "reservation.read", "availability", recordedAvailability, {
      arrive: recordedAvailability.query.arriveOn,
      depart: recordedAvailability.query.departOn,
    });

  const answer = loaded.value;

  const title = el("div", "title");
  const heading = el("div");

  heading.append(el("div", "ht", "New booking"), el("div", "hsub", answer.mode));
  title.append(heading, el("div", "grow"), control("btn", "Walk-in", walkIn));

  const query = el("div", "fltr");
  query.append(
    box("Arrive", answer.query.arrive),
    box("Depart", answer.query.depart),
    party(answer.query.party),
  );

  const cards = el("div", "cols");
  cards.append(sources(), conflict(recordedConflict));

  const body = el("div", "body");
  fill(
    body,
    loaded.live ? null : standIn(loaded.because),
    query,
    availability(answer.types),
    explain(),
    cards,
  );

  into.replaceChildren(title, body);
}

/** A date field: its name, and the date in bold. */
function box(label: string, value: string): HTMLElement {
  const element = el("div", "inp");
  element.append(document.createTextNode(`${label} `), el("b", undefined, value));
  return element;
}

/** The party, which is a chooser rather than a date. */
function party(value: string): HTMLElement {
  const element = el("div", "inp");
  element.append(document.createTextNode(value), el("span", "grow", "▾"));
  return element;
}

/**
 * The note that explains the Suite row.
 *
 * Its own function because it is the screen's *argument* rather than one of its
 * values: four suites physically fine, unsold, and not for sale is the case
 * that separates **stop-sell** — our own setting, the seller's control — from
 * **out of order**, which is EngineeringOps saying a room cannot be used and
 * which we hear as an event. Neither is stored as inventory here.
 */
function explain(): HTMLElement {
  const note = el("div", "note");

  note.append(
    el("b", undefined, "The Suite row is the one that explains the design."),
    document.createTextNode(
      " Four suites are physically fine, unsold, and not for sale — a manager "
        + "held them for a wedding party. That is stop-sell: our own setting, "
        + "per room type and date range, the seller's control. The Deluxe "
        + "King's out-of-order room is a different thing entirely — "
        + "EngineeringOps says that room cannot be used, and we hear it as an "
        + "event. Neither number is stored as inventory here.",
    ),
  );

  return note;
}

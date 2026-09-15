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

import { pager } from "../../chrome/pager";
import { APP, failureDrawing, load, type Availability } from "../../book";
import { control, el, fill } from "../../chrome/element";
import { failed } from "../../chrome/marks";
import { availability } from "./availability";
import { sources } from "./sources";

/**
 * Render the screen.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 * @param walkIn what the Walk-in action does
 */
/** Room types per page — a catalogue, and a resort's is not eleven rows. */
const PAGE = 12;

export async function newBooking(
  host: HostApi,
  into: HTMLElement,
  page: number,
  turn: (page: number) => void,
  walkIn: () => void,
): Promise<void> {
  // **THE DATES ARE NOT SUPPLIED, AND THAT IS THE FIX RATHER THAN AN
  // OMISSION.** This asked with `recordedAvailability.query.arriveOn` and
  // `departOn` — the fixture's dates — because the backend refuses a request
  // without them and nothing on this screen captures a person's own yet.
  //
  // It is a different defect from the five fixtures that were being *drawn*,
  // and the remedy is the opposite one: those stop drawing, this stops
  // supplying. A fixture reaching a screen shows a person data that is not
  // theirs; invented input reaching a real query returns **a plausible answer
  // to a question nobody asked** — availability for a fortnight in the column
  // a guest is quoted a rate from. Of the two, this is the one a person acts
  // on without noticing.
  //
  // So the screen asks without them and lets the service answer. The backend
  // refuses a request with no dates, and **that refusal is the truth about this
  // screen today** — a person sees the service's own sentence rather than a
  // fortnight nobody chose. The date capture is Phase 3's; building it here
  // would be feature work wearing a conversion's clothes.
  const loaded = await load<Availability>(
    host, "reservation.read", "availability", { page, pageSize: PAGE });

  // **A read that did not answer renders the failure, not a stand-in** —
  // APPS-Q42. Nothing below this line runs on data nobody's platform produced.
  if (!loaded.ok) {
    into.replaceChildren(
      failed(
        failureDrawing(loaded.failure, { app: APP, the: "this property's availability" }),
        () => turn(page),
      ));
    return;
  }

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

  // **The conflict panel is gone, not emptied.** It drew `recordedConflict`
  // unconditionally, beside a live availability read — so on a real property
  // the rooms were the property's and the clash beneath them was a fixture's.
  // No method serves a conflict, so there is nothing to call: the panel returns
  // when the backend has one to answer with.
  const cards = el("div", "cols");
  cards.append(sources());

  const body = el("div", "body");
  fill(
    body,
    query,
    availability(answer.types),
    pager(answer.total, page, PAGE, answer.types.length, turn),
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

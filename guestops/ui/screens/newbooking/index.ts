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

import { APP, failureDrawing, load, type Availability } from "../../book";
import { control, el, fill } from "../../chrome/element";
import { failed } from "../../chrome/marks";
import { day } from "../../chrome/when";
import { availability } from "./availability";

/**
 * Render the screen.
 *
 * **Every room type, with only the list scrolling — the owner's ruling on this
 * screen, 2026-09-19 (G7).** It paged by twelve, and frame 14 put an
 * explanatory note and a "where each number comes from" card under the pager,
 * leaving the list two rows tall. The owner ruled both panels developer notes
 * that should never have been built as screen, and the list unpaged.
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
  // **THE DATES ARE NOT SUPPLIED, AND THAT IS THE FIX RATHER THAN AN
  // OMISSION.** This asked with the fixture's dates because the backend refuses
  // a request without them and nothing on this screen captures a person's own
  // yet. Invented input reaching a real query returns a plausible answer to a
  // question nobody asked — availability for a fortnight in the column a guest
  // is quoted from. So the screen asks without them, and the service's refusal
  // is the truth about this screen until the booking flow the owner asked for
  // (2026-09-19) is drawn, approved and built.
  const loaded = await load<Availability>(host, "reservation.read", "availability");

  // **A read that did not answer renders the failure, not a stand-in** —
  // APPS-Q42. Nothing below this line runs on data nobody's platform produced.
  if (!loaded.ok) {
    into.replaceChildren(
      failed(
        failureDrawing(loaded.failure, { app: APP, the: "this property's availability" }),
        () => void newBooking(host, into, walkIn),
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
    box("Arrive", day(answer.query.arrive, host.property, "day-month")),
    box("Depart", day(answer.query.depart, host.property, "day-month")),
    party(answer.query.party),
  );

  // `unpaged`: the list is the scroll container and the body is not — CORE-Q28
  // for a list with no pager (chrome/styles/table.ts).
  const body = el("div", "body unpaged");
  fill(body, query, availability(answer.types));

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

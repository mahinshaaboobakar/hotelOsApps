/**
 * The Servicing tab — a night at a time, and none of it ours. Frame 6.
 */

import type { Night, Servicing } from "../../book";
import { el, fill, unavailable } from "../../chrome/element";
import { mark } from "../../chrome/marks";
import { card } from "../../chrome/panel";

/**
 * Draw the tab.
 *
 * **A four-night stay is cleaned four times**, and the desk is asked about it —
 * *"has anyone been in my room?"*. That is why this is a strip per night rather
 * than one status: a room that sat empty before arrival is **freshened, not
 * turned around**; a room whose guest is due out and into which nobody arrives
 * tonight is **stripped rather than made ready**; and **a declined day is
 * neither clean nor dirty, it is declined** (R1, R3).
 *
 * **GuestOps owns none of it.** It announces occupancy and departure; Room Care
 * decides what work that becomes (APPS-Q1, S21). This tab reports and asserts
 * nothing.
 *
 * @param servicing the nights, where Room Care answered
 * @returns the tab's contents
 */
export function servicingTab(servicing: Servicing): readonly HTMLElement[] {
  // **Three states, not two.** Room Care absent is an invitation; Room Care
  // present with no record is a different sentence entirely; and only the third
  // draws the strip. Collapsing the first two — which one `nights === null`
  // check would do — makes the screen tell a property that Room Care is not
  // installed while it is running.
  if (servicing.roomCareInstalled === false) {
    return [absent()];
  }

  if (servicing.nights === null) {
    return [unread()];
  }

  const strip = el("div", "nights");

  for (const night of servicing.nights) {
    strip.append(cell(night));
  }

  // A banner naming whose record this is and the route it is read by, and two
  // cards on why a day can be blank and what the desk may do, stood around the
  // strip — developer notes from the frame, removed under
  // the owner's ruling of 2026-09-19: a mock's notes for the developer are never built as screen.
  return [strip];
}

/** One night. */
function cell(night: Night): HTMLElement {
  const element = el("div", night.now ? "ng now" : "ng");

  const date = el("div", "dt");
  date.append(
    document.createTextNode(`${night.weekday} `),
    el("b", undefined, night.date),
  );

  if (night.qualifier !== null) {
    date.append(document.createTextNode(` · ${night.qualifier}`));
  }

  const state = el("div", "st");
  fill(
    state,
    // A fact carries a mark and a plan carries a pill: `Serviced 10:20` happened
    // and `Planned` has not, and drawing them the same way would let the desk
    // tell a guest the room was done when it is merely scheduled.
    night.mark === null ? null : mark(night.mark),
    night.state === null ? null : el("span", `pill ${night.stateTone}`, night.state),
    night.detail === null ? null : el("span", "hint", night.detail),
    night.action === null ? null : unavailable("link", night.action, "Not available from this screen yet."),
  );

  element.append(date, state);
  return element;
}

/**
 * Room Care is here and its record is not reachable from this build.
 *
 * Stated as what it is — a read this application cannot make — rather than as
 * an empty strip, which would read as *nobody has been in the room*. That is
 * the answer a guest gets told, and it would be wrong.
 */
function unread(): HTMLElement {
  const { root, body } = card("Nothing to show here yet");

  body.append(el(
    "div",
    "hint",
    // The reason stays — it is this screen's failure sentence — in plain words.
    // It named the service it is read through and the certificate that service
    // wants until 2026-09-19.
    "GuestOps cannot show Room Care's record for this stay yet. This does not "
      + "mean nobody has been in the room.",
  ));

  return root;
}

/**
 * Room Care is not installed, so there is nothing to report.
 *
 * The tab is reached only when it is dimmed rather than hidden — which is
 * deliberate: *which tabs a stay has* is itself information, and a property
 * looking at a dimmed Servicing learns that servicing is a thing HotelOS can
 * show them.
 */
function absent(): HTMLElement {
  const root = el("div", "card ghost");
  const empty = el("div", "empty");
  const text = el("p");

  text.append(
    document.createTextNode("Nothing here is GuestOps's to record. Install "),
    el("b", undefined, "Room Care"),
    document.createTextNode(
      " from Software Center to see what happened in the room while the guest "
        + "was in it.",
    ),
  );

  empty.append(
    el("div", "ic", "◍"),
    el("b", undefined, "Room Care is not installed"),
    text,
  );

  root.append(empty);
  return root;
}

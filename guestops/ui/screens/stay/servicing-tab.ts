/**
 * The Servicing tab — a night at a time, and none of it ours. Frame 6.
 */

import type { Night, Servicing } from "../../book";
import { control, el, fill } from "../../chrome/element";
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
    return [attribution(), unread()];
  }

  const strip = el("div", "nights");

  for (const night of servicing.nights) {
    strip.append(cell(night));
  }

  return [attribution(), strip, explanations()];
}

/** Whose record this is, said before any of it is read. */
function attribution(): HTMLElement {
  const banner = el("div", "ban info");
  const text = el("div");

  text.append(
    el("b", undefined, "All of this is Room Care's."),
    document.createTextNode(
      " GuestOps shows it because the desk is asked about it — it is read "
        + "through the Context Service and stored nowhere here.",
    ),
  );

  banner.append(text);
  return banner;
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
    night.action === null ? null : control("link", night.action),
  );

  element.append(date, state);
  return element;
}

/** The two cards: why a day can be blank, and what the desk may do. */
function explanations(): HTMLElement {
  const cols = el("div", "cols");
  const why = card("Why a day can be blank");

  const policy = el("div", "note");
  policy.append(
    el("b", undefined, "Cleaning is policy, not a consequence."),
    document.createTextNode(
      " Not every hotel services every room every day; a declined day is a real "
        + "outcome, and a room nobody arrives into tonight may be cleaned "
        + "tomorrow. Room Care decides all of it — this tab reports, and asserts "
        + "nothing.",
    ),
  );

  fill(
    why.body,
    policy,
    el(
      "div",
      "hint",
      "A one-night stay shows two rows and is rarely opened. This tab earns its "
        + "place on the long stays, which is where the desk gets asked “has "
        + "anyone been in my room?”",
    ),
  );

  const can = card("What the desk can do here");

  fill(
    can.body,
    row("Ask for service", "records the guest's request and hands it on", null),
    row("Decline recorded", "by Room Care, not by GuestOps", null),
    row("Not available", null, "ASSIGNING AN ATTENDANT"),
    row("Never", null, "BLOCKING A CHECK-IN ON A DIRTY ROOM"),
    el(
      "div",
      "hint",
      "Asking is a request, exactly like a job's. Who cleans, in what order, and "
        + "to what standard is Room Care's — and if Room Care is not installed "
        + "this tab does not exist. What does not change either way is the "
        + "check-in: readiness is shown here, never enforced at the desk.",
    ),
  );

  cols.append(why.root, can.root);
  return cols;
}

/** One capability line, present or refused. */
function row(label: string, value: string | null, refused: string | null): HTMLElement {
  const element = el("div", "fr");
  const cell_ = el("div", "v");

  if (value !== null) {
    cell_.append(document.createTextNode(value));
  }

  if (refused !== null) {
    cell_.append(el("span", "lock no", refused));
  }

  element.append(el("div", "k", label), cell_);
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
    "Room Care answers for this property, and GuestOps cannot read its record "
      + "yet: the servicing history is resolved through the Context Service, "
      + "which an installed application cannot call until it is enrolled with a "
      + "service certificate. This is a gap in the platform, not an empty room "
      + "— nothing here says whether anybody has been in it.",
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

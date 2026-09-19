/**
 * The Activity tab — everything that happened, with who said it. Frame 4.
 */

import { formatInstant, type PropertyEnvironment } from "@hotelos/sdk";

import type { Activity, ActivityEntry } from "../../book";
import { el, unavailable } from "../../chrome/element";
import { mark } from "../../chrome/marks";

const COLUMNS = ["When", "Who", "What"] as const;

/**
 * Draw the list.
 *
 * **This is the screen that answers a complaint.** An activity list showing
 * only GuestOps's own facts would answer none of the questions a duty manager
 * asks at 9 p.m., because half the story belongs to Opera, Room Care and Jobs.
 *
 * **The disagreement is a row like any other**, in place and in time, rather
 * than a banner that vanishes when it is cleared — clearing adds a row, it
 * never removes one.
 *
 * @param activity the filters, and what is showing
 * @param property whose zone and locale the times are drawn in
 * @returns the tab's contents
 */
export function activityTab(activity: Activity, property: PropertyEnvironment): readonly HTMLElement[] {
  // A note under the list explained where each kind of row comes from — which
  // service reads another application's records, and what happens when one is
  // uninstalled. Removed under the owner's ruling of 2026-09-19: a mock's notes for the developer are never built as screen.
  return [sources(activity), list(activity.entries, property)];
}

/** The four source filters, and the note about ordering. */
function sources(activity: Activity): HTMLElement {
  const row = el("div", "acts");

  for (const filter of activity.filters) {
    // The service names the filters and nothing here applies one yet: the
    // chosen one is drawn as the state it is, and none can be pressed.
    row.append(unavailable(filter.on ? "btn sm pri" : "btn sm", filter.label,
      "Filtering this list is not available yet."));
  }

  // Both facts a reader needs to interpret the list, and neither is guessable:
  // the order, and whose clock the times are on. A list of instants with no
  // stated zone is R12's defect on a screen instead of in a column.
  row.append(el("div", "hint grow", "Newest last · times are the property's"));
  return row;
}

/** The rows. */
function list(entries: readonly ActivityEntry[], property: PropertyEnvironment): HTMLElement {
  const element = el("div", "tbl");
  const head = el("div", "ev hd");

  for (const column of COLUMNS) {
    head.append(el("div", undefined, column));
  }

  element.append(head);

  if (entries.length === 0) {
    const empty = el("div", "ev");
    empty.append(el("div", "hint", "Nothing has happened to this stay yet."));
    element.append(empty);
    return element;
  }

  for (const entry of entries) {
    element.append(line(entry, property));
  }

  return element;
}

function line(entry: ActivityEntry, property: PropertyEnvironment): HTMLElement {
  const element = el("div", `ev${entry.disagrees ? " disagrees" : ""}`);

  const when = el("div", "tm");
  when.append(
    el("b", undefined, formatInstant(entry.at, property, "date")),
    el("span", undefined, formatInstant(entry.at, property, "time")),
  );

  const who = el("div");
  who.append(mark(entry.who));

  const what = el("div", "w");
  what.append(
    document.createTextNode(entry.what),
    ...(entry.detail === null ? [] : [el("span", undefined, entry.detail)]),
  );

  element.append(when, who, what);
  return element;
}

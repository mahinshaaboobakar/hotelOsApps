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
  return [sources(activity), list(activity.entries, property), provenance()];
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
    el("span", undefined, entry.detail),
  );

  element.append(when, who, what);
  return element;
}

/**
 * The three kinds of source, named under the list.
 *
 * On the screen rather than in a document because it is what makes the list
 * safe to read: another application's rows are **read through the Context
 * Service and stored nowhere here**, so if that application is uninstalled
 * tomorrow its rows stop appearing and nothing in this stay's history is
 * orphaned.
 */
function provenance(): HTMLElement {
  const note = el("div", "note");

  note.append(
    el("b", undefined, "Three sources, one list, and the difference is never hidden."),
    document.createTextNode(" "),
    mark({ mark: "pms", text: "Opera" }),
    document.createTextNode(" is a fact the PMS wrote. "),
    mark({ mark: "override", text: "a person" }),
    document.createTextNode(" is one of ours, named. "),
    mark({ mark: "other", text: "another app" }),
    document.createTextNode(
      " is Room Care's or Jobs' own record, read through the Context Service and "
        + "stored nowhere here — if that application is uninstalled tomorrow, its "
        + "rows simply stop appearing, and nothing in this stay's history is "
        + "orphaned.",
    ),
  );

  return note;
}

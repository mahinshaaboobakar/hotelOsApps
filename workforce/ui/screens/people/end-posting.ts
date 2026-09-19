/**
 * Ending a posting — and the consequence stated before the button.
 *
 * # This is the round's finding, and drawing it is what surfaced it
 *
 * Ending a posting closes every team membership it supported. That has been
 * true since the object was built, is enforced in the posting's own
 * transaction, and is tested — and **nothing told anybody**. A supervisor ended
 * a posting, two teams quietly emptied, and no surface mentioned it.
 *
 * The panel below is the whole fix: *the consequence stated before the button
 * rather than as a toast afterwards*. A toast arrives when the decision is
 * already made and reports what a person can no longer choose about; a panel
 * arrives while they still hold the choice.
 *
 * # It needs no new logic here, deliberately
 *
 * The memberships come from the service's own read — the same query the write
 * makes. A screen that predicted the consequence with its own rule would
 * eventually predict it wrongly, and the version a person read would be the
 * wrong one.
 */

import { formatDay, type HostApi, type PropertyEnvironment } from "@hotelos/sdk";

import { foot } from "../../chrome/confirm";
import { el, fill } from "../../chrome/element";
import { overlay } from "../../chrome/overlay";
import { UNKNOWN_OUTCOME, write, WriteRefused } from "../../roster";
import type { PostingEnding, Supported } from "../../roster/team";

/**
 * Build the dialog.
 *
 * @param close called when it is dismissed
 * @param ending what the service says this is about to do
 * @returns the overlay
 */
export function endPosting(
  host: HostApi,
  close: () => void,
  ending: PostingEnding,
  done: () => void,
): HTMLElement {
  const head = el("div");
  head.append(
    el("div", "ht", `End ${ending.who}'s posting in ${ending.department}?`));

  const refusal = el("div", "note warn");

  // Destructive, so the confirm is filled — §2. Nothing is outstanding: the
  // posting is the row's and the last day is the service's, so it is live from
  // the moment the dialog opens.
  const acts = foot("End posting", "Ending…", close, "destructive");

  acts.onConfirm(() => {
    void (async () => {
      refusal.replaceChildren();
      acts.working(true);

      try {
        await write(host, "posting.assign", "end", {
          id: ending.id,
          version: ending.version,
          lastDay: ending.lastDay,
        });
        done();
      } catch (error) {
        // §9: the dialog stays open with the reason. Closing on a failed end
        // would leave a supervisor believing somebody had been taken off the
        // rota — and the panel above has just told them what else went with it.
        refusal.append(el("span", undefined,
          error instanceof WriteRefused
            ? error.message
            : UNKNOWN_OUTCOME));
        acts.working(false);

        if (!(error instanceof WriteRefused)) throw error;
      }
    })();
  });

  // A dialog: the person is confirming an end (§9).
  return overlay("dialog", {
    head: [head],
    body: [
      lastDay(ending, host.property),
      // Absent when the posting holds nothing open — the panel is a statement
      // about this posting, not furniture that appears empty.
      ...(ending.alsoEnds.length === 0
        ? []
        : [consequence(ending.alsoEnds, ending.department, host.property)]),
      refusal,
    ],
    foot: [acts.row],
  }, close);
}

/**
 * The day it ends.
 *
 * Drawn rather than typed into — §10 — because nothing behind this dialog
 * accepts a different day: the service answers the property's today and the
 * write sends that same value back. A date control here would be a field that
 * takes a value nothing carries.
 */
function lastDay(ending: PostingEnding, property: PropertyEnvironment): HTMLElement {
  const field = el("div", "fld");

  return fill(field,
    el("div", "fld-label", "Last day"),
    el("div", "inp", formatDay(ending.lastDay, property, "day-month-year")));
}

/** What else this closes, listed before the button that does it. */
function consequence(
  teams: readonly Supported[], department: string, property: PropertyEnvironment,
): HTMLElement {
  const panel = el("div", "conseq");

  panel.append(el("em", undefined, "This also ends"));

  for (const team of teams) {
    const row = el("div", "cr");
    row.append(
      el("b", "code neutral", team.department),
      el("b", undefined, team.team),
      // "member since 12 Mar" - the words are the screen's and the day is the
      // wire's. The fixture used to carry the whole phrase.
      el("span", "quiet",
        `— member since ${formatDay(team.since, property, "day-month-year")}`));
    panel.append(row);
  }

  const why = el("div", "note");
  why.append(el("span", undefined,
    "A team routes work to its members. Somebody with no posting in "
    + `${department} cannot be assigned there, so these close on the same day `
    + "— in the same save."));

  panel.append(why);
  return panel;
}

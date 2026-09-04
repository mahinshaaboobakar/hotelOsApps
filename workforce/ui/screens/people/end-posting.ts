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

import { el, fill } from "../../chrome/element";
import type { PostingEnding, Supported } from "../../roster/team";
import { recordedPostingEnding } from "../../roster/teams";

/**
 * Build the dialog.
 *
 * @param close called when it is dismissed
 * @param ending what the service says this is about to do
 * @returns the overlay
 */
export function endPosting(
  close: () => void,
  ending: PostingEnding = recordedPostingEnding,
): HTMLElement {
  const scrim = el("div", "scrim");
  const dialog = el("div", "dlg");

  const head = el("div");
  head.append(
    el("div", "ht", `End ${ending.who}'s posting in ${ending.department}?`));

  fill(dialog, head, lastDay(ending),
    // Absent when the posting holds nothing open — the panel is a statement
    // about this posting, not furniture that appears empty.
    ending.alsoEnds.length === 0 ? null : consequence(ending.alsoEnds, ending.department),
    actions(close));

  scrim.append(dialog);
  scrim.addEventListener("click", (event) => {
    if (event.target === scrim) close();
  });

  return scrim;
}

/** The day it ends. */
function lastDay(ending: PostingEnding): HTMLElement {
  const field = el("div", "fld");

  return fill(field,
    el("div", "flab", "Last day"),
    el("div", "finput", ending.lastDay));
}

/** What else this closes, listed before the button that does it. */
function consequence(teams: readonly Supported[], department: string): HTMLElement {
  const panel = el("div", "conseq");

  panel.append(el("em", undefined, "This also ends"));

  for (const team of teams) {
    const row = el("div", "cr");
    row.append(
      el("b", "code neutral", team.department),
      el("b", undefined, team.team),
      el("span", "quiet", `— ${team.since}`));
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

function actions(close: () => void): HTMLElement {
  const row = el("div", "acts");

  const cancel = el("button", "btn", "Cancel");
  cancel.setAttribute("type", "button");
  cancel.addEventListener("click", close);

  const confirm = el("button", "btn danger", "End posting");
  confirm.setAttribute("type", "button");

  return fill(row, el("div", "grow"), cancel, confirm);
}

/**
 * Add a member — and the one person who cannot be added.
 *
 * # The refusal is shown, never filtered out
 *
 * The service refuses somebody who holds no posting in the team's department on
 * the day the membership starts. A picker that simply omitted them would leave
 * a supervisor scrolling for Joseph and concluding the screen is broken; drawn
 * and dashed with the reason beside him, the rule teaches itself once.
 *
 * # The day is the caller's, and it is checked against that day
 *
 * Next week's crew is formed against **next week's** postings. Checking today
 * would refuse the person who starts on Monday, which is exactly the crew a
 * supervisor sits down on Friday to build.
 */

import { el, fill } from "../../chrome/element";
import { recordedTeams } from "../../roster/teams";
import type { Candidate } from "../../roster/team";

/**
 * Build the dialog.
 *
 * @param close called when it is dismissed
 * @returns the overlay
 */
export function addMember(close: () => void): HTMLElement {
  const scrim = el("div", "scrim");
  const dialog = el("div", "dlg");
  const open = recordedTeams.detail;

  const department = open?.team.departmentName ?? "its department";

  const head = el("div");
  head.append(
    el("div", "ht", "Add a member"),
    el("div", "hsub", `To ${open?.team.name ?? "this team"}, in ${department}.`));

  fill(
    dialog,
    head, from(), who(open?.candidates ?? []),
    // The refusal explains itself only when there is one to explain.
    (open?.candidates ?? []).some((one) => one.refused !== null)
      ? why(open?.candidates ?? [], department)
      : null,
    actions(close));

  scrim.append(dialog);
  scrim.addEventListener("click", (event) => {
    if (event.target === scrim) close();
  });

  return scrim;
}

/** The day the membership starts. */
function from(): HTMLElement {
  const field = el("div", "fld");
  const input = el("div", "finput", "Thu 4 Sep 2026");

  const note = el("div", "note");
  note.append(
    el("span", undefined, "The day the membership starts. Next week's crew is formed against "),
    el("b", undefined, "next week's"),
    el("span", undefined, " postings, not today's."));

  return fill(field, el("div", "flab", "From"), input, note);
}

/** Everybody the picker offers, and the one it refuses. */
function who(candidates: readonly Candidate[]): HTMLElement {
  const field = el("div", "fld");
  const list = el("div", "tlist");

  candidates.forEach((candidate, index) => {
    const row = el(
      "button",
      `tmem${candidate.refused === null ? (index === 0 ? " on" : "") : " no"}`);
    row.setAttribute("type", "button");
    if (candidate.refused !== null) row.setAttribute("aria-disabled", "true");

    const person = el("div");
    person.append(
      el("b", undefined, candidate.name),
      el("s", undefined, `${candidate.role} · ${candidate.department}`));

    fill(
      row,
      el("div", "av", initials(candidate.name)),
      person,
      el("div", "grow"),
      candidate.refused === null ? null : el("span", "pill bad", candidate.refused));

    list.append(row);
  });

  return fill(field, el("div", "flab", "Who"), list);
}

/**
 * Why a row is refused — the invariant, in a sentence, naming the person.
 *
 * The sentence is built from the candidates rather than written out, so a
 * screen showing two refusals cannot explain one of them.
 */
function why(candidates: readonly Candidate[], department: string): HTMLElement {
  const note = el("div", "note twarn");
  const refused = candidates.filter((one) => one.refused !== null);
  const names = refused.map((one) => one.name.split(" ")[0]).join(" and ");

  note.append(el("span", undefined,
    `${names} holds no posting in ${department} on the day above. A team exists `
    + "to receive work in its own department, so a member who cannot be "
    + "assigned there is a row that lies."));

  return note;
}

/** Two letters from a name this module borrowed and does not keep. */
function initials(name: string): string {
  const parts = name.split(" ").filter((part) => part.length > 0);
  const first = parts[0]?.[0] ?? "";
  const last = parts.length > 1 ? parts[parts.length - 1]?.[0] ?? "" : "";

  return `${first}${last}`.toUpperCase();
}

function actions(close: () => void): HTMLElement {
  const row = el("div", "acts");

  const cancel = el("button", "btn", "Cancel");
  cancel.setAttribute("type", "button");
  cancel.addEventListener("click", close);

  const confirm = el("button", "btn pri", "Add to team");
  confirm.setAttribute("type", "button");

  return fill(row, el("div", "grow"), cancel, confirm);
}

/**
 * Stand a team down — and the toggle that says what happens to its people.
 *
 * # Deactivate and Reactivate, never Archive and Restore
 *
 * ADR 0062's vocabulary, and reactivation is required rather than optional: a
 * deactivate with no counterpart states a capability in the schema and withholds
 * it from the service. A seasonal crew comes back.
 *
 * # The toggle defaults to keeping them, because that is what seasonal means
 *
 * A crew stood down for the low season returns with the same people, so the
 * switch is **on** and standing down is not a disband. The other position is a
 * second decision, made once, at the moment somebody decides it — and even then
 * the memberships **close** rather than vanish, because *who was in this team in
 * March* is still a question.
 */

import { el, fill } from "../../chrome/element";
import type { TeamDetail } from "../../roster/team";

/**
 * Build the dialog.
 *
 * @param close called when it is dismissed
 * @param open the team the detail pane has open — the one being stood down
 * @returns the overlay
 */
export function standDown(close: () => void, open: TeamDetail): HTMLElement {
  const scrim = el("div", "scrim");
  const dialog = el("div", "dlg");

  const members = open.members.length;

  const head = el("div");
  head.append(
    el("div", "ht", `Stand down ${open.team.name}?`),
    el("div", "hsub", `${open.team.departmentName} · ${members} members`));

  dialog.append(head, what(), keep(members), actions(close));

  scrim.append(dialog);
  scrim.addEventListener("click", (event) => {
    if (event.target === scrim) close();
  });

  return scrim;
}

/** What standing down does, and what it does not. */
function what(): HTMLElement {
  const note = el("div", "note");

  note.append(
    el("span", undefined, "It stops being offered when work is assigned. "),
    el("b", undefined, "It does not disappear"),
    el("span", undefined,
      " — jobs already given to it are in somebody's history, and the team can "
      + "be brought back."));

  return note;
}

/**
 * The toggle.
 *
 * A real button rather than a styled div: a switch that cannot be reached from
 * a keyboard is a decision only a mouse can make.
 *
 * @param members how many people the decision is about
 * @returns the row
 */
function keep(members: number): HTMLElement {
  const row = el("div", "tog");
  const label = el("div");

  label.append(
    el("span", undefined, `Keep its ${members} members`),
    el("s", undefined, "They stay recorded, and return with the team"));

  const toggle = el("button", "tsw on");
  toggle.setAttribute("type", "button");
  toggle.setAttribute("role", "switch");
  toggle.setAttribute("aria-checked", "true");
  toggle.setAttribute("aria-label", "Keep its members");

  toggle.addEventListener("click", () => {
    const on = toggle.classList.toggle("on");
    toggle.setAttribute("aria-checked", String(on));
  });

  return fill(row, label, toggle);
}

function actions(close: () => void): HTMLElement {
  const row = el("div", "acts");

  const cancel = el("button", "btn", "Cancel");
  cancel.setAttribute("type", "button");
  cancel.addEventListener("click", close);

  const confirm = el("button", "btn danger confirm", "Stand down");
  confirm.setAttribute("type", "button");

  return fill(row, el("div", "grow"), cancel, confirm);
}

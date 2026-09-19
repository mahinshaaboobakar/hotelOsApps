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

import { formatNumber, type HostApi } from "@hotelos/sdk";

import { foot } from "../../chrome/confirm";
import { el, fill } from "../../chrome/element";
import { overlay } from "../../chrome/overlay";
import { UNKNOWN_OUTCOME, write, WriteRefused } from "../../roster";
import type { TeamDetail } from "../../roster/team";

/**
 * Build the dialog.
 *
 * @param host the bridge
 * @param close called when it is dismissed
 * @param open the team the detail pane has open — the one being stood down
 * @param done called after it is stood down, so the list is re-read
 * @returns the overlay
 */
export function standDown(
  host: HostApi,
  close: () => void,
  open: TeamDetail,
  done: () => void,
): HTMLElement {
  const members = open.members.length;
  const many = formatNumber(members, host.property, "whole");

  // The toggle's position, held here because the write carries it. It starts
  // where the switch starts, and the switch starts where the SERVICE's own
  // default is — `keepMembers` defaults to true on the wire, so the screen and
  // the service agree about what happens if nobody touches anything.
  let keepMembers = true;

  const head = el("div");
  head.append(
    el("div", "ht", `Stand down ${open.team.name}?`),
    el("div", "hsub", `${open.team.departmentName} · ${many} members`));

  const refusal = el("div", "note warn");

  // **Destructive, so the confirm is FILLED** — page 64 §2. There is nothing
  // to wait for: the team is the pane's and the toggle has a position, so this
  // confirm is live from the moment the dialog opens rather than `off` with a
  // reason. A dialog whose confirm is never `off` is not a dialog missing the
  // rule; it is one with nothing outstanding.
  const acts = foot("Stand down", "Standing down…", close, "destructive");

  acts.onConfirm(() => {
    void (async () => {
      refusal.replaceChildren();
      acts.working(true);

      try {
        await write(host, "posting.assign", "standing", {
          id: open.team.id,
          version: open.team.version,
          active: false,
          keepMembers,
        });
        done();
      } catch (error) {
        // §9: the overlay stays open, carrying the reason. A stand-down that
        // closed on a failed write would leave a supervisor believing a crew
        // had been taken off the board.
        refusal.append(el("span", undefined,
          error instanceof WriteRefused
            ? error.message
            : UNKNOWN_OUTCOME));
        acts.working(false);

        if (!(error instanceof WriteRefused)) throw error;
      }
    })();
  });

  // A dialog: the person is confirming a stand-down (§9).
  return overlay("dialog", {
    head: [head],
    body: [what(), keep(many, (on) => { keepMembers = on; }), refusal],
    foot: [acts.row],
  }, close);
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
 * @param members how many people the decision is about, already in the
 *   property's digits
 * @param set called with the new position
 * @returns the row
 */
function keep(members: string, set: (on: boolean) => void): HTMLElement {
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
    set(on);
  });

  return fill(row, label, toggle);
}

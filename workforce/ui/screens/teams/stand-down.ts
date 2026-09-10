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

import type { HostApi } from "@hotelos/sdk";

import { foot } from "../../chrome/confirm";
import { el, fill } from "../../chrome/element";
import { write, WriteRefused } from "../../roster";
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
  const scrim = el("div", "scrim");
  const dialog = el("div", "dlg");

  const members = open.members.length;

  // The toggle's position, held here because the write carries it. It starts
  // where the switch starts, and the switch starts where the SERVICE's own
  // default is — `keepMembers` defaults to true on the wire, so the screen and
  // the service agree about what happens if nobody touches anything.
  let keepMembers = true;

  const head = el("div");
  head.append(
    el("div", "ht", `Stand down ${open.team.name}?`),
    el("div", "hsub", `${open.team.departmentName} · ${members} members`));

  const refusal = el("div", "note twarn");

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
            : "That did not go through. Nothing was changed."));
        acts.working(false);

        if (!(error instanceof WriteRefused)) throw error;
      }
    })();
  });

  dialog.append(head, what(),
    keep(members, (on) => { keepMembers = on; }),
    refusal, acts.row);

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
 * @param set called with the new position
 * @returns the row
 */
function keep(members: number, set: (on: boolean) => void): HTMLElement {
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

/**
 * The foot of a sheet or a dialog — cancel, confirm, and what it is waiting for.
 *
 * **Shared because the second dialog needed it, not before.** Form a team grew
 * this shape first; Add a member is the same four states, and a confirm that
 * behaves differently on two screens teaches a person two things — the second
 * of which they meet under pressure. Page 64 §2 says the destructive twin lives
 * in the chrome rather than in a screen, and this is that rule for the whole
 * foot: the states are the platform's, the wording is the dialog's.
 *
 * Four states, and each one is a rule from page 64 rather than a preference:
 *
 * ```text
 * waiting   nothing to send yet   drawn `off`, with the reason beside it (§2)
 * ready     the confirm is live
 * working   the write is in flight
 * refused   the sheet stays open, carrying the reason (§9)
 * ```
 *
 * **`off` with the reason, never live-and-refusing.** §2's rule exists because
 * a primary action that accepts a click and then explains why it could not
 * have worked spends a person's attention twice. And a greyed control with no
 * explanation is one a person reads as broken, which is why the reason is a
 * field of this component rather than something each dialog remembers.
 */

import { el, fill } from "./element";

/** What a dialog's foot can be asked to do. */
export interface Foot {
  /** The row itself. */
  row: HTMLElement;

  /** Run this when the confirm is pressed and there is something to send. */
  onConfirm: (run: () => void) => void;

  /**
   * What the confirm is waiting for, or null when it is ready.
   *
   * @param reason the field still to be answered, in the dialog's own words
   */
  waitingFor: (reason: string | null) => void;

  /** In flight, or not. */
  working: (busy: boolean) => void;
}

/**
 * What kind of confirm the dialog ends in.
 *
 * **Page 64 §2 splits the destructive control in two, and the split is the
 * point**: an inline destructive affordance is an outline, and *the confirm
 * step of the flow is FILLED*. Leaving the confirm as an outline whispers at
 * the exact moment weight is wanted — a person has already decided by then, and
 * the quietest control on the screen should not be the one that does it.
 */
export type Kind = "primary" | "destructive";

/**
 * Build the foot.
 *
 * @param confirmLabel what the confirm says at rest — the dialog's own verb
 * @param workingLabel what it says while the write is in flight
 * @param close called when the person cancels
 * @param kind primary by default; destructive fills it in `--color-bad`
 * @returns the row, and the handles to drive it
 */
export function foot(
  confirmLabel: string,
  workingLabel: string,
  close: () => void,
  kind: Kind = "primary",
): Foot {
  const row = el("div", "acts");
  const reason = el("div", "note");

  const cancel = el("button", "btn", "Cancel");
  cancel.setAttribute("type", "button");
  cancel.addEventListener("click", close);

  const lit = kind === "destructive" ? "danger confirm" : "pri";
  const confirm = el("button", `btn ${lit}`, confirmLabel);
  confirm.setAttribute("type", "button");

  fill(row, reason, el("div", "grow"), cancel, confirm);

  return {
    row,

    onConfirm(run) {
      confirm.addEventListener("click", () => {
        // Guarded here as well as by the attribute. `disabled` stops a click on
        // a real `<button>`, and this is the one place that decides a write
        // happens — so it does not depend on the attribute having been set.
        if (confirm.hasAttribute("disabled")) return;
        run();
      });
    },

    waitingFor(waiting) {
      reason.textContent = waiting ?? "";
      confirm.classList.toggle("off", waiting !== null);

      // Whichever class this foot lit, not `pri` unconditionally: a
      // destructive confirm that fell back to the primary fill while waiting
      // would change colour to say it was unavailable, which is the one moment
      // its colour should not move.
      for (const one of lit.split(" ")) {
        confirm.classList.toggle(one, waiting === null);
      }

      confirm.toggleAttribute("disabled", waiting !== null);
    },

    working(busy) {
      confirm.textContent = busy ? workingLabel : confirmLabel;
      confirm.toggleAttribute("disabled", busy);
    },
  };
}

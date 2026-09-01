/**
 * Request leave — the form, and the warning that does not stop it.
 *
 * # `WF-Q5` rendered at the point of entry
 *
 * The balance is shown **while the request is being made**, negative sign and
 * all, and the request can still be raised. *"Warn, never block"* is not a
 * property of the approval screen alone: a form that refused here would have
 * moved the block one step earlier and called it validation.
 *
 * # Provenance is on the form, not inferred from it
 *
 * `WF-Q9`(b). Most of the workforce has no login, so a supervisor raises most
 * of these — and the form says whose request it is and who is raising it, in a
 * sentence rather than a field somebody has to interpret.
 */

import { el } from "../../chrome/element";
import type { Balance } from "../../roster/leave";

/**
 * Build the form.
 *
 * @param who the person the request is for
 * @param raisedBy the account raising it
 * @param balance the balance for the chosen type
 * @param close called when it is dismissed
 * @returns the overlay
 */
export function requestForm(
  who: string,
  raisedBy: string,
  balance: Balance,
  close: () => void,
): HTMLElement {
  const scrim = el("div", "scrim");
  const dialog = el("div", "dlg");

  const head = el("div");
  head.append(
    el("div", "ht", "Request leave"),
    el("div", "hsub", `Raised by ${raisedBy}`),
  );

  dialog.append(
    head,
    forWhom(who, raisedBy),
    type(balance),
    dates(),
    note(),
    actions(close),
  );

  scrim.append(dialog);
  scrim.addEventListener("click", (event) => {
    if (event.target === scrim) close();
  });

  return scrim;
}

/** Who it is for, and the sentence that keeps the record honest. */
function forWhom(who: string, raisedBy: string): HTMLElement {
  const row = el("div", "fld");
  const picker = el("div", "finput");

  picker.append(el("span", "av", initials(who)), el("span", undefined, who));

  row.append(
    el("div", "flab", "For"),
    picker,
    el("div", "note",
      `Recorded as raised by ${raisedBy} on behalf of ${who}. `
      + "The record never claims they did it themselves."),
  );

  return row;
}

/**
 * The type, and the balance beside it.
 *
 * The warning carries **the number and the permission in one breath**: what the
 * balance is, and that the request can still be made. A warning that only said
 * "insufficient balance" would read as a refusal.
 */
function type(balance: Balance): HTMLElement {
  const row = el("div", "fld");

  row.append(el("div", "flab", "Type"), el("div", "finput", `${balance.type} leave`));

  if (balance.days < 0) {
    const warn = el("div", "warnrow");
    warn.append(
      el("span", undefined, "⚠"),
      el("span", undefined,
        `Balance is ${balance.days} of ${balance.of}. The request can still be made `
        + "and approved — your manager sees the balance on the decision."),
    );
    row.append(warn);
  }

  return row;
}

/** The two dates, and what they add up to. */
function dates(): HTMLElement {
  const row = el("div", "fld");
  const pair = el("div", "spans");

  pair.append(el("div", "finput", "14 Sep 2026"), el("div", "finput", "16 Sep 2026"));

  row.append(
    el("div", "flab", "Dates"),
    pair,
    // The second sentence is the one that matters: the form knows who else is
    // away, which is the fact a manager would otherwise discover after
    // approving.
    el("div", "note", "3 days. 2 of your team are already away on the 15th."),
  );

  return row;
}

function note(): HTMLElement {
  const row = el("div", "fld");

  row.append(
    el("div", "flab", "Note"),
    el("div", "finput", "Brother's wedding — travelling on the 13th."),
  );

  return row;
}

function actions(close: () => void): HTMLElement {
  const row = el("div", "acts");
  const cancel = el("div", "btn", "Cancel");

  cancel.addEventListener("click", close);
  row.append(cancel, el("div", "btn go", "Raise request"));
  return row;
}

/** Initials, derived here so every avatar in this module derives them alike. */
function initials(name: string): string {
  return name.split(" ").map((part) => part[0] ?? "").join("").slice(0, 2);
}

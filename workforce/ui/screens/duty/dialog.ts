/**
 * Assign Manager on Duty — a span, and the sentence that proves it is one.
 *
 * `WF-Q8`. The two ends are datetimes, not a date and a shift, and the form
 * states what they add up to: *"12 hours, crossing midnight. Both dates carry
 * the duty."* A form that took a date and a shift could not express the duty
 * the owner described, and this sentence is how a person checks that it did.
 */

import { el } from "../../chrome/element";

/**
 * Build the dialog.
 *
 * @param close called when it is dismissed
 * @returns the overlay
 */
export function assignDuty(close: () => void): HTMLElement {
  const scrim = el("div", "scrim");
  const dialog = el("div", "dlg");

  const head = el("div");
  head.append(
    el("div", "ht", "Assign Manager on Duty"),
    el("div", "hsub", "Friday 28 August"),
  );

  dialog.append(head, span(), who(), actions(close));

  scrim.append(dialog);
  scrim.addEventListener("click", (event) => {
    if (event.target === scrim) close();
  });

  return scrim;
}

/** The two ends, and what they come to. */
function span(): HTMLElement {
  const row = el("div", "fld");
  // The chrome's field row, two columns. It was Policy's four-column grid for
  // a while and each date got a quarter of the width, so `Fri 28 · 20:00`
  // wrapped at the separator and one instant read as two.
  const pair = el("div", "spans");

  pair.append(
    el("div", "finput", "Fri 28 · 20:00"),
    el("div", "finput", "Sat 29 · 08:00"),
  );

  row.append(
    el("div", "flab", "From"),
    pair,
    // Derived from the two ends and shown back, so a person can see that the
    // form understood what they typed — the alternative is discovering it on
    // the register at 3 a.m.
    el("div", "note", "12 hours, crossing midnight. Both dates carry the duty."),
  );

  return row;
}

/**
 * Who may hold it — anybody posted, from any department.
 *
 * MOD is property-wide, so the list is not filtered to the department whose
 * rota opened it: the owner's own scenario is a front-office person one night
 * and security the next.
 */
function who(): HTMLElement {
  const row = el("div", "fld");
  const list = el("div", "picks");

  for (const [initials, name, role, code] of [
    ["AM", "Anjali Menon", "Receptionist", "FO"],
    ["RN", "Rahul Nair", "Security officer", "SEC"],
    ["VD", "Vishnu Das", "Night auditor", "FO"],
  ] as const) {
    const option = el("div", "pk");
    option.append(
      el("b", "av", initials),
      el("span", undefined, name),
      el("s", undefined, `${role} · ${code}`),
    );
    list.append(option);
  }

  row.append(el("div", "flab", "Who"), list);
  return row;
}

function actions(close: () => void): HTMLElement {
  const row = el("div", "acts");
  const cancel = el("div", "btn", "Cancel");

  cancel.addEventListener("click", close);
  row.append(cancel, el("div", "btn pri", "Assign duty"));
  return row;
}

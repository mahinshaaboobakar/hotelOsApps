/**
 * Assign Manager on Duty — a span, and the sentence that proves it is one.
 *
 * `WF-Q8`. The two ends are datetimes, not a date and a shift, and the form
 * states what they add up to: *"12 hours, crossing midnight. Both dates carry
 * the duty."* A form that took a date and a shift could not express the duty
 * the owner described, and this sentence is how a person checks that it did.
 */

import { formatDay, type PropertyEnvironment } from "@hotelos/sdk";

import { foot } from "../../chrome/confirm";
import { el } from "../../chrome/element";
import type { DutyCandidate } from "../../roster/duty";

/**
 * Build the dialog.
 *
 * @param close called when it is dismissed
 * @returns the overlay
 */
export function assignDuty(
  close: () => void,
  candidates: readonly DutyCandidate[],
  day: string | undefined,
  property: PropertyEnvironment,
): HTMLElement {
  const scrim = el("div", "scrim");
  const dialog = el("div", "dlg");

  let chosen: DutyCandidate | null = null;

  const head = el("div");
  head.append(
    el("div", "ht", "Assign Manager on Duty"),
    // The day the register is showing, not the literal `Friday 28 August`
    // this carried for every property on every week.
    el("div", "hsub", day === undefined ? "" : formatDay(day, property, "day-month-year")),
  );

  // **Never live, and it says why.** §2: a primary action with nothing to send
  // is drawn `off` with the reason beside it. `AssignDutyCommand` takes two
  // datetimes, and this screen has no control that sets them and no default it
  // could honestly supply — `20:00 → 08:00` appears once in the codebase, in a
  // doc comment explaining why two datetimes are needed, and an example given
  // in passing is not a rule. Choosing a span here would promote it to one.
  //
  // So the person can choose WHO, the reason names what is missing, and the
  // span is a frame for the owner rather than a number invented in a module.
  const acts = foot("Assign duty", "Assigning…", close);
  acts.waitingFor("No duty span is set — this screen cannot choose one yet");

  dialog.append(head, span(), who(candidates, (one) => {
    chosen = one;
    acts.waitingFor(chosen === null
      ? "Choose somebody"
      : "No duty span is set — this screen cannot choose one yet");
  }), acts.row);

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

  // **Placeholders, not values** - section 10's `.ph`. These drew
  // `Fri 28 · 20:00` and `Sat 29 · 08:00` as though somebody had chosen
  // them, on a card whose own foot says no span is set: two claims about one
  // thing, on one screen, contradicting each other.
  //
  // Null is *nobody has supplied this*, and that is exactly the state - there
  // is no control here that could.
  pair.append(
    el("div", "inp ph", "Start"),
    el("div", "inp ph", "End"),
  );

  row.append(
    el("div", "fld-label", "From"),
    pair,
    // Derived from the two ends and shown back, so a person can see that the
    // form understood what they typed — the alternative is discovering it on
    // the register at 3 a.m.
    // Describes the FIELD, now that the example span it described has gone.
    // "12 hours" was true of two literals nobody chose; the shape is the
    // rule - WF-Q8, two datetimes rather than a date.
    el("div", "note",
      "Two datetimes, not a date. A duty crossing midnight carries both days."),
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
function who(
  candidates: readonly DutyCandidate[],
  pick: (candidate: DutyCandidate) => void,
): HTMLElement {
  const row = el("div", "fld");
  const list = el("div", "picks");

  for (const candidate of candidates) {
    // A real button: it is a choice, and a choice a keyboard cannot reach is a
    // decision only a mouse can make.
    const option = el("button", "pk");
    option.setAttribute("type", "button");

    option.addEventListener("click", () => {
      for (const other of Array.from(list.querySelectorAll(".pk"))) {
        other.classList.remove("on");
      }
      option.classList.add("on");
      pick(candidate);
    });

    option.append(
      el("b", "av", initials(candidate.name)),
      el("span", undefined, candidate.name ?? ""),
      el("s", undefined, `${candidate.role} · ${candidate.department}`),
    );
    list.append(option);
  }

  row.append(el("div", "fld-label", "Who"), list);
  return row;
}

/** Two letters from a name this module borrowed and does not keep. */
function initials(name: string | null): string {
  if (name === null) return "";

  const parts = name.split(" ").filter((part) => part.length > 0);
  const first = parts[0]?.[0] ?? "";
  const last = parts.length > 1 ? parts[parts.length - 1]?.[0] ?? "" : "";

  return `${first}${last}`.toUpperCase();
}

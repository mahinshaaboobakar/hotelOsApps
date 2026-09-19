/**
 * Assign Manager on Duty — a span, and the sentence that proves it is one.
 *
 * `WF-Q8`. The two ends are datetimes, not a date and a shift, and the form
 * states what they add up to: *"12 hours, crossing midnight. Both dates carry
 * the duty."* A form that took a date and a shift could not express the duty
 * the owner described, and this sentence is how a person checks that it did.
 */

import { formatDay, type HostApi, type PropertyEnvironment } from "@hotelos/sdk";

import { foot } from "../../chrome/confirm";
import { el } from "../../chrome/element";
import { overlay } from "../../chrome/overlay";
import { UNKNOWN_OUTCOME, write, WriteRefused } from "../../roster";
import type { DutyCandidate } from "../../roster/duty";

/**
 * Build the dialog.
 *
 * @param close called when it is dismissed
 * @returns the overlay
 */
export function assignDuty(
  host: HostApi,
  close: () => void,
  candidates: readonly DutyCandidate[],
  day: string | undefined,
  property: PropertyEnvironment,
  done: () => void,
): HTMLElement {
  let chosen: DutyCandidate | null = null;

  const head = el("div");
  head.append(
    el("div", "ht", "Assign Manager on Duty"),
    // The day the register is showing, not the literal `Friday 28 August`
    // this carried for every property on every week.
    el("div", "hsub", day === undefined ? "" : formatDay(day, property, "day-month-year")),
  );

  // **The span is typed, and the confirm waits on it.** The first pass drew
  // placeholders and left this permanently `off`, reporting the span as a
  // frame the owner owed. That was a step too early: the question was never
  // *what does the span default to*, it was that nothing here could be typed
  // into — and section 10 licenses the control the moment a write path accepts
  // what is typed. A control removes the question instead of answering it.
  const draft = { from: "", to: "" };

  const refusal = el("div", "note warn");
  const acts = foot("Assign duty", "Assigning…", close);

  function waiting(): string | null {
    if (chosen === null) return "Choose somebody";
    if (draft.from === "" || draft.to === "") return "Set when the duty starts and ends";

    // Compared as the control writes them - `YYYY-MM-DDTHH:mm`, which sorts
    // lexically in the same order it sorts chronologically.
    if (draft.to <= draft.from) return "A duty has to end after it starts";
    return null;
  }

  acts.onConfirm(() => {
    void (async () => {
      if (chosen === null) return;

      refusal.replaceChildren();
      acts.working(true);

      try {
        await write(host, "duty.assign", "assign", {
          staffId: chosen.staffId,
          from: new Date(draft.from).toISOString(),
          to: new Date(draft.to).toISOString(),
        });
        done();
      } catch (error) {
        refusal.append(el("span", undefined,
          error instanceof WriteRefused
            ? error.message
            : UNKNOWN_OUTCOME));
        acts.working(false);

        if (!(error instanceof WriteRefused)) throw error;
      }
    })();
  });

  acts.waitingFor(waiting());

  // A sheet: the person is composing an assignment (§9).
  return overlay("sheet", {
    head: [head],
    body: [
      span(day, (which, value) => { draft[which] = value; acts.waitingFor(waiting()); }),
      who(candidates, (one) => { chosen = one; acts.waitingFor(waiting()); }),
      refusal,
    ],
    foot: [acts.row],
  }, close);
}

/** The two ends, and what they come to. */
function span(
  day: string | undefined,
  set: (which: "from" | "to", value: string) => void,
): HTMLElement {
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
  // **Two real controls, and section 10 is what licenses them**: *a field is a
  // div until there is a write path behind it that accepts what is typed*, and
  // `AssignDutyCommand` takes two datetimes.
  //
  // They drew `Fri 28 · 20:00` and `Sat 29 · 08:00` as though somebody had
  // chosen them, then placeholders on a card whose foot said no span was set.
  // Neither was a span a person could set, which is what this field is for.
  //
  // `datetime-local`, not a time: a duty may cross midnight - WF-Q8's whole
  // point - so a time alone cannot say which day it lands on.
  pair.append(stamp(day, (value) => { set("from", value); }),
    stamp(day, (value) => { set("to", value); }));

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
 * One end of the span.
 *
 * @param day the day the register is showing, so the picker opens near it
 * @param set called with the value the person chose
 * @returns the control
 */
function stamp(day: string | undefined, set: (value: string) => void): HTMLElement {
  const input = document.createElement("input");
  input.className = "inp";
  input.type = "datetime-local";

  // Where the picker opens, not a value: `min` narrows the control and sends
  // nothing. It opens on the day being looked at rather than the browser's own.
  if (day !== undefined) input.min = `${day}T00:00`;

  input.addEventListener("input", () => { set(input.value); });

  return input;
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

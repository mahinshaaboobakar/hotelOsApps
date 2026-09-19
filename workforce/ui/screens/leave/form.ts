/**
 * Request leave — the form, and the warning that does not stop it.
 *
 * # It raises what is entered, for the person signed in
 *
 * `leave.request · raise` takes a type, a first and a last day and an optional
 * note, and **derives whose leave it is from the caller** — ADR 0172. So there
 * is no "For" field: a request naming somebody else is inexpressible on the
 * wire, and a control offering it would promise what the service refuses by
 * construction. The form says so in a sentence rather than drawing a choice.
 *
 * The owner met this form dead on 0.3.3: every box a drawing and the primary
 * live over nothing. On 2026-09-19 it was first drawn honestly off; it is built
 * now, because everything it needs is served.
 *
 * # `WF-Q5` at the point of entry
 *
 * The chosen type's balance is shown **while the request is being made**,
 * minus sign and all, and the request can still be raised. *Warn, never block*
 * is not a property of the approval screen alone: a form that refused here
 * would have moved the block one step earlier and called it validation.
 */

import { formatNumber, type HostApi } from "@hotelos/sdk";

import { foot } from "../../chrome/confirm";
import { el } from "../../chrome/element";
import { overlay } from "../../chrome/overlay";
import { UNKNOWN_OUTCOME, write, WriteRefused } from "../../roster";
import type { Balance } from "../../roster/leave";

/** What the sheet holds, and what the write carries. */
interface Draft {
  type: string | null;
  from: string;
  to: string;
  note: string;
}

/**
 * Build the form — a sheet, because a person composes a request here (§9).
 *
 * @param host the bridge
 * @param types every leave type, with the reader's balance in each
 * @param close called when it is dismissed
 * @param done called after the request is raised, so the board is re-read
 * @returns the overlay
 */
export function requestForm(
  host: HostApi, types: readonly Balance[], close: () => void, done: () => void,
): HTMLElement {
  const draft: Draft = { type: null, from: "", to: "", note: "" };

  const balance = el("div", "fld");
  const refusal = el("div", "note warn");
  const acts = foot("Raise request", "Raising…", close);

  function redraw(): void {
    balance.replaceChildren();
    const chosen = types.find((one) => one.id === draft.type);
    if (chosen !== undefined) balance.append(held(chosen, host));

    // §2: off, with the field it waits on — never live and refusing. Dates
    // compare as the control writes them, YYYY-MM-DD, which sorts as it reads.
    acts.waitingFor(draft.type === null
      ? "Choose a type of leave"
      : draft.from === "" || draft.to === ""
        ? "Choose the first and last day"
        : draft.to < draft.from
          ? "The last day is before the first"
          : null);
  }

  acts.onConfirm(() => {
    void (async () => {
      refusal.replaceChildren();
      acts.working(true);

      try {
        await write(host, "leave.request", "raise", {
          typeId: draft.type,
          from: draft.from,
          to: draft.to,
          ...(draft.note.trim() === "" ? {} : { note: draft.note.trim() }),
        });
        done();
      } catch (error) {
        // §9: a refusal keeps the sheet open, carrying the reason.
        refusal.append(el("span", undefined,
          error instanceof WriteRefused ? error.message : UNKNOWN_OUTCOME));
        acts.working(false);

        if (!(error instanceof WriteRefused)) throw error;
      }
    })();
  });

  redraw();

  return overlay("sheet", {
    head: [
      el("div", "ht", "Request leave"),
      el("div", "hsub", "For you — a request is raised by the person it is for."),
    ],
    body: [
      type(types, (id) => { draft.type = id; redraw(); }),
      balance,
      dates((which, value) => { draft[which] = value; redraw(); }),
      note((value) => { draft.note = value; }),
      refusal,
    ],
    foot: [acts.row],
  }, close);
}

/** The type — a real choice, and nothing chosen until the person chooses. */
function type(types: readonly Balance[], chosen: (id: string | null) => void): HTMLElement {
  const row = el("div", "fld");
  const picker = document.createElement("select");
  picker.className = "inp ph";
  picker.name = "type";

  const none = document.createElement("option");
  none.value = "";
  none.textContent = "Choose a type";
  picker.append(none);

  for (const one of types) {
    const option = document.createElement("option");
    option.value = one.id;
    option.textContent = one.type;
    picker.append(option);
  }

  picker.addEventListener("change", () => {
    picker.classList.toggle("ph", picker.value === "");
    chosen(picker.value === "" ? null : picker.value);
  });

  row.append(el("div", "fld-label", "Type"), picker);
  return row;
}

/** The chosen type's balance — a warning, never a gate (WF-Q5). */
function held(chosen: Balance, host: HostApi): HTMLElement {
  // The default precision, because a balance can be a half day.
  const days = formatNumber(chosen.days, host.property);
  const text = chosen.of === null
    ? `${days} left`
    : `${days} of ${formatNumber(chosen.of, host.property)} left`;

  return el("div", chosen.days < 0 ? "note warn balance" : "note balance", text);
}

/** The first and last day — the service counts the days between them. */
function dates(set: (which: "from" | "to", value: string) => void): HTMLElement {
  const row = el("div", "fld");
  const pair = el("div", "spans");

  for (const which of ["from", "to"] as const) {
    const input = document.createElement("input");
    input.className = "inp";
    input.type = "date";
    input.name = which;
    input.setAttribute("aria-label", which === "from" ? "First day" : "Last day");
    input.addEventListener("input", () => { set(which, input.value); });
    pair.append(input);
  }

  row.append(el("div", "fld-label", "Dates"), pair);
  return row;
}

/** A note the approver reads — optional. */
function note(set: (value: string) => void): HTMLElement {
  const row = el("div", "fld");
  const input = document.createElement("input");
  input.className = "inp";
  input.type = "text";
  input.name = "note";
  input.setAttribute("maxlength", "500");
  input.addEventListener("input", () => { set(input.value); });

  row.append(el("div", "fld-label", "Note"), input);
  return row;
}

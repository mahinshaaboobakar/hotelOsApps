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
 *
 * # Both of those need a CHOICE, and this form cannot take one yet
 *
 * **It drew a finished request nobody made** (the app surface audit,
 * 2026-09-19, C11 · F1 · C8): two people named as literals in the screen —
 * *"for"* one, *"raised by"* another — the overdrawn balance, chosen *"because
 * that is the one the frame raises the warning against"*, two dates, a note,
 * and *"3 days. 2 of your team are already away on the 15th."* — a fact
 * computed about dates nobody entered. Under it, a live *Raise request*.
 *
 * Every value box is now §10's placeholder, and the two rules above return the
 * day a person and a type can be chosen here: the balance beside the chosen
 * type, and the provenance sentence naming the two people it is true of. **Until
 * then the form states the rules, not a record that does not exist.**
 */

import { control, el } from "../../chrome/element";
import { overlay } from "../../chrome/overlay";

/**
 * What an empty value box shows. §10 draws `.inp.ph` *"when nobody has
 * supplied a value"*; the dash is §11's mark for absent.
 */
const NOTHING = "—";

/**
 * Build the form — a sheet, because a person composes a request here (§9).
 *
 * @param close called when it is dismissed
 * @returns the overlay
 */
export function requestForm(close: () => void): HTMLElement {
  return overlay("sheet", {
    head: [el("div", "ht", "Request leave")],
    body: [forWhom(), type(), dates(), note()],
    foot: [actions(close)],
  }, close);
}

/** Who it is for — and the rule the record keeps, stated rather than filled in. */
function forWhom(): HTMLElement {
  const row = el("div", "fld");

  row.append(
    el("div", "fld-label", "For"),
    el("div", "inp ph", NOTHING),
    el("div", "note",
      "Raised for somebody else, it is recorded as raised on their behalf — "
      + "the record never claims they did it themselves."),
  );

  return row;
}

/**
 * The type. Its balance is shown beside it once one is chosen — `WF-Q5`, warn
 * and never block — and not before: a balance beside no type is a figure about
 * nothing.
 */
function type(): HTMLElement {
  const row = el("div", "fld");
  row.append(el("div", "fld-label", "Type"), el("div", "inp ph", NOTHING));
  return row;
}

/** The two dates. What they add up to, and who else is away, follow the dates. */
function dates(): HTMLElement {
  const row = el("div", "fld");
  const pair = el("div", "spans");

  pair.append(el("div", "inp ph", NOTHING), el("div", "inp ph", NOTHING));
  row.append(el("div", "fld-label", "Dates"), pair);

  return row;
}

function note(): HTMLElement {
  const row = el("div", "fld");
  row.append(el("div", "fld-label", "Note"), el("div", "inp ph", NOTHING));
  return row;
}

function actions(close: () => void): HTMLElement {
  const row = el("div", "acts");
  const cancel = control("btn", "Cancel", close);

  // **Off, and says why** — §2: *"A primary action with nothing to send is
  // drawn `off`, with the reason beside it — never live-and-refusing."*
  // `leave.request · raise` is served and needs no staff id at all — ADR 0172
  // derives the requester from the scope, so *whose leave* is not a field and
  // cannot be one. What this sheet has no way to supply is the person's own
  // answers: every value box above is a rendered `div`, not a control, so there
  // is nothing to read a date or a note out of. Building those is a surface,
  // not a wiring. It was a live `div.btn.pri` over nothing until 2026-09-19.
  const raise = control("btn pri off", "Raise request");
  raise.setAttribute("disabled", "true");

  row.append(
    cancel,
    raise,
    el("span", "why", "Dates and a note cannot be entered here yet, so there is nothing to send."),
  );
  return row;
}

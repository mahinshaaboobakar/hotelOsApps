/**
 * Form a team — two fields, and the rule that belongs to each.
 *
 * Both refusals drawn here are the service's, not the screen's invention: the
 * department must be one the property has **activated**, and two live teams in
 * one department may not share a name. Drawing them beside the field they
 * govern is the only way a person meets the rule before the save rather than
 * after it.
 */

import { el, fill } from "../../chrome/element";

/**
 * Build the sheet.
 *
 * @param close called when it is dismissed
 * @returns the overlay
 */
export function formTeam(close: () => void): HTMLElement {
  // A sheet rather than a centred dialog: the name is being checked against
  // the list behind it, so the form holds the edge and leaves the list visible.
  const scrim = el("div", "scrim edge");
  const sheet = el("div", "dlg edge");

  const head = el("div");
  head.append(
    el("div", "ht", "Form a team"),
    el("div", "hsub", "A named group of people in one department, to assign work to"));

  sheet.append(head, department(), name(), actions(close));

  scrim.append(sheet);
  scrim.addEventListener("click", (event) => {
    if (event.target === scrim) close();
  });

  return scrim;
}

/** The department — one, and unchangeable afterwards. */
function department(): HTMLElement {
  const field = el("div", "fld");
  const picker = el("div", "finput");

  picker.append(el("span", undefined, "Housekeeping"));

  return fill(field,
    el("div", "flab", "Department"),
    picker,
    el("div", "note",
      "One department, and it cannot be changed afterwards. Moving a team "
      + "elsewhere would move every member with it — and a member holds a "
      + "posting in this department, so that is two decisions rather than one "
      + "field."));
}

/** The name — the property's own word, and the duplicate it collides with. */
function name(): HTMLElement {
  const field = el("div", "fld");
  const input = el("div", "finput", "Morning Crew");

  const rule = el("div", "note");
  rule.append(
    el("span", undefined, "The property's own word — "),
    el("b", undefined, "“Team A”, “Morning Crew”, “Tower Block”"),
    el("span", undefined,
      ". No code and no list to choose from: a department is the industry's "
      + "vocabulary, a team is this hotel's."));

  // Against the department chosen above, never against the property: Front
  // Office may have its own Morning Crew, and refusing that would be a rule
  // nobody made.
  const taken = el("div", "note twarn");
  taken.append(
    el("span", undefined, "Housekeeping already has a team called "),
    el("b", undefined, "Morning Crew"),
    el("span", undefined,
      ". Two with one name is a supervisor choosing at random."));

  return fill(field, el("div", "flab", "Name"), input, rule, taken);
}

function actions(close: () => void): HTMLElement {
  const row = el("div", "acts");

  const cancel = el("button", "btn", "Cancel");
  cancel.setAttribute("type", "button");
  cancel.addEventListener("click", close);

  const confirm = el("button", "btn go", "Form team");
  confirm.setAttribute("type", "button");

  return fill(row, el("div", "grow"), cancel, confirm);
}

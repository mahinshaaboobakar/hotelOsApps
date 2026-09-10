/**
 * Form a team — two fields, the rule that belongs to each, and the write.
 *
 * Both refusals drawn here are the service's, not the screen's invention: the
 * department must be one the property has **activated**, and two live teams in
 * one department may not share a name. Drawing them beside the field they
 * govern is the only way a person meets the rule before the save rather than
 * after it.
 *
 * # The fields accept typing because there is somewhere for it to go
 *
 * Page 64 §10: *a field is a `<div>`, not an `<input>`, until there is a write
 * path behind it that accepts what is typed.* Until now there was none — the
 * confirm had no listener at all, and both boxes carried the drawing's own
 * words, `Housekeeping` and `Morning Crew`, as literals. A wired confirm over
 * those would have posted the drawing to the property.
 *
 * The write path is `posting.assign/form`, mapped at the door and exercised by
 * the service suite, so the condition §10 sets is met and the boxes become
 * real controls.
 *
 * # The name warning is computed, not drawn
 *
 * This sheet used to state, unconditionally, that *Housekeeping already has a
 * team called Morning Crew* — true of the frame it was drawn from and of
 * nothing else. It is now read from the property's own teams, filtered to the
 * department actually chosen: Front Office may have its own Morning Crew, and
 * warning about that would be a rule nobody made.
 */

import type { HostApi } from "@hotelos/sdk";

import { el, fill } from "../../chrome/element";
import { write, WriteRefused } from "../../roster";
import type { Department, Team } from "../../roster/team";

/** What the sheet is holding, and what the write will carry. */
interface Draft {
  department: string | null;
  name: string;
}

/**
 * Build the sheet.
 *
 * @param host the bridge
 * @param departments every department a team may be formed in
 * @param teams the property's teams, for the name already taken
 * @param close called when it is dismissed
 * @param done called after the team is formed, so the list is re-read
 * @returns the overlay
 */
export function formTeam(
  host: HostApi,
  departments: readonly Department[],
  teams: readonly Team[],
  close: () => void,
  done: () => void,
): HTMLElement {
  // A sheet rather than a centred dialog: the name is being checked against
  // the list behind it, so the form holds the edge and leaves the list visible.
  const scrim = el("div", "scrim edge");
  const sheet = el("div", "dlg sheet");

  const draft: Draft = { department: null, name: "" };

  const head = el("div");
  head.append(
    el("div", "ht", "Form a team"),
    el("div", "hsub", "A named group of people in one department, to assign work to"));

  const taken = el("div", "note twarn");
  const refusal = el("div", "note twarn");
  const acts = actions(close);

  function redraw(): void {
    const clash = teams.find((team) =>
      team.department === draft.department &&
      team.name.trim().toLowerCase() === draft.name.trim().toLowerCase());

    taken.replaceChildren();
    if (clash !== undefined) {
      taken.append(
        el("span", undefined,
          `${departmentName(departments, clash.department)} already has a team called `),
        el("b", undefined, clash.name),
        el("span", undefined,
          ". Two with one name is a supervisor choosing at random."));
    }

    // §2: a primary action with nothing to send is drawn `off`, with the
    // reason beside it — never live and refusing. The reason names the field
    // it is waiting on, because a greyed-out confirm with no explanation is a
    // control a person reads as broken.
    acts.setReason(draft.department === null
      ? "Choose a department"
      : draft.name.trim() === ""
        ? "Name the team"
        : null);
  }

  async function submit(): Promise<void> {
    refusal.replaceChildren();
    acts.working(true);

    try {
      await write(host, "posting.assign", "form",
        { department: draft.department, name: draft.name.trim() });
      done();
    } catch (error) {
      // §9: a refusal keeps the overlay open, carrying the reason. Closing on
      // failure leaves a person believing the team was formed.
      refusal.append(el("span", undefined,
        error instanceof WriteRefused
          ? error.message
          : "That did not go through. Nothing was changed."));
      acts.working(false);

      if (!(error instanceof WriteRefused)) throw error;
    }
  }

  acts.onConfirm(() => { void submit(); });

  sheet.append(
    head,
    department(forReader(departments, host.property.locale),
      (code) => { draft.department = code; redraw(); }),
    name(taken, (value) => { draft.name = value; redraw(); }),
    refusal,
    acts.row);

  redraw();

  scrim.append(sheet);
  scrim.addEventListener("click", (event) => {
    // The scrim dismisses; the surface does not. A click inside a sheet is a
    // person working in it, and closing on that throws away what they entered.
    if (event.target === scrim) close();
  });

  return scrim;
}

/**
 * The departments in the order the person reading them expects.
 *
 * **Ordered here, not by the service, because the locale is here.** The read
 * answers in code order — the same on every machine — and the code is not what
 * anybody reads. The first version sorted by name in the service with
 * `CurrentCultureIgnoreCase`, which looks like the right intent and is not:
 * nothing sets a culture in that process, so the order of a hotel's
 * departments would have been a property of the account the service runs
 * under. This platform is sold into India and the GCC and writes no country
 * into code.
 *
 * `locale` is null when the property has not set one, and the SDK renders that
 * state honestly rather than inventing a locale. `Intl.Collator(undefined)`
 * is the runtime's own default, which is the honest answer to *nobody said*:
 * the alternative is picking a locale on the property's behalf.
 *
 * @param departments as the read answered them
 * @param locale the property's, or null when it has none
 * @returns the same departments, ordered for a reader
 */
function forReader(
  departments: readonly Department[], locale: string | null,
): readonly Department[] {
  const collator = new Intl.Collator(locale ?? undefined, { sensitivity: "base" });
  return [...departments].sort((a, b) => collator.compare(a.name, b.name));
}

/** What a department is called, for a code a row carries. */
function departmentName(departments: readonly Department[], code: string): string {
  return departments.find((one) => one.code === code)?.name ?? code;
}

/** The department — one, and unchangeable afterwards. */
function department(
  departments: readonly Department[],
  chosen: (code: string | null) => void,
): HTMLElement {
  const field = el("div", "fld");
  const picker = document.createElement("select");
  picker.className = "inp ph";

  // The unchosen state is a real option rather than a pre-selected first
  // department: §10's `null` is *nobody has supplied this*, and a picker that
  // arrives already reading "Front Office" has supplied it on the person's
  // behalf.
  const none = document.createElement("option");
  none.value = "";
  none.textContent = "Choose a department";
  picker.append(none);

  for (const one of departments) {
    const option = document.createElement("option");
    option.value = one.code;
    option.textContent = one.name;
    picker.append(option);
  }

  picker.addEventListener("change", () => {
    picker.classList.toggle("ph", picker.value === "");
    chosen(picker.value === "" ? null : picker.value);
  });

  return fill(field,
    el("div", "fld-label", "Department"),
    picker,
    el("div", "note",
      "One department, and it cannot be changed afterwards. Moving a team "
      + "elsewhere would move every member with it — and a member holds a "
      + "posting in this department, so that is two decisions rather than one "
      + "field."));
}

/** The team's name — the property's own word. */
function name(taken: HTMLElement, typed: (value: string) => void): HTMLElement {
  const field = el("div", "fld");
  const input = document.createElement("input");
  input.className = "inp";
  input.type = "text";
  input.placeholder = "Morning Crew";
  input.setAttribute("maxlength", "80");

  input.addEventListener("input", () => { typed(input.value); });

  const rule = el("div", "note");
  rule.append(
    el("span", undefined, "The property's own word — "),
    el("b", undefined, "“Team A”, “Morning Crew”, “Tower Block”"),
    el("span", undefined,
      ". No code and no list to choose from: a department is the industry's "
      + "vocabulary, a team is this hotel's."));

  return fill(field, el("div", "fld-label", "Name"), input, rule, taken);
}

/** The foot: cancel, confirm, and what the confirm is waiting for. */
interface Actions {
  row: HTMLElement;
  onConfirm: (run: () => void) => void;
  setReason: (reason: string | null) => void;
  working: (busy: boolean) => void;
}

function actions(close: () => void): Actions {
  const row = el("div", "acts");
  const reason = el("div", "note");

  const cancel = el("button", "btn", "Cancel");
  cancel.setAttribute("type", "button");
  cancel.addEventListener("click", close);

  const confirm = el("button", "btn pri", "Form team");
  confirm.setAttribute("type", "button");

  fill(row, reason, el("div", "grow"), cancel, confirm);

  return {
    row,

    onConfirm(run) {
      confirm.addEventListener("click", () => {
        // Guarded here as well as by the attribute: `disabled` stops a click on
        // a `<button>`, and this function is the one place that decides the
        // write happens, so it does not depend on the attribute being right.
        if (confirm.hasAttribute("disabled")) return;
        run();
      });
    },

    setReason(waiting) {
      reason.textContent = waiting ?? "";
      confirm.classList.toggle("off", waiting !== null);
      confirm.classList.toggle("pri", waiting === null);
      confirm.toggleAttribute("disabled", waiting !== null);
    },

    working(busy) {
      confirm.textContent = busy ? "Forming…" : "Form team";
      confirm.toggleAttribute("disabled", busy);
    },
  };
}

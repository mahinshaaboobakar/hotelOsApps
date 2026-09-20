/**
 * Workforce policy — the four things a property configures, and the one it does not.
 *
 * # Four rulings landed on one screen
 *
 * The shift catalogue is the property's own and free-form (`WF-Q11`); editing
 * times is effective-forward from a chosen date (`WF-Q15`); leave is a **rate**,
 * not an allowance, and comp-off has no accrual row because HR grants it
 * (`WF-Q13`); overtime is one threshold that warns at planning time (`WF-Q14`).
 *
 * And the fifth thing is here precisely because it is **not** configurable:
 * the holiday calendar is Core Administration's (`WF-Q16`), read here and owned
 * elsewhere.
 */

import { formatNumber, type HostApi, load } from "@hotelos/sdk";

import { span } from "../../chrome/clock";
import { control, el } from "../../chrome/element";
import { UNKNOWN_OUTCOME, write, WriteRefused } from "../../roster";
import { ROSTER_READ } from "../../chrome/permissions";
import { codeChip, colourDot } from "../../chrome/code";
import { failureScreen } from "../../chrome/failure";
import { assignments } from "./catalogue";
import { newShift } from "./dialog";
import { type CatalogueRow, type LeaveRow, type Policy } from "../../roster/policy";

/** One table cell holding an element rather than text. */
function cell(child: HTMLElement, className?: string): HTMLElement {
  const box = el("div", className);
  box.append(child);
  return box;
}


/** Draw the screen. */
export async function policy(
  host: HostApi,
  main: HTMLElement,
  dialog = false,
  close: () => void = () => {},
  open: () => void = () => {},
): Promise<void> {
  const got = await load<Policy>(host, ROSTER_READ, "policy");

  // No fallback - `APPS-Q26(4)`. A failed read renders the failure,
  // never a recorded list with an apology under it.
  if (!got.ok) {
    failureScreen(main, "Policy", got.failure, { the: "this property's policy" }, host.property,
      () => void policy(host, main, dialog, close, open));
    return;
  }

  const config = got.value;

  // The thresholds are the one thing on this screen that saves, and Save is in
  // the header — so the section and the button are built together and share one
  // draft. A save re-reads, so the screen draws what is stored, not what was typed.
  const thresholds = overtime(config, host,
    () => void policy(host, main, dialog, close, open));

  const body = el("div", "body");
  body.append(shifts(config.catalogue, host), leave(config.leave, host), thresholds.section,
    holidays(config));

  main.replaceChildren(header(config, open, thresholds.save), body);

  // Drawn over the screen it belongs to, not on a page of its own: a shift is
  // created from the catalogue it joins, and the list behind it is the context
  // that makes the short code's uniqueness visible.
  if (dialog) main.append(newShift(host, close, close));
}

function header(config: Policy, open: () => void, save: HTMLElement): HTMLElement {
  const head = el("div", "tools");
  const title = el("div");

  title.append(el("div", "hsub", config.property));

  const grow = el("div", "grow");
  const add = control("btn", "＋ New shift", open);

  // **Save saves the overtime thresholds.** It was drawn off because "every
  // row on this screen is read-only: the overtime threshold renders as a
  // div.field" — and the owner met it dead on 0.3.3. The thresholds are inputs
  // now and `setOvertime` takes them. The leave and shift tables stay listings:
  // their writes need an id and a version the Policy read does not send.
  head.append(title, grow, add, save);
  return head;
}

/** The catalogue, and the sentence that makes editing it safe. */
function shifts(rows: readonly CatalogueRow[], host: HostApi): HTMLElement {
  const section = el("div", "sect");
  const columns = "1.4fr 100px 140px 1fr 140px";

  section.append(el("div", "stitle", "Shifts — the property's own catalogue"));

  const list = el("div", "rows");
  const head = el("div", "row hd");
  head.style.gridTemplateColumns = columns;
  for (const label of ["Shift", "Short code", "Times", "Colour & kind", "In use"]) {
    head.append(el("div", undefined, label));
  }
  list.append(head);

  for (const row of rows) {
    const item = el("div", "row");
    item.style.gridTemplateColumns = columns;
    item.append(
      el("b", undefined, row.name),
      // The tone is the service's (row.tone, ledger D3): a local mapping drew
      // Rose neutral here and red on the rota.
      cell(codeChip(row.code, row.tone)),
      // Composed here: the separator between the two windows of a split shift
      // is the reader's too, and the service used to send ", " between them.
      el("div", "quiet", [span(row.hours, host.property), span(row.second, host.property)]
        .filter((one) => one !== null).join(", ") || "—"),
      cell(colourDot(`${row.colour} · ${row.kind}`, row.tone), "quiet"),
      // Why retiring a shift is not deleting it: these assignments still name it,
      // and a rota worked under it has to stay readable.
      //
      // The noun and the figure are both written here. It arrived as
      // `"88 assignments"` from the service, so the word was English on every
      // property's screen and the number carried the service's own grouping.
      el("div", "quiet", assignments(row.inUse, host.property)),
    );
    list.append(item);
  }

  const note = el("div", "note");
  note.append(el("span", undefined,
    "Editing a shift's times takes effect forward from a date you choose — rotas "
    + "already worked keep the times they were worked under."));

  section.append(list, note);
  return section;
}

/** Leave types — a rate, never an annual allowance. */
function leave(rows: readonly LeaveRow[], host: HostApi): HTMLElement {
  const section = el("div", "sect");
  const columns = "1fr 150px 100px 1.6fr";

  section.append(el("div", "stitle", "Leave"));

  const list = el("div", "rows");
  const head = el("div", "row hd");
  head.style.gridTemplateColumns = columns;
  for (const label of ["Type", "Accrues", "Per year", "Notes"]) {
    head.append(el("div", undefined, label));
  }
  list.append(head);

  for (const row of rows) {
    const item = el("div", "row");
    item.style.gridTemplateColumns = columns;
    item.append(
      el("b", undefined, row.type),
      // Composed here, because the unit and the word for the period are the
      // reader's. The service sends the rate and says nothing about how it
      // reads - ADR 0174, and ADR 0175's rule that the wire carries the value
      // while the reader-facing layer composes the presentation.
      el("div", undefined, row.accruesPerMonth === null
        ? "Granted by HR"
        : `${formatNumber(row.accruesPerMonth, host.property, "at-most-2")} / month`),
      el("div", "quiet", row.perYear === null
        ? "—"
        : formatNumber(row.perYear, host.property, "at-most-2")),
      el("div", "quiet", row.note),
    );
    list.append(item);
  }

  const note = el("div", "note");
  // What the administrator needs to know, not where the list came from:
  // "seeded from a template chosen for this property" was provenance, the
  // developer's (owner ruling, 2026-09-19).
  note.append(el("span", undefined,
    "A balance may be overdrawn — the manager sees it and decides."));

  section.append(list, note);
  return section;
}

/**
 * The overtime thresholds, and the Save that writes them.
 *
 * `setOvertime` writes both every time and clears an omitted one, so the form
 * sends what its two boxes hold and an empty box is "no threshold" — said on the
 * screen. **Absent is not zero**: an unset threshold opens as an empty box,
 * never "0", which would read as "warn on every hour".
 *
 * @param config the policy as read
 * @param host the bridge
 * @param saved called after the write lands, so the screen re-reads
 * @returns the section, and the Save for the header
 */
function overtime(
  config: Policy, host: HostApi, saved: () => void,
): { section: HTMLElement; save: HTMLElement } {
  const section = el("div", "sect");
  section.append(el("div", "stitle", "Overtime"));

  const start = { daily: text(config.overtimeDaily), weekly: text(config.overtimeWeekly) };
  const draft = { ...start };

  const why = el("span", "why");
  const button = control("btn pri", "Save changes", () => { void send(); });
  const save = el("span", "unavail");
  save.append(why, button);

  const refusal = el("div", "note warn");

  function redraw(): void {
    const waiting = waitingFor(draft, start);
    why.textContent = waiting ?? "";
    button.classList.toggle("off", waiting !== null);
    button.toggleAttribute("disabled", waiting !== null);
  }

  async function send(): Promise<void> {
    if (waitingFor(draft, start) !== null) return;

    refusal.replaceChildren();
    button.toggleAttribute("disabled", true);

    try {
      await write(host, "roster.configure", "setOvertime", {
        ...(draft.daily.trim() === "" ? {} : { daily: Number(draft.daily) }),
        ...(draft.weekly.trim() === "" ? {} : { weekly: Number(draft.weekly) }),
      });
      saved();
    } catch (error) {
      refusal.append(el("span", undefined,
        error instanceof WriteRefused ? error.message : UNKNOWN_OUTCOME));
      redraw();

      if (!(error instanceof WriteRefused)) throw error;
    }
  }

  const row = el("div", "otrow");
  row.append(
    el("span", "quiet", "Overtime begins after"),
    hours("daily", draft.daily, "hours a day", (value) => { draft.daily = value; redraw(); }),
    hours("weekly", draft.weekly, "hours a week", (value) => { draft.weekly = value; redraw(); }),
    el("span", "quiet", "Leave a box empty for no threshold. Warns while the rota is being "
      + "built — never blocks."),
  );

  section.append(row, refusal);
  redraw();
  return { section, save };
}

/** A threshold as the box shows it — empty where none is set, never "0". */
function text(hours: number | null): string {
  return hours === null ? "" : String(hours);
}

/** What Save waits for — the reasons the service would otherwise refuse with. */
function waitingFor(
  draft: { daily: string; weekly: string }, start: { daily: string; weekly: string },
): string | null {
  if (draft.daily === start.daily && draft.weekly === start.weekly) {
    return "Change a threshold to save it";
  }

  for (const [value, cap, over] of [
    [draft.daily, 24, "A day has 24 hours"],
    [draft.weekly, 168, "A week has 168 hours"],
  ] as const) {
    if (value.trim() === "") continue;
    const hours = Number(value);
    if (!Number.isFinite(hours) || hours <= 0) return "A threshold is a number of hours above zero";
    if (hours > cap) return over;
  }

  return null;
}

/** One threshold's box, and the unit it is counted in. */
function hours(
  name: string, value: string, unit: string, set: (value: string) => void,
): HTMLElement {
  const field = el("label", "field");
  const input = document.createElement("input");
  input.className = "inp";
  input.type = "number";
  input.name = name;
  input.min = "0";
  input.step = "0.5";
  input.value = value;
  input.addEventListener("input", () => { set(input.value); });

  field.append(input, el("span", "quiet", ` ${unit}`));
  return field;
}

/** The one thing this screen shows and cannot change. */
function holidays(config: Policy): HTMLElement {
  const section = el("div", "sect");
  const title = el("div", "stitle");

  // No provenance pill and no "Workforce plans around them": which
  // application owns the calendar is the developer's note, not the reader's
  // (owner ruling, 2026-09-19).
  title.append(el("span", undefined, "Holidays"));

  const note = el("div", "note");
  note.append(
    el("span", undefined, config.holidays ?? "No holiday calendar is configured."),
  );

  section.append(title, note);
  return section;
}

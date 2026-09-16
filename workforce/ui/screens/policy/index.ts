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

import { el } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { codeChip, colourDot } from "../../chrome/code";
import { failureScreen } from "../../chrome/failure";
import { newShift } from "./dialog";
import { type CatalogueRow, type LeaveRow, type Policy } from "../../roster/policy";

/** One table cell holding an element rather than text. */
function cell(child: HTMLElement, className?: string): HTMLElement {
  const box = el("div", className);
  box.append(child);
  return box;
}

/** The published tone a property's colour name maps onto. */
function swatch(colour: string): string {
  if (colour === "Cyan" || colour === "Indigo" || colour === "Violet") return "brand";
  if (colour === "Emerald") return "ok";
  if (colour === "Amber") return "warn";
  return "neutral";
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
    failureScreen(main, "Policy", got.failure, { the: "this property's policy" },
      () => void policy(host, main, dialog, close, open));
    return;
  }

  const config = got.value;

  const body = el("div", "body");
  body.append(shifts(config.catalogue), leave(config.leave, host), overtime(config, host), holidays(config));

  main.replaceChildren(header(config, open), body);

  // Drawn over the screen it belongs to, not on a page of its own: a shift is
  // created from the catalogue it joins, and the list behind it is the context
  // that makes the short code's uniqueness visible.
  if (dialog) main.append(newShift(close));
}

function header(config: Policy, open: () => void): HTMLElement {
  const head = el("div", "tools");
  const title = el("div");

  title.append(el("div", "hsub", config.property));

  const grow = el("div", "grow");
  const add = el("div", "btn", "＋ New shift");
  add.addEventListener("click", open);

  // **Inert, and this one has nothing to save.** `setOvertime` and
  // `setLeaveType` are both served, and every row on this screen is read-only:
  // the overtime threshold renders as a `div.field`, and the shift and leave
  // tables are listings. A Save with no editable field in front of it is a
  // button that could only ever re-send what is already stored.
  head.append(title, grow, add, el("div", "btn pri", "Save changes"));
  return head;
}

/** The catalogue, and the sentence that makes editing it safe. */
function shifts(rows: readonly CatalogueRow[]): HTMLElement {
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
      cell(codeChip(row.code, swatch(row.colour))),
      el("div", "quiet", row.times),
      cell(colourDot(`${row.colour} · ${row.kind}`, swatch(row.colour)), "quiet"),
      // Why retiring a shift is not deleting it: these assignments still name it,
      // and a rota worked under it has to stay readable.
      el("div", "quiet", row.inUse),
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
      // reads - ADR 0174, and ADR 0152's argument applied to composition.
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
  note.append(el("span", undefined,
    "Seeded from a template chosen for this property, then edited here. A balance "
    + "may be overdrawn — the manager sees it and decides."));

  section.append(list, note);
  return section;
}

/** One threshold, warning at planning time. */
function overtime(config: Policy, host: HostApi): HTMLElement {
  const section = el("div", "sect");

  section.append(el("div", "stitle", "Overtime"));

  const row = el("div", "otrow");
  row.append(
    el("span", "quiet", "Overtime begins after"),
    el("div", "field", threshold(config.overtimeDaily, "day", host)),
    el("div", "field", threshold(config.overtimeWeekly, "week", host)),
    el("span", "quiet", "Warns while the rota is being built. Never blocks."),
  );

  section.append(row);
  return section;
}

/** The one thing this screen shows and cannot change. */
function holidays(config: Policy): HTMLElement {
  const section = el("div", "sect");
  const title = el("div", "stitle");

  title.append(
    el("span", undefined, "Holidays"),
    el("span", "pill neu", "read-only · Core Administration"),
  );

  const note = el("div", "note");
  note.append(
    el("span", undefined, config.holidays ?? "No holiday calendar is configured."),
    el("b", undefined,
      " The administrator sets these for the property; Workforce plans around them."),
  );

  section.append(title, note);
  return section;
}

/**
 * An overtime threshold, or the absence of one.
 *
 * @param hours the threshold, null where the property set none
 * @param per the period it is measured over
 * @param host for the property's locale
 * @returns the sentence a person reads
 *
 * @remarks
 * This was `Hours(decimal?, string)` in the service, building `"9 h / day"` —
 * so the unit, the separator and the English word for the period were all
 * decided one component away from the reader, in one language, and an em-dash
 * stood in for absence with no way for a surface to say it differently.
 */
function threshold(hours: number | null, per: string, host: HostApi): string {
  return hours === null
    ? "—"
    : `${formatNumber(hours, host.property, "at-most-2")} h / ${per}`;
}

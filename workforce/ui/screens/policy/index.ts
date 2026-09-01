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

import type { HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { standIn } from "../../chrome/standin";
import { load } from "../../roster";
import { recordedPolicy, type CatalogueRow, type LeaveRow, type Policy } from "../../roster/policy";

/** Draw the screen. */
export async function policy(host: HostApi, main: HTMLElement): Promise<void> {
  const got = await load(host, ROSTER_READ, "policy", recordedPolicy);
  const config = got.value;

  const body = el("div", "body");
  body.append(shifts(config.catalogue), leave(config.leave), overtime(config), holidays(config));

  if (!got.live) {
    body.append(standIn("policy", got.because));
  }

  main.replaceChildren(header(config), body);
}

function header(config: Policy): HTMLElement {
  const head = el("div", "head");
  const title = el("div");

  title.append(el("div", "ht", "Workforce policy"), el("div", "hsub", config.property));

  const grow = el("div", "grow");
  head.append(title, grow, el("div", "btn go", "Save changes"));
  return head;
}

/** The catalogue, and the sentence that makes editing it safe. */
function shifts(rows: readonly CatalogueRow[]): HTMLElement {
  const section = el("div", "sect");
  const columns = "1.4fr 110px 150px 1fr";

  section.append(el("div", "stitle", "Shifts — the property's own catalogue"));

  const list = el("div", "rows");
  const head = el("div", "row hd");
  head.style.gridTemplateColumns = columns;
  for (const label of ["Shift", "Short code", "Times", "Colour & kind"]) {
    head.append(el("div", undefined, label));
  }
  list.append(head);

  for (const row of rows) {
    const item = el("div", "row");
    item.style.gridTemplateColumns = columns;
    item.append(
      el("b", undefined, row.name),
      el("div", "code", row.code),
      el("div", "dim", row.times),
      el("div", "dim", `${row.colour} · ${row.kind}`),
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
function leave(rows: readonly LeaveRow[]): HTMLElement {
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
      el("div", undefined, row.accrues),
      el("div", "dim", row.perYear),
      el("div", "dim", row.note),
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
function overtime(config: Policy): HTMLElement {
  const section = el("div", "sect");

  section.append(el("div", "stitle", "Overtime"));

  const row = el("div", "otrow");
  row.append(
    el("span", "dim", "Overtime begins after"),
    el("div", "field", config.overtimeDaily),
    el("div", "field", config.overtimeWeekly),
    el("span", "dim", "Warns while the rota is being built. Never blocks."),
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

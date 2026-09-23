/**
 * 4c · the sheet — the wall with editable cells, source · when on every row,
 * a changed row tinted, a conflicting row red, a "show" filter, and "set for
 * all" on a selection in the dock at the foot.
 */

import type { HostApi } from "@hotelos/sdk";

import { chip } from "../../chrome/bar";
import { control, el, option } from "../../chrome/element";
import { clock } from "../../chrome/instant";
import type { Nav } from "../../chrome/nav";
import { whole } from "../../chrome/number";
import { source } from "../../chrome/words";
import type { RoomStates, StateRow } from "../../model";
import type { Edits } from "./edits";

export const CONDITIONS = ["DIRTY", "CLEAN", "INSPECTED"];
export const OCCUPANCIES = ["VACANT", "OCCUPIED"];
export const STAYS: readonly (readonly [string, string])[] = [["DEPARTED", "departed"], ["ARRIVED", "arrived"], ["IN_HOUSE", "in house"], ["NONE", "none"]];

/**
 * Which rooms the sheet shows, and the selection under it. Both start fresh every time a person opens Room states.
 *
 * **A filter that remembers is a filter that hides what arrived** — ADR 0229 · 07, the owner, 2026-09-23, ruling on
 * what 01d drew: next morning, four rooms had come in overnight and a remembered "Dirty" kept them off the list,
 * under a count ("13 of 21") that is easy to read past at seven in the morning.
 */
let show = "All";
const selected = new Set<string>();

/**
 * Forget the sheet's selection — called each time Room states is entered, so leaving (another section, a room)
 * drops it. "Apply to selected" is a bulk change; a selection that outlived the screen would act on rows the person
 * is no longer looking at (architect, 2026-09-19). The unsaved edits already go the same way: `states()` makes new
 * ones on every entry. `show` stays remembered: whether a filter is kept is the owner's choice, queued, not made here.
 */
export function forgetSelection(): void {
  selected.clear();
  show = "All";
}

export function sheetView(host: HostApi, data: RoomStates, edits: Edits, conflicts: ReadonlySet<string>, redraw: () => void, nav: Nav): HTMLElement[] {
  const shows: readonly (readonly [string, (r: StateRow) => boolean])[] = [
    ["All", () => true], ["Dirty", (r) => edits.value(r, "condition") === "DIRTY"], ["Occupied", (r) => edits.value(r, "occupancy") === "OCCUPIED"],
    ["Vacant", (r) => edits.value(r, "occupancy") === "VACANT"], ["Sold tonight", (r) => r.soldAt !== null],
    [`Changed · ${whole(host, edits.size)}`, (r) => edits.has(r.roomId)], [`Conflicts · ${whole(host, conflicts.size)}`, (r) => conflicts.has(r.roomId)],
  ];
  const filters = el("div", "chips");
  // Zone is the only grouping: said, not offered as a chip that does nothing when pressed (tests/live.test.ts).
  filters.append(el("span", "lbl", "grouped by zone"), el("span", "lbl", "show"));
  // INTERIM, the safe behaviour until the owner chooses: changing the filter forgets the selection — the same one
  // lifetime rule as leaving the sheet (`forgetSelection`). The invariant is not a choice: Apply never changes a row
  // the person can't see (tests/live.test.ts). The alternative drawn for the owner keeps the selection and has Apply
  // act only on the rows shown.
  for (const [label] of shows) filters.append(chip(label, show.split(" ·")[0] === label.split(" ·")[0], () => { show = label; selected.clear(); redraw(); }));
  const keep = shows.find(([label]) => label.split(" ·")[0] === show.split(" ·")[0])?.[1] ?? (() => true);

  const house = el("div", "house");
  const table = el("table", "wall states");
  const head = el("tr");
  const all = el("input") as HTMLInputElement;
  all.type = "checkbox";
  all.setAttribute("aria-label", "select every room shown");
  all.addEventListener("change", () => {
    for (const r of data.zones.flatMap((z) => z.rooms).filter(keep)) (all.checked ? selected.add(r.roomId) : selected.delete(r.roomId));
    redraw();
  });
  const first = el("th");
  first.append(all);
  head.append(first);
  for (const name of ["Room", "Condition", "Occupancy", "Sold tonight", "Stay", "Source · when", ""]) head.append(el("th", undefined, name));
  const thead = el("thead");
  thead.append(head);
  const tbody = el("tbody");
  for (const zone of data.zones) {
    const group = el("tr", "g");
    const changed = zone.rooms.filter((r) => edits.has(r.roomId)).length;
    const cell = el("td", undefined, `▾ ${zone.name} · ${whole(host, zone.rooms.length)} rooms${changed > 0 ? ` · ${whole(host, changed)} changed` : ""}`);
    cell.setAttribute("colspan", "8");
    group.append(cell);
    tbody.append(group);
    for (const row of zone.rooms.filter(keep)) tbody.append(line(host, row, edits, conflicts, redraw, nav));
  }
  table.append(thead, tbody);
  house.append(table);
  return [filters, house, dock(host, edits, redraw, data.rooms)];
}

function line(host: HostApi, row: StateRow, edits: Edits, conflicts: ReadonlySet<string>, redraw: () => void, nav: Nav): HTMLElement {
  const tr = el("tr", conflicts.has(row.roomId) ? "conf" : edits.has(row.roomId) ? "chg" : "");
  const openCell = el("td");
  openCell.append(control("btn sm", "▸", () => nav.openRoom(row.roomId)));
  if (row.blocked) {
    const cell = el("td", "mono", "blocked · deep clean · out of order (its owner) — not editable here");
    cell.setAttribute("colspan", "5");
    tr.append(el("td"), el("td", "num", row.number), cell, openCell);
    return tr;
  }
  const box = el("input") as HTMLInputElement;
  box.type = "checkbox";
  box.checked = selected.has(row.roomId);
  box.setAttribute("aria-label", `select ${row.number}`);
  box.addEventListener("change", () => { (box.checked ? selected.add(row.roomId) : selected.delete(row.roomId)); redraw(); });
  const pick = (fact: "condition" | "occupancy" | "stay", options: readonly (readonly [string, string])[]): HTMLElement => {
    const select = el("select", edits.has(row.roomId) && edits.value(row, fact) !== row[fact] ? "cell chg" : "cell") as HTMLSelectElement;
    for (const [value, label] of options) select.append(option(label, value, edits.value(row, fact) === value));
    select.addEventListener("change", () => { edits.set(row.roomId, fact, select.value); redraw(); });
    return select;
  };
  const sold = el("input", "cell") as HTMLInputElement;
  sold.placeholder = "—";
  sold.setAttribute("aria-label", `sold tonight, ${row.number}`);
  sold.value = row.soldAt === null ? "" : clock(host, row.soldAt);
  sold.addEventListener("change", () => { edits.set(row.roomId, "soldAt", sold.value.trim() === "" ? null : sold.value.trim()); redraw(); });
  const origin = el("td", "mono");
  if (conflicts.has(row.roomId)) origin.append(el("span", "said bad", `${source(row.source)} changed it since this screen was drawn — look again`));
  else {
    origin.append(document.createTextNode(`${row.sourceBy?.split(" ")[0] ?? source(row.source)} ${clock(host, row.sourceAt)}`));
    if (edits.has(row.roomId)) origin.append(el("span", "tag man", "unsaved"));
  }
  const cells = [el("td"), el("td", "num", edits.has(row.roomId) ? `${row.number} ✎` : row.number), el("td"), el("td"), el("td"), el("td")];
  cells[0]!.append(box);
  cells[2]!.append(pick("condition", CONDITIONS.map((c) => [c, c.toLowerCase()] as const)));
  cells[3]!.append(pick("occupancy", OCCUPANCIES.map((c) => [c, c.toLowerCase()] as const)));
  cells[4]!.append(sold);
  cells[5]!.append(pick("stay", STAYS));
  tr.append(...cells, origin, openCell);
  return tr;
}

function dock(host: HostApi, edits: Edits, redraw: () => void, total: number): HTMLElement {
  const bar = el("div", "dock");
  const make = (label: string, options: readonly (readonly [string, string])[]): HTMLSelectElement => {
    const select = el("select", "cell") as HTMLSelectElement;
    select.append(option(label, ""), ...options.map(([v, l]) => option(l, v)));
    return select;
  };
  const condition = make("condition", CONDITIONS.map((c) => [c, c.toLowerCase()] as const));
  const occupancy = make("occupancy", OCCUPANCIES.map((c) => [c, c.toLowerCase()] as const));
  const stay = make("stay", STAYS);
  const sold = el("input", "cell") as HTMLInputElement;
  sold.placeholder = "sold at";
  // Deliberately .btn.sm outside a list's row: the owner chose small for a toolbar's buttons (01b, 2026-09-19,
  // "A small"), against page 64 §2's "inside a row or a card". A labelled deviation, not a defect; APPS-Q43's
  // card half is a separate question and still open.
  // Apply is live only when it would change something: rows selected AND a value chosen. Otherwise it is drawn off
  // with the missing half named — it used to be a live button that did nothing with no value chosen.
  const live = control("btn sm", "Apply to selected", () => {
    for (const id of selected) {
      if (condition.value !== "") edits.set(id, "condition", condition.value);
      if (occupancy.value !== "") edits.set(id, "occupancy", occupancy.value);
      if (stay.value !== "") edits.set(id, "stay", stay.value);
      if (sold.value.trim() !== "") edits.set(id, "soldAt", sold.value.trim());
    }
    redraw();
  });
  let apply: HTMLElement = el("span");
  const refresh = (): void => {
    const chosen = [condition.value, occupancy.value, stay.value, sold.value.trim()].some((v) => v !== "");
    const next = selected.size === 0 ? el("span", "btn sm off", "Apply to selected — select rows first")
      : !chosen ? el("span", "btn sm off", "Apply to selected — choose what to set first") : live;
    if (next !== apply) { apply.replaceWith(next); apply = next; }
  };
  for (const field of [condition, occupancy, stay]) field.addEventListener("change", refresh);
  sold.addEventListener("input", refresh);
  bar.append(el("b", undefined, `${whole(host, selected.size)} rows selected`), el("span", "dim", "set for all:"), condition, occupancy, sold, stay, apply,
    el("span", "grow mono", `${whole(host, total)} of ${whole(host, total)} — no pages`));
  refresh();
  return bar;
}

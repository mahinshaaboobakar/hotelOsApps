/**
 * Catalogue — frame 7: category › item › resolutions, organisation-owned,
 * property-activated, and how they are made (the New-item dialog, the inline
 * resolution). Jobs' own, read by the other apps through Context.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { JOB_CURATE, JOB_READ } from "../../chrome/permissions";
import { standIn } from "../../chrome/standin";
import { subnav } from "../../chrome/tabs";
import { load, may, type Catalogue, type CatalogueItem } from "../../board";
import { recordedCatalogue } from "../../board/recorded/catalogue";

export async function catalogue(host: HostApi, main: HTMLElement): Promise<void> {
  const got = await load(host, JOB_READ, "catalogue", recordedCatalogue);
  const curate = may(host, JOB_CURATE);
  const body = el("div", "body");
  body.append(subnav([{ label: `${got.value.organisation} · master` }, { label: "Marina Bay · this property" }, { label: "Import / export" }], `${got.value.organisation} · master`, () => {}));
  const grid = el("div", "cols");
  grid.style.gridTemplateColumns = "260px 1fr";
  const item = got.value.items[0];
  grid.append(categories(got.value, curate), fill(el("div"), item === undefined ? null : detail(item, curate), curate ? newItem() : null));
  body.append(grid);
  if (!got.live) body.append(standIn("catalogue", got.because));
  main.replaceChildren(body);
}

function categories(c: Catalogue, curate: boolean): HTMLElement {
  const box = el("div", "card");
  const title = el("h3", undefined, "Categories");
  if (curate) title.append(fill(el("span", "grow"), control("btn sm", "＋ New")));
  box.append(title);
  for (const cat of c.categories) {
    const row = el("div", "wrow");
    row.append(el("span", cat.activeHere ? undefined : "dim", cat.name), el("span", "mono", cat.activeHere ? `${cat.department} · ${String(cat.items)} items` : "not active here"));
    box.append(row);
  }
  box.append(el("div", "mono", "A category is a name and a department code from the ADR 0119 canon. Nothing else."));
  return box;
}

function detail(item: CatalogueItem, curate: boolean): HTMLElement {
  const box = el("div", "card");
  const title = el("h3", undefined, `Air conditioning › ${item.name}`);
  if (curate) title.append(fill(el("span", "grow"), control("btn sm", "Edit")));
  box.append(title);
  const kv = el("div", "kv");
  kv.append(
    el("div", "k", "Department"), el("div", undefined, item.department),
    el("div", "k", "Default priority"), el("div", undefined, item.defaultPriority),
    el("div", "k", "Due within"), el("div", undefined, item.dueWithinMinutes === null ? "the category's, else the department's" : `${String(item.dueWithinMinutes)} min`),
    el("div", "k", "Restricted by default"), el("div", undefined, item.restricted ? "Yes" : "No"),
    el("div", "k", "Aliases"), el("div", undefined, item.aliases.map((a) => `"${a}"`).join(" · ")),
    el("div", "k", "Active at"), el("div", undefined, item.activeAt.map((p) => `${p.property} ${p.on ? "✓" : "— off"}`).join(" · ")),
  );
  box.append(kv, el("div", "sect", "Resolutions"));
  const chips = el("div", "chips");
  for (const r of item.resolutions) chips.append(control("chip", r));
  box.append(chips);
  if (curate) {
    const row = el("div", "row");
    row.append(el("div", "field", "Condenser coil cleaned"), control("btn", "＋ Add resolution"));
    box.append(row);
  }
  return box;
}

function newItem(): HTMLElement {
  const dlg = el("div", "dlg");
  dlg.append(el("h3", undefined, "New item in Air conditioning"));
  const grid = el("div", "cols");
  grid.append(
    fill(el("div"), el("label", "lbl", "Name"), el("div", "field", "Water dripping from unit"), el("label", "lbl", "Aliases · one per line"), el("div", "field", "AC leaking · water from AC · ceiling wet under AC")),
    fill(el("div"), el("label", "lbl", "Default priority"), el("div", "field", "P2"), el("label", "lbl", "Due within"), el("div", "field", "60 min"),
      el("label", "lbl", "Resolutions to start with"), el("div", "field", "Drain cleared · Drain pipe replaced · Condensate pump replaced")),
  );
  dlg.append(grid, fill(el("div", "row"), control("btn pri", "Create item"), control("btn", "Cancel"),
    el("span", "mono", "Created at the organisation; active at every property unless a property turns it off.")));
  return dlg;
}

/**
 * Catalogue — frame 7: category › item › resolutions, organisation-owned,
 * property-activated, and how they are made (the New-item dialog, the inline
 * resolution). Jobs' own, read by the other apps through Context.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { choose, lines, saying, text, values } from "../../chrome/form";
import { JOB_CURATE, JOB_READ } from "../../chrome/permissions";
import { standIn } from "../../chrome/standin";
import { subnav } from "../../chrome/tabs";
import { act, load, may, type Catalogue, type CatalogueItem } from "../../board";
import { recordedCatalogue } from "../../board/recorded/catalogue";

export async function catalogue(host: HostApi, main: HTMLElement, onChanged: () => void): Promise<void> {
  const got = await load(host, JOB_READ, "catalogue", recordedCatalogue);
  const curate = may(host, JOB_CURATE);
  const body = el("div", "body");
  const said = saying();

  /** One curation, and then the catalogue is read again. */
  const doing = (method: string, params: unknown): void => {
    void act(host, JOB_CURATE, method, params).then((done) => {
      if (done.ok) onChanged();
      else said.say(done.refused ?? "the catalogue was not changed");
    });
  };
  body.append(subnav([{ label: `${got.value.organisation} · master` }, { label: "Marina Bay · this property" }, { label: "Import / export" }], `${got.value.organisation} · master`, () => {}));
  const grid = el("div", "cols");
  grid.style.gridTemplateColumns = "260px 1fr";
  const item = got.value.items[0];
  grid.append(
    categories(got.value, curate, doing, said.say),
    fill(
      el("div", "stack"),
      item === undefined ? null : detail(item, curate, doing, said.say),
      curate ? newItem(got.value, doing, said.say) : null,
    ),
  );
  body.append(grid, said.line);
  if (!got.live) body.append(standIn("catalogue", got.because));
  main.replaceChildren(body);
}

function categories(
  c: Catalogue,
  curate: boolean,
  doing: (method: string, params: unknown) => void,
  say: (message: string) => void,
): HTMLElement {
  const box = el("div", "card");
  const title = el("h3", undefined, "Categories");
  const form = el("div");
  form.hidden = true;
  form.append(
    text("Name", "name", "Lifts"),
    text("Code", "code", "LIFTS"),
    text("Department", "department", "ENG"),
    fill(el("div", "row"), control("btn pri", "Create category", () => {
      const held = values(form);
      if (String(held.name ?? "").length === 0 || String(held.code ?? "").length === 0) {
        say("a category needs a name and a code");
        return;
      }

      doing("saveCategory", { name: held.name, code: held.code, department: held.department });
    })),
  );
  if (curate) title.append(fill(el("span", "grow"), control("btn sm", "＋ New", () => { form.hidden = !form.hidden; })));
  box.append(title, form);
  for (const cat of c.categories) {
    const row = el("div", "wrow");
    row.append(el("span", cat.activeHere ? undefined : "dim", cat.name), el("span", "mono", cat.activeHere ? `${cat.department} · ${String(cat.items)} items` : "not active here"));
    box.append(row);
  }
  box.append(el("div", "mono", "A category is a name and a department code from the ADR 0119 canon. Nothing else."));
  return box;
}

function detail(
  item: CatalogueItem,
  curate: boolean,
  doing: (method: string, params: unknown) => void,
  say: (message: string) => void,
): HTMLElement {
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
  for (const r of item.resolutions) chips.append(control("chip", r.name));
  box.append(chips);
  if (curate) {
    const adding = el("div");
    adding.append(text("New resolution", "name", "Condenser coil cleaned"));
    adding.append(fill(el("div", "row"), control("btn", "＋ Add resolution", () => {
      const name = String(values(adding).name ?? "");
      if (name.length === 0) {
        say("a resolution needs a name");
        return;
      }

      doing("addResolution", { categoryId: item.categoryId, name });
    })));
    box.append(adding);
  }

  return box;
}

function newItem(
  c: Catalogue,
  doing: (method: string, params: unknown) => void,
  say: (message: string) => void,
): HTMLElement {
  const dlg = el("div", "dlg");
  dlg.append(el("h3", undefined, "New item"));
  const grid = el("div", "cols");
  grid.append(
    fill(
      el("div"),
      choose("Category", "categoryId", c.categories.map((category) => ({ value: category.id, label: category.name }))),
      text("Name", "name", "Water dripping from unit"),
      text("Code", "code", "AC_WATER_DRIPPING"),
      lines("Aliases · comma separated", "aliases", "AC leaking, water from AC, ceiling wet under AC"),
    ),
    fill(
      el("div"),
      choose("Default priority", "defaultPriority", [
        { value: "P3", label: "P3" }, { value: "P2", label: "P2" }, { value: "P1", label: "P1" },
      ]),
      text("Due within · minutes", "dueWithinMinutes", "60"),
    ),
  );

  dlg.append(grid, fill(el("div", "row"), control("btn pri", "Create item", () => {
    const held = values(dlg);
    if (String(held.name ?? "").length === 0 || String(held.code ?? "").length === 0) {
      say("an item needs a name and a code");
      return;
    }

    doing("saveItem", {
      categoryId: held.categoryId,
      name: held.name,
      code: held.code,
      defaultPriority: held.defaultPriority,
      dueWithinMinutes: Number(held.dueWithinMinutes ?? 0),
      aliases: String(held.aliases ?? "")
        .split(",")
        .map((alias) => alias.trim())
        .filter((alias) => alias.length > 0),
    });
  }), el("span", "mono", "Created at the organisation; active at every property unless a property turns it off.")));
  return dlg;
}

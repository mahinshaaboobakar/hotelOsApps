/**
 * The concern-policy tab — mockup 02 frames 1 and 7–10: Engineering's clock
 * with the scope rail; the policies list for two departments; the three-step
 * flow that creates one, ending in the ladder builder.
 */

import { control, el, fill } from "../../chrome/element";
import { priority } from "../../chrome/marks";
import type { PolicyRow, Settings } from "../../board";

function rail(s: Settings): HTMLElement {
  const box = el("div", "card");
  box.append(el("h3", undefined, "Scope"));
  for (const scope of s.scopes) {
    const row = el("div", "wrow");
    row.style.paddingLeft = `${String(scope.indent * 14)}px`;
    row.append(el("span", undefined, scope.label), scope.state === "override" ? el("span", "pill", "override") : el("span", "mono", scope.state));
    box.append(row);
  }
  box.append(el("div", "mono", "Property → department → category → item. Each level overrides only the rows it changes."));
  return box;
}

/** Frame 1 — Engineering's clock, per priority. */
export function concernPolicy(s: Settings, configure: boolean, onView: (view: string) => void): HTMLElement {
  const grid = el("div", "cols");
  grid.style.gridTemplateColumns = "220px 1fr";
  const box = el("div", "card");
  const title = el("h3", undefined, "Engineering ");
  title.append(el("span", "mono", "overrides the property default"), fill(el("span", "grow"), control("btn sm", "All policies", () => onView("list"))));
  box.append(title);
  const t = el("table");
  const head = el("tr");
  for (const h of ["Priority", "Due within", "At risk at", "Stuck: not accepted", "Stuck: no session", "Ladder", "Manager at risk"]) head.append(el("th", undefined, h));
  t.append(head);
  for (const r of s.engineeringRules) {
    const tr = el("tr");
    tr.append(fill(el("td"), priority(r.priority)), el("td", undefined, r.due), el("td", undefined, r.atRisk), el("td", undefined, r.notAccepted),
      el("td", undefined, r.noSession), el("td", undefined, r.ladder), fill(el("td"), fill(el("span", r.managerAtRisk ? "tog on" : "tog"), el("i")), r.managerAtRisk ? "yes" : "no"));
    t.append(tr);
  }
  const nt = el("tr");
  nt.append(fill(el("td"), priority("NOT_TRIAGED")), el("td", "dim", "Not triaged: no clock · stuck after 15 min untriaged → supervisor"));
  (nt.lastElementChild as HTMLElement).setAttribute("colspan", "6");
  t.append(nt);
  box.append(t, el("div", "mono", "Outside presence: P1 keeps running · P2 and P3 pause until the department is present."));
  if (configure) box.append(fill(el("div", "row"), control("btn pri", "Save"), control("btn", "Discard")));
  return fill(grid, rail(s), box);
}

/** Frame 7 — every policy this property has, nested by scope. */
export function policies(s: Settings, configure: boolean, onView: (view: string) => void): HTMLElement {
  const root = el("div");
  const top = el("div", "row");
  if (configure) top.append(control("btn pri", "＋ New policy", () => onView("1")));
  top.append(el("span", "mono", `${String(s.policies.length)} policies · a job uses the most specific one that matches it`), fill(el("span", "grow"), control("btn sm", "Engineering's clock", () => onView("engineering"))));
  const t = el("table");
  const head = el("tr");
  for (const h of ["Scope", "Policy name", "Due · P1 / P2 / P3", "At risk", "Ladder (P1)", "Used by", ""]) head.append(el("th", undefined, h));
  t.append(head);
  for (const p of s.policies) t.append(policyLine(p, configure));
  return fill(root, top, t, el("div", "pager", `${String(s.policies.length)} of ${String(s.policies.length)}`));
}

function policyLine(p: PolicyRow, configure: boolean): HTMLElement {
  const tone: Record<PolicyRow["scope"], string> = { property: "", department: "", category: "run", item: "ok" };
  const indent: Record<PolicyRow["scope"], number> = { property: 0, department: 0, category: 28, item: 46 };
  const tr = el("tr");
  const scope = fill(el("td"), el("span", `pill ${tone[p.scope]}`.trim(), p.scope), ` ${p.scopeLabel}`);
  scope.style.paddingLeft = `${String(10 + indent[p.scope])}px`;
  tr.append(scope, el("td", undefined, p.name), el("td", undefined, p.due), el("td", undefined, p.atRisk), el("td", undefined, p.ladder), el("td", undefined, p.usedBy),
    fill(el("td"), configure ? control("btn sm", "Edit") : null));
  return tr;
}

/** Frames 8–10 — new policy: name and scope, the clock, the ladder. */
export function policyFlow(s: Settings, step: "1" | "2" | "3", onView: (view: string) => void): HTMLElement {
  const root = el("div");
  const steps = el("div", "subnav");
  for (const [n, label] of [["1", "1 · Name and scope"], ["2", "2 · The clock"], ["3", "3 · The ladder"]] as const) {
    steps.append(control(n === step ? "tab on" : "tab", label, () => onView(n)));
  }
  root.append(steps);
  if (step === "1") root.append(scopeStep(), fill(el("div", "row"), control("btn pri", "Next · the clock", () => onView("2")), control("btn", "Cancel", () => onView("list"))));
  else if (step === "2") root.append(clockStep(s), fill(el("div", "row"), control("btn pri", "Next · the ladder", () => onView("3")), control("btn", "Back", () => onView("1"))));
  else root.append(ladderStep(), fill(el("div", "row"), control("btn pri", "Save policy", () => onView("list")), control("btn", "Back", () => onView("2"))));
  return root;
}

function sample(title: string, lines: readonly [string, string, boolean?][], applies: string): HTMLElement {
  const box = el("div", "card");
  box.append(el("h3", undefined, title));
  for (const [label, value, ph] of lines) box.append(el("label", "lbl", label), el("div", ph === true ? "field ph" : "field", value));
  box.append(el("div", "mono", applies));
  return box;
}

function scopeStep(): HTMLElement {
  const grid = el("div", "cols");
  grid.append(
    sample("Sample A · Engineering, one category", [["Department", "Engineering ▾"], ["Category · optional", "AC not working ▾"], ["Item · optional", "— all items of the category ▾", true], ["Name", "AC — guest in room"], ["Start from", "Copy of \"Engineering\" (department) ▾"]], "Applies to: every AC job at Marina Bay, unless the item has its own policy."),
    sample("Sample B · Housekeeping, one category", [["Department", "Housekeeping ▾"], ["Category · optional", "Bottle of water ▾"], ["Item · optional", "— all items of the category ▾", true], ["Name", "Water — 10 minutes"], ["Start from", "Copy of \"Housekeeping\" (department) ▾"]], "Applies to: Still water · Sparkling water — the two items of the category."),
  );
  const c = el("div", "card");
  c.append(el("h3", undefined, "Sample C · one item, narrower still"));
  const kv = el("div", "kv");
  kv.style.gridTemplateColumns = "130px 1fr";
  for (const [k, v] of [["Department", "Engineering"], ["Category", "AC not working"], ["Item", "Water dropping from unit"],
    ["Name", "AC leak — ceiling risk"], ["Start from", 'Copy of "AC — guest in room" (category)']] as const) {
    kv.append(el("div", "k", k), el("div", undefined, v));
  }
  c.append(kv);
  return fill(el("div"), grid, c, el("div", "mono", "The pickers are the catalogue's own lists for this property. Leave category empty for a department policy; leave item empty for a category policy. One policy per scope."));
}

function clockStep(s: Settings): HTMLElement {
  const t = el("table");
  const head = el("tr");
  for (const h of ["Priority", "Due within", "At risk when", "Stuck if not accepted in", "Stuck if no work session in", "Outside presence"]) head.append(el("th", undefined, h));
  t.append(head);
  for (const r of s.engineeringRules) {
    const tr = el("tr");
    tr.append(fill(el("td"), priority(r.priority)), el("td", undefined, r.due), el("td", undefined, `${r.atRisk} of due`), el("td", undefined, r.notAccepted), el("td", undefined, r.noSession), el("td", undefined, r.priority === "P1" ? "keeps running" : "pauses"));
    t.append(tr);
  }

  const nt = el("tr");
  const rest = el("td", "dim", "Not triaged: no clock · stuck after 15 min untriaged → supervisor");
  rest.setAttribute("colspan", "5");
  nt.append(fill(el("td"), priority("NOT_TRIAGED")), rest);
  t.append(nt);
  return fill(el("div"), t, el("div", "mono", "Example, P1 raised 13:31: at risk from 14:00 (75 % of 40 min) · breached from 14:10 · stuck at 13:39 if nobody has accepted, or at 14:02 if accepted but nobody has started."));
}

function ladderStep(): HTMLElement {
  const grid = el("div", "cols3");
  const ladders: readonly [string, readonly [string, string][], boolean][] = [
    ["P1 · 4 steps · sample A", [["1 · Assignee", "at risk"], ["2 · Department supervisor", "breached"], ["3 · Department manager", "breached + 15 min"], ["4 · Property jobs manager", "breached + 45 min"]], true],
    ["P2 · 3 steps", [["1 · Assignee", "at risk"], ["2 · Department supervisor", "breached"], ["3 · Department manager", "breached + 60 min"]], false],
    ["P3 · 2 steps", [["1 · Assignee", "at risk"], ["2 · Department supervisor", "breached"]], false],
  ];
  for (const [title, rungs, managerAtRisk] of ladders) {
    const box = el("div", "card");
    box.append(el("h3", undefined, title));
    for (const [role, trigger] of rungs) box.append(fill(el("div", "wrow"), el("span", undefined, role), el("span", "mono", trigger)));
    box.append(fill(el("div", "row"), fill(el("span", managerAtRisk ? "tog on" : "tog"), el("i")), el("span", "mono", "manager also accountable from at risk")));
    box.append(fill(el("div", "row"), control("btn sm", "＋ step"), el("span", "mono", "drag to reorder")));
    grid.append(box);
  }
  const dlg = el("div", "dlg");
  dlg.append(el("h3", undefined, "Add a step to P1"), el("label", "lbl", "Role"), el("div", "field", "Department manager ▾"), el("label", "lbl", "Becomes accountable"), el("div", "field", "when breached ▾ + 15 min"),
    fill(el("div", "row"), control("btn pri", "Add"), control("btn", "Cancel")));
  return fill(el("div"), grid, dlg, el("div", "mono", "Stuck goes straight to the supervisor for every priority. If a step's role has nobody today, the sweep moves one step up and records why."));
}

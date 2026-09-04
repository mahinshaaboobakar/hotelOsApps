/**
 * The other five settings tabs — mockup 02 frames 2 to 6: shifts & presence,
 * who is told, holds & reminders, closing & rating, access (read-only).
 */

import { control, el, fill } from "../../chrome/element";
import type { Detail, Settings } from "../../board";

function toggle(on: boolean, text?: string): HTMLElement {
  return fill(el("span"), fill(el("span", on ? "tog on" : "tog"), el("i")), text ?? "");
}

function table(headers: readonly string[], rows: readonly (readonly (Node | string)[])[]): HTMLElement {
  const t = el("table");
  const head = el("tr");
  for (const h of headers) head.append(el("th", undefined, h));
  t.append(head);
  for (const cells of rows) {
    const tr = el("tr");
    for (const c of cells) tr.append(fill(el("td"), c));
    t.append(tr);
  }
  return t;
}

function saveRow(configure: boolean): HTMLElement | null {
  return configure ? fill(el("div", "row"), control("btn pri", "Save"), control("btn", "Discard")) : null;
}

function kv(lines: readonly Detail[]): HTMLElement {
  const grid = el("div", "kv");
  grid.style.gridTemplateColumns = "130px 1fr";
  for (const line of lines) grid.append(el("div", "k", line.k), el("div", undefined, line.v));
  return grid;
}

/** Frame 2 — when a department's clock runs. */
export function presence(s: Settings, configure: boolean): HTMLElement {
  const rows = s.presence.map((p) => [
    p.department, p.enabled ? el("span", "pill ok", "on") : el("span", "pill", "off"),
    p.enabled ? toggle(p.followShifts, p.followShifts ? "yes" : "no · hours only") : el("span", "dim", "—"),
    p.enabled ? p.hours : el("span", "dim", "—"), p.now,
  ]);
  return fill(el("div"), table(["Department", "Presence", "Follow Workforce shifts", "Service hours (fallback)", "Now"], rows), saveRow(configure));
}

/** Frame 3 — which role hears which concern. */
export function whoIsTold(s: Settings, configure: boolean): HTMLElement {
  const rows = s.whoIsTold.map((w) => [
    w.role, toggle(w.atRisk), w.breached === "—" ? el("span", "dim", "—") : toggle(true, w.breached === "yes" ? "" : w.breached),
    w.stuck === "—" ? el("span", "dim", "—") : toggle(true, w.stuck === "yes" ? "" : w.stuck), toggle(w.untriaged), w.repeat, w.departments,
  ]);
  return fill(el("div"), table(["Role", "At risk", "Breached", "Stuck", "Not triaged", "Repeat every", "Departments"], rows),
    el("div", "mono", "In-app only. There is no channel, no quiet hours, no per-person setting — the role decides (S9 D10)."), saveRow(configure));
}

/** Frame 4 — waiting with a date, and being warned before it. */
export function holds(s: Settings, configure: boolean): HTMLElement {
  const grid = el("div", "cols");
  grid.append(
    fill(el("div", "card"), el("h3", undefined, "Putting a job on hold"), kv(s.holds)),
    fill(el("div", "card"), el("h3", undefined, "Warn before the date"), table(["When", "Who"], s.holdWarnings.map((w) => [w.when, w.who]))),
  );
  return fill(el("div"), grid, el("div", "mono", "Manual reminders (S9 D3) need no setting: anyone can set one on a job they can see, for themselves, from the job's \"More ▾\"."), saveRow(configure));
}

/** Frame 5 — from RESOLVED to CLOSED, and what the guest is asked. */
export function closing(s: Settings, configure: boolean): HTMLElement {
  const grid = el("div", "cols");
  grid.append(
    fill(el("div", "card"), el("h3", undefined, "Auto-close after RESOLVED"), table(["Scope", "Hours"], s.closing.map((c) => [c.scope, c.hours])),
      el("div", "mono", "Until then the raiser may reopen; after, it is CLOSED and the resolution stands.")),
    fill(el("div", "card"), el("h3", undefined, "Resolving and rating"), kv(s.rating)),
  );
  return fill(el("div"), grid, saveRow(configure));
}

/** Frame 6 — who holds what, shown, not edited: Workforce's and Identity's facts through Context. */
export function access(s: Settings): HTMLElement {
  return fill(el("div"), table(["Label", "Who", "Comes from"], s.access.map((a) => [a.label, a.who, a.from])),
    el("div", "mono", "To change any of this: postings and headships in Workforce; the jobs-manager grant in Identity (GM only). Jobs has no editor because it owns none of these facts — none of it is in Jobs' database."));
}

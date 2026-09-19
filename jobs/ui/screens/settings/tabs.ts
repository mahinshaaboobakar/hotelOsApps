/**
 * The other five settings tabs — mockup 02 frames 2 to 6: shifts & presence,
 * who is told, holds & reminders, closing & rating, access (read-only).
 */

import { formatNumber, type PropertyEnvironment } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { choose, confirming, text, toggle as switchOf, values } from "../../chrome/form";
import type { Detail, Settings } from "../../board";

/** What a settings tab does when Save is pressed — the one call it makes. */
export type Saving = (method: string, params: unknown) => void;

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

/**
 * Save and Discard, where Save sends what the form holds and Discard puts the
 * screen back to what the service says.
 *
 * Discard is a re-read rather than a reset: what is being discarded is the
 * edit, and the thing to go back to is the property's own answer rather than
 * the values this screen happened to be drawn with.
 */
function saveRow(
  configure: boolean,
  form: HTMLElement,
  save: Saving,
  method: string,
  params: (held: Record<string, string | boolean>) => unknown,
  discard: () => void,
): HTMLElement | null {
  if (!configure) return null;
  return fill(
    el("div", "row"),
    control("btn pri", "Save", () => save(method, params(values(form)))),
    control("btn", "Discard", discard),
  );
}

function kv(lines: readonly Detail[]): HTMLElement {
  const grid = el("div", "kv");
  grid.style.gridTemplateColumns = "130px 1fr";
  for (const line of lines) grid.append(el("div", "k", line.k), el("div", undefined, line.v));
  return grid;
}

/** Frame 2 — when a department's clock runs. */
export function presence(s: Settings, configure: boolean, save: Saving, discard: () => void): HTMLElement {
  const rows = s.presence.map((p) => [
    p.department, p.enabled ? el("span", "pill ok", "on") : el("span", "pill", "off"),
    p.enabled ? toggle(p.followShifts, p.followShifts ? "yes" : "no · hours only") : el("span", "dim", "—"),
    p.enabled ? p.hours : el("span", "dim", "—"), p.now,
  ]);
  // One department at a time, because that is what the service takes and what
  // a person changes: a department's clock is its own decision.
  const form = el("div", "cols");
  const first = s.presence[0];
  form.append(
    fill(
      el("div"),
      choose("Department", "department", s.presence.map((p) => ({ value: p.department, label: p.department }))),
      switchOf("Presence is followed here", "enabled", first?.enabled ?? true),
      switchOf("Follow Workforce shifts", "followShifts", first?.followShifts ?? true),
    ),
    fill(
      el("div"),
      text("Service hours from", "from", "07:00"),
      text("to", "to", "23:00"),
      el("div", "hint mono", "The fallback when shifts are not followed."),
    ),
  );

  return fill(
    el("div"),
    table(["Department", "Presence", "Follow Workforce shifts", "Service hours (fallback)", "Now"], rows),
    configure ? form : null,
    saveRow(configure, form, save, "savePresence", (held) => ({
      department: held.department,
      enabled: held.enabled === true,
      followShifts: held.followShifts === true,
    }), discard),
    configure
      ? fill(el("div", "row"), control("btn", "Save the hours", () => {
        const held = values(form);
        if (String(held.from ?? "").length === 0 || String(held.to ?? "").length === 0) return;
        save("saveHours", { department: held.department, from: held.from, to: held.to });
      }))
      : null,
  );
}

/** Frame 3 — which role hears which concern. */
export function whoIsTold(s: Settings, configure: boolean): HTMLElement {
  const rows = s.whoIsTold.map((w) => [
    w.role, toggle(w.atRisk), w.breached === "—" ? el("span", "dim", "—") : toggle(true, w.breached === "yes" ? "" : w.breached),
    w.stuck === "—" ? el("span", "dim", "—") : toggle(true, w.stuck === "yes" ? "" : w.stuck), toggle(w.untriaged), w.repeat, w.departments,
  ]);
  return fill(el("div"), table(["Role", "At risk", "Breached", "Stuck", "Not triaged", "Repeat every", "Departments"], rows),
    el("div", "mono", "In-app only. There is no channel, no quiet hours, no per-person setting — the role decides (S9 D10)."),
    configure
      ? el("div", "mono dim", "Editing waits on one read. The tab groups the subscriptions by role to draw them and "
        + "saving replaces the whole set, so an editor needs the rows as they are stored — which this read does not "
        + "yet carry. Reported rather than half-wired.")
      : null);
}

/** Frame 4 — waiting with a date, and being warned before it. */
export function holds(
  s: Settings,
  configure: boolean,
  save: Saving,
  discard: () => void,
  property: PropertyEnvironment,
): HTMLElement {
  const grid = el("div", "cols");
  grid.append(
    fill(el("div", "card"), el("h3", undefined, "Putting a job on hold"), kv(s.holds)),
    fill(
      el("div", "card"),
      el("h3", undefined, "Warn before the date"),
      table(["When", "Who"], s.holdWarnings.map((w) => [w.when, w.who])),
      // The read returns the ten nearest, so the card says so rather than
      // looking like the whole set — a truncated list that does not admit it is
      // the defect the conformance pass looks for.
      el("div", "mono", s.holdWarnings.length < 10
        ? `${formatNumber(s.holdWarnings.length, property)} hold(s) with a date`
        : "the ten nearest holds with a date"),
    ),
  );
  const form = el("div", "cols");
  form.append(
    fill(el("div"), text("Longest hold · days", "maxHoldDays", "30"), text("Warn · days before", "warnDaysBefore", "1")),
    fill(
      el("div"),
      choose("Warn whom", "warnRole", [
        { value: "SUPERVISOR", label: "Supervisor" },
        { value: "MANAGER", label: "Manager" },
        { value: "JOBS_MANAGER", label: "Jobs manager" },
      ]),
      switchOf("Warn the assignee on the day", "warnAssigneeOnDay", true),
    ),
  );

  return fill(el("div"), grid, configure ? form : null, saveRow(configure, form, save, "saveHold", (held) => ({
    maxHoldDays: Number(held.maxHoldDays ?? 30),
    warnDaysBefore: Number(held.warnDaysBefore ?? 1),
    warnRole: held.warnRole,
    warnAssigneeOnDay: held.warnAssigneeOnDay === true,
  }), discard), el("div", "mono", "Manual reminders (S9 D3) need no setting: anyone can set one on a job they can see, for themselves, from the job's \"More ▾\"."));
}

/** Frame 5 — from RESOLVED to CLOSED, and what the guest is asked. */
export function closing(s: Settings, configure: boolean, save: Saving, discard: () => void): HTMLElement {
  const grid = el("div", "cols");
  grid.append(
    fill(el("div", "card"), el("h3", undefined, "Auto-close after RESOLVED"), table(["Scope", "Hours"], s.closing.map((c) => [c.scope, c.hours])),
      el("div", "mono", "Until then the raiser may reopen; after, it is CLOSED and the resolution stands.")),
    fill(el("div", "card"), el("h3", undefined, "Resolving and rating"), kv(s.rating)),
  );
  const form = el("div", "cols");
  form.append(
    fill(el("div"), text("Department · empty for the property", "department", "a department code"), text("Auto-close after · hours", "autoCloseHours", "4")),
    fill(el("div"), switchOf("Ask the guest to rate on close", "ratingOnClose", true)),
  );

  return fill(el("div"), grid, configure ? form : null, saveRow(configure, form, save, "saveClosing", (held) => ({
    department: String(held.department ?? "").length === 0 ? undefined : held.department,
    autoCloseHours: Number(held.autoCloseHours ?? 4),
    ratingOnClose: held.ratingOnClose === true,
  }), discard));
}

/**
 * Frame 6 — who holds what, and the one grant this application does own.
 *
 * The caption used to end *"Jobs has no editor because it owns none of these
 * facts"*, and for the jobs-manager grant that stopped being true: design
 * §4.2 makes the general manager's action Jobs' own, published as
 * `user.jobs_manager_granted` and folded into `property#jobs_manager` by the
 * Kernel. Postings and headships are still Workforce's and are still read-only
 * here.
 *
 * **The grant is by id, not by a person picker.** Jobs holds no people, and a
 * picker would need a directory this application has no business keeping — so
 * the field takes the id the shell shows, and the list shows ids back.
 */
export function access(
  s: Settings, configure: boolean, save: Saving, discard: () => void,
): HTMLElement {
  // **Revoking is destructive and is drawn as destructive** — standard §2's
  // pair. It ended a person's property-wide authority over every job from a
  // plain `btn sm`, with no confirm at all, while cancelling one job had both.
  // A fidelity sweep measures the button that is there and finds it perfect;
  // nothing measures that it should have been the other button.
  const asked = el("div");
  const held = s.jobsManagers.map((m) => [
    m.userId,
    m.grantedAt,
    configure
      ? control("btn sm danger", "Revoke…", () => {
        asked.replaceChildren(confirming(
          "Revoke this person's jobs-manager grant?",
          `${m.userId} · granted ${m.grantedAt}. They keep every other permission they hold.`,
          () => {
            asked.replaceChildren();
            save("revokeJobsManager", { userId: m.userId });
          },
          () => asked.replaceChildren(),
        ));
      })
      : el("span", "dim", "—"),
  ]);

  const grant = fill(el("div", "cols"), fill(el("div"), text("User id", "userId", "")));

  return fill(
    el("div"),
    table(["Label", "Who", "Comes from"], s.access.map((a) => [a.label, a.who, a.from])),
    // `sect`, not `hd`: the sheet already names this treatment, and `hd` was a
    // second name for it that no rule defined.
    el("div", "sect", "Jobs managers"),
    s.jobsManagers.length > 0
      ? table(["User", "Granted", ""], held)
      : el("div", "dim", "Nobody at this property holds it."),
    asked,
    configure ? grant : null,
    saveRow(configure, grant, save, "grantJobsManager", (h) => ({ userId: h.userId }), discard),
    el("div", "mono", "Postings and headships are Workforce's and are read here, not edited. The jobs-manager grant is this property's to make: Jobs announces it and the Kernel writes the relation — nothing here writes an authorization tuple."),
  );
}

/**
 * One module instance, in one realm.
 *
 * Each pane of the harness is a real `<iframe>`, because that is the shape the
 * module actually gets. It matters visually: the module sizes itself with
 * `100vh`, which is its realm's height in production and would be the whole
 * scrolling page if it were mounted into a plain `<div>` — the capture would
 * then show a layout no property will ever see.
 *
 * # It fakes the host and nothing else
 *
 * The identity, the granted capabilities and the answers to `host.call` are
 * this file's. The module's own code, its stylesheet and its token references
 * are the shipped ones, so what appears here is what a property would see.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { activate } from "../application";
import { recordedOvertime, recordedWeek } from "../roster";
import { recordedDay } from "../roster/attendance";
import { recordedRegister } from "../roster/duty";
import { recordedLeave } from "../roster/leave";
import { recordedFirstRun, recordedPeople } from "../roster/people";
import { recordedPolicy } from "../roster/policy";
import { recordedMonth } from "../roster/reports";
import { recordedSchedule } from "../roster/schedule";
import { recordedNoTeams, recordedTeams } from "../roster/teams";

const params = new URLSearchParams(location.search);

/** Which week the host answers with — the harness varies the DATA, not the module. */
const week = params.get("state") === "overtime" ? recordedOvertime : recordedWeek;

/**
 * A host that grants what the manifest requests and answers from the fixtures.
 *
 * `granted` is a parameter so the harness can show the refusal path too: the
 * module renders a stand-in note when a capability was not granted, and that
 * note is a design element the audit has to be able to see.
 */
function host(granted: readonly string[]): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: granted },

    // What the host tells a module at connect — `JOBS-Q1(8)`. The harness has
    // to hand it too, or the panes run against a contract no property serves.
    property: { timezone: "Asia/Kolkata", locale: null },

    /**
     * Answer every method a screen asks for.
     *
     * **Every one, and that is not thoroughness for its own sake.** The
     * module's seam rethrows anything that is not a `HostCallError`, so a
     * method this fake does not know leaves the screen blank rather than
     * falling back — eleven panes photographed empty before this was fixed.
     * The harness has to be as complete as the host it stands in for.
     */
    call(capability: string, method: string): Promise<unknown> {
      // The first run is a data state, not a screen: the same People screen,
      // answered with a property that has posted nobody.
      if (method === "people") {
        return Promise.resolve(
          params.get("state") === "first-run" ? recordedFirstRun : recordedPeople);
      }

      // Frame 7 the same way: the property that has formed no team is answered
      // with none, and the screen it gets is the screen everybody gets.
      if (method === "teams") {
        return Promise.resolve(
          params.get("state") === "no-teams" ? recordedNoTeams : recordedTeams);
      }

      const answers: Record<string, unknown> = {
        week,
        leave: recordedLeave,
        day: recordedDay,
        register: recordedRegister,
        month: recordedMonth,
        schedule: recordedSchedule,
        policy: recordedPolicy,
      };

      const answer = answers[method];

      return answer === undefined
        ? Promise.reject(new HostCallError(
          { kind: "unavailable", message: `no answer for ${capability}/${method}` }))
        : Promise.resolve(answer);
    },

    on(): () => void {
      return () => {};
    },
  };
}

const granted = params.get("granted") === "none"
  ? []
  : ["roster.read", "roster.plan", "leave.approve", "attendance.record"];

activate(host(granted)).mount(document.body);

/**
 * Click one person's cell on one day of the rota.
 *
 * @param person whose row
 * @param day which column, zero-based from Monday
 */
function clickCell(person: string, day: number): void {
  const rows = Array.from(document.querySelectorAll<HTMLElement>(".rgrid .person"));
  const row = rows.find((node) => node.textContent?.includes(person) === true);
  if (row === undefined) return;

  // The grid is one flat list: a person cell followed by seven day cells, so a
  // row's day is its own index plus the offset.
  const cells = Array.from(row.parentElement?.children ?? []);
  const start = cells.indexOf(row);
  (cells[start + 1 + day] as HTMLElement | undefined)?.click();
}

/**
 * Click the first element matching `selector` whose text contains `text` —
 * waiting for it to exist rather than assuming it already does.
 *
 * **A fixed pause is not a wait for a control.** This clicked once, immediately,
 * and every capture that opened a dialog from a screen's header worked while the
 * one whose button sits under an asynchronously loaded list silently did
 * nothing: *Add a member* is drawn after a team's members resolve, so the click
 * ran against a detail pane that had a Rename and a Stand down and not yet the
 * button being asked for. The capture then photographed a correct screen with no
 * dialog on it, which is the worst failure a harness can have — it looks like a
 * missing feature, and there was a `settle()` on the line above vouching for it.
 *
 * Polling for the node makes the harness wait for the thing rather than for a
 * duration, which is the same correction the ready flag already applies to the
 * screen as a whole.
 *
 * @returns whether anything was found and clicked
 */
async function click(selector: string, text: string): Promise<boolean> {
  for (let turn = 0; turn < 40; turn += 1) {
    for (const node of Array.from(document.querySelectorAll<HTMLElement>(selector))) {
      if (node.textContent?.includes(text) === true) {
        node.click();
        return true;
      }
    }

    await new Promise((resolve) => { setTimeout(resolve, 50); });
  }

  return false;
}

/**
 * Drive this realm to the screen it was asked for, then say so.
 *
 * The flag is what the capture waits on. A screenshot taken on a timer catches
 * a half-rendered screen often enough to be believed, and a loading state
 * photographs well.
 */
/**
 * Drive this realm to the state it was asked for, then say so.
 *
 * # Each step waits for the one before it
 *
 * Every screen draws **asynchronously** — it awaits the seam before it renders
 * anything. So a click on a control that screen owns has to come after the
 * screen exists, not in the same turn as the click that opened it: the first
 * version clicked *Policy* and then *New shift* immediately, and the second
 * click found nothing because the header had not been drawn yet.
 */
const misses: string[] = [];

async function drive(): Promise<void> {
  const settle = (): Promise<void> =>
    new Promise((resolve) => { setTimeout(resolve, 120); });

  await settle();

  // Two levels: the bar names a section, `view` names one of its views. The
  // module opens Rota's first view on mount, so neither is required.
  const screen = params.get("screen");
  if (screen !== null && screen !== "Rota") { missed(".head .tab", screen, await click(".head .tab", screen)); await settle(); }

  const view = params.get("view");
  if (view !== null) { missed(".tabs .tab", view, await click(".tabs .tab", view)); await settle(); }

  const tab = params.get("tab");
  if (tab !== null) { missed(".tab", tab, await click(".tab", tab)); await settle(); }

  // The two states the rail cannot reach are opened the way a person opens
  // them — by clicking the button the approved frame draws.
  const open = params.get("open");
  if (open === "print") { missed(".btn", "Print", await click(".btn", "Print")); await settle(); }
  if (open === "shift") { missed(".btn", "New shift", await click(".btn", "New shift")); await settle(); }
  if (open === "leave") { missed(".btn", "Request leave", await click(".btn", "Request leave")); await settle(); }
  if (open === "duty") { missed(".btn", "Assign duty", await click(".btn", "Assign duty")); await settle(); }
  if (open === "form") { missed(".btn", "Form a team", await click(".btn", "Form a team")); await settle(); }

  // Frame 2 opens by clicking the team, and frame 6 by clicking the person —
  // both are rows, and both are the row the frame draws rather than the first
  // one that matches. The rota picker taught that lesson once already.
  const openTeam = params.get("team");
  if (openTeam !== null) { missed("button.tgrid", openTeam, await click("button.tgrid", openTeam)); await settle(); }

  const endWho = params.get("end");
  if (endWho !== null) { missed("button.row", endWho, await click("button.row", endWho)); await settle(); }

  // After the team, never before: both controls live in the detail pane, which
  // does not exist until one is open. This is the same ordering the shift
  // dialog needed — a click on a control its own screen has not drawn yet
  // finds nothing and fails silently.
  if (open === "member") { missed(".btn", "Add a member", await click(".btn", "Add a member")); await settle(); }
  if (open === "down") { missed(".btn", "Stand down", await click(".btn", "Stand down")); await settle(); }

  // The rota's picker opens on a cell rather than a button, so it is reached by
  // clicking the cell a person would click — and it must be THE cell the frame
  // draws it on. The first version clicked the first chip reading "M", which is
  // Priya's Monday, so the capture showed a different person and a different day
  // than the frame it was to be read beside.
  if (open === "pick") { clickCell("Anjali Menon", 3); await settle(); }

  // **Timers, not `requestAnimationFrame`.** A capture harness is driven in a
  // tab that is frequently not the foreground one, and rAF does not fire there —
  // the frame never paints, the callback never runs, and the flag never lands.
  // Every screen rendered correctly while `data-ready` stayed absent, which is
  // the worst shape for a signal: the thing it reports was fine and the signal
  // was not.
  //
  // **A step that found nothing never becomes ready.** The realm is only worth
  // photographing if every control the query asked for was actually reached, so
  // a miss is stated on the screen and the flag is withheld — the capture cannot
  // then be mistaken for a screen that simply has no dialog on it.
  if (misses.length > 0) {
    const banner = document.createElement("div");

    banner.textContent = "HARNESS MISSED: " + misses.join(" · ");
    banner.setAttribute("style", "position:fixed;inset:0 0 auto 0;z-index:9999;"
      + "background:#b91c1c;color:#fff;font:600 13px system-ui;padding:10px 14px");

    document.body.append(banner);
    document.documentElement.setAttribute("data-ready", "missed");
    return;
  }

  document.documentElement.setAttribute("data-ready", "true");
}

/** Remember a control the query named and the screen never drew. */
function missed(selector: string, text: string, found: boolean): void {
  if (!found) misses.push(`${text} (${selector})`);
}

void drive();

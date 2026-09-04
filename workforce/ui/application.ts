/**
 * Workforce's desktop module — the rota, leave, attendance and the numbers.
 *
 * # What this is
 *
 * A **package's** UI, not the shell's: built in the package, shipped in `ui/`,
 * and run in its own iframe realm. Its only connection to HotelOS is
 * `@hotelos/sdk` and the port the host transfers in — there is no ambient
 * capability here, so no database, no tuple writer and no route past the Hub,
 * because **no name is bound to those things in this realm**.
 *
 * # It is styled, never themed
 *
 * Every colour is a `var()` on the host's injected tokens, and only on the
 * fourteen names the shell publishes. An installed application looks like
 * HotelOS because the platform styles it, not because it renders the platform's
 * components.
 *
 * # This file composes and holds no screen
 *
 * ADR 0042. Each screen is a directory of its own; what they share is `chrome/`
 * for the drawing and `roster/` for the single data seam.
 */

import type { Activate, HostApi, HostedModule } from "@hotelos/sdk";

import { el } from "./chrome/element";
import { recordedLeave } from "./roster/leave";
import { bar, switcher, type Operator, type Section } from "./chrome/bar";
import { stylesheet } from "./chrome/styles";
import { attendance } from "./screens/attendance";
import { ATTENDANCE_CSS } from "./screens/attendance/styles";
import { duty } from "./screens/duty";
import { DUTY_CSS } from "./screens/duty/styles";
import { leave } from "./screens/leave";
import { LEAVE_CSS } from "./screens/leave/styles";
import { people } from "./screens/people";
import { PEOPLE_CSS } from "./screens/people/styles";
import { policy } from "./screens/policy";
import { printed } from "./screens/printed";
import { PRINTED_CSS } from "./screens/printed/styles";
import { POLICY_CSS } from "./screens/policy/styles";
import { reports } from "./screens/reports";
import { REPORTS_CSS } from "./screens/reports/styles";
import { shifts } from "./screens/shifts";
import { teams } from "./screens/teams";
import { TEAMS_CSS } from "./screens/teams/styles";
import { rota } from "./screens/rota";
import { ROTA_CSS } from "./screens/rota/styles";
import { schedule } from "./screens/schedule";
import { SCHEDULE_CSS } from "./screens/schedule/styles";

/** What a screen needs to draw itself. */
interface Place {
  /** Which tab, for the one screen that has them. */
  tab: string;

  /** Whether the screen's dialog is open. */
  dialog: boolean;

  /**
   * Which one, for a screen that has more than one.
   *
   * A second boolean per dialog would make two-open-at-once expressible, and
   * the first state nobody drew is the one that ships.
   */
  which: string | null;

  /** Change the tab and redraw. */
  go: (tab: string) => void;

  /** Close whatever is open over the screen. */
  close: () => void;

  /** Open a state the rail cannot reach — the dialog, or the printed sheet. */
  open: (what: string) => void;

  /** Which rota cell is being filled, when one is. */
  pick: { person: string; day: number } | null;

  /** Open the picker on a cell. */
  onPick: (person: string, day: number) => void;

  /** Whose posting is being ended, when one is. */
  who: string | null;

  /** Which team the detail pane is open on, when one is. */
  team: string | null;

  /** Open one. */
  onTeam: (id: string) => void;

  /** Open the end-posting dialog on somebody. */
  onWho: (who: string) => void;

  /** Which page of the one list that has them. */
  page: number;

  /** Turn to a page of a list, 0-based. */
  onPage: (page: number) => void;
}

/** One view, and the screen it opens. */
interface View {
  label: string;
  draw: (host: HostApi, main: HTMLElement, place: Place) => void;
}

/**
 * Every destination, and the views inside it.
 *
 * **Both levels of navigation are derived from this**, so a destination without
 * a screen cannot be listed and a screen nothing reaches cannot be written. The
 * alternative — a list of bar items beside a chain of `if`s — needs a fallback
 * for the case the two disagree, and a fallback that quietly draws something
 * else is an unreachable state wearing a different hat.
 *
 * **Nine views, seven sections.** They do not fit a 56px bar, and the standard
 * was written from two applications with four sections each. Two sections carry
 * two views, which is the shape §3 itself sanctions rather than one invented
 * here: *the bar carries sections; a switcher within a section stays in the
 * body*. Rota is one question at two scopes, and Postings and Teams are both
 * who works here.
 */
const SECTIONS: readonly { label: string; views: readonly View[] }[] = [
  {
    label: "Rota",
    views: [
      {
        label: "Team rota",
        draw: (h, m, place) => void rota(
          h, m, () => place.open("print"), undefined,
          place.pick, place.onPick, place.close),
      },
      { label: "Staff schedule", draw: (h, m) => void schedule(h, m) },
    ],
  },
  {
    label: "Leave & Requests",
    views: [{
      label: "Leave & Requests",
      draw: (h, m, place) => void leave(
        h, m, place.tab, place.go, place.dialog, () => place.open("leave"), place.close),
    }],
  },
  { label: "Attendance", views: [{ label: "Attendance", draw: (h, m) => void attendance(h, m) }] },
  {
    label: "Duty",
    views: [{
      label: "Duty",
      draw: (h, m, place) => void duty(h, m, place.dialog, () => place.open("duty"), place.close),
    }],
  },
  {
    label: "People",
    views: [
      {
        label: "Postings",
        draw: (h, m, place) => void people(
          h, m, place.who, place.close, (who) => { place.onWho(who); },
          (chosen) => { place.onPage(chosen); }, place.page),
      },
      {
        label: "Teams",
        draw: (h, m, place) => void teams(
          h, m, {
            dialog: place.which, open: place.open, close: place.close,
            team: place.team, onTeam: place.onTeam,
          }),
      },
    ],
  },
  { label: "Reports", views: [{ label: "Reports", draw: (h, m) => void reports(h, m) }] },
  {
    label: "Policy",
    views: [{
      label: "Policy",
      draw: (h, m, place) => void policy(h, m, false, place.close, () => place.open("shift")),
    }],
  },
];

/** Who is signed in, drawn at the rail's foot. */
const OPERATOR: Operator = {
  name: "Priya Thomas",
  department: "Front Office",
  property: "Kochi Beach Resort",
  role: "Head of Front Office",
};

/**
 * Rendered by the host into the module's own document.
 *
 * @param host the bridge, and the only route out of this realm
 * @returns the module's mount and unmount
 */
export const activate: Activate = (host: HostApi): HostedModule => {
  let root: HTMLElement | null = null;

  // Held, not appended once. `show` replaces the root's children on every
  // screen change, so a stylesheet appended at mount is deleted by the first
  // render — the module then draws itself as an unstyled column, and neither
  // the type-check nor the suite can see it.
  const style = stylesheet([
    ROTA_CSS, LEAVE_CSS, ATTENDANCE_CSS, DUTY_CSS,
    PEOPLE_CSS, REPORTS_CSS, SCHEDULE_CSS, POLICY_CSS, PRINTED_CSS, TEAMS_CSS,
  ]);

  // Where the module is, at both levels. `view` is null until a section with
  // two of them is opened, and resolves to that section's first — so a section
  // cannot show a view belonging to another one, which is the state a single
  // `current` string made expressible.
  let current = "Rota";
  let view: string | null = null;
  let tab = "Requests";
  let dialog = false;
  let which: string | null = null;
  let detail: string | null = null;
  let pick: { person: string; day: number } | null = null;
  let who: string | null = null;
  let page = 0;
  let team: string | null = null;

  function show(next: string, chosen: string | null = null): void {
    if (root === null) return;

    const section = SECTIONS.find((entry) => entry.label === next);
    if (section === undefined) return;

    // A section change lands on its first view; a switcher click names one.
    // Resolved rather than remembered, so `view` can never hold a label the
    // current section does not offer.
    const wanted = next === current ? chosen ?? view : chosen;
    const screen = section.views.find((one) => one.label === wanted)
      ?? section.views[0]!;

    current = next;
    view = screen.label;

    const frame = el("div", "wf");
    const main = el("div", "main");

    // **Beside `main`, not inside it.** Every screen calls `replaceChildren` on
    // the element it is handed, so a switcher appended there is deleted by the
    // first render — the same trap the stylesheet hit, one element over.
    const strip = switcher(section.views, screen.label,
      (label) => { show(current, label); });

    frame.append(bar(sections(), current, OPERATOR, show));
    if (strip !== null) frame.append(strip);
    frame.append(main);

    root.replaceChildren(style, frame);

    if (detail === "Shifts") {
      void shifts(host, main, dialog, () => open("shift"),
        () => { dialog = false; detail = null; show(current); });
      return;
    }

    screen.draw(host, main, {
      tab,
      dialog,
      which,
      go: (chosen) => { tab = chosen; show(current); },
      close: () => {
        dialog = false;
        which = null;
        detail = null;
        pick = null;
        who = null;
        // The team stays selected. Cancelling *Add a member* returns to the
        // team it was opened from, not to the list — dismissing a dialog is
        // not a decision to leave the page behind it.
        show(current);
      },
      open,
      pick,
      onPick: (person, day) => { pick = { person, day }; show(current); },
      who,
      onWho: (person) => { who = person; show(current); },
      // Which page of the one list that has them. Held here rather than in the
      // screen, because a screen is redrawn from scratch on every change.
      page,
      onPage: (chosen) => { page = chosen; show(current); },
      team,
      // Clicking the open team closes it, which is the only way back to the
      // plain list: neither level of navigation reaches a state it has no
      // entry for.
      onTeam: (id) => { team = team === id ? null : id; show(current); },
    });
  }

  /**
   * Open a dialog, or the printed sheet, from outside the bar.
   *
   * The printed week is **not a screen**: it replaces the module's chrome
   * entirely, because it is a different artifact rather than a view of one.
   */
  function open(what: string): void {
    if (root === null) return;

    // Shifts is a place, not a dialog: the frame draws it as its own screen with
    // the bar still on Policy, and the New shift dialog opens over it.
    if (what === "shift") {
      dialog = true;
      detail = "Shifts";
      show("Policy");
      return;
    }

    if (what === "print") {
      // The sheet replaces the module's chrome, so the way back has to be
      // handed in — a preview a person cannot leave is worse than one whose
      // Print does not work yet.
      root.replaceChildren(style);
      void printed(host, root, () => { show(current); });
      return;
    }

    dialog = true;
    which = what;
    show(current);
  }

  return {
    mount(element) {
      root = element;
      show(current);
    },

    unmount() {
      root = null;
    },
  };
};

/**
 * The bar's sections.
 *
 * **The count is the queue's own length**, read from the same facts the screen
 * draws — not a literal beside it. A bar carrying its own number would
 * eventually disagree with the list it points at, and the bar is the one a
 * person believes. When a client lands, both come through the seam and this
 * stays true without being changed.
 */
function sections(): readonly Section[] {
  return SECTIONS.map((section) => section.label === "Leave & Requests"
    ? { label: section.label, count: String(recordedLeave.waiting.length) }
    : { label: section.label });
}

export default activate;

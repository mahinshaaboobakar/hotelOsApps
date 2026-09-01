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
import { rail, type Operator, type RailItem } from "./chrome/rail";
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
}

/**
 * Every destination, its glyph, and the screen it opens.
 *
 * **The rail is derived from this**, so a destination without a screen cannot
 * be listed and a screen nothing reaches cannot be written. The alternative — a
 * list of rail items beside a chain of `if`s — needs a fallback for the case
 * the two disagree, and a fallback that quietly draws something else is the
 * unreachable state this module just deleted, wearing a different hat.
 */
const SCREENS: readonly {
  label: string;
  glyph: string;
  draw: (host: HostApi, main: HTMLElement, place: Place) => void;
}[] = [
  { label: "Staff Schedule", glyph: "◫", draw: (h, m) => void schedule(h, m) },
  {
    label: "Team Rota", glyph: "▦",
    draw: (h, m, place) => void rota(
      h, m, () => place.open("print"), undefined,
      place.pick, place.onPick, place.close),
  },
  {
    label: "Leave & Requests", glyph: "◷",
    draw: (h, m, place) => void leave(
      h, m, place.tab, place.go, place.dialog, () => place.open("leave"), place.close),
  },
  { label: "Attendance", glyph: "◉", draw: (h, m) => void attendance(h, m) },
  {
    label: "Duty Register", glyph: "★",
    draw: (h, m, place) => void duty(h, m, place.dialog, () => place.open("duty"), place.close),
  },
  { label: "People", glyph: "◎", draw: (h, m) => void people(h, m) },
  { label: "Reports", glyph: "▤", draw: (h, m) => void reports(h, m) },
  {
    label: "Policy", glyph: "⚙",
    draw: (h, m, place) => void policy(h, m, false, place.close, () => place.open("shift")),
  },
];

/** Who is signed in, drawn at the rail's foot. */
const OPERATOR: Operator = {
  name: "Priya Thomas",
  where: "Front Office · Supervisor",
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
    PEOPLE_CSS, REPORTS_CSS, SCHEDULE_CSS, POLICY_CSS, PRINTED_CSS,
  ]);

  let current = "Team Rota";
  let tab = "Requests";
  let dialog = false;
  let detail: string | null = null;
  let pick: { person: string; day: number } | null = null;

  function show(next: string): void {
    if (root === null) return;

    const screen = SCREENS.find((entry) => entry.label === next);
    if (screen === undefined) return;

    current = next;

    const frame = el("div", "wf");
    const main = el("div", "main");

    frame.append(rail(items(), current, OPERATOR, show), main);
    root.replaceChildren(style, frame);

    if (detail === "Shifts") {
      void shifts(host, main, dialog, () => open("shift"),
        () => { dialog = false; detail = null; show(current); });
      return;
    }

    screen.draw(host, main, {
      tab,
      dialog,
      go: (chosen) => { tab = chosen; show(current); },
      close: () => { dialog = false; detail = null; pick = null; show(current); },
      open,
      pick,
      onPick: (person, day) => { pick = { person, day }; show(current); },
    });
  }

  /**
   * Open a dialog, or the printed sheet, from outside the rail.
   *
   * The printed week is **not a screen**: it replaces the module's chrome
   * entirely, because it is a different artifact rather than a view of one.
   */
  function open(what: string): void {
    if (root === null) return;

    // Shifts is a place, not a dialog: the frame draws it as its own screen with
    // the rail still on Policy, and the New shift dialog opens over it.
    if (what === "shift") {
      dialog = true;
      detail = "Shifts";
      show("Policy");
      return;
    }

    if (what === "print") {
      root.replaceChildren(style);
      void printed(host, root);
      return;
    }

    dialog = true;
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
 * The rail's entries.
 *
 * **The count is the queue's own length**, read from the same facts the screen
 * draws — not a literal beside it. A rail carrying its own number would
 * eventually disagree with the list it points at, and the rail is the one a
 * person believes. When a client lands, both come through the seam and this
 * stays true without being changed.
 */
function items(): readonly RailItem[] {
  return SCREENS.map((screen) => screen.label === "Leave & Requests"
    ? { label: screen.label, glyph: screen.glyph, count: String(recordedLeave.waiting.length) }
    : { label: screen.label, glyph: screen.glyph });
}

export default activate;

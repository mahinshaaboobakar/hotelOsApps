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
 * Every colour is a `var()` on the host's injected tokens, and only on names the
 * shell publishes (`SHELL-Q30`). An installed application looks like HotelOS
 * because the platform styles it, not because it renders the platform's
 * components.
 *
 * # This file composes and holds no screen
 *
 * ADR 0042. Each screen is a directory of its own; what they share is `chrome/`
 * for the drawing and `roster/` for the single data seam.
 */

import type { Activate, HostApi, HostedModule } from "@hotelos/sdk";

import { el } from "./chrome/element";
import { rail, type Operator, type RailItem } from "./chrome/rail";
import { stylesheet } from "./chrome/styles";
import { rota } from "./screens/rota";
import { ROTA_CSS } from "./screens/rota/styles";

/** The eight destinations the approved frames draw, in their order. */
const DESTINATIONS: readonly RailItem[] = [
  { label: "Staff Schedule", glyph: "◫" },
  { label: "Team Rota", glyph: "▦" },
  { label: "Leave & Requests", glyph: "◷", count: "3" },
  { label: "Attendance", glyph: "◉" },
  { label: "Duty Register", glyph: "★" },
  { label: "People", glyph: "◎" },
  { label: "Reports", glyph: "▤" },
  { label: "Policy", glyph: "⚙" },
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
  const style = stylesheet([ROTA_CSS]);

  let screen = "Team Rota";

  function show(next: string): void {
    if (root === null) return;
    screen = next;

    const frame = el("div", "wf");
    const main = el("div", "main");

    frame.append(rail(DESTINATIONS, screen, OPERATOR, show), main);
    root.replaceChildren(style, frame);

    if (screen === "Team Rota") {
      void rota(host, main);
      return;
    }

    main.replaceChildren(unbuilt(screen));
  }

  return {
    mount(element) {
      root = element;
      show(screen);
    },

    unmount() {
      root = null;
    },
  };
};

/**
 * A screen the approved design draws and this slice does not build.
 *
 * ADR 0124: it fails **in place** and names what it awaits, rather than showing
 * a blank panel a person has to interpret.
 */
function unbuilt(screen: string): HTMLElement {
  const head = el("div", "head");
  const title = el("div");

  title.append(
    el("div", "ht", screen),
    el("div", "hsub", "Drawn in the approved design; not built in this slice."),
  );

  head.append(title);
  return head;
}

export default activate;

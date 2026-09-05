/**
 * The five widgets, at the size the shell actually gives them.
 *
 * # Why a stack and not five pages
 *
 * The certificate wants each widget beside its approved frame, and the frames
 * were drawn as a canvas of cards. Rendering all five under one host, in one
 * load, means they are photographed under one device ratio and one stylesheet —
 * the same reason the measuring pass reuses its realms rather than reloading
 * per frame.
 *
 * # The size is the shell's constant, not a number typed here
 *
 * `WIDGET_SIZE` is 320 × 384 in `shell/widget-host/geometry.ts`, and the design
 * page's own arithmetic depends on it: *320 × 384 take 976px of a 1366 × 768
 * front-desk screen*. A harness that drew them larger would photograph content
 * the shell would have cut — **content that does not fit is cut by the widget,
 * not by the shell** — so a capture at the wrong size proves the opposite of
 * what it is taken for.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { stylesheet } from "../widgets/card";
import { attendanceToday } from "../widgets/panel/attendance-today";
import { comingUp } from "../widgets/panel/coming-up";
import { onLeave } from "../widgets/panel/on-leave";
import { pendingRequests } from "../widgets/panel/pending-requests";
import { shiftBoard } from "../widgets/panel/shift-board";

/** The shell's own frame — `apps/desktop/src/shell/widget-host/geometry.ts`. */
const WIDGET = { width: 320, height: 384 };

const PANELS: readonly { id: string; panel: (host: HostApi) => Promise<HTMLElement> }[] = [
  { id: "shift-board", panel: shiftBoard },
  { id: "attendance-today", panel: attendanceToday },
  { id: "pending-requests", panel: pendingRequests },
  { id: "coming-up", panel: comingUp },
  { id: "on-leave", panel: onLeave },
];

/**
 * The host a widget is handed.
 *
 * The same shape `frame.ts` builds for a screen, and refusing every call so the
 * cards draw their recorded facts: a capture pass that reached a live backend
 * would photograph whatever a property happened to hold that afternoon, and the
 * frames it is set beside were drawn against stated content.
 */
function host(): HostApi {
  return {
    identity: {
      id: "workforce",
      version: "0.1.0",
      capabilities: ["roster.read"],
    },
    property: { timezone: "Asia/Kolkata", locale: "en-IN" },
    // A `HostCallError`, not a plain one: `load` re-throws anything else, so a
    // bare Error takes the panel down instead of falling back — which is how
    // the first version of this harness drew a stylesheet and no cards.
    call: () => Promise.reject(new HostCallError({
      kind: "unavailable", message: "the capture pass serves recorded facts",
    })),
    on: () => () => {},
  };
}

async function draw(): Promise<void> {
  const stack = document.getElementById("stack")!;
  stack.append(stylesheet());

  for (const { id, panel } of PANELS) {
    const cell = document.createElement("div");
    cell.className = "cell";
    cell.dataset["widget"] = id;

    const frame = document.createElement("div");
    frame.className = "frame";
    frame.style.width = `${WIDGET.width}px`;
    frame.style.height = `${WIDGET.height}px`;

    const label = document.createElement("div");
    label.className = "label";
    label.textContent = id;

    frame.append(await panel(host()));
    cell.append(label, frame);
    stack.append(cell);
  }

  // Every card is mounted and drawn. The flag is what the capture waits on, and
  // it is set after the loop rather than inside it so a pass can never
  // photograph four cards and a gap.
  document.documentElement.setAttribute("data-ready", "true");
}

void draw();

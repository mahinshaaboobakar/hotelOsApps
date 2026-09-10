import type { HostApi } from "@hotelos/sdk";

import { blocked } from "../widgets/panel/blocked";
import { byPriority } from "../widgets/panel/by-priority";
import { dueSoon } from "../widgets/panel/due-soon";
import { raisedToday } from "../widgets/panel/raised-today";
import { theBoard } from "../widgets/panel/the-board";

/**
 * Which widget the capture harness can draw, by the id the manifest ships.
 *
 * **Its own module so a guard can read it rather than read about it.** The
 * first version of that guard checked whether `frame.ts` contained the widget's
 * name as a string, and reported `blocked` as unreachable — because the map
 * used shorthand property syntax and the text was never written. The widget was
 * perfectly drivable; the check was looking at source instead of at the thing.
 * That is GG's defect of this morning in a second costume: **a check that reads
 * text cannot see behaviour.**
 *
 * `jobs-now` is absent on purpose. It is reached by its three drawn states —
 * quiet, escalated, mine — which is the frame's own vocabulary, and the guard
 * knows that exception by name rather than by inference.
 */
export const PANELS: Record<string, (host: HostApi) => Promise<HTMLElement>> = {
  "the-board": theBoard,
  "blocked": blocked,
  "by-priority": byPriority,
  "due-soon": dueSoon,
  "raised-today": raisedToday,
};

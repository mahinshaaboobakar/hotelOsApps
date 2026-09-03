/**
 * Shift Board — the packaged entry, bundled to `ui/widgets/shift-board.js`.
 *
 * One entry per widget because esbuild writes one output per entry, and one
 * output per widget because the manifest declares each `file` separately and
 * the loader verifies each against its own digest.
 */

import { shiftBoard } from "../panel/shift-board";
import { serve } from "../serve";

serve(shiftBoard);

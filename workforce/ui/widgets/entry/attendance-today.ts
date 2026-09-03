/**
 * Attendance Today — the packaged entry, bundled to `ui/widgets/attendance-today.js`.
 *
 * One entry per widget because esbuild writes one output per entry, and one
 * output per widget because the manifest declares each `file` separately and
 * the loader verifies each against its own digest.
 */

import { attendanceToday } from "../panel/attendance-today";
import { serve } from "../serve";

serve(attendanceToday);

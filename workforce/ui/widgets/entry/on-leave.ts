/**
 * On Leave — the packaged entry, bundled to `ui/widgets/on-leave.js`.
 *
 * One entry per widget because esbuild writes one output per entry, and one
 * output per widget because the manifest declares each `file` separately and
 * the loader verifies each against its own digest.
 */

import { onLeave } from "../panel/on-leave";
import { serve } from "../serve";

serve(onLeave);

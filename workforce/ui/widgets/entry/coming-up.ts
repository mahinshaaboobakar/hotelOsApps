/**
 * Coming Up — the packaged entry, bundled to `ui/widgets/coming-up.js`.
 *
 * One entry per widget because esbuild writes one output per entry, and one
 * output per widget because the manifest declares each `file` separately and
 * the loader verifies each against its own digest.
 */

import { comingUp } from "../panel/coming-up";
import { serve } from "../serve";

serve(comingUp);

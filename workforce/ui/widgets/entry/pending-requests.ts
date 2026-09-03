/**
 * Pending Requests — the packaged entry, bundled to `ui/widgets/pending-requests.js`.
 *
 * One entry per widget because esbuild writes one output per entry, and one
 * output per widget because the manifest declares each `file` separately and
 * the loader verifies each against its own digest.
 */

import { pendingRequests } from "../panel/pending-requests";
import { serve } from "../serve";

serve(pendingRequests);

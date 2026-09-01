/**
 * The capability names this module asks the host for.
 *
 * **Constants, so a rename is a compile-move.** The backend learned this the
 * expensive way: six permission ids were renamed under `F11`/`AUTHZ-Q26`, and
 * the constants carried every call site while the test literals made the change
 * deliberate a second time. The same split applies here — this file holds the
 * names, and `tests/` spells them out.
 *
 * They must match `workforce/manifest.yaml` exactly: the host grants what the
 * manifest requested, and a name that disagrees is a capability silently never
 * granted — which renders as a fallback banner rather than as an error.
 */

/** Read the rota, the register, balances, attendance and the numbers. */
export const ROSTER_READ = "roster.read";

/** Fill the rota, copy a week forward, exchange two shifts. */
export const ROSTER_PLAN = "roster.plan";

/** Approve or decline leave, and correct a balance. */
export const LEAVE_APPROVE = "leave.approve";

/** Record who turned up, when they arrived and when they left. */
export const ATTENDANCE_RECORD = "attendance.record";

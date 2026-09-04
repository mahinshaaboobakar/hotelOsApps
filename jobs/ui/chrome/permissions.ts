/**
 * The eight capabilities the manifest requests — design §4.1, ruled
 * 2026-09-04 — as the strings the seam asks the host about. A screen renders
 * what the grant allows; the host enforces.
 */

export const JOB_READ = "job.read";
export const JOB_CREATE = "job.create";
export const JOB_ASSIGN = "job.assign";
export const JOB_COMPLETE = "job.complete";
export const JOB_CANCEL = "job.cancel";
export const JOB_AMEND = "job.amend";
export const JOB_CONFIGURE = "job.configure";
export const JOB_CURATE = "job.curate";

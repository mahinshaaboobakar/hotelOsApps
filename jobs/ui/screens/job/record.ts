/**
 * The Record tab — frame 2g: the audit columns, the version, a person's own
 * reminders. The one tab most people never open, kept so nothing the record
 * holds is invisible.
 */

import { control, el, fill } from "../../chrome/element";
import type { JobDetail } from "../../board";
import { card } from "./overview";

export function record(d: JobDetail): HTMLElement {
  const identity = d.record.filter((x) => ["job_id", "Number", "Property", "Version"].includes(x.k));
  const audit = d.record.filter((x) => ["Created", "Updated", "Deleted"].includes(x.k));
  const reminders = fill(
    el("div", "card"),
    el("h3", undefined, "Reminders"),
    el("div", "mono", `Mine · ${d.record.find((x) => x.k === "Reminders")?.v ?? "none"}`),
    control("btn sm", "Remind me…"),
  );
  return fill(el("div", "cols3"), card("Identity", identity), card("Audit", audit), reminders);
}

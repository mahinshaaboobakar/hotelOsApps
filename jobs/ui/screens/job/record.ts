/**
 * The Record tab — frame 2g: the audit columns, the version, a person's own
 * reminders. The one tab most people never open, kept so nothing the record
 * holds is invisible.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { when } from "../../chrome/instant";
import type { JobDetail } from "../../board";
import { card } from "./overview";

/** The two instants the backend sends as ISO, rendered in the property's form (§11); a dash stays a dash. */
const INSTANTS = ["Created", "Updated"];

export function record(host: HostApi, d: JobDetail): HTMLElement {
  // Owner, 2026-09-19 (c): the job number and the property's name, never raw ids.
  const identity = d.record.filter((x) => ["Number", "Property", "Version"].includes(x.k));
  const audit = d.record
    .filter((x) => ["Created", "Updated", "Deleted"].includes(x.k))
    .map((x) => (INSTANTS.includes(x.k) && x.v !== "—" ? { ...x, v: when(host, x.v) } : x));
  const reminders = fill(
    el("div", "card"),
    el("h3", undefined, "Reminders"),
    el("div", "mono", `Mine · ${d.record.find((x) => x.k === "Reminders")?.v ?? "none"}`),
    control("btn sm", "Remind me…"),
  );
  return fill(el("div", "cols3"), card("Identity", identity), card("Audit", audit), reminders);
}

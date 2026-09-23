/**
 * The Record tab — frame 2g: the audit columns, the version, a person's own
 * reminders. The one tab most people never open, kept so nothing the record
 * holds is invisible.
 */

import type { HostApi } from "@hotelos/sdk";

import { el, fill, off } from "../../chrome/element";
import { when } from "../../chrome/instant";
import type { Detail, JobDetail } from "../../board";
import { card } from "./overview";

/** The two instants the backend sends as ISO, rendered in the property's form (§11); a dash stays a dash. */
const INSTANTS = ["Created", "Updated"];

/**
 * Who did it, beside when — ADR 0225 §2 (`JOBS-Q4`, owner, 2026-09-22), as frame
 * 2g draws it: *Created 02 Sept, 13:31 · the guest of stay 7F2A* (en-GB).
 *
 * The service sends the name as its own value (`Created by`, `Updated by`) and
 * this writes the line, because a server that composed the sentence would fix
 * its separator and its order for every property. Where nobody is named the
 * value is already words — "no name on record", or what raised it.
 */
function withWho(rows: readonly Detail[], host: HostApi): Detail[] {
  return rows
    .filter((row) => INSTANTS.includes(row.k) || row.k === "Deleted")
    .map((row) => {
      const when_ = INSTANTS.includes(row.k) && row.v !== "—" ? when(host, row.v) : row.v;
      const who = rows.find((other) => other.k === `${row.k} by`)?.v;
      return { ...row, v: who === undefined || row.v === "—" ? when_ : `${when_} · ${who}` };
    });
}

export function record(host: HostApi, d: JobDetail): HTMLElement {
  // Owner, 2026-09-19 (c): the job number and the property's name, never raw ids.
  const identity = d.record.filter((x) => ["Number", "Property", "Version"].includes(x.k));
  const audit = withWho(d.record, host);
  const reminders = fill(
    el("div", "card"),
    el("h3", undefined, "Reminders"),
    el("div", "mono", `Mine · ${d.record.find((x) => x.k === "Reminders")?.v ?? "none"}`),
    // The backend answers `remind`; choosing when needs a form no frame draws.
    off("btn sm", "Remind me…", "setting a reminder isn't available here yet"),
  );
  return fill(el("div", "cols3"), card("Identity", identity), card("Audit", audit), reminders);
}

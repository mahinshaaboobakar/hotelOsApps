/**
 * "Jobs now" — the dock widget of mockup 01 frame 9, in its three states:
 * quiet, escalated, and mine. It reads the same sweep output the Live tab
 * does, and shows the viewer what their access shows them.
 */

import { load, type HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { failure } from "../../chrome/failure";
import { JOB_READ } from "../../chrome/permissions";
import { type JobsNow } from "../../board";
import { card, figures, openRow } from "../card";

export async function jobsNow(host: HostApi): Promise<HTMLElement> {
  const got = await load<JobsNow>(host, JOB_READ, "jobsNow");

  // **A widget shows its property's figures or says why it cannot.**
  // There is no recorded argument to fall back to any more, which is the
  // point: the seam makes the old behaviour unwriteable rather than
  // forbidden (owner, 2026-09-09).
  if (!got.ok) return card("Jobs Now", "this property", [failure(got.failure, "your department's jobs")]);

  const now = got.value;
  const quiet = now.breached === 0 && now.stuck === 0 && now.atRisk === 0;

  const body: (Node | null)[] = [
    figures([
      { value: String(now.open), label: "open", tone: "" },
      { value: String(now.running), label: "running", tone: "run" },
      quiet
        ? { value: "ON TRACK", label: "concern", tone: "ok" }
        : { value: `${String(now.breached)} · ${String(now.stuck)}`, label: "breached · stuck", tone: "bad" },
    ]),
  ];

  if (quiet) body.push(el("div", "wquiet", "Nothing at risk. The sweep last ran a minute ago."));
  for (const worst of now.worst) body.push(openRow(host, worst.number, worst.line, worst.tone, `jobs:${worst.number}`));
  if (now.unreadNudges > 0) body.push(openRow(host, "Nudges", `${String(now.unreadNudges)} unread`, "warn", "jobs:nudges"));

  return card("Jobs now", now.scope, body);
}

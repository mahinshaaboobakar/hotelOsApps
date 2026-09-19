/**
 * "Blocked" — what is waiting, and whose clock is running while it waits. Z's
 * canvas frame `JobsBlocked`, owner-approved 2026-09-03; built 2026-09-05.
 *
 * Two states and not one, because the difference is the point: a job ON_HOLD
 * has its concern clock stopped, and a paused session does not — the job's
 * clock keeps running while the person is away from it.
 */

import { formatNumber, load, type HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { failedCard } from "../failed";
import { JOB_READ } from "../../chrome/permissions";
import { type BlockedNow } from "../../board";
import { card, figures, openRow } from "../card";

export async function blocked(host: HostApi): Promise<HTMLElement> {
  const got = await load<BlockedNow>(host, JOB_READ, "widgetBlocked");

  // **A widget shows its property's figures or says why it cannot.**
  // There is no recorded argument to fall back to any more, which is the
  // point: the seam makes the old behaviour unwriteable rather than
  // forbidden (owner, 2026-09-09).
  if (!got.ok) return failedCard(host, "Blocked", "this property", got.failure, "the blocked list", () => blocked(host));

  const now = got.value;

  const body: (Node | null)[] = [
    figures([
      { value: formatNumber(now.onHold, host.property), label: "on hold", tone: "warn" },
      { value: formatNumber(now.pausedCount, host.property), label: "paused", tone: "run" },
    ]),
  ];

  if (now.held.length > 0) body.push(el("div", "wquiet", "On hold — the SLA clock is stopped"));
  for (const row of now.held) {
    body.push(openRow(host, row.number, `${row.what} · ${row.since}`, row.tone, `jobs:board?status=ON_HOLD&job=${row.number}`));
  }

  if (now.paused.length > 0) body.push(el("div", "wquiet", "Paused — the clock keeps running"));
  for (const row of now.paused) {
    body.push(openRow(host, row.number, `${row.what} · ${row.since}`, row.tone, `jobs:job?number=${row.number}`));
  }

  if (now.held.length === 0 && now.paused.length === 0) {
    body.push(el("div", "wquiet", "Nothing is waiting."));
  }

  body.push(el("div", "wrefusal", "Two states, because whose delay it is decides whose clock runs."));

  return card("Blocked", "this property", body);
}

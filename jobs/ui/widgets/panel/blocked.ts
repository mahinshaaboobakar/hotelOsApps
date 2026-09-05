/**
 * "Blocked" — what is waiting, and whose clock is running while it waits. Z's
 * canvas frame `JobsBlocked`, owner-approved 2026-09-03; built 2026-09-05.
 *
 * Two states and not one, because the difference is the point: a job ON_HOLD
 * has its concern clock stopped, and a paused session does not — the job's
 * clock keeps running while the person is away from it.
 */

import type { HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { JOB_READ } from "../../chrome/permissions";
import { load } from "../../board";
import { recordedBlockedNow } from "../../board/recorded/widgets-two";
import { card, figures, openRow } from "../card";

export async function blocked(host: HostApi): Promise<HTMLElement> {
  const got = await load(host, JOB_READ, "widgetBlocked", recordedBlockedNow);
  const now = got.value;

  const body: (Node | null)[] = [
    figures([
      { value: String(now.onHold), label: "on hold", tone: "warn" },
      { value: String(now.pausedCount), label: "paused", tone: "run" },
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

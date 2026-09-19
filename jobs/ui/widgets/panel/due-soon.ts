/**
 * "Due Soon" — what is already late, then what falls due within two hours. Z's
 * canvas frame `JobsDue`, amended 2026-09-05, corrected 2026-09-06 and
 * owner-approved 2026-09-08.
 *
 * Two groups and one clock. The frame used to split its rows by deadline
 * *source* — from the guest flow against from the service — and Jobs stores one
 * `due_at`; `priority_decided_by` records FLOW as the source of a **priority**,
 * never of a deadline. The split named a distinction the model does not carry.
 *
 * The correction that followed is why the order here is not arbitrary: the
 * amendment left the new headings sitting on the old groups, so for a day the
 * frame read "Overdue — furthest past due first" above two jobs due in an hour.
 * This follows the corrected drawing: late first, furthest past due at the top.
 */

import { formatNumber, load, type HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { failedCard } from "../failed";
import { JOB_READ } from "../../chrome/permissions";
import { type DueNow } from "../../board";
import { card, figures, openRow } from "../card";

export async function dueSoon(host: HostApi): Promise<HTMLElement> {
  const got = await load<DueNow>(host, JOB_READ, "widgetDue");

  // **A widget shows its property's figures or says why it cannot.**
  // There is no recorded argument to fall back to any more, which is the
  // point: the seam makes the old behaviour unwriteable rather than
  // forbidden (owner, 2026-09-09).
  if (!got.ok) return failedCard(host, "Due Soon", "this property", got.failure, "the due list", () => dueSoon(host));

  const now = got.value;

  const body: (Node | null)[] = [
    figures([
      { value: formatNumber(now.overdue, host.property), label: "overdue", tone: "bad" },
      { value: formatNumber(now.dueWithinTwoHours, host.property), label: "due within 2h", tone: "warn" },
    ]),
  ];

  if (now.late.length > 0) body.push(el("div", "wquiet", "Overdue — furthest past due first"));
  for (const row of now.late) {
    body.push(openRow(host, row.number, `${row.allowance} · ${row.mark}`, row.tone, `jobs:board?due=overdue&job=${row.number}`));
  }

  if (now.soon.length > 0) body.push(el("div", "wquiet", "Due within two hours"));
  for (const row of now.soon) {
    body.push(openRow(host, row.number, `${row.allowance} · ${row.mark}`, row.tone, `jobs:board?due=soon&job=${row.number}`));
  }

  if (now.late.length === 0 && now.soon.length === 0) {
    body.push(el("div", "wquiet", "Nothing is late and nothing is close."));
  }

  body.push(el("div", "wrefusal", "One due_at per job, from the policy chain. A job with no deadline is in neither figure."));

  return card("Due Soon", "this property", body);
}

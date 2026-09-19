/**
 * "By Priority" — where the pressure is, and how much nobody has judged yet.
 * Z's canvas frame `JobsPriority`, amended 2026-09-05 and owner-approved
 * 2026-09-08.
 *
 * The frame drew emergency · high · normal · low until the amendment. This
 * design rules `P1 · P2 · P3 · NOT_TRIAGED`, and translating quietly would have
 * put two vocabularies in one product — a person reading "emergency" here and
 * "P1" on the board would reasonably think they were different things.
 *
 * `NOT_TRIAGED` is a figure of its own, never folded into P3: an unjudged job is
 * not a low-priority one, and the untriaged queue is exactly what this widget
 * exists to surface.
 */

import { formatNumber, load, type HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { failedCard } from "../failed";
import { JOB_READ } from "../../chrome/permissions";
import { type PriorityNow } from "../../board";
import { card, figures, openRow } from "../card";

export async function byPriority(host: HostApi): Promise<HTMLElement> {
  const got = await load<PriorityNow>(host, JOB_READ, "widgetPriority");

  // **A widget shows its property's figures or says why it cannot.**
  // There is no recorded argument to fall back to any more, which is the
  // point: the seam makes the old behaviour unwriteable rather than
  // forbidden (owner, 2026-09-09).
  if (!got.ok) return failedCard(host, "By Priority", "this property", got.failure, "the priority count", () => byPriority(host));

  const now = got.value;

  const body: (Node | null)[] = [
    figures([
      { value: formatNumber(now.p1, host.property), label: "P1", tone: "bad" },
      { value: formatNumber(now.p2, host.property), label: "P2", tone: "warn" },
      { value: formatNumber(now.p3, host.property), label: "P3", tone: "run" },
      { value: formatNumber(now.notTriaged, host.property), label: "not triaged", tone: "hold" },
    ]),
  ];

  if (now.pressing.length > 0) body.push(el("div", "wquiet", "P1 and P2 — longest open first"));
  for (const row of now.pressing) {
    body.push(openRow(
      host,
      row.number,
      `${row.what} · ${row.raised}`,
      row.priority === "P1" ? "bad" : "warn",
      `jobs:board?priority=${row.priority}&job=${row.number}`,
    ));
  }

  if (now.pressing.length === 0) body.push(el("div", "wquiet", "Nothing urgent is open."));

  body.push(el("div", "wrefusal", "Not triaged is counted apart from P3 — nobody has judged those yet."));

  return card("By Priority", "this property", body);
}

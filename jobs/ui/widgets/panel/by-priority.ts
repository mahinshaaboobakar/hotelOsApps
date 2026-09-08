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

import type { HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { JOB_READ } from "../../chrome/permissions";
import { load } from "../../board";
import { recordedPriorityNow } from "../../board/recorded/widgets-three";
import { card, figures, openRow } from "../card";

export async function byPriority(host: HostApi): Promise<HTMLElement> {
  const got = await load(host, JOB_READ, "widgetPriority", recordedPriorityNow);
  const now = got.value;

  const body: (Node | null)[] = [
    figures([
      { value: String(now.p1), label: "P1", tone: "bad" },
      { value: String(now.p2), label: "P2", tone: "warn" },
      { value: String(now.p3), label: "P3", tone: "run" },
      { value: String(now.notTriaged), label: "not triaged", tone: "hold" },
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

/**
 * The History tab — frame 2c: status, concern and work rows read as one
 * timeline, each keeping its kind.
 */

import type { HostApi } from "@hotelos/sdk";

import { el, fill } from "../../chrome/element";
import { when } from "../../chrome/instant";
import type { JobDetail } from "../../board";

export function history(host: HostApi, d: JobDetail): HTMLElement {
  const t = el("table");
  const head = el("tr");
  for (const h of ["When", "Kind", "What", "By", "Detail"]) head.append(el("th", undefined, h));
  t.append(head);
  for (const line of d.history) {
    const tone = line.kind === "concern" ? (line.what === "BREACHED" || line.what === "STUCK" ? "bad" : "warn") : line.kind === "work" ? "run" : "";
    const tr = el("tr");
    tr.append(
      el("td", undefined, when(host, line.at)),
      fill(el("td"), el("span", `pill ${tone}`.trim(), line.kind)),
      el("td", undefined, line.what), el("td", undefined, line.by), el("td", undefined, line.detail),
    );
    t.append(tr);
  }
  return t;
}

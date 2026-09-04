/**
 * The Rating tab — frame 2f: the guest's stars and line once a guest-raised
 * job is closed; before that, "not yet — asked after close" (S10 D2).
 */

import type { HostApi } from "@hotelos/sdk";

import { el, fill } from "../../chrome/element";
import { when } from "../../chrome/instant";
import { stars } from "../../chrome/marks";
import type { JobDetail } from "../../board";

export function rating(host: HostApi, d: JobDetail): HTMLElement {
  if (d.rating === null) {
    return fill(el("div", "card"), el("h3", undefined, "The guest's rating"), el("div", "mono", "not yet — asked after close"));
  }

  const r = d.rating;
  const left = el("div", "card");
  left.append(
    el("h3", undefined, "The guest's rating"),
    stars(r.stars),
    el("div", undefined, `“${r.text}”`),
    el("div", "mono", `${d.whoAsked.find((x) => x.k === "Stay")?.v ?? ""} · rated ${when(host, r.ratedAt)} · via the guest app`),
  );

  const right = el("div", "card");
  right.append(el("h3", undefined, "How it was asked"));
  const kv = el("div", "kv");
  kv.style.gridTemplateColumns = "130px 1fr";
  kv.append(
    el("div", "k", "Asked"), el("div", undefined, `${when(host, r.askedAt)} · on auto-close · in the guest app`),
    el("div", "k", "Window"), el("div", undefined, `until ${r.windowUntil}`),
    el("div", "k", "Resolved by"), el("div", undefined, r.resolvedBy),
    el("div", "k", "Raised → resolved"), el("div", undefined, `${String(r.minutesRaisedToResolved)} min`),
  );
  right.append(kv);
  return fill(el("div", "cols"), left, right);
}

/**
 * A room's cards — the day in order, the decision Room Care made, inspection
 * today, and the jobs against the room (frames 4, 4b).
 */

import type { HostApi } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { clock, day } from "../../chrome/instant";
import { lower, service } from "../../chrome/words";
import type { RoomPage } from "./index";

export function cards(host: HostApi, page: RoomPage): HTMLElement {
  const grid = el("div", "cols");
  const left = el("div");
  const right = el("div");
  left.append(timeline(host, page), history(host, page));
  right.append(decision(page), inspection(host, page), jobs(host, page));
  right.querySelectorAll(".card").forEach((c) => ((c as HTMLElement).style.marginBottom = "14px"));
  grid.append(left, right);
  return grid;
}

function timeline(host: HostApi, page: RoomPage): HTMLElement {
  const card = el("section", "card");
  card.append(el("h3", undefined, "Today — every attempt, in order"));
  const tl = el("div", "tl");
  if (page.today.length === 0) tl.append(el("div", "ev dim", "nothing has happened to this room today"));
  for (const entry of page.today) {
    const ev = el("div", "ev");
    ev.append(el("b", undefined, `${clock(host, entry.at)} · ${lower(entry.kind)}`), el("span", undefined, [entry.what, entry.by].filter((x) => x !== null && x !== "").join(" · ")));
    tl.append(ev);
  }
  card.append(tl);
  return card;
}

function history(host: HostApi, page: RoomPage): HTMLElement {
  const card = el("section", "card");
  card.style.marginTop = "14px";
  card.append(el("h3", undefined, "History · 14 days"));
  if (page.history.length === 0) card.append(el("div", "dim", "no earlier days recorded"));
  for (const d of page.history) {
    const row = el("div", "row");
    row.append(el("span", "num", day(host, d.day)), el("span", undefined, d.services.map((s, i) => `${service(s)} — ${lower(d.outcomes[i] ?? "open")}`).join(" · ")));
    card.append(row);
  }
  return card;
}

function decision(page: RoomPage): HTMLElement {
  const card = el("section", "card");
  card.append(el("h3", undefined, "The decision Room Care made — recorded, not re-derived"));
  const d = page.decision;
  if (d === null) {
    card.append(el("div", "dim", "no service was decided for this room today"));
    return card;
  }
  const kv = el("div", "kv");
  const inputs = [d.condition, d.occupancy, d.stayStatuses.length > 0 ? `stays [${d.stayStatuses.map(lower).join(", ")}]` : null,
    d.soldAt !== null ? "sold tonight" : "not sold tonight", d.window !== null ? `${lower(d.window)} window` : null, `standard v${d.ruleVersion}`]
    .filter((x) => x !== null).map((x) => lower(x as string)).join(" · ");
  kv.append(
    el("div", "k", "Inputs"), el("div", undefined, inputs),
    el("div", "k", "Answer"), el("div", undefined, `${service(d.service)} · ${d.minutes} min · priority ${d.priority} · inspection: ${lower(d.inspectionRule)}`),
    el("div", "k", "Why"), el("div", undefined, d.reason ?? "—"),
    el("div", "k", "Decided by"), el("div", undefined, [lower(d.decidedBy), d.runBy].filter((x) => x !== null).join(" · ")),
  );
  card.append(kv);
  return card;
}

function inspection(host: HostApi, page: RoomPage): HTMLElement {
  const card = el("section", "card");
  card.append(el("h3", undefined, "Inspection — today"));
  const i = page.inspection;
  const kv = el("div", "kv");
  if (!i.applicationInstalled && i.requestedAt === null) {
    kv.append(el("div", "k", "Rule"), el("div", undefined, `${lower(i.rule)} — no inspection application at this property`));
  } else {
    kv.append(
      el("div", "k", "Rule"), el("div", undefined, lower(i.rule)),
      el("div", "k", "Requested"), el("div", undefined, i.requestedAt === null ? "not requested" : clock(host, i.requestedAt)),
      el("div", "k", "Answered"), el("div", undefined, i.answeredAt === null ? "—" : `${clock(host, i.answeredAt)} · ${lower(i.result ?? "")}${i.note === null ? "" : ` — ${i.note}`}`),
    );
  }
  kv.append(el("div", "k", "If it fails"), el("div", undefined, "the room goes back to dirty with the inspector's reason"));
  card.append(kv);
  return card;
}

function jobs(host: HostApi, page: RoomPage): HTMLElement {
  const card = el("section", "card");
  card.append(el("h3", undefined, "Jobs against this room today"));
  if (page.jobs.length === 0) card.append(el("div", "dim", "— none raised, none closed"));
  for (const job of page.jobs) {
    const label = job.kind === "CLOSED" ? `extra service ${clock(host, job.at)} · ${job.jobNumber ?? "a job"}` : `${job.jobNumber ?? "a job"} raised ${clock(host, job.at)} · ${job.by ?? ""}`;
    card.append(el("div", undefined, `${label}${job.summary === null ? "" : ` — ${job.summary}`}`));
  }
  return card;
}

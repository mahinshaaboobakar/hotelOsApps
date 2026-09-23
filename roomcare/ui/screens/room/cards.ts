/**
 * A room's tabs — Today (the disagreement, the day in order, the decision,
 * inspection, jobs), History · 14 days, and the Record (frames 4, 4b).
 */

import type { HostApi } from "@hotelos/sdk";

import { card } from "../../chrome/card";
import { el } from "../../chrome/element";
import { clock, day, when } from "../../chrome/instant";
import { whole } from "../../chrome/number";
import { lower, said, service, source } from "../../chrome/words";
import type { RoomPage } from "./index";

export function cards(host: HostApi, page: RoomPage, disagreement: HTMLElement | null): HTMLElement {
  const grid = el("div", "cols");
  const left = el("div");
  const right = el("div");
  if (disagreement !== null) left.append(disagreement);
  left.append(timeline(host, page));
  right.append(decision(host, page), inspection(host, page), jobs(host, page));
  for (const column of [left, right]) {
    column.querySelectorAll<HTMLElement>(".card").forEach((c, i) => { if (i > 0) c.style.marginTop = "14px"; });
  }
  grid.append(left, right);
  return grid;
}

/** What a history line is called — the transition's destination, as a person says it. */
function called(entry: RoomPage["today"][number]): string {
  switch (entry.kind) {
    case "TRANSITION":
      return ({ PLANNED: "Prepared", PENDING_POLICY: "Prepared · pending", ASSIGNED: "Assigned", IN_PROGRESS: "Started", ENDED: "Ended", CLOSED_BY_POLICY: "Closed by the window" } as Record<string, string>)[entry.status ?? ""] ?? "Changed";
    case "ENDED": return ({ DONE: "Done", PARTIAL: "Partial", SUPERVISOR_CLEANED: "Cleaned on the supervisor's word", SUPERVISOR_DND_APPROVED: "DND approved", NOT_REACHED: "Not reached" } as Record<string, string>)[entry.status ?? ""] ?? `Ended — ${lower(entry.status ?? "")}`;
    case "ATTEMPT": return `At the door — ${lower(entry.status ?? "")}`;
    case "OBSERVED": return "Observed";
    case "REDUCTION": return "Reduced for the guest";
    case "REPRIORITISED": return "Re-prioritised";
    case "SUPERVISOR_DECISION": return "The supervisor decided";
    case "EXTRA_TIME": return "Extra time asked";
    case "ISSUE": return "Issue found";
    default: return lower(entry.kind);
  }
}

function timeline(host: HostApi, page: RoomPage): HTMLElement {
  const tl = el("div", "tl");
  if (page.today.length === 0) tl.append(el("div", "ev dim", "nothing has happened to this room today"));
  for (const entry of page.today) {
    const ev = el("div", "ev");
    const detail = [entry.what, entry.kind === "OBSERVED" ? lower(entry.status ?? "") : null, entry.by].filter((x) => x !== null && x !== "").join(" · ");
    ev.append(el("b", undefined, `${clock(host, entry.at)} · ${called(entry)}`), el("span", undefined, detail));
    tl.append(ev);
  }
  return card("Today — every attempt, in order", tl);
}

export function history(host: HostApi, page: RoomPage): HTMLElement {
  const days = card("History · 14 days", page.history.length === 0 ? el("div", "dim", "no earlier days recorded") : null);
  for (const d of page.history) {
    const row = el("div", "row");
    row.append(el("span", "num", day(host, d.day)), el("span", undefined, d.services.map((s, i) => `${service(s)} — ${(d.outcomes[i] === undefined || d.outcomes[i] === null ? "no outcome recorded" : lower(d.outcomes[i]!))}`).join(" · ")));
    days.append(row);
  }
  return days;
}

export function record(host: HostApi, page: RoomPage): HTMLElement {
  const kv = el("div", "kv");
  kv.append(el("div", "k", "Condition"), el("div", undefined, `${said(page.line.condition)} · ${page.line.setBy ?? source(page.line.source)} · ${when(host, page.line.setAt)}`),
    el("div", "k", "Days without service"), el("div", undefined, whole(host, page.facts.daysWithoutService)),
    el("div", "k", "Supervisor's since"), el("div", undefined, page.facts.supervisedSince === null ? "—" : day(host, page.facts.supervisedSince)));
  const heard = page.today.filter((e) => e.kind === "OBSERVED").map((entry) => el("div", "mono", `${when(host, entry.at)} · ${entry.what} · ${lower(entry.status ?? "")}`));
  return card("The record — who set the condition, and every fact heard today", kv, ...heard);
}

function decision(host: HostApi, page: RoomPage): HTMLElement {
  const title = "The decision Room Care made";
  const d = page.decision;
  if (d === null) return card(title, el("div", "dim", "no service was decided for this room today"));
  const kv = el("div", "kv");
  const inputs = [d.condition, d.occupancy, d.stayStatuses.length > 0 ? `stays [${d.stayStatuses.map(lower).join(", ")}]` : null,
    d.soldAt !== null ? "sold tonight" : "not sold tonight", d.window !== null ? `${lower(d.window)} window` : null, `standard v${whole(host, d.ruleVersion)}`]
    .filter((x) => x !== null).map((x) => lower(x as string)).join(" · ");
  kv.append(
    el("div", "k", "Inputs"), el("div", undefined, inputs),
    el("div", "k", "Answer"), el("div", undefined, `${lower(d.service)} · ${whole(host, d.minutes)} min · priority ${whole(host, d.priority)} · inspection: ${lower(d.inspectionRule)}`),
    el("div", "k", "Decided by"), el("div", undefined, [lower(d.decidedBy), d.runBy].filter((x) => x !== null).join(" · ")),
  );
  return card(title, kv);
}

function inspection(host: HostApi, page: RoomPage): HTMLElement {
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
  return card("Inspection — today", kv);
}

function jobs(host: HostApi, page: RoomPage): HTMLElement {
  const tl = el("div", "tl");
  if (page.jobs.length === 0) tl.append(el("div", "ev dim", "— none raised, none closed"));
  for (const job of page.jobs) {
    const label = job.kind === "CLOSED" ? `extra service ${clock(host, job.at)} · ${job.jobNumber ?? "a job"}` : `${job.jobNumber ?? "a job"} raised ${clock(host, job.at)} · ${job.by ?? ""}`;
    tl.append(el("div", "ev", `${label}${job.summary === null ? "" : ` — ${job.summary}`}`));
  }
  return card("Jobs against this room today", tl);
}

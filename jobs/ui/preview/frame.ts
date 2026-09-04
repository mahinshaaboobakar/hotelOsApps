/**
 * The capture harness — one module realm, driven to a named screen so a frame
 * can be photographed beside the approved drawing.
 *
 * # It fakes the host and nothing else
 *
 * The identity, the property environment, the granted capabilities and the
 * answers to `host.call` are this file's. The module's own code, its
 * stylesheet and its token references are the shipped ones, so what appears
 * here is what a property would see.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { activate } from "../application";
import { recordedBoard, recordedToday } from "../board/recorded/board";
import { recordedCatalogue } from "../board/recorded/catalogue";
import { recordedJob, recordedRatedJob } from "../board/recorded/job";
import { recordedLive, recordedScheduled } from "../board/recorded/live";
import { recordedSettings } from "../board/recorded/settings";
import { recordedMe } from "../board/recorded/me";
import { recordedEscalated, recordedMine, recordedQuiet } from "../board/recorded/widget";
import { jobsNow } from "../widgets/panel/jobs-now";
import { stylesheet } from "../widgets/card";

const params = new URLSearchParams(location.search);

/** Marina Bay: 24-hour, day-month, Asia/Qatar — the frames' own form. */
const PROPERTY = { timezone: "Asia/Qatar", locale: "en-GB" };

const GRANTS = ["job.read", "job.create", "job.assign", "job.complete", "job.cancel", "job.amend", "job.configure", "job.curate"];

function host(granted: readonly string[], widget?: "quiet" | "escalated" | "mine"): HostApi {
  return {
    identity: { id: "jobs", version: "0.1.0", capabilities: granted },
    property: PROPERTY,
    call(capability: string, method: string): Promise<unknown> {
      const answers: Record<string, unknown> = {
        me: recordedMe,
        today: recordedToday,
        board: recordedBoard,
        job: params.get("job") === "rated"
          ? recordedRatedJob
          : params.get("granted") === "none"
            // A supervisor looking at somebody else's job: not the assignee, so
            // no work controls — the state the read-only pane exists to show.
            ? { ...recordedJob, row: { ...recordedJob.row, viewerIsAssignee: false } }
            : recordedJob,
        live: recordedLive,
        scheduled: recordedScheduled,
        catalogue: recordedCatalogue,
        settings: recordedSettings,
        jobsNow: widget === "quiet" ? recordedQuiet : widget === "mine" ? recordedMine : recordedEscalated,
      };
      const answer = answers[method];
      return answer === undefined
        ? Promise.reject(new HostCallError({ kind: "unavailable", message: `no answer for ${capability}/${method}` }))
        : Promise.resolve(answer);
    },
    on(): () => void {
      return () => {};
    },
  };
}

function click(selector: string, text: string): void {
  for (const node of Array.from(document.querySelectorAll<HTMLElement>(selector))) {
    if (node.textContent?.includes(text) === true) {
      node.click();
      return;
    }
  }
}

async function settle(): Promise<void> {
  await new Promise((done) => setTimeout(done, 0));
  await new Promise((done) => setTimeout(done, 0));
}

async function drive(): Promise<void> {
  const widget = params.get("widget");
  if (widget !== null) {
    const panel = await jobsNow(host(GRANTS, widget as "quiet" | "escalated" | "mine"));
    document.body.replaceChildren(stylesheet(), panel);
    document.documentElement.setAttribute("data-ready", "true");
    return;
  }

  const granted = params.get("granted") === "none" ? ["job.read"] : GRANTS;
  activate(host(granted)).mount(document.body);
  await settle();

  const screen = params.get("screen");
  if (screen !== null && screen !== "Board") { click(".tab", screen); await settle(); }

  // The states a top tab cannot reach are opened the way a person opens them —
  // by clicking the control the approved frame draws.
  const open = params.get("open");
  if (open === "raise") { click(".btn", "Raise a job"); await settle(); }
  if (open === "job" || open === "resolve") {
    click(".num", params.get("job") === "rated" ? "MRN-HK-388" : "MRN-ENG-142");
    await settle();
  }
  if (open === "resolve") { click(".btn", "Resolve…"); await settle(); }

  const tab = params.get("tab");
  if (tab !== null) { click(".tab", tab); await settle(); }

  // The policy flow is reached the way a person reaches it: the clock, then
  // the list, then New policy.
  const view = params.get("view");
  if (view !== null) {
    click(".btn", "All policies");
    await settle();
    if (view === "flow") { click(".btn", "＋ New policy"); await settle(); }
  }

  const step = params.get("step");
  if (step !== null) { click(".tab", step); await settle(); }

  // Timers, not `requestAnimationFrame`: a capture tab is often not the
  // foreground one, and rAF does not fire there — the flag would never land
  // while every screen rendered correctly.
  document.documentElement.setAttribute("data-ready", "true");
}

void drive();

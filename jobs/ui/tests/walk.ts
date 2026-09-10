/**
 * Walking the module the way a person does — shared by every conformance test.
 *
 * **One walker, because there was briefly going to be two.** The pagination
 * table needed a host, a settle and an "open this screen"; so did §2's control
 * rules, and a second copy would have drifted in exactly the half nobody reads:
 * which screens each one bothers to visit. The screen list lives here for the
 * same reason — a rule that walks fewer screens than its neighbour passes for a
 * reason that has nothing to do with the rule.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { recordedBoard, recordedToday } from "../board/recorded/board";
import { recordedCatalogue } from "../board/recorded/catalogue";
import { recordedJob } from "../board/recorded/job";
import { recordedLive, recordedScheduled } from "../board/recorded/live";
import { recordedSettings } from "../board/recorded/settings";

const ALL = ["job.read", "job.create", "job.assign", "job.complete", "job.cancel", "job.amend", "job.configure", "job.curate"];

export function host(): HostApi {
  const answers: Record<string, unknown> = {
    today: recordedToday, board: recordedBoard, job: recordedJob, live: recordedLive,
    scheduled: recordedScheduled, catalogue: recordedCatalogue, settings: recordedSettings,
  };

  return {
    identity: { id: "jobs", version: "0.1.0", capabilities: ALL },
    property: { timezone: "Asia/Qatar", locale: "en-GB" },
    call: (capability, method) => {
      const answer = answers[method];
      return answer === undefined
        ? Promise.reject(new HostCallError({ kind: "unavailable", message: `no answer for ${capability}/${method}` }))
        : Promise.resolve(answer);
    },
    on: () => () => {},
  };
}

export async function settle(): Promise<void> {
  await new Promise((done) => setTimeout(done, 0));
  await new Promise((done) => setTimeout(done, 0));
}

/** Open a screen the way a person does — by pressing what it says. */
export async function open(root: HTMLElement, steps: readonly string[]): Promise<void> {
  for (const step of steps) {
    if (step === "job") {
      root.querySelectorAll<HTMLElement>("tr.pick")[0]?.click();
    } else {
      const target = Array.from(root.querySelectorAll<HTMLElement>("button"))
        .find((button) => button.textContent?.startsWith(step) === true);
      if (target === undefined) throw new Error(`no control opens "${step}"`);
      target.click();
    }

    await settle();
  }
}


/** Every screen a conformance walk visits, in the order a person reaches them. */
export const SCREENS: { name: string; open: readonly string[] }[] = [
      { name: "Board", open: ["Board"] },
      { name: "Live", open: ["Live"] },
      { name: "Scheduled", open: ["Scheduled"] },
      { name: "Catalogue", open: ["Catalogue"] },
      { name: "Settings · Concern policy", open: ["Settings"] },
      { name: "Settings · Shifts & presence", open: ["Settings", "Shifts & presence"] },
      { name: "Settings · Who is told", open: ["Settings", "Who is told"] },
      { name: "Settings · Holds & reminders", open: ["Settings", "Holds & reminders"] },
      { name: "Settings · Closing & rating", open: ["Settings", "Closing & rating"] },
      { name: "Settings · Access", open: ["Settings", "Access"] },
      { name: "Settings · Policies", open: ["Settings", "All policies"] },
      { name: "One job · Overview", open: ["job"] },
      { name: "One job · Work", open: ["job", "Work"] },
      { name: "One job · History", open: ["job", "History"] },
      { name: "One job · Notes & photos", open: ["job", "Notes & photos"] },
      { name: "One job · Links & steps", open: ["job", "Links & steps"] },
      { name: "One job · Record", open: ["job", "Record"] },
    ];

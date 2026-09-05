/**
 * Resolve — frame 4: the item's resolutions as chips, the plain text box,
 * a photo, and what follows (auto-close, the guest's rating).
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { lines, saying, values } from "../../chrome/form";
import { elapsed } from "../../chrome/instant";
import { JOB_COMPLETE, JOB_READ } from "../../chrome/permissions";
import { standIn } from "../../chrome/standin";
import { act, load, type Catalogue, type JobDetail } from "../../board";
import { recordedCatalogue } from "../../board/recorded/catalogue";
import { recordedJob } from "../../board/recorded/job";

export async function resolve(
  host: HostApi,
  main: HTMLElement,
  jobId: string,
  onDone: () => void,
): Promise<void> {
  const got = await load(host, JOB_READ, "job", recordedJob, { id: jobId });
  const catalogue = await load(host, JOB_READ, "catalogue", recordedCatalogue);
  const job = got.value;
  const chosen = resolutions(catalogue.value, job);

  const body = el("div", "body");
  const said = saying();
  const words = el("div");
  words.append(lines("In your words · optional", "note", "What was actually done"));

  // Which chip is picked is the screen's own state — it is not a fact about
  // the property until Resolve is pressed, and a chip that redrew the whole
  // screen would lose what has been typed beside it.
  let picked: string | null = chosen[0]?.id ?? null;
  const chips = el("div", "chips");
  const draw = (): void => {
    chips.replaceChildren();
    for (const resolution of chosen) {
      chips.append(control(resolution.id === picked ? "chip on" : "chip", resolution.name, () => {
        picked = resolution.id;
        draw();
      }));
    }

    chips.append(control(picked === null ? "chip on" : "chip", "Other…", () => {
      picked = null;
      draw();
    }));
  };

  draw();

  const photo = control("btn", "Add a photo");
  photo.setAttribute("disabled", "true");
  photo.title = "photos wait for a media client";

  body.append(
    el("div", "sect", `Resolve ${job.row.number} · ${job.row.what}`),
    el("div", "mono", `work ${elapsed(job.totalWorkedSeconds)} across ${String(job.sessions.length)} sessions · stopping the clock now`),
    el("label", "lbl", "What fixed it"),
    chips,
    words,
    el("label", "lbl", "Photo · optional"),
    fill(el("div", "row"), photo),
    fill(
      el("div", "row"),
      control("btn pri", "Resolve", () => {
        const note = String(values(words).note ?? "");
        if (picked === null && note.length === 0) {
          said.say("say what fixed it, or pick one");
          return;
        }

        void act(host, JOB_COMPLETE, "resolve", {
          id: job.row.id,
          version: version(job),
          resolutionId: picked ?? undefined,
          note: note.length === 0 ? undefined : note,
        }).then((done) => {
          if (done.ok) onDone();
          else said.say(done.refused ?? "it was not resolved");
        });
      }),
      control("btn", "Back", onDone),
    ),
    said.line,
    el("div", "mono", job.row.raisedBy.startsWith("Guest")
      ? "Guest-raised: the guest will be asked to rate this after it closes. Auto-close follows the property's hours."
      : "Auto-close follows the property's hours unless it is reopened."),
  );

  if (!got.live) body.append(standIn("job", got.because));
  main.replaceChildren(body);
}

/** The resolutions this job's item offers, with the ids the service accepts. */
function resolutions(catalogue: Catalogue, job: JobDetail): readonly { id: string; name: string }[] {
  const item = catalogue.items.find((candidate) => job.row.what.endsWith(candidate.name)) ?? catalogue.items[0];
  return item?.resolutions ?? [];
}

/** The version this screen drew — the record tab's, so it is the stored one. */
function version(job: JobDetail): number {
  const held = job.record.find((line) => line.k === "Version");
  return held === undefined ? 0 : Number(held.v);
}

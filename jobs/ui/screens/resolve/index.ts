/**
 * Resolve — frame 4: the item's resolutions as chips, the plain text box,
 * a photo, and what follows (auto-close, the guest's rating).
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { elapsed } from "../../chrome/instant";
import { JOB_READ } from "../../chrome/permissions";
import { load } from "../../board";
import { recordedCatalogue } from "../../board/recorded/catalogue";
import { recordedJob } from "../../board/recorded/job";

export async function resolve(host: HostApi, main: HTMLElement, onDone: () => void): Promise<void> {
  const job = await load(host, JOB_READ, "job", recordedJob);
  const catalogue = await load(host, JOB_READ, "catalogue", recordedCatalogue);
  const item = catalogue.value.items.find((i) => job.value.row.what.endsWith(i.name)) ?? catalogue.value.items[0];

  const body = el("div", "body");
  body.append(
    el("div", "sect", `Resolve ${job.value.row.number} · ${job.value.row.what}`),
    el("div", "mono", `work ${elapsed(job.value.totalWorkedSeconds)} across ${String(job.value.sessions.length)} sessions · stopping the clock now`),
    el("label", "lbl", "What fixed it"),
    chips([...(item?.resolutions ?? []), "Other…"], "Refrigerant topped up"),
    el("label", "lbl", "In your words · optional"),
    el("div", "field", "Suction 45 psi, charged to 68. Recommend leak test at next PPM."),
    el("label", "lbl", "Photo · optional"),
    el("div", "field ph", "Add a photo"),
    fill(el("div", "row"), control("btn pri", "Resolve", onDone), control("btn", "Back", onDone)),
    el("div", "mono", "Guest-raised: the guest will be asked to rate this after it closes. Auto-close in 4 h unless reopened."),
  );
  main.replaceChildren(body);
}

function chips(names: readonly string[], chosen: string): HTMLElement {
  const row = el("div", "chips");
  for (const name of names) row.append(control(name === chosen ? "chip on" : "chip", name));
  return row;
}

/**
 * Raise a job — frame 3: where, what (from the catalogue), summary, details;
 * department and priority follow the item, the due time follows the policy,
 * and a day makes it scheduled instead of raised.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { choose, day, lines, saying, text, toggle, values } from "../../chrome/form";
import { JOB_CREATE, JOB_READ } from "../../chrome/permissions";
import { standIn } from "../../chrome/standin";
import { act, load, type Catalogue, type CatalogueItem } from "../../board";
import { recordedCatalogue } from "../../board/recorded/catalogue";

export async function raise(host: HostApi, main: HTMLElement, onDone: () => void): Promise<void> {
  const got = await load(host, JOB_READ, "catalogue", recordedCatalogue);
  const catalogue = got.value;
  const body = el("div", "body");
  const form = el("div", "cols");
  const said = saying();

  form.append(left(catalogue), right(catalogue));

  const actions = el("div", "row");
  actions.append(
    control("btn pri", "Raise it", () => {
      void raiseIt(host, values(form), said.say).then((ok) => {
        if (ok) onDone();
      });
    }),
    control("btn", "Cancel", onDone),
  );

  body.append(el("div", "sect", "Raise a job"), form, actions, said.line);
  if (!got.live) body.append(standIn("catalogue", got.because));
  main.replaceChildren(body);
}

/**
 * Send it, and keep the person on the form if the service says no.
 *
 * A refusal here is nearly always something they can fix — a summary they did
 * not write, a location the property does not have — so the form stays as it
 * is, with what they typed still in it.
 */
async function raiseIt(
  host: HostApi,
  held: Record<string, string | boolean>,
  say: (message: string, bad?: boolean) => void,
): Promise<boolean> {
  const summary = String(held.summary ?? "");
  const location = String(held.locationId ?? "");
  if (summary.length === 0) {
    say("a job needs one line saying what is wrong");
    return false;
  }

  if (location.length === 0) {
    say("a job needs a place");
    return false;
  }

  const scheduled = String(held.scheduledFor ?? "");
  const priority = String(held.priority ?? "");
  const done = await act(host, JOB_CREATE, "raise", {
    itemId: held.itemId,
    locationId: location,
    summary,
    details: held.details,
    priority: priority.length === 0 ? undefined : priority,
    restricted: held.restricted === true,
    scheduledFor: scheduled.length === 0 ? undefined : scheduled,
  });

  if (!done.ok) {
    say(done.refused ?? "the job was not raised");
    return false;
  }

  return true;
}

function left(catalogue: Catalogue): HTMLElement {
  return fill(
    el("div"),
    // Typed rather than picked: the picker is Master Data's location tree, and
    // no client reaches it from a module yet. Named as what it is rather than
    // drawn as a chooser that cannot choose.
    text("Where · location id", "locationId", "the location this is about"),
    choose("What", "itemId", catalogue.items.map((item) => ({ value: item.id, label: label(item) }))),
    text("Summary", "summary", "One line: what is wrong"),
    lines("Details · optional", "details", "Anything the technician should know first"),
  );
}

function right(catalogue: Catalogue): HTMLElement {
  const item = catalogue.items[0];
  return fill(
    el("div"),
    choose(
      "Priority",
      "priority",
      [
        { value: "", label: "From the catalogue item" },
        { value: "P1", label: "P1" },
        { value: "P2", label: "P2" },
        { value: "P3", label: "P3" },
      ],
      item === undefined ? undefined : `${item.name} defaults to ${item.defaultPriority}`,
    ),
    day("Schedule for a day · optional", "scheduledFor", "Empty raises it now; a day makes it SCHEDULED until then"),
    toggle("Restricted · only the department sees it", "restricted"),
    el("div", "hint mono", "The department, the due time and the concern policy all follow the item."),
  );
}

function label(item: CatalogueItem): string {
  const clock = item.dueWithinMinutes === null ? "no clock" : `${String(item.dueWithinMinutes)} min`;
  return `${item.department} › ${item.name} · ${item.defaultPriority} · ${clock}`;
}

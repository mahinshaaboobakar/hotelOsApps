/**
 * Raise a job — frame 3: where, what (from the catalogue), summary, details;
 * department and priority follow the item, the due time follows the policy,
 * and a day makes it scheduled instead of raised.
 *
 * **A full-screen composition, not a sheet — an approved exception.** Standard
 * §9 rules that a sheet composes and a dialog confirms; mockup 01 draws this as
 * a screen and was owner-locked 2026-09-04. Ruled `APPS-Q26`, 2026-09-09: the
 * locked drawing governs and §9 is **not** amended — the exception is Jobs' and
 * is labelled here rather than left to be tidied back by somebody reading the
 * standard alone.
 */

import type { HostApi } from "@hotelos/sdk";

import { control, el, fill } from "../../chrome/element";
import { choose, day, lines, saying, text, toggle, values } from "../../chrome/form";
import { when } from "../../chrome/instant";
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

  form.append(left(catalogue), right(host, catalogue));

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


/**
 * What follows from the item, shown rather than promised.
 *
 * The screen drew a hint — <em>“The department, the due time and the concern
 * policy all follow the item.”</em> — where frame 3 draws three values. The
 * sentence was true and insufficient: the catalogue read already carries the
 * department, the default priority and the item's allowance, so the screen
 * could say what they are and was telling the person to imagine them.
 *
 * **The due is the ITEM's allowance and is labelled as the item's.** The frame
 * annotates it <em>“policy: P3 within 60 min”</em>, and the policy chain —
 * item, then category, then department, then property — is resolved by the
 * service when the job is raised. A client that printed a policy result would
 * be attributing a resolution nobody ran, which is the invented-trace-id defect
 * wearing a due date. So this says what it knows and names its source.
 */
function follows(host: HostApi, item: CatalogueItem | undefined): HTMLElement {
  if (item === undefined) return el("div", "hint mono", "Choose an item and its department, priority and time appear here.");

  const due = item.dueWithinMinutes === null
    ? null
    : new Date(Date.now() + item.dueWithinMinutes * 60_000).toISOString();

  return fill(
    el("div"),
    shown("Department", item.department, "from the catalogue item"),
    shown("Priority", item.defaultPriority, "the item's default · you may override"),
    shown(
      "Due",
      due === null ? "no allowance on this item" : when(host, due),
      due === null
        ? "the item sets no time; the service's policy chain decides"
        : `the item allows ${String(item.dueWithinMinutes)} min · the service's policy chain decides the stored due`,
    ),
  );
}

/** A value the desk did not choose: drawn, never typed into — standard §10. */
function shown(label: string, value: string, hint: string): HTMLElement {
  return fill(
    el("div"),
    el("label", "lbl", label),
    el("div", "field", value),
    el("div", "hint mono", hint),
  );
}

function right(host: HostApi, catalogue: Catalogue): HTMLElement {
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
    follows(host, item),
    // The sentence this replaced is now three values — see `follows`.
  );
}

function label(item: CatalogueItem): string {
  const clock = item.dueWithinMinutes === null ? "no clock" : `${String(item.dueWithinMinutes)} min`;
  return `${item.department} › ${item.name} · ${item.defaultPriority} · ${clock}`;
}

/**
 * The Requests tab — ours always, Jobs' when Jobs is here. Frames 5 and 5b.
 */

import type { Request, Requests } from "../../book";
import { control, el, fill } from "../../chrome/element";
import { mark } from "../../chrome/marks";
import { card } from "../../chrome/panel";

/**
 * Draw the tab.
 *
 * **The request is ours; the work is not.** GuestOps records the guest's
 * request and announces it; Jobs creates the job and owns everything after that
 * — assignment, status, completion. GuestOps never calls Jobs, never stores a
 * job's status, and never assigns a person.
 *
 * **With Jobs absent the request is still recorded.** What disappears is the
 * raising, not the guest's complaint — the owner's ruling of 2026-08-31: *an
 * application's own flow is never gated on another application being installed;
 * an absent dependency loses its capability, never the flow.*
 *
 * @param requests what the guest asked for, and what became of it
 * @returns the tab's contents
 */
export function requestsTab(requests: Requests): readonly HTMLElement[] {
  const cols = el("div", "cols even");
  cols.append(ours(requests), neighbour(requests));

  return requests.jobsInstalled === false
    ? [cols, renamed()]
    : [cols];
}

/** What the guest asked for — always here, whatever else is installed. */
function ours(requests: Requests): HTMLElement {
  const { root, body } = card(
    "Guest requests",
    // The attribution is only worth making where there is another panel to
    // distinguish it from. Alone on the tab it would be a label on the only
    // thing there.
    requests.jobsInstalled === false ? undefined : "GuestOps owns these",
  );

  for (const request of requests.ours) {
    body.append(row(request));
  }

  if (requests.jobsInstalled !== false) {
    body.append(el(
      "div",
      "hint",
      "A request is a fact about the guest's stay and lives here whether or not "
        + "any work follows from it. Not every request is a job — a late "
        + "checkout is answered at the desk.",
    ));
  }

  body.append(control("btn sm", "＋ Log a request"));
  return root;
}

/** What Jobs made of them — or the invitation to install it. */
function neighbour(requests: Requests): HTMLElement {
  if (requests.jobsInstalled === false || requests.jobs === null) {
    return absent();
  }

  const { root, body } = card("Jobs from this stay");
  const heading = root.querySelector(".ch");

  // The attribution goes in the card's header because it is a claim about the
  // whole panel: none of it is ours, and none of it is stored here.
  heading?.append(fill(el("div", "grow"), mark({ mark: "other", text: "Jobs · via Context" })));

  for (const job of requests.jobs) {
    body.append(row(job));
  }

  const note = el("div", "note");
  note.append(
    el("b", undefined, "This panel is Jobs' data, not ours."),
    document.createTextNode(
      " GuestOps published the request; Jobs created the job and carries the "
        + "stay reference on it. What you see is resolved live — no job state is "
        + "stored in GuestOps, and no call is made into Jobs.",
    ),
  );

  body.append(note);
  return root;
}

/**
 * Where the neighbour would be — ADR 0106 §4's guided hierarchy.
 *
 * **The invitation names the application and where it comes from**, because
 * Software Center is how it arrives. What it does not do is imply the property
 * is missing something it needs: requests are recorded either way, and worked
 * however this property works today.
 */
function absent(): HTMLElement {
  const root = el("div", "card ghost");
  const empty = el("div", "empty");
  const text = el("p");

  text.append(
    document.createTextNode(
      "Requests are recorded here and worked however this property works today. "
        + "Install ",
    ),
    el("b", undefined, "Jobs"),
    document.createTextNode(
      " from Software Center to raise and track work from a guest's stay.",
    ),
  );

  empty.append(
    el("div", "ic", "＋"),
    el("b", undefined, "Jobs is not installed"),
    text,
  );

  root.append(empty);
  return root;
}

/** One request or job. */
function row(request: Request): HTMLElement {
  const element = el("div", "fr");
  const value = el("div", "v");

  value.append(document.createTextNode(request.what));

  if (request.state !== null) {
    value.append(el("span", `pill ${request.stateTone}`, request.state));
  }

  if (request.note !== null) {
    value.append(el("span", "hint", request.note));
  }

  element.append(el("div", "k", request.key), value);
  return element;
}

/** Why the tab is called something else when Jobs is away. */
function renamed(): HTMLElement {
  const note = el("div", "note");

  note.append(
    el("b", undefined, "The tab is renamed, not emptied."),
    document.createTextNode(
      " With Jobs absent the section is Requests; with Jobs present it is "
        + "Requests & jobs. Servicing is dimmed the same way when Room Care is "
        + "absent. Nothing about the guest's stay depends on another application "
        + "being installed.",
    ),
  );

  return note;
}

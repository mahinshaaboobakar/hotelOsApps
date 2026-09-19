/**
 * The Requests tab — ours always, Jobs' when Jobs is here. Frames 5 and 5b.
 */

import type { Request, Requests } from "../../book";
import { el, unavailable } from "../../chrome/element";
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
    // It carried "GuestOps owns these" beside Jobs' panel — which application
    // owns a record is the developer's distinction, not the desk's. Removed
    // under the owner's ruling of 2026-09-19: a mock's notes for the developer are never built as screen.
  );

  for (const request of requests.ours) {
    body.append(row(request));
  }

  const why = "Logging a request from GuestOps is not available yet.";
  body.append(unavailable("btn sm", "＋ Log a request", why), el("div", "hint", why));
  return root;
}

/** What Jobs made of them — or the invitation to install it. */
function neighbour(requests: Requests): HTMLElement {
  if (requests.jobsInstalled === false || requests.jobs === null) {
    return absent();
  }

  const { root, body } = card("Jobs from this stay");

  // "Jobs · via Context" in the header and a note on how the job was created,
  // resolved and stored were here — the data's route, for the developer.
  // Removed under the owner's ruling of 2026-09-19: a mock's notes for the developer are never built as screen.

  for (const job of requests.jobs) {
    body.append(row(job));
  }

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

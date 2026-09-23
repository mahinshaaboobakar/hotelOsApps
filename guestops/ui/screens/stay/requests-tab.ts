/**
 * The Requests tab — ours always, Jobs' when Jobs is here. Frames 5 and 5b.
 */

import type { PropertyEnvironment } from "@hotelos/sdk";

import type { Request, Requests } from "../../book";
import { control, el, unavailable } from "../../chrome/element";
import { card } from "../../chrome/panel";
import { instant } from "../../chrome/when";

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
 * @param property whose zone and locale the times are drawn in
 * @returns the tab's contents
 */
export function requestsTab(
  requests: Requests,
  property: PropertyEnvironment,
  log?: (text: string, handOff: boolean) => void,
): readonly HTMLElement[] {
  const cols = el("div", "cols even");
  cols.append(ours(requests, property, log), neighbour(requests, property));

  return requests.jobsInstalled === false
    ? [cols, renamed()]
    : [cols];
}

/** What the guest asked for — always here, whatever else is installed. */
function ours(
  requests: Requests,
  property: PropertyEnvironment,
  log?: (text: string, handOff: boolean) => void,
): HTMLElement {
  const { root, body } = card(
    "Guest requests",
    // It carried "GuestOps owns these" beside Jobs' panel — which application
    // owns a record is the developer's distinction, not the desk's. Removed
    // under the owner's ruling of 2026-09-19: a mock's notes for the developer are never built as screen.
  );

  for (const request of requests.ours) {
    body.append(row(request, property));
  }

  // **The control drew and did nothing until 2026-09-23** — the service has
  // recorded requests since it was written and the module served no method
  // that reached it. Gold frame 5.
  if (log === undefined) {
    const why = "Logging a request from GuestOps is not available yet.";
    body.append(unavailable("btn sm", "＋ Log a request", why), el("div", "hint", why));
    return root;
  }

  const text = document.createElement("input");
  text.type = "text";
  text.placeholder = "What the guest asked for";

  const box = el("div", "inp");
  box.append(text);

  // **The request is recorded whether or not Jobs is installed** — frame 5b:
  // "the request is still recorded; what disappears is the raising". So the
  // hand-off is offered only where there is something to hand off to, and
  // logging is offered always.
  const raise = el("div", "row");
  raise.append(
    control("btn sm", "Log", () => {
      if (text.value.trim() !== "") log(text.value.trim(), false);
    }),
    ...(requests.jobsInstalled === false ? [] : [control("btn sm pri", "Log and raise a job", () => {
      if (text.value.trim() !== "") log(text.value.trim(), true);
    })]),
  );

  body.append(box, raise);
  return root;
}

/** What Jobs made of them — or the invitation to install it. */
function neighbour(requests: Requests, property: PropertyEnvironment): HTMLElement {
  if (requests.jobsInstalled === false || requests.jobs === null) {
    return absent();
  }

  const { root, body } = card("Jobs from this stay");

  // "Jobs · via Context" in the header and a note on how the job was created,
  // resolved and stored were here — the data's route, for the developer.
  // Removed under the owner's ruling of 2026-09-19: a mock's notes for the developer are never built as screen.

  for (const job of requests.jobs) {
    body.append(row(job, property));
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
function row(request: Request, property: PropertyEnvironment): HTMLElement {
  const element = el("div", "fr");
  const value = el("div", "v");

  value.append(document.createTextNode(request.what));

  if (request.state !== null) {
    value.append(el("span", `pill ${request.stateTone}`, request.state));
  }

  if (request.note !== null) {
    value.append(el("span", "hint", request.note));
  }

  element.append(el("div", "k", request.key ?? instant(request.at, property, "time")), value);
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

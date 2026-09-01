/**
 * One module instance, in one realm.
 *
 * Each pane of the harness is a real `<iframe>`, because that is the shape the
 * module actually gets. It matters visually: the module sizes itself with
 * `100vh`, which is its realm's height in production and would be the whole
 * scrolling page if it were mounted into a plain `<div>` — the capture would
 * then show a layout no property will ever see.
 *
 * # It fakes the host and nothing else
 *
 * The identity, the granted capabilities and the answers to `host.call` are
 * this file's. The module's own code, its stylesheet and its token references
 * are the shipped ones, so what appears here is what a property would see.
 */

import type { HostApi } from "@hotelos/sdk";

import { activate } from "../module";
import { recordedOvertime, recordedWeek } from "../roster";
import { recordedFirstRun, recordedPeople } from "../roster/people";

const params = new URLSearchParams(location.search);

/** Which week the host answers with — the harness varies the DATA, not the module. */
const week = params.get("state") === "overtime" ? recordedOvertime : recordedWeek;

/**
 * A host that grants what the manifest requests and answers from the fixtures.
 *
 * `granted` is a parameter so the harness can show the refusal path too: the
 * module renders a stand-in note when a capability was not granted, and that
 * note is a design element the audit has to be able to see.
 */
function host(granted: readonly string[]): HostApi {
  return {
    identity: { id: "workforce", version: "0.1.0", capabilities: granted },

    call(capability: string, method: string): Promise<unknown> {
      if (method === "week") return Promise.resolve(week);

      // The first run is a data state, not a screen: the same People screen,
      // answered with a property that has posted nobody.
      if (method === "people") {
        return Promise.resolve(
          params.get("state") === "first-run" ? recordedFirstRun : recordedPeople);
      }

      // Everything else falls through to the module's own recorded facts, which
      // is the fallback path pane 12 exists to show.
      return Promise.reject(new Error(`unhandled ${capability}/${method}`));
    },

    on(): () => void {
      return () => {};
    },
  };
}

const granted = params.get("granted") === "none"
  ? []
  : ["roster.read", "roster.plan", "leave.approve", "attendance.record"];

activate(host(granted)).mount(document.body);

/** Click the first element matching `selector` whose text contains `text`. */
function click(selector: string, text: string): void {
  for (const node of Array.from(document.querySelectorAll<HTMLElement>(selector))) {
    if (node.textContent?.includes(text) === true) {
      node.click();
      return;
    }
  }
}

/**
 * Drive this realm to the screen it was asked for, then say so.
 *
 * The flag is what the capture waits on. A screenshot taken on a timer catches
 * a half-rendered screen often enough to be believed, and a loading state
 * photographs well.
 */
function drive(): void {
  const screen = params.get("screen");
  if (screen !== null && screen !== "Team Rota") click(".ri", screen);

  const tab = params.get("tab");
  if (tab !== null) click(".tab", tab);

  // The two states the rail cannot reach are opened the way a person opens
  // them — by clicking the button the approved frame draws.
  const open = params.get("open");
  if (open === "print") click(".btn", "Print");
  if (open === "shift") click(".btn", "New shift");

  requestAnimationFrame(() =>
    requestAnimationFrame(() =>
      setTimeout(() => document.documentElement.setAttribute("data-ready", "true"), 40)));
}

setTimeout(drive, 60);

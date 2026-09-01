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

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { activate } from "../module";
import { recordedOvertime, recordedWeek } from "../roster";
import { recordedDay } from "../roster/attendance";
import { recordedRegister } from "../roster/duty";
import { recordedLeave } from "../roster/leave";
import { recordedFirstRun, recordedPeople } from "../roster/people";
import { recordedPolicy } from "../roster/policy";
import { recordedMonth } from "../roster/reports";
import { recordedSchedule } from "../roster/schedule";

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

    /**
     * Answer every method a screen asks for.
     *
     * **Every one, and that is not thoroughness for its own sake.** The
     * module's seam rethrows anything that is not a `HostCallError`, so a
     * method this fake does not know leaves the screen blank rather than
     * falling back — eleven panes photographed empty before this was fixed.
     * The harness has to be as complete as the host it stands in for.
     */
    call(capability: string, method: string): Promise<unknown> {
      // The first run is a data state, not a screen: the same People screen,
      // answered with a property that has posted nobody.
      if (method === "people") {
        return Promise.resolve(
          params.get("state") === "first-run" ? recordedFirstRun : recordedPeople);
      }

      const answers: Record<string, unknown> = {
        week,
        leave: recordedLeave,
        day: recordedDay,
        register: recordedRegister,
        month: recordedMonth,
        schedule: recordedSchedule,
        policy: recordedPolicy,
      };

      const answer = answers[method];

      return answer === undefined
        ? Promise.reject(new HostCallError(
          { kind: "unavailable", message: `no answer for ${capability}/${method}` }))
        : Promise.resolve(answer);
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
/**
 * Drive this realm to the state it was asked for, then say so.
 *
 * # Each step waits for the one before it
 *
 * Every screen draws **asynchronously** — it awaits the seam before it renders
 * anything. So a click on a control that screen owns has to come after the
 * screen exists, not in the same turn as the click that opened it: the first
 * version clicked *Policy* and then *New shift* immediately, and the second
 * click found nothing because the header had not been drawn yet.
 */
async function drive(): Promise<void> {
  const settle = (): Promise<void> =>
    new Promise((resolve) => { setTimeout(resolve, 120); });

  await settle();

  const screen = params.get("screen");
  if (screen !== null && screen !== "Team Rota") { click(".ri", screen); await settle(); }

  const tab = params.get("tab");
  if (tab !== null) { click(".tab", tab); await settle(); }

  // The two states the rail cannot reach are opened the way a person opens
  // them — by clicking the button the approved frame draws.
  const open = params.get("open");
  if (open === "print") { click(".btn", "Print"); await settle(); }
  if (open === "shift") { click(".btn", "New shift"); await settle(); }

  // **Timers, not `requestAnimationFrame`.** A capture harness is driven in a
  // tab that is frequently not the foreground one, and rAF does not fire there —
  // the frame never paints, the callback never runs, and the flag never lands.
  // Every screen rendered correctly while `data-ready` stayed absent, which is
  // the worst shape for a signal: the thing it reports was fine and the signal
  // was not.
  document.documentElement.setAttribute("data-ready", "true");
}

void drive();

/**
 * Staff Schedule — one person's month, chosen from a picker.
 *
 * # A manager's screen, and the staff member's too
 *
 * The self-serve view is this same screen with the picker fixed to the
 * signed-in person. One surface, two audiences — which is why nothing here is
 * shaped around who is looking, and why the actions read *"Request leave"*
 * rather than *"Request leave for them"*.
 */

import { formatDay, formatInstant, type HostApi, load, type PropertyEnvironment,
  type Read }
  from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { failureScreen, unaskableScreen } from "../../chrome/failure";
import type { Operator } from "../../roster/model";
import { type Schedule, type ScheduleDay } from "../../roster/schedule";

const WEEKDAYS = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"] as const;

/**
 * Draw the screen.
 *
 * @param host the bridge
 * @param main the mount
 * @param me who is signed in, or the reason the platform could not say — `null`
 *   while that read is still in flight
 *
 * @remarks
 * **This read names a person, and the name has to come from somewhere.**
 * `schedule` takes a required `staffId`; the call used to send no body at all,
 * so against a real backend it could only ever answer *`'staffId'` is required*
 * — an `invalid` a person met as *Workforce could not build this person's
 * month*, three components from the omission. It was invisible here because the
 * harness answers from a fixture and never reaches the view that requires it.
 *
 * Until the picker this screen's own header promises is built, the person is
 * the signed-in one, so the id travels from the `me` read the bar already makes.
 */
export async function schedule(
  host: HostApi,
  main: HTMLElement,
  me: Read<Operator> | null,
): Promise<void> {
  // The `me` read has not returned. Not an empty screen and not a failure: the
  // module redraws when it lands, and drawing either of those first would put a
  // state on screen that is contradicted a moment later.
  if (me === null) return;

  // It did return, and the platform could not answer it. That failure is the
  // real one and it is reported as itself — this screen's inability to ask is a
  // consequence of it, not a second thing that went wrong.
  if (!me.ok) {
    failureScreen(main, "Rota", me.failure, { the: "this person's month" }, host.property,
      () => void schedule(host, main, me));
    return;
  }

  // Answered, and named nobody this application can post a month against. The
  // three reasons collapse on purpose one layer down — a caller with no user, a
  // login with no staff record, somebody no longer active — so this says what
  // is true of all three and does not guess which.
  if (me.value.staffId === null) {
    unaskableScreen(main, "Rota",
      "There is no staff record for the signed-in account",
      "A month is a person's, and this account is not yet one of this "
      + "property's people. Nothing is broken and nothing is missing from the "
      + "rota — somebody has to be given a staff record before there is a "
      + "month to show.");
    return;
  }

  const got = await load<Schedule>(
    host, ROSTER_READ, "schedule", { staffId: me.value.staffId });

  // No fallback - `APPS-Q26(4)`. A failed read renders the failure,
  // never a recorded list with an apology under it.
  if (!got.ok) {
    failureScreen(main, "Rota", got.failure, { the: "this person's month" }, host.property,
      () => void schedule(host, main, me));
    return;
  }

  const month = got.value;

  // **No legend here, and its absence is the finding.** This drew
  // `legend(recordedWeek.catalogue)` — "this property's shifts", from a
  // recorded fixture, on every render including a successful read. The month
  // this screen loads carries no catalogue, so there is no live source for it:
  // absent rather than invented (`APPS-Q26(4)`, and the gap rule it rests on).
  // Restoring it means a second read for the catalogue, which is a decision
  // rather than a repair, and it is reported instead of taken.
  const body = el("div", "body");
  body.append(
    figures(month, host.property),
    calendar(month, host.property));

  main.replaceChildren(header(month, host.property), body);
}

function header(month: Schedule, property: PropertyEnvironment): HTMLElement {
  const head = el("div", "tools");

  // No sub-line either: the picker below names the person, and a line above it
  // saying the same name is the duplication §3 removes, one level down.
  const picker = el("div", "sel");
  picker.append(
    el("span", "av", month.initials),
    el("span", undefined, month.who),
    el("i", undefined, "▾"),
  );

  const grow = el("div", "grow");
  head.append(picker, grow,
    // Formatted here. `month` is an ISO day now, and rendering it raw would
    // print "2026-08-01" — which the compiler cannot see, both being strings.
    el("div", "btn", `‹ ${formatDay(month.month, property, "month-year")} ›`),
    el("div", "btn", "⇄ Propose swap"),
    el("div", "btn pri", "＋ Request leave"));
  return head;
}

/**
 * The four figures — one strip, not four cards.
 *
 * The frame draws them as a single thin row of inline numbers with the balance
 * pushed to the right, and the difference is not decoration: four cards the
 * height of the month's first week push the grid down and make the month the
 * second thing on the screen. The month is what this screen is.
 */
function figures(month: Schedule, property: PropertyEnvironment): HTMLElement {
  const strip = el("div", "meta");

  const shifts = month.days.filter(
    (day) => day.tone !== null && day.tone !== "leave" && day.mark !== "OFF").length;
  const leaveDays = month.days.filter((day) => day.tone === "leave").length;

  strip.append(
    fig(String(shifts), "shifts"),
    fig(String(leaveDays), "days leave"),
    // The count is the service's; the sentence beside it is composed here, in
    // the property's clock. A service that wrote "1 MOD duty · Fri 28,
    // 20:00–08:00" would have chosen the reader's locale and hour cycle.
    fig(String(month.duty), duties(month, property)),
  );

  const balance = el("div", "mpush");
  const [figure, ...rest] = month.balance.split(" ");
  balance.append(el("i", undefined, `${figure} ${rest[0] ?? ""}`),
    el("span", undefined, rest.slice(1).join(" ")));

  strip.append(balance);
  return strip;
}

/** One figure and its label, inline. */
function fig(figure: string, label: string): HTMLElement {
  const item = el("div", "mfig");
  item.append(el("i", undefined, figure), el("span", undefined, label));
  return item;
}

/** The month grid, Monday first. */
function calendar(month: Schedule, property: PropertyEnvironment): HTMLElement {
  const grid = el("div", "cal");

  for (const day of WEEKDAYS) {
    grid.append(el("div", "rhd", day));
  }

  for (const day of month.days) {
    grid.append(cell(day, property));
  }

  return grid;
}

function cell(day: ScheduleDay, property: PropertyEnvironment): HTMLElement {
  const box = el("div", day.tone === null ? "cday out" : day.today === true ? "cday today" : "cday");

  box.append(el("s", undefined, day.date === null ? "" : String(day.date)));

  if (day.mark !== null && day.tone !== null) {
    box.append(el("b", `cm ${day.tone}`, day.mark));
  }

  // The duty rides on the day it is held, BENEATH the shift rather than instead
  // of it — MOD is property-wide and the person keeps their own posting — and it
  // prints its span, because a duty crossing midnight is the one whose hours a
  // person actually needs.
  if (day.dutyFrom !== undefined && day.dutyTo !== undefined) {
    box.append(el("div", "cduty",
      `MOD ${formatInstant(day.dutyFrom, property, "time")}`
      + `→${formatInstant(day.dutyTo, property, "time")}`));
  }

  // The next day carries the tail, quieter: the duty ends there.
  if (day.tail !== undefined) {
    box.append(el("div", "cduty tail", day.tail));
  }

  return box;
}

/**
 * "MOD duty · Fri, 28 Aug, 08:00 pm → 08:00 am", in the property's form.
 *
 * The SPAN, not just its start: a duty crossing midnight is the one whose end a
 * person needs, and the frame shows both. The end is drawn as a time alone
 * because the weekday beside it would repeat the day the span already names.
 */
function duties(month: Schedule, property: PropertyEnvironment): string {
  if (month.duty === 0 || month.dutyFrom === null || month.dutyTo === null) {
    return "MOD duty";
  }

  return `MOD duty · ${formatInstant(month.dutyFrom, property, "weekday-time")}`
    + ` → ${formatInstant(month.dutyTo, property, "time")}`;
}

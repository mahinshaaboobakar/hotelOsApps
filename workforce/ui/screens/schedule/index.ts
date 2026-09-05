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

import { formatInstant, type HostApi, type PropertyEnvironment } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { legend } from "../../chrome/legend";
import { standIn } from "../../chrome/standin";
import { load, recordedWeek } from "../../roster";
import { recordedSchedule, type Schedule, type ScheduleDay } from "../../roster/schedule";

const WEEKDAYS = ["Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun"] as const;

/** Draw the screen. */
export async function schedule(host: HostApi, main: HTMLElement): Promise<void> {
  const got = await load(host, ROSTER_READ, "schedule", recordedSchedule);
  const month = got.value;

  const body = el("div", "body");
  body.append(
    figures(month, host.property),
    calendar(month, host.property),
    legend(recordedWeek.catalogue));

  if (!got.live) {
    body.append(standIn("month", got.because));
  }

  main.replaceChildren(header(month), body);
}

function header(month: Schedule): HTMLElement {
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
    el("div", "btn", `‹ ${month.month} ›`),
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

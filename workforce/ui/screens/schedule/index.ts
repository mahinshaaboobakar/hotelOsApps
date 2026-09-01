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

import type { HostApi } from "@hotelos/sdk";

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
  body.append(figures(month), calendar(month), legend(recordedWeek.catalogue));

  if (!got.live) {
    body.append(standIn("month", got.because));
  }

  main.replaceChildren(header(month), body);
}

function header(month: Schedule): HTMLElement {
  const head = el("div", "head");
  const title = el("div");

  title.append(el("div", "ht", "Staff Schedule"));

  const picker = el("div", "sel");
  picker.append(
    el("span", "av", month.initials),
    el("span", undefined, month.who),
    el("i", undefined, "▾"),
  );

  const grow = el("div", "grow");
  head.append(title, picker, grow,
    el("div", "btn", `‹ ${month.month} ›`),
    el("div", "btn", "⇄ Propose swap"),
    el("div", "btn go", "＋ Request leave"));
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
function figures(month: Schedule): HTMLElement {
  const strip = el("div", "meta");

  const shifts = month.days.filter(
    (day) => day.tone !== null && day.tone !== "leave" && day.mark !== "OFF").length;
  const leaveDays = month.days.filter((day) => day.tone === "leave").length;

  strip.append(
    fig(String(shifts), "shifts"),
    fig(String(leaveDays), "days leave"),
    fig("1", month.duty),
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
function calendar(month: Schedule): HTMLElement {
  const grid = el("div", "cal");

  for (const day of WEEKDAYS) {
    grid.append(el("div", "rhd", day));
  }

  for (const day of month.days) {
    grid.append(cell(day));
  }

  return grid;
}

function cell(day: ScheduleDay): HTMLElement {
  const box = el("div", day.tone === null ? "cday out" : "cday");

  box.append(el("s", undefined, day.date === null ? "" : String(day.date)));

  if (day.mark !== null && day.tone !== null) {
    box.append(el("b", `cm ${day.tone}`, day.mark));
  }

  // The duty rides on the day it is held, beside the shift rather than instead
  // of it: MOD is property-wide and the person keeps their own posting.
  if (day.duty) {
    box.append(el("i", "cduty", "★"));
  }

  // Where a duty ran past midnight, the next day carries its end.
  if (day.tail !== undefined) {
    box.append(el("i", "ctail", day.tail));
  }

  return box;
}

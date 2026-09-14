/**
 * The Duty Register — two bands, because a duty crosses midnight.
 *
 * # Why the grid has two rows per day
 *
 * `WF-Q8`. The upper band is day duties; the lower is night duties, which run
 * into the next date. A single row per day would have to choose which date an
 * overnight duty belongs to, and both answers are wrong.
 *
 * # Now and next are the clock against the spans
 *
 * Never a stored flag, never a nightly job moving a marker — the same shape the
 * backend refused for the same reason.
 */

import { formatDay, formatInstant, type HostApi, load, type PropertyEnvironment }
  from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { failureScreen } from "../../chrome/failure";
import { assignDuty } from "./dialog";
import { type Duty, type Holder, type Register } from "../../roster/duty";

/**
 * The week, as one label.
 *
 * **Composed here because the dash is a word.** The service sent
 * `1 Sep – 7 Sep` already joined, so the separator, the order and the culture
 * of the month name all lived on the wire. It sends the seven days now; this
 * is the first and the last of them, and nothing else.
 *
 * @param days the week, ISO, as the read answered
 * @param property the property's zone and locale
 * @returns the range, or an empty string when the week is somehow empty
 */
function weekRange(
  days: readonly string[], property: PropertyEnvironment,
): string {
  const first = days[0];
  const last = days[days.length - 1];
  if (first === undefined || last === undefined) return "";

  return `${formatDay(first, property, "day-month-year")} – `
    + formatDay(last, property, "day-month-year");
}

/** Draw the screen. */
export async function duty(
  host: HostApi,
  main: HTMLElement,
  dialog = false,
  open: () => void = () => {},
  close: () => void = () => {},
): Promise<void> {
  const got = await load<Register>(host, ROSTER_READ, "register");

  // No fallback - `APPS-Q26(4)`. A failed read renders the failure,
  // never a recorded list with an apology under it.
  if (!got.ok) {
    failureScreen(main, "Duty", got.failure, { the: "the duty register" },
      () => void duty(host, main, dialog, open, close));
    return;
  }

  const register = got.value;

  const body = el("div", "body");
  body.append(nowNext(register, host.property), week(register, host.property), reading());

  main.replaceChildren(header(register, open, host.property), body);

  // The day the register is showing — its first — and the people it says
  // could hold a duty. Both come from the read; the dialog used to hold
  // a literal date and three names.
  if (dialog) {
    main.append(assignDuty(
      host, close, register.candidates, register.days[0], host.property,
      () => { close(); }));
  }
}

function header(
  register: Register, open: () => void, property: PropertyEnvironment,
): HTMLElement {
  const head = el("div", "tools");
  const title = el("div");

  // Property-wide, and the subtitle says so: the person keeps their own
  // department and posting — WF-Q1, MOD is a duty, not a role.
  title.append(
    el("div", "hsub",
      `Manager on Duty · property-wide · ${register.duties.filter((d) => d.who !== null).length} duties this week`),
  );

  const grow = el("div", "grow");
  head.append(title, grow,
    el("div", "btn", `‹ ${weekRange(register.days, property)} ›`),
    assign(open));
  return head;
}

/** The button that opens the dialog. */
function assign(open: () => void): HTMLElement {
  const button = el("div", "btn pri", "＋ Assign duty");
  button.addEventListener("click", open);
  return button;
}

/** The two lines a duty manager opens this screen for. */
function nowNext(register: Register, property: PropertyEnvironment): HTMLElement {
  const row = el("div", "nn");

  if (register.now !== null) {
    row.append(line("NOW", register.now.who, held(register.now, property), true));
  }

  if (register.next !== null) {
    row.append(line("Next", register.next.who, held(register.next, property), false));
  }

  return row;
}

/**
 * The sentence under a name — composed here, in the property's own clock.
 *
 * The service used to write "since 20:00 · ends 08:00 tomorrow", which put
 * three decisions where only the reader's property can make them: the clock,
 * the locale, and whether the end is *tomorrow* — and tomorrow is a different
 * day in two timezones.
 */
function held(holder: Holder, property: PropertyEnvironment): string {
  return `${formatInstant(holder.from, property, "weekday-time")}`
    + ` → ${formatInstant(holder.to, property, "weekday-time")}`;
}

function line(label: string, who: string, detail: string, live: boolean): HTMLElement {
  const card = el("div", live ? "nl on" : "nl");
  const text = el("div");

  text.append(el("b", undefined, who), el("s", undefined, detail));
  card.append(el("em", undefined, label), text);
  return card;
}

/** The week, two bands deep. */
function week(register: Register, property: PropertyEnvironment): HTMLElement {
  const grid = el("div", "dgrid");

  grid.append(el("div", "rhd", "This week"));
  for (const day of register.days) {
    // `weekday-day` — "Mon 24" — which is what a week strip wants and what
    // the service used to render as `ddd d` in its own culture.
    grid.append(el("div", "rhd", formatDay(day, property, "weekday-day")));
  }

  grid.append(el("div", "rlab", "★ Duty"));
  for (let day = 0; day < register.days.length; day += 1) {
    grid.append(cell(register.duties.filter((d) => d.day === day), property));
  }

  return grid;
}

/** One day's pair of bands. */
function cell(duties: readonly Duty[], property: PropertyEnvironment): HTMLElement {
  const stack = el("div", "dstack");

  for (const item of duties) {
    const band = el("div", item.who === null ? "dband none" : `dband ${item.band}`);

    band.append(
      el("b", undefined, item.who ?? "no MOD"),
      el("s", undefined, span(item, property)),
    );

    stack.append(band);
  }

  return stack;
}

/** The legend — a printed sheet has no hover, and neither does a glance. */
function reading(): HTMLElement {
  const panel = el("div", "panel");
  const note = el("div", "note");

  note.append(
    el("b", undefined, "Reading it. "),
    el("span", undefined,
      "Upper band, day duties. Lower band, night duties, which cross midnight. "
      + "Dashed means nobody holds the duty. MOD is property-wide — the person "
      + "keeps their own department and posting."),
  );

  panel.append(note);
  return panel;
}

/**
 * A band's hours, in the property's clock — or the em-dash for a band nobody
 * covers, which is a fact rather than a blank.
 */
function span(duty: Duty, property: PropertyEnvironment): string {
  return duty.from === null || duty.to === null
    ? "—"
    : `${formatInstant(duty.from, property, "time")}–${formatInstant(duty.to, property, "time")}`;
}

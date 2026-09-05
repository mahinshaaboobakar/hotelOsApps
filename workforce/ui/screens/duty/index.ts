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

import { formatInstant, type HostApi, type PropertyEnvironment } from "@hotelos/sdk";

import { el } from "../../chrome/element";
import { ROSTER_READ } from "../../chrome/permissions";
import { load } from "../../roster";
import { assignDuty } from "./dialog";
import { recordedRegister, type Duty, type Holder, type Register } from "../../roster/duty";

/** Draw the screen. */
export async function duty(
  host: HostApi,
  main: HTMLElement,
  dialog = false,
  open: () => void = () => {},
  close: () => void = () => {},
): Promise<void> {
  const got = await load(host, ROSTER_READ, "register", recordedRegister);
  const register = got.value;

  const body = el("div", "body");
  body.append(nowNext(register, host.property), week(register, host.property), reading());

  main.replaceChildren(header(register, open), body);

  if (dialog) main.append(assignDuty(close));
}

function header(register: Register, open: () => void): HTMLElement {
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
    el("div", "btn", `‹ ${register.week} ›`),
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
    grid.append(el("div", "rhd", day));
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

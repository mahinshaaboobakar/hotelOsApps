/**
 * The booking flow's first step — the dates and the party, taken from a person.
 *
 * **This is the fix for a refusal rather than a new feature.** New booking
 * asked for availability with no dates at all, because nothing on the screen
 * captured any, and the service refuses a query without them: the screen's own
 * failure card was the truth about it. The 05 flow the owner approved on
 * 2026-09-19 starts here.
 *
 * **Plain date fields rather than the drawn calendar, and that is a divergence
 * to see rather than to discover.** The approved frame draws a month grid; this
 * takes two native date inputs. What a person can express is the same, and the
 * date a browser produces is already ISO, which is what the service reads. The
 * calendar is presentation and can follow; withholding the whole flow until it
 * is drawn keeps a screen that cannot book anything.
 *
 * **The party is two counts, not one.** `NewStay` takes adults and children
 * separately, and ADR 0223's capacity warning compares their sum against what
 * the type sleeps — a single "guests" number would have to be split again by
 * whoever sends it, and the screen would be choosing where to put the children.
 */

import { control, el } from "../../chrome/element";

/** What the first step produces. */
export interface Query {
  readonly arrive: string;
  readonly depart: string;
  readonly adults: number;
  readonly children: number;
}

/** A sensible pair of dates for a screen nobody has typed into yet. */
export function opening(today: Date): Query {
  const iso = (days: number): string =>
    new Date(today.getTime() + days * 86_400_000).toISOString().slice(0, 10);

  return { arrive: iso(0), depart: iso(1), adults: 2, children: 0 };
}

/**
 * The query bar: two dates, two counts, and the control that runs the search.
 *
 * `check` is handed the query rather than reading the inputs itself, so nothing
 * downstream needs to know this step is a form.
 */
export function query(
  initial: Query,
  check: (query: Query) => void,
  label = "Check availability",
): HTMLElement {
  const arrive = date("Arrive", initial.arrive);
  const depart = date("Depart", initial.depart);
  const adults = count("Adults", initial.adults, 1);
  const children = count("Children", initial.children, 0);

  const bar = el("div", "fltr");

  bar.append(
    arrive.root,
    depart.root,
    adults.root,
    children.root,
    control("btn pri", label, () => check({
      arrive: arrive.input.value,
      depart: depart.input.value,
      adults: Number(adults.input.value),
      children: Number(children.input.value),
    })),
  );

  return bar;
}

/** Nights, where both ends are a day apart or more. */
export function nights(from: string, to: string): number {
  const days = (Date.parse(to) - Date.parse(from)) / 86_400_000;
  return Number.isFinite(days) && days > 0 ? days : 0;
}

interface Box {
  readonly root: HTMLElement;
  readonly input: HTMLInputElement;
}

function date(label: string, value: string): Box {
  const input = document.createElement("input");
  input.type = "date";
  input.value = value;

  return { root: wrap(label, input), input };
}

function count(label: string, value: number, least: number): Box {
  const input = document.createElement("input");
  input.type = "number";
  input.min = String(least);
  input.max = "9";
  input.value = String(value);

  return { root: wrap(label, input), input };
}

function wrap(label: string, input: HTMLInputElement): HTMLElement {
  const root = el("div", "inp");
  root.append(document.createTextNode(`${label} `), input);
  return root;
}

/**
 * Today — the front desk day. Gold frame 1.
 *
 * **The day is the business day**, not midnight to midnight: the header names
 * the date it rolls on and the hour it rolls at, because a guest checking in at
 * 01:00 belongs to the night before and a screen that said "today" without
 * saying which would be wrong for three hours every night.
 *
 * The stat strip is a **filter, not a summary** — the selected tile is outlined
 * and its list is the one below. Tabs carry the same counts, so the two agree
 * by construction rather than by two calls that could drift.
 */

import type { HostApi } from "@hotelos/sdk";

import { load, recordedToday, type DayRow, type Stat, type Today } from "../../book";
import { control, el, fill } from "../../chrome/element";
import { standIn } from "../../chrome/marks";
import { tabs } from "../../chrome/panel";
import { table } from "./table";

/**
 * Render the day.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 * @param list which tab is showing
 * @param go what to do when another list is chosen
 * @param open what to do when a stay is picked
 */
export async function today(
  host: HostApi,
  into: HTMLElement,
  list: string,
  go: (list: string) => void,
  open: (row: DayRow) => void,
): Promise<void> {
  const loaded = await load(host, "reservation.read", "today", recordedToday);
  const day = loaded.value;

  const showing = day.lists.find((one) => one.label === list) ?? day.lists[0];

  const body = el("div", "body");
  fill(
    body,
    loaded.live ? null : standIn(loaded.because),
    strip(day.stats, showing?.label ?? ""),
    tabs(day.lists.map((one) => ({ label: one.label, count: one.count })), showing?.label ?? "", go),
    table(showing?.rows ?? [], open),
  );

  into.replaceChildren(header(day), body);
}

/** Title, the business-day sentence, and the two ways to start a stay. */
function header(day: Today): HTMLElement {
  const head = el("div", "head");
  const title = el("div");

  const sub = el("div", "hsub");
  sub.append(
    document.createTextNode("Business day "),
    el("b", undefined, day.businessDate),
    document.createTextNode(
      ` · rolls at ${day.rollsAt} · `
      + (day.connected
        ? "PMS-connected — Opera writes the lifecycle"
        : "standalone — this property is the book"),
    ),
  );

  title.append(el("div", "ht", "Today"), sub);

  const acts = el("div", "grow");
  acts.append(control("btn2", "Walk-in"), control("create", "＋ New booking"));

  head.append(title, acts);
  return head;
}

/**
 * The stat strip.
 *
 * The selected tile is matched by label rather than by index, so the strip and
 * the tabs cannot disagree about which list is showing.
 */
function strip(stats: readonly Stat[], showing: string): HTMLElement {
  const element = el("div", "strip");

  for (const stat of stats) {
    // The label carries a sub-detail — "Arrivals · 6 unassigned" — so the tile
    // is matched on the word before it.
    const selected = stat.label.split(" · ")[0] === showing;
    const tile = el("div", selected ? "stat on" : "stat");

    tile.append(el("b", undefined, stat.value), el("span", undefined, stat.label));
    element.append(tile);
  }

  return element;
}

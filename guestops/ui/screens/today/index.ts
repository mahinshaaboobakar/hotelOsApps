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
import { pager } from "../../chrome/pager";
import { table } from "./table";

/**
 * How many rows a page of the day holds.
 *
 * The server clamps whatever it is sent, so this is a request rather than a
 * promise — but it is the number the pager divides the total by, so it has to
 * be the number that was actually asked for.
 */
const PAGE = 25;

/**
 * Render the day.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 * @param list which tab is showing
 * @param page which page of that list, 0-based
 * @param go what to do when another list is chosen
 * @param turn what to do when another page is chosen
 * @param open what to do when a stay is picked
 */
export async function today(
  host: HostApi,
  into: HTMLElement,
  list: string,
  page: number,
  go: (list: string) => void,
  turn: (page: number) => void,
  open: (row: DayRow) => void,
): Promise<void> {
  // The page travels as this application's own body — `{page, pageSize}` — and
  // comes back clamped by the same `Paging.Of` the gRPC surface uses, so the
  // module route and the wire cannot disagree about what page 0 means.
  const loaded = await load(host, "reservation.read", "today", recordedToday, {
    page,
    pageSize: PAGE,
  });
  const day = loaded.value;

  const showing = day.lists.find((one) => one.label === list) ?? day.lists[0];

  const views = tabs(
    day.lists.map((one) => ({ label: one.label, count: one.count })),
    showing?.label ?? "",
    go,
  );

  // The screen's actions sit at the right of the view switcher, where Jobs
  // floats "＋ Raise a job". With the section named in the bar there is no
  // page header left to put them in — docs/working/64 §3.
  views.append(
    el("div", "grow"),
    control("btn", "Walk-in"),
    control("btn pri", "＋ New booking"),
  );

  const body = el("div", "body");
  fill(
    body,
    loaded.live ? null : standIn(loaded.because),
    strip(day.stats, showing?.label ?? "", day),
    views,
    table(showing?.rows ?? [], open),
    pager(Number(showing?.count ?? 0), page, PAGE, turn),
  );

  // No page heading. It said "Today", which the bar already says — the same
  // word twice and a row of vertical space (§3). What it carried is not lost:
  // the business day moved into the strip, the actions onto the tabs.
  into.replaceChildren(body);
}

/**
 * The counts, and the day they describe.
 *
 * One thin bar rather than four cards — docs/working/64 §5. The cards cost
 * about 68px at the top of the screen, which is two guests' worth of rows,
 * and repeated the counts the tabs directly below already carry.
 *
 * The selected entry is matched by label rather than by index, so the strip
 * and the tabs cannot disagree about which list is showing.
 */
function strip(stats: readonly Stat[], showing: string, day: Today): HTMLElement {
  const element = el("div", "strip");

  for (const stat of stats) {
    // The label carries a sub-detail — "Arrivals · 6 unassigned" — so the entry
    // is matched on the word before it.
    const selected = stat.label.split(" · ")[0] === showing;
    const entry = el("span", selected ? "on" : undefined);

    entry.append(el("b", undefined, stat.value), document.createTextNode(stat.label));
    element.append(entry);
  }

  element.append(context(day));
  return element;
}

/** The business day, pushed right — Jobs' board carries its date here. */
function context(day: Today): HTMLElement {
  const ctx = el("span", "ctx");

  ctx.append(
    document.createTextNode("Business day "),
    el("b", undefined, day.businessDate),
    document.createTextNode(
      ` · rolls at ${day.rollsAt} · `
      + (day.connected
        ? "PMS-connected — Opera writes the lifecycle"
        : "standalone — this property is the book"),
    ),
  );

  return ctx;
}

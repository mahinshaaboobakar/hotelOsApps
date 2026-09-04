/**
 * One stay — the page the front desk works from. Gold frame 3.
 *
 * # What this page is claiming
 *
 * Every value shows **where it came from and how it was established**: a room
 * carries `yours` and `Opera: 208` at once, an arrival carries `OBSERVED` beside
 * the override that set it, a departure says it was `DERIVED FROM PROPERTY
 * CLOCK`. Every timestamp carries its basis (R12, R13) because a time without
 * one is a time somebody will later have to guess about.
 *
 * `FROM OPERA` is a **mark, not a lock** — it says who established the value,
 * and staff may still write it. PMS-connected has never meant read-only
 * (GUEST-Q1, as amended).
 *
 * A **preference carries to the next stay; a note dies with this one** — the
 * same distinction the schema draws, made visible where it is entered rather
 * than only in the model.
 *
 * This file composes; it holds no drawing of its own beyond the header. The
 * band is `banner.ts`, the timeline is `activity.ts`, and the panel's rows are
 * the shared `detail()`.
 */

import type { HostApi } from "@hotelos/sdk";

import { load, recordedStay, type StayPage } from "../../book";
import { control, el, fill } from "../../chrome/element";
import { mark, standIn } from "../../chrome/marks";
import { card, detail, tabs } from "../../chrome/panel";
import { banner } from "./banner";
import { timeline } from "./activity";

/**
 * Render the stay page.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 * @param tab which tab to show; tabs beyond Overview are honest empty states
 * @param go what to do when another tab is chosen
 */
export async function stay(
  host: HostApi,
  into: HTMLElement,
  tab: string,
  go: (tab: string) => void,
): Promise<void> {
  const loaded = await load(host, "reservation.read", "stay", recordedStay);
  const page = loaded.value;

  const body = el("div", "body");
  fill(body, tabs(page.tabs, tab, go), loaded.live ? null : standIn(loaded.because));

  if (tab === "Overview") {
    fill(
      body,
      page.banner === null ? null : banner(page.banner),
      overview(page),
    );
  } else {
    body.append(awaiting(tab));
  }

  into.replaceChildren(header(page), body);
}

/** Title, identifiers, who manages the stay, and the three actions. */
function header(page: StayPage): HTMLElement {
  const head = el("div", "head");
  const title = el("div");

  const room = page.room === null ? "" : ` · Room ${page.room}`;
  const sub = el("div", "hsub");
  sub.append(document.createTextNode(`Stay ${page.stayId} · booking ${page.bookingRef}`));

  // The chip says who owns the lifecycle — the whole PMS-connected/standalone
  // distinction, in one mark. Absent in a standalone property, where the
  // question does not arise.
  if (page.managedBy !== null) {
    sub.append(mark({ mark: "pms", text: page.managedBy }));
  }

  title.append(el("div", "ht", `${page.guest}${room}`), sub);

  const acts = el("div", "grow");
  for (const action of page.actions) {
    acts.append(control(action.danger ? "btn danger" : "btn", action.label));
  }

  head.append(title, acts);
  return head;
}

/** The two columns: the stay's own values, and what happened to it. */
function overview(page: StayPage): HTMLElement {
  const cols = el("div", "cols");

  const stayCard = card(
    "The stay",
    page.standing === null ? undefined : mark({ mark: "override", text: page.standing }),
  );

  for (const row of page.rows) {
    stayCard.body.append(detail(row));
  }

  const activity = card("What happened, in order", control("link", "Full activity →"));
  activity.body.append(timeline(page.timeline), el("div", "note", page.consequence));

  cols.append(stayCard.root, activity.root);
  return cols;
}

/**
 * A tab that exists and has nothing behind it yet.
 *
 * The bar is drawn in full because the design's information is *which tabs a
 * stay has* — Servicing saying `4 nights` tells the desk something before it is
 * opened. What is not built says so plainly rather than opening a blank page,
 * which is the same honesty the stand-in banner owes.
 */
function awaiting(tab: string): HTMLElement {
  const { root, body } = card(tab);

  body.append(
    el(
      "div",
      "hint",
      `${tab} is drawn in the approved design and is not built in this slice. `
      + "The tab is here because which tabs a stay has is itself information.",
    ),
  );

  return root;
}

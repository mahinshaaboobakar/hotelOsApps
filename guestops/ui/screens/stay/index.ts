/**
 * One stay — the page the front desk works from. Gold frames 3 to 7.
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
 * # Six tabs, and two of them depend on a neighbour
 *
 * Requests is renamed and Servicing is dimmed when Jobs or Room Care is absent.
 * **Neither is emptied**, because the owner's ruling of 2026-08-31 is that *an
 * application's own flow is never gated on another application being installed
 * — an absent dependency loses its capability, never the flow.* The guest's
 * request is GuestOps's own record either way.
 *
 * # This file composes and holds no tab
 *
 * ADR 0042. The header and the tab bar are here; every tab is its own file
 * beside this one, and the Overview's two halves are `banner.ts` and
 * `activity.ts`.
 */

import type { HostApi } from "@hotelos/sdk";

import {
  APP,
  failureDrawing,
  load,
  type Requests,
  type Servicing,
  type StayPage,
  type Tab,
} from "../../book";
import { control, el, fill } from "../../chrome/element";
import { cannot, mark, failed } from "../../chrome/marks";
import { card, detail, tabs } from "../../chrome/panel";
// `activityTab` and `paymentTab` are not imported: nothing calls them while
// the two tabs have no read, and the compiler saying so is the proof the
// fixtures are gone rather than merely unreferenced. Phase 3 brings both back
// with the call that feeds them.
import { banner } from "./banner";
import { requestsTab } from "./requests-tab";
import { servicingTab } from "./servicing-tab";
import { timeline } from "./activity";

/**
 * Render the stay page.
 *
 * @param host the bridge — the only route out of this realm
 * @param into the element this screen owns
 * @param stayId which stay — the id the row that opened this screen carried
 * @param tab which tab to show
 * @param go what to do when another tab is chosen
 */
export async function stay(
  host: HostApi,
  into: HTMLElement,
  stayId: string,
  tab: string,
  go: (tab: string) => void,
): Promise<void> {
  // **Reached without a stay, which is a defect rather than an empty state.**
  // Every route here comes from a row, and a row has an id; arriving without
  // one means something upstream dropped it, and drawing a stay would be
  // drawing somebody's.
  if (stayId === "") {
    into.replaceChildren(cannot(
      "No stay was chosen",
      "Open a stay from the day or from a booking. Nothing was asked of the "
      + "platform here, so there is no answer to report.",
    ));
    return;
  }

  const loaded = await load<StayPage>(
    host, "reservation.read", "stay", { stayId });

  if (!loaded.ok) {
    into.replaceChildren(
      failed(
        failureDrawing(loaded.failure, { app: APP, the: "this stay" }),
        () => void stay(host, into, stayId, tab, go),
      ));
    return;
  }

  const page = loaded.value;

  // **The tabs are read with the stay's id, which they always needed.** The
  // backend's `requests` has taken `Stay(request.Body)` since it was written
  // and would have refused every call this screen made — "this method needs a
  // stay" — the moment one reached the platform. It never did, because the
  // fallback answered first.
  const asked = await load<Requests>(
    host, "reservation.read", "requests", { stayId });

  const serviced = await load<Servicing>(
    host, "reservation.read", "servicing", { stayId });

  if (!asked.ok) {
    into.replaceChildren(
      failed(
        failureDrawing(asked.failure, { app: APP, the: "this stay's requests" }),
        () => void stay(host, into, stayId, tab, go),
      ));
    return;
  }

  if (!serviced.ok) {
    into.replaceChildren(
      failed(
        failureDrawing(serviced.failure, { app: APP, the: "this stay's servicing" }),
        () => void stay(host, into, stayId, tab, go),
      ));
    return;
  }

  const requests = asked.value;
  const servicing = serviced.value;

  const body = el("div", "body");

  fill(
    body,
    tabs(labelled(page.tabs, requests, servicing.roomCareInstalled), tab, go),
  );

  if (tab === "Overview") {
    fill(body, page.banner === null ? null : banner(page.banner), overview(page));
  } else if (tab.startsWith("Requests")) {
    fill(body, ...requestsTab(requests));
  } else if (tab === "Activity") {
    // **The fixture is gone and the call is Phase 3's.** `activity` is served
    // and queries `GuestOpsDbContext`; what is missing is the call, not the
    // data. Drawing `recordedActivity` here put one stay's invented history in
    // front of somebody reading another stay's page.
    fill(body, cannot(
      "This stay's activity has not been read",
      "The record exists and this screen has not asked for it yet. Nothing is "
      + "shown rather than somebody else's history.",
    ));
  } else if (tab === "Servicing") {
    fill(body, ...servicingTab(servicing));
  } else if (tab === "Payment") {
    // Same: `payment` is served and reads the property's own terms. A fixture
    // here is a folio — amounts, a deadline, a rate — belonging to nobody.
    fill(body, cannot(
      "This stay's payment has not been read",
      "The terms exist and this screen has not asked for them yet. Nothing is "
      + "shown rather than a folio that is not this guest's.",
    ));
  } else {
    body.append(awaiting(tab));
  }

  into.replaceChildren(header(page, tab, requests), body);
}

/**
 * The tab bar, with the two labels a neighbour changes.
 *
 * **Renamed and dimmed, never removed.** Which tabs a stay has is itself
 * information: a property looking at a dimmed Servicing learns that servicing
 * is something HotelOS can show them, which an absent tab would not.
 *
 * `jobsInstalled` and `roomCareInstalled` are three-valued, and `null` — nobody
 * established it — draws the **installed** variant. Guessing *absent* would
 * take a capability away from a property that has the application.
 */
function labelled(
  declared: readonly Tab[],
  requests: Requests,
  roomCare: boolean | null,
): readonly Tab[] {
  return declared.map((tab) => {
    if (tab.label.startsWith("Requests") && requests.jobsInstalled === false) {
      return { ...tab, label: "Requests" };
    }

    if (tab.label === "Servicing" && roomCare === false) {
      // **The count goes with it.** `4 nights` is Room Care's answer, and a
      // property that has not installed Room Care has no such answer — leaving
      // the number on a dimmed tab would be a value nobody could have produced,
      // which is the whole class this platform keeps re-deriving. Frame 5b
      // draws the tab with no count for exactly this reason.
      const { count, ...rest } = tab;
      void count;
      return { ...rest, gone: true };
    }

    return tab;
  });
}

/** Title, identifiers, who manages the stay, and the tab's own action. */
/*
 * `.title`, not `.head` — docs/working/64 §3. This heading STAYS, because it
 * names a record rather than a section: no menu carries "Rajesh Pillai · Room
 * 214", and Jobs keeps the same thing on its own detail screen. What was wrong
 * was the class: `.head` now means the app bar, so a record title was claiming
 * the chrome's own selector.
 */
function header(page: StayPage, tab: string, requests: Requests): HTMLElement {
  const head = el("div", "title");
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
  fill(acts, ...actions(page, tab, requests));

  head.append(title, acts);
  return head;
}

/**
 * The actions, which are the tab's rather than the page's.
 *
 * Each tab's frame carries its own — Overview has the lifecycle three, Activity
 * offers an export, Requests raises a job, Servicing asks for service, Payment
 * links out. Keeping the Overview's three on every tab would offer *Cancel* on
 * a screen about cleaning.
 */
function actions(
  page: StayPage, tab: string, requests: Requests,
): readonly HTMLElement[] {
  if (tab === "Activity") {
    // **Dimmed, and it says why.** Handing a file to the user is the half of
    // SHELL-Q23 still open: a print dialog and a file-save are different shell
    // capabilities, and this one does not exist. A live button would produce
    // nothing and look broken.
    const off = control("btn off", "Export");
    off.append(el("span", "lock no", "NEEDS SHELL-Q23'S FILE-SAVE HALF"));
    return [off];
  }

  if (tab.startsWith("Requests")) {
    return requests.jobsInstalled === false
      ? [control("btn off", "＋ Raise a job")]
      : [control("btn pri", "＋ Raise a job")];
  }

  if (tab === "Servicing") {
    return [control("btn", "Ask for service")];
  }

  if (tab === "Payment") {
    // A link, not an integration: it takes the user to the system that holds
    // the folio and asserts nothing about what is in it.
    return [control("btn", "Open in Opera")];
  }

  return page.actions.map(
    (action) => control(action.danger ? "btn danger" : "btn", action.label));
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
 * Only Documents reaches this now. The bar is drawn in full because the
 * design's information is *which tabs a stay has*; what is not built says so
 * plainly rather than opening a blank page, which is the same honesty the
 * stand-in banner owes.
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

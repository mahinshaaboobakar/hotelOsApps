/**
 * The capture harness — one module realm, driven to a named screen so a frame
 * can be photographed beside the approved drawing.
 *
 * # It fakes the host and nothing else
 *
 * The identity, the property environment, the granted capabilities and the
 * answers to `host.call` are this file's. The module's own code, its
 * stylesheet and its token references are the shipped ones, so what appears
 * here is what a property would see.
 */

import { HostCallError, type HostApi } from "@hotelos/sdk";

import { activate } from "../application";
import type { BoardPage } from "../board";
import { recordedBoard, recordedToday } from "../board/recorded/board";
import { recordedCatalogue } from "../board/recorded/catalogue";
import { recordedJob, recordedRatedJob } from "../board/recorded/job";
import { recordedLive, recordedScheduled } from "../board/recorded/live";
import { recordedSettings } from "../board/recorded/settings";
import { recordedMe } from "../board/recorded/me";
import { recordedEscalated, recordedMine, recordedQuiet } from "../board/recorded/widget";
import { jobsNow } from "../widgets/panel/jobs-now";
import { PANELS } from "./widgets";
import { stylesheet } from "../widgets/sheet";

const params = new URLSearchParams(location.search);

/**
 * Marina Bay: 24-hour, day-month, Asia/Qatar — the frames' own form.
 *
 * `?locale=none` is the checklist's `NL` state: a property whose locale and zone
 * are not established, which §11 draws as ISO, 24-hour, marked UTC.
 */
const PROPERTY = params.get("locale") === "none"
  ? { timezone: null, locale: null }
  : { timezone: "Asia/Qatar", locale: "en-GB" };

const GRANTS = ["job.read", "job.create", "job.assign", "job.complete", "job.cancel", "job.amend", "job.configure", "job.curate"];

/**
 * `?fail=<kind>` — every call refused with that kind, for the page-64b audit.
 *
 * **The real screens, failing, rather than a page that draws the surface on its
 * own.** What the owner saw on 2026-09-18 was the Board failing — a surface
 * placed by a screen inside the module's window — and a harness that rendered
 * the surface in isolation would have photographed the part that was right and
 * missed the placement that was wrong. The kinds are the ones `causeOf` maps:
 * `unavailable` → unanswered, `forbidden` → forbidden, `internal` → faulted —
 * none of them for people, so the Answer fact is the seam's own sentence, as the
 * frame draws it.
 */
const KINDS = [
  "unavailable", "forbidden", "internal",
  // Contract v2's three (ADR 0192, d45f028d) — the page-64 audit covers every
  // cause, so the harness must be able to produce every cause.
  "local_forbidden", "user_forbidden", "model_unavailable",
] as const;
const FAIL = KINDS.find((kind) => kind === params.get("fail")) ?? null;

/**
 * `?data=empty|single` — a list with no rows, or one that fits on one page.
 *
 * **The empty case is where the Board's pager failed, and nobody had captured
 * it** (owner's screenshot, 2026-09-19 12:07): every capture this harness took
 * drew the recorded example, which is 47 jobs across four pages, so the state a
 * new property opens on was never photographed. Absent means the recorded
 * example — the multi-page case. These are harness states, stated as such in
 * the audit's table; they are not a property's data.
 */
const DATA = params.get("data");

function list<T>(rows: readonly T[], single: number): readonly T[] {
  return DATA === "empty" ? [] : DATA === "single" ? rows.slice(0, single) : rows;
}

/**
 * The Board in each of the checklist's five list states.
 *
 * The recorded page is 12 rows of a 47-row list, so the other states are built
 * from it and say so: `last` is page 4 of 4 (11 rows — §6's short last page),
 * `barren` a page past the rows (0 rows, 47 in the list — an empty page of a
 * non-empty list). Absent is the full first page.
 */
function boardFor(state: string | null): BoardPage {
  const recorded = recordedBoard;
  switch (state) {
    case "empty": return { rows: [], paging: { ...recorded.paging, total: 0 } };
    case "single": return { rows: recorded.rows.slice(0, 5), paging: { ...recorded.paging, total: 5 } };
    case "last": return { rows: recorded.rows.slice(0, 11), paging: { ...recorded.paging, page: 3 } };
    case "barren": return { rows: [], paging: { ...recorded.paging, page: 4 } };
    default: return recorded;
  }
}

/**
 * `&only=<method>` — refuse that one call and answer the rest.
 *
 * For the placement 64b does not draw: the Board's figures strip (`today`)
 * failing inside a board that loaded. With every call refused the board never
 * renders, so the strip's own failure could not be photographed at all.
 */
const ONLY = params.get("only");

function host(granted: readonly string[], widget?: "quiet" | "escalated" | "mine"): HostApi {
  return {
    identity: { id: "jobs", version: "0.1.0", capabilities: granted },
    property: PROPERTY,
    call(capability: string, method: string): Promise<unknown> {
      if (FAIL !== null && (ONLY === null || ONLY === method)) {
        return Promise.reject(new HostCallError({ kind: FAIL, message: `${capability}/${method} refused for the audit` }));
      }

      const answers: Record<string, unknown> = {
        me: recordedMe,
        today: recordedToday,
        board: boardFor(DATA),
        job: params.get("job") === "rated"
          ? recordedRatedJob
          : params.get("granted") === "none"
            // A supervisor looking at somebody else's job: not the assignee, so
            // no work controls — the state the read-only pane exists to show.
            ? { ...recordedJob, row: { ...recordedJob.row, viewerIsAssignee: false } }
            : recordedJob,
        live: DATA === null ? recordedLive : { ...recordedLive, departments: list(recordedLive.departments, 1) },
        scheduled: { rows: list(recordedScheduled, 2), paging: { page: 0, pageSize: 12, total: list(recordedScheduled, 2).length } },
        catalogue: DATA === null ? recordedCatalogue : { ...recordedCatalogue, categories: list(recordedCatalogue.categories, 2) },
        settings: recordedSettings,
        jobsNow: widget === "quiet" ? recordedQuiet : widget === "mine" ? recordedMine : recordedEscalated,
      };
      const answer = answers[method];
      return answer === undefined
        ? Promise.reject(new HostCallError({ kind: "unavailable", message: `no answer for ${capability}/${method}` }))
        : Promise.resolve(answer);
    },
    on(): () => void {
      return () => {};
    },
  };
}

/**
 * Every drive step that found nothing to click.
 *
 * **A missed click used to be silent**, which was fine while this page only
 * photographed itself and is not now: the merged sweep keys on a ready signal,
 * and a harness that says "ready" after driving to nothing hands the audit a
 * picture of the wrong screen with no sign that it is wrong. GG's refinement,
 * taken one layer deeper than the signal itself.
 */
const missed: string[] = [];

function click(selector: string, text: string): void {
  for (const node of Array.from(document.querySelectorAll<HTMLElement>(selector))) {
    if (node.textContent?.includes(text) === true) {
      node.click();
      return;
    }
  }

  missed.push(`${selector} containing "${text}"`);
}

/**
 * Say ready — but only when every step landed.
 *
 * On a miss the shared signal is withheld and the page says so on itself. The
 * sweep still measures it through its `readyState` fallback, so the miss lands
 * IN the reading rather than being absent from it: an audit that quietly drops
 * what it could not drive is an audit grading itself.
 */
function ready(): void {
  document.documentElement.setAttribute("data-ready", "true");

  if (missed.length === 0) {
    document.documentElement.setAttribute("data-review-ready", "true");
    return;
  }

  const note = document.createElement("div");
  note.setAttribute("data-missed", "true");
  note.style.cssText = "padding:10px 14px;font:13px system-ui;color:#f87171";
  note.textContent = `This capture was not driven to its screen: ${missed.join("; ")} matched nothing.`;
  document.body.prepend(note);
}

async function settle(): Promise<void> {
  await new Promise((done) => setTimeout(done, 0));
  await new Promise((done) => setTimeout(done, 0));
}


/**
 * Put a value into a control the way a person does, and say so if it did not
 * take.
 *
 * **By parameter where a control exists, by typing only where none can.**
 * Typing into something the screen is supposed to set makes the capture depend
 * on the control under test: a priority chip that fails to set produces an
 * empty capture and twenty divergences that are really one broken control, and
 * the audit blames the drawing (`ARCH-Q20`, 2026-09-10).
 */
function put(name: string, value: string): void {
  const field = document.querySelector<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>(
    `[name="${name}"]`,
  );

  if (field === null) {
    missed.push(`a field named "${name}"`);
    return;
  }

  if (field instanceof HTMLSelectElement) {
    const option = Array.from(field.options).find((o) => o.textContent?.includes(value) === true);
    if (option === undefined) {
      missed.push(`an option reading "${value}" in "${name}"`);
      return;
    }

    field.value = option.value;
  } else {
    field.value = value;
  }

  field.dispatchEvent(new Event("input", { bubbles: true }));
  field.dispatchEvent(new Event("change", { bubbles: true }));
}

/**
 * The state each frame draws, reached before the capture is called ready.
 *
 * A capture of an unfilled form must never reach the audit as a filled one, so
 * every step here records its own miss and `ready()` withholds the shared
 * signal when any of them did.
 */
async function fill(what: string): Promise<void> {
  if (what === "raise") {
    put("locationId", "Room 0817 · Floor 8 · Tower A");
    put("itemId", "Bedside lamp dead");
    put("summary", "Guest says right-side bedside lamp is dead, bulb changed by HK, still dead.");
    await settle();
    return;
  }

  if (what === "resolve") {
    click(".btn", "Refrigerant topped up");
    await settle();
    put("note", "Suction 45 psi, charged to 68. Recommend leak test at next PPM.");
    await settle();
    return;
  }

  missed.push(`a fill named "${what}"`);
}

async function drive(): Promise<void> {
  const widget = params.get("widget");
  if (widget !== null) {
    // **All six, not one.** This drove `jobs-now` and nothing else, so the other
    // five could be described as captured without a capture existing — and every
    // widget picture this stream holds was taken on 2026-09-05, against a host
    // that published seventeen tokens rather than today's nineteen. A defect in
    // an instrument invalidates its output backwards; the fix travels forwards
    // only, so the pictures are re-taken rather than re-labelled.
    const panel = Object.hasOwn(PANELS, widget)
      ? await PANELS[widget]!(host(GRANTS))
      : await jobsNow(host(GRANTS, widget as "quiet" | "escalated" | "mine"));

    document.body.replaceChildren(stylesheet(), panel);
    ready();
    return;
  }

  const granted = params.get("granted") === "none" ? ["job.read"] : GRANTS;
  activate(host(granted)).mount(document.body);
  await settle();

  const screen = params.get("screen");
  if (screen !== null && screen !== "Board") { click(".tab", screen); await settle(); }

  // Frame 1 draws the board of somebody who has been into MRN-ENG-142 and come
  // back — that is why its row is tinted. The capture is driven to the same
  // state the way a person reaches it, rather than the state being set behind
  // the screen's back.
  // A failing board has no row to open — the drive is skipped rather than
  // recorded as a miss, because the miss would be the audit's own doing.
  if ((screen === null || screen === "Board") && (FAIL === null || ONLY !== null) && DATA !== "empty" && DATA !== "barren") {
    if (params.get("open") === null) {
      click(".num", "MRN-ENG-142");
      await settle();
      click(".btn", "‹ Board");
      await settle();
    }
  }

  // The states a top tab cannot reach are opened the way a person opens them —
  // by clicking the control the approved frame draws.
  const open = params.get("open");
  if (open === "raise") { click(".btn", "Raise a job"); await settle(); }
  if (open === "job" || open === "resolve") {
    click(".num", params.get("job") === "rated" ? "MRN-HK-388" : "MRN-ENG-142");
    await settle();
  }
  if (open === "resolve") { click(".btn", "Resolve…"); await settle(); }

  const filling = params.get("fill");
  if (filling !== null) await fill(filling);

  const tab = params.get("tab");
  if (tab !== null) { click(".tab", tab); await settle(); }

  // The policy flow is reached the way a person reaches it: the clock, then
  // the list, then New policy.
  const view = params.get("view");
  if (view !== null) {
    click(".btn", "All policies");
    await settle();
    if (view === "flow") { click(".btn", "＋ New policy"); await settle(); }
  }

  const step = params.get("step");
  if (step !== null) { click(".tab", step); await settle(); }

  // Timers, not `requestAnimationFrame`: a capture tab is often not the
  // foreground one, and rAF does not fire there — the flag would never land
  // while every screen rendered correctly.
  ready();
}

void drive();

/**
 * Copy last week into this one — confirmed first, because it writes a week.
 *
 * `roster.plan · copyWeek` takes the Monday to copy from, the Monday to copy
 * into and the department, and **fills empty cells only**: a cell somebody has
 * already decided for the new week is never changed (`RotaService.CopyWeekAsync`).
 * It cannot lose anything, and it still writes across a whole week at once — so
 * it is a §9 dialog, saying exactly what it will and will not touch, rather than
 * a button that acts on one press. The owner met Copy dead on 0.3.3.
 */

import { formatDay, type HostApi } from "@hotelos/sdk";

import { foot } from "../../chrome/confirm";
import { el } from "../../chrome/element";
import { overlay } from "../../chrome/overlay";
import { UNKNOWN_OUTCOME, write, WriteRefused, type Week } from "../../roster";

/**
 * A calendar day moved by whole days — the ISO date in, the ISO date out.
 *
 * Date arithmetic on the calendar and nothing else: no zone is involved in
 * "the Monday seven days before this Monday", so none is consulted.
 *
 * @param day an ISO date
 * @param days how far to move it
 * @returns the ISO date that many days away
 */
export function shifted(day: string, days: number): string {
  const at = new Date(`${day}T00:00:00Z`);
  at.setUTCDate(at.getUTCDate() + days);
  return at.toISOString().slice(0, 10);
}

/**
 * Build the dialog.
 *
 * @param host the bridge
 * @param week the week on screen — the one copied into
 * @param close called when it is dismissed
 * @param done called after the copy lands, so the grid is re-read
 * @returns the overlay
 */
export function copyWeek(
  host: HostApi, week: Week, close: () => void, done: () => void,
): HTMLElement {
  const from = shifted(week.monday, -7);
  const refusal = el("div", "note warn");
  const acts = foot("Copy week", "Copying…", close);

  acts.onConfirm(() => {
    void (async () => {
      refusal.replaceChildren();
      acts.working(true);

      try {
        await write(host, "roster.plan", "copyWeek", {
          from,
          to: week.monday,
          department: week.departmentCode,
        });
        done();
      } catch (error) {
        refusal.append(el("span", undefined,
          error instanceof WriteRefused ? error.message : UNKNOWN_OUTCOME));
        acts.working(false);

        if (!(error instanceof WriteRefused)) throw error;
      }
    })();
  });

  acts.waitingFor(null);

  const what = el("div", "note");
  what.append(
    el("b", undefined, "Only empty cells are filled. "),
    el("span", undefined,
      "A shift already chosen for this week is left as it is, and so is leave."),
  );

  return overlay("dialog", {
    head: [
      el("div", "ht", `Copy last week into this week?`),
      // formatDay named on each line, not through a local wrapper: the ISO
      // guard (tests/iso-never-rendered) reads for the formatter where a date
      // is drawn, and a wrapper hides it from the one check that looks.
      // "Every department" where the week named none — which is what
      // `copyWeek` then copies, since an empty code is no filter. This printed
      // the echoed filter raw, and a real property's read echoes null.
      el("div", "hsub",
        `${week.department ?? "Every department"}`
        + ` · ${formatDay(from, host.property, "day-month")}`
        + ` – ${formatDay(shifted(from, 6), host.property, "day-month")} into `
        + `${formatDay(week.monday, host.property, "day-month")}`
        + ` – ${formatDay(week.sunday, host.property, "day-month")}`),
    ],
    body: [what, refusal],
    foot: [acts.row],
  }, close);
}
